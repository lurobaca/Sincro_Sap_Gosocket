
using System;
using System.Text.Json.Serialization;

namespace Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Peticiones
{
    /// <summary>
    /// Petición para consultar estado y metadata de un documento.
    /// </summary>
    public class PeticionGetDocument
    {
        [JsonPropertyName("Country")]
        public string Country { get; set; } = "CR";

        [JsonPropertyName("globalDocumentId")]
        public string GlobalDocumentId { get; set; } = string.Empty;

        [JsonPropertyName("SenderCode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SenderCode { get; set; }

        [JsonPropertyName("DateFrom")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? DateFrom { get; set; }

        [JsonPropertyName("DateTo")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public DateTime? DateTo { get; set; }
    }
}
