using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Dominio; 
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Sincro_Sap_Gosocket.Aplicacion.Servicios
{
    public sealed class TraductorXml : ITraductorXml
    {
        // 8 conceptos FE44 (serv/merc x grav/exe/exo/ns)
        // 8 conceptos FE44 (según ResumenFactura XML)
        private const int COD_TotalServGravados = 1001;
        private const int COD_TotalServExentos = 1002;
        private const int COD_TotalServExonerado = 1003;


        private const int COD_TotalServNoSujeto = 1006;//1007; 
        private const int COD_TotalMercanciasGravadas = 1007;//1004; 
        private const int COD_TotalMercanciasExentas = 1004;//1005;
        private const int COD_TotalMercExonerada = 1005;//1006; 
        private const int COD_TotalMercNoSujeta = 1008;


        // Si tenías un 9no concepto, agregalo aquí (ejemplo)
        private const int COD_OTRO_9 = 1009;

        public string Traducir(string tipoDocumento, DataTable datos)
        {
            if (datos == null) throw new ArgumentNullException(nameof(datos));
            if (datos.Rows.Count == 0) throw new InvalidOperationException("El SP no devolvió filas.");

            var r0 = datos.Rows[0];

            var idDocumento = GetString(r0, "CodSeguridad"); // o "Numero", o el campo que tenga 1000062509


            var dte = new GosocketDte
            {
                Documento = new GosocketDocumento
                {
                    ID = idDocumento,
                    Encabezado = ConstruirEncabezadoDesdeSp(tipoDocumento, r0),
                    Detalle = ConstruirDetalleDesdeSp(datos),
                    //Referencia = new List<GosocketReferencia>(),
                    Otros = null
                }
            };

 

            // Personalizados opcionales
            dte.Personalizados = ConstruirPersonalizados(r0);

            return SerializarUtf8SinBom(dte);
        }
        
        private static GosocketEncabezado ConstruirEncabezadoDesdeSp(string tipoDocumento, DataRow r0)
        {

            var Emisor_nombreComercial = GetString(r0, "Emisor_NombreComercial"); // MH: Emisor/NombreComercial
            var Receptor_nombreComercial = GetString(r0, "Receptor_NombreComercial"); // MH: Emisor/NombreComercial
            var Receptor_OtrasSenasExtranjero = GetString(r0, "Receptor_OtrasSenasExtranjero"); // MH: Emisor/NombreComercial

            var totales = ConstruirTotalesDesdeSp(r0);
            var impuestos = ConstruirImpuestosEncabezadoDesdeSp(r0, totales.MntImp);

            var encabezado = new GosocketEncabezado
            {
                IdDoc = new GosocketIdDoc
                {
                    Version = "4.4",
                    Ambiente = "Sandbox",
                    Tipo = MapTipoDocumento(GetString(r0, "TipoComprobante", tipoDocumento)),
                    Numero = GetString(r0, "CodSeguridad"),      // si viene NULL, lo genera su sistema/GoSocket
                    NumeroInterno =  GetString(r0, "CodSeguridad"),   // si usa el ERP interno
                    FechaEmis = ToIso8601(GetDate(r0, "Fecha")),   // su SP trae datetimeoffset textual
                    CondPago = GetString(r0, "CondicionVenta"),
                    Pagos = new List<GosocketPago>
                    {
                        new GosocketPago
                        {
                            TipoPago = GetString(r0, "MedioPago"),
                            DescPago = GetString(r0, "DescPago"),
                            Monto    = GetDecimal(r0, "ResumenFactura_TotalComprobante"),
                        }
                    },

                    //ExtrInfoDoc = new List<GosocketExtraInfoDetalle>
                    //{
                    //    new GosocketExtraInfoDetalle
                    //    {
                    //        // GoSocket: Encabezado/IdDoc/ExtrInfoDoc[@name='CodSeguridad']
                    //        // MH (referencia típica): IdDoc/CodSeguridad
                    //        Name = "CondicionVentaOtros",
                    //        Value = GetString(r0, "CondicionVenta")
                    //    }
                    //},

                    TermPagoCdg = GetString(r0, "PlazoCredito"),
                    ContenidoTC = GetString(r0, "Clave"),
                    TipoEmision = GetString(r0, "CodigoActividadEconomica"),
                    Establecimiento = GetString(r0, "Consecutivo"),
                },
                Emisor = new GosocketEmisor
                {
                    NmbEmisor = GetString(r0, "Emisor_Nombre", GetString(r0, "Emisor_NombreComercial")),
                    TipoContribuyente = (GetString(r0, "Emisor_Tipo") ?? "").Trim().PadLeft(2, '0'),
                    IDEmisor = GetString(r0, "Emisor_Numero"), 
                   
                    // GoSocket: Encabezado/Emisor/NombreEmiso/PrimerNombre
                    NombreEmisor = string.IsNullOrWhiteSpace(Emisor_nombreComercial)
                                    ? null
                                    : new GosocketNombreEmisor { PrimerNombre = Emisor_nombreComercial },
                    DomFiscal = new GosocketDomFiscal
                    {
                        Calle = GetString(r0, "Emisor_OtrasSenas"),
                        Departamento = GetString(r0, "Emisor_Provincia"),
                        Distrito = GetString(r0, "Emisor_Distrito"),
                        Ciudad = GetString(r0, "Emisor_Canton"),
                        Municipio = GetString(r0, "Emisor_Barrio"),                     
                        //Referencia = GetString(r0, "Emisor_OtrasSenas")
                    },
                    ContactoEmisor = new GosocketContactoEmisor
                    {
                        Extension = GetString(r0, "Emisor_CodigoPais"),
                        Telefono = GetString(r0, "Emisor_NumTelefono"),
                        eMail = NormalizeEmail(GetString(r0, "Emisor_CorreoElectronico"))
                    },
                    //ExtrInfoEmisor = new List<GosocketExtraInfoDetalle>
                    //{
                    //    new GosocketExtraInfoDetalle
                    //    {
                    //        // GoSocket: Encabezado/Emisor/ExtrInfoEmisor[@name='Registrofiscal8707']
                    //        // MH (referencia típica): Emisor/Identificacion/Numero (o dato fiscal interno que usted mapea)
                    //        Name = "Registrofiscal8707",
                    //        Value = GetString(r0, "Emisor_RegistroFiscal8707")
                    //    }
                    //},
                },
                Receptor = new GosocketReceptor
                {  
                    NmbRecep = GetString(r0, "Receptor_Nombre"),                 
                    DocRecep = new GosocketDocRecep
                    {
                        // En su SP "Receptor_Tipo" ya viene como número (2, etc.).
                        TipoDocRecep = GetString(r0, "Receptor_Tipo"),
                        NroDocRecep = GetString(r0, "Receptor_Numero")
                    },
 
                    // GoSocket: Encabezado/Emisor/NombreEmiso/PrimerNombre
                    NombreRecep = string.IsNullOrWhiteSpace(Receptor_nombreComercial )
                                    ? null
                                    : new GosocketNombreRecep{ PrimerNombre = Receptor_nombreComercial },

                    RegimenContableR = GetString(r0, "CodigoActividadReceptor"),

                    DomFiscalRcp = new GosocketDomFiscal
                    {
                        Departamento = GetString(r0, "Receptor_Provincia"),
                        Distrito = GetString(r0, "Receptor_Canton"),
                        Ciudad = GetString(r0, "Receptor_Distrito"),
                        Municipio = GetString(r0, "Receptor_Barrio"),
                        Calle = GetString(r0, "Receptor_OtrasSenas"),
                        //Referencia = GetString(r0, "Receptor_OtrasSenasExtranjero")
                    },

                    LugarRecep = string.IsNullOrWhiteSpace(Receptor_OtrasSenasExtranjero)
                                    ? null
                                    : new GosocketLugarRecep { Calle = Receptor_OtrasSenasExtranjero },

                    ContactoReceptor = new GosocketContactoReceptor
                    {
                        Extension = GetString(r0, "Receptor_CodigoPais"),
                        Telefono =  GetString(r0, "Receptor_NumTelefono"),
                        eMail = NormalizeEmail(GetString(r0, "Receptor_CorreoElectronico"))
                    },
                    ExtrInfoDoc = new List<GosocketExtraInfoDetalle>()
                },
                Totales = totales,             
                Impuestos = impuestos
                //ExtrInfoDoc = new List<GosocketExtraInfoDetalle>(),
            };

            // Registro fiscal 8707 (si viene)
            AddExtra(encabezado.Emisor.ExtrInfoEmisor, "Registrofiscal8707", GetString(r0, "Emisor_Registrofiscal8707"));

            // Condición venta / plazo crédito como extras (si su XML genérico lo requiere así)
            //AddExtra(encabezado.Receptor.ExtrInfoDoc, "CondicionVenta", GetString(r0, "CondicionVenta"));
            //AddExtra(encabezado.Receptor.ExtrInfoDoc, "PlazoCredito", GetString(r0, "PlazoCredito"));
            //AddExtra(encabezado.ExtrInfoDoc, "CodigoActividadEconomica", GetString(r0, "CodigoActividadEconomica"));

            return encabezado;
        }

       


        #region "DetalleComp"
        private static List<GosocketDetalle> ConstruirDetalleDesdeSp(DataTable dt)
        {
            var detalles = new List<GosocketDetalle>(dt.Rows.Count);

            foreach (DataRow row in dt.Rows)
            {
                var item = ConstruirDetalle(row);
                detalles.Add(item);
            }

            return detalles;
        }

        private static GosocketDetalle ConstruirDetalle(DataRow row)
        {
            var codigos = ConstruirCodigosItem(row);
            var extraInfo = ConstruirExtraInfoDetalle(row);
            var SubRecargo = ConstruirSubRecargo(row);
            var partida = GetString(row, "DetalleServicio_PartidaArancelaria");

            var detalle = new GosocketDetalle
            {
                NroLinDet = GetInt(row, "DetalleServicio_NumeroLinea"), 
                TpoListaItem = (string.IsNullOrWhiteSpace(partida) || partida.Trim() == "0") ? null : partida.Trim(),
                CdgItem = codigos,
                DscItem = GetString(row, "DetalleServicio_Detalle"),
                QtyItem = GetDecimal(row, "DetalleServicio_Cantidad"),
                UnmdItem = GetString(row, "DetalleServicio_UnidadMedida"),
                IndListaItem = null,//GetString(row, "DetalleServicio_TipoTransaccion"),
                UnidadMedidaComercial = GetString(row, "DetalleServicio_UnidadMedidaComercial"),          
                PrcNetoItem = GetDecimal(row, "DetalleServicio_PrecioUnitario"),
                MontoBrutoItem = GetDecimal(row, "DetalleServicio_MontoTotal"),
                MontoNetoItem = GetDecimal(row, "DetalleServicio_SubTotal"),
                RecargoMonto = GetDecimal(row, "DetalleServicio_BaseImponible", 0m),
                ImpuestosDet = new List<GosocketImpuestosDet>(),
                SubRecargo= SubRecargo,


                MontoTotalItem = GetDecimal(row, "DetalleServicio_MontoTotalLinea"),
                ExtraInfoDetalle = extraInfo
            };

            AplicarDescuento(detalle, row);


            AplicarImpuesto(detalle, row);

            // Esto ya lo estabas metiendo como extra; lo dejo en una sola línea clara.
         

            // DetalleComp (surtido) - solo si hay datos de surtido en la fila
            var detalleComp = ConstruirDetalleCompSiAplica(row);
            if (detalleComp != null)
                detalle.DetalleComp = detalleComp;

            return detalle;
        }

        private static List<GosocketCdgItem> ConstruirCodigosItem(DataRow row)
        {
            var codigos = new List<GosocketCdgItem>();

            var cabys = GetString(row, "DetalleServicio_CodigoProductoServicio");
            var vin = GetString(row, "DetalleServicio_NumeroVINoSerie");

            var tipoCodComercial = GetStringOrDefault(row, "DetalleServicio_TipoCodigo", "04");
            var valorCodComercial = GetStringOrDefault(row, "DetalleServicio_Codigo", "").Trim();

            AddCodigoSiTieneValor(codigos, tipoCodComercial, valorCodComercial);
            AddCodigoSiTieneValor(codigos, "CABYS", cabys);    
            AddCodigoSiTieneValor(codigos, "VIN", vin);

            return codigos;
        }
        private static GosocketSubRecargo ConstruirSubRecargo(DataRow row)
        {
            var mnt = GetDecimal(row, "DetalleServicio_ImpuestoNeto", 0m); // <-- ajustá el nombre real de tu columna

            if (mnt <= 0m) return null;

            return new GosocketSubRecargo
            {

                MntRecargo = mnt
            };
        }
        private static List<GosocketExtraInfoDetalle> ConstruirExtraInfoDetalle(DataRow row)
        {
            var extras = new List<GosocketExtraInfoDetalle>();
            AddExtraSiTieneValor(extras, "ImpuestoAsumido", GetString(row, "DetalleServicio_IVACobradoFabrica"));
            AddExtraSiTieneValorNoCero(extras, "RegistroMedicamento", GetString(row, "DetalleServicio_RegistroMedicamento"));
            AddExtraSiTieneValorNoCero(extras, "FormaFarmaceutica", GetString(row, "DetalleServicio_FormaFarmaceutica"));

            return extras;
        }

        private static void AplicarDescuento(GosocketDetalle item, DataRow row)
        {
            var montoDescuento = GetDecimal(row, "DetalleServicio_MontoDescuento", 0m);
            if (montoDescuento <= 0m) return;

            item.SubDscto = new GosocketSubDscto
            {
                MntDscto = montoDescuento,
                PctDscto = GetString(row, "DetalleServicio_CodigoDescuento"),
                GlosaDscto = GetString(row, "DetalleServicio_NaturalezaDescuento")
            };
        }

        private static void AplicarImpuesto(GosocketDetalle item, DataRow row)
        {
            var montoImp = GetDecimal(row, "DetalleServicio_ImpuestoMonto", 0m);
            if (montoImp <= 0m) return;

            item.ImpuestosDet.Add(new GosocketImpuestosDet
            {
                TipoImp = GetString(row, "DetalleServicio_ImpuestoCodigo", "01"),
                //CodImp = GetString(row, "DetalleServicio_ImpuestoCodigo", "01"),
                CodTasaImp = GetString(row, "DetalleServicio_ImpuestoCodigoTarifa"),
                TasaImp = GetDecimal(row, "DetalleServicio_ImpuestoTarifa", 0m),
                MontoImp = montoImp
            });

 
        }

        private static GosocketDetalleComp? ConstruirDetalleCompSiAplica(DataRow row)
        { 
            var qtyParte = GetDecimal(row, "CantidadSurtido", 0m);
            if (qtyParte <= 0m) return null;

            var parte = new GosocketParte
            {
                // Si luego agregás CdgParte, lo hacés aquí igual que CdgItem
                CdgParte = new List<GosocketCdgParte>(),

                QtyItemParte = qtyParte,
                UnmdItemParte = GetString(row, "UnidadMedidaSurtido"),
                UnidadComercialParte = GetString(row, "UnidadMedidaComercialSurtido"),
                DscItemParte = GetString(row, "DetalleSurtido"),
                PrcNetoParte = GetDecimal(row, "PrecioUnitarioSurtido"),
                MontoBrutoParte = GetDecimal(row, "MontoTotalSurtido"),

                MontoNetoParte = GetDecimal(row, "MontoNetoSurtido", 0m),      // si existe
                MontoTotalParte = GetDecimal(row, "MontoTotalParte", 0m)       // si existe
            };
            

            AplicarDescuentoParte(parte, row);
            AplicarImpuestoParte(parte, row);

            return new GosocketDetalleComp
            {
                Parte = new List<GosocketParte> { parte }
            };
        }

        private static void AplicarDescuentoParte(GosocketParte parte, DataRow row)
        {
            var montoDcto = GetDecimal(row, "MontoDescuentoSurtido", 0m);
            if (montoDcto <= 0m) return;

            var tipoDcto = GetString(row, "CodigoDescuentoSurtido");

            parte.SubDsctoParte = new GosocketSubDsctoParte
            {
                MntDscto = montoDcto,
                //TipoDscto = string.IsNullOrWhiteSpace(tipoDcto) ? "01" : tipoDcto
                // GlosaDscto = GetString(row, "GlosaDescuentoSurtido") // si existe
            };
        }

        private static void AplicarImpuestoParte(GosocketParte parte, DataRow row)
        {
            var montoImp = GetDecimal(row, "ImpuestoParte_MontoImp", 0m);
            if (montoImp <= 0m) return;

            var tipoImp = GetString(row, "ImpuestoParte_TipoImp");
            if (string.IsNullOrWhiteSpace(tipoImp)) return;

            parte.ImpuestosParte = new GosocketImpuestosParte
            {
                TipoImp = tipoImp,
                CodTasaImp = GetString(row, "ImpuestoParte_CodTasaImp"),
                TasaImp = GetDecimal(row, "ImpuestoParte_TasaImp", 0m),
                CuotaImp = montoImp
            };
        }


        private static void AddCodigoSiTieneValor(List<GosocketCdgItem> codigos, string tipo, string valor)
        {
            if (string.IsNullOrWhiteSpace(tipo)) return;
            if (string.IsNullOrWhiteSpace(valor)) return;

            codigos.Add(new GosocketCdgItem { TpoCodigo = tipo, VlrCodigo = valor });
        }

       
        private static void AddExtraSiTieneValor(List<GosocketExtraInfoDetalle> extras,string name,string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            extras.Add(new GosocketExtraInfoDetalle
            {
                Name = name,
                Value = value.Trim()
            });
        }
        private static void AddExtraSiTieneValorNoCero(
    List<GosocketExtraInfoDetalle> extras,
    string name,
    string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            var v = value.Trim();

            // Ignorar ceros comunes
            if (v == "0" || v == "0.0" || v == "0.00" || v == "0.000000") return;

            extras.Add(new GosocketExtraInfoDetalle
            {
                Name = name,
                Value = v
            });
        }
        private static string GetStringOrDefault(DataRow row, string columnName, string defaultValue)
        {
            return row.Table.Columns.Contains(columnName)
                ? (row[columnName]?.ToString() ?? defaultValue)
                : defaultValue;
        }

        #endregion

        private static List<GosocketImpuestoTotal> ConstruirImpuestosEncabezadoDesdeSp(DataRow r0, decimal? mntImp)
        {
            var lista = new List<GosocketImpuestoTotal>();
            if (mntImp <= 0m) return lista;

            // Si tiene código de tarifa en SP, úselo; si no, NO serialice el nodo (null)
            var codTasa = GetString(r0, "DetalleServicio_ImpuestoCodigoTarifa"); // ajuste al nombre real
             var ImpuestoCodigo = GetString(r0, "DetalleServicio_ImpuestoCodigo"); // ajuste al nombre real
            lista.Add(new GosocketImpuestoTotal
            {
                TipoImp = ImpuestoCodigo,             
                CodTasaImp = string.IsNullOrWhiteSpace(codTasa) ? null : codTasa,
                MontoImp = mntImp
            });

            return lista;
        }
        //private static GosocketTotales ConstruirTotalesDesdeSp(DataRow r0)
        //{
        //    var tot = new GosocketTotales
        //    {
        //        Moneda = GetString(r0, "ResumenFactura_CodigoMoneda"),
        //        FctConv = GetDecimal(r0, "ResumenFactura_TipoCambio", 0m),
        //        SubTotal = GetDecimal(r0, "ResumenFactura_TotalVenta", 0m),
        //        MntDcto = GetDecimal(r0, "ResumenFactura_TotalDescuentos", 0m),
        //        MntBase = GetDecimal(r0, "ResumenFactura_TotalVentaNeta", 0m),
        //        MntImp = GetDecimal(r0, "ResumenFactura_TotalImpuesto", 0m),
        //        VlrPagar = GetDecimal(r0, "ResumenFactura_TotalComprobante", GetDecimal(r0, "ResumenFactura_TotalVentaNeta", 0m) + GetDecimal(r0, "ResumenFactura_TotalImpuesto", 0m)),

        //        VlrPalabras="",
        //        MntExe = 0m,
        //        ImporteNoGravado = 0m,
        //        SaldoAnterior = GetDecimal(r0, "ResumenFactura_TotalVentaNeta", 0m),
        //        ImporteOtrosTributos = 0m,
        //        MntRcgo = 0m,
        //        TotSubMonto = new List<GosocketTotSubMonto>()
        //    };

        //    // Subtotal por concepto (si quiere alimentar el [1..8] con TotalGravado por ejemplo)
        //    var totalGravado = GetDecimal(r0, "ResumenFactura_TotalGravado", 0m);
        //    if (totalGravado > 0m)
        //        tot.TotSubMonto.Add(new GosocketTotSubMonto { MontoConcepto = totalGravado });

        //    //// Desglose impuesto
        //    //var totalImp = tot.MntImp;
        //    //if (totalImp > 0m)
        //    //{
        //    //    tot.Impuesto.Add(new GosocketImpuestoTotal
        //    //    {
        //    //        Tipolmp = "01",
        //    //        CodTasaImp = null, // su SP trae ImpuestoCodigoTarifa como NULL en capturas
        //    //        MontoImp = totalImp
        //    //    });
        //    //}


        //    return tot;
        //}
        //private static GosocketTotales ConstruirTotalesDesdeSp(DataRow r0)
        //{
        //    var tot = new GosocketTotales
        //    {
        //        Moneda = GetString(r0, "ResumenFactura_CodigoMoneda"),
        //        FctConv = GetDecimal(r0, "ResumenFactura_TipoCambio", 0m),
        //        SubTotal = GetDecimal(r0, "ResumenFactura_TotalVenta", 0m),
        //        MntDcto = GetDecimal(r0, "ResumenFactura_TotalDescuentos", 0m),
        //        MntBase = GetDecimal(r0, "ResumenFactura_TotalVentaNeta", 0m),
        //        MntImp = GetDecimal(r0, "ResumenFactura_TotalImpuesto", 0m),
        //        VlrPagar = GetDecimal(
        //            r0,
        //            "ResumenFactura_TotalComprobante",
        //            GetDecimal(r0, "ResumenFactura_TotalVentaNeta", 0m) + GetDecimal(r0, "ResumenFactura_TotalImpuesto", 0m)
        //        ),

        //        VlrPalabras = "",
        //        MntExe = 0m,
        //        ImporteNoGravado = 0m,
        //        SaldoAnterior = GetDecimal(r0, "ResumenFactura_TotalVentaNeta", 0m),
        //        ImporteOtrosTributos = 0m,
        //        MntRcgo = null
        //    };

        //    // ✅ TotSubMonto[1..8] en el orden correcto:
        //    var sGrav = GetDecimal(r0, "ResumenFactura_TotalServGravados", 0m);
        //    var sExe = GetDecimal(r0, "ResumenFactura_TotalServExentos", 0m);
        //    var sExo = GetDecimal(r0, "ResumenFactura_TotalServExonerado", 0m);
        //    var sNS = GetDecimal(r0, "ResumenFactura_TotalServNoSujeto", 0m);

        //    var mGrav = GetDecimal(r0, "ResumenFactura_TotalMercanciasGravadas", 0m);
        //    var mExe = GetDecimal(r0, "ResumenFactura_TotalMercanciasExentas", 0m);
        //    var mExo = GetDecimal(r0, "ResumenFactura_TotalMercanciasExonerada", 0m);
        //    var mNS = GetDecimal(r0, "ResumenFactura_TotalMercanciasNoSujeto", 0m);

        //    // Si NO hay nada que reportar, no serialice TotSubMonto
        //    if (sGrav + sExe + sExo + sNS + mGrav + mExe + mExo + mNS > 0m)
        //    {
        //        tot.TotSubMonto = new List<GosocketTotSubMonto>
        //{
        //    new GosocketTotSubMonto { MontoConcepto = sGrav }, // [1] Serv Gravados
        //    new GosocketTotSubMonto { MontoConcepto = sExe  }, // [2] Serv Exentos
        //    new GosocketTotSubMonto { MontoConcepto = sExo  }, // [3] Serv Exonerado
        //    new GosocketTotSubMonto { MontoConcepto = sNS   }, // [4] Serv No Sujeto
        //    new GosocketTotSubMonto { MontoConcepto = mGrav }, // [5] Merc Gravadas
        //    new GosocketTotSubMonto { MontoConcepto = mExe  }, // [6] Merc Exentas
        //    new GosocketTotSubMonto { MontoConcepto = mExo  }, // [7] Merc Exonerada
        //    new GosocketTotSubMonto { MontoConcepto = mNS   }  // [8] Merc No Sujeto
        //};
        //    }
        //    else
        //    {
        //        tot.TotSubMonto = null; // evita nodo vacío
        //    }


        //    AddExtra(tot.ExtraInfoTotal, "TotalNoSujeto", GetString(r0, "ResumenFactura_TotalNoSujeto"));
        //    AddExtra(tot.ExtraInfoTotal, "TotalImpAsumFabrica", GetString(r0, "DetalleServicio_IVACobradoFabrica"));


        //    return tot;
        //}

        private static GosocketTotales ConstruirTotalesDesdeSp(DataRow r0)
        {
            var tot = new GosocketTotales
            {
                Moneda = GetString(r0, "ResumenFactura_CodigoMoneda"),
                FctConv = GetDecimal(r0, "ResumenFactura_TipoCambio", 0m),
                SubTotal = GetDecimal(r0, "ResumenFactura_TotalVenta", 0m),
                MntDcto = GetDecimal(r0, "ResumenFactura_TotalDescuentos", 0m),
                MntBase = GetDecimal(r0, "ResumenFactura_TotalVentaNeta", 0m),
                MntImp = GetDecimal(r0, "ResumenFactura_TotalImpuesto", 0m),
                VlrPagar = GetDecimal(
                    r0,
                    "ResumenFactura_TotalComprobante",
                    GetDecimal(r0, "ResumenFactura_TotalVentaNeta", 0m) + GetDecimal(r0, "ResumenFactura_TotalImpuesto", 0m)
                ),
                VlrPalabras = "",
                MntExe = 0m,
                ImporteNoGravado = 0m,
                SaldoAnterior = GetDecimal(r0, "ResumenFactura_TotalVentaNeta", 0m),
                ImporteOtrosTributos = 0m,
                MntRcgo = null
            };

            var TotalServGravados = GetDecimal(r0, "ResumenFactura_TotalServGravados", 0m);
            var TotalServExentos = GetDecimal(r0, "ResumenFactura_TotalServExentos", 0m);
            var TotalServExonerado = GetDecimal(r0, "ResumenFactura_TotalServExonerado", 0m);
            var TotalServNoSujeto = GetDecimal(r0, "ResumenFactura_TotalServNoSujeto", 0m);

            var TotalMercanciasGravadas = GetDecimal(r0, "ResumenFactura_TotalMercanciasGravadas", 0m);
            var TotalMercanciasExentas = GetDecimal(r0, "ResumenFactura_TotalMercanciasExentas", 0m);
            var TotalMercanciasExonerada = GetDecimal(r0, "ResumenFactura_TotalMercanciasExonerada", 0m);
            var TotalMercanciasNoSujeto = GetDecimal(r0, "ResumenFactura_TotalMercanciasNoSujeto", 0m);

            // Si ocupás un 9no, sacalo del SP (ejemplo)
            // var otro9 = GetDecimal(r0, "ResumenFactura_AlgoMas", 0m);

            var list = new List<GosocketTotSubMonto>();

            
            AddTotSubMonto(list, 0, TotalServGravados); // TotalServGravados
            AddTotSubMonto(list, 0, TotalServExentos); // TotalServExentos
            AddTotSubMonto(list, 0, TotalServExonerado); // TotalServExonerado
            AddTotSubMonto(list, 0, TotalMercanciasGravadas); // TotalMercanciasGravadas
            AddTotSubMonto(list, 0, TotalMercanciasExentas); // TotalMercanciasExentas
            AddTotSubMonto(list, 0, TotalMercanciasExonerada); // TotalMercExonerada           
            AddTotSubMonto(list, 0, TotalServNoSujeto); // TotalServNoSujeto
            AddTotSubMonto(list, 0, TotalMercanciasNoSujeto); // TotalMercanciasNoSujeto

            // AddTotSubMonto(list, COD_OTRO_9, otro9);  // [9] si aplica

            tot.TotSubMonto = list.Count > 0 ? list : null;

            AddExtra(tot.ExtraInfoTotal, "TotalNoSujeto", GetString(r0, "ResumenFactura_TotalNoSujeto"));
            AddExtra(tot.ExtraInfoTotal, "TotalImpAsumFabrica", GetString(r0, "DetalleServicio_IVACobradoFabrica"));

            return tot;
        }

        private static void AddTotSubMonto(List<GosocketTotSubMonto> list, int codigo, decimal monto)
        {
            // Si querés que salgan aunque sea 0, quitá este if.
            //if (monto == 0m) return;

            list.Add(new GosocketTotSubMonto
            {
                Tipo = codigo,
                CodTipoMonto = codigo,
                MontoConcepto = monto
            });
        }

        private static GosocketPersonalizados ConstruirPersonalizados(DataRow r0)
        {
            var p = new GosocketPersonalizados();

            var SistemaEmisor = GetString(r0, "SistemaEmisor");  
            if (!string.IsNullOrWhiteSpace(SistemaEmisor))
                p.CampoString.Add(new GosocketCampoString { Name = "SistemaEmisor", Value = SistemaEmisor });

            var DireccionReceptor = GetString(r0, "Receptor_OtrasSenas");  
            if (!string.IsNullOrWhiteSpace(DireccionReceptor))
                p.CampoString.Add(new GosocketCampoString { Name = "DireccionReceptor", Value = DireccionReceptor });

            var CorreosReceptor = GetString(r0, "Receptor_CorreoElectronico");  
            if (!string.IsNullOrWhiteSpace(CorreosReceptor))
                p.CampoString.Add(new GosocketCampoString { Name = "CorreosReceptor", Value = CorreosReceptor });

            var Observaciones = GetString(r0, "Observaciones");
            if (!string.IsNullOrWhiteSpace(CorreosReceptor))
                p.CampoString.Add(new GosocketCampoString { Name = "Observaciones", Value = CorreosReceptor });

            var Adenda_Tipo = GetString(r0, "Adenda_Tipo");
            if (!string.IsNullOrWhiteSpace(CorreosReceptor))
                p.CampoString.Add(new GosocketCampoString { Name = "Adenda_Tipo", Value = Adenda_Tipo });

            var MontoEnLetras = GetString(r0, "MontoEnLetras");
            if (!string.IsNullOrWhiteSpace(CorreosReceptor))
                p.CampoString.Add(new GosocketCampoString { Name = "MontoEnLetras", Value = MontoEnLetras });

            var Param18 = GetString(r0, "Param18");
            if (!string.IsNullOrWhiteSpace(CorreosReceptor))
                p.CampoString.Add(new GosocketCampoString { Name = "Param18", Value = Param18 });
            

            return p.CampoString.Count > 0 ? p : null;
        }


        // -------------------- SERIALIZAR --------------------
        private static string SerializarUtf8SinBom(GosocketDte dte)
        {
            var serializer = new XmlSerializer(typeof(GosocketDte));
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
                OmitXmlDeclaration = false
            };

            using var sw = new StringWriterWithEncoding(settings.Encoding);
            using var xw = XmlWriter.Create(sw, settings);
            serializer.Serialize(xw, dte);
            return sw.ToString();
        }

        private sealed class StringWriterWithEncoding : StringWriter
        {
            private readonly Encoding _encoding;
            public StringWriterWithEncoding(Encoding encoding) => _encoding = encoding;
            public override Encoding Encoding => _encoding;
        }

        // -------------------- HELPERS --------------------
        private static void AddExtra(List<GosocketExtraInfoDetalle> list, string name, string value)
        {
            if (list == null) return;
            if (string.IsNullOrWhiteSpace(value)) return;
            list.Add(new GosocketExtraInfoDetalle { Name = name, Value = value });
        }
     private static void AddExtra(List<GosocketNameValue> list, string name, string value)
        {
            if (list == null) return;
            if (string.IsNullOrWhiteSpace(value)) return;
            list.Add(new GosocketNameValue { Name = name, Value = value });
        }

        private static string GetString(DataRow r, string col, string defaultValue = "")
        {
            if (r.Table.Columns.Contains(col) && r[col] != DBNull.Value)
                return Convert.ToString(r[col])?.Trim() ?? defaultValue;

            return defaultValue;
        }

        private static int GetInt(DataRow r, string col, int defaultValue = 0)
        {
            if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value)
                return defaultValue;

            return int.TryParse(Convert.ToString(r[col]), out var v) ? v : defaultValue;
        }

        private static decimal GetDecimal(DataRow r, string col, decimal defaultValue = 0m)
        {
            if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value)
                return defaultValue;

            var s = Convert.ToString(r[col]);
            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                return v;

            if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out v))
                return v;

            return defaultValue;
        }

        private static DateTimeOffset? GetDate(DataRow r, string col)
        {
            if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value)
                return null;

            // El SP parece devolver string con offset (2025-12-30T22:43:33-06:00)
            var s = Convert.ToString(r[col])?.Trim();
            if (DateTimeOffset.TryParse(s, out var dto)) return dto;

            // fallback
            if (DateTimeOffset.TryParse(Convert.ToString(r[col]), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out dto))
                return dto;

            return null;
        }

        private static string ToIso8601(DateTimeOffset? dto)
            => dto.HasValue ? dto.Value.ToString("yyyy-MM-ddTHH:mm:sszzz") : DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");

        private static string NormalizeEmail(string email)
            => string.IsNullOrWhiteSpace(email) ? "" : email.Trim().ToLowerInvariant();

        private static string MapTipoDocumento(string tipo)
        {
            // Ajuste según su convención real en GoSocket:
            // Si GoSocket espera el "tipo FE44", manténgalo como "01" para FE
            var t = (tipo ?? "").Trim().ToUpperInvariant();
            return t switch
            {
                "FE" => "01",
                "NC" => "03",
                "ND" => "02",
                "FEC" => "08",
                _ => "01"
            };
        }
    }
} 