using System.Threading;
using System.Threading.Tasks;

namespace Sincro_Sap_Gosocket.Aplicacion.Interfaces
{
    public interface IClienteGosocket
    {
        /// <summary>
        /// Envía el XML (ya traducido a GoSocket/xDoc) hacia GoSocket para enviarlo a la Autoridad.
        /// </summary>
        Task<GosocketSendResult> EnviarAsync(string xmlUtf8, CancellationToken ct);

        /// <summary>
        /// Consulta el estado/detalle de un documento en GoSocket por su identificador (trackId / documentId).
        /// </summary>
        Task<GosocketGetDocumentResult> GetDocumentoAsync(string documentId, CancellationToken ct);

        /// <summary>
        /// Descarga el XML almacenado en GoSocket.
        /// </summary>
        Task<GosocketFileResult> DescargarXmlAsync(string documentId, CancellationToken ct);

        /// <summary>
        /// Descarga el PDF almacenado en GoSocket.
        /// </summary>
        Task<GosocketFileResult> DescargarPdfAsync(string documentId, CancellationToken ct);

        /// <summary>
        /// Cambia el estado de un documento en GoSocket (si tu integración lo requiere).
        /// </summary>
        Task<GosocketChangeStatusResult> CambiarEstadoAsync(string documentId, string newStatus, CancellationToken ct);
    }

    // ===== DTOs mínimos (puedes moverlos a Infraestructura/Gosocket/ModelosGosocket.cs) =====

    public sealed class GosocketSendResult
    {
        public bool Ok { get; set; }
        public int HttpStatus { get; set; }
        public string? DocumentId { get; set; }     // trackId / id retornado por GoSocket
        public string? Raw { get; set; }            // respuesta cruda (por trazabilidad)
        public string? Error { get; set; }
    }

    public sealed class GosocketGetDocumentResult
    {
        public bool Ok { get; set; }
        public int HttpStatus { get; set; }
        public string? Raw { get; set; }
        public string? Error { get; set; }
    }

    public sealed class GosocketFileResult
    {
        public bool Ok { get; set; }
        public int HttpStatus { get; set; }
        public byte[]? Content { get; set; }
        public string? ContentType { get; set; }
        public string? FileName { get; set; }
        public string? Error { get; set; }
    }

    public sealed class GosocketChangeStatusResult
    {
        public bool Ok { get; set; }
        public int HttpStatus { get; set; }
        public string? Raw { get; set; }
        public string? Error { get; set; }
    }
}
