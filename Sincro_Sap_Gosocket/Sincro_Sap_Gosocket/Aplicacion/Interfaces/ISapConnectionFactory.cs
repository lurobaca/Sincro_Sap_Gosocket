using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 
using SAPbobsCOM; 

namespace Sincro_Sap_Gosocket.Aplicacion.Interfaces
{
    public interface ISapConnectionFactory
    {
        Company CrearConexion();
    }
}