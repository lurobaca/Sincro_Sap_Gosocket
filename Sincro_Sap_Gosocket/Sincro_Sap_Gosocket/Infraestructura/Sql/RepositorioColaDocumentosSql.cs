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
                    //SourceSystem = rd.IsDBNull(rd.GetOrdinal("SourceSystem")) ? null : rd.GetString(rd.GetOrdinal("SourceSystem")),
                    TipoCE = rd.IsDBNull(rd.GetOrdinal("TipoCE")) ? null : rd.GetString(rd.GetOrdinal("TipoCE")),
                    ObjType = rd.IsDBNull(rd.GetOrdinal("ObjType")) ? null : rd.GetString(rd.GetOrdinal("ObjType")),
                    DocEntry = rd.GetInt32(rd.GetOrdinal("DocEntry")),
                    DocNum = rd.IsDBNull(rd.GetOrdinal("DocNum")) ? (int?)null : rd.GetInt32(rd.GetOrdinal("DocNum")),
                    DocSubType = rd.IsDBNull(rd.GetOrdinal("DocSubType")) ? null : rd.GetString(rd.GetOrdinal("DocSubType")),
                    DocType = rd.IsDBNull(rd.GetOrdinal("DocType")) ? null : rd.GetString(rd.GetOrdinal("DocType")),
                    CardCode = rd.IsDBNull(rd.GetOrdinal("CardCode")) ? null : rd.GetString(rd.GetOrdinal("CardCode")),
                    //TaxDate = rd.IsDBNull(rd.GetOrdinal("TaxDate")) ? null : rd.GetString(rd.GetOrdinal("TaxDate")),
                    //CreateDateTime = rd.IsDBNull(rd.GetOrdinal("CreateDateTime")) ? (DateTime?)null : rd.GetDateTime(rd.GetOrdinal("CreateDateTime")),
                    //Status = rd.GetString(rd.GetOrdinal("Status")),
                    //AttemptCount = rd.GetInt32(rd.GetOrdinal("AttemptCount")),
                    //NextAttemptAt = rd.IsDBNull(rd.GetOrdinal("NextAttemptAt")) ? (DateTime?)null : rd.GetDateTime(rd.GetOrdinal("NextAttemptAt")),
                    //LastAttemptAt = rd.IsDBNull(rd.GetOrdinal("LastAttemptAt")) ? (DateTime?)null : rd.GetDateTime(rd.GetOrdinal("LastAttemptAt")),
                    //LockedAt = rd.IsDBNull(rd.GetOrdinal("LockedAt")) ? (DateTime?)null : rd.GetDateTime(rd.GetOrdinal("LockedAt")),
                    //LockedBy = rd.IsDBNull(rd.GetOrdinal("LockedBy")) ? null : rd.GetString(rd.GetOrdinal("LockedBy")),
                    //LastError = rd.IsDBNull(rd.GetOrdinal("LastError")) ? null : rd.GetString(rd.GetOrdinal("LastError")),
                    //FechaCreacion = rd.IsDBNull(rd.GetOrdinal("FechaCreacion")) ? DateTime.MinValue : rd.GetDateTime(rd.GetOrdinal("FechaCreacion"))
                };

                // NOTA: Las siguientes propiedades no están incluidas en el stored procedure OUTPUT
                // Si necesitas estas propiedades, debes agregarlas al OUTPUT del SP
                // item.SituacionDeComprobante = rd.IsDBNull(rd.GetOrdinal("SituacionDeComprobante")) ? null : rd.GetString(rd.GetOrdinal("SituacionDeComprobante"));
                // item.Remitente = rd.IsDBNull(rd.GetOrdinal("Remitente")) ? null : rd.GetString(rd.GetOrdinal("Remitente"));
                // item.Receptor = rd.IsDBNull(rd.GetOrdinal("Receptor")) ? null : rd.GetString(rd.GetOrdinal("Receptor"));
                // item.Folio = rd.IsDBNull(rd.GetOrdinal("Folio")) ? (int?)null : rd.GetInt32(rd.GetOrdinal("Folio"));

                lista.Add(item);
            }

            return lista.AsReadOnly();
        }
    }
}