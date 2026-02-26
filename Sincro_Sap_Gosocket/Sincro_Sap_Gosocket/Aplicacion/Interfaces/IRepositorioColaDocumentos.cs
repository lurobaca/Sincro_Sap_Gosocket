// Aplicacion/Interfaces/IRepositorioColaDocumentos.cs
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sincro_Sap_Gosocket.Dominio.Entidades;

namespace Sincro_Sap_Gosocket.Aplicacion.Interfaces
{
    public interface IRepositorioColaDocumentos
    {
        Task<IReadOnlyList<DocumentoCola>> ObtenerPendientesAsync(int batchSize, CancellationToken ct);

        /// <summary>
        /// Documentos que ya fueron enviados y están en espera de estado de Hacienda vía GoSocket.
        /// </summary>
        Task<IReadOnlyList<DocumentoCola>> ObtenerPendientesSeguimientoAsync(string status, int batchSize, CancellationToken ct);

        Task<bool> LockearAsync(int documentosPendientesId, string lockedBy, CancellationToken ct);


        /// <summary>
        /// Incrementa en 1 el consecutivo del tipo indicado (FE, TE, NC, ND, FEC, MR_Aceptado, etc.)
        /// y retorna el consecutivo nuevo formateado a 20 dígitos.
        /// </summary>
        Task<string> ActualizarConsecutivoHaciendaAsync(string tipo , CancellationToken ct);


    }
}
