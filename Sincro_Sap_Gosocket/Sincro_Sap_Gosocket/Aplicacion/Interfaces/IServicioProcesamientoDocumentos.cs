using System.Threading;
using System.Threading.Tasks;

namespace Sincro_Sap_Gosocket.Aplicacion.Interfaces
{
    public interface IServicioProcesamientoDocumentos
    {
        Task ProcesarPendientesAsync(int batchSize, CancellationToken ct);
    }
}
