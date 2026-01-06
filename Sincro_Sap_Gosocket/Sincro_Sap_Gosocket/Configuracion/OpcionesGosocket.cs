namespace Sincro_Sap_Gosocket.Configuracion
{
    public sealed class OpcionesGosocket
    {
        public string ApiUrl { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public string Password { get; set; } = "";
        public int TimeoutSeconds { get; set; } = 100;
    }
}
