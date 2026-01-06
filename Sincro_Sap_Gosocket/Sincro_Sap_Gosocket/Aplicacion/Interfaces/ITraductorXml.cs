namespace Sincro_Sap_Gosocket.Aplicacion.Interfaces
{
    public interface ITraductorXml
    {
        /// <summary>
        /// Traduce la data obtenida (FE44/SPs) a XML destino (GoSocket / xDoc).
        /// </summary>
        /// <param name="tipo">FE, NC, ND, FEC</param>
        /// <param name="datos">Resultado del SP (DataSet/DataTable/DTO)</param>
        /// <returns>XML listo para enviar</returns>
        string Traducir(string tipo, object datos);
    }
}
