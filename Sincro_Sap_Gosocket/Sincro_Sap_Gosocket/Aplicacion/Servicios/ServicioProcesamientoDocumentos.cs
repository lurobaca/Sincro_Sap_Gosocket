using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SAPbobsCOM;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Configuracion;
using Sincro_Sap_Gosocket.Dominio.Entidades;
using Sincro_Sap_Gosocket.Dominio.Enumeraciones;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Comun;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Peticiones;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Respuestas;
using Sincro_Sap_Gosocket.Infraestructura.Logs;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sincro_Sap_Gosocket.Aplicacion.Servicios
{
    public class ServicioProcesamientoDocumentos : IServicioProcesamientoDocumentos
    {
        private const string STATUS_WAITING_HACIENDA = "WAITING_HACIENDA";
        private readonly IServicioActualizacionSap _servicioActualizacionSap;

        private static readonly HashSet<string> EstadosFinalesHacienda =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "ACEPTADO",
                "RECHAZADO",
                "BAD_REQUEST"
            };

        private readonly ILogger<ServicioProcesamientoDocumentos> _logger;
        private readonly IRepositorioColaDocumentos _repositorioCola;
        private readonly IRepositorioEstados _repositorioEstados;
        private readonly IClienteGosocket _clienteGosocket;
        private readonly IEjecutorProcedimientos _sp;
        private readonly ITraductorXml _traductor;
        private readonly OpcionesGosocket _gosocketOptions;

     
        public ServicioProcesamientoDocumentos(
        ILogger<ServicioProcesamientoDocumentos> logger,
        IRepositorioColaDocumentos repositorioCola,
        IRepositorioEstados repositorioEstados,
        IEjecutorProcedimientos sp,
        IClienteGosocket clienteGosocket,
        ITraductorXml traductor,
        IServicioActualizacionSap servicioActualizacionSap,
        IOptions<OpcionesGosocket> gosocketOptions)
        {
            _logger = logger;
            _repositorioCola = repositorioCola;
            _repositorioEstados = repositorioEstados;
            _clienteGosocket = clienteGosocket;
            _sp = sp;
            _traductor = traductor;
            _servicioActualizacionSap = servicioActualizacionSap;

            _gosocketOptions = gosocketOptions?.Value ?? throw new ArgumentNullException(nameof(gosocketOptions));
            _gosocketOptions.ValidarConfiguracion(exigirOutputPath: false);
        }

        /// <summary>
        /// Procesa un lote de documentos en estado pendiente (cola) y los envía a GoSocket para su validación ante Hacienda,
        /// utilizando el XML previamente generado y almacenado en disco.
        /// </summary>
        /// <param name="batchSize">
        /// Cantidad máxima de documentos a procesar en esta ejecución (lote). Se usa para controlar carga y tiempos del Worker.
        /// </param>
        /// <param name="ct">
        /// Token de cancelación del Worker/Host. Si se solicita cancelación, el procesamiento se detiene de forma ordenada.
        /// </param>
        /// <remarks>
        /// Flujo general:
        /// 1) Consulta en la cola los documentos "pendientes" (por ejemplo: PENDING/RETRY) según la política del repositorio.
        /// 2) Por cada documento:
        ///    a) Valida que exista ruta de XML (XmlFilePath) y que el archivo exista físicamente.
        ///       - Si falta o no existe: marca el documento para reintento o fallo (según la política) y continúa con el siguiente.
        ///    b) Lee el XML desde disco.
        ///    c) Construye la petición a GoSocket (SendDocumentToAuthority) con datos mínimos requeridos:
        ///       - DocumentoXml: contenido del XML.
        ///       - TipoDocumento: tipo de comprobante (TipoCE).
        ///       - CodigoPais: "CR".
        ///       - Asincrono: true (el resultado final se consulta posteriormente).
        ///    d) Envía el documento a GoSocket.
        ///    e) Serializa la respuesta y obtiene un TrackId (si existe) para seguimiento.
        ///    f) Actualiza el estado del documento en la cola a "WAITING_HACIENDA" guardando:
        ///       - TrackId (si se pudo extraer),
        ///       - JSON de respuesta de GoSocket.
        /// 3) Si ocurre una excepción durante el envío/lectura/marcado:
        ///    - Registra el error en logs.
        ///    - Marca el documento para reintento o fallo (con el mensaje de error y el máximo de intentos configurado).
        ///
        /// Consideraciones:
        /// - Este método NO consulta el estado final en Hacienda; únicamente deja el documento en seguimiento (WAITING_HACIENDA).
        /// - El repositorio de estados define la política real de reintentos (incremento de AttemptCount, NextAttemptAt, etc.).
        /// - Se detiene el bucle si ct solicita cancelación, evitando dejar operaciones a medias en un apagado controlado.
        /// </remarks>
        /// <exception cref="OperationCanceledException">
        /// Puede propagarse si la cancelación ocurre durante operaciones async (lectura de archivo, ejecución SQL, llamada HTTP).
        /// </exception>
        public async Task ProcesarPendientesAsync(int batchSize, CancellationToken ct)
        {
            TrazaArchivo.Escribir("Ejecuta ProcesarPendientesAsync / ObtenerPendientesAsync");

            var pendientes = await _repositorioCola.ObtenerPendientesAsync(batchSize, ct);
            if (pendientes.Count == 0) return;

            foreach (var doc in pendientes)
            {
                if (ct.IsCancellationRequested) break;

                try
                {

                    TrazaArchivo.Escribir($"Procesando comprobante DocNum={doc.DocNum} Tipo={doc.TipoCE} " );

                    _logger.LogInformation("Procesando QueueId={QueueId} ObjType={ObjType} DocEntry={DocEntry} DocSubType={DocSubType}",
                             doc.DocumentosPendientes_Id  , doc.ObjType, doc.DocEntry, doc.DocSubType);


                    // 2) Traer datos + tipo (FE/NC/ND/FEC)
                    var (tipo, datos) = await ConsultarDocumentoAsync(doc, ct);

                    if (datos.Rows.Count == 0) {
                        TrazaArchivo.Escribir($"El SP ConsultarDocumentoAsync no devolvió filas para DocNum={doc.DocNum} Tipo={doc.TipoCE}.");
                        throw new InvalidOperationException($"El SP ConsultarDocumentoAsync no devolvió filas para DocNum={doc.DocNum} Tipo={doc.TipoCE}.");
                    }
                       
                    var claveComprobante = ObtenerClaveDesdeResultadoSp(datos);

                    TrazaArchivo.Escribir($"Se obtuvo la Clave:{claveComprobante} para el comprobante DocNum={doc.DocNum} Tipo={doc.TipoCE} ");

                    if (!string.IsNullOrWhiteSpace(claveComprobante))
                    {
                        TrazaArchivo.Escribir($"Ejecuta ActualizarClaveDocumentoPendienteAsync para el comprobante DocNum={doc.DocNum} Tipo={doc.TipoCE}.");
                        await _repositorioCola.ActualizarClaveDocumentoPendienteAsync(
                            doc.DocumentosPendientes_Id,
                            claveComprobante,
                            ct);
                    }

                    TrazaArchivo.Escribir($"Ejecuta el metodo Traducir para el comprobante DocNum={doc.DocNum} Tipo={doc.TipoCE} .");
                    // 3) Traducir (ideal: el traductor sabe el tipo)
                    // AJUSTA AQUÍ si tu ITraductorXml actualmente solo recibe 1 parámetro.
                    var xmlGosocket = _traductor.Traducir(tipo, datos);

                    // 3.1) Guardar XML en disco (si OutputPath está configurado)
                    var rutaXml = GuardarXmlEnDisco(doc, tipo, datos.Rows[0], xmlGosocket, "Comprobante");

                    if (!string.IsNullOrWhiteSpace(rutaXml)) {
                        TrazaArchivo.Escribir($"XML GoSocket guardado en: {rutaXml}");
                        _logger.LogInformation("XML GoSocket guardado en: {Ruta}", rutaXml);
                    }
                    
                    xmlGosocket = NormalizarXml(xmlGosocket);

                    // 2) Construir petición GoSocket
                    var peticion = new PeticionSendDocumentToAuthority
                    {
                        FileContent = xmlGosocket,
                        Async = true,
                        Mapping = "11111111-1111-1111-1111-111111111111",
                        Sign=true,
                        DefaultCertificate =false,
                        Folio= doc.DocNum
                    };

                    // 3) Enviar
                    //var respuesta = await _clienteGosocket.EnviarDocumentoAutoridadAsync(peticion, ct);

                    //var json = JsonSerializer.Serialize(respuesta);
                    //var GlobalDocumentId = TryParseGlobalDocumentIdDesdeJson(json);

                    TrazaArchivo.Escribir(
                         $"Ejecuta EnviarDocumentoAutoridadAsync Peticion: " +
                         $"FileContentLength={(peticion.FileContent?.Length ?? 0)} | " +
                         $"Async={peticion.Async} | " +
                         $"Mapping={peticion.Mapping} | " +
                         $"Sign={peticion.Sign} | " +
                         $"DefaultCertificate={peticion.DefaultCertificate} | " +
                         $"Folio={peticion.Folio}"
                     );

                    var respuesta = await _clienteGosocket.EnviarDocumentoAutoridadAsync(peticion, ct);

                    // Convertir a JSON (solo como puente)
                    var json = JsonSerializer.Serialize(respuesta);

                    // Caer en tu estructura
                    var envelope = JsonSerializer.Deserialize<GosocketRespuesta>(json);
                    var envioOk = envelope?.Exitoso == true && envelope?.Datos?.Success == true;
                    var GlobalDocumentId = envelope?.Datos?.GlobalDocumentId;

                    var code = envelope?.Datos?.Code ?? envelope?.CodigoError ?? "N/A";
                    var desc = envelope?.Datos?.Description ?? envelope?.MensajeError ?? "Error sin descripción.";
                    var detalle = (envelope?.Datos?.Messages != null && envelope.Datos.Messages.Count > 0  )
                        ? string.Join(" | ", envelope.Datos.Messages  )
                        : null;

                    if (!desc.Equals("")) detalle = "Messages: "+ detalle + " | MensajeError:" + desc;

                    var TextoRespuesa = $"{detalle}";
                    TrazaArchivo.Escribir($"GuardarXmlEnDisco Respuesta: {TextoRespuesa}");
                    GuardarXmlEnDisco(doc, tipo, datos.Rows[0], TextoRespuesa, "Respuesta");

                    if (!envioOk)
                    {

                        TrazaArchivo.Escribir($"GoSocket rechazó el envío para el comprobante DocNum={doc.DocNum} Tipo={doc.TipoCE} Detalle={detalle}");

                        _logger.LogWarning(
                             "GoSocket rechazó el envío. QueueId={QueueId} GlobalDocumentId={GlobalDocumentId} Code={Code} Desc={Desc}{Detalle}",
                             doc.DocumentosPendientes_Id,
                             GlobalDocumentId,
                             code,
                             desc,
                             detalle is null ? "" : $" Detalle={detalle}"
                         );

                        throw new InvalidOperationException(
                           $"GoSocket rechazó el envío para el comprobante DocNum={doc.DocNum} Tipo={doc.TipoCE} Detalle={detalle}"
                        );
                    }


                    await _repositorioEstados.MarcarWaitingHaciendaAsync(
                        doc.DocumentosPendientes_Id,
                        GlobalDocumentId,
                        null,
                        json,
                        ct);

                    var actualizacionSapEnvio = new ActualizacionEstadoHacienda
                    {
                        TipoDocumento = MapearTipoDocumentoSap(doc.TipoCE),
                        DocEntry = doc.DocEntry,
                        EstadoHacienda = "ENVIADO",
                        MensajeHacienda = "Documento enviado a GoSocket, pendiente respuesta de Hacienda",
                        Clave = string.IsNullOrWhiteSpace(claveComprobante) ? null : claveComprobante,
                        FechaRespuestaTexto = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        CampoEstado = "U_EstadoHacienda",
                        CampoMensaje = "U_RespuestaHacienda",
                        CampoClave = "U_ClaveHacienda",
                        CampoFechaRespuesta = "U_FechaRespHacienda",
                        Reintenta = "U_Reintenta"
                    };

                    TrazaArchivo.Escribir(
                                        $"Ejecuta ActualizarEstadoHaciendaEnSapAsync Peticion: " +
                                        $"TipoDocumento={actualizacionSapEnvio.TipoDocumento} | " +
                                        $"DocEntry={actualizacionSapEnvio.DocEntry} | " +
                                        $"EstadoHacienda={actualizacionSapEnvio.EstadoHacienda} | " +
                                        $"MensajeHacienda={actualizacionSapEnvio.MensajeHacienda} | " +
                                        $"Clave={actualizacionSapEnvio.Clave} | " +
                                        $"FechaRespuestaTexto={actualizacionSapEnvio.FechaRespuestaTexto} | " +
                                        $"CampoEstado={actualizacionSapEnvio.CampoEstado} | " +
                                        $"CampoMensaje={actualizacionSapEnvio.CampoMensaje} | " +
                                        $"CampoClave={actualizacionSapEnvio.CampoClave} | " +
                                        $"CampoFechaRespuesta={actualizacionSapEnvio.CampoFechaRespuesta} | " +
                                        $"Reintenta={actualizacionSapEnvio.Reintenta}"
                                    );

                    //PENDIENTE DE HABILITAR NO BORRAR
                    await _servicioActualizacionSap.ActualizarEstadoHaciendaEnSapAsync(actualizacionSapEnvio, ct);

                    TrazaArchivo.Escribir($"Documento enviado a GoSocket. DocId={doc.DocumentosPendientes_Id} GlobalDocumentId={GlobalDocumentId}");

                    _logger.LogInformation(
                        "Documento enviado a GoSocket. DocId={DocId} GlobalDocumentId={GlobalDocumentId}",
                        doc.DocumentosPendientes_Id,
                        GlobalDocumentId);

                   await _repositorioCola.ActualizarConsecutivoHaciendaAsync (doc.TipoCE,  ct);

                }
                catch (Exception ex)
                {
                    TrazaArchivo.Escribir($"Error enviando documento para el comprobante Tipo={doc.TipoCE} DocNum={doc.DocNum} Detalle={ex.Message}.");
          
                    _logger.LogError(ex, "Error enviando documento. DocId={DocId}", doc.DocumentosPendientes_Id);

                    await _repositorioEstados.MarcarRetryOFalloAsync(
                        doc.DocumentosPendientes_Id,
                        ex.Message,
                        10,
                        ct);
                }
            }
        }
        private static string ObtenerClaveDesdeResultadoSp(DataTable datosSp)
        {
            TrazaArchivo.Escribir($"Ejecuta ObtenerClaveDesdeResultado");

            if (datosSp == null)
                throw new ArgumentNullException(nameof(datosSp));

            if (datosSp.Rows.Count == 0)
                return string.Empty;

            if (!datosSp.Columns.Contains("Clave"))
                return string.Empty;

            var valorClave = datosSp.Rows[0]["Clave"];

            if (valorClave == null || valorClave == DBNull.Value)
                return string.Empty;

            return Convert.ToString(valorClave)?.Trim() ?? string.Empty;
        }
        static string NormalizarXml(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml)) return xml;

            // Quitar BOM si viene embebido al inicio del string
            xml = xml.TrimStart('\uFEFF', '\u200B');

            // Asegurar saltos de línea consistentes (opcional)
            xml = xml.Replace("\r\n", "\n");

            return xml.Trim();
        }
        /// <summary>
        /// Guarda un archivo XML en disco usando la ruta configurada en appsettings (GoSocket:OutputPath).
        /// Crea una carpeta por comprobante usando el consecutivo interno de SAP, y dentro de ella
        /// almacena únicamente los archivos vinculados a ese comprobante:
        /// - XML de GoSocket
        /// - Respuesta de GoSocket
        /// - XML de Hacienda
        /// Retorna la ruta completa del archivo creado, o string.Empty si no hay OutputPath configurado.
        /// </summary>
        private string GuardarXmlEnDisco(DocumentoCola item, string tipo, DataRow r0, string xml, string prefijo)
        {
            TrazaArchivo.Escribir($"Ejecuta GuardarXmlEnDisco del comprobante DocNum={item.DocNum} Tipo={item.TipoCE} ");

            // Si no se configuró OutputPath, no se guarda (no se considera error).
            if (string.IsNullOrWhiteSpace(_gosocketOptions.OutputPath))
                return string.Empty;

            // Consecutivo interno de SAP para nombrar la carpeta.
            // Ajustá el nombre de la columna si en tu DataRow viene con otro nombre.
            var consecutivoInternoSap = GetString(r0, "DocNum");

            // Fallback por si no viene DocNum
            if (string.IsNullOrWhiteSpace(consecutivoInternoSap))
                consecutivoInternoSap = $"{item.DocumentosPendientes_Id}_{item.DocNum}";

            consecutivoInternoSap = SanitizeFileName(consecutivoInternoSap);

            // Carpeta raíz configurada
            var carpetaRaiz = _gosocketOptions.OutputPath;

            if (_gosocketOptions.CrearSubcarpetaPorTipo)
                carpetaRaiz = Path.Combine(carpetaRaiz, tipo);

            // Carpeta exclusiva del comprobante
            var carpetaComprobante = Path.Combine(carpetaRaiz, consecutivoInternoSap);

            Directory.CreateDirectory(carpetaComprobante);

            // Nombre del archivo dentro de la carpeta del comprobante
            var nombreArchivo = $"{prefijo}.xml";
            var fullPath = Path.Combine(carpetaComprobante, nombreArchivo);

            // UTF-8 sin BOM
            var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            File.WriteAllText(fullPath, xml, utf8NoBom);

            return fullPath;
        }
        private static string GetString(DataRow r, string col)
        {
            if (r.Table.Columns.Contains(col) && r[col] != DBNull.Value)
                return Convert.ToString(r[col])?.Trim() ?? string.Empty;

            return string.Empty;
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            return name;
        }

        /// <summary>
        /// Consulta el SP correspondiente y retorna (Tipo, DataTable).
        /// En su SP actual, el DataTable contiene 1 fila por línea de detalle,
        /// repitiendo columnas de encabezado/emisor/receptor en cada fila.
        /// </summary>
        private async Task<(string Tipo, DataTable Datos)> ConsultarDocumentoAsync(DocumentoCola item, CancellationToken ct)
        {
            TrazaArchivo.Escribir($"Ejecuta ConsultarDocumentoAsync DocNum={item.DocNum} Tipo={item.TipoCE} ");

            // 1) Elegir SP por tipo
            var spName = item.TipoCE switch
            {
                "FE" or "TE" => "SP_Consulta_FE_FES_V44",
                "NC" => "SP_Consulta_NC_NCS_V44",
                "ND" => "SP_Consulta_ND_NDS_V44",
                "FEC" => "SP_Consulta_FEC_V44",
                _ => throw new InvalidOperationException($"Tipo no soportado: {item.TipoCE}")
            };

            // 2) Ejecutar SP
            // Nota: su SP parece esperar @DocNum, @Situacion_de_Comprobante y @Tipo.
            // Ajuste tamaños si su definición real es distinta.
            var parametros = new[]
            {
                new SqlParameter("@DocNum", SqlDbType.VarChar, 50)
                {
                    Value = (object?)item.DocNum ?? DBNull.Value
                },
                new SqlParameter("@Situacion_de_Comprobante", SqlDbType.VarChar, 1)
                {
                    // Usted había puesto Value=1. Si el SP es VarChar, mande "1".
                    Value = "1"
                },
                new SqlParameter("@Tipo", SqlDbType.VarChar, 1)
                {
                    // En su ejemplo usa "I" (probablemente "Invoice" o similar).
                    Value = "I"
                },
            };

            var datos = await _sp.EjecutarDataTableAsync(spName, parametros, ct);
            return (item.TipoCE, datos);
        }



        /// <summary>
        /// Consulta estados de Hacienda vía GoSocket.
        /// </summary>
        //public async Task ProcesarSeguimientoHaciendaAsync(int batchSize, CancellationToken ct)
        //{
        //    var pendientes = await _repositorioCola.ObtenerPendientesSeguimientoAsync(
        //        STATUS_WAITING_HACIENDA,
        //        batchSize,
        //        ct);

        //    foreach (var doc in pendientes)
        //    {
        //        if (string.IsNullOrWhiteSpace(doc.GoSocket_TrackId))
        //            continue;

        //        try
        //        {
        //            var peticion = new PeticionGetDocument
        //            {
        //                CodigoDocumento = doc.GoSocket_TrackId.ToString(),
        //                CodigoPais = "CR"
        //            };

        //            var respuesta = await _clienteGosocket.ObtenerDocumentoAsync(peticion, ct);
        //            var json = JsonSerializer.Serialize(respuesta);

        //            var estado = TryParseEstadoHaciendaDesdeJson(json);
        //            var esFinal = EstadosFinalesHacienda.Contains(estado ?? string.Empty);

        //            if (esFinal)
        //            {
        //                await _repositorioEstados.MarcarDoneAsync(
        //                    doc.DocumentosPendientes_Id,
        //                    ct);
        //            }
        //            else
        //            {
        //                await _repositorioEstados.ActualizarSeguimientoHaciendaAsync(
        //                    doc.DocumentosPendientes_Id,
        //                    estado,
        //                    json,
        //                    esFinal,
        //                    10,
        //                    ct);
        //            }


        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Error en seguimiento. DocId={DocId}", doc.DocumentosPendientes_Id);

        //            await _repositorioEstados.MarcarRetryOFalloAsync(
        //                doc.DocumentosPendientes_Id,
        //                ex.Message,
        //                10,
        //                ct);
        //        }
        //    }
        //}


        /// <summary>
        /// Consulta estados de Hacienda vía GoSocket.
        /// </summary>
        public async Task ProcesarSeguimientoHaciendaAsync(int batchSize, CancellationToken ct)
        {

            TrazaArchivo.Escribir("Ejecuta ObtenerPendientesSeguimientoAsync STATUS_WAITING_HACIENDA");
            var pendientes = await _repositorioCola.ObtenerPendientesSeguimientoAsync(
                STATUS_WAITING_HACIENDA,
                batchSize,
                ct);

            TrazaArchivo.Escribir($"Resultado de ejecutar ObtenerPendientesSeguimientoAsync pendientes={pendientes.Count}");

            foreach (var doc in pendientes)
            {
                TrazaArchivo.Escribir($"Obteniendo estado de Hacienda para DocId={doc.DocumentosPendientes_Id} DocNum={doc.DocNum} TipoCE={doc.TipoCE} TrackId={doc.GoSocket_TrackId}");
    
                _logger.LogInformation("Obteniendo estado de Hacienda. DocId={DocId} DocNum={DocNum} TipoCE={TipoCE} TrackId={TrackId}",
                        doc.DocumentosPendientes_Id,
                        doc.DocNum,
                        doc.TipoCE,
                        doc.GoSocket_TrackId);
               
                if (string.IsNullOrWhiteSpace(doc.GoSocket_TrackId))
                    continue;

                try
                {
                    var peticion = new PeticionGetDocument
                    {
                        GlobalDocumentId = doc.GoSocket_TrackId,
                        SenderCode=doc.SenderCode,
                        Country = "CR"
                    };

                    
                    TrazaArchivo.Escribir($"Ejecuta ObtenerDocumentoAsync CodigoDocumento={doc.GoSocket_TrackId}");

                    var respuesta = await _clienteGosocket.ObtenerDocumentoAsync(peticion, ct);

                    if (!respuesta.Exitoso || respuesta.Datos == null)
                    {
                        TrazaArchivo.Escribir(
                                        $"[SEGUIMIENTO_HACIENDA][SIN_RESPUESTA] " +
                                        $"DocPendienteId={doc.DocumentosPendientes_Id} | " +
                                        $"DocEntry={doc.DocEntry} | " +
                                        $"DocNum={doc.DocNum} | " +
                                        $"TipoCE={doc.TipoCE} | " +
                                        $"TrackId={doc.GoSocket_TrackId} | " +
                                        $"StatusActual={doc.Status} | " +
                                        $"FechaDoc={doc.TaxDate:yyyy-MM-dd} | " +
                                        $"Exitoso={respuesta?.Exitoso} | " +
                                        $"TieneDatos={(respuesta?.Datos != null)}"
                                    );

                        await _repositorioEstados.ActualizaEstadoHaciendaEnDocumentosPendientesAsync(
                            doc.DocumentosPendientes_Id,
                            "SIN_RESPUESTA",
                            JsonSerializer.Serialize(respuesta),
                            false,
                            10,
                            ct);

                        continue;
                    } 

                    var datos = respuesta.Datos;

                    if (datos.Documents == null || datos.Documents.Count == 0)
                    {
                        await _repositorioEstados.ActualizaEstadoHaciendaEnDocumentosPendientesAsync(
                            doc.DocumentosPendientes_Id,
                            "SIN_DOCUMENTOS",
                            JsonSerializer.Serialize(respuesta),
                            false,
                            10,
                            ct);

                        continue;
                    }

                    var documento = datos.Documents[0];
                    var json = JsonSerializer.Serialize(documento);

                    var authorityStatusCodigo = ObtenerTag(documento, "AuthorityStatus");
                    var estado = MapearAuthorityStatus(authorityStatusCodigo);

                    var notaMh = ObtenerNotaMh(documento);
                    var fechaRespuesta = notaMh?.TimeStamp ?? documento.AuthorityTimeStamp;
                    var mensajeHacienda = ConstruirMensajeHacienda(documento, estado);

                    var esFinal =
                        string.Equals(estado, "ACEPTADO", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(estado, "RECHAZADO", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(estado, "BAD_REQUEST", StringComparison.OrdinalIgnoreCase);

                    if (esFinal)
                    {
                        TrazaArchivo.Escribir($"Ejecuta MarcarDoneAsync. Del comprobante DocNm={doc.DocNum} TipoCE={doc.TipoCE} Estado={estado}");

                        //Marcar como Done en nuestra cola/estado para que no se vuelva a procesar (si ya tine un estado final)
                        await _repositorioEstados.MarcarDoneAsync(doc.DocumentosPendientes_Id, ct, estado, mensajeHacienda);

                        var actualizacionSap = new ActualizacionEstadoHacienda
                        {
                            TipoDocumento = MapearTipoDocumentoSap(doc.TipoCE),
                            DocEntry = doc.DocEntry,
                            EstadoHacienda = estado,
                            MensajeHacienda = mensajeHacienda,
                            Clave = documento.CountryDocumentId,
                            FechaRespuestaTexto = fechaRespuesta?.ToString("yyyy-MM-dd HH:mm:ss")
                           ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            CampoEstado = "U_EstadoHacienda",
                            CampoMensaje = "U_RespuestaHacienda",
                            CampoClave = "U_ClaveHacienda",
                            CampoFechaRespuesta = "U_FechaRespuesta"
                        };

                        TrazaArchivo.Escribir($"Ejecuta ActualizarEstadoHaciendaEnSapAsync. Del comprobante DocNm={doc.DocNum} TipoCE={doc.TipoCE} Estado={estado} Mensaje Hacienda={mensajeHacienda}");


                        //PENDIENTE DE HABILITAR NO BORRAR
                        await _servicioActualizacionSap.ActualizarEstadoHaciendaEnSapAsync(actualizacionSap, ct);
                    }
                    else
                    {
                        TrazaArchivo.Escribir($"Ejecuta ActualizarEstadoHaciendaAsync. Del comprobante DocNm={doc.DocNum} TipoCE={doc.TipoCE} Estado={estado} Mensaje Hacienda={mensajeHacienda}");

                        await _repositorioEstados.ActualizaEstadoHaciendaEnDocumentosPendientesAsync(
                            doc.DocumentosPendientes_Id,
                            estado,
                            json,
                            esFinal,
                            10,
                            ct);
                    }
                }
                catch (Exception ex)
                {

                    TrazaArchivo.Escribir($"Error en seguimiento Hacienda. Del comprobante DocNm={doc.DocNum } TipoCE={doc.TipoCE}");

                    _logger.LogError(ex,
                        "Error en seguimiento Hacienda. QueueId={QueueId} DocEntry={DocEntry} TipoCE={TipoCE}",
                        doc.DocumentosPendientes_Id,
                        doc.DocEntry,
                        doc.TipoCE);

                    await _repositorioEstados.MarcarRetryOFalloAsync(
                        doc.DocumentosPendientes_Id,
                        ex.Message,
                        10,
                        ct);
                }
            }
        }
        private static string ConstruirMensajeHacienda(GetDocumentItem documento, string estado)
        {
            var partes = new List<string>();

            var notaMh = ObtenerNotaMh(documento);

            //if (notaMh?.TimeStamp != null)
            //    partes.Add($"Fecha MH: {notaMh.TimeStamp:yyyy-MM-dd HH:mm:ss}");

            //if (!string.IsNullOrWhiteSpace(estado))
            //    partes.Add($"Estado: {estado}");

            //if (!string.IsNullOrWhiteSpace(notaMh?.Code))
            //    partes.Add($"Código MH: {notaMh.Code}");

            if (!string.IsNullOrWhiteSpace(notaMh?.Note))
                partes.Add(notaMh.Note.Trim());

            //if (partes.Count == 0 && documento.AuthorityTimeStamp != null)
            //    partes.Add($"Fecha MH: {documento.AuthorityTimeStamp:yyyy-MM-dd HH:mm:ss}");

            var texto = string.Join(" | ", partes);
            return LimitarTexto(texto, 250);
        }
        private static string ObtenerTag(GetDocumentItem documento, string codigo)
        {
            if (documento?.DocumentTags == null || documento.DocumentTags.Count == 0)
                return string.Empty;

            var tag = documento.DocumentTags.Find(x =>
                string.Equals(x.Code, codigo, StringComparison.OrdinalIgnoreCase));

            return tag?.Value?.Trim() ?? string.Empty;
        }

        private static GetDocumentNote? ObtenerNotaMh(GetDocumentItem documento)
        {
            if (documento?.Notes == null || documento.Notes.Count == 0)
                return null;

            return documento.Notes.Find(x =>
                string.Equals(x.Source, "MH", StringComparison.OrdinalIgnoreCase));
        }

        private static string MapearAuthorityStatus(string codigo)
        {
            return (codigo ?? string.Empty).Trim() switch
            {
                "1" => "RECIBIDO",
                "2" => "ACEPTADO",
                "3" => "RECHAZADO",
                "4" => "PROCESANDO",
                _ => string.IsNullOrWhiteSpace(codigo) ? "SIN_ESTADO" : $"AUTHORITY_STATUS_{codigo}"
            };
        }

         
        private static string LimitarTexto(string texto, int maximo)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            texto = texto.Trim();
            return texto.Length <= maximo ? texto : texto.Substring(0, maximo);
        }
        private static string ResumirRespuestaSap(string estado, string json)
        {
            var texto = $"Estado={estado}";
            if (!string.IsNullOrWhiteSpace(json))
                texto += " | Respuesta recibida de Hacienda";

            return texto.Length > 250 ? texto.Substring(0, 250) : texto;
        }

        private static string? TryParseGlobalDocumentIdDesdeJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);

                // 1) Entrar a "Datos"
                if (!doc.RootElement.TryGetProperty("Datos", out var datos))
                    return null;

                // 2) Leer "TrackId"
                if (!datos.TryGetProperty("GlobalDocumentId", out var trackProp))
                    return null;

                var trackId = trackProp.GetString();
                return string.IsNullOrWhiteSpace(trackId) ? null : trackId;
            }
            catch
            {
                return null;
            }
        }
        private static TipoDocumentoSap MapearTipoDocumentoSap(string tipoCe)
        {
            return (tipoCe ?? string.Empty).Trim().ToUpperInvariant() switch
            {
                "FE" => TipoDocumentoSap.Factura,
                "TE" => TipoDocumentoSap.Factura,
                "FEC" => TipoDocumentoSap.Factura,
                "NC" => TipoDocumentoSap.NotaCredito,
                "ND" => TipoDocumentoSap.NotaDebito,
                _ => throw new InvalidOperationException($"TipoCE no soportado para SAP: {tipoCe}")
            };
        }
        //private static string? TryParseEstadoHaciendaDesdeJson(string json)
        //{
        //    try
        //    {
        //        using var doc = JsonDocument.Parse(json);
        //        return doc.RootElement.TryGetProperty("Estado", out var v)
        //            ? v.GetString()
        //            : null;
        //    }
        //    catch { return null; }
        //}
        //private static string? TryParseEstadoHaciendaDesdeJson(string json)
        //{
        //    try
        //    {
        //        using var doc = JsonDocument.Parse(json);

        //        if (!doc.RootElement.TryGetProperty("Datos", out var datos))
        //            return null;

        //        return datos.TryGetProperty("Estado", out var estado)
        //            ? estado.GetString()
        //            : null;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}
    }
}
