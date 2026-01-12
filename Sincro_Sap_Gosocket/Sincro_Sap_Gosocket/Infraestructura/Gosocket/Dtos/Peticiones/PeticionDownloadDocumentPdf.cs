// Sincro_Sap_Gosocket/Infraestructura/Gosocket/Dtos/Peticiones/PeticionDownloadDocumentPdf.cs
using Newtonsoft.Json;

namespace Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Peticiones
{
    /// <summary>
    /// Petición para descargar PDF de un documento
    /// Manual v10 - Página 250 (DOWNLOADDOCUMENTPDF)
    /// </summary>
    public class PeticionDownloadDocumentPdf
    {
        /// <summary>
        /// Código único del documento en GoSocket
        /// </summary>
        [JsonProperty("documentCode")]
        public string CodigoDocumento { get; set; } = string.Empty;

        /// <summary>
        /// Código del país (ISO 3166-1 alpha-2)
        /// </summary>
        [JsonProperty("countryCode", NullValueHandling = NullValueHandling.Ignore)]
        public string CodigoPais { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de representación PDF:
        /// - "standard": PDF estándar
        /// - "ticket": Formato ticket (depende del país)
        /// </summary>
        [JsonProperty("format", NullValueHandling = NullValueHandling.Ignore)]
        public string Formato { get; set; } = "standard";

        /// <summary>
        /// Idioma del PDF (es, en, pt, etc.)
        /// </summary>
        [JsonProperty("language", NullValueHandling = NullValueHandling.Ignore)]
        public string Idioma { get; set; } = "es";
    }
}