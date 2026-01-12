// Sincro_Sap_Gosocket/Infraestructura/Gosocket/Dtos/Comun/RespuestaError.cs
using Newtonsoft.Json;

namespace Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Comun
{
    /// <summary>
    /// Estructura estándar de error de la API GoSocket
    /// </summary>
    public class RespuestaError
    {
        [JsonProperty("error")]
        public string Error { get; set; } = string.Empty;

        [JsonProperty("error_description")]
        public string ErrorDescription { get; set; } = string.Empty;

        [JsonProperty("error_uri", NullValueHandling = NullValueHandling.Ignore)]
        public string ErrorUri { get; set; } = string.Empty;

        [JsonProperty("trace_id", NullValueHandling = NullValueHandling.Ignore)]
        public string TraceId { get; set; } = string.Empty;
    }
}