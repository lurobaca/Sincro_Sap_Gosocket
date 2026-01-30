using Sincro_Sap_Gosocket.Aplicacion.Interfaces;
using Sincro_Sap_Gosocket.Dominio; 
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Sincro_Sap_Gosocket.Aplicacion.Servicios
{
    public sealed class TraductorXml : ITraductorXml
    {
        public string Traducir(string tipoDocumento, DataTable datos)
        {
            if (datos == null) throw new ArgumentNullException(nameof(datos));
            if (datos.Rows.Count == 0) throw new InvalidOperationException("El SP no devolvió filas.");

            var r0 = datos.Rows[0];

            var dte = new GosocketDte
            {
                Documento = new GosocketDocumento
                {
                    Encabezado = ConstruirEncabezadoDesdeSp(tipoDocumento, r0),
                    Detalle = ConstruirDetalleDesdeSp(datos),
                    Referencia = new List<GosocketReferencia>(),
                    Otros = null
                }
            };

            // Totales: tomarlos del ResumenFactura_* (más confiable)
            dte.Documento.Encabezado.Totales = ConstruirTotalesDesdeSp(r0);

            // Personalizados opcionales
            dte.Personalizados = ConstruirPersonalizados(r0);

            return SerializarUtf8SinBom(dte);
        }

      


        private static GosocketEncabezado ConstruirEncabezadoDesdeSp(string tipoDocumento, DataRow r0)
        {

            var Emisor_nombreComercial = GetString(r0, "Emisor_NombreComercial"); // MH: Emisor/NombreComercial
            var Receptor_nombreComercial = GetString(r0, "Receptor_NombreComercial"); // MH: Emisor/NombreComercial
            var Receptor_OtrasSenasExtranjero = GetString(r0, "Receptor_OtrasSenasExtranjero"); // MH: Emisor/NombreComercial

            var encabezado = new GosocketEncabezado
            {
                IdDoc = new GosocketIdDoc
                {
                    Version = "4.4",
                    Ambiente = "Sandbox",
                    Tipo = MapTipoDocumento(GetString(r0, "TipoComprobante", tipoDocumento)),
                    Numero = null,      // si viene NULL, lo genera su sistema/GoSocket
                    NumeroInterno =  GetString(r0, "CodSeguridad"),   // si usa el ERP interno
                    FechaEmis = ToIso8601(GetDate(r0, "Fecha")),   // su SP trae datetimeoffset textual
                    CondPago = GetString(r0, "CondicionVenta"),

                    ExtrInfoDoc= new List<GosocketExtraInfoDetalle>
                    {
                        new GosocketExtraInfoDetalle
                        {
                            // GoSocket: Encabezado/IdDoc/ExtrInfoDoc[@name='CodSeguridad']
                            // MH (referencia típica): IdDoc/CodSeguridad
                            Name = "CondicionVentaOtros",
                            Value = GetString(r0, "CondicionVenta")
                        }
                    },

                    TermPagoCdg = GetString(r0, "PlazoCredito"),
                    ContenidoTC = GetString(r0, "Clave"),
                    TipoEmision = GetString(r0, "CodigoActividadEconomica"),
                    Establecimiento = GetString(r0, "Consecutivo"),
                },
                Emisor = new GosocketEmisor
                {
                    NmbEmisor = GetString(r0, "Emisor_Nombre", GetString(r0, "Emisor_NombreComercial")),
                    TipoContribuyente = GetString(r0, "Emisor_Tipo"),                    
                    IDEmisor = GetString(r0, "Emisor_Numero"), 
                    ExtrInfoEmisor = new List<GosocketExtraInfoDetalle>
                    {
                        new GosocketExtraInfoDetalle
                        {
                            // GoSocket: Encabezado/Emisor/ExtrInfoEmisor[@name='Registrofiscal8707']
                            // MH (referencia típica): Emisor/Identificacion/Numero (o dato fiscal interno que usted mapea)
                            Name = "Registrofiscal8707",
                            Value = GetString(r0, "Emisor_RegistroFiscal8707")
                        } 
                    },
                    // GoSocket: Encabezado/Emisor/NombreEmiso/PrimerNombre
                    NombreEmiso = string.IsNullOrWhiteSpace(Emisor_nombreComercial)
                                    ? null
                                    : new GosocketNombreEmisor { PrimerNombre = Emisor_nombreComercial },
                    DomFiscal = new GosocketDomFiscal
                    {
                        Departamento = GetString(r0, "Emisor_Provincia"),
                        Distrito = GetString(r0, "Emisor_Canton"),
                        Ciudad = GetString(r0, "Emisor_Distrito"),
                        Municipio = GetString(r0, "Emisor_Barrio"),
                        Calle = GetString(r0, "Emisor_OtrasSenas"),
                        Referencia = GetString(r0, "Emisor_OtrasSenas")
                    },
                    ContactoEmisor = new GosocketContactoEmisor
                    {
                        Extension = GetString(r0, "Emisor_CodigoPais"),
                        Telefono = GetString(r0, "Emisor_NumTelefono"),
                        eMail = NormalizeEmail(GetString(r0, "Emisor_CorreoElectronico"))
                    }
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
                ExtrInfoDoc = new List<GosocketExtraInfoDetalle>()
            };
            // Registro fiscal 8707 (si viene)
            AddExtra(encabezado.Emisor.ExtrInfoEmisor, "Registrofiscal8707", GetString(r0, "Emisor_Registrofiscal8707"));

            // Condición venta / plazo crédito como extras (si su XML genérico lo requiere así)
            AddExtra(encabezado.Receptor.ExtrInfoDoc, "CondicionVenta", GetString(r0, "CondicionVenta"));
            AddExtra(encabezado.Receptor.ExtrInfoDoc, "PlazoCredito", GetString(r0, "PlazoCredito"));

            // Código actividad económica
            AddExtra(encabezado.ExtrInfoDoc, "CodigoActividadEconomica", GetString(r0, "CodigoActividadEconomica"));

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

            var detalle = new GosocketDetalle
            {
                NroLinDet = GetInt(row, "DetalleServicio_NumeroLinea"),
                TpoListaItem = GetString(row, "DetalleServicio_PartidaArancelaria"),
                CdgItem = codigos,
                QtyItem = GetDecimal(row, "DetalleServicio_Cantidad"),
                UnmdItem = GetString(row, "DetalleServicio_UnidadMedida"),
                IndListaItem = GetString(row, "DetalleServicio_TipoTransaccion"),
                UnidadMedidaComercial = GetString(row, "DetalleServicio_UnidadMedidaComercial"),
                DscItem = GetString(row, "DetalleServicio_Detalle"),
                PrcNetoItem = GetDecimal(row, "DetalleServicio_PrecioUnitario"),
                MontoBrutoItem = GetDecimal(row, "DetalleServicio_MontoTotal"),
                ExtraInfoDetalle = extraInfo,
                ImpuestosDet = new List<GosocketImpuestosDet>(),
                MontoTotLinea = GetDecimal(row, "DetalleServicio_MontoTotalLinea")
            };

            AplicarDescuento(detalle, row);
            AplicarImpuesto(detalle, row);

            // Esto ya lo estabas metiendo como extra; lo dejo en una sola línea clara.
            AddExtra(detalle.ExtraInfoDetalle, "TipoTransaccion", GetString(row, "DetalleServicio_TipoTransaccion"));

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

            AddCodigoSiTieneValor(codigos, "CABYS", cabys);
            AddCodigoSiTieneValor(codigos, tipoCodComercial, valorCodComercial);
            AddCodigoSiTieneValor(codigos, "VIN", vin);

            return codigos;
        }

        private static List<GosocketExtraInfoDetalle> ConstruirExtraInfoDetalle(DataRow row)
        {
            var extras = new List<GosocketExtraInfoDetalle>();

            AddExtraSiTieneValor(extras, "RegistroMedicamento", GetString(row, "DetalleServicio_RegistroMedicamento"));
            AddExtraSiTieneValor(extras, "FormaFarmaceutica", GetString(row, "DetalleServicio_FormaFarmaceutica"));

            return extras;
        }

        private static void AplicarDescuento(GosocketDetalle item, DataRow row)
        {
            var montoDescuento = GetDecimal(row, "DetalleServicio_MontoDescuento", 0m);
            if (montoDescuento <= 0m) return;

            item.SubDscto = new GosocketSubDscto
            {
                MntDscto = montoDescuento,
                GlosaDscto = GetString(row, "DetalleServicio_NaturalezaDescuento")
            };
        }

        private static void AplicarImpuesto(GosocketDetalle item, DataRow row)
        {
            var montoImp = GetDecimal(row, "DetalleServicio_ImpuestoMonto", 0m);
            if (montoImp <= 0m) return;

            item.ImpuestosDet.Add(new GosocketImpuestosDet
            {
                CodImp = GetString(row, "DetalleServicio_ImpuestoCodigo", "01"),
                CodTasaImp = GetString(row, "DetalleServicio_ImpuestoCodigoTarifa"),
                TasaImp = GetDecimal(row, "DetalleServicio_ImpuestoTarifa", 0m),
                MontoImp = montoImp
            });

            AddExtra(item.ExtraInfoDetalle, "ImpuestoNeto",
                GetDecimal(row, "DetalleServicio_ImpuestoNeto", montoImp).ToString("0.00", CultureInfo.InvariantCulture));

            AddExtra(item.ExtraInfoDetalle, "BaseImponible",
                GetDecimal(row, "DetalleServicio_BaseImponible", 0m).ToString("0.00", CultureInfo.InvariantCulture));
        }

        private static GosocketDetalleComp? ConstruirDetalleCompSiAplica(DataRow row)
        {
            // Si no hay surtido, no crear el nodo
         
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
                TipoDscto = string.IsNullOrWhiteSpace(tipoDcto) ? "01" : tipoDcto
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

        private static void AddExtraSiTieneValor(List<GosocketExtraInfoDetalle> extras, string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            extras.Add(new GosocketExtraInfoDetalle { Name = name, Value = value });
        }

        private static string GetStringOrDefault(DataRow row, string columnName, string defaultValue)
        {
            return row.Table.Columns.Contains(columnName)
                ? (row[columnName]?.ToString() ?? defaultValue)
                : defaultValue;
        }
      
        #endregion


        private static GosocketTotales ConstruirTotalesDesdeSp(DataRow r0)
        {
            var tot = new GosocketTotales
            {
                SubTotal = GetDecimal(r0, "ResumenFactura_TotalVenta", 0m),
                MntDcto = GetDecimal(r0, "ResumenFactura_TotalDescuentos", 0m),
                MntBase = GetDecimal(r0, "ResumenFactura_TotalVentaNeta", 0m),
                MntImp = GetDecimal(r0, "ResumenFactura_TotalImpuesto", 0m),
                VlrPagar = GetDecimal(r0, "ResumenFactura_TotalComprobante", GetDecimal(r0, "ResumenFactura_TotalVentaNeta", 0m) + GetDecimal(r0, "ResumenFactura_TotalImpuesto", 0m)),
                MntExe = 0m,
                ImporteNoGravado = 0m,
                SaldoAnterior = 0m,
                ImporteOtrosTributos = 0m,
                MntRcgo = 0m,
                TotSubMonto = new List<GosocketTotSubMonto>(),
                Impuestos = new List<GosocketImpuestoTotal>()
            };

            // Subtotal por concepto (si quiere alimentar el [1..8] con TotalGravado por ejemplo)
            var totalGravado = GetDecimal(r0, "ResumenFactura_TotalGravado", 0m);
            if (totalGravado > 0m)
                tot.TotSubMonto.Add(new GosocketTotSubMonto { MontoConcepto = totalGravado });

            // Desglose impuesto
            var totalImp = tot.MntImp;
            if (totalImp > 0m)
            {
                tot.Impuestos.Add(new GosocketImpuestoTotal
                {
                    Tipolmp = "01",
                    CodTasaImp = null, // su SP trae ImpuestoCodigoTarifa como NULL en capturas
                    MontoImp = totalImp
                });
            }

       

            return tot;
        }

        private static GosocketPersonalizados ConstruirPersonalizados(DataRow r0)
        {
            var p = new GosocketPersonalizados();
            p.CampoString.Add(new GosocketCampoString { Name = "ProveedorSistemas", Value = GetString(r0, "ProveedorSistemas") });
            p.CampoString.Add(new GosocketCampoString { Name = "CodCliente", Value = GetString(r0, "CodCliente") });
            return p.HasContent() ? p : null;
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