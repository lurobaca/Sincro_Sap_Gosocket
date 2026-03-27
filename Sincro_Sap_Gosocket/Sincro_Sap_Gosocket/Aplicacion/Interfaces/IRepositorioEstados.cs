using Sincro_Sap_Gosocket.Dominio.Entidades;
using System.Threading;
using System.Threading.Tasks;

namespace Sincro_Sap_Gosocket.Aplicacion.Interfaces
{
    public interface IRepositorioEstados
    {
        Task MarcarDoneAsync(long documentosPendientesId, CancellationToken ct, String Estado, String Resp);

        Task MarcarRetryOFalloAsync(long documentosPendientesId, string ultimoError, int maxIntentos, CancellationToken ct);

        Task MarcarWaitingHaciendaAsync(long documentosPendientesId, string? gosocketTrackId, int? gosocketHttpStatus, string gosocketResponseJson, CancellationToken ct);

        Task ActualizaEstadoHaciendaEnDocumentosPendientesAsync(long documentosPendientesId, string? haciendaEstado, string? haciendaResponseJson, bool esFinal, int maxIntentos, CancellationToken ct);

        Task ActualizaEstadoHaciendaEnSapAsync(ActualizacionEstadoHacienda actualizacion,  CancellationToken cancellationToken = default);
    }
}
