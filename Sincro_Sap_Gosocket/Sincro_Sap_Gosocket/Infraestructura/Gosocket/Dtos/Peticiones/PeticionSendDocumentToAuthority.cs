// Sincro_Sap_Gosocket/Infraestructura/Gosocket/Dtos/Peticiones/PeticionSendDocumentToAuthority.cs
using Newtonsoft.Json;

namespace Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Peticiones
{
    /// <summary>
    /// Petición para enviar o validar un documento
    /// Manual v10 - Página 19 (SENDDOCUMENTTOAUTHORITY)
    /// Manual v10 - Página 118 (SENDDOCUMENTTOVALIDATE)
    /// </summary>
    public class PeticionSendDocumentToAuthority
    {
        ///// <summary>
        ///// XML del documento tributario en formato string
        ///// </summary>
        //[JsonProperty("document")]
        //public string DocumentoXml { get; set; } = string.Empty;

        ///// <summary>
        ///// Tipo de documento según schema local
        ///// </summary>
        //[JsonProperty("documentType")]
        //public string TipoDocumento { get; set; } = string.Empty;

        ///// <summary>
        ///// Código del país (ISO 3166-1 alpha-2)
        ///// </summary>
        //[JsonProperty("countryCode")]
        //public string CodigoPais { get; set; } = string.Empty;

        ///// <summary>
        ///// Indica si el proceso es asíncrono
        ///// true: Retorna inmediatamente con trackId
        ///// false: Espera respuesta completa de la autoridad
        ///// </summary>
        //[JsonProperty("async")]
        //public bool Asincrono { get; set; } = false;

        ///// <summary>
        ///// Folio asignado al documento (opcional)
        ///// </summary>
        //[JsonProperty("folio", NullValueHandling = NullValueHandling.Ignore)]
        //public int? Folio { get; set; }

        ///// <summary>
        ///// RUT/NIT/CUI del emisor (opcional)
        ///// </summary>
        //[JsonProperty("sender", NullValueHandling = NullValueHandling.Ignore)]
        //public string Remitente { get; set; } = string.Empty;

        ///// <summary>
        ///// RUT/NIT/CUI del receptor (opcional)
        ///// </summary>
        //[JsonProperty("receiver", NullValueHandling = NullValueHandling.Ignore)]
        //public string Receptor { get; set; } = string.Empty;

        ///// <summary>
        ///// Firma digital del documento (opcional, depende del país)
        ///// </summary>
        //[JsonProperty("signature", NullValueHandling = NullValueHandling.Ignore)]
        //public string FirmaDigital { get; set; } = string.Empty;

        /// <summary>
        /// Contenido del archivo XML en string (o base64, según la plataforma).
        /// Requerido.
        /// </summary>
        [JsonProperty("FileContent", Required = Required.Always)]
        public string FileContent { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de respuesta de la API.
        /// Según su documentación:
        /// - true  => Síncrono
        /// - false => Asíncrono
        /// Requerido.
        /// </summary>
        [JsonProperty("Async", Required = Required.Always)]
        public bool Async { get; set; }

        /// <summary>
        /// Código/identificador de mapeo a utilizar para la transformación del documento.
        /// Requerido.
        /// </summary>
        [JsonProperty("Mapping", Required = Required.Always)]
        public string Mapping { get; set; } = string.Empty;

        /// <summary>
        /// Indica si GoSocket debe firmar el XML.
        /// - true  => firma el documento
        /// - false => no firma
        /// Requerido.
        /// </summary>
        [JsonProperty("Sign", Required = Required.Always)]
        public bool Sign { get; set; }

        /// <summary>
        /// Indica si se utilizará el certificado "default" configurado en la plataforma.
        /// - true  => certificado default de la plataforma
        /// - false => certificado configurado en la empresa
        /// Requerido.
        /// </summary>
        [JsonProperty("DefaultCertificate", Required = Required.Always)]
        public bool DefaultCertificate { get; set; }

        ///// <summary>
        ///// Parámetro opcional.
        ///// </summary>
        //[JsonProperty("IgnoreDownWorkload", NullValueHandling = NullValueHandling.Ignore)]
        //public bool? IgnoreDownWorkload { get; set; }

        ///// <summary>
        ///// Indica si el archivo recibido es un TXT.
        ///// - true  => es txt
        ///// - false => no es txt
        ///// Opcional.
        ///// </summary>
        //[JsonProperty("IsTxt", NullValueHandling = NullValueHandling.Ignore)]
        //public bool? IsTxt { get; set; }
    }
}