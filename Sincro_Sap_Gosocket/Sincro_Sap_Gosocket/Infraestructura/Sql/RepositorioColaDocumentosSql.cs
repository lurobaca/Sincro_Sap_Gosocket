using Microsoft.Data.SqlClient;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace Sincro_Sap_Gosocket.Infraestructura.Sql
{
    public sealed class RepositorioColaDocumentosSql : IRepositorioColaDocumentos
    {
        private const string SP_DEQUEUE = "Integration.DequeueDocuments";
        private readonly ISqlConnectionFactory _cnFactory;

        public RepositorioColaDocumentosSql(ISqlConnectionFactory cnFactory)
        {
            _cnFactory = cnFactory;
        }

        public async Task<IReadOnlyList<DocumentoCola>> ClaimPendientesAsync(int batchSize, string workerId, CancellationToken ct)
        {
            var list = new List<DocumentoCola>();

            await using var cn = (SqlConnection)_cnFactory.CreateConnection();

            await cn.OpenAsync(ct);

            await using var cmd = cn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = SP_DEQUEUE;

            cmd.Parameters.Add(new SqlParameter("@WorkerId", SqlDbType.VarChar, 100) { Value = workerId });
            cmd.Parameters.Add(new SqlParameter("@BatchSize", SqlDbType.Int) { Value = batchSize });

            await using var rd = await cmd.ExecuteReaderAsync(ct);
            while (await rd.ReadAsync(ct))
            {
                var item = new DocumentoCola
                {
                    QueueId = rd.GetInt64(rd.GetOrdinal("QueueId")),
                    ObjType = rd.GetString(rd.GetOrdinal("ObjType")),
                    DocEntry = rd.GetInt32(rd.GetOrdinal("DocEntry")),
                    DocNum = rd.IsDBNull(rd.GetOrdinal("DocNum")) ? null : rd.GetInt32(rd.GetOrdinal("DocNum")),
                    DocSubType = rd.IsDBNull(rd.GetOrdinal("DocSubType")) ? null : rd.GetString(rd.GetOrdinal("DocSubType")),
                    DocType = rd.IsDBNull(rd.GetOrdinal("DocType")) ? null : rd.GetString(rd.GetOrdinal("DocType")),
                    CardCode = rd.IsDBNull(rd.GetOrdinal("CardCode")) ? null : rd.GetString(rd.GetOrdinal("CardCode")),
                    TaxDate = rd.IsDBNull(rd.GetOrdinal("TaxDate")) ? null : rd.GetDateTime(rd.GetOrdinal("TaxDate")),
                    Status = rd.GetString(rd.GetOrdinal("Status")),
                    AttemptCount = rd.GetInt32(rd.GetOrdinal("AttemptCount")),
                    NextAttemptAt = rd.IsDBNull(rd.GetOrdinal("NextAttemptAt")) ? null : rd.GetDateTime(rd.GetOrdinal("NextAttemptAt")),
                    LastAttemptAt = rd.IsDBNull(rd.GetOrdinal("LastAttemptAt")) ? null : rd.GetDateTime(rd.GetOrdinal("LastAttemptAt")),
                    LockedBy = rd.IsDBNull(rd.GetOrdinal("LockedBy")) ? null : rd.GetString(rd.GetOrdinal("LockedBy")),
                    LockedAt = rd.IsDBNull(rd.GetOrdinal("LockedAt")) ? null : rd.GetDateTime(rd.GetOrdinal("LockedAt")),
                    LastError = rd.IsDBNull(rd.GetOrdinal("LastError")) ? null : rd.GetString(rd.GetOrdinal("LastError"))
                };

                list.Add(item);
            }

            return list;
        }
    }
}
