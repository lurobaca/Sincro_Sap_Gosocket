using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;

public sealed class RepositorioEstadosSql : IRepositorioEstados
{
    private readonly ISqlConnectionFactory _cnFactory;

    private const string SP_DONE = "TU_SP_DONE_AQUI";
    private const string SP_RETRY_FAIL = "TU_SP_RETRY_FAIL_AQUI";

    public RepositorioEstadosSql(ISqlConnectionFactory cnFactory)
    {
        _cnFactory = cnFactory;
    }

    public async Task MarcarDoneAsync(long queueId, GosocketSendResult resp, CancellationToken ct)
    {
        var respTxt = JsonSerializer.Serialize(resp);

        await using var cn = (SqlConnection)_cnFactory.CreateConnection();
        await cn.OpenAsync(ct);

        await using var cmd = new SqlCommand(SP_DONE, cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.Add(new SqlParameter("@QueueId", SqlDbType.BigInt) { Value = queueId });
        cmd.Parameters.Add(new SqlParameter("@RespuestaGoSocket", SqlDbType.NVarChar, -1)
        {
            Value = (object?)respTxt ?? DBNull.Value
        });

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task MarcarRetryOFalloAsync(long queueId, string detalleError, CancellationToken ct)
    {
        await using var cn = (SqlConnection)_cnFactory.CreateConnection();
        await cn.OpenAsync(ct);

        await using var cmd = new SqlCommand(SP_RETRY_FAIL, cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.Add(new SqlParameter("@QueueId", SqlDbType.BigInt) { Value = queueId });
        cmd.Parameters.Add(new SqlParameter("@DetalleError", SqlDbType.NVarChar, -1)
        {
            Value = (object?)detalleError ?? DBNull.Value
        });

        await cmd.ExecuteNonQueryAsync(ct);
    }
}
