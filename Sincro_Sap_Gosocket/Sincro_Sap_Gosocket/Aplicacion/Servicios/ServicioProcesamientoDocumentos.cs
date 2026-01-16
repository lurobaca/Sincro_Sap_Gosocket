using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Configuracion;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Comun;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Peticiones;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Respuestas;
using System;
using System.Data;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Sincro_Sap_Gosocket.Aplicacion.Servicios
{
    /// <summary>
    /// Orquesta el procesamiento de documentos pendientes:
    /// 1) Reclama pendientes de la cola
    /// 2) Consulta datos mediante SP
    /// 3) Traduce a XML GoSocket (DTE Genérico)
    /// 4) Guarda XML en disco (según appsettings)
    /// 5) Envía a GoSocket
    /// 6) Marca estado DONE o RETRY/FAIL
    /// </summary>
    public sealed class ServicioProcesamientoDocumentos : IServicioProcesamientoDocumentos
    {
        private readonly ILogger<ServicioProcesamientoDocumentos> _logger;
        private readonly IRepositorioColaDocumentos _cola;
        private readonly IRepositorioEstados _estados;
        private readonly IEjecutorProcedimientos _sp;
        private readonly ITraductorXml _traductor;
        private readonly IClienteGosocket _clienteGosocket;
        private readonly OpcionesGosocket _gosocketOptions;

        public ServicioProcesamientoDocumentos(
            ILogger<ServicioProcesamientoDocumentos> logger,
            IRepositorioColaDocumentos cola,
            IRepositorioEstados estados,
            IEjecutorProcedimientos sp,
            ITraductorXml traductor,
            IClienteGosocket clienteGosocket,
            IOptions<OpcionesGosocket> gosocketOptions)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cola = cola ?? throw new ArgumentNullException(nameof(cola));
            _estados = estados ?? throw new ArgumentNullException(nameof(estados));
            _sp = sp ?? throw new ArgumentNullException(nameof(sp));
            _traductor = traductor ?? throw new ArgumentNullException(nameof(traductor));
            _clienteGosocket = clienteGosocket ?? throw new ArgumentNullException(nameof(clienteGosocket));

            _gosocketOptions = gosocketOptions?.Value ?? throw new ArgumentNullException(nameof(gosocketOptions));

            // Valida que existan credenciales/URLs (OAuth o Basic). OutputPath no es obligatorio aquí.
            _gosocketOptions.ValidarConfiguracion(exigirOutputPath: false);
        }

        public async Task ProcesarPendientesAsync(int batchSize, CancellationToken ct)
        {
            // 1) Reclamar lote de documentos pendientes
            var items = await _cola.ClaimPendientesAsync(batchSize, Environment.MachineName, ct);

            if (items == null || items.Count == 0)
            {
                _logger.LogDebug("No hay documentos pendientes para procesar.");
                return;
            }

            _logger.LogInformation("Iniciando procesamiento de {Cantidad} documentos pendientes.", items.Count);

            foreach (var item in items)
            {
                // Si el token se cancela, se detiene el procesamiento del lote.
                ct.ThrowIfCancellationRequested();
                await ProcesarDocumentoIndividualAsync(item, ct);
            }
        }

        private async Task ProcesarDocumentoIndividualAsync(DocumentoCola item, CancellationToken ct)
        {
            try
            {
                _logger.LogInformation(
                    "Procesando QueueId={QueueId} ObjType={ObjType} DocNum={DocNum} DocSubType={DocSubType} TipoCE={TipoCE}",
                    item.DocumentosPendientes_Id, item.ObjType, item.DocNum, item.DocSubType, item.TipoCE);

                // 2) Traer datos desde el SP (DataTable flat: 1 fila por línea con encabezado repetido)
                var (tipo, datos) = await ConsultarDocumentoAsync(item, ct);

                if (datos.Rows.Count == 0)
                    throw new InvalidOperationException($"El SP no devolvió filas para QueueId={item.DocumentosPendientes_Id} DocNum={item.DocNum}.");

                // 3) Traducir a XML GoSocket (DTE genérico)
                var xmlGosocket = _traductor.Traducir(tipo, datos);

                // 3.1) Guardar XML en disco (si OutputPath está configurado)
                var rutaXml = GuardarXmlEnDisco(item, tipo, datos.Rows[0], xmlGosocket);
                if (!string.IsNullOrWhiteSpace(rutaXml))
                    _logger.LogInformation("XML GoSocket guardado en: {Ruta}", rutaXml);

                // 4) Crear petición para enviar a la autoridad (GoSocket)
                var peticionEnvio = CrearPeticionEnvio(item, xmlGosocket);

                // 5) Enviar documento a la autoridad tributaria (GoSocket)
                var respuesta = await _clienteGosocket.EnviarDocumentoAutoridadAsync(peticionEnvio);

                // 6) Procesar respuesta
                await ProcesarRespuestaEnvioAsync(item.DocumentosPendientes_Id, respuesta, rutaXml,ct);

                _logger.LogInformation("DONE QueueId={QueueId} Tipo={Tipo}", item.DocumentosPendientes_Id, tipo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo QueueId={QueueId}", item.DocumentosPendientes_Id);

                // 7) Retry o Fail
                await _estados.MarcarRetryOFalloAsync(item.DocumentosPendientes_Id, ex.ToString(), ct);
            }
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
        /// Construye la petición hacia GoSocket para enviar el documento.
        /// </summary>
        private PeticionSendDocumentToAuthority CrearPeticionEnvio(DocumentoCola item, string xmlDocumento)
        {
            // Nota: estos códigos (33, 61, 56, 34) son típicos de Chile (SII).
            // Si su integración es CR, asegúrese de que GoSocket espere estos valores o cambie el mapeo.
            var peticion = new PeticionSendDocumentToAuthority
            {
                DocumentoXml = xmlDocumento,
                TipoDocumento = ObtenerCodigoTipoDocumento(item.TipoCE),

                // Ajuste según su implementación GoSocket (en CR esto puede ser distinto)
                CodigoPais = "CR",

                // Proceso asíncrono para mejor rendimiento
                Asincrono = true
            };

            // Asignación opcional si existen en item
            if (!string.IsNullOrWhiteSpace(item.Remitente))
                peticion.Remitente = item.Remitente;

            if (!string.IsNullOrWhiteSpace(item.Receptor))
                peticion.Receptor = item.Receptor;

            if (item.Folio.HasValue)
                peticion.Folio = item.Folio.Value;

            return peticion;
        }

        /// <summary>
        /// Interpreta la respuesta de GoSocket y marca el estado del documento en la cola.
        /// </summary>
        private async Task ProcesarRespuestaEnvioAsync(
     long queueId,
     RespuestaApi<RespuestaSendDocumentToAuthority> respuesta,
     string rutaXmlComprobante,
     CancellationToken ct)
        {
            // 1) Guardar respuesta GoSocket junto al XML del comprobante (si rutaXmlComprobante existe)
            GuardarRespuestaGosocketEnDisco(respuesta, rutaXmlComprobante);

            // 2) Lógica original
            if (respuesta == null)
                throw new InvalidOperationException("La respuesta de GoSocket llegó nula.");

            if (!respuesta.Exitoso)
            {
                throw new InvalidOperationException(
                    $"Error al enviar documento a GoSocket: {respuesta.MensajeError} (Código: {respuesta.CodigoError})");
            }

            if (respuesta.Datos == null)
                throw new InvalidOperationException("Respuesta exitosa pero sin datos (respuesta.Datos == null).");

            var resultadoEnvio = new
            {
                TrackId = respuesta.Datos.TrackId,
                Estado = respuesta.Datos.Estado,
                CodigoDocumento = respuesta.Datos.CodigoDocumento,
                EstadoAutoridad = respuesta.Datos.EstadoAutoridad,
                FechaRecepcion = respuesta.Datos.FechaRecepcion
            };

            await _estados.MarcarDoneAsync(queueId, resultadoEnvio, ct);
        }

        private void GuardarRespuestaGosocketEnDisco(
    RespuestaApi<RespuestaSendDocumentToAuthority> respuesta,
    string rutaXmlComprobante)
        {
            // Si por alguna razón no se guardó el XML, no hay donde “acompañar” la respuesta.
            if (string.IsNullOrWhiteSpace(rutaXmlComprobante))
                return;

            try
            {
                var carpeta = Path.GetDirectoryName(rutaXmlComprobante);
                if (string.IsNullOrWhiteSpace(carpeta))
                    return;

                var nombreBase = Path.GetFileNameWithoutExtension(rutaXmlComprobante);
                if (string.IsNullOrWhiteSpace(nombreBase))
                    return;

                Directory.CreateDirectory(carpeta);

                // Archivo de respuesta al lado del XML
                var pathRespuesta = Path.Combine(carpeta, $"{nombreBase}.gosocket.response.json");

                // Serializar JSON legible
                var json = JsonSerializer.Serialize(
                    respuesta,
                    new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                // UTF-8 sin BOM
                var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                File.WriteAllText(pathRespuesta, json, utf8NoBom);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo guardar la respuesta de GoSocket junto al XML. RutaXml={RutaXml}", rutaXmlComprobante);
            }
        }


        /// <summary>
        /// Mapea su tipo interno (FE/NC/ND/FEC) al código esperado por su API de GoSocket.
        /// Ajuste estos valores al estándar real que su endpoint use.
        /// </summary>
        private static string ObtenerCodigoTipoDocumento(string tipoCE)
        {
            return tipoCE switch
            {
                "FE" => "33",   // Factura electrónica (ejemplo Chile)
                "NC" => "61",   // Nota de crédito
                "ND" => "56",   // Nota de débito
                "FEC" => "34",  // Factura exenta
                _ => "33"
            };
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
    }
}
