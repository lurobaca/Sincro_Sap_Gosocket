using System;
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
using Sincro_Sap_Gosocket.Options;

namespace Sincro_Sap_Gosocket.Infraestructura.Gosocket
{
    public sealed class ClienteGosocket : IClienteGosocket
    {
        private readonly HttpClient _http;
        private readonly ILogger<ClienteGosocket> _logger;
        private readonly OpcionesGosocket _opt;

        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        public ClienteGosocket(
            HttpClient http,
            IOptions<OpcionesGosocket> opt,
            ILogger<ClienteGosocket> logger)
        {
            _http = http;
            _logger = logger;
            _opt = opt.Value;

            ConfigurarAuthSiHaceFalta(_http, _opt);
        }

        public async Task<GosocketSendResult> EnviarAsync(string xmlUtf8, CancellationToken ct)
        {
            // Endpoint típico según tu memoria: POST Document/SendDocumentToAuthority
            // BaseUrl debe venir como .../api/v1/
            var url = "Document/SendDocumentToAuthority";

            try
            {
                if (string.IsNullOrWhiteSpace(xmlUtf8))
                    throw new InvalidOperationException("El XML a enviar viene vacío.");

                // En muchos integradores GoSocket se envía XML en base64 dentro de un JSON.
                // Si tu GoSocket espera otra estructura, cambia SOLO el payload.
                var xmlBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(xmlUtf8));

                var payload = new
                {
                    // Nombres genéricos; ajústalos al contrato real si tu ModelosGosocket.cs ya define uno.
                    // Algunos ejemplos suelen usar: "xml", "file", "xmlBase64", "document"
                    xmlBase64 = xmlBase64
                };

                var json = JsonSerializer.Serialize(payload, JsonOpts);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var resp = await _http.PostAsync(url, content, ct);
                var raw = await resp.Content.ReadAsStringAsync(ct);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("GoSocket EnviarAsync fallo HTTP {Status}. Body: {Body}", (int)resp.StatusCode, raw);
                    return new GosocketSendResult
                    {
                        Ok = false,
                        HttpStatus = (int)resp.StatusCode,
                        Raw = raw,
                        Error = "HTTP error en envío a GoSocket"
                    };
                }

                // Intenta extraer un id si viene; si no, guardamos raw.
                // Si ya tienes un modelo, deserializa a tu modelo.
                string? documentId = TryExtractDocumentId(raw);

                return new GosocketSendResult
                {
                    Ok = true,
                    HttpStatus = (int)resp.StatusCode,
                    Raw = raw,
                    DocumentId = documentId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción en EnviarAsync");
                return new GosocketSendResult
                {
                    Ok = false,
                    HttpStatus = 0,
                    Error = ex.Message
                };
            }
        }

        public async Task<GosocketGetDocumentResult> GetDocumentoAsync(string documentId, CancellationToken ct)
        {
            // Endpoint típico: POST Document/GetDocument
            var url = "Document/GetDocument";

            try
            {
                if (string.IsNullOrWhiteSpace(documentId))
                    throw new InvalidOperationException("documentId vacío.");

                var payload = new { documentId };
                var json = JsonSerializer.Serialize(payload, JsonOpts);

                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var resp = await _http.PostAsync(url, content, ct);
                var raw = await resp.Content.ReadAsStringAsync(ct);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("GoSocket GetDocumentoAsync fallo HTTP {Status}. Body: {Body}", (int)resp.StatusCode, raw);
                    return new GosocketGetDocumentResult
                    {
                        Ok = false,
                        HttpStatus = (int)resp.StatusCode,
                        Raw = raw,
                        Error = "HTTP error consultando documento"
                    };
                }

                return new GosocketGetDocumentResult
                {
                    Ok = true,
                    HttpStatus = (int)resp.StatusCode,
                    Raw = raw
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción en GetDocumentoAsync");
                return new GosocketGetDocumentResult
                {
                    Ok = false,
                    HttpStatus = 0,
                    Error = ex.Message
                };
            }
        }

        public async Task<GosocketFileResult> DescargarXmlAsync(string documentId, CancellationToken ct)
        {
            // Endpoint típico: GET File/DownloadDocumentXml?documentId=...
            var url = $"File/DownloadDocumentXml?documentId={Uri.EscapeDataString(documentId)}";
            return await DescargarArchivoAsync(url, $"{documentId}.xml", ct);
        }

        public async Task<GosocketFileResult> DescargarPdfAsync(string documentId, CancellationToken ct)
        {
            // Endpoint típico: GET File/DownloadDocumentPdf?documentId=...
            var url = $"File/DownloadDocumentPdf?documentId={Uri.EscapeDataString(documentId)}";
            return await DescargarArchivoAsync(url, $"{documentId}.pdf", ct);
        }

        public async Task<GosocketChangeStatusResult> CambiarEstadoAsync(string documentId, string newStatus, CancellationToken ct)
        {
            // Endpoint típico mencionado: ChangeDocumentStatus (puede ser POST)
            var url = "Document/ChangeDocumentStatus";

            try
            {
                if (string.IsNullOrWhiteSpace(documentId))
                    throw new InvalidOperationException("documentId vacío.");
                if (string.IsNullOrWhiteSpace(newStatus))
                    throw new InvalidOperationException("newStatus vacío.");

                var payload = new { documentId, status = newStatus };
                var json = JsonSerializer.Serialize(payload, JsonOpts);

                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var resp = await _http.PostAsync(url, content, ct);
                var raw = await resp.Content.ReadAsStringAsync(ct);

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogError("GoSocket CambiarEstadoAsync fallo HTTP {Status}. Body: {Body}", (int)resp.StatusCode, raw);
                    return new GosocketChangeStatusResult
                    {
                        Ok = false,
                        HttpStatus = (int)resp.StatusCode,
                        Raw = raw,
                        Error = "HTTP error cambiando estado"
                    };
                }

                return new GosocketChangeStatusResult
                {
                    Ok = true,
                    HttpStatus = (int)resp.StatusCode,
                    Raw = raw
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción en CambiarEstadoAsync");
                return new GosocketChangeStatusResult
                {
                    Ok = false,
                    HttpStatus = 0,
                    Error = ex.Message
                };
            }
        }

        // ================== Helpers ==================

        private static void ConfigurarAuthSiHaceFalta(HttpClient http, OpcionesGosocket opt)
        {
            // Si ya lo configuras en Program.cs, esto no estorba.
            // Evita duplicar si ya hay Authorization.
            if (http.DefaultRequestHeaders.Authorization != null)
                return;

            var apiKey = opt.ApiKey ?? "";
            var pass = opt.Password ?? "";

            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{pass}"));
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        }

        private async Task<GosocketFileResult> DescargarArchivoAsync(string relativeUrl, string defaultName, CancellationToken ct)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(relativeUrl))
                    throw new InvalidOperationException("URL vacía.");

                using var resp = await _http.GetAsync(relativeUrl, ct);

                if (!resp.IsSuccessStatusCode)
                {
                    var raw = await resp.Content.ReadAsStringAsync(ct);
                    _logger.LogError("GoSocket DescargarArchivo fallo HTTP {Status}. Body: {Body}", (int)resp.StatusCode, raw);
                    return new GosocketFileResult
                    {
                        Ok = false,
                        HttpStatus = (int)resp.StatusCode,
                        Error = raw
                    };
                }

                var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
                var contentType = resp.Content.Headers.ContentType?.ToString();

                return new GosocketFileResult
                {
                    Ok = true,
                    HttpStatus = (int)resp.StatusCode,
                    Content = bytes,
                    ContentType = contentType,
                    FileName = defaultName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción descargando archivo de GoSocket");
                return new GosocketFileResult
                {
                    Ok = false,
                    HttpStatus = 0,
                    Error = ex.Message
                };
            }
        }

        private static string? TryExtractDocumentId(string raw)
        {
            // Intento “best effort” sin conocer tu modelo real.
            // Si tu API devuelve JSON con { "documentId": "..."} o { "id": "..."} lo captura.
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("documentId", out var documentId) && documentId.ValueKind == JsonValueKind.String)
                        return documentId.GetString();

                    if (root.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                        return id.GetString();

                    if (root.TryGetProperty("trackId", out var trackId) && trackId.ValueKind == JsonValueKind.String)
                        return trackId.GetString();
                }
            }
            catch
            {
                // Ignorar parse fallido; devolvemos null
            }

            return null;
        }
    }
}
