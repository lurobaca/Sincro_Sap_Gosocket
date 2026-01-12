// Sincro_Sap_Gosocket/Infraestructura/Gosocket/Dtos/Respuestas/RespuestaGetDocument.cs
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Respuestas
{
    /// <summary>
    /// Respuesta de consulta de documento
    /// </summary>
    public class RespuestaGetDocument
    {
        /// <summary>
        /// Código único del documento en GoSocket
        /// </summary>
        [JsonProperty("documentCode")]
        public string CodigoDocumento { get; set; } = string.Empty;

        /// <summary>
        /// Estado del documento en GoSocket
        /// </summary>
        [JsonProperty("status")]
        public string Estado { get; set; } = string.Empty;

        /// <summary>
        /// Estado del documento en la autoridad tributaria
        /// </summary>
        [JsonProperty("authorityStatus")]
        public string EstadoAutoridad { get; set; } = string.Empty;

        /// <summary>
        /// Código de trackId para seguimiento
        /// </summary>
        [JsonProperty("trackId", NullValueHandling = NullValueHandling.Ignore)]
        public string TrackId { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de documento (33, 34, 61, etc.)
        /// </summary>
        [JsonProperty("documentType")]
        public string TipoDocumento { get; set; } = string.Empty;

        /// <summary>
        /// Folio asignado al documento
        /// </summary>
        [JsonProperty("folio", NullValueHandling = NullValueHandling.Ignore)]
        public int? Folio { get; set; }

        /// <summary>
        /// Fecha de emisión del documento
        /// </summary>
        [JsonProperty("issueDate", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? FechaEmision { get; set; }

        /// <summary>
        /// RUT/NIT/CUI del emisor
        /// </summary>
        [JsonProperty("sender", NullValueHandling = NullValueHandling.Ignore)]
        public string Remitente { get; set; } = string.Empty;

        /// <summary>
        /// RUT/NIT/CUI del receptor
        /// </summary>
        [JsonProperty("receiver", NullValueHandling = NullValueHandling.Ignore)]
        public string Receptor { get; set; } = string.Empty;

        /// <summary>
        /// Monto total del documento
        /// </summary>
        [JsonProperty("totalAmount", NullValueHandling = NullValueHandling.Ignore)]
        public decimal? MontoTotal { get; set; }

        /// <summary>
        /// Metadatos adicionales del documento
        /// </summary>
        [JsonProperty("metadata")]
        public Dictionary<string, object> Metadatos { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Fecha de última actualización del estado
        /// </summary>
        [JsonProperty("lastStatusUpdate", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? UltimaActualizacionEstado { get; set; }
    }
}