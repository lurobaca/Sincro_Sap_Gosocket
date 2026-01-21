// Sincro_Sap_Gosocket/Aplicacion/Interfaces/IServicioProcesamientoDocumentos.cs
using System.Threading;
using System.Threading.Tasks;

namespace Sincro_Sap_Gosocket.Aplicacion.Interfaces
{
    public interface IServicioProcesamientoDocumentos
    {
        /// <summary>
        /// Procesa documentos pendientes por ENVIAR a GoSocket (genera XML + envío).
        /// </summary>
        Task ProcesarPendientesAsync(int batchSize, CancellationToken ct);

        /// <summary>
        /// Procesa documentos YA ENVIADOS a GoSocket que están esperando respuesta de Hacienda.
        /// Consulta el estado en GoSocket hasta llegar a estado final (ACEPTADO/RECHAZADO/BAD_REQUEST).
        /// </summary>
        Task ProcesarSeguimientoHaciendaAsync(int batchSize, CancellationToken ct);
    }
}
