using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

 
namespace Sincro_Sap_Gosocket.Configuracion
{
    public class OpcionesServicio
    {
        public int PollSeconds { get; set; } = 5;
        public int BatchSize { get; set; } = 10;
    }
}
