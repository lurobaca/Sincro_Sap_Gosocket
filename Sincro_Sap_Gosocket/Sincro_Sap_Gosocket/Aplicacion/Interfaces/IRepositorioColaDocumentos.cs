using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sincro_Sap_Gosocket.Aplicacion.Interfaces
{
    public interface IRepositorioColaDocumentos
    {
        Task<IReadOnlyList<DocumentoCola>> ClaimPendientesAsync(int batchSize, string workerId, CancellationToken ct);
    }

    public sealed class DocumentoCola
    {
        public long QueueId { get; set; }
        public string ObjType { get; set; } = "";   // en tabla es VARCHAR(10)
        public int DocEntry { get; set; }
        public int? DocNum { get; set; }
        public string? DocSubType { get; set; }
        public string? DocType { get; set; }
        public string? CardCode { get; set; }
        public DateTime? TaxDate { get; set; }
        public string Status { get; set; } = "";
        public int AttemptCount { get; set; }
        public DateTime? NextAttemptAt { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public string? LockedBy { get; set; }
        public DateTime? LockedAt { get; set; }
        public string? LastError { get; set; }

        // Helper para tu switch (13,14,18)
        public int ObjTypeInt => int.TryParse(ObjType, out var n) ? n : 0;
    }
}
