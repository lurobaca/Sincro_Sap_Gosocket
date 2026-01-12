using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Comun;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Peticiones;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Respuestas;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Sincro_Sap_Gosocket.Aplicacion.Servicios
{
    public class ServicioProcesamientoDocumentos : IServicioProcesamientoDocumentos
    {
        private readonly ILogger<ServicioProcesamientoDocumentos> _logger;
        private readonly IRepositorioColaDocumentos _cola;
        private readonly IRepositorioEstados _estados;
        private readonly IEjecutorProcedimientos _sp;
        private readonly ITraductorXml _traductor;
        private readonly IClienteGosocket _clienteGosocket;

        public ServicioProcesamientoDocumentos(
            ILogger<ServicioProcesamientoDocumentos> logger,
            IRepositorioColaDocumentos cola,
            IRepositorioEstados estados,
            IEjecutorProcedimientos sp,
            ITraductorXml traductor,
            IClienteGosocket clienteGosocket)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cola = cola ?? throw new ArgumentNullException(nameof(cola));
            _estados = estados ?? throw new ArgumentNullException(nameof(estados));
            _sp = sp ?? throw new ArgumentNullException(nameof(sp));
            _traductor = traductor ?? throw new ArgumentNullException(nameof(traductor));
            _clienteGosocket = clienteGosocket ?? throw new ArgumentNullException(nameof(clienteGosocket));
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

                // 2) Traer datos + tipo (FE/NC/ND/FEC)
                var (tipo, datos) = await ConsultarDocumentoAsync(item, ct);

                // 3) Traducir a XML según el tipo de documento
                var xmlGosocket = _traductor.Traducir(tipo, datos);

                // 4) Crear petición para enviar a la autoridad
                var peticionEnvio = CrearPeticionEnvio(item, xmlGosocket);

                // 5) Enviar documento a la autoridad tributaria
                var respuesta = await _clienteGosocket.EnviarDocumentoAutoridadAsync(peticionEnvio);

                // 6) Procesar respuesta
                await ProcesarRespuestaEnvioAsync(item.DocumentosPendientes_Id, respuesta, ct);

                _logger.LogInformation("DONE QueueId={QueueId} Tipo={Tipo}", item.DocumentosPendientes_Id, tipo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo QueueId={QueueId}", item.DocumentosPendientes_Id);

                // 7) Retry o Fail
                await _estados.MarcarRetryOFalloAsync(item.DocumentosPendientes_Id, ex.ToString(), ct);
            }
        }

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
            var parametros = new[]
            {
                new SqlParameter("@DocNum", SqlDbType.VarChar, 50)
                {
                    Value = (object?)item.DocNum ?? DBNull.Value
                },
                new SqlParameter("@Situacion_de_Comprobante", SqlDbType.VarChar, 1)
                {
                    Value = 1
                },
                new SqlParameter("@Tipo", SqlDbType.VarChar, 1)
                {
                    Value = item.TipoCE  
                },
            };

            
            var datos = await _sp.EjecutarDataTableAsync(spName, parametros, ct);
            // Asumo que EjecutarAsync devuelve un DataTable
            return (item.TipoCE, datos);
        }

        private PeticionSendDocumentToAuthority CrearPeticionEnvio(DocumentoCola item, string xmlDocumento)
        {
            // Crear la petición con los datos mínimos requeridos
            var peticion = new PeticionSendDocumentToAuthority
            {
                DocumentoXml = xmlDocumento,
                TipoDocumento = ObtenerCodigoTipoDocumento(item.TipoCE),
                CodigoPais = "CL", // Ajustar según tu país
                Asincrono = true,   // Proceso asíncrono para mejor rendimiento
                // Añade aquí más propiedades si las tienes disponibles en item
            };

            // Opcional: Si tienes más datos en item, puedes asignarlos
            if (!string.IsNullOrEmpty(item.Remitente))
                peticion.Remitente = item.Remitente;

            if (!string.IsNullOrEmpty(item.Receptor))
                peticion.Receptor = item.Receptor;

            if (item.Folio.HasValue)
                peticion.Folio = item.Folio.Value;

            return peticion;
        }

        private async Task ProcesarRespuestaEnvioAsync(
            long queueId,
            RespuestaApi<RespuestaSendDocumentToAuthority> respuesta,
            CancellationToken ct)
        {
            if (respuesta.Exitoso)
            {
                // Crear un objeto con la información necesaria para MarcarDoneAsync
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
            else
            {
                // Si la API de GoSocket devolvió un error
                throw new InvalidOperationException(
                    $"Error al enviar documento a GoSocket: {respuesta.MensajeError} (Código: {respuesta.CodigoError})");
            }
        }

        private string ObtenerCodigoTipoDocumento(string tipoCE)
        {
            // Mapear tipos de documento a códigos de la API de GoSocket
            return tipoCE switch
            {
                "FE" => "33", // Factura electrónica
                "NC" => "61", // Nota de crédito
                "ND" => "56", // Nota de débito
                "FEC" => "34", // Factura exenta
                _ => "33" // Por defecto, factura electrónica
            };
        }
    }
}