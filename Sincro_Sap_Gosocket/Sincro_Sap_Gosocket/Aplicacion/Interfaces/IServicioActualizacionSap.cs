using Sincro_Sap_Gosocket.Dominio.Entidades;
using System.Threading;
using System.Threading.Tasks;

namespace Sincro_Sap_Gosocket.Aplicacion.Interfaces
{
    public interface IServicioActualizacionSap
    {
        Task ActualizarEstadoHaciendaAsync(
            ActualizacionEstadoHacienda actualizacion,
            CancellationToken cancellationToken = default);
    }
}