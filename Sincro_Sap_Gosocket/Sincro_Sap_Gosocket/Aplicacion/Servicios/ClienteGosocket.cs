// Sincro_Sap_Gosocket/Aplicacion/Servicios/ClienteGosocket.cs
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Configuracion;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Comun;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Peticiones;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Respuestas;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Excepciones;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;

namespace Sincro_Sap_Gosocket.Aplicacion.Servicios
{
    /// <summary>
    /// Implementación del cliente GoSocket API
    /// Diseñado con principios SOLID y Clean Code
    /// </summary>
    public class ClienteGosocket : IClienteGosocket
    {
        private readonly HttpClient _httpClient;
        private readonly IServicioAutenticacion _servicioAutenticacion;
        private readonly OpcionesGosocket _opciones;
        private readonly ILogger<ClienteGosocket> _logger;
        private readonly JsonSerializerSettings _configuracionJson;

        // Constantes para configuración
        private const int TimeoutSegundos = 120;
        private const int MaximoReintentosPeticion = 3;
        private readonly TimeSpan TiempoEsperaEntreReintentos = TimeSpan.FromSeconds(2);

        public ClienteGosocket(
            HttpClient httpClient,
            IServicioAutenticacion servicioAutenticacion,
            IOptions<OpcionesGosocket> opciones,
            ILogger<ClienteGosocket> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _servicioAutenticacion = servicioAutenticacion ?? throw new ArgumentNullException(nameof(servicioAutenticacion));
            _opciones = opciones?.Value ?? throw new ArgumentNullException(nameof(opciones));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Configurar serialización JSON
            _configuracionJson = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatString = "yyyy-MM-ddTHH:mm:ss",
                Formatting = Newtonsoft.Json.Formatting.None
            };

            ConfigurarHttpClient();
            ValidarConfiguracion();
        }

        private void ConfigurarHttpClient()
        {
            _httpClient.BaseAddress = new Uri(_opciones.ApiBaseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.Timeout = TimeSpan.FromSeconds(TimeoutSegundos);

            // Headers adicionales recomendados
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SincroSapGoSocket/1.0");
            _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
        }

        private void ValidarConfiguracion()
        {
            if (!_opciones.UsarOAuth)
            {
                throw new InvalidOperationException(
                    "El ClienteGosocket requiere configuración OAuth 2.0. " +
                    "Verifique las propiedades en appsettings.json.");
            }
        }

        #region Implementación de Métodos de la API

        public async Task<RespuestaApi<RespuestaGetAccount>> ConsultarCuentaAsync(PeticionGetAccount peticion)
        {
            if (peticion == null)
                throw new ArgumentNullException(nameof(peticion));

            return await EjecutarPeticionAsync<RespuestaGetAccount>(
                endpoint: "GetAccount",
                metodoHttp: HttpMethod.Post,
                cuerpoPeticion: peticion);
        }

        public async Task<RespuestaApi<RespuestaGetDocument>> ObtenerDocumentoAsync(PeticionGetDocument peticion)
        {
            if (peticion == null)
                throw new ArgumentNullException(nameof(peticion));

            return await EjecutarPeticionAsync<RespuestaGetDocument>(
                endpoint: "GetDocument",
                metodoHttp: HttpMethod.Post,
                cuerpoPeticion: peticion);
        }

        public async Task<RespuestaApi<RespuestaSendDocumentToAuthority>> EnviarDocumentoAutoridadAsync(
            PeticionSendDocumentToAuthority peticion)
        {
            if (peticion == null)
                throw new ArgumentNullException(nameof(peticion));

            return await EjecutarPeticionAsync<RespuestaSendDocumentToAuthority>(
                endpoint: "SendDocumentToAuthority",
                metodoHttp: HttpMethod.Post,
                cuerpoPeticion: peticion);
        }

        public async Task<RespuestaApi<RespuestaSendDocumentToAuthority>> ValidarDocumentoAsync(
            PeticionSendDocumentToAuthority peticion)
        {
            if (peticion == null)
                throw new ArgumentNullException(nameof(peticion));

            return await EjecutarPeticionAsync<RespuestaSendDocumentToAuthority>(
                endpoint: "SendDocumentToValidate",
                metodoHttp: HttpMethod.Post,
                cuerpoPeticion: peticion);
        }

        public async Task<RespuestaApi<RespuestaDownloadDocumentXml>> DescargarXmlDocumentoAsync(
            PeticionDownloadDocumentXml peticion)
        {
            if (peticion == null)
                throw new ArgumentNullException(nameof(peticion));

            return await EjecutarPeticionAsync<RespuestaDownloadDocumentXml>(
                endpoint: "DownloadDocumentXml",
                metodoHttp: HttpMethod.Post,
                cuerpoPeticion: peticion);
        }

        public async Task<RespuestaApi<RespuestaDownloadDocumentPdf>> DescargarPdfDocumentoAsync(
            PeticionDownloadDocumentPdf peticion)
        {
            if (peticion == null)
                throw new ArgumentNullException(nameof(peticion));

            return await EjecutarPeticionAsync<RespuestaDownloadDocumentPdf>(
                endpoint: "DownloadDocumentPdf",
                metodoHttp: HttpMethod.Post,
                cuerpoPeticion: peticion);
        }

        // Métodos pendientes de implementación específica
        public Task<RespuestaApi<object>> ObtenerDocumentosRecibidosAsync(object peticion)
        {
            throw new NotImplementedException("Implementación pendiente según el manual v10 página 131");
        }

        public Task<RespuestaApi<object>> ConfirmarDocumentosRecibidosAsync(object peticion)
        {
            throw new NotImplementedException("Implementación pendiente según el manual v10 página 137");
        }

        public Task<RespuestaApi<object>> ConsultarEventosDocumentoAsync(object peticion)
        {
            throw new NotImplementedException("Implementación pendiente según el manual v10 página 142");
        }

        public Task<RespuestaApi<object>> CambiarEstadoDocumentoAsync(object peticion)
        {
            throw new NotImplementedException("Implementación pendiente según el manual v10 página 150");
        }

        #endregion

        #region Método Genérico para Ejecutar Peticiones

        private async Task<RespuestaApi<T>> EjecutarPeticionAsync<T>(
            string endpoint,
            HttpMethod metodoHttp,
            object cuerpoPeticion = null) where T : class
        {
            var idCorrelacion = Guid.NewGuid();
            var contextoLog = new { IdCorrelacion = idCorrelacion, Endpoint = endpoint };

            try
            {
                _logger.LogDebug(
                    "[{IdCorrelacion}] Iniciando petición {Metodo} {Endpoint}",
                    idCorrelacion, metodoHttp, endpoint);

                // Ejecutar con reintentos
                return await EjecutarPeticionConReintentosAsync<T>(
                    endpoint, metodoHttp, cuerpoPeticion, idCorrelacion);
            }
            catch (GoSocketApiException ex)
            {
                _logger.LogError(ex,
                    "[{IdCorrelacion}] Error específico de API en {Endpoint}: {Mensaje}",
                    idCorrelacion, endpoint, ex.MensajeAmigable);

                return RespuestaApi<T>.CrearFallido(
                    mensajeError: ex.MensajeAmigable,
                    codigoError: ex.CodigoError ?? "API_ERROR");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[{IdCorrelacion}] Error inesperado en {Endpoint}",
                    idCorrelacion, endpoint);

                return RespuestaApi<T>.CrearFallido(
                    mensajeError: $"Error inesperado: {ex.Message}",
                    codigoError: "UNEXPECTED_ERROR");
            }
        }

        private async Task<RespuestaApi<T>> EjecutarPeticionConReintentosAsync<T>(
            string endpoint,
            HttpMethod metodoHttp,
            object cuerpoPeticion,
            Guid idCorrelacion) where T : class
        {
            int intento = 0;
            Exception ultimaExcepcion = null;

            while (intento < MaximoReintentosPeticion)
            {
                intento++;

                try
                {
                    // 1. Obtener token de acceso
                    var tokenAcceso = await _servicioAutenticacion.ObtenerTokenDeAccesoAsync();

                    // 2. Crear petición HTTP
                    var peticionHttp = CrearPeticionHttp(endpoint, metodoHttp, cuerpoPeticion, tokenAcceso);

                    // 3. Enviar petición
                    _logger.LogDebug("[{IdCorrelacion}] Enviando petición (Intento {Intento})",
                        idCorrelacion, intento);

                    var respuesta = await _httpClient.SendAsync(peticionHttp);

                    // 4. Procesar respuesta
                    return await ProcesarRespuestaHttpAsync<T>(respuesta, idCorrelacion);
                }
                catch (HttpRequestException ex) when (intento < MaximoReintentosPeticion)
                {
                    ultimaExcepcion = ex;
                    _logger.LogWarning(ex,
                        "[{IdCorrelacion}] Error de red (Intento {Intento}). Reintentando...",
                        idCorrelacion, intento);

                    await Task.Delay(TiempoEsperaEntreReintentos * intento);
                }
                catch (UnauthorizedAccessException) when (intento < MaximoReintentosPeticion)
                {
                    // Token posiblemente expirado, limpiar caché y reintentar
                    _logger.LogWarning(
                        "[{IdCorrelacion}] Token no autorizado (Intento {Intento}). Renovando token...",
                        idCorrelacion, intento);

                    _servicioAutenticacion.LimpiarCacheToken();
                    await Task.Delay(TiempoEsperaEntreReintentos);
                }
            }

            _logger.LogError(ultimaExcepcion,
                "[{IdCorrelacion}] Fallo después de {MaximoReintentos} intentos",
                idCorrelacion, MaximoReintentosPeticion);

            throw new GoSocketApiException(
                $"Fallo después de {MaximoReintentosPeticion} intentos",
                ultimaExcepcion);
        }

        private HttpRequestMessage CrearPeticionHttp(
            string endpoint,
            HttpMethod metodoHttp,
            object cuerpoPeticion,
            string tokenAcceso)
        {
            var peticion = new HttpRequestMessage(metodoHttp, endpoint);

            // Configurar headers
            peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenAcceso);
            peticion.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());

            // Configurar body si existe
            if (cuerpoPeticion != null)
            {
                var json = JsonConvert.SerializeObject(cuerpoPeticion, _configuracionJson);
                peticion.Content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogTrace("Body de petición: {Json}", json);
            }

            return peticion;
        }

        private async Task<RespuestaApi<T>> ProcesarRespuestaHttpAsync<T>(
            HttpResponseMessage respuesta,
            Guid idCorrelacion) where T : class
        {
            var contenidoRespuesta = await respuesta.Content.ReadAsStringAsync();

            _logger.LogTrace("[{IdCorrelacion}] Respuesta: Status={StatusCode}, Body={Contenido}",
                idCorrelacion, respuesta.StatusCode, contenidoRespuesta);

            if (respuesta.IsSuccessStatusCode)
            {
                try
                {
                    var datos = JsonConvert.DeserializeObject<T>(contenidoRespuesta, _configuracionJson);

                    _logger.LogDebug("[{IdCorrelacion}] Petición exitosa", idCorrelacion);
                    return RespuestaApi<T>.CrearExitoso(datos);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex,
                        "[{IdCorrelacion}] Error al deserializar respuesta JSON exitosa",
                        idCorrelacion);

                    return RespuestaApi<T>.CrearFallido(
                        "Error al procesar la respuesta del servidor",
                        "DESERIALIZATION_ERROR");
                }
            }
            else
            {
                return await ManejarErrorHttpAsync<T>(respuesta, contenidoRespuesta, idCorrelacion);
            }
        }

        private async Task<RespuestaApi<T>> ManejarErrorHttpAsync<T>(
            HttpResponseMessage respuesta,
            string contenidoRespuesta,
            Guid idCorrelacion) where T : class
        {
            _logger.LogWarning("[{IdCorrelacion}] Error HTTP: Status={StatusCode}",
                idCorrelacion, respuesta.StatusCode);

            string mensajeError = $"Error HTTP {(int)respuesta.StatusCode}: {respuesta.ReasonPhrase}";
            string codigoError = $"HTTP_{(int)respuesta.StatusCode}";

            try
            {
                var errorApi = JsonConvert.DeserializeObject<RespuestaError>(contenidoRespuesta);

                if (errorApi != null && !string.IsNullOrEmpty(errorApi.Error))
                {
                    mensajeError = errorApi.ErrorDescription ?? errorApi.Error;
                    codigoError = errorApi.Error;

                    // Si es error de autenticación, limpiar caché de token
                    if (respuesta.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        _servicioAutenticacion.LimpiarCacheToken();
                    }
                }
            }
            catch (JsonException)
            {
                // Si no se puede deserializar como error estructurado, usar el contenido crudo
                if (!string.IsNullOrWhiteSpace(contenidoRespuesta))
                {
                    mensajeError = contenidoRespuesta.Trim().Length > 500
                        ? contenidoRespuesta.Trim().Substring(0, 500) + "..."
                        : contenidoRespuesta.Trim();
                }
            }

            return RespuestaApi<T>.CrearFallido(mensajeError, codigoError);
        }

        #endregion
    }
}