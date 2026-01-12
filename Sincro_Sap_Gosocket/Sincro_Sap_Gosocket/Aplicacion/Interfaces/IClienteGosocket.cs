// Sincro_Sap_Gosocket/Aplicacion/Interfaces/IClienteGosocket.cs
using System.Threading.Tasks;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Comun;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Peticiones;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Respuestas;

namespace Sincro_Sap_Gosocket.Aplicacion.Interfaces
{
    /// <summary>
    /// Cliente principal para interactuar con la API de GoSocket
    /// Implementa los métodos descritos en el manual v10
    /// </summary>
    public interface IClienteGosocket
    {
        // ========== MÉTODOS DE CONSULTA ==========

        /// <summary>
        /// Consulta información de una cuenta/contribuyente (GETACCOUNT)
        /// Manual v10 - Páginas 266, 272, 281
        /// </summary>
        Task<RespuestaApi<RespuestaGetAccount>> ConsultarCuentaAsync(PeticionGetAccount peticion);

        /// <summary>
        /// Consulta el estado y metadata de un documento (GETDOCUMENT)
        /// Manual v10 - Página 223
        /// </summary>
        Task<RespuestaApi<RespuestaGetDocument>> ObtenerDocumentoAsync(PeticionGetDocument peticion);

        // ========== MÉTODOS DE ENVÍO ==========

        /// <summary>
        /// Envía un documento a la autoridad tributaria (SENDDOCUMENTTOAUTHORITY)
        /// Manual v10 - Página 19
        /// </summary>
        Task<RespuestaApi<RespuestaSendDocumentToAuthority>> EnviarDocumentoAutoridadAsync(
            PeticionSendDocumentToAuthority peticion);

        /// <summary>
        /// Valida un documento sin enviarlo a la autoridad (SENDDOCUMENTTOVALIDATE)
        /// Manual v10 - Página 118
        /// </summary>
        Task<RespuestaApi<RespuestaSendDocumentToAuthority>> ValidarDocumentoAsync(
            PeticionSendDocumentToAuthority peticion);

        // ========== MÉTODOS DE DESCARGA ==========

        /// <summary>
        /// Descarga el XML de un documento (DOWNLOADDOCUMENTXML)
        /// Manual v10 - Página 238
        /// </summary>
        Task<RespuestaApi<RespuestaDownloadDocumentXml>> DescargarXmlDocumentoAsync(
            PeticionDownloadDocumentXml peticion);

        /// <summary>
        /// Descarga el PDF de un documento (DOWNLOADDOCUMENTPDF)
        /// Manual v10 - Página 250
        /// </summary>
        Task<RespuestaApi<RespuestaDownloadDocumentPdf>> DescargarPdfDocumentoAsync(
            PeticionDownloadDocumentPdf peticion);

        // ========== MÉTODOS DE DOCUMENTOS RECIBIDOS ==========

        /// <summary>
        /// Obtiene la lista de documentos recibidos (GETRECEIVEDDOCUMENT)
        /// Manual v10 - Página 131
        /// </summary>
        Task<RespuestaApi<object>> ObtenerDocumentosRecibidosAsync(object peticion);

        /// <summary>
        /// Marca documentos como recibidos (CONFIRMRECEIVEDDOCUMENT)
        /// Manual v10 - Página 137
        /// </summary>
        Task<RespuestaApi<object>> ConfirmarDocumentosRecibidosAsync(object peticion);

        // ========== MÉTODOS DE EVENTOS ==========

        /// <summary>
        /// Consulta eventos del documento ante la entidad tributaria (DOCUMENTGETEVENTS)
        /// Manual v10 - Página 142
        /// </summary>
        Task<RespuestaApi<object>> ConsultarEventosDocumentoAsync(object peticion);

        /// <summary>
        /// Informa eventos en los documentos (CHANGEDOCUMENTSTATUS)
        /// Manual v10 - Página 150
        /// </summary>
        Task<RespuestaApi<object>> CambiarEstadoDocumentoAsync(object peticion);
    }
}