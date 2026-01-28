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

        public bool ShouldSerializePersonalizados()
            => Personalizados != null && Personalizados.HasContent();
    }

    // =========================
    // DOCUMENTO
    // =========================
    public class GosocketDocumento
    {
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

        // Flex: extras a nivel documento
        [XmlElement("ExtrInfoDoc", Order = 4)]
        public List<GosocketExtraInfoDetalle> ExtrInfoDoc { get; set; } = new();

        public bool ShouldSerializeExtrInfoDoc()
            => ExtrInfoDoc != null && ExtrInfoDoc.Count > 0;

        [XmlElement("Totales", Order = 5)]
        public GosocketTotales Totales { get; set; }
    }

    // =========================
    // IDDOC
    // =========================
    public class GosocketIdDoc
    {
        //Campos adicionales para Gosocket
        public string Version { get; set; }
        public string Ambiente { get; set; }
        public string Tipo { get; set; }
        public string Numero { get; set; }
        public string NumeroInterno { get; set; }

        //Clave
        public string ContenidoTC { get; set; }
        //CodigoActividadEmisor
        public string TipoEmision { get; set; }

        //NumeroConsecutivo
        public string Establecimiento { get; set; }

        // FechaEmision
        public string FechaEmis { get; set; }
        //CondicionVenta
        public string CondPago { get; set; }
        //CondicionVentaOtros
        public string ExtraInfoDoc { get; set; }
        //PlazoCredito
        public string TermPagoCdg { get; set; }

        [XmlElement("Pagos")]
        public List<GosocketPago> Pagos { get; set; } = new();

        public bool ShouldSerializePagos() => Pagos != null && Pagos.Count > 0;

        public bool ShouldSerializeNumeroInterno() => !string.IsNullOrWhiteSpace(NumeroInterno);
        public bool ShouldSerializeContenidoTC() => !string.IsNullOrWhiteSpace(ContenidoTC);
    
    }

    // =========================
    // EMISOR / RECEPTOR
    // =========================
    public class GosocketEmisor
    {
        //Nombre
        public string NmbEmisor { get; set; }
        //TipoContribuyente/Tipo
        public string TipoContribuyente { get; set; }
        //TipoContribuyente/Numero
        public string IDEmisor { get; set; }

        // Flex: extras de emisor
        [XmlElement("ExtrInfoEmisor")]
        public List<GosocketExtraInfoDetalle> ExtrInfoEmisor { get; set; } = new();

        public bool ShouldSerializeExtrInfoEmisor()
            => ExtrInfoEmisor != null && ExtrInfoEmisor.Count > 0;
        //NombreComercial
        public GosocketNombreEmisor NombreEmiso { get; set; }

        //Ubicacion
        [XmlElement("DomFiscal")]
        public GosocketDomFiscal DomFiscal { get; set; }

        [XmlElement("ContactoEmisor")]
        public GosocketContactoEmisor ContactoEmisor { get; set; }

        public bool ShouldSerializeContactoEmisor()
            => ContactoEmisor != null && ContactoEmisor.HasContent();
    }

    public class GosocketReceptor
    {
        public string NmbRecep { get; set; }

        [XmlElement("DocRecep")]
        public GosocketDocRecep DocRecep { get; set; }

        // Flex: extras receptor (regimen, condición venta otros, etc.)
        [XmlElement("ExtrInfoDoc")]
        public List<GosocketExtraInfoDetalle> ExtrInfoDoc { get; set; } = new();

        public bool ShouldSerializeExtrInfoDoc()
            => ExtrInfoDoc != null && ExtrInfoDoc.Count > 0;

        [XmlElement("DomFiscalRcp")]
        public GosocketDomFiscal DomFiscalRcp { get; set; }

        [XmlElement("ContactoReceptor")]
        public GosocketContactoReceptor ContactoReceptor { get; set; }

        public bool ShouldSerializeContactoReceptor()
            => ContactoReceptor != null && ContactoReceptor.HasContent();
    }
    public class GosocketNombreEmisor
    {
        public string PrimerNombre { get; set; }
    }
        public class GosocketDocRecep
    {
        public string TipoDoc { get; set; }
        public string NumDoc { get; set; }
    }

    public class GosocketDomFiscal
    {
        //Provincia
        public string Departamento { get; set; }
        //Canton
        public string Distrito { get; set; }
        //Distrito
        public string Ciudad { get; set; }
        //Barrio
        public string Municipio { get; set; }
        //Otras Señas
        public string Calle { get; set; }
        //OtrasSenasExtranjero
        public string Referencia { get; set; }

        public bool ShouldSerializeBarrio() => !string.IsNullOrWhiteSpace(Municipio);
        public bool ShouldSerializeCalle() => !string.IsNullOrWhiteSpace(Calle);
        public bool ShouldSerializeOtrasSenas() => !string.IsNullOrWhiteSpace(Referencia);

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
        //CodigoPais
        public string Extension { get; set; }
        //NumTelefono
        public string Telefono { get; set; }
        //CorreoElectronico
        public string eMail { get; set; }

        public bool HasContent()
            => !string.IsNullOrWhiteSpace(Telefono) || !string.IsNullOrWhiteSpace(Telefono);

        public bool ShouldSerializeTelefono() => !string.IsNullOrWhiteSpace(Telefono);
        public bool ShouldSerializeCorreo() => !string.IsNullOrWhiteSpace(eMail);
    }
  public class GosocketContactoReceptor
    {
        //CodigoPais
        public string Extension { get; set; }
        //NumTelefono
        public string Telefono { get; set; }
        //CorreoElectronico
        public string eMail { get; set; }

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
        public int NroLinDet { get; set; }
        public int TpoListaItem { get; set; }

        public GosocketCdgItem CdgItem { get; set; }
          
        public decimal QtyItem { get; set; }
        public string UnmdItem { get; set; }
        public string IndListaItem { get; set; } 
        public string UnidadMedidaComercial { get; set; }
        public string DscItem { get; set; }

         public decimal PrcNetoItem { get; set; }
       public decimal MontoBrutoItem { get; set; }

        [XmlElement("SubDscto")]
        public GosocketSubDscto SubDscto { get; set; }

        public bool ShouldSerializeSubDscto()
            => SubDscto != null && SubDscto.MntDscto  > 0m;

        // Flex: aquí marcamos autoconsumo: TipoTransaccion=03/05
        [XmlElement("ExtraInfoDetalle")]
        public List<GosocketExtraInfoDetalle> ExtraInfoDetalle { get; set; } = new();

        public decimal RecargoMonto { get; set; }

        public bool ShouldSerializeExtraInfoDetalle()
            => ExtraInfoDetalle != null && ExtraInfoDetalle.Count > 0;

        [XmlElement("ImpuestosDet")]
        public List<GosocketImpuestosDet> ImpuestosDet { get; set; } = new();

        public bool ShouldSerializeImpuestosDet()
            => ImpuestosDet != null && ImpuestosDet.Count > 0;

        [XmlElement("Exoneracion")]
        public GosocketExoneracion Exoneracion { get; set; }

        public bool ShouldSerializeExoneracion()
            => Exoneracion != null && Exoneracion.HasContent();

        public decimal MontoTotLinea { get; set; }

        public bool ShouldSerializeIndIsrItem() => !string.IsNullOrWhiteSpace(IndListaItem);
        public bool ShouldSerializeUnidadMedidaComercial() => !string.IsNullOrWhiteSpace(UnidadMedidaComercial);
    }

    #region DetalleComp (Surtido / Partes)

    // /DTE/Documento/Detalle[1]/DetalleComp
    public class GosocketDetalleComp
    {
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]
        [XmlElement("Parte")]
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
        [XmlElement("CdgParte")]
        public List<GosocketCdgParte> CdgParte { get; set; } = new();

        public bool ShouldSerializeCdgParte() => CdgParte != null && CdgParte.Count > 0;

        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/QtyItemParte
        [XmlElement("QtyItemParte")]
        public decimal QtyItemParte { get; set; }

        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/UnmdItemParte
        [XmlElement("UnmdItemParte")]
        public string UnmdItemParte { get; set; }

        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/UnidadComercialParte
        [XmlElement("UnidadComercialParte")]
        public string UnidadComercialParte { get; set; }

        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/DscItemParte
        [XmlElement("DscItemParte")]
        public string DscItemParte { get; set; }

        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/PrcNetoParte
        [XmlElement("PrcNetoParte")]
        public decimal PrcNetoParte { get; set; }

        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/MontoBrutoParte
        [XmlElement("MontoBrutoParte")]
        public decimal MontoBrutoParte { get; set; }

        // Descuento surtido
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/SubDsctoParte/MntDscto
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/SubDsctoParte/TipoDscto
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/SubDsctoParte/GlosaDscto
        [XmlElement("SubDsctoParte")]
        public GosocketSubDsctoParte SubDsctoParte { get; set; }

        public bool ShouldSerializeSubDsctoParte() => SubDsctoParte != null;

        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/MontoNetoParte
        [XmlElement("MontoNetoParte")]
        public decimal MontoNetoParte { get; set; }

        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/ExtraInfoParte[@name='IVACobradoFabrica']
        [XmlElement("ExtraInfoParte")]
        public List<GosocketNameValue> ExtraInfoParte { get; set; } = new();

        public bool ShouldSerializeExtraInfoParte() => ExtraInfoParte != null && ExtraInfoParte.Count > 0;

        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/MontoTotalParte
        [XmlElement("MontoTotalParte")]
        public decimal MontoTotalParte { get; set; }

        // Impuesto surtido
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/ImpuestosParte/TipoImp
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/ImpuestosParte/CodTasaImp
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/ImpuestosParte/TasaImp
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/ImpuestosParte/CuotaImp
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/ImpuestosParte/ExtraInfoImpDetParte[@name='CantidadUnidadMedida'|'Porcentaje'|'Proporcion'|'VolumenUnidadConsumo'|'ImpuestoUnidad']
        // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/ImpuestosParte/MontoImp
        [XmlElement("ImpuestosParte")]
        public GosocketImpuestosParte ImpuestosParte { get; set; }

        public bool ShouldSerializeImpuestosParte() => ImpuestosParte != null;
    }

    // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/CdgParte
    public class GosocketCdgParte
    {
        // TpoCodigoParte='CABYS' o 'XX' (o el código que aplique según su catálogo)
        [XmlAttribute("TpoCodigoParte")]
        public string TpoCodigoParte { get; set; }

        [XmlElement("VlrCodigoParte")]
        public string VlrCodigoParte { get; set; }

        public bool ShouldSerializeTpoCodigoParte() => !string.IsNullOrWhiteSpace(TpoCodigoParte);
        public bool ShouldSerializeVlrCodigoParte() => !string.IsNullOrWhiteSpace(VlrCodigoParte);
    }

    // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/SubDsctoParte
    public class GosocketSubDsctoParte
    {
        [XmlElement("MntDscto")]
        public decimal MntDscto { get; set; }

        [XmlElement("TipoDscto")]
        public string TipoDscto { get; set; }

        [XmlElement("GlosaDscto")]
        public string GlosaDscto { get; set; }

        public bool ShouldSerializeTipoDscto() => !string.IsNullOrWhiteSpace(TipoDscto);
        public bool ShouldSerializeGlosaDscto() => !string.IsNullOrWhiteSpace(GlosaDscto);
    }

    // /DTE/Documento/Detalle[1]/DetalleComp/Parte[1]/ImpuestosParte
    public class GosocketImpuestosParte
    {
        [XmlElement("TipoImp")]
        public string TipoImp { get; set; }

        [XmlElement("CodTasaImp")]
        public string CodTasaImp { get; set; }

        [XmlElement("TasaImp")]
        public decimal? TasaImp { get; set; }

        [XmlElement("CuotaImp")]
        public decimal? CuotaImp { get; set; }

        //CantidadUni
        //Porcentaje
        //Proporcion
        //VolumenUnidadConsumo
        //ImpuestoUnidad
        [XmlElement("ExtraInfoImpDetParte")]
        public List<GosocketNameValue> ExtraInfoImpDetParte { get; set; } = new();

        [XmlElement("MontoImp")]
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
        public string TpoCodigo { get; set; }
        public string VlrCodigo { get; set; }
           
        public bool ShouldSerializeTpoCodigo() => !string.IsNullOrWhiteSpace(TpoCodigo);
        public bool ShouldSerializeVlrCodigo() => !string.IsNullOrWhiteSpace(VlrCodigo);

    }
  
    public class GosocketSubDscto
    {
        public decimal MntDscto { get; set; }    
        public decimal PctDscto { get; set; }
        public decimal TipoDscto { get; set; }
        public string GlosaDscto { get; set; }

        public bool ShouldSerializeGlosaDscto() => !string.IsNullOrWhiteSpace(GlosaDscto);
    }

    // =========================
    // IMPUESTOS / EXONERACIÓN
    // =========================
    public class GosocketImpuestosDet
    {
        public string TipoImp { get; set; }
        public string CodImp { get; set; }
        public string CodTasaImp { get; set; }
        public decimal TasaImp { get; set; }
        public decimal TasMontoBaseImpaImp { get; set; }
        public decimal MontoImp { get; set; }
        public decimal MontoExportacion { get; set; }
 
    }
 

public class GosocketExoneracion
    {
        // =========================
        // MH: LineaDetalle/Impuesto/Exoneracion/TipoDocumentoEX1
        // GoSocket: .../Exoneracion/TipoDocumento
        // =========================
        [XmlElement("TipoDocumento")]
        public string TipoDocumento { get; set; } = "";

        // =========================
        // MH: LineaDetalle/Impuesto/Exoneracion/TipoDocumentoOTRO
        // GoSocket: .../Exoneracion/NombreDocumento
        // (GoSocket lo nombra "NombreDocumento" aunque MH es "TipoDocumentoOTRO")
        // =========================
        [XmlElement("NombreDocumento")]
        public string NombreDocumento { get; set; } = "";

        // =========================
        // MH: LineaDetalle/Impuesto/Exoneracion/NumeroDocumento
        // GoSocket: .../Exoneracion/NumeroDocumento
        // =========================
        [XmlElement("NumeroDocumento")]
        public string NumeroDocumento { get; set; } = "";

        // =========================
        // MH: LineaDetalle/Impuesto/Exoneracion/Articulo
        // GoSocket: .../Exoneracion/Articulo
        // =========================
        [XmlElement("Articulo")]
        public string Articulo { get; set; } = "";

        // =========================
        // MH: LineaDetalle/Impuesto/Exoneracion/Inciso
        // GoSocket: .../Exoneracion/Inciso
        // =========================
        [XmlElement("Inciso")]
        public string Inciso { get; set; } = "";

        // =========================
        // MH: LineaDetalle/Impuesto/Exoneracion/NombreInstitucion
        // GoSocket: .../Exoneracion/Institucion
        // (En tu Excel lo marcan así: Institucion)
        // =========================
        [XmlElement("Institucion")]
        public string Institucion { get; set; } = "";

        // =========================
        // MH: LineaDetalle/Impuesto/Exoneracion/NombreInstitucionOtros
        // GoSocket: .../Exoneracion/NombreInstitucion
        // =========================
        [XmlElement("NombreInstitucion")]
        public string NombreInstitucion { get; set; } = "";

        // =========================
        // MH: LineaDetalle/Impuesto/Exoneracion/FechaEmisionEX
        // GoSocket: .../Exoneracion/FechaEmision
        // Recomendación: guardarlo como string ya formateado (ISO) para no pelear con formatos.
        // =========================
        [XmlElement("FechaEmision")]
        public string FechaEmision { get; set; } = "";

        // =========================
        // MH: LineaDetalle/Impuesto/Exoneracion/TarifaExonerada
        // GoSocket: .../Exoneracion/PorcentajeCompra
        // =========================
        [XmlElement("PorcentajeCompra")]
        public decimal? PorcentajeCompra { get; set; }

        // =========================
        // MH: LineaDetalle/Impuesto/Exoneracion/MontoExoneracion
        // GoSocket: .../Exoneracion/MontoImpuesto
        // =========================
        [XmlElement("MontoImpuesto")]
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
        [XmlElement("NroLinDR")]
        public int NroLinDR { get; set; }

        /// <summary>
        /// GoSocket: /DTE/Documento/DscRcgGlobal[1]/CodigoDR
        /// MH: /FacturaElectronica/OtrosCargos/OtroCargo/TipoDocumentoOC
        /// (o el equivalente que estés trayendo para identificar el tipo de cargo)
        /// </summary>
        [XmlElement("CodigoDR")]
        public string? CodigoDR { get; set; }

        /// <summary>
        /// GoSocket: /DTE/Documento/DscRcgGlobal[1]/TpoMov
        /// MH: /FacturaElectronica/OtrosCargos/OtroCargo/TipoDocumentoOTROS (si aplica)
        /// o un mapeo tuyo para "tipo de movimiento" (Cargo/Descuento) según tu lógica.
        /// </summary>
        [XmlElement("TpoMov")]
        public string? TpoMov { get; set; }

        // ---------------- Identificación Tercero ----------------

        /// <summary>
        /// GoSocket: /DTE/Documento/DscRcgGlobal[1]/TipoIdentidadTercero
        /// MH: /FacturaElectronica/OtrosCargos/OtroCargo/IdentificacionTercero/Tipo
        /// </summary>
        [XmlElement("TipoIdentidadTercero")]
        public string? TipoIdentidadTercero { get; set; }

        /// <summary>
        /// GoSocket: /DTE/Documento/DscRcgGlobal[1]/NumeroIdentidadTercero
        /// MH: /FacturaElectronica/OtrosCargos/OtroCargo/IdentificacionTercero/Numero
        /// </summary>
        [XmlElement("NumeroIdentidadTercero")]
        public string? NumeroIdentidadTercero { get; set; }

        /// <summary>
        /// GoSocket: /DTE/Documento/DscRcgGlobal[1]/NombreTercero
        /// MH: /FacturaElectronica/OtrosCargos/OtroCargo/NombreTercero
        /// </summary>
        [XmlElement("NombreTercero")]
        public string? NombreTercero { get; set; }

        // ---------------- Detalle ----------------

        /// <summary>
        /// GoSocket: /DTE/Documento/DscRcgGlobal[1]/GlosaDR
        /// MH: /FacturaElectronica/OtrosCargos/OtroCargo/Detalle
        /// </summary>
        [XmlElement("GlosaDR")]
        public string? GlosaDR { get; set; }

        /// <summary>
        /// GoSocket: /DTE/Documento/DscRcgGlobal[1]/IndExeDR
        /// MH: (no existe como tal)
        /// Se usa como indicador (p.ej. exento/no gravado) según reglas de GoSocket / tu integración.
        /// </summary>
        [XmlElement("IndExeDR")]
        public string? IndExeDR { get; set; }

        /// <summary>
        /// GoSocket: /DTE/Documento/DscRcgGlobal[1]/ValorDR
        /// MH: /FacturaElectronica/OtrosCargos/OtroCargo/MontoCargo
        /// </summary>
        [XmlElement("ValorDR")]
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
        [XmlElement("Moneda")]
        public string? Moneda { get; set; }
        public bool ShouldSerializeMoneda() => !string.IsNullOrWhiteSpace(Moneda);

        // /DTE/Documento/Encabezado/Totales/FctConv
        [XmlElement("FctConv")]
        public decimal? FctConv { get; set; }
        public bool ShouldSerializeFctConv() => FctConv.HasValue;

        // /DTE/Documento/Encabezado/Totales/TotSubMonto[i]/MontoConcepto   (i = 1..8)
        [XmlElement("TotSubMonto")]
        public List<GosocketTotSubMonto> TotSubMonto { get; set; } = new();
        public bool ShouldSerializeTotSubMonto() => TotSubMonto != null && TotSubMonto.Count > 0;

        // /DTE/Documento/Encabezado/Totales/SubTotal
        [XmlElement("SubTotal")]
        public decimal? SubTotal { get; set; }
        public bool ShouldSerializeSubTotal() => SubTotal.HasValue;

        // /DTE/Documento/Encabezado/Totales/MntExe
        [XmlElement("MntExe")]
        public decimal? MntExe { get; set; }
        public bool ShouldSerializeMntExe() => MntExe.HasValue;

        // /DTE/Documento/Encabezado/Totales/ImporteNoGravado
        [XmlElement("ImporteNoGravado")]
        public decimal? ImporteNoGravado { get; set; }
        public bool ShouldSerializeImporteNoGravado() => ImporteNoGravado.HasValue;

        // /DTE/Documento/Encabezado/Totales/ExtraInfoTotal[@name='TotalNoSujeto']
        // (y otros extras de Totales)
        [XmlElement("ExtraInfoTotal")]
        public List<GosocketNameValue>? ExtraInfoTotal { get; set; }
        public bool ShouldSerializeExtraInfoTotal() => ExtraInfoTotal != null && ExtraInfoTotal.Count > 0;

        // /DTE/Documento/Encabezado/Totales/SaldoAnterior
        [XmlElement("SaldoAnterior")]
        public decimal? SaldoAnterior { get; set; }
        public bool ShouldSerializeSaldoAnterior() => SaldoAnterior.HasValue;

        // /DTE/Documento/Encabezado/Totales/MntDcto
        [XmlElement("MntDcto")]
        public decimal? MntDcto { get; set; }
        public bool ShouldSerializeMntDcto() => MntDcto.HasValue;

        // /DTE/Documento/Encabezado/Totales/MntBase
        [XmlElement("MntBase")]
        public decimal? MntBase { get; set; }
        public bool ShouldSerializeMntBase() => MntBase.HasValue;

        // /DTE/Documento/Encabezado/Impuestos[*]  (sí: va como "Impuestos" a nivel Encabezado)
        // En la práctica lo estás colgando dentro de Totales, pero el tag debe ser "Impuestos".
        [XmlElement("Impuestos")]
        public List<GosocketImpuestoTotal> Impuestos { get; set; } = new();
        public bool ShouldSerializeImpuestos() => Impuestos != null && Impuestos.Count > 0;

        // /DTE/Documento/Encabezado/Totales/MntImp
        [XmlElement("MntImp")]
        public decimal? MntImp { get; set; }
        public bool ShouldSerializeMntImp() => MntImp.HasValue;

        // /DTE/Documento/Encabezado/Totales/ImporteOtrosTributos
        [XmlElement("ImporteOtrosTributos")]
        public decimal? ImporteOtrosTributos { get; set; }
        public bool ShouldSerializeImporteOtrosTributos() => ImporteOtrosTributos.HasValue;

        // /DTE/Documento/Encabezado/Totales/MntRcgo
        [XmlElement("MntRcgo")]
        public decimal? MntRcgo { get; set; }
        public bool ShouldSerializeMntRcgo() => MntRcgo.HasValue;

       
        // /DTE/Documento/Encabezado/Totales/VlrPagar
        [XmlElement("VlrPagar")]
        public decimal? VlrPagar { get; set; }
        public bool ShouldSerializeVlrPagar() => VlrPagar.HasValue;
    }

    // Cada ítem representa un concepto distinto según el índice [1..8]
    public class GosocketTotSubMonto
    {
        // /DTE/Documento/Encabezado/Totales/TotSubMonto[i]/MontoConcepto
        [XmlElement("MontoConcepto")]
        public decimal MontoConcepto { get; set; }
    }

    public class GosocketImpuestoTotal
    {
        // /DTE/Documento/Encabezado/Impuestos[*]/Tipolmp
        [XmlElement("Tipolmp")]
        public string? Tipolmp { get; set; }
        public bool ShouldSerializeTipolmp() => !string.IsNullOrWhiteSpace(Tipolmp);

        // /DTE/Documento/Encabezado/Impuestos[*]/CodTasaImp
        [XmlElement("CodTasaImp")]
        public string? CodTasaImp { get; set; }
        public bool ShouldSerializeCodTasaImp() => !string.IsNullOrWhiteSpace(CodTasaImp);

        // /DTE/Documento/Encabezado/Impuestos[*]/MontoImp
        [XmlElement("MontoImp")]
        public decimal? MontoImp { get; set; }
        public bool ShouldSerializeMontoImp() => MontoImp.HasValue;
    }

    public class GosocketPago
    {
        // MH: <MedioPago><TipoMedioPago/> -> GoSocket: /Encabezado/IdDoc/Pagos/TipoPago
        [XmlElement("TipoPago")]
        public string TipoPago { get; set; }

        // MH: <MedioPago><MedioPagoOtros/> -> GoSocket: /Encabezado/IdDoc/Pagos/DescPago
        [XmlElement("DescPago")]
        public string DescPago { get; set; }

        // MH: <MedioPago><TotalMedioPago/> -> GoSocket: /Encabezado/IdDoc/Pagos/Monto
        [XmlElement("Monto")]
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
        public string TpoDocRef { get; set; }
        public string NumeroRef { get; set; }
        public string FechaRef { get; set; }
        public string CodRef { get; set; }
        public string GlosaRef { get; set; }
        public string RazonRef { get; set; }

        public bool ShouldSerializeGlosaRef() => !string.IsNullOrWhiteSpace(GlosaRef);
        public bool ShouldSerializeRazonRef() => !string.IsNullOrWhiteSpace(RazonRef);
    }

    public class GosocketOtros
    {
        [XmlElement("OtroTexto")]
        public List<GosocketExtraInfoDetalle> OtroTexto { get; set; } = new();

        [XmlElement("OtroContenido")]
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
        [XmlElement("campoString")]
        public List<GosocketCampoString> CampoString { get; set; } = new();

        [XmlElement("CustDetalle")]
        public List<GosocketCustDetalle> CustDetalle { get; set; } = new();

        public bool HasContent()
            => CampoString != null && CampoString.Count > 0 || CustDetalle != null && CustDetalle.Count > 0;

        public bool ShouldSerializeCampoString()
            => CampoString != null && CampoString.Count > 0;

        public bool ShouldSerializeCustDetalle()
            => CustDetalle != null && CustDetalle.Count > 0;
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
        public int NroLinDet { get; set; }

        [XmlElement("campoString")]
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
