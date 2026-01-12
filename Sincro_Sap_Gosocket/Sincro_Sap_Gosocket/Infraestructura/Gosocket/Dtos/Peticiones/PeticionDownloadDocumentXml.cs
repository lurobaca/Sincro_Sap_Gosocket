// Sincro_Sap_Gosocket/Infraestructura/Gosocket/Dtos/Peticiones/PeticionDownloadDocumentXml.cs
using Newtonsoft.Json;

namespace Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Peticiones
{
    /// <summary>
    /// Petición para descargar XML de un documento
    /// Manual v10 - Página 238 (DOWNLOADDOCUMENTXML)
    /// </summary>
    public class PeticionDownloadDocumentXml
    {
        /// <summary>
        /// Código único del documento en GoSocket
        /// </summary>
        [JsonProperty("documentCode")]
        public string CodigoDocumento { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de XML a descargar:
        /// - "default": XML generado por GoSocket
        /// - "distribution": XML de distribución
        /// - "original": XML original de la entidad tributaria
        /// </summary>
        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Tipo { get; set; } = "default";

        /// <summary>
        /// Código del país (ISO 3166-1 alpha-2)
        /// </summary>
        [JsonProperty("countryCode", NullValueHandling = NullValueHandling.Ignore)]
        public string CodigoPais { get; set; } = string.Empty;

        /// <summary>
        /// Indica si se debe incluir la firma digital (depende del país)
        /// </summary>
        [JsonProperty("includeSignature", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IncluirFirma { get; set; }
    }
}
