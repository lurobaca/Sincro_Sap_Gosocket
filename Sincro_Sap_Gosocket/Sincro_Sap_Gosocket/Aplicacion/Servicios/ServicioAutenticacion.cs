// Sincro_Sap_Gosocket/Aplicacion/Servicios/ServicioAutenticacion.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Configuracion;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Excepciones;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sincro_Sap_Gosocket.Aplicacion.Servicios
{
    /// <summary>
    /// Implementación del servicio de autenticación para GoSocket API
    /// Maneja OAuth 2.0 con Client Credentials Grant
    /// </summary>
    public class ServicioAutenticacion : IServicioAutenticacion
    {
        private readonly HttpClient _httpClient;
        private readonly OpcionesGosocket _opciones;
        private readonly ILogger<ServicioAutenticacion> _logger;

        // Caché de token con seguridad thread-safe
        private string _tokenEnCache = string.Empty;
        private DateTime _fechaExpiracionToken = DateTime.MinValue;
        private readonly object _bloqueoSincronizacion = new object();
        private const int MargenSeguridadExpiracionSegundos = 300; // 5 minutos

        // Constantes para reintentos
        private const int MaximoReintentos = 3;
        private const int TiempoEsperaReintentoMs = 1000;

        public ServicioAutenticacion(
            HttpClient httpClient,
            IOptions<OpcionesGosocket> opciones,
            ILogger<ServicioAutenticacion> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _opciones = opciones?.Value ?? throw new ArgumentNullException(nameof(opciones));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Validar configuración al instanciar
            _opciones.ValidarConfiguracion();

            if (!_opciones.UsarOAuth)
            {
                throw new InvalidOperationException(
                    "El ServicioAutenticacion requiere configuración OAuth 2.0. " +
                    "Verifique las propiedades ClientId, ClientSecret, OAuthTokenUrl y ApiBaseUrl."
                );
            }
        }

        public async Task<string> ObtenerTokenDeAccesoAsync()
        {
            // Verificar si tenemos un token válido en caché
            if (TokenEnCacheEsValido())
            {
                _logger.LogDebug("Token de acceso válido encontrado en caché. Token expira: {Expiracion}",
                    _fechaExpiracionToken);
                return _tokenEnCache;
            }

            // Si no hay token válido, obtener uno nuevo con reintentos
            return await ObtenerNuevoTokenConReintentoAsync();
        }

        public async Task<string> RenovarTokenDeAccesoAsync()
        {
            _logger.LogInformation("Renovación forzada de token de acceso solicitada.");
            LimpiarCacheToken();
            return await ObtenerNuevoTokenConReintentoAsync();
        }

        public void LimpiarCacheToken()
        {
            lock (_bloqueoSincronizacion)
            {
                _tokenEnCache = string.Empty;
                _fechaExpiracionToken = DateTime.MinValue;
                _logger.LogDebug("Cache de token limpiado exitosamente.");
            }
        }

        private bool TokenEnCacheEsValido()
        {
            lock (_bloqueoSincronizacion)
            {
                return !string.IsNullOrEmpty(_tokenEnCache)
                    && DateTime.UtcNow < _fechaExpiracionToken;
            }
        }

        private async Task<string> ObtenerNuevoTokenConReintentoAsync()
        {
            int intento = 0;
            Exception ultimaExcepcion = null;

            while (intento < MaximoReintentos)
            {
                intento++;

                try
                {
                    _logger.LogInformation("Solicitando nuevo token de acceso (Intento {Intento}/{Maximo})",
                        intento, MaximoReintentos);

                    var token = await SolicitarTokenAlServidorAsync();

                    _logger.LogInformation("Token obtenido exitosamente en intento {Intento}", intento);
                    return token;
                }
                catch (HttpRequestException ex) when (intento < MaximoReintentos)
                {
                    ultimaExcepcion = ex;
                    _logger.LogWarning(ex,
                        "Error de red al obtener token (Intento {Intento}). Reintentando en {TiempoEspera}ms...",
                        intento, TiempoEsperaReintentoMs);

                    await Task.Delay(TiempoEsperaReintentoMs * intento); // Backoff exponencial
                }
                catch (Newtonsoft.Json.JsonException ex) when (intento < MaximoReintentos)
                {
                    ultimaExcepcion = ex;
                    _logger.LogWarning(ex,
                        "Error de formato JSON al obtener token (Intento {Intento}). Reintentando...",
                        intento);

                    await Task.Delay(TiempoEsperaReintentoMs);
                }
            }

            _logger.LogError(ultimaExcepcion,
                "Fallo al obtener token después de {MaximoReintentos} intentos",
                MaximoReintentos);

            throw new GoSocketApiException(
                "No se pudo obtener token de acceso después de múltiples intentos",
                ultimaExcepcion);
        }

        private async Task<string> SolicitarTokenAlServidorAsync()
        {
            var parametrosPeticion = new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _opciones.ClientId,
                ["client_secret"] = _opciones.ClientSecret,
                ["scope"] = "all"
            };

            using var contenidoPeticion = new FormUrlEncodedContent(parametrosPeticion);

            // Configurar timeout específico para autenticación
            using var tokenCancelacion = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));

            var respuesta = await _httpClient.PostAsync(
                _opciones.OAuthTokenUrl,
                contenidoPeticion,
                tokenCancelacion.Token);

            if (!respuesta.IsSuccessStatusCode)
            {
                var contenidoError = await respuesta.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Error en autenticación. Status: {StatusCode}, Respuesta: {ContenidoError}",
                    respuesta.StatusCode,
                    contenidoError);

                throw new GoSocketApiException(
                    $"Error de autenticación OAuth 2.0: {respuesta.StatusCode}",
                    contenidoError);
            }

            var jsonRespuesta = await respuesta.Content.ReadAsStringAsync();
             var respuestaToken = JsonConvert.DeserializeObject<RespuestaTokenOAuth>(jsonRespuesta);

            if (respuestaToken == null || string.IsNullOrEmpty(respuestaToken.AccessToken))
            {
                throw new GoSocketApiException("La respuesta del servidor OAuth no contiene un token válido.");
            }

            // Actualizar caché con el nuevo token
            lock (_bloqueoSincronizacion)
            {
                _tokenEnCache = respuestaToken.AccessToken;
                _fechaExpiracionToken = DateTime.UtcNow.AddSeconds(
                    respuestaToken.ExpiresIn - MargenSeguridadExpiracionSegundos);
            }

            _logger.LogInformation("Token de acceso almacenado en caché. Expira en: {Expiracion} (UTC)",
                _fechaExpiracionToken);

            return respuestaToken.AccessToken;
        }

        // Clase interna para deserialización de respuesta de token
        private class RespuestaTokenOAuth
        {
            [JsonProperty("access_token")]
            public string AccessToken { get; set; } = string.Empty;

            [JsonProperty("token_type")]
            public string TokenType { get; set; } = string.Empty;

            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonProperty("scope", NullValueHandling = NullValueHandling.Ignore)]
            public string Scope { get; set; } = string.Empty;
        }
    }
}