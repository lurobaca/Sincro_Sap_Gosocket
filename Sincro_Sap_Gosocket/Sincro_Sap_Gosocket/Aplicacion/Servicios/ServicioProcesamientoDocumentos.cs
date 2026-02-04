using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Configuracion;
using Sincro_Sap_Gosocket.Dominio.Entidades;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Peticiones;
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
            IOptions<OpcionesGosocket> gosocketOptions)
        {
            _logger = logger;
            _repositorioCola = repositorioCola;
            _repositorioEstados = repositorioEstados;
            _clienteGosocket = clienteGosocket;
            _sp = sp;
            _traductor = traductor ;

            _gosocketOptions = gosocketOptions?.Value ?? throw new ArgumentNullException(nameof(gosocketOptions));

            // Valida que existan credenciales/URLs (OAuth o Basic). OutputPath no es obligatorio aquí.
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
            var pendientes = await _repositorioCola.ObtenerPendientesAsync(batchSize, ct);
            if (pendientes.Count == 0) return;

            foreach (var doc in pendientes)
            {
                if (ct.IsCancellationRequested) break;

                try
                {

                    _logger.LogInformation("Procesando QueueId={QueueId} ObjType={ObjType} DocEntry={DocEntry} DocSubType={DocSubType}",
                             doc.DocumentosPendientes_Id  , doc.ObjType, doc.DocEntry, doc.DocSubType);


                    // 2) Traer datos + tipo (FE/NC/ND/FEC)
                    var (tipo, datos) = await ConsultarDocumentoAsync(doc, ct);

                    if (datos.Rows.Count == 0)
                        throw new InvalidOperationException($"El SP no devolvió filas para QueueId={doc.DocumentosPendientes_Id} DocNum={doc.DocNum}.");


                    // 3) Traducir (ideal: el traductor sabe el tipo)
                    // AJUSTA AQUÍ si tu ITraductorXml actualmente solo recibe 1 parámetro.
                    var xmlGosocket = _traductor.Traducir(tipo, datos);

                    // 3.1) Guardar XML en disco (si OutputPath está configurado)
                    var rutaXml = GuardarXmlEnDisco(doc, tipo, datos.Rows[0], xmlGosocket);
                    if (!string.IsNullOrWhiteSpace(rutaXml))
                        _logger.LogInformation("XML GoSocket guardado en: {Ruta}", rutaXml);


                    xmlGosocket = NormalizarXml(xmlGosocket);

                    // 2) Construir petición GoSocket
                    var peticion = new PeticionSendDocumentToAuthority
                    {
                        FileContent = xmlGosocket,
                        Async = true,
                        Mapping = "11111111-1111-1111-1111-111111111111",
                        Sign=true,
                        DefaultCertificate =false
                    };

                    // 3) Enviar
                    var respuesta = await _clienteGosocket.EnviarDocumentoAutoridadAsync(peticion, ct);

                    var json = JsonSerializer.Serialize(respuesta);
                    var GlobalDocumentId = TryParseGlobalDocumentIdDesdeJson(json);

                    await _repositorioEstados.MarcarWaitingHaciendaAsync(
                        doc.DocumentosPendientes_Id,
                        GlobalDocumentId,
                        null,
                        json,
                        ct);

                    _logger.LogInformation(
                        "Documento enviado a GoSocket. DocId={DocId} GlobalDocumentId={GlobalDocumentId}",
                        doc.DocumentosPendientes_Id,
                        GlobalDocumentId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando documento. DocId={DocId}", doc.DocumentosPendientes_Id);

                    await _repositorioEstados.MarcarRetryOFalloAsync(
                        doc.DocumentosPendientes_Id,
                        ex.Message,
                        10,
                        ct);
                }
            }
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
        /// Guarda el XML generado en disco usando la ruta configurada en appsettings (GoSocket:OutputPath).
        /// Retorna la ruta completa del archivo creado, o string.Empty si no hay OutputPath configurado.
        /// </summary>
        private string GuardarXmlEnDisco(DocumentoCola item, string tipo, DataRow r0, string xml)
        {
            // Si no se configuró OutputPath, no se guarda (no se considera error).
            if (string.IsNullOrWhiteSpace(_gosocketOptions.OutputPath))
                return string.Empty;

            // Usa consecutivo o clave si viene, si no usa QueueId+DocNum para nombre estable.
            var consecutivo = GetString(r0, "Consecutivo");
            var clave = GetString(r0, "Clave");

            var nombreBase =
                !string.IsNullOrWhiteSpace(consecutivo) ? consecutivo :
                !string.IsNullOrWhiteSpace(clave) ? clave :
                $"{item.DocumentosPendientes_Id}_{item.DocNum}";

            nombreBase = SanitizeFileName(nombreBase);

            var carpeta = _gosocketOptions.OutputPath;

            if (_gosocketOptions.CrearSubcarpetaPorTipo)
                carpeta = Path.Combine(carpeta, tipo);

            if (_gosocketOptions.CrearSubcarpetaPorFecha)
                carpeta = Path.Combine(carpeta, DateTime.Now.ToString("yyyyMMdd"));

            Directory.CreateDirectory(carpeta);

            var fullPath = Path.Combine(carpeta, $"{nombreBase}.xml");

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
            // 1) Elegir SP por tipo
            var spName = item.TipoCE switch
            {
                "FE" => "SP_Consulta_FE_FES_V44",
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
        public async Task ProcesarSeguimientoHaciendaAsync(int batchSize, CancellationToken ct)
        {
            var pendientes = await _repositorioCola.ObtenerPendientesSeguimientoAsync(
                STATUS_WAITING_HACIENDA,
                batchSize,
                ct);

            foreach (var doc in pendientes)
            {
                if (string.IsNullOrWhiteSpace(doc.GoSocket_TrackId))
                    continue;

                try
                {
                    var peticion = new PeticionGetDocument
                    {
                        CodigoDocumento = doc.GoSocket_TrackId.ToString(),
                        CodigoPais = "CR"
                    };

                    var respuesta = await _clienteGosocket.ObtenerDocumentoAsync(peticion, ct);
                    var json = JsonSerializer.Serialize(respuesta);

                    var estado = TryParseEstadoHaciendaDesdeJson(json);
                    var esFinal = EstadosFinalesHacienda.Contains(estado ?? string.Empty);

                    if (esFinal)
                    {
                        await _repositorioEstados.MarcarDoneAsync(
                            doc.DocumentosPendientes_Id,
                            ct);
                    }
                    else
                    {
                        await _repositorioEstados.ActualizarSeguimientoHaciendaAsync(
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
                    _logger.LogError(ex, "Error en seguimiento. DocId={DocId}", doc.DocumentosPendientes_Id);

                    await _repositorioEstados.MarcarRetryOFalloAsync(
                        doc.DocumentosPendientes_Id,
                        ex.Message,
                        10,
                        ct);
                }
            }
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

        private static string? TryParseEstadoHaciendaDesdeJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.TryGetProperty("Estado", out var v)
                    ? v.GetString()
                    : null;
            }
            catch { return null; }
        }
    }
}
