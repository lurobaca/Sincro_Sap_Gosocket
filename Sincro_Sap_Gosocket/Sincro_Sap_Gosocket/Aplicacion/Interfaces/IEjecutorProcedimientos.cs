using Microsoft.Data.SqlClient;
using System.Data;

public interface IEjecutorProcedimientos
{
    Task<DataSet> EjecutarDataSetAsync(string spName, int docEntry, CancellationToken ct);
    Task<DataSet> EjecutarDataSetAsync(string spName, IEnumerable<SqlParameter> parametros, CancellationToken ct);
    Task<DataTable> EjecutarDataTableAsync(string spName, int docEntry, CancellationToken ct);
    Task<DataTable> EjecutarDataTableAsync(string spName, IEnumerable<SqlParameter> parameters, CancellationToken ct);
    Task<int> EjecutarNonQueryAsync(string spName, IEnumerable<SqlParameter> parametros, CancellationToken ct);
    Task<string> EjecutarEscalarAsync(string spName, int docEntry, CancellationToken ct);
    Task<string> EjecutarEscalarAsync(string spName, IEnumerable<SqlParameter> parameters, CancellationToken ct);
    Task<object> EjecutarValorUnicoAsync(string spName, IEnumerable<SqlParameter> parameters, CancellationToken ct);
}