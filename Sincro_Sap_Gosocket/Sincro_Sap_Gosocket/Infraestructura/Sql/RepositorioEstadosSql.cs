// Infraestructura/Sql/RepositorioEstadosSql.cs
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Dominio.Entidades;
using Sincro_Sap_Gosocket.Infraestructura.Logs;
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

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

        public async Task MarcarDoneAsync(long documentosPendientesId, CancellationToken ct, string estado, string resp)
        {
            var sql = $@"
                UPDATE {Tabla}
                SET
                    Status = 'DONE',
                    LockedBy = NULL,
                    LockedAt = NULL,
                    NextAttemptAt = NULL,
                    Hacienda_Estado = @Estado,
                    Hacienda_ResponseJson = @Resp
                WHERE DocumentosPendientes_Id = @Id;";

            using var cn = await _cnFactory.CreateOpenConnectionAsync(ct);
            using var cmd = new SqlCommand(sql, cn);

            cmd.Parameters.Add("@Id", SqlDbType.BigInt).Value = documentosPendientesId;
            cmd.Parameters.Add("@Estado", SqlDbType.NVarChar, 50).Value = (object?)estado ?? DBNull.Value;
            cmd.Parameters.Add("@Resp", SqlDbType.NVarChar).Value = (object?)resp ?? DBNull.Value;

            await cmd.ExecuteNonQueryAsync(ct);
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

        public async Task MarcarWaitingHaciendaAsync(long documentosPendientesId, string? goSocketGlobalDocumentId, int? httpStatus, string? responseJson, CancellationToken ct)
        {
            var sql = $@"
                        UPDATE {Tabla}
                        SET
                            Status = 'WAITING_HACIENDA',
                            GoSocket_TrackId = @GlobalDocumentId,
                            GoSocket_HttpStatus = @HttpStatus,
                            GoSocket_ResponseJson = @Resp,
                            LastAttemptAt = SYSUTCDATETIME(),
                            LockedBy = NULL,
                            LockedAt = NULL
                        WHERE DocumentosPendientes_Id = @Id;";

            using var cn = await _cnFactory.CreateOpenConnectionAsync(ct);
            using var cmd = new SqlCommand(sql, cn);

            cmd.Parameters.AddWithValue("@Id", documentosPendientesId);
            cmd.Parameters.AddWithValue("@GlobalDocumentId", (object?)goSocketGlobalDocumentId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@HttpStatus", (object?)httpStatus ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Resp", (object?)responseJson ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task ActualizaEstadoHaciendaEnDocumentosPendientesAsync(long documentosPendientesId, string? haciendaEstado, string? haciendaResponseJson, bool done, int attemptCount, CancellationToken ct)
        {

            try {
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
            catch (Exception ex)
            {
                TrazaArchivo.Escribir($"Error al actualizar estado Hacienda en ActualizaEstadoHaciendaEnDocumentosPendientesAsync Error={ex.Message}");

                _logger.LogError(ex, "Error al actualizar estado Hacienda en DocumentosPendientes. Id={Id}", documentosPendientesId);
                throw;
            }
        }

        public async Task ActualizaEstadoHaciendaEnSapAsync(ActualizacionEstadoHacienda actualizacion,  CancellationToken ct = default)
        {

            var DocEntry = actualizacion.DocEntry;
            try
            { 
           
                var EstadoHacienda = actualizacion.EstadoHacienda;
                var RespuestaHacienda = actualizacion.MensajeHacienda ;
                var Clave = actualizacion.Clave  ;
                var FechaRespuesta = actualizacion.FechaRespuestaTexto ;

                string TablaSap = "";

                if (string.IsNullOrWhiteSpace(actualizacion.TipoDocumento))
                {
                    throw new ArgumentException("TipoDocumento es requerido.", nameof(actualizacion));
                }

                var tipo = actualizacion.TipoDocumento.Trim().ToUpperInvariant();

                if (tipo == "TE"  ||
                    tipo == "FE"  ||
                    tipo == "ND"  ||
                    tipo == "TES" ||
                    tipo == "FES" ||
                    tipo == "NDS" ||
                    tipo == "FEE")
                {
                    TablaSap = "[SBO_LARCE].[dbo].[OINV]";
                }
                else if (tipo == "NC")
                {
                    TablaSap = "[SBO_LARCE].[dbo].[ORIN]";
                }
                else
                {
                    throw new NotSupportedException($"TipoDocumento no soportado: {actualizacion.TipoDocumento}");
                }

                var sql = $@"
                        UPDATE {TablaSap}
                        SET
                            U_EstadoHacienda = @EstadoHacienda,
                            U_RespuestaHacienda = @RespuestaHacienda,
                            U_ClaveHacienda = @ClaveHacienda,
                            U_FechaRespuesta = @FechaRespuesta                       
                        WHERE DocEntry = @DocEntry;";

                using var cn = await _cnFactory.CreateOpenConnectionAsync(ct);
                using var cmd = new SqlCommand(sql, cn);

                cmd.Parameters.AddWithValue("@EstadoHacienda", (object?)EstadoHacienda ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@RespuestaHacienda", (object?)RespuestaHacienda ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ClaveHacienda", (object?)Clave ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FechaRespuesta", (object?)FechaRespuesta ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@DocEntry", DocEntry);


                var filas = await cmd.ExecuteNonQueryAsync(ct);

                if (filas <= 0)
                {
                    throw new InvalidOperationException(
                        $"No se actualizó ningún registro SAP. Tabla={TablaSap}, DocEntry={DocEntry}");
                }
            }
            catch (Exception ex)
            {
                TrazaArchivo.Escribir($"Error al actualizar estado Hacienda en ActualizaEstadoHaciendaEnSapAsync  DocEntry={DocEntry} Error={ex.Message}");

                _logger.LogError(ex,
                 "Error al actualizar estado Hacienda en ActualizaEstadoHaciendaEnSapAsync. DocEntry={DocEntry}",
                 DocEntry); 
                throw;
            }
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
