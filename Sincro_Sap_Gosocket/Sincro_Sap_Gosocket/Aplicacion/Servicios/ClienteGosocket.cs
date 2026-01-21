// Infraestructura/Gosocket/ClienteGosocket.cs
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Configuracion;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Comun;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Peticiones;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Respuestas;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Excepciones;

namespace Sincro_Sap_Gosocket.Infraestructura.Gosocket
{
    /// <summary>
    /// Cliente HTTP para consumir GoSocket según Manual-API_Cliente:
    /// autenticación Basic Auth (ApiKey/Password).
    /// </summary>
    public class ClienteGosocket : IClienteGosocket
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ClienteGosocket> _logger;
        private readonly OpcionesGosocket _opciones;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ClienteGosocket(
            HttpClient httpClient,
            ILogger<ClienteGosocket> logger,
            IOptions<OpcionesGosocket> opciones)
        {
            _httpClient = httpClient;
            _logger = logger;
            _opciones = opciones.Value;

            _opciones.ValidarConfiguracion();

            // BaseAddress para API (v1 según su documentación)
            // Ej: https://developers-sbx.gosocket.net/api/v1/
            _httpClient.BaseAddress = new Uri(_opciones.ApiBaseUrl);

            // Basic Auth: ApiKey como username + Password
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_opciones.ApiKey}:{_opciones.ApiPassword}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);

            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public Task<RespuestaApi<RespuestaSendDocumentToAuthority>> EnviarDocumentoAutoridadAsync(
            PeticionSendDocumentToAuthority peticion,
            CancellationToken ct)
        {
            return EjecutarPeticionAsync<RespuestaSendDocumentToAuthority>(
                endpoint: "Document/SendDocumentToAuthority",
                metodo: HttpMethod.Post,
                body: peticion,
                ct: ct);
        }

        public Task<RespuestaApi<RespuestaGetAccount>> ConsultarCuentaAsync(PeticionGetAccount peticion, CancellationToken ct)
        {
            return EjecutarPeticionAsync<RespuestaGetAccount>(
                endpoint: "Document/GetAccount",
                metodo: HttpMethod.Post,
                body: peticion,
                ct: ct);
        }

        public Task<RespuestaApi<RespuestaGetDocument>> ObtenerDocumentoAsync(PeticionGetDocument peticion, CancellationToken ct)
        {
            return EjecutarPeticionAsync<RespuestaGetDocument>(
                endpoint: "Document/GetDocument",
                metodo: HttpMethod.Post,
                body: peticion,
                ct: ct);
        }

        public Task<RespuestaApi<RespuestaDownloadDocumentPdf>> DescargarPdfDocumentoAsync(PeticionDownloadDocumentPdf peticion, CancellationToken ct)
        {
            return EjecutarPeticionAsync<RespuestaDownloadDocumentPdf>(
                endpoint: "File/DownloadDocumentPdf",
                metodo: HttpMethod.Get,
                body: null,
                ct: ct,
                queryFromObject: peticion);
        }

        public Task<RespuestaApi<RespuestaDownloadDocumentXml>> DescargarXmlDocumentoAsync(PeticionDownloadDocumentXml peticion, CancellationToken ct)
        {
            return EjecutarPeticionAsync<RespuestaDownloadDocumentXml>(
                endpoint: "File/DownloadDocumentXml",
                metodo: HttpMethod.Get,
                body: null,
                ct: ct,
                queryFromObject: peticion);
        }

        public Task<RespuestaApi<object>> ObtenerDocumentosRecibidosAsync(object peticion, CancellationToken ct)
        {
            return EjecutarPeticionGenericaAsync("Document/GetDocumentsReceived", HttpMethod.Post, peticion, ct);
        }

        public Task<RespuestaApi<object>> ConfirmarDocumentosRecibidosAsync(object peticion, CancellationToken ct)
        {
            return EjecutarPeticionGenericaAsync("Document/ConfirmDocumentsReceived", HttpMethod.Post, peticion, ct);
        }

        public Task<RespuestaApi<object>> ConsultarEventosDocumentoAsync(object peticion, CancellationToken ct)
        {
            return EjecutarPeticionGenericaAsync("Document/GetDocumentEvents", HttpMethod.Post, peticion, ct);
        }

        public Task<RespuestaApi<object>> CambiarEstadoDocumentoAsync(object peticion, CancellationToken ct)
        {
            return EjecutarPeticionGenericaAsync("Document/ChangeDocumentStatus", HttpMethod.Post, peticion, ct);
        }

        private async Task<RespuestaApi<object>> EjecutarPeticionGenericaAsync(
            string endpoint,
            HttpMethod metodo,
            object? body,
            CancellationToken ct)
        {
            var resultado = await EjecutarPeticionAsync<Dictionary<string, object>>(
                endpoint: endpoint,
                metodo: metodo,
                body: body,
                ct: ct);

            if (!resultado.Exitoso)
                return RespuestaApi<object>.CrearFallido(resultado.MensajeError, resultado.CodigoError);

            return RespuestaApi<object>.CrearExitoso((object)resultado.Datos!);
        }

        private async Task<RespuestaApi<T>> EjecutarPeticionAsync<T>(
            string endpoint,
            HttpMethod metodo,
            object? body,
            CancellationToken ct,
            object? queryFromObject = null)
            where T : class
        {
            try
            {
                var requestUrl = endpoint;

                if (metodo == HttpMethod.Get && queryFromObject != null)
                    requestUrl = endpoint + ConstruirQueryString(queryFromObject);

                using var request = new HttpRequestMessage(metodo, requestUrl);

                if (metodo != HttpMethod.Get && body != null)
                {
                    var json = JsonSerializer.Serialize(body, _jsonOptions);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }

                using var response = await _httpClient.SendAsync(request, ct);
                var raw = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    // Intentar parsear error estándar GoSocket
                    var error = TryParse<RespuestaError>(raw);               

                    var mensaje =
                        error != null && (!string.IsNullOrWhiteSpace(error.Error) || !string.IsNullOrWhiteSpace(error.ErrorDescription))
                            ? $"{error.Error}: {error.ErrorDescription}".Trim().Trim(':')
                            : $"HTTP {(int)response.StatusCode} - {response.ReasonPhrase}";

                    return RespuestaApi<T>.CrearFallido(mensaje, ((int)response.StatusCode).ToString());


                    return RespuestaApi<T>.CrearFallido(mensaje, ((int)response.StatusCode).ToString());
                }

                var dto = TryParse<T>(raw);
                if (dto == null)
                    return RespuestaApi<T>.CrearFallido("No se pudo deserializar respuesta GoSocket.", "DESERIALIZE_ERROR");

                return RespuestaApi<T>.CrearExitoso(dto);
            }
            catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Timeout consumiendo GoSocket en endpoint {Endpoint}", endpoint);
                return RespuestaApi<T>.CrearFallido("Timeout consumiendo GoSocket.", "TIMEOUT");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consumiendo GoSocket en endpoint {Endpoint}", endpoint);
                throw new GoSocketApiException("Error consumiendo GoSocket.", ex);
            }
        }

        private string ConstruirQueryString(object obj)
        {
            // Construcción simple: propiedades públicas => ?a=1&b=2
            var props = obj.GetType().GetProperties();
            var pairs = new List<string>();

            foreach (var p in props)
            {
                var value = p.GetValue(obj);
                if (value == null) continue;

                pairs.Add($"{Uri.EscapeDataString(p.Name)}={Uri.EscapeDataString(value.ToString()!)}");
            }

            return pairs.Count == 0 ? "" : "?" + string.Join("&", pairs);
        }

        private T? TryParse<T>(string raw) where T : class
        {
            try { return JsonSerializer.Deserialize<T>(raw, _jsonOptions); }
            catch { return null; }
        }
    }
}
