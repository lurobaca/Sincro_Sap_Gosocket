// Sincro_Sap_Gosocket/Infraestructura/Gosocket/Dtos/Respuestas/RespuestaDownloadDocumentXml.cs
using Newtonsoft.Json;

namespace Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Respuestas
{
    /// <summary>
    /// Respuesta de descarga de XML
    /// </summary>
    public class RespuestaDownloadDocumentXml
    {
        /// <summary>
        /// Código único del documento
        /// </summary>
        [JsonProperty("documentCode")]
        public string CodigoDocumento { get; set; } = string.Empty;

        /// <summary>
        /// XML del documento en formato string
        /// </summary>
        [JsonProperty("xml")]
        public string Xml { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de XML descargado (default, distribution, original)
        /// </summary>
        [JsonProperty("type")]
        public string Tipo { get; set; } = string.Empty;

        /// <summary>
        /// Nombre sugerido para el archivo
        /// </summary>
        [JsonProperty("fileName", NullValueHandling = NullValueHandling.Ignore)]
        public string NombreArchivo { get; set; } = string.Empty;

        /// <summary>
        /// Tamaño del XML en bytes
        /// </summary>
        [JsonProperty("fileSize", NullValueHandling = NullValueHandling.Ignore)]
        public int? TamanoArchivo { get; set; }

        /// <summary>
        /// Hash MD5 del contenido (para verificación)
        /// </summary>
        [JsonProperty("md5Hash", NullValueHandling = NullValueHandling.Ignore)]
        public string HashMd5 { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de generación del XML
        /// </summary>
        [JsonProperty("generationDate", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? FechaGeneracion { get; set; }
    }
}