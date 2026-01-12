// Sincro_Sap_Gosocket/Aplicacion/Interfaces/IServicioAutenticacion.cs
using System.Threading.Tasks;

namespace Sincro_Sap_Gosocket.Aplicacion.Interfaces
{
    /// <summary>
    /// Define las operaciones para la autenticación con la API de GoSocket
    /// </summary>
    public interface IServicioAutenticacion
    {
        /// <summary>
        /// Obtiene un token de acceso válido para la API
        /// Implementa caché automático y renovación cuando expira
        /// </summary>
        Task<string> ObtenerTokenDeAccesoAsync();

        /// <summary>
        /// Fuerza la renovación del token, ignorando el caché
        /// Útil cuando se detecta que un token ha sido revocado
        /// </summary>
        Task<string> RenovarTokenDeAccesoAsync();

        /// <summary>
        /// Limpia el token en caché, forzando una nueva autenticación
        /// </summary>
        void LimpiarCacheToken();
    }
}