using Microsoft.Data.SqlClient;

namespace Sincro_Sap_Gosocket.Aplicacion.Interfaces
{
    public interface ISqlConnectionFactory
    {
        SqlConnection CreateConnection();
    }
}
