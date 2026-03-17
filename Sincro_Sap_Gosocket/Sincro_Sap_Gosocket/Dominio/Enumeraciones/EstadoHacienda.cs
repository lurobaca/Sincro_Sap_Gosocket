using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Sincro_Sap_Gosocket.Dominio.Enumeraciones
{
    public enum EstadoHacienda
    {
        Pendiente = 0,
        Procesando = 1,
        Aceptado = 2,
        Rechazado = 3,
        Error = 4
    }
}