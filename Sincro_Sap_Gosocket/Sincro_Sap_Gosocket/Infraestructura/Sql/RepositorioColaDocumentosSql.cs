// Infraestructura/Sql/RepositorioColaDocumentosSql.cs
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Dominio.Entidades;

namespace Sincro_Sap_Gosocket.Infraestructura.Sql
{
    public class RepositorioColaDocumentosSql : IRepositorioColaDocumentos
    {
        private const string Tabla = "[Integration].[DocumentosPendientes]";

        private readonly ISqlConnectionFactory _cnFactory;
        private readonly ILogger<RepositorioColaDocumentosSql> _logger;

        public RepositorioColaDocumentosSql(
            ISqlConnectionFactory cnFactory,
            ILogger<RepositorioColaDocumentosSql> logger)
        {
            _cnFactory = cnFactory ?? throw new ArgumentNullException(nameof(cnFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Obtiene un lote de documentos pendientes listos para procesar.
        /// 
        /// La consulta:
        /// - Usa TOP(@BatchSize) para limitar el tamaño del lote en cada ciclo del Worker.
        /// - Filtra por Status = 'PENDING', LockedAt IS NULL y por ventana de reintento (NextAttemptAt).
        /// - Usa WITH (READPAST) para "saltar" filas bloqueadas por otro proceso, evitando esperas/locks.
        /// - Ordena por el intento más antiguo (LastAttemptAt) y, si nunca se intentó, por CreateDateTime,
        ///   garantizando un procesamiento FIFO razonable y favoreciendo reintentos atrasados.
        /// 
        /// Importante:
        /// - Los valores variables van parametrizados (ej. @BatchSize) para evitar SQL Injection y mejorar
        ///   el plan caching del SQL Server.
        /// - El nombre de la tabla NO puede parametrizarse; se inserta desde una constante controlada (Tabla).
        /// </summary>
        public async Task<IReadOnlyList<DocumentoCola>> ObtenerPendientesAsync(int batchSize, CancellationToken ct)
        {
            var sql = $@"
                        SELECT TOP (@BatchSize)
                            DocumentosPendientes_Id,
                            SourceSystem,
                            TipoCE,
                            ObjType,
                            DocEntry,
                            DocNum,
                            CardCode,
                            TaxDate,
                            Status,
                            AttemptCount,
                            NextAttemptAt,
                            LastAttemptAt,
                            LockedBy,
                            LockedAt,
                            LastError,
                            XmlFilePath,
                            GoSocket_TrackId,
                            GoSocket_HttpStatus,
                            GoSocket_ResponseJson,
                            Hacienda_Estado,
                            Hacienda_ResponseJson
                        FROM {Tabla} WITH (READPAST)
                        WHERE
                            Status = 'PENDING'
                            AND LockedAt IS NULL
                            AND (NextAttemptAt IS NULL OR NextAttemptAt <= SYSUTCDATETIME())
                        ORDER BY ISNULL(LastAttemptAt, CreateDateTime) ASC;";

            return await EjecutarSelectAsync(sql, batchSize, ct);
        }

        /// <summary>
        /// Obtiene un lote de documentos para seguimiento con Hacienda (por ejemplo, WAITING_HACIENDA),
        /// aplicando reglas de concurrencia y reintento:
        /// - Filtra por el <paramref name="status"/> indicado.
        /// - Solo toma registros no bloqueados (LockedAt IS NULL).
        /// - Requiere que ya exista TrackId de GoSocket (GoSocket_TrackId IS NOT NULL),
        ///   porque el seguimiento depende de ese identificador.
        /// - Respeta el “próximo intento” (NextAttemptAt): no trae filas antes de tiempo.
        /// - Ordena por el intento más antiguo para dar prioridad a lo que lleva más tiempo esperando.
        /// </summary>
        /// <param name="status">Estado a consultar (ej.: WAITING_HACIENDA). Es requerido.</param>
        /// <param name="batchSize">Cantidad máxima de registros a retornar en esta corrida.</param>
        /// <param name="ct">Token de cancelación para abortar la consulta si el servicio se detiene.</param>
        /// <returns>Lista de documentos listos para consultar estado en Hacienda.</returns>
        /// <remarks>
        /// El estado se envía como parámetro (@Status) para evitar inyección y mejorar reutilización del plan.
        /// El nombre de la tabla se inyecta por string porque SQL Server no permite parametrizar identificadores.
        /// WITH (READPAST) permite que varias instancias del servicio trabajen sin bloquearse entre sí.
        /// </remarks>
        
        public async Task<IReadOnlyList<DocumentoCola>> ObtenerPendientesSeguimientoAsync(string status, int batchSize, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(status))
                throw new ArgumentException("El status es requerido.", nameof(status));

            var sql = $@"
                        SELECT TOP (@BatchSize)
                            DocumentosPendientes_Id,
                            SourceSystem,
                            TipoCE,
                            ObjType,
                            DocEntry,
                            DocNum,
                            CardCode,
                            TaxDate,
                            Status,
                            AttemptCount,
                            NextAttemptAt,
                            LastAttemptAt,
                            LockedBy,
                            LockedAt,
                            LastError,
                            XmlFilePath,
                            GoSocket_TrackId,
                            GoSocket_HttpStatus,
                            GoSocket_ResponseJson,
                            Hacienda_Estado,
                            Hacienda_ResponseJson
                        FROM {Tabla} WITH (READPAST)
                        WHERE
                            Status = @Status
                            AND LockedAt IS NULL
                            AND GoSocket_TrackId IS NOT NULL
                            AND (NextAttemptAt IS NULL OR NextAttemptAt <= SYSUTCDATETIME())
                        ORDER BY ISNULL(LastAttemptAt, CreateDateTime) ASC;";

            return await EjecutarSelectAsync(sql, batchSize, ct, cmd =>
            {
                cmd.Parameters.Add("@Status", SqlDbType.VarChar, 50).Value = status;
            });
        }

        public async Task<bool> LockearAsync(int documentosPendientesId, string lockedBy, CancellationToken ct)
        {
            if (documentosPendientesId <= 0)
                throw new ArgumentOutOfRangeException(nameof(documentosPendientesId));

            if (string.IsNullOrWhiteSpace(lockedBy))
                throw new ArgumentException("lockedBy es requerido.", nameof(lockedBy));

            var sql = $@"
                    UPDATE {Tabla}
                    SET
                        LockedBy = @LockedBy,
                        LockedAt = SYSUTCDATETIME()
                    WHERE
                        DocumentosPendientes_Id = @Id
                        AND LockedAt IS NULL;";

            await using var cn = await AbrirConexionAsync(ct);
            await using var cmd = new SqlCommand(sql, cn);

            cmd.Parameters.Add("@Id", SqlDbType.Int).Value = documentosPendientesId;
            cmd.Parameters.Add("@LockedBy", SqlDbType.NVarChar, 200).Value = lockedBy;

            var filas = await cmd.ExecuteNonQueryAsync(ct);
            return filas > 0;
        }

        private async Task<IReadOnlyList<DocumentoCola>> EjecutarSelectAsync(
            string sql,
            int batchSize,
            CancellationToken ct,
            Action<SqlCommand>? parametrizar = null)
        {
            var lista = new List<DocumentoCola>();

            await using var cn = await AbrirConexionAsync(ct);
            await using var cmd = new SqlCommand(sql, cn);

            cmd.Parameters.Add("@BatchSize", SqlDbType.Int).Value = batchSize;

            parametrizar?.Invoke(cmd);

            await using var rd = await cmd.ExecuteReaderAsync(ct);
            while (await rd.ReadAsync(ct))
            {
                lista.Add(Mapear(rd));
            }

            return lista;
        }

        private async Task<SqlConnection> AbrirConexionAsync(CancellationToken ct)
        {
            var cn = _cnFactory.CreateConnection();
            await cn.OpenAsync(ct);
            return cn;
        }
        /// <summary>
        /// Mapear permite crear y cargar un objeto del tio DocumentoCola para utilizarlo en los procesos posteriores 
        /// que crearan el xml y lo enviaran
        /// </summary>
        /// <param name="rd"></param>
        /// <returns></returns>
        private static DocumentoCola Mapear(SqlDataReader rd)
        {
            return new DocumentoCola
            {
                DocumentosPendientes_Id = GetInt64(rd, "DocumentosPendientes_Id"),
                SourceSystem = GetString(rd, "SourceSystem"),
                TipoCE = GetString(rd, "TipoCE"),
                ObjType = GetString(rd, "ObjType"),
                DocEntry = GetInt32(rd, "DocEntry"),
                DocNum = GetInt32(rd, "DocNum"),
                CardCode = GetString(rd, "CardCode"),
                TaxDate = GetDateTime(rd, "TaxDate"),
                Status = GetString(rd, "Status"),
                AttemptCount = GetInt32(rd, "AttemptCount"),
                NextAttemptAt = GetNullableDateTime(rd, "NextAttemptAt"),
                LastAttemptAt = GetNullableDateTime(rd, "LastAttemptAt"),
                LockedBy = GetString(rd, "LockedBy"),
                LockedAt = GetNullableDateTime(rd, "LockedAt"),
                LastError = GetString(rd, "LastError"),
                XmlFilePath = GetString(rd, "XmlFilePath"),
                GoSocket_TrackId = GetString(rd, "GoSocket_TrackId"),
                GoSocket_HttpStatus = GetNullableInt32(rd, "GoSocket_HttpStatus"),
                GoSocket_ResponseJson = GetString(rd, "GoSocket_ResponseJson"),
                Hacienda_Estado = GetString(rd, "Hacienda_Estado"),
                Hacienda_ResponseJson = GetString(rd, "Hacienda_ResponseJson"),
            };
        }
        private static Int64 GetInt64(SqlDataReader rd, string col)
        {
            var ord = rd.GetOrdinal(col);
            return rd.IsDBNull(ord) ? 0 : rd.GetInt64(ord);
        }
        private static int GetInt32(SqlDataReader rd, string col)
        {
            var ord = rd.GetOrdinal(col);
            return rd.IsDBNull(ord) ? 0 : rd.GetInt32(ord);
        }

        private static int? GetNullableInt32(SqlDataReader rd, string col)
        {
            var ord = rd.GetOrdinal(col);
            return rd.IsDBNull(ord) ? (int?)null : rd.GetInt32(ord);
        }

        private static string GetString(SqlDataReader rd, string col)
        {
            var ord = rd.GetOrdinal(col);
            return rd.IsDBNull(ord) ? string.Empty : rd.GetString(ord);
        }

        private static DateTime GetDateTime(SqlDataReader rd, string col)
        {
            var ord = rd.GetOrdinal(col);
            return rd.IsDBNull(ord) ? DateTime.MinValue : rd.GetDateTime(ord);
        }

        private static DateTime? GetNullableDateTime(SqlDataReader rd, string col)
        {
            var ord = rd.GetOrdinal(col);
            return rd.IsDBNull(ord) ? (DateTime?)null : rd.GetDateTime(ord);
        }
    }
}
