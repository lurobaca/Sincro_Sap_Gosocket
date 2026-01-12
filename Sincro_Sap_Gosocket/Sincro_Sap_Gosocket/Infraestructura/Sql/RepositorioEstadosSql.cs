using System;
using System.Data;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;

namespace Sincro_Sap_Gosocket.Infraestructura.Sql
{
    public sealed class RepositorioEstadosSql : IRepositorioEstados
    {
        private readonly ISqlConnectionFactory _cnFactory;

        // Nombres corregidos según tus stored procedures
        private const string SP_DONE = "Integration.MarkDone";
        private const string SP_RETRY_FAIL = "Integration.MarkFailed";

        public RepositorioEstadosSql(ISqlConnectionFactory cnFactory)
        {
            _cnFactory = cnFactory ?? throw new ArgumentNullException(nameof(cnFactory));
        }

        public async Task MarcarDoneAsync(long queueId, object resultado, CancellationToken ct)
        {
            if (resultado == null)
                throw new ArgumentNullException(nameof(resultado));

            // Serializar el resultado si es necesario
            string? respTxt = null;

            if (resultado is string strResult)
            {
                respTxt = strResult;
            }
            else
            {
                try
                {
                    respTxt = JsonSerializer.Serialize(resultado);
                }
                catch
                {
                    respTxt = resultado.ToString();
                }
            }

            await using var cn = (SqlConnection)_cnFactory.CreateConnection();
            await cn.OpenAsync(ct);

            await using var cmd = new SqlCommand(SP_DONE, cn)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Parámetro según tu stored procedure MarkDone
            cmd.Parameters.Add(new SqlParameter("@QueueId", SqlDbType.BigInt)
            {
                Value = queueId
            });

            // NOTA: El SP MarkDone que me mostraste solo recibe @QueueId
            // Si necesitas guardar la respuesta, deberías modificar el SP
            // o usar otro campo. Por ahora solo ejecutamos con el QueueId.
            // Si tu tabla tiene un campo para la respuesta, deberías añadir:
            // cmd.Parameters.Add(new SqlParameter("@ResponseData", SqlDbType.NVarChar, -1)
            // {
            //     Value = (object?)respTxt ?? DBNull.Value
            // });

            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task MarcarRetryOFalloAsync(long queueId, string detalleError, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(detalleError))
                throw new ArgumentException("Detalle del error es requerido.", nameof(detalleError));

            await using var cn = (SqlConnection)_cnFactory.CreateConnection();
            await cn.OpenAsync(ct);

            await using var cmd = new SqlCommand(SP_RETRY_FAIL, cn)
            {
                CommandType = CommandType.StoredProcedure
            };

            // Parámetros según tu stored procedure MarkFailed
            cmd.Parameters.Add(new SqlParameter("@QueueId", SqlDbType.BigInt)
            {
                Value = queueId
            });

            cmd.Parameters.Add(new SqlParameter("@Error", SqlDbType.NVarChar, 2000)
            {
                Value = detalleError.Length > 2000 ? detalleError.Substring(0, 2000) : detalleError
            });

            // Valores por defecto según tu SP
            cmd.Parameters.Add(new SqlParameter("@RetryInSeconds", SqlDbType.Int)
            {
                Value = 60 // Valor por defecto
            });

            cmd.Parameters.Add(new SqlParameter("@MaxAttempts", SqlDbType.Int)
            {
                Value = 10 // Valor por defecto
            });

            await cmd.ExecuteNonQueryAsync(ct);
        }

        // Método opcional para obtener documentos pendientes
        public async Task<DataTable> ObtenerClaimPendientesAsync(string workerId, int batchSize, CancellationToken ct)
        {
            const string SP_CLAIM_PENDIENTES = "Integration.ClaimPendientes";

            await using var cn = (SqlConnection)_cnFactory.CreateConnection();
            await cn.OpenAsync(ct);

            await using var cmd = new SqlCommand(SP_CLAIM_PENDIENTES, cn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@WorkerId", SqlDbType.VarChar, 100)
            {
                Value = workerId
            });

            cmd.Parameters.Add(new SqlParameter("@BatchSize", SqlDbType.Int)
            {
                Value = batchSize
            });

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            var dataTable = new DataTable();
            dataTable.Load(reader);
            return dataTable;
        }

        // Método opcional para manejo más flexible de reintentos
        public async Task MarcarParaReintentoAsync(long queueId, string error, int retryInSeconds, int maxAttempts, CancellationToken ct)
        {
            await using var cn = (SqlConnection)_cnFactory.CreateConnection();
            await cn.OpenAsync(ct);

            await using var cmd = new SqlCommand(SP_RETRY_FAIL, cn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@QueueId", SqlDbType.BigInt)
            {
                Value = queueId
            });

            cmd.Parameters.Add(new SqlParameter("@Error", SqlDbType.NVarChar, 2000)
            {
                Value = error.Length > 2000 ? error.Substring(0, 2000) : error
            });

            cmd.Parameters.Add(new SqlParameter("@RetryInSeconds", SqlDbType.Int)
            {
                Value = retryInSeconds
            });

            cmd.Parameters.Add(new SqlParameter("@MaxAttempts", SqlDbType.Int)
            {
                Value = maxAttempts
            });

            await cmd.ExecuteNonQueryAsync(ct);
        }
    }
}