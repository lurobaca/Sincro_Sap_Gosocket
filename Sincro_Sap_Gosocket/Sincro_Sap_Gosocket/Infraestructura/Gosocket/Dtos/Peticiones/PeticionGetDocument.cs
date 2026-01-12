// Sincro_Sap_Gosocket/Infraestructura/Gosocket/Dtos/Peticiones/PeticionGetDocument.cs
using Newtonsoft.Json;

namespace Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Peticiones
{
    /// <summary>
    /// Petición para consultar estado y metadata de un documento
    /// Manual v10 - Página 223 (GETDOCUMENT)
    /// </summary>
    public class PeticionGetDocument
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
        /// Tipo de documento (33: Factura, 34: Factura Exenta, etc.)
        /// </summary>
        [JsonProperty("documentType", NullValueHandling = NullValueHandling.Ignore)]
        public string TipoDocumento { get; set; } = string.Empty;

        /// <summary>
        /// Folio del documento (opcional)
        /// </summary>
        [JsonProperty("folio", NullValueHandling = NullValueHandling.Ignore)]
        public int? Folio { get; set; }
    }
}