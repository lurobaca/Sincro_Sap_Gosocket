// Sincro_Sap_Gosocket/Infraestructura/Gosocket/Dtos/Respuestas/RespuestaSendDocumentToAuthority.cs
using Newtonsoft.Json;

namespace Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Respuestas
{
    /// <summary>
    /// Respuesta de envío o validación de documento
    /// </summary>
    public class RespuestaSendDocumentToAuthority
    {
        /// <summary>
        /// Código único de track para seguimiento del proceso
        /// </summary>
        [JsonProperty("trackId")]
        public string TrackId { get; set; } = string.Empty;

        /// <summary>
        /// Estado del proceso (PENDING, PROCESSING, SUCCESS, ERROR, etc.)
        /// </summary>
        [JsonProperty("status")]
        public string Estado { get; set; } = string.Empty;

        /// <summary>
        /// Mensaje descriptivo del estado
        /// </summary>
        [JsonProperty("message")]
        public string Mensaje { get; set; } = string.Empty;

        /// <summary>
        /// Código del documento generado (cuando es exitoso)
        /// </summary>
        [JsonProperty("documentCode", NullValueHandling = NullValueHandling.Ignore)]
        public string CodigoDocumento { get; set; } = string.Empty;

        /// <summary>
        /// Estado en la autoridad tributaria (cuando es síncrono)
        /// </summary>
        [JsonProperty("authorityStatus", NullValueHandling = NullValueHandling.Ignore)]
        public string EstadoAutoridad { get; set; } = string.Empty;

        /// <summary>
        /// Folio asignado al documento
        /// </summary>
        [JsonProperty("folio", NullValueHandling = NullValueHandling.Ignore)]
        public int? Folio { get; set; }

        /// <summary>
        /// Fecha de recepción del documento
        /// </summary>
        [JsonProperty("receivedDate", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? FechaRecepcion { get; set; }

        /// <summary>
        /// Código de respuesta específico de la autoridad
        /// </summary>
        [JsonProperty("authorityResponseCode", NullValueHandling = NullValueHandling.Ignore)]
        public string CodigoRespuestaAutoridad { get; set; } = string.Empty;

        /// <summary>
        /// Mensaje de respuesta de la autoridad
        /// </summary>
        [JsonProperty("authorityResponseMessage", NullValueHandling = NullValueHandling.Ignore)]
        public string MensajeRespuestaAutoridad { get; set; } = string.Empty;
    }
}
