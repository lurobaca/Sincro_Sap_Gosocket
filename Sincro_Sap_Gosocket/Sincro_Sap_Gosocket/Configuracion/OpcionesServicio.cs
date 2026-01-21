// Sincro_Sap_Gosocket/Configuracion/OpcionesServicio.cs
namespace Sincro_Sap_Gosocket.Configuracion
{
    /// <summary>
    /// Opciones de ejecución del servicio (Worker).
    /// </summary>
    public class OpcionesServicio
    {
        /// <summary>
        /// Intervalo del ciclo del Worker (segundos).
        /// </summary>
        public int PollSeconds { get; set; } = 5;

        /// <summary>
        /// Cantidad máxima de documentos a procesar por ciclo (envío y seguimiento).
        /// </summary>
        public int BatchSize { get; set; } = 10;
    }
}
