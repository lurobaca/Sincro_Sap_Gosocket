using Microsoft.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace Sincro_Sap_Gosocket.Aplicacion.Interfaces
{
    public interface ISqlConnectionFactory
    {
        SqlConnection CreateConnection();

        Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken ct);
    }
}
