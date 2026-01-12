// Sincro_Sap_Gosocket/Infraestructura/Gosocket/Dtos/Respuestas/RespuestaDownloadDocumentPdf.cs
using Newtonsoft.Json;

namespace Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Respuestas
{
    /// <summary>
    /// Respuesta de descarga de PDF
    /// </summary>
    public class RespuestaDownloadDocumentPdf
    {
        /// <summary>
        /// Código único del documento
        /// </summary>
        [JsonProperty("documentCode")]
        public string CodigoDocumento { get; set; } = string.Empty;

        /// <summary>
        /// PDF del documento en base64
        /// </summary>
        [JsonProperty("pdf")]
        public string PdfBase64 { get; set; } = string.Empty;

        /// <summary>
        /// Nombre sugerido para el archivo
        /// </summary>
        [JsonProperty("fileName")]
        public string NombreArchivo { get; set; } = string.Empty;

        /// <summary>
        /// Tamaño del PDF en bytes
        /// </summary>
        [JsonProperty("fileSize", NullValueHandling = NullValueHandling.Ignore)]
        public int? TamanoArchivo { get; set; }

        /// <summary>
        /// Formato del PDF (standard, ticket, etc.)
        /// </summary>
        [JsonProperty("format", NullValueHandling = NullValueHandling.Ignore)]
        public string Formato { get; set; } = string.Empty;

        /// <summary>
        /// Idioma del PDF
        /// </summary>
        [JsonProperty("language", NullValueHandling = NullValueHandling.Ignore)]
        public string Idioma { get; set; } = string.Empty;

        /// <summary>
        /// Hash MD5 del contenido (para verificación)
        /// </summary>
        [JsonProperty("md5Hash", NullValueHandling = NullValueHandling.Ignore)]
        public string HashMd5 { get; set; } = string.Empty;
    }
}