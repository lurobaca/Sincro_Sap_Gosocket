using Sincro_Sap_Gosocket.Dominio.Enumeraciones;

namespace Sincro_Sap_Gosocket.Dominio.Entidades
{
    public sealed class ActualizacionEstadoHacienda
    { 
        public TipoDocumentoSap TipoDocumento { get; set; }
        public int DocEntry { get; set; }

        public string EstadoHacienda { get; set; } = string.Empty;
        public string MensajeHacienda { get; set; } = string.Empty;
        public string? Clave { get; set; }
        public string? FechaRespuestaTexto { get; set; }

        public string? CampoEstado { get; set; } = "U_EstadoHacienda";
        public string? CampoMensaje { get; set; } = "U_RespuestaHacienda";
        public string? CampoClave { get; set; } = "U_ClaveHacienda";
        public string? CampoFechaRespuesta { get; set; } = "U_FechaRespHacienda";
    }
}