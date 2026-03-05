using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Comun
{
public class GosocketRespuesta
    {
        [JsonPropertyName("Exitoso")]
        public bool Exitoso { get; set; }

        [JsonPropertyName("Datos")]
        public GosocketDatos? Datos { get; set; }

        [JsonPropertyName("MensajeError")]
        public string? MensajeError { get; set; }

        [JsonPropertyName("CodigoError")]
        public string? CodigoError { get; set; }

        [JsonPropertyName("FechaRespuesta")]
        public DateTime FechaRespuesta { get; set; }
    }

    public class GosocketDatos
    {
        [JsonPropertyName("Success")]
        public bool Success { get; set; }

        [JsonPropertyName("GlobalDocumentId")]
        public string? GlobalDocumentId { get; set; }

        [JsonPropertyName("CountryDocumentId")]
        public string? CountryDocumentId { get; set; }

        [JsonPropertyName("OtherData")]
        public GosocketOtherData? OtherData { get; set; }

        [JsonPropertyName("Messages")]
        public List<string> Messages { get; set; } = new();

        // A veces viene null, a veces puede venir objeto/string según endpoint.
        // Si usted sabe el tipo real, cámbielo por el tipo específico.
        [JsonPropertyName("ResponseValue")]
        public JsonElement? ResponseValue { get; set; }

        [JsonPropertyName("Code")]
        public string? Code { get; set; }

        [JsonPropertyName("Description")]
        public string? Description { get; set; }

        [JsonPropertyName("ErrorException")]
        public JsonElement? ErrorException { get; set; }
    }

    public class GosocketOtherData
    {
        [JsonPropertyName("Country")]
        public string? Country { get; set; }

        [JsonPropertyName("Certifier")]
        public string? Certifier { get; set; }
    }

    // Ejemplo de uso:
    public static class GosocketParser
    {
        public static GosocketRespuesta Parse(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false
            };

            return JsonSerializer.Deserialize<GosocketRespuesta>(json, options)
                   ?? throw new InvalidOperationException("No se pudo deserializar el JSON.");
        }
    }
}

