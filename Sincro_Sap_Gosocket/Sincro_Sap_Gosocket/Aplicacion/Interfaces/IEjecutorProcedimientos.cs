using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Sincro_Sap_Gosocket.Aplicacion.Interfaces
{
    public interface IEjecutorProcedimientos
    {
        // Caso típico: SP que recibe DocEntry
        Task<DataSet> EjecutarDataSetAsync(string spName, int docEntry, CancellationToken ct);

        // SP genérico con parámetros
        Task<DataSet> EjecutarDataSetAsync(string spName, IEnumerable<SqlParameter> parametros, CancellationToken ct);

        // Si ocupas 1 sola tabla (más simple para traductor)
        Task<DataTable> EjecutarDataTableAsync(string spName, int docEntry, CancellationToken ct);

        // SP que no retorna dataset (UPDATE/INSERT), retorna filas afectadas
        Task<int> EjecutarNonQueryAsync(string spName, IEnumerable<SqlParameter> parametros, CancellationToken ct);
        /// <summary>
        /// Ejecuta un SP que recibe DocEntry y retorna los datos necesarios para traducir a GoSocket.
        /// Retorna un objeto (normalmente string XML/JSON, o un DTO) según tu estrategia.
        /// </summary>
        Task<object> EjecutarAsync(string spName, int docEntry, CancellationToken ct);
    }
}
