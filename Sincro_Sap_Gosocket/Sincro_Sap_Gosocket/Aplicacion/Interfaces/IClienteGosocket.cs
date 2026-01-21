// Aplicacion/Interfaces/IClienteGosocket.cs
using System.Threading;
using System.Threading.Tasks;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Comun;
using Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Peticiones;

namespace Sincro_Sap_Gosocket.Aplicacion.Interfaces
{
    public interface IClienteGosocket
    {
        Task<RespuestaApi<Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Respuestas.RespuestaSendDocumentToAuthority>> EnviarDocumentoAutoridadAsync(
            PeticionSendDocumentToAuthority peticion,
            CancellationToken ct);

        Task<RespuestaApi<Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Respuestas.RespuestaGetAccount>> ConsultarCuentaAsync(
            PeticionGetAccount peticion,
            CancellationToken ct);

        Task<RespuestaApi<Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Respuestas.RespuestaGetDocument>> ObtenerDocumentoAsync(
            PeticionGetDocument peticion,
            CancellationToken ct);

        Task<RespuestaApi<Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Respuestas.RespuestaDownloadDocumentPdf>> DescargarPdfDocumentoAsync(
            PeticionDownloadDocumentPdf peticion,
            CancellationToken ct);

        Task<RespuestaApi<Sincro_Sap_Gosocket.Infraestructura.Gosocket.Dtos.Respuestas.RespuestaDownloadDocumentXml>> DescargarXmlDocumentoAsync(
            PeticionDownloadDocumentXml peticion,
            CancellationToken ct);

        Task<RespuestaApi<object>> ObtenerDocumentosRecibidosAsync(object peticion, CancellationToken ct);
        Task<RespuestaApi<object>> ConfirmarDocumentosRecibidosAsync(object peticion, CancellationToken ct);
        Task<RespuestaApi<object>> ConsultarEventosDocumentoAsync(object peticion, CancellationToken ct);
        Task<RespuestaApi<object>> CambiarEstadoDocumentoAsync(object peticion, CancellationToken ct);
    }
}
