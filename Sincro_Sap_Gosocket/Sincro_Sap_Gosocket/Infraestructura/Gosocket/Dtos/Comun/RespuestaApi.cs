// Sincro_Sap_Gosocket/Infraestructura/Gosocket/Dtos/Comun/RespuestaApi.cs
namespace Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Comun
{
    /// <summary>
    /// Respuesta genérica estructurada para todas las peticiones API
    /// </summary>
    /// <typeparam name="T">Tipo de datos contenidos en la respuesta</typeparam>
    public class RespuestaApi<T>
    {
        /// <summary>
        /// Indica si la petición fue exitosa
        /// </summary>
        public bool Exitoso { get; set; }

        /// <summary>
        /// Datos de la respuesta (solo cuando Exitoso = true)
        /// </summary>
        public T Datos { get; set; }

        /// <summary>
        /// Mensaje de error descriptivo (solo cuando Exitoso = false)
        /// </summary>
        public string MensajeError { get; set; } = string.Empty;

        /// <summary>
        /// Código de error para identificación programática
        /// </summary>
        public string CodigoError { get; set; } = string.Empty;

        /// <summary>
        /// Fecha y hora de la respuesta
        /// </summary>
        public DateTime FechaRespuesta { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Crea una respuesta exitosa con datos
        /// </summary>
        public static RespuestaApi<T> CrearExitoso(T datos) =>
            new RespuestaApi<T>
            {
                Exitoso = true,
                Datos = datos,
                FechaRespuesta = DateTime.UtcNow
            };

        /// <summary>
        /// Crea una respuesta fallida con mensaje de error
        /// </summary>
        public static RespuestaApi<T> CrearFallido(string mensajeError, string codigoError = "") =>
            new RespuestaApi<T>
            {
                Exitoso = false,
                MensajeError = mensajeError,
                CodigoError = codigoError,
                FechaRespuesta = DateTime.UtcNow
            };
    }
}