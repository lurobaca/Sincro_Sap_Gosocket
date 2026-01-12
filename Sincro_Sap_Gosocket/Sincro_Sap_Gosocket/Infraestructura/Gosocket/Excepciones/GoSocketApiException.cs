// Sincro_Sap_Gosocket/Infraestructura/Gosocket/Excepciones/GoSocketApiException.cs
using System;

namespace Sincro_Sap_Gosocket.Infraestructura.Gosocket.Excepciones
{
    /// <summary>
    /// Excepción personalizada para errores específicos de la API GoSocket
    /// </summary>
    public class GoSocketApiException : Exception
    {
        /// <summary>
        /// Código de error específico de la API
        /// </summary>
        public string CodigoError { get; }

        /// <summary>
        /// Mensaje amigable para el usuario
        /// </summary>
        public string MensajeAmigable { get; }

        /// <summary>
        /// Indica si el error es temporal y se puede reintentar
        /// </summary>
        public bool EsErrorTemporal { get; }

        /// <summary>
        /// Crea una nueva excepción de API GoSocket
        /// </summary>
        public GoSocketApiException(string mensaje)
            : base(mensaje)
        {
            MensajeAmigable = mensaje;
            CodigoError = "API_ERROR";
            EsErrorTemporal = false;
        }

        /// <summary>
        /// Crea una nueva excepción de API GoSocket con código de error
        /// </summary>
        public GoSocketApiException(string mensaje, string codigoError, bool esErrorTemporal = false)
            : base($"{codigoError}: {mensaje}")
        {
            MensajeAmigable = mensaje;
            CodigoError = codigoError;
            EsErrorTemporal = esErrorTemporal;
        }

        /// <summary>
        /// Crea una nueva excepción de API GoSocket con inner exception
        /// </summary>
        public GoSocketApiException(string mensaje, Exception innerException)
            : base(mensaje, innerException)
        {
            MensajeAmigable = mensaje;
            CodigoError = "API_ERROR";
            EsErrorTemporal = innerException is System.Net.Http.HttpRequestException;
        }

        /// <summary>
        /// Crea una nueva excepción de API GoSocket completa
        /// </summary>
        public GoSocketApiException(string mensaje, string codigoError, Exception innerException, bool esErrorTemporal = false)
            : base($"{codigoError}: {mensaje}", innerException)
        {
            MensajeAmigable = mensaje;
            CodigoError = codigoError;
            EsErrorTemporal = esErrorTemporal || innerException is System.Net.Http.HttpRequestException;
        }
    }
}