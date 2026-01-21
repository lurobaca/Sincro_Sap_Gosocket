using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Configuracion.OpcionesSql;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sincro_Sap_Gosocket.Infraestructura.Sql
{
    public sealed class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(IOptions<OpcionesSql> opcionesSql)
        {
            if (opcionesSql is null) throw new ArgumentNullException(nameof(opcionesSql));

            _connectionString = opcionesSql.Value.ConnectionString;

            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException("La ConnectionString de SQL no está configurada (OpcionesSql.ConnectionString).");
        }

        public SqlConnection CreateConnection()
            => new SqlConnection(_connectionString);

        public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct)
        {
            var cn = CreateConnection();
            await cn.OpenAsync(ct);
            return cn;
        }
    }
}
