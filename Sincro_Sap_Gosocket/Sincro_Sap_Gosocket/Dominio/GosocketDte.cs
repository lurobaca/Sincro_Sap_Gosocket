using Sincro_Sap_Gosocket.Dominio.Enums;
using System.Threading;
using System.Xml.Serialization;

namespace Sincro_Sap_Gosocket.Dominio
{
    // =========================
    // ROOT
    // =========================
    [XmlRoot("DTE")]
    public class GosocketDte
    {
      

        [XmlElement("Documento", Order = 1)]
        public GosocketDocumento Documento { get; set; }

        [XmlElement("Personalizados", Order = 2)]
        public GosocketPersonalizados Personalizados { get; set; }

        //public bool ShouldSerializePersonalizados()
        //    => Personalizados != null && Personalizados.HasContent();
    }

    // =========================
    // DOCUMENTO
    // =========================
    public class GosocketDocumento
    {
        [XmlAttribute("ID")]
        public string ID { get; set; }

        [XmlElement("Encabezado", Order = 1)]
        public GosocketEncabezado Encabezado { get; set; }

        [XmlElement("Detalle", Order = 2)]
        public List<GosocketDetalle> Detalle { get; set; } = new();

        public bool ShouldSerializeDetalle()
            => Detalle != null && Detalle.Count > 0;

        [XmlElement("Referencia", Order = 3)]
        public List<GosocketReferencia> Referencia { get; set; } = new();

        public bool ShouldSerializeReferencia()
            => Referencia != null && Referencia.Count > 0;

        [XmlElement("Otros", Order = 4)]
        public GosocketOtros Otros { get; set; }

        public bool ShouldSerializeOtros()
            => Otros != null && Otros.HasContent();
    }

    // =========================
    // ENCABEZADO
    // =========================
    public class GosocketEncabezado
    {
        [XmlElement("IdDoc", Order = 1)]
        public GosocketIdDoc IdDoc { get; set; }

        [XmlElement("Emisor", Order = 2)]
        public GosocketEmisor Emisor { get; set; }

        [XmlElement("Receptor", Order = 3)]
        public GosocketReceptor Receptor { get; set; }

        [XmlElement("Totales", Order = 4)]
        public GosocketTotales Totales { get; set; }

        // /DTE/Documento/Encabezado/Impuestos[*]  (sí: va como "Impuestos" a nivel Encabezado)
        // En la práctica lo estás colgando dentro de Totales, pero el tag debe ser "Impuestos".
        [XmlElement("Impuestos", Order = 5)]
        public List<GosocketImpuestoTotal> Impuestos { get; set; } = new();
        public bool ShouldSerializeImpuestos() => Impuestos != null && Impuestos.Count > 0;

        //// Flex: extras a nivel documento
        //[XmlElement("ExtrInfoDoc", Order = 6)]
        //public List<GosocketExtraInfoDetalle> ExtrInfoDoc { get; set; } = new();

        //public bool ShouldSerializeExtrInfoDoc()
        //    => ExtrInfoDoc != null && ExtrInfoDoc.Count > 0;

           
    }

    // =========================
    // IDDOC
    // =========================
    public class GosocketIdDoc
    {
        //Campos adicionales para Gosocket
        [XmlElement(Order = 1)]
        public string Version { get; set; }
        [XmlElement(Order = 2)]
        public string Ambiente { get; set; }

        //CodigoActividadEmisor
        [XmlElement(Order = 3)]
        public string TipoEmision { get; set; }

        //Clave
        [XmlElement(Order = 4)]
        public string ContenidoTC { get; set; }

        [XmlElement(Order = 5)]
        public string Tipo { get; set; }

        [XmlElement(Order = 6)]
        public string Numero { get; set; }
        [XmlElement(Order = 7)]
        public string NumeroInterno { get; set; }

        // FechaEmision
        [XmlElement(Order = 8)]
        public string FechaEmis { get; set; }

        //NumeroConsecutivo
        [XmlElement(Order = 9)]
        public string Establecimiento { get; set; }

        [XmlElement("Pagos", Order = 10)]
        public List<GosocketPago> Pagos { get; set; } = new();

        //CondicionVenta
        [XmlElement(Order = 11)]
        public string CondPago { get; set; }

        //PlazoCredito
        [XmlElement(Order = 12)]
        public string TermPagoCdg { get; set; }

        //CondicionVentaOtros

        // Flex: extras a nivel documento
        [XmlElement("ExtrInfoDoc", Order = 13)]
        public List<GosocketExtraInfoDetalle> ExtrInfoDoc { get; set; } = new();
          
        public bool ShouldSerializePagos() => Pagos != null && Pagos.Count > 0;
        public bool ShouldSerializeNumeroInterno() => !string.IsNullOrWhiteSpace(NumeroInterno);
        public bool ShouldSerializeContenidoTC() => !string.IsNullOrWhiteSpace(ContenidoTC);

    }

    // =========================
    // EMISOR / RECEPTOR
    // =========================
    public class GosocketEmisor
    {
        //TipoContribuyente/Tipo
        [XmlElement(Order = 1)]
        public string TipoContribuyente { get; set; }

        //TipoContribuyente/Numero
        [XmlElement(Order = 2)]
        public string IDEmisor { get; set; }

        //Nombre
        [XmlElement(Order = 3)]
        public string NmbEmisor { get; set; }

        //NombreComercial
        [XmlElement(Order = 4)]
        public GosocketNombreEmisor NombreEmisor { get; set; }

        //Ubicacion
        [XmlElement("DomFiscal", Order = 5)]
        public GosocketDomFiscal DomFiscal { get; set; }

        [XmlElement("ContactoEmisor", Order = 6)]
        public GosocketContactoEmisor ContactoEmisor { get; set; }
        // Flex: extras de emisor
        [XmlElement("ExtrInfoEmisor", Order = 7)]
        public List<GosocketExtraInfoDetalle> ExtrInfoEmisor { get; set; } = new();

        public bool ShouldSerializeExtrInfoEmisor()
            => ExtrInfoEmisor != null && ExtrInfoEmisor.Count > 0;
     
        public bool ShouldSerializeContactoEmisor()
            => ContactoEmisor != null && ContactoEmisor.HasContent();
    }

    public class GosocketReceptor
    {
        [XmlElement("DocRecep", Order = 1)]
        public GosocketDocRecep DocRecep { get; set; }
        
        [XmlElement(Order = 2)]
        public string NmbRecep { get; set; }

        [XmlElement(Order = 3)]
        public GosocketNombreRecep NombreRecep { get; set; }

        [XmlElement(Order = 4)]
        public string RegimenContableR { get; set; }
      
        [XmlElement("DomFiscalRcp", Order = 5)]
         public GosocketDomFiscal DomFiscalRcp { get; set; }
       
        [XmlElement(Order = 6)]
        public GosocketLugarRecep LugarRecep { get; set; }
        
        [XmlElement("ContactoReceptor", Order = 7)]
        public GosocketContactoReceptor ContactoReceptor { get; set; }

        // Flex: extras receptor (regimen, condición venta otros, etc.)
        [XmlElement("ExtrInfoDoc", Order = 8)]
        public List<GosocketExtraInfoDetalle> ExtrInfoDoc { get; set; } = new();

        public bool ShouldSerializeExtrInfoDoc()
            => ExtrInfoDoc != null && ExtrInfoDoc.Count > 0;
        
        public bool ShouldSerializeContactoReceptor()
            => ContactoReceptor != null && ContactoReceptor.HasContent();
    }
    public class GosocketNombreEmisor
    {
        [XmlElement(Order = 1)]
        public string PrimerNombre { get; set; }
    }
    public class GosocketNombreRecep
    {
        [XmlElement(Order = 1)]
        public string PrimerNombre { get; set; }
    }
    public class GosocketLugarRecep
    {
        [XmlElement(Order = 1)]
        public string Calle { get; set; }
    }
    public class GosocketDocRecep
    {
        [XmlElement(Order = 1)]
        public string TipoDocRecep { get; set; }
        [XmlElement(Order = 2)]
        public string NroDocRecep { get; set; }
    }

    public class GosocketDomFiscal
    {
        //Otras Señas
        [XmlElement(Order = 1)]
        public string Calle { get; set; }

        //Provincia
        [XmlElement(Order = 2)]
        public string Departamento { get; set; }
        //Canton
        [XmlElement(Order = 3)]
        public string Distrito { get; set; }
        //Distrito
        [XmlElement(Order = 4)]
        public string Ciudad { get; set; }
        //Barrio
        [XmlElement(Order = 5)]
        public string Municipio { get; set; }
  
        //OtrasSenasExtranjero
        [XmlElement(Order = 6)]
        public string Referencia { get; set; }

        public bool ShouldSerializeBarrio() => !string.IsNullOrWhiteSpace(Municipio);
        public bool ShouldSerializeCalle() => !string.IsNullOrWhiteSpace(Calle); 
        public bool ShouldSerializeReferencia()  => !string.IsNullOrWhiteSpace(Referencia);
    }
    //    public class GosocketDomFiscal
    //{
    //    public string Provincia { get; set; }
    //    public string Canton { get; set; }
    //    public string Distrito { get; set; }

    //    public string Barrio { get; set; }
    //    public string Calle { get; set; }
    //    public string OtrasSenas { get; set; }

    //    public bool ShouldSerializeBarrio() => !string.IsNullOrWhiteSpace(Barrio);
    //    public bool ShouldSerializeCalle() => !string.IsNullOrWhiteSpace(Calle);
    //    public bool ShouldSerializeOtrasSenas() => !string.IsNullOrWhiteSpace(OtrasSenas);
    //}


    public class GosocketContactoEmisor
    {

        // MH: Emisor/CorreoElectronico -> GoSocket: Encabezado/Emisor/ContactoEmisor/eMail
        [XmlElement(Order = 1)]
        public string eMail { get; set; }

        // MH: Emisor/Telefono/CodigoPais -> GoSocket: Encabezado/Emisor/ContactoEmisor/Extension
        [XmlElement(Order = 2)]
        public string Extension { get; set; }
        // MH: Emisor/Telefono/NumTelefono -> GoSocket: Encabezado/Emisor/ContactoEmisor/Telefono
        [XmlElement(Order =3)]
        public string Telefono { get; set; }

 

        public bool HasContent()
            => !string.IsNullOrWhiteSpace(Telefono) || !string.IsNullOrWhiteSpace(eMail);

        public bool ShouldSerializeExtension() => !string.IsNullOrWhiteSpace(Extension);
        public bool ShouldSerializeTelefono() => !string.IsNullOrWhiteSpace(Telefono);
        public bool ShouldSerializeeMail() => !string.IsNullOrWhiteSpace(eMail);
    }

    public class GosocketContactoReceptor
    { 
        //CorreoElectronico
        [XmlElement(Order = 1)]
        public string eMail { get; set; }
        //CodigoPais
        [XmlElement(Order = 2)]
        public string Extension { get; set; }
        //NumTelefono
        [XmlElement(Order = 3)]
        public string Telefono { get; set; }
      

        public bool HasContent()
            => !string.IsNullOrWhiteSpace(Telefono) || !string.IsNullOrWhiteSpace(Telefono);

        public bool ShouldSerializeTelefono() => !string.IsNullOrWhiteSpace(Telefono);
        public bool ShouldSerializeCorreo() => !string.IsNullOrWhiteSpace(eMail);
    }
    // =========================
    // DETALLE (líneas)
    // =========================
    public class GosocketDetalle
    {
        [XmlElement(Order = 1)]
        public int NroLinDet { get; set; }

        [XmlElement(Order = 2)]
        public string TpoListaItem { get; set; }

        public bool ShouldSerializeTpoListaItem() =>   !string.IsNullOrWhiteSpace(TpoListaItem);


        // ...
        [XmlElement("CdgItem", Order = 3)]
        public List<GosocketCdgItem> CdgItem { get; set; } = new();

        [XmlElement(Order = 4)]
        public string DscItem { get; set; }

        [XmlElement(Order = 5)]
        public decimal QtyItem { get; set; }

        [XmlElement(Order = 6)]
        public string UnmdItem { get; set; }

        [XmlElement(Order = 7)]
        public string IndListaItem { get; set; }

        [XmlElement(Order = 8)]
        public string UnidadMedidaComercial { get; set; }

        [XmlElement(Order = 9)]
        public decimal PrcNetoItem { get; set; }

        [XmlElement(Order = 10)]
        public GosocketSubDscto SubDscto { get; set; }
        public bool ShouldSerializeSubDscto()
           => SubDscto != null && SubDscto.MntDscto > 0m;
              
        [XmlElement(Order = 11)]
        public decimal RecargoMonto { get; set; }

        [XmlElement(Order = 12)]
        public GosocketSubRecargo SubRecargo { get; set; }

        [XmlElement("ImpuestosDet", Order = 13)]
        public List<GosocketImpuestosDet> ImpuestosDet { get; set; } = new();

        public bool ShouldSerializeImpuestosDet()
            => ImpuestosDet != null && ImpuestosDet.Count > 0;

        [XmlElement(Order = 14)]
        public decimal MontoBrutoItem { get; set; }
        public bool ShouldSerializeMontoBrutoItem()
         => MontoBrutoItem != null;

        [XmlElement(Order = 15)]
        public decimal MontoNetoItem { get; set; }
        public bool ShouldSerializeMontoNetoItem()
         => MontoNetoItem != null;


        [XmlElement("Exoneracion", Order = 16)]
        public GosocketExoneracion Exoneracion { get; set; }
        public bool ShouldSerializeExoneracion() => Exoneracion != null && Exoneracion.HasContent();

        [XmlElement("DetalleComp", Order = 17)]
        public GosocketDetalleComp DetalleComp { get; set; }
        
        [XmlElement(Order = 18)]
        public decimal MontoTotalItem { get; set; }
        
        // Flex: aquí marcamos autoconsumo: TipoTransaccion=03/05
        [XmlElement("ExtraInfoDetalle", Order = 19)]
        public List<GosocketExtraInfoDetalle> ExtraInfoDetalle { get; set; } = new();
    
        public bool ShouldSerializeExtraInfoDetalle() => ExtraInfoDetalle != null && ExtraInfoDetalle.Count > 0;
        public bool ShouldSerializeCdgItem() => CdgItem != null && CdgItem.Count > 0;
        public bool ShouldSerializeIndIsrItem() => !string.IsNullOrWhiteSpace(IndListaItem);
        public bool ShouldSerializeUnidadMedidaComercial() => !string.IsNullOrWhiteSpace(UnidadMedidaComercial);
        public bool ShouldSerializeDetalleComp() => DetalleComp != null && DetalleComp.ShouldSerializeParte();
    }

    #region DetalleComp (Surtido / Partes)



    //public class GosocketSubRecargo
    //{

    //      <TipoRecargo />
    //            <GlosaRecargo />



    //    [XmlElement("MntRecargo", Order = 1)]
    //    public decimal MntRecargo { get; set; }

    //    public bool ShouldSerializeMntRecargo() => MntRecargo > 0m;
    //}
    public class GosocketSubRecargo
    {
        [XmlElement("TipoRecargo", Order = 1)]
        public string? TipoRecargo { get; set; }

        [XmlElement("GlosaRecargo", Order = 2)]
        public string? GlosaRecargo { get; set; }

        [XmlElement("MntRecargo", Order = 3)]
        public decimal? MntRecargo { get; set; }

        // --- Condicionar serialización ---
        public bool ShouldSerializeTipoRecargo() =>
            !string.IsNullOrWhiteSpace(TipoRecargo);

        public bool ShouldSerializeGlosaRecargo() =>
            !string.IsNullOrWhiteSpace(GlosaRecargo);

        public bool ShouldSerializeMntRecargo() =>
            MntRecargo.HasValue && MntRecargo.Value > 0m;

       
    }

    // /DTE/Documento/Detalle[1]/DetalleComp
    public class GosocketDetalleComp
    {
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]
        [XmlElement("Parte", Order = 1)]
        public List<GosocketParte> Parte { get; set; } = new();

        public bool ShouldSerializeParte() => Parte != null && Parte.Count > 0;
    }

    // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]
    public class GosocketParte
    {
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/CdgParte[TpoCodigoParte='CABYS']/VlrCodigoParte
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/CdgParte/TpoCodigoParte
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/CdgParte[TpoCodigoParte='XX']/VlrCodigoParte
        //
        // Nota: este nodo se repite (uno para CABYS y otro para Código Comercial “XX” o el que aplique)
        [XmlElement("CdgParte", Order = 1)]
        public List<GosocketCdgParte> CdgParte { get; set; } = new();

        public bool ShouldSerializeCdgParte() => CdgParte != null && CdgParte.Count > 0;

        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/QtyItemParte
        [XmlElement("QtyItemParte", Order = 2)]
        public decimal QtyItemParte { get; set; }

        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/UnmdItemParte
        [XmlElement("UnmdItemParte", Order = 3)]
        public string UnmdItemParte { get; set; }

        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/UnidadComercialParte
        [XmlElement("UnidadComercialParte", Order = 4)]
        public string UnidadComercialParte { get; set; }

        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/DscItemParte
        [XmlElement("DscItemParte", Order = 5)]
        public string DscItemParte { get; set; }

        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/PrcNetoParte
        [XmlElement("PrcNetoParte", Order = 6)]
        public decimal PrcNetoParte { get; set; }

        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/MontoBrutoParte
        [XmlElement("MontoBrutoParte", Order = 7)]
        public decimal MontoBrutoParte { get; set; }

        // Descuento surtido
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/SubDsctoParte/MntDscto
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/SubDsctoParte/TipoDscto
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/SubDsctoParte/GlosaDscto
        [XmlElement("SubDsctoParte", Order = 8)]
        public GosocketSubDsctoParte SubDsctoParte { get; set; }

        public bool ShouldSerializeSubDsctoParte() => SubDsctoParte != null;

        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/MontoNetoParte
        [XmlElement("MontoNetoParte", Order = 9)]
        public decimal MontoNetoParte { get; set; }

        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/ExtraInfoParte[@name='IVACobradoFabrica']
        [XmlElement("ExtraInfoParte", Order = 10)]
        public List<GosocketNameValue> ExtraInfoParte { get; set; } = new();

        public bool ShouldSerializeExtraInfoParte() => ExtraInfoParte != null && ExtraInfoParte.Count > 0;

        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/MontoTotalParte
        [XmlElement("MontoTotalParte", Order = 11)]
        public decimal MontoTotalParte { get; set; }

        // Impuesto surtido
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/ImpuestosParte/TipoImp
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/ImpuestosParte/CodTasaImp
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/ImpuestosParte/TasaImp
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/ImpuestosParte/CuotaImp
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/ImpuestosParte/ExtraInfoImpDetParte[@name='CantidadUnidadMedida'|'Porcentaje'|'Proporcion'|'VolumenUnidadConsumo'|'ImpuestoUnidad']
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/ImpuestosParte/MontoImp
        [XmlElement("ImpuestosParte", Order = 12)]
        public GosocketImpuestosParte ImpuestosParte { get; set; }

        public bool ShouldSerializeImpuestosParte() => ImpuestosParte != null;
    }

    // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/CdgParte
    public class GosocketCdgParte
    {
        // TpoCodigoParte='CABYS' o 'XX' (o el código que aplique según su catálogo)
        [XmlAttribute("TpoCodigoParte")]
        public string TpoCodigoParte { get; set; }

        [XmlElement("VlrCodigoParte", Order = 2)]
        public string VlrCodigoParte { get; set; }

        public bool ShouldSerializeTpoCodigoParte() => !string.IsNullOrWhiteSpace(TpoCodigoParte);
        public bool ShouldSerializeVlrCodigoParte() => !string.IsNullOrWhiteSpace(VlrCodigoParte);
    }

    // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/SubDsctoParte
    public class GosocketSubDsctoParte
    {
        [XmlElement("MntDscto", Order = 1)]
        public decimal MntDscto { get; set; }

        [XmlElement("TipoDscto", Order = 2)]
        public string TipoDscto { get; set; }

        [XmlElement("GlosaDscto", Order = 3)]
        public string GlosaDscto { get; set; }

        public bool ShouldSerializeTipoDscto() => !string.IsNullOrWhiteSpace(TipoDscto);
        public bool ShouldSerializeGlosaDscto() => !string.IsNullOrWhiteSpace(GlosaDscto);
    }

    // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/ImpuestosParte
    public class GosocketImpuestosParte
    {
        [XmlElement("TipoImp", Order = 1)]
        public string TipoImp { get; set; }

        [XmlElement("CodTasaImp", Order = 2)]
        public string CodTasaImp { get; set; }

        [XmlElement("TasaImp", Order = 3)]
        public decimal? TasaImp { get; set; }

        [XmlElement("CuotaImp", Order = 4)]
        public decimal? CuotaImp { get; set; }

        //CantidadUni
        //Porcentaje
        //Proporcion
        //VolumenUnidadConsumo
        //ImpuestoUnidad
        [XmlElement("ExtraInfoImpDetParte", Order = 5)]
        public List<GosocketNameValue> ExtraInfoImpDetParte { get; set; } = new();

        [XmlElement("MontoImp", Order = 6)]
        public decimal MontoImp { get; set; }

        public bool ShouldSerializeTipoImp() => !string.IsNullOrWhiteSpace(TipoImp);
        public bool ShouldSerializeCodTasaImp() => !string.IsNullOrWhiteSpace(CodTasaImp);
        public bool ShouldSerializeTasaImp() => TasaImp.HasValue;
        public bool ShouldSerializeCuotaImp() => CuotaImp.HasValue;
        public bool ShouldSerializeExtraInfoImpDetParte() => ExtraInfoImpDetParte != null && ExtraInfoImpDetParte.Count > 0;
    }



    #endregion

    /// <summary>
    /// Importante TpoCodigo puede tener 3 valores:
    ///CABYS= CodigoCABYS
    ///VIN=NumeroVINoSerie
    /// </summary>
    public class GosocketCdgItem
    {
        [XmlElement(Order = 1)]
        public string TpoCodigo { get; set; }
        [XmlElement(Order = 2)]
        public string VlrCodigo { get; set; }

        public bool ShouldSerializeTpoCodigo() => !string.IsNullOrWhiteSpace(TpoCodigo);
        public bool ShouldSerializeVlrCodigo() => !string.IsNullOrWhiteSpace(VlrCodigo);

    }

    public class GosocketSubDscto
    {
        [XmlElement(Order = 1)]
        public string GlosaDscto { get; set; }

        [XmlElement(Order = 2)]
        public string PctDscto { get; set; }
        [XmlElement(Order = 3)]
        public decimal MntDscto { get; set; }

        [XmlElement(Order = 4)]
        public string TipoDscto { get; set; }

        public bool ShouldSerializeGlosaDscto() => !string.IsNullOrWhiteSpace(GlosaDscto);
        public bool ShouldSerializeTipoDscto() => !string.IsNullOrWhiteSpace(TipoDscto);
    }

    // =========================
    // IMPUESTOS / EXONERACIÓN
    // =========================
    public class GosocketImpuestosDet
    {
        [XmlElement(Order = 1)]
        public string TipoImp { get; set; }
        [XmlElement(Order = 2)]
        public string CodImp { get; set; }
        [XmlElement(Order = 3)]
        public string CodTasaImp { get; set; }
        [XmlElement(Order = 4)]
        public decimal TasaImp { get; set; }

        [XmlElement(Order = 5)]
        public decimal? TasMontoBaseImpaImp { get; set; }

        public bool ShouldSerializeTasMontoBaseImpaImp() => TasMontoBaseImpaImp.HasValue;


        [XmlElement(Order = 6)]
        public decimal MontoImp { get; set; }
        [XmlElement(Order = 7)]
        public decimal? MontoExportacion { get; set; }

        public bool ShouldSerializeMontoExportacion() => MontoExportacion.HasValue;
    }


    public class GosocketExoneracion
    {
        // =========================
        // MH: LineaDetalle/Impuesto/Exoneracion/TipoDocumentoEX1
        // GoSocket: .../Exoneracion/TipoDocumento
        // =========================
        [XmlElement("TipoDocumento", Order = 1)]
        public string TipoDocumento { get; set; } = "";

        // =========================
        // MH: LineaDetalle/Impuesto/Exoneracion/TipoDocumentoOTRO
        // GoSocket: .../Exoneracion/NombreDocumento
        // (GoSocket lo nombra "NombreDocumento" aunque MH es "TipoDocumentoOTRO")
        // =========================
        [XmlElement("NombreDocumento", Order = 2)]
        public string NombreDocumento { get; set; } = "";

        // =========================
        // MH: LineaDetalle/Impuesto/Exoneracion/NumeroDocumento
        // GoSocket: .../Exoneracion/NumeroDocumento
        // =========================
        [XmlElement("NumeroDocumento", Order = 3)]
        public string NumeroDocumento { get; set; } = "";

        // =========================
        // MH: LineaDetalle/Impuesto/Exoneracion/Articulo
        // GoSocket: .../Exoneracion/Articulo
        // =========================
        [XmlElement("Articulo", Order = 4)]
        public string Articulo { get; set; } = "";

        // =========================
        // MH: LineaDetalle/Impuesto/Exoneracion/Inciso
        // GoSocket: .../Exoneracion/Inciso
        // =========================
        [XmlElement("Inciso", Order = 5)]
        public string Inciso { get; set; } = "";

        // =========================
        // MH: LineaDetalle/Impuesto/Exoneracion/NombreInstitucion
        // GoSocket: .../Exoneracion/Institucion
        // (En tu Excel lo marcan así: Institucion)
        // =========================
        [XmlElement("Institucion", Order = 6)]
        public string Institucion { get; set; } = "";

        // =========================
        // MH: LineaDetalle/Impuesto/Exoneracion/NombreInstitucionOtros
        // GoSocket: .../Exoneracion/NombreInstitucion
        // =========================
        [XmlElement("NombreInstitucion", Order = 7)]
        public string NombreInstitucion { get; set; } = "";

        // =========================
        // MH: LineaDetalle/Impuesto/Exoneracion/FechaEmisionEX
        // GoSocket: .../Exoneracion/FechaEmision
        // Recomendación: guardarlo como string ya formateado (ISO) para no pelear con formatos.
        // =========================
        [XmlElement("FechaEmision", Order = 8)]
        public string FechaEmision { get; set; } = "";

        // =========================
        // MH: LineaDetalle/Impuesto/Exoneracion/TarifaExonerada
        // GoSocket: .../Exoneracion/PorcentajeCompra
        // =========================
        [XmlElement("PorcentajeCompra", Order = 9)]
        public decimal? PorcentajeCompra { get; set; }

        // =========================
        // MH: LineaDetalle/Impuesto/Exoneracion/MontoExoneracion
        // GoSocket: .../Exoneracion/MontoImpuesto
        // =========================
        [XmlElement("MontoImpuesto", Order = 10)]
        public decimal? MontoImpuesto { get; set; }

        // ==========================================================
        // SHOULD SERIALIZE (evita nodos vacíos => evita rechazos)
        // ==========================================================
        public bool ShouldSerializeTipoDocumento() => !string.IsNullOrWhiteSpace(TipoDocumento);

        public bool ShouldSerializeNombreDocumento() => !string.IsNullOrWhiteSpace(NombreDocumento);

        public bool ShouldSerializeNumeroDocumento() => !string.IsNullOrWhiteSpace(NumeroDocumento);

        public bool ShouldSerializeArticulo() => !string.IsNullOrWhiteSpace(Articulo);

        public bool ShouldSerializeInciso() => !string.IsNullOrWhiteSpace(Inciso);

        public bool ShouldSerializeInstitucion() => !string.IsNullOrWhiteSpace(Institucion);

        public bool ShouldSerializeNombreInstitucion() => !string.IsNullOrWhiteSpace(NombreInstitucion);

        public bool ShouldSerializeFechaEmision() => !string.IsNullOrWhiteSpace(FechaEmision);

        public bool ShouldSerializePorcentajeCompra() => PorcentajeCompra.HasValue;

        public bool ShouldSerializeMontoImpuesto() => MontoImpuesto.HasValue;

        /// <summary>
        /// Útil si en tu código quieres decidir “si creo Exoneracion o no”.
        /// Regla mínima típica: si no hay TipoDocumento + NumeroDocumento, no debería existir el nodo.
        /// </summary>
        public bool HasContent()
            => !string.IsNullOrWhiteSpace(TipoDocumento)
               && !string.IsNullOrWhiteSpace(NumeroDocumento);
    }

    #region GoSocket - OtrosCargos (MH) -> DscRcgGlobal (GoSocket)

    /// <summary>
    /// GoSocket: /DTE/Documento/DscRcgGlobal[*]
    /// MH:      /FacturaElectronica/OtrosCargos/OtroCargo[*]
    /// </summary>
    public class GosocketDscRcgGlobal
    {
        /// <summary>
        /// GoSocket: /DTE/Documento/DscRcgGlobal[1]/NroLinDR
        /// MH: (no existe como tal) -> se puede generar como consecutivo 1..n por cada OtroCargo
        /// </summary>
        [XmlElement("NroLinDR", Order = 1)]
        public int NroLinDR { get; set; }

        /// <summary>
        /// GoSocket: /DTE/Documento/DscRcgGlobal[1]/CodigoDR
        /// MH: /FacturaElectronica/OtrosCargos/OtroCargo/TipoDocumentoOC
        /// (o el equivalente que estés trayendo para identificar el tipo de cargo)
        /// </summary>
        [XmlElement("CodigoDR", Order = 2)]
        public string? CodigoDR { get; set; }

        /// <summary>
        /// GoSocket: /DTE/Documento/DscRcgGlobal[1]/TpoMov
        /// MH: /FacturaElectronica/OtrosCargos/OtroCargo/TipoDocumentoOTROS (si aplica)
        /// o un mapeo tuyo para "tipo de movimiento" (Cargo/Descuento) según tu lógica.
        /// </summary>
        [XmlElement("TpoMov", Order = 3)]
        public string? TpoMov { get; set; }

        // ---------------- Identificación Tercero ----------------

        /// <summary>
        /// GoSocket: /DTE/Documento/DscRcgGlobal[1]/TipoIdentidadTercero
        /// MH: /FacturaElectronica/OtrosCargos/OtroCargo/IdentificacionTercero/Tipo
        /// </summary>
        [XmlElement("TipoIdentidadTercero", Order = 4)]
        public string? TipoIdentidadTercero { get; set; }

        /// <summary>
        /// GoSocket: /DTE/Documento/DscRcgGlobal[1]/NumeroIdentidadTercero
        /// MH: /FacturaElectronica/OtrosCargos/OtroCargo/IdentificacionTercero/Numero
        /// </summary>
        [XmlElement("NumeroIdentidadTercero", Order = 5)]
        public string? NumeroIdentidadTercero { get; set; }

        /// <summary>
        /// GoSocket: /DTE/Documento/DscRcgGlobal[1]/NombreTercero
        /// MH: /FacturaElectronica/OtrosCargos/OtroCargo/NombreTercero
        /// </summary>
        [XmlElement("NombreTercero", Order = 6)]
        public string? NombreTercero { get; set; }

        // ---------------- Detalle ----------------

        /// <summary>
        /// GoSocket: /DTE/Documento/DscRcgGlobal[1]/GlosaDR
        /// MH: /FacturaElectronica/OtrosCargos/OtroCargo/Detalle
        /// </summary>
        [XmlElement("GlosaDR", Order = 7)]
        public string? GlosaDR { get; set; }

        /// <summary>
        /// GoSocket: /DTE/Documento/DscRcgGlobal[1]/IndExeDR
        /// MH: (no existe como tal)
        /// Se usa como indicador (p.ej. exento/no gravado) según reglas de GoSocket / tu integración.
        /// </summary>
        [XmlElement("IndExeDR", Order = 8)]
        public string? IndExeDR { get; set; }

        /// <summary>
        /// GoSocket: /DTE/Documento/DscRcgGlobal[1]/ValorDR
        /// MH: /FacturaElectronica/OtrosCargos/OtroCargo/MontoCargo
        /// </summary>
        [XmlElement("ValorDR", Order = 9)]
        public decimal ValorDR { get; set; }

        // ----------------- ShouldSerialize (evitar nodos vacíos) -----------------

        public bool ShouldSerializeCodigoDR() => !string.IsNullOrWhiteSpace(CodigoDR);
        public bool ShouldSerializeTpoMov() => !string.IsNullOrWhiteSpace(TpoMov);

        public bool ShouldSerializeTipoIdentidadTercero() => !string.IsNullOrWhiteSpace(TipoIdentidadTercero);
        public bool ShouldSerializeNumeroIdentidadTercero() => !string.IsNullOrWhiteSpace(NumeroIdentidadTercero);
        public bool ShouldSerializeNombreTercero() => !string.IsNullOrWhiteSpace(NombreTercero);

        public bool ShouldSerializeGlosaDR() => !string.IsNullOrWhiteSpace(GlosaDR);
        public bool ShouldSerializeIndExeDR() => !string.IsNullOrWhiteSpace(IndExeDR);

        // ValorDR es decimal no-nullable: siempre saldrá. Si quieres ocultarlo cuando sea 0:
        // public bool ShouldSerializeValorDR() => ValorDR != 0m;
    }

    #endregion

    #region TOTALES (GoSocket)

    // =========================
    // TOTALES (GoSocket)
    // =========================
    // Encabezado/Totales/*
    public class GosocketTotales
    {
        // /DTE/Documento/Encabezado/Totales/Moneda
        [XmlElement("Moneda", Order = 1)]
        public string? Moneda { get; set; }
        public bool ShouldSerializeMoneda() => !string.IsNullOrWhiteSpace(Moneda);

        // /DTE/Documento/Encabezado/Totales/FctConv
        [XmlElement("FctConv", Order = 2)]
        public decimal? FctConv { get; set; }
        public bool ShouldSerializeFctConv() => FctConv.HasValue;

        // /DTE/Documento/Encabezado/Totales/SubTotal
        [XmlElement("SubTotal", Order = 3)]
        public decimal? SubTotal { get; set; }
        public bool ShouldSerializeSubTotal() => SubTotal.HasValue;


        // /DTE/Documento/Encabezado/Totales/MntDcto
        [XmlElement("MntDcto", Order = 4)]
        public decimal? MntDcto { get; set; }
        public bool ShouldSerializeMntDcto() => MntDcto.HasValue;

        // /DTE/Documento/Encabezado/Totales/MntBase
        [XmlElement("MntBase", Order = 5)]
        public decimal? MntBase { get; set; }
        public bool ShouldSerializeMntBase() => MntBase.HasValue;


        // /DTE/Documento/Encabezado/Totales/MntExe
        [XmlElement("MntExe", Order = 6)]
        public decimal? MntExe { get; set; }
        public bool ShouldSerializeMntExe() => MntExe.HasValue;

        // /DTE/Documento/Encabezado/Totales/ImporteNoGravado
        [XmlElement("ImporteNoGravado", Order = 7)]
        public decimal? ImporteNoGravado { get; set; }
        public bool ShouldSerializeImporteNoGravado() => ImporteNoGravado.HasValue;

        // /DTE/Documento/Encabezado/Totales/ImporteOtrosTributos
        [XmlElement("ImporteOtrosTributos", Order = 8)]
        public decimal? ImporteOtrosTributos { get; set; }
        public bool ShouldSerializeImporteOtrosTributos() => ImporteOtrosTributos.HasValue;

        // /DTE/Documento/Encabezado/Totales/MntImp
        [XmlElement("MntImp", Order = 9)]
        public decimal? MntImp { get; set; }
        public bool ShouldSerializeMntImp() => MntImp.HasValue;

        // /DTE/Documento/Encabezado/Totales/SaldoAnterior
        [XmlElement("SaldoAnterior", Order = 10)]
        public decimal? SaldoAnterior { get; set; }
        public bool ShouldSerializeSaldoAnterior() => SaldoAnterior.HasValue;

        // /DTE/Documento/Encabezado/Totales/VlrPagar
        [XmlElement("VlrPagar", Order = 11)]
        public decimal? VlrPagar { get; set; }
        public bool ShouldSerializeVlrPagar() => VlrPagar.HasValue;

        // /DTE/Documento/Encabezado/Totales/VlrPalabras
        [XmlElement("VlrPalabras", Order = 12)]
        public string? VlrPalabras { get; set; }
        public bool ShouldSerializeVlrPalabras() => !string.IsNullOrWhiteSpace(VlrPalabras);


        // /DTE/Documento/Encabezado/Totales/TotSubMonto[i]/MontoConcepto   (i = 1..8)
        [XmlElement("TotSubMonto", Order = 13)]
        public List<GosocketTotSubMonto> TotSubMonto { get; set; } = new();
        public bool ShouldSerializeTotSubMonto() => TotSubMonto != null && TotSubMonto.Count > 0;
                
        // /DTE/Documento/Encabezado/Totales/ExtraInfoTotal[@name='TotalNoSujeto']
        // (y otros extras de Totales)
        [XmlElement("ExtraInfoTotal", Order = 14)]
        public List<GosocketNameValue>? ExtraInfoTotal { get; set; }
        public bool ShouldSerializeExtraInfoTotal() => ExtraInfoTotal != null && ExtraInfoTotal.Count > 0;
                   
        // /DTE/Documento/Encabezado/Totales/MntRcgo
        [XmlElement("MntRcgo", Order = 15)]
        public decimal? MntRcgo { get; set; }
        public bool ShouldSerializeMntRcgo() => MntRcgo.HasValue;
           
    }

    // Cada ítem representa un concepto distinto según el índice [1..8]
    public class GosocketTotSubMonto
    {
        // /DTE/Documento/Encabezado/Totales/TotSubMonto[i]/Tipo
        [XmlElement("Tipo", Order = 1)]
        public decimal Tipo { get; set; }
       
        // /DTE/Documento/Encabezado/Totales/TotSubMonto[i]/CodTipoMonto
        [XmlElement("CodTipoMonto", Order = 2)]
        public decimal CodTipoMonto { get; set; }

        // /DTE/Documento/Encabezado/Totales/TotSubMonto[i]/MontoConcepto
        [XmlElement("MontoConcepto", Order = 3)]
        public decimal MontoConcepto { get; set; }
    }

    public class GosocketImpuestoTotal
    {
        // /DTE/Documento/Encabezado/Impuestos[*]/TipoImp
        [XmlElement("TipoImp", Order = 1)]
        public string? TipoImp { get; set; }
        public bool ShouldSerializeTipoImp() => !string.IsNullOrWhiteSpace(TipoImp);

        // /DTE/Documento/Encabezado/Impuestos[*]/CodTasaImp
        [XmlElement("CodTasaImp", Order = 2)]
        public string? CodTasaImp { get; set; }
        public bool ShouldSerializeCodTasaImp() => !string.IsNullOrWhiteSpace(CodTasaImp);

        // /DTE/Documento/Encabezado/Impuestos[*]/MontoImp
        [XmlElement("MontoImp", Order = 3)]
        public decimal? MontoImp { get; set; }
        public bool ShouldSerializeMontoImp() => MontoImp.HasValue;
    }

    public class GosocketPago
    {
        // MH: <MedioPago><TipoMedioPago/> -> GoSocket: /Encabezado/IdDoc/Pagos/TipoPago
        [XmlElement("TipoPago", Order = 1)]
        public string TipoPago { get; set; }

        // MH: <MedioPago><MedioPagoOtros/> -> GoSocket: /Encabezado/IdDoc/Pagos/DescPago
        [XmlElement("DescPago", Order = 2)]
        public string DescPago { get; set; }

        // MH: <MedioPago><TotalMedioPago/> -> GoSocket: /Encabezado/IdDoc/Pagos/Monto
        [XmlElement("Monto", Order = 3)]
        public decimal Monto { get; set; }

        public bool ShouldSerializeTipoPago() => !string.IsNullOrWhiteSpace(TipoPago);
        public bool ShouldSerializeDescPago() => !string.IsNullOrWhiteSpace(DescPago);
    }


    // ExtraInfoTotal[@name='X']value
    public class GosocketNameValue
    {
        [XmlAttribute("name")]
        public string? Name { get; set; }

        [XmlText]
        public string? Value { get; set; }

        public bool ShouldSerializeName() => !string.IsNullOrWhiteSpace(Name);
        public bool ShouldSerializeValue() => !string.IsNullOrWhiteSpace(Value);
    }

    #endregion

    // =========================
    // REFERENCIA / OTROS
    // =========================
    public class GosocketReferencia
    {
        [XmlElement(Order = 1)]
        public string TpoDocRef { get; set; }
        [XmlElement(Order = 2)]
        public string NumeroRef { get; set; }
        [XmlElement(Order = 3)]
        public string FechaRef { get; set; }
        [XmlElement(Order = 4)]
        public string CodRef { get; set; }
        [XmlElement(Order = 5)]
        public string GlosaRef { get; set; }
        [XmlElement(Order = 6)]
        public string RazonRef { get; set; }

        public bool ShouldSerializeGlosaRef() => !string.IsNullOrWhiteSpace(GlosaRef);
        public bool ShouldSerializeRazonRef() => !string.IsNullOrWhiteSpace(RazonRef);
    }

    public class GosocketOtros
    {
        [XmlElement("OtroTexto", Order = 1)]
        public List<GosocketExtraInfoDetalle> OtroTexto { get; set; } = new();

        [XmlElement("OtroContenido", Order = 2)]
        public List<GosocketExtraInfoDetalle> OtroContenido { get; set; } = new();

        public bool HasContent()
            => OtroTexto != null && OtroTexto.Count > 0 || OtroContenido != null && OtroContenido.Count > 0;

        public bool ShouldSerializeOtroTexto()
            => OtroTexto != null && OtroTexto.Count > 0;

        public bool ShouldSerializeOtroContenido()
            => OtroContenido != null && OtroContenido.Count > 0;
    }

    // =========================
    // PERSONALIZADOS (GoSocket)
    // =========================
    public class GosocketPersonalizados
    {
        [XmlElement("campoString", Order = 1)]
        public List<GosocketCampoString> CampoString { get; set; } = new();

        //[XmlElement("CustDetalle", Order = 2)]
        //public List<GosocketCustDetalle> CustDetalle { get; set; } = new();

        //public bool HasContent()
        //    => CampoString != null && CampoString.Count > 0 || CustDetalle != null && CustDetalle.Count > 0;

        public bool ShouldSerializeCampoString()
            => CampoString != null && CampoString.Count > 0;

        //public bool ShouldSerializeCustDetalle()
        //    => CustDetalle != null && CustDetalle.Count > 0;
    }

    public class GosocketCampoString
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlText]
        public string Value { get; set; }
    }

    public class GosocketCustDetalle
    {
        [XmlElement(Order = 1)]
        public int NroLinDet { get; set; }

        [XmlElement("campoString", Order = 2)]
        public List<GosocketCampoString> CampoString { get; set; } = new();

        public bool ShouldSerializeCampoString()
            => CampoString != null && CampoString.Count > 0;
    }

    // =========================
    // GosocketExtraInfoDetalle, es una especie de campos dinamico o extras
    // que permite GoSocket agregar ya que por defecto estos campos no lo tiene

    //Name se usara para:
    //  RegistroMedicamento
    //  FormaFarmaceutica
    //  IVACobradoFabrica
    //  ImpuestoAsumido
    //  TotalNoSujeto
    //  TotalImpAsumFabrica
    //Value se usara para contener el valor del Name

    // =========================
    public class GosocketExtraInfoDetalle
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlText]
        public string Value { get; set; }
    }
}