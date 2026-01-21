// Dominio/Entidades/DocumentoCola.cs
using System;

namespace Sincro_Sap_Gosocket.Dominio.Entidades
{
    /// <summary>
    /// Representa un registro de la cola Integration.DocumentosPendientes.
    /// Se usa tanto para envío como para seguimiento Hacienda vía GoSocket.
    /// </summary>
    public class DocumentoCola
    {
        public int DocumentosPendientes_Id { get; set; }

        public string? SourceSystem { get; set; }
        public string? TipoCE { get; set; }
        public int ObjType { get; set; }

        public int DocEntry { get; set; }
        public int DocNum { get; set; }

        public string? DocSubType { get; set; }
        public string? DocType { get; set; }

        public string? CardCode { get; set; }
        public DateTime TaxDate { get; set; }
        public DateTime CreateDateTime { get; set; }

        public string Status { get; set; } = "PENDING";

        public int AttemptCount { get; set; }
        public DateTime? NextAttemptAt { get; set; }
        public DateTime? LastAttemptAt { get; set; }

        public string? LockedBy { get; set; }
        public DateTime? LockedAt { get; set; }

        public string? LastError { get; set; }

        // XML generado
        public string? XmlFilePath { get; set; }

        // Respuesta de GoSocket al enviar
        public string? GoSocket_TrackId { get; set; }
        public int? GoSocket_HttpStatus { get; set; }
        public string? GoSocket_ResponseJson { get; set; }

        // Respuesta de Hacienda (obtenida consultando a GoSocket)
        public string? Hacienda_Estado { get; set; }
        public string? Hacienda_ResponseJson { get; set; }
    }
}
