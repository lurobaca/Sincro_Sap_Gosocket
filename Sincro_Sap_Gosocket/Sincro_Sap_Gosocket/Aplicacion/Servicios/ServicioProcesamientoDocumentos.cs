using Microsoft.Extensions.Logging;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;

namespace Sincro_Sap_Gosocket.Aplicacion.Servicios
{
    public class ServicioProcesamientoDocumentos : IServicioProcesamientoDocumentos
    {
        private readonly ILogger<ServicioProcesamientoDocumentos> _logger;
        private readonly IRepositorioColaDocumentos _cola;
        private readonly IRepositorioEstados _estados;
        private readonly IEjecutorProcedimientos _sp;
        private readonly ITraductorXml _traductor;
        private readonly IClienteGosocket _gosocket;

        public ServicioProcesamientoDocumentos(
            ILogger<ServicioProcesamientoDocumentos> logger,
            IRepositorioColaDocumentos cola,
            IRepositorioEstados estados,
            IEjecutorProcedimientos sp,
            ITraductorXml traductor,
            IClienteGosocket gosocket)
        {
            _logger = logger;
            _cola = cola;
            _estados = estados;
            _sp = sp;
            _traductor = traductor;
            _gosocket = gosocket;
        }

        public async Task ProcesarPendientesAsync(int batchSize, CancellationToken ct)
        {
            // 1) Claim lote (evita duplicados si tu SP claim está bien hecho)
            var items = await _cola.ClaimPendientesAsync(batchSize, Environment.MachineName, ct);

            if (items == null || items.Count == 0)
                return;

            foreach (var item in items)
            {
                try
                {
                    _logger.LogInformation("Procesando QueueId={QueueId} ObjType={ObjType} DocEntry={DocEntry} DocSubType={DocSubType}",
                        item.QueueId, item.ObjType, item.DocEntry, item.DocSubType);

                    // 2) Traer datos + tipo (FE/NC/ND/FEC)
                    var (tipo, datos) = await ConsultarDocumentoAsync(item, ct);

                    // 3) Traducir (ideal: el traductor sabe el tipo)
                    // AJUSTA AQUÍ si tu ITraductorXml actualmente solo recibe 1 parámetro.
                    var xmlGosocket = _traductor.Traducir(tipo, datos);

                    // 4) Enviar
                    var resp = await _gosocket.EnviarAsync(xmlGosocket, ct); 

                    // 5) DONE
                    await _estados.MarcarDoneAsync(item.QueueId, resp, ct);

                    _logger.LogInformation("DONE QueueId={QueueId} Tipo={Tipo}", item.QueueId, tipo);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fallo QueueId={QueueId}", item.QueueId);

                    // 6) Retry o Fail
                    await _estados.MarcarRetryOFalloAsync(item.QueueId, ex.ToString(), ct);
                }
            }
        }

      
        // IMPORTANTE: Tipar el item (no dynamic)
        // Ajusta el tipo "DocumentoCola" al tipo real que te devuelva tu repositorio.
        private async Task<(string Tipo, object Datos)> ConsultarDocumentoAsync(DocumentoCola item, CancellationToken ct)
        {
            // 1) Determinar tipo
            // Si ya tienes una columna DocKind/Tipo en la cola, úsala aquí y elimina este switch.
            var tipo = item.ObjType switch
            {
                "13" => string.Equals(item.DocSubType, "DN", StringComparison.OrdinalIgnoreCase) ? "ND" : "FE",
                "14" => "NC",
                "18" => "FEC",
                _ => throw new InvalidOperationException($"ObjType no soportado: {item.ObjType}")
            };

            // 2) Elegir SP por tipo
            var spName = tipo switch
            {
                "FE" => "SP_Consulta_FE_FES_V44",
                "NC" => "SP_Consulta_NC_NCS_V44",
                "ND" => "SP_Consulta_ND_NDS_V44",
                "FEC" => "SP_Consulta_FEC_V44",
                _ => throw new InvalidOperationException($"Tipo no soportado: {tipo}")
            };

            // 3) Ejecutar SP
            // AJUSTA ESTE LLAMADO al método real de tu IEjecutorProcedimientos.
            // Ejemplo: EjecutarAsync(spName, new { DocEntry = item.DocEntry }, ct) o similar.
            var datos = await _sp.EjecutarAsync(spName, item.DocEntry, ct);

            return (tipo, datos);
        }
    }

   
}
