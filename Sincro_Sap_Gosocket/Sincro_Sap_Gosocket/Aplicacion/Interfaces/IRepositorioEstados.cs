using System.Threading;
using System.Threading.Tasks;

namespace Sincro_Sap_Gosocket.Aplicacion.Interfaces
{
    public interface IRepositorioEstados
    {
        Task MarcarDoneAsync(long queueId, GosocketSendResult resp, CancellationToken ct);
        Task MarcarRetryOFalloAsync(long queueId, string detalleError, CancellationToken ct);
    }
}
