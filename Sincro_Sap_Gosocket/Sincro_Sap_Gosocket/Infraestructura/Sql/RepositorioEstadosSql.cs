// Infraestructura/Sql/RepositorioEstadosSql.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;

namespace Sincro_Sap_Gosocket.Infraestructura.Sql
{
    public class RepositorioEstadosSql : IRepositorioEstados
    {
        private const string Tabla = "[SincroSapGoSocket].[Integration].[DocumentosPendientes]";

        private readonly ISqlConnectionFactory _cnFactory;
        private readonly ILogger<RepositorioEstadosSql> _logger;

        public RepositorioEstadosSql(ISqlConnectionFactory cnFactory, ILogger<RepositorioEstadosSql> logger)
        {
            _cnFactory = cnFactory ?? throw new ArgumentNullException(nameof(cnFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task MarcarDoneAsync(long documentosPendientesId, CancellationToken ct)
        {
            var sql = $@"
UPDATE {Tabla}
SET
    Status = 'DONE',
    LockedBy = NULL,
    LockedAt = NULL,
    NextAttemptAt = NULL
WHERE DocumentosPendientes_Id = @Id;";

            await EjecutarAsync(sql, documentosPendientesId, ct);
        }

        public async Task MarcarRetryOFalloAsync(long documentosPendientesId, string lastError, int attemptCount, CancellationToken ct)
        {
            // Regla típica: si ya reintentó mucho => FAIL, si no => RETRY
            var nuevoStatus = attemptCount >= 5 ? "FAIL" : "RETRY";

            var sql = $@"
UPDATE {Tabla}
SET
    Status = @Status,
    LastError = @LastError,
    AttemptCount = @AttemptCount,
    LastAttemptAt = SYSUTCDATETIME(),
    NextAttemptAt = DATEADD(MINUTE, 5, SYSUTCDATETIME()),
    LockedBy = NULL,
    LockedAt = NULL
WHERE DocumentosPendientes_Id = @Id;";

            using var cn = await _cnFactory.CreateOpenConnectionAsync(ct);
            using var cmd = new SqlCommand(sql, cn);

            cmd.Parameters.AddWithValue("@Id", documentosPendientesId);
            cmd.Parameters.AddWithValue("@Status", nuevoStatus);
            cmd.Parameters.AddWithValue("@LastError", (object?)lastError ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@AttemptCount", attemptCount);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task MarcarWaitingHaciendaAsync(long documentosPendientesId, string? goSocketTrackId, int? httpStatus, string? responseJson, CancellationToken ct)
        {
            var sql = $@"
UPDATE {Tabla}
SET
    Status = 'WAITING_HACIENDA',
    GoSocket_TrackId = @TrackId,
    GoSocket_HttpStatus = @HttpStatus,
    GoSocket_ResponseJson = @Resp,
    LastAttemptAt = SYSUTCDATETIME(),
    LockedBy = NULL,
    LockedAt = NULL
WHERE DocumentosPendientes_Id = @Id;";

            using var cn = await _cnFactory.CreateOpenConnectionAsync(ct);
            using var cmd = new SqlCommand(sql, cn);

            cmd.Parameters.AddWithValue("@Id", documentosPendientesId);
            cmd.Parameters.AddWithValue("@TrackId", (object?)goSocketTrackId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@HttpStatus", (object?)httpStatus ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Resp", (object?)responseJson ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task ActualizarSeguimientoHaciendaAsync(long documentosPendientesId, string? haciendaEstado, string? haciendaResponseJson, bool done, int attemptCount, CancellationToken ct)
        {
            var statusFinal = done ? "DONE" : "WAITING_HACIENDA";

            var sql = $@"
UPDATE {Tabla}
SET
    Hacienda_Estado = @Estado,
    Hacienda_ResponseJson = @Resp,
    Status = @Status,
    AttemptCount = @AttemptCount,
    LastAttemptAt = SYSUTCDATETIME(),
    NextAttemptAt = CASE WHEN @Status = 'WAITING_HACIENDA' THEN DATEADD(MINUTE, 5, SYSUTCDATETIME()) ELSE NULL END,
    LockedBy = NULL,
    LockedAt = NULL
WHERE DocumentosPendientes_Id = @Id;";

            using var cn = await _cnFactory.CreateOpenConnectionAsync(ct);
            using var cmd = new SqlCommand(sql, cn);

            cmd.Parameters.AddWithValue("@Id", documentosPendientesId);
            cmd.Parameters.AddWithValue("@Estado", (object?)haciendaEstado ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Resp", (object?)haciendaResponseJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", statusFinal);
            cmd.Parameters.AddWithValue("@AttemptCount", attemptCount);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        private async Task EjecutarAsync(string sql, long id, CancellationToken ct)
        {
            using var cn = await _cnFactory.CreateOpenConnectionAsync(ct);
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}
