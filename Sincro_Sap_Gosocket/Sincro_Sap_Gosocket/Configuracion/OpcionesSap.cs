using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

 
namespace Sincro_Sap_Gosocket.Configuracion
{
    public sealed class OpcionesSap
    {
        public string Servidor { get; set; } = string.Empty;
        public string LicenciaServidor { get; set; } = string.Empty;
        public string BaseDatos { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string Clave { get; set; } = string.Empty;
        public string UsuarioDb { get; set; } = string.Empty;
        public string ClaveDb { get; set; } = string.Empty;
        public bool UsarTrusted { get; set; }

        // SQL Server
        public string TipoServidorSap { get; set; } = "MSSQL2019";

        // Lenguaje SAP opcional
        public int Lenguaje { get; set; } = 23; // ln_Spanish_La
    }
}