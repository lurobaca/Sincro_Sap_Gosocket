using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;

namespace Sincro_Sap_Gosocket.Infraestructura.Sql
{
    public sealed class EjecutorProcedimientosSql : IEjecutorProcedimientos
    {
        private readonly ISqlConnectionFactory _cnFactory;

        public EjecutorProcedimientosSql(ISqlConnectionFactory cnFactory)
        {
            _cnFactory = cnFactory ?? throw new ArgumentNullException(nameof(cnFactory));
        }

        public Task<DataSet> EjecutarDataSetAsync(string spName, int docEntry, CancellationToken ct)
        {
            var p = new[]
            {
                new SqlParameter("@DocEntry", SqlDbType.Int) { Value = docEntry }
            };
            return EjecutarDataSetAsync(spName, p, ct);
        }

        public async Task<DataSet> EjecutarDataSetAsync(string spName, IEnumerable<SqlParameter> parametros, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(spName))
                throw new ArgumentException("SP name requerido.", nameof(spName));

            await using var cn = (SqlConnection)_cnFactory.CreateConnection();
            await cn.OpenAsync(ct);

            await using var cmd = new SqlCommand(spName, cn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 120
            };

            if (parametros != null)
            {
                foreach (var p in parametros)
                {
                    if (p == null) continue;
                    cmd.Parameters.Add(p);
                }
            }

            var ds = new DataSet();

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            int tableIndex = 0;

            do
            {
                var dt = new DataTable($"T{tableIndex}");
                dt.Load(reader);
                ds.Tables.Add(dt);
                tableIndex++;
            }
            while (!reader.IsClosed && await reader.NextResultAsync(ct));

            return ds;
        }

        public async Task<DataTable> EjecutarDataTableAsync(string spName, int docEntry, CancellationToken ct)
        {
            var ds = await EjecutarDataSetAsync(spName, docEntry, ct);
            if (ds.Tables.Count == 0)
                return new DataTable("T0");
            return ds.Tables[0];
        }

        public async Task<int> EjecutarNonQueryAsync(string spName, IEnumerable<SqlParameter> parametros, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(spName))
                throw new ArgumentException("SP name requerido.", nameof(spName));

            await using var cn = (SqlConnection)_cnFactory.CreateConnection();
            await cn.OpenAsync(ct);

            await using var cmd = new SqlCommand(spName, cn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 120
            };

            if (parametros != null)
            {
                foreach (var p in parametros)
                {
                    if (p == null) continue;
                    cmd.Parameters.Add(p);
                }
            }

            return await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task<string> EjecutarEscalarAsync(string spName, int docEntry, CancellationToken ct)
        {
            var p = new[]
            {
                new SqlParameter("@DocEntry", SqlDbType.Int) { Value = docEntry }
            };
            return await EjecutarEscalarAsync(spName, p, ct);
        }

        public async Task<string> EjecutarEscalarAsync(string spName, IEnumerable<SqlParameter> parameters, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(spName))
                throw new ArgumentException("spName es requerido.", nameof(spName));

            await using var cn = (SqlConnection)_cnFactory.CreateConnection();
            await cn.OpenAsync(ct);

            await using var cmd = new SqlCommand(spName, cn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 120
            };

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.Add(param);
                }
            }

            var result = await cmd.ExecuteScalarAsync(ct);
            return result == null || result == DBNull.Value ? string.Empty : result.ToString();
        }

        public async Task<DataTable> EjecutarDataTableAsync(string spName, IEnumerable<SqlParameter> parameters, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(spName))
                throw new ArgumentException("spName es requerido.", nameof(spName));

            await using var cn = (SqlConnection)_cnFactory.CreateConnection();
            await cn.OpenAsync(ct);

            await using var cmd = new SqlCommand(spName, cn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 120
            };

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.Add(param);
                }
            }

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            var dataTable = new DataTable();
            dataTable.Load(reader);
            return dataTable;
        }

        public async Task<object> EjecutarValorUnicoAsync(string spName, IEnumerable<SqlParameter> parameters, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(spName))
                throw new ArgumentException("spName es requerido.", nameof(spName));

            await using var cn = (SqlConnection)_cnFactory.CreateConnection();
            await cn.OpenAsync(ct);

            await using var cmd = new SqlCommand(spName, cn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 120
            };

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    cmd.Parameters.Add(param);
                }
            }

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            if (!await reader.ReadAsync(ct))
                throw new InvalidOperationException($"El SP {spName} no devolvió datos.");

            var value = reader.GetValue(0);
            return value == DBNull.Value ? null : value;
        }
    }
}