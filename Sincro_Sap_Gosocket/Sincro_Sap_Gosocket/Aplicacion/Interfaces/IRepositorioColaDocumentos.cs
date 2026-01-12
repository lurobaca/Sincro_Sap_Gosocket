using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sincro_Sap_Gosocket.Aplicacion.Interfaces
{
    public interface IRepositorioColaDocumentos
    {
        Task<IReadOnlyList<DocumentoCola>> ClaimPendientesAsync(int batchSize, string workerId, CancellationToken ct);
    }

    public class DocumentoCola
    {
        public long DocumentosPendientes_Id { get; set; }
        public string ObjType { get; set; }
        public int DocEntry { get; set; }
        public int? DocNum { get; set; }
        public string DocSubType { get; set; }
        public string DocType { get; set; }
        public string CandCode { get; set; }
        public string IsoRule { get; set; }
        public string Status { get; set; }
        public int AttemptCount { get; set; }
        public DateTime? NextAttemptAt { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public DateTime? LockedAt { get; set; }
        public string LockedBy { get; set; }
        public string LastError { get; set; }

        // Propiedades adicionales del documento
        public string TipoCE { get; set; }
        public string SituacionDeComprobante { get; set; }
        public string Remitente { get; set; }
        public string Receptor { get; set; }
        public int? Folio { get; set; }
        public string FirmaDigital { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}