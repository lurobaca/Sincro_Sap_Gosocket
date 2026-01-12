// Sincro_Sap_Gosocket/Infraestructura/Gosocket/Dtos/Respuestas/RespuestaGetAccount.cs
using Newtonsoft.Json;

namespace Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Respuestas
{
    /// <summary>
    /// Respuesta de consulta de cuenta/contribuyente
    /// </summary>
    public class RespuestaGetAccount
    {
        /// <summary>
        /// Identificador del contribuyente consultado
        /// </summary>
        [JsonProperty("account")]
        public string Identificador { get; set; } = string.Empty;

        /// <summary>
        /// Nombre o razón social del contribuyente
        /// </summary>
        [JsonProperty("name")]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Indica si el contribuyente está activo
        /// </summary>
        [JsonProperty("active")]
        public bool Activo { get; set; }

        /// <summary>
        /// Código del país del contribuyente
        /// </summary>
        [JsonProperty("countryCode")]
        public string CodigoPais { get; set; } = string.Empty;

        /// <summary>
        /// Dirección del contribuyente
        /// </summary>
        [JsonProperty("address", NullValueHandling = NullValueHandling.Ignore)]
        public string Direccion { get; set; } = string.Empty;

        /// <summary>
        /// Ciudad del contribuyente
        /// </summary>
        [JsonProperty("city", NullValueHandling = NullValueHandling.Ignore)]
        public string Ciudad { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de contribuyente (natural, jurídica, etc.)
        /// </summary>
        [JsonProperty("taxpayerType", NullValueHandling = NullValueHandling.Ignore)]
        public string TipoContribuyente { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de última actualización de los datos
        /// </summary>
        [JsonProperty("lastUpdate", NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? UltimaActualizacion { get; set; }

        /// <summary>
        /// Mensaje adicional de la consulta
        /// </summary>
        [JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
        public string Mensaje { get; set; } = string.Empty;
    }
}