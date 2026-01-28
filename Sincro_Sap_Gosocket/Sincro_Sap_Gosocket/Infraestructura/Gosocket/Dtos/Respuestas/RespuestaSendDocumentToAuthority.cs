// Sincro_Sap_Gosocket/Infraestructura/Gosocket/Dtos/Respuestas/RespuestaSendDocumentToAuthority.cs
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Respuestas
{
    /// <summary>
    /// Respuesta v10 de GoSocket para SendDocumentToAuthority
    /// </summary>
    public class RespuestaSendDocumentToAuthority
    {
        [JsonProperty("Success")]
        public bool Success { get; set; }

        /// <summary>
        /// Identificador global del documento en GoSocket (GUID).
        /// Cuando hay error, puede venir como 00000000-0000-0000-0000-000000000000
        /// </summary>
        [JsonProperty("GlobalDocumentId")]
        public Guid GlobalDocumentId { get; set; }

        /// <summary>
        /// Identificador del documento para el país (puede venir null).
        /// </summary>
        [JsonProperty("CountryDocumentId")]
        public string? CountryDocumentId { get; set; }

        [JsonProperty("OtherData")]
        public OtherDataDto? OtherData { get; set; }

        /// <summary>
        /// Lista de mensajes (errores/observaciones).
        /// </summary>
        [JsonProperty("Messages")]
        public List<string>? Messages { get; set; }

        /// <summary>
        /// Algunos ambientes/versiones pueden devolver un objeto adicional aquí.
        /// Déjelo como object para no romper deserialización.
        /// </summary>
        [JsonProperty("ResponseValue")]
        public object? ResponseValue { get; set; }

        /// <summary>
        /// Código (ej: "401", "500", etc.)
        /// </summary>
        [JsonProperty("Code")]
        public string? Code { get; set; }

        [JsonProperty("Description")]
        public string? Description { get; set; }

        [JsonProperty("ErrorException")]
        public object? ErrorException { get; set; }
    }

    public class OtherDataDto
    {
        [JsonProperty("Country")]
        public string? Country { get; set; }

        [JsonProperty("Certifier")]
        public string? Certifier { get; set; }
    }
}
