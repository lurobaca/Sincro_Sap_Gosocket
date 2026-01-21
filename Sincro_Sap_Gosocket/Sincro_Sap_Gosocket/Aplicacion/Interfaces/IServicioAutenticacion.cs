// Sincro_Sap_Gosocket/Aplicacion/Interfaces/IServicioAutenticacion.cs
using System.Net.Http.Headers;

namespace Sincro_Sap_Gosocket.Aplicacion.Interfaces
{
    /// <summary>
    /// Provee la autenticación requerida para consumir el API de GoSocket
    /// según el Manual-API_Cliente: Basic Auth (ApiKey/Password).
    /// </summary>
    public interface IServicioAutenticacion
    {
        /// <summary>
        /// Construye el encabezado Authorization para GoSocket usando Basic Auth.
        /// </summary>
        AuthenticationHeaderValue ObtenerEncabezadoAutorizacion();

    

        /// <summary>
        /// Valida que exista configuración mínima para autenticación Basic.
        /// Útil para fallar temprano al iniciar el Worker.
        /// </summary>
        void ValidarConfiguracion();
    }
}
