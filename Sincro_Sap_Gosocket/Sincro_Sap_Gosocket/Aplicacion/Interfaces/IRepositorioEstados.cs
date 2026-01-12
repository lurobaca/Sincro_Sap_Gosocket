using System.Threading;
using System.Threading.Tasks;

namespace Sincro_Sap_Gosocket.Aplicacion.Interfaces
{
    public interface IRepositorioEstados
    {
        Task MarcarDoneAsync(long queueId, object resultado, CancellationToken ct);
        Task MarcarRetryOFalloAsync(long queueId, string detalleError, CancellationToken ct);
    }
}