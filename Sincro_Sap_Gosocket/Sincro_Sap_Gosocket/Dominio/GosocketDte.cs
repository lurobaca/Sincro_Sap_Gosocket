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
        public List<GosocketDetalleItem> Detalle { get; set; } = new();

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
        public List<GosocketNameValue> ExtrInfoDoc { get; set; } = new();

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
        public string Version { get; set; }
        public string Ambiente { get; set; }
        public string Tipo { get; set; }
        public string Numero { get; set; }

        public string NumeroInterno { get; set; }
        public string ContenidoTC { get; set; }
        public string TipoEmision { get; set; }
        public string Establecimiento { get; set; }

        // ISO 8601 string (recomendado). Si lo prefiere como DateTime, se puede.
        public string FechaEmis { get; set; }

        public bool ShouldSerializeNumeroInterno() => !string.IsNullOrWhiteSpace(NumeroInterno);
        public bool ShouldSerializeContenidoTC() => !string.IsNullOrWhiteSpace(ContenidoTC);
    }

    // =========================
    // EMISOR / RECEPTOR
    // =========================
    public class GosocketEmisor
    {
        public string NmbEmisor { get; set; }
        public string IDEmisor { get; set; }

        // Flex: extras de emisor
        [XmlElement("ExtrInfoEmisor")]
        public List<GosocketNameValue> ExtrInfoEmisor { get; set; } = new();

        public bool ShouldSerializeExtrInfoEmisor()
            => ExtrInfoEmisor != null && ExtrInfoEmisor.Count > 0;

        [XmlElement("DomFiscal")]
        public GosocketDomicilio DomFiscal { get; set; }

        [XmlElement("ContactoEmisor")]
        public GosocketContacto ContactoEmisor { get; set; }

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
        public List<GosocketNameValue> ExtrInfoDoc { get; set; } = new();

        public bool ShouldSerializeExtrInfoDoc()
            => ExtrInfoDoc != null && ExtrInfoDoc.Count > 0;

        [XmlElement("DomFiscalRcp")]
        public GosocketDomicilio DomFiscalRcp { get; set; }

        [XmlElement("ContactoReceptor")]
        public GosocketContacto ContactoReceptor { get; set; }

        public bool ShouldSerializeContactoReceptor()
            => ContactoReceptor != null && ContactoReceptor.HasContent();
    }

    public class GosocketDocRecep
    {
        public string TipoDoc { get; set; }
        public string NumDoc { get; set; }
    }

    public class GosocketDomicilio
    {
        public string Provincia { get; set; }
        public string Canton { get; set; }
        public string Distrito { get; set; }

        public string Barrio { get; set; }
        public string Calle { get; set; }
        public string OtrasSenas { get; set; }

        public bool ShouldSerializeBarrio() => !string.IsNullOrWhiteSpace(Barrio);
        public bool ShouldSerializeCalle() => !string.IsNullOrWhiteSpace(Calle);
        public bool ShouldSerializeOtrasSenas() => !string.IsNullOrWhiteSpace(OtrasSenas);
    }

    public class GosocketContacto
    {
        public string Telefono { get; set; }
        public string Correo { get; set; }

        public bool HasContent()
            => !string.IsNullOrWhiteSpace(Telefono) || !string.IsNullOrWhiteSpace(Correo);

        public bool ShouldSerializeTelefono() => !string.IsNullOrWhiteSpace(Telefono);
        public bool ShouldSerializeCorreo() => !string.IsNullOrWhiteSpace(Correo);
    }

    // =========================
    // DETALLE (líneas)
    // =========================
    public class GosocketDetalleItem
    {
        public int NroLinDet { get; set; }

        [XmlElement("CdgItem")]
        public GosocketCodigoItem CdgItem { get; set; }

        public decimal QtyItem { get; set; }
        public string UnmdItem { get; set; }

        public string IndIsrItem { get; set; }
        public string UnidadMedidaComercial { get; set; }
        public string DscItem { get; set; }

        public decimal PrcNetoItem { get; set; }
        public decimal MontoBrutoItem { get; set; }

        [XmlElement("SubDscto")]
        public GosocketSubDescuento SubDscto { get; set; }

        public bool ShouldSerializeSubDscto()
            => SubDscto != null && SubDscto.MontoDscto > 0m;

        // Flex: aquí marcamos autoconsumo: TipoTransaccion=03/05
        [XmlElement("ExtraInfoDetalle")]
        public List<GosocketNameValue> ExtraInfoDetalle { get; set; } = new();

        public bool ShouldSerializeExtraInfoDetalle()
            => ExtraInfoDetalle != null && ExtraInfoDetalle.Count > 0;

        [XmlElement("ImpuestosDet")]
        public List<GosocketImpuesto> ImpuestosDet { get; set; } = new();

        public bool ShouldSerializeImpuestosDet()
            => ImpuestosDet != null && ImpuestosDet.Count > 0;

        [XmlElement("Exoneracion")]
        public GosocketExoneracion Exoneracion { get; set; }

        public bool ShouldSerializeExoneracion()
            => Exoneracion != null && Exoneracion.HasContent();

        public decimal MontoTotLinea { get; set; }

        public bool ShouldSerializeIndIsrItem() => !string.IsNullOrWhiteSpace(IndIsrItem);
        public bool ShouldSerializeUnidadMedidaComercial() => !string.IsNullOrWhiteSpace(UnidadMedidaComercial);
    }

    public class GosocketCodigoItem
    {
        public string CABYS { get; set; }
        public string Codigo { get; set; }

        public bool ShouldSerializeCABYS() => !string.IsNullOrWhiteSpace(CABYS);
        public bool ShouldSerializeCodigo() => !string.IsNullOrWhiteSpace(Codigo);
    }

    public class GosocketSubDescuento
    {
        public decimal MontoDscto { get; set; }
        public string GlosaDscto { get; set; }

        public bool ShouldSerializeGlosaDscto() => !string.IsNullOrWhiteSpace(GlosaDscto);
    }

    // =========================
    // IMPUESTOS / EXONERACIÓN
    // =========================
    public class GosocketImpuesto
    {
        public string Codigo { get; set; }
        public string CodigoTarifaIVA { get; set; }
        public decimal TasaImp { get; set; }
        public decimal MontoImp { get; set; }

        public bool ShouldSerializeCodigoTarifaIVA() => !string.IsNullOrWhiteSpace(CodigoTarifaIVA);
    }

    public class GosocketExoneracion
    {
        public string TipoDoc { get; set; }
        public string NumDoc { get; set; }
        public string NomInstitucion { get; set; }
        public string FechaEmision { get; set; }
        public decimal PorExoneracion { get; set; }
        public decimal MontoExoneracion { get; set; }

        public bool HasContent()
            => !string.IsNullOrWhiteSpace(TipoDoc) || !string.IsNullOrWhiteSpace(NumDoc);

        public bool ShouldSerializeNomInstitucion() => !string.IsNullOrWhiteSpace(NomInstitucion);
        public bool ShouldSerializeFechaEmision() => !string.IsNullOrWhiteSpace(FechaEmision);
    }

    // =========================
    // TOTALES
    // =========================
    public class GosocketTotales
    {
        [XmlElement("TotSubMonto")]
        public List<GosocketTotSubMonto> TotSubMonto { get; set; } = new();

        public bool ShouldSerializeTotSubMonto()
            => TotSubMonto != null && TotSubMonto.Count > 0;

        public decimal SubTotal { get; set; }
        public decimal MntExe { get; set; }
        public decimal ImporteNoGravado { get; set; }
        public decimal SaldoAnterior { get; set; }
        public decimal MntDcto { get; set; }
        public decimal MntBase { get; set; }

        [XmlElement("Impuestos")]
        public List<GosocketImpuestoTotal> Impuestos { get; set; } = new();

        public bool ShouldSerializeImpuestos()
            => Impuestos != null && Impuestos.Count > 0;

        public decimal MntImp { get; set; }
        public decimal ImporteOtrosTributos { get; set; }
        public decimal MntRcgo { get; set; }

        [XmlElement("Pagos")]
        public List<GosocketPago> Pagos { get; set; } = new();

        public bool ShouldSerializePagos()
            => Pagos != null && Pagos.Count > 0;

        public decimal VlrPagar { get; set; }
    }

    public class GosocketTotSubMonto
    {
        public decimal MontoConcepto { get; set; }
    }

    public class GosocketImpuestoTotal
    {
        public string Codigo { get; set; }
        public string CodigoTarifaIVA { get; set; }
        public decimal MontoImp { get; set; }

        public bool ShouldSerializeCodigoTarifaIVA() => !string.IsNullOrWhiteSpace(CodigoTarifaIVA);
    }

    public class GosocketPago
    {
        public string MedioPago { get; set; }
        public decimal MontoPago { get; set; }
    }

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
        public List<GosocketNameValue> OtroTexto { get; set; } = new();

        [XmlElement("OtroContenido")]
        public List<GosocketNameValue> OtroContenido { get; set; } = new();

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
    // NAME/VALUE GENÉRICO (ExtrInfoDoc, ExtraInfoDetalle, OtroTexto, etc.)
    // =========================
    public class GosocketNameValue
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlText]
        public string Value { get; set; }
    }
}
