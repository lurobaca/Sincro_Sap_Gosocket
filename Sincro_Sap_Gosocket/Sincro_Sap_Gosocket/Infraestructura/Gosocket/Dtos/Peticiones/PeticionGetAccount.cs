// Sincro_Sap_Gosocket/Infraestructura/Gosocket/Dtos/Peticiones/PeticionGetAccount.cs
using Newtonsoft.Json;

namespace Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Peticiones
{
    /// <summary>
    /// Petición para consultar información de una cuenta/contribuyente
    /// Manual v10 - Páginas 266, 272, 281 (GETACCOUNT)
    /// </summary>
    public class PeticionGetAccount
    {
        /// <summary>
        /// Identificador del contribuyente (RUT, NIT, CUI, etc.)
        /// Formato depende del país
        /// </summary>
        [JsonProperty("account")]
        public string Identificador { get; set; } = string.Empty;

        /// <summary>
        /// Código del país para la consulta (ISO 3166-1 alpha-2)
        /// Ejemplos: CL (Chile), CO (Colombia), PE (Perú)
        /// </summary>
        [JsonProperty("countryCode", NullValueHandling = NullValueHandling.Ignore)]
        public string CodigoPais { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de documento de identificación (opcional)
        /// </summary>
        [JsonProperty("documentType", NullValueHandling = NullValueHandling.Ignore)]
        public string TipoDocumento { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de consulta: "nit", "cui", "rut" (opcional)
        /// </summary>
        [JsonProperty("queryType", NullValueHandling = NullValueHandling.Ignore)]
        public string TipoConsulta { get; set; } = string.Empty;
    }
}