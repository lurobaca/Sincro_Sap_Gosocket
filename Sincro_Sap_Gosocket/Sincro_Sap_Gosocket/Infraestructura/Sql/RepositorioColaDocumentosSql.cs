using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;

namespace Sincro_Sap_Gosocket.Infraestructura.Sql
{
    public sealed class RepositorioColaDocumentosSql : IRepositorioColaDocumentos
    {
        private readonly ISqlConnectionFactory _cnFactory;
        private const string SP_CLAIM = "Integration.ClaimPendientes";

        public RepositorioColaDocumentosSql(ISqlConnectionFactory cnFactory)
        {
            _cnFactory = cnFactory ?? throw new ArgumentNullException(nameof(cnFactory));
        }

        public async Task<IReadOnlyList<DocumentoCola>> ClaimPendientesAsync(int batchSize, string workerId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(workerId))
                throw new ArgumentException("WorkerId es requerido.", nameof(workerId));

            if (batchSize <= 0)
                throw new ArgumentException("BatchSize debe ser mayor a 0.", nameof(batchSize));

            var lista = new List<DocumentoCola>();

            await using var cn = (SqlConnection)_cnFactory.CreateConnection();
            await cn.OpenAsync(ct);

            await using var cmd = new SqlCommand(SP_CLAIM, cn)
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

            await using var rd = await cmd.ExecuteReaderAsync(ct);

            while (await rd.ReadAsync(ct))
            {
                var item = new DocumentoCola
                {
                    DocumentosPendientes_Id = rd.GetInt64(rd.GetOrdinal("DocumentosPendientes_Id")),
                    ObjType = rd.GetString(rd.GetOrdinal("ObjType")),
                    DocEntry = rd.GetInt32(rd.GetOrdinal("DocEntry")),
                    DocNum = rd.IsDBNull(rd.GetOrdinal("DocNum")) ? (int?)null : rd.GetInt32(rd.GetOrdinal("DocNum")),
                    DocSubType = rd.IsDBNull(rd.GetOrdinal("DocSubType")) ? null : rd.GetString(rd.GetOrdinal("DocSubType")),
                    DocType = rd.IsDBNull(rd.GetOrdinal("DocType")) ? null : rd.GetString(rd.GetOrdinal("DocType")),
                    CandCode = rd.IsDBNull(rd.GetOrdinal("CandCode")) ? null : rd.GetString(rd.GetOrdinal("CandCode")),
                    IsoRule = rd.IsDBNull(rd.GetOrdinal("IsoRule")) ? null : rd.GetString(rd.GetOrdinal("IsoRule")),
                    Status = rd.GetString(rd.GetOrdinal("Status")),
                    AttemptCount = rd.GetInt32(rd.GetOrdinal("AttemptCount")),
                    NextAttemptAt = rd.IsDBNull(rd.GetOrdinal("NextAttemptAt")) ? (DateTime?)null : rd.GetDateTime(rd.GetOrdinal("NextAttemptAt")),
                    LastAttemptAt = rd.IsDBNull(rd.GetOrdinal("LastAttemptAt")) ? (DateTime?)null : rd.GetDateTime(rd.GetOrdinal("LastAttemptAt")),
                    LockedAt = rd.IsDBNull(rd.GetOrdinal("LockedAt")) ? (DateTime?)null : rd.GetDateTime(rd.GetOrdinal("LockedAt")),
                    LockedBy = rd.IsDBNull(rd.GetOrdinal("LockedBy")) ? null : rd.GetString(rd.GetOrdinal("LockedBy")),
                    LastError = rd.IsDBNull(rd.GetOrdinal("LastError")) ? null : rd.GetString(rd.GetOrdinal("LastError")),
                    FechaCreacion = rd.IsDBNull(rd.GetOrdinal("FechaCreacion")) ? DateTime.MinValue : rd.GetDateTime(rd.GetOrdinal("FechaCreacion"))
                };

                lista.Add(item);
            }

            return lista.AsReadOnly();
        }
    }
}