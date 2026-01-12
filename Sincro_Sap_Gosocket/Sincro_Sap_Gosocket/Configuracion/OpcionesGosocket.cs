// Sincro_Sap_Gosocket/Configuracion/OpcionesGosocket.cs
using System;

namespace Sincro_Sap_Gosocket.Configuracion
{
    /// <summary>
    /// Configuración para la conexión con la API de GoSocket
    /// Soportando tanto OAuth 2.0 como Basic Auth para compatibilidad
    /// </summary>
    public class OpcionesGosocket
    {
        // ========== CONFIGURACIÓN OAUTH 2.0 (RECOMENDADO) ==========

        /// <summary>
        /// URL base para consumir los endpoints de la API (versión OAuth 2.0)
        /// Ejemplo Sandbox: https://developers-sbx.gosocket.net/api/v2/
        /// </summary>
        public string ApiBaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// URL para obtener el token de acceso OAuth 2.0
        /// Ejemplo Sandbox: https://developers-sbx.gosocket.net/oauth2/token
        /// </summary>
        public string OAuthTokenUrl { get; set; } = string.Empty;

        /// <summary>
        /// Client ID para autenticación OAuth 2.0 (grant_type: client_credentials)
        /// Formato: UUID (ej: 6188048e-8989-4272-ac83-6d6e06cbc496)
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Client Secret para autenticación OAuth 2.0 (grant_type: client_credentials)
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;

        // ========== CONFIGURACIÓN BASIC AUTH (LEGACY) ==========

        /// <summary>
        /// URL base para autenticación Basic (legacy)
        /// Mantenido por compatibilidad con configuraciones existentes
        /// </summary>
        public string ApiUrl { get; set; } = string.Empty;

        /// <summary>
        /// API Key/Username para autenticación Basic (legacy)
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Password para autenticación Basic (legacy)
        /// </summary>
        public string Password { get; set; } = string.Empty;

        // ========== PROPIEDADES CALCULADAS ==========

        /// <summary>
        /// Indica si la configuración está preparada para usar OAuth 2.0
        /// </summary>
        public bool UsarOAuth =>
            !string.IsNullOrWhiteSpace(OAuthTokenUrl)
            && !string.IsNullOrWhiteSpace(ApiBaseUrl)
            && !string.IsNullOrWhiteSpace(ClientId)
            && !string.IsNullOrWhiteSpace(ClientSecret);

        /// <summary>
        /// Indica si la configuración está preparada para usar Basic Auth
        /// </summary>
        public bool UsarBasicAuth =>
            !string.IsNullOrWhiteSpace(ApiUrl)
            && !string.IsNullOrWhiteSpace(ApiKey)
            && !string.IsNullOrWhiteSpace(Password);

        /// <summary>
        /// Valida que la configuración mínima esté presente
        /// </summary>
        public void ValidarConfiguracion()
        {
            if (!UsarOAuth && !UsarBasicAuth)
            {
                throw new InvalidOperationException(
                    "La configuración de GoSocket no está completa. " +
                    "Configure OAuth 2.0 (ApiBaseUrl, OAuthTokenUrl, ClientId, ClientSecret) " +
                    "o Basic Auth (ApiUrl, ApiKey, Password)."
                );
            }

            if (UsarOAuth)
            {
                if (!Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out _))
                    throw new InvalidOperationException($"ApiBaseUrl no es una URL válida: {ApiBaseUrl}");

                if (!Uri.TryCreate(OAuthTokenUrl, UriKind.Absolute, out _))
                    throw new InvalidOperationException($"OAuthTokenUrl no es una URL válida: {OAuthTokenUrl}");
            }

            if (UsarBasicAuth && !Uri.TryCreate(ApiUrl, UriKind.Absolute, out _))
                throw new InvalidOperationException($"ApiUrl no es una URL válida: {ApiUrl}");
        }
    }
}