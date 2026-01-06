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
                CommandTimeout = 120 // ajusta si ocupas
            };

            if (parametros != null)
            {
                foreach (var p in parametros)
                {
                    if (p == null) continue;
                    cmd.Parameters.Add(p);
                }
            }

            // Lector -> DataSet con tablas (ideal si tu SP devuelve Encabezado + Detalle + etc.)
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
        public async Task<object> EjecutarAsync(string spName, int docEntry, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(spName))
                throw new ArgumentException("spName es requerido.", nameof(spName));

            await using var cn = _cnFactory.CreateConnection();
            await cn.OpenAsync(ct);

            await using var cmd = new SqlCommand(spName, cn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 120
            };

            // Ajusta el nombre del parámetro al real de tu SP: @DocEntry o @docEntry
            cmd.Parameters.Add(new SqlParameter("@DocEntry", SqlDbType.Int) { Value = docEntry });

            // Caso común: el SP devuelve 1 fila con 1 columna (XML/JSON)
            // Si tu SP devuelve muchas columnas/filas, cambia esta lectura.
            await using var rd = await cmd.ExecuteReaderAsync(ct);

            if (!await rd.ReadAsync(ct))
                throw new InvalidOperationException($"El SP {spName} no devolvió datos para DocEntry={docEntry}.");

            // Si la primera columna es NVARCHAR(MAX)/XML/JSON:
            var value = rd.GetValue(0);
            return value == DBNull.Value ? "" : value;
        }
    }
}
