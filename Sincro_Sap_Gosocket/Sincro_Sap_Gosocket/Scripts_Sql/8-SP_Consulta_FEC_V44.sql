USE [Pruebas_SincroSapGoSocket]
GO
/****** Object:  StoredProcedure [dbo].[SP_Consulta_FEC_V44]    Script Date: 22/05/2026 16:03:05 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER   PROCEDURE [dbo].[SP_Consulta_FEC_V44]
    @DocNum VARCHAR(50),
    @Situacion_de_Comprobante VARCHAR(1),
    @Tipo VARCHAR(1) = NULL  -- NULL permite detectar automáticamente si la factura de proveedor es de artículo (I) o servicio (S)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Emisor AS (
        -- En FEC el "Emisor" del XML es el proveedor
        SELECT TOP 1
            NULLIF(OPCH.U_LDT_ActEconomica, '') AS CodigoActividadEconomica,
            OCRD.CardName AS Emisor_Nombre,
            NULLIF(
                RIGHT('00' + LTRIM(RTRIM(CONVERT(VARCHAR(10), ISNULL(OCRD.U_LDT_IDType, '')))), 2),
                '00'
            ) AS Emisor_Tipo,
            LTRIM(RTRIM(ISNULL(OCRD.LicTradNum, ''))) AS Emisor_Numero,
            ISNULL(OCRD.CardFName, OCRD.CardName) AS Emisor_NombreComercial,

            RIGHT(
                '0' + ISNULL(
                    CASE 
                        WHEN ISNULL(OCRD.U_LDT_State, '') LIKE '%-%'
                            THEN RIGHT(OCRD.U_LDT_State, CHARINDEX('-', REVERSE(OCRD.U_LDT_State) + '-') - 1)
                        ELSE OCRD.U_LDT_State
                    END, '0'
                ), 1
            ) AS Emisor_Provincia,

            RIGHT(
                '00' + ISNULL(
                    CASE 
                        WHEN ISNULL(OCRD.U_LDT_County, '') LIKE '%-%'
                            THEN RIGHT(OCRD.U_LDT_County, CHARINDEX('-', REVERSE(OCRD.U_LDT_County) + '-') - 1)
                        ELSE OCRD.U_LDT_County
                    END, '0'
                ), 2
            ) AS Emisor_Canton,

            RIGHT(
                '00' + ISNULL(
                    CASE 
                        WHEN ISNULL(OCRD.U_LDT_District, '') LIKE '%-%'
                            THEN RIGHT(OCRD.U_LDT_District, CHARINDEX('-', REVERSE(OCRD.U_LDT_District) + '-') - 1)
                        ELSE OCRD.U_LDT_District
                    END, '0'
                ), 2
            ) AS Emisor_Distrito,

            ISNULL(OCRD.U_LDT_Nom_NeighB, '') AS Emisor_Barrio,
            ISNULL(OCRD.U_LDT_Direccion, '') AS Emisor_OtrasSenas,

			CASE 
			WHEN RIGHT('00' + LTRIM(RTRIM(CONVERT(VARCHAR(10), ISNULL(OCRD.U_LDT_IDType, '')))), 2) = '05'
				THEN NULLIF(LTRIM(RTRIM(CONVERT(VARCHAR(MAX), ISNULL(OCRD.U_LDT_OtSenEx, '')))), '')
			ELSE ''
		END AS Emisor_OtrasSenasExtranjero,

	CASE 
		WHEN RIGHT('00' + LTRIM(RTRIM(CONVERT(VARCHAR(10), ISNULL(OCRD.U_LDT_IDType, '')))), 2) = '05'
			THEN ''
		ELSE '506'
	END AS Emisor_CodigoPais,

	CASE 
		WHEN RIGHT('00' + LTRIM(RTRIM(CONVERT(VARCHAR(10), ISNULL(OCRD.U_LDT_IDType, '')))), 2) = '05'
			THEN ''
		ELSE ISNULL(OCRD.Phone1, '')
	END AS Emisor_NumTelefono,

	CASE 
		WHEN RIGHT('00' + LTRIM(RTRIM(CONVERT(VARCHAR(10), ISNULL(OCRD.U_LDT_IDType, '')))), 2) = '05'
			THEN ''
		ELSE ISNULL(OCRD.E_Mail, '')
	END AS Emisor_CorreoElectronico,
            '' AS Emisor_Registrofiscal8707
        FROM ZZTEST_SBO_LARCE.dbo.OPCH OPCH
        INNER JOIN ZZTEST_SBO_LARCE.dbo.OCRD OCRD
            ON OCRD.CardCode = OPCH.CardCode
        WHERE OPCH.DocNum = @DocNum
          AND (NULLIF(@Tipo,'') IS NULL OR OPCH.DocType = @Tipo)
    ),

    Receptor AS (
        -- En FEC el "Receptor" del XML es nuestra empresa
        SELECT TOP 1
			NULLIF(OADM.U_LDT_ActEconomica, '')  CodigoActividadReceptor,
            OADM.CompnyName AS Receptor_Nombre,
            NULLIF(
                RIGHT('00' + LTRIM(RTRIM(CONVERT(VARCHAR(10), ISNULL(OADM.U_LDT_IDType, '')))), 2),
                '00'
            ) AS Receptor_Tipo,
            ISNULL(OADM.TaxIdNum, '') AS Receptor_Numero,
            ISNULL(OADM.AliasName, OADM.CompnyName) AS Receptor_NombreComercial,
            ISNULL(OADM.Phone1, '') AS Receptor_NumTelefono,
            ISNULL(OADM.E_Mail, '') AS Receptor_CorreoElectronico,
            '506' AS Receptor_CodigoPais,
            '' AS Receptor_IdentificacionExtranjero
        FROM ZZTEST_SBO_LARCE.dbo.OADM OADM
    ),

    DireccionReceptor AS (
        SELECT TOP 1
            RIGHT(
                '0' + ISNULL(
                    CASE 
                        WHEN OADM.U_LDT_State LIKE '%-%'
                            THEN RIGHT(OADM.U_LDT_State, CHARINDEX('-', REVERSE(OADM.U_LDT_State) + '-') - 1)
                        ELSE OADM.U_LDT_State
                    END, '0'
                ), 1
            ) AS Receptor_Provincia,

            RIGHT(
                '00' + ISNULL(
                    CASE 
                        WHEN OADM.U_LDT_County LIKE '%-%'
                            THEN RIGHT(OADM.U_LDT_County, CHARINDEX('-', REVERSE(OADM.U_LDT_County) + '-') - 1)
                        ELSE OADM.U_LDT_County
                    END, '0'
                ), 2
            ) AS Receptor_Canton,

            RIGHT(
                '00' + ISNULL(
                    CASE 
                        WHEN OADM.U_LDT_District LIKE '%-%'
                            THEN RIGHT(OADM.U_LDT_District, CHARINDEX('-', REVERSE(OADM.U_LDT_District) + '-') - 1)
                        ELSE OADM.U_LDT_District
                    END, '0'
                ), 2
            ) AS Receptor_Distrito,

            ISNULL(OADM.U_LDT_Nom_NeighB, '') AS Receptor_Barrio,
            ISNULL(OADM.CompnyAddr, '') AS Receptor_OtrasSenas,
            '' AS Receptor_OtrasSenasExtranjero
        FROM ZZTEST_SBO_LARCE.dbo.OADM OADM
    ),

    TipoFactura AS (
        SELECT
            T0.DocEntry,
            '08' AS CodigoTipoDocumento
        FROM ZZTEST_SBO_LARCE.dbo.OPCH T0
        WHERE T0.DocNum = @DocNum
          AND (NULLIF(@Tipo,'') IS NULL OR T0.DocType = @Tipo)
    ),
	Consecutivo AS (
    SELECT 
        CASE TF.CodigoTipoDocumento
            WHEN '01' THEN CE.FE
            WHEN '02' THEN CE.ND
            WHEN '03' THEN CE.NC
            WHEN '04' THEN CE.TE
			WHEN '08' THEN CE.FEC
            WHEN '09' THEN CE.FEE
			ELSE CE.FE
        END AS Consecutivo
    FROM TipoFactura TF
    CROSS JOIN Pruebas_SincroSapGoSocket.dbo.ComprobantesElectronicos_Consecutivos CE
),	

    ClaveGenerada AS (
        SELECT
            T0.DocNum,
            CAST('506' AS VARCHAR(3)) COLLATE DATABASE_DEFAULT +
            CAST(SUBSTRING(CONVERT(VARCHAR, T0.DocDate, 103), 1, 2) AS VARCHAR(2)) COLLATE DATABASE_DEFAULT +
            CAST(SUBSTRING(CONVERT(VARCHAR, T0.DocDate, 103), 4, 2) AS VARCHAR(2)) COLLATE DATABASE_DEFAULT +
            CAST(SUBSTRING(CONVERT(VARCHAR, T0.DocDate, 103), 9, 2) AS VARCHAR(2)) COLLATE DATABASE_DEFAULT +
            RIGHT(
                CAST('000000000000' AS VARCHAR(12)) COLLATE DATABASE_DEFAULT +
                CAST(
                    REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(ISNULL(R.Receptor_Numero, ''))), '-', ''), ' ', ''), '.', '')
                    AS VARCHAR(20)
                ) COLLATE DATABASE_DEFAULT,
                12
            ) +
            CAST(C.Consecutivo AS VARCHAR(20)) COLLATE DATABASE_DEFAULT +
            CAST(@Situacion_de_Comprobante AS VARCHAR(1)) COLLATE DATABASE_DEFAULT +
            RIGHT(
                CAST('00000000' AS VARCHAR(8)) COLLATE DATABASE_DEFAULT +
                CAST(CAST(T0.DocNum AS BIGINT) AS VARCHAR(20)) COLLATE DATABASE_DEFAULT,
                8
            ) AS Clave
        FROM ZZTEST_SBO_LARCE.dbo.OPCH T0
        CROSS JOIN Receptor R
        CROSS JOIN Consecutivo C
        WHERE T0.DocNum = @DocNum
          AND (NULLIF(@Tipo,'') IS NULL OR T0.DocType = @Tipo)
    ),

   InfoReferencia AS (
    SELECT
        T0.DocEntry,
        NULLIF(LTRIM(RTRIM(T0.NumAtCard)), '') AS Referencia_Numero,

		  CASE
		WHEN LTRIM(RTRIM(ISNULL(OCRD.U_LDT_IDType, ''))) = '05' THEN '16'
		WHEN NULLIF(LTRIM(RTRIM(T0.U_LDT_TipoDocRef)), '') IS NOT NULL THEN LTRIM(RTRIM(T0.U_LDT_TipoDocRef))
		ELSE '99'
	END AS Referencia_TipoDoc,

        CONVERT(VARCHAR(19), CAST(ISNULL(T0.TaxDate, T0.DocDate) AS DATETIME), 126) + '-06:00' AS Referencia_FechaEmision,
        CAST(NULL AS DATETIME2(0)) AS Referencia_HoraEmision,

        CASE
            WHEN LTRIM(RTRIM(ISNULL(OCRD.U_LDT_IDType, ''))) = '05' THEN '11'
            ELSE RIGHT('00' + ISNULL(NULLIF(LTRIM(RTRIM(T0.U_LDT_CodRef)), ''), '04'), 2)
        END AS Referencia_Codigo,

        CASE
            WHEN LTRIM(RTRIM(ISNULL(OCRD.U_LDT_IDType, ''))) = '05'
                THEN 'Compra a proveedor no domiciliado'
            ELSE 'Registro de documento del proveedor'
        END AS Referencia_Razon
    FROM ZZTEST_SBO_LARCE.dbo.OPCH T0
    INNER JOIN ZZTEST_SBO_LARCE.dbo.OCRD OCRD
        ON OCRD.CardCode = T0.CardCode
    WHERE T0.DocNum = @DocNum
      AND (NULLIF(@Tipo,'') IS NULL OR T0.DocType = @Tipo)
),

    CodigosImpuesto AS (
        SELECT
            T1.DocEntry,
            T1.LineNum,
            CASE
                WHEN LTRIM(RTRIM(T1.TaxCode)) IN ('EX','EXE','EXES') THEN '10'
                WHEN LTRIM(RTRIM(T1.TaxCode)) LIKE 'EXO%' THEN
                    CASE
                        WHEN COALESCE(T1.VatPrcnt,0) = 13 THEN '08'
                        WHEN COALESCE(T1.VatPrcnt,0) = 4 THEN '04'
                        WHEN COALESCE(T1.VatPrcnt,0) = 2 THEN '03'
                        WHEN COALESCE(T1.VatPrcnt,0) = 1 THEN '06'
                        WHEN COALESCE(T1.VatPrcnt,0) = 0.5 THEN '09'
                        ELSE NULL
                    END
                WHEN LTRIM(RTRIM(T1.TaxCode)) IN ('IVA 13','IV','IV1') THEN '08'
                WHEN LTRIM(RTRIM(T1.TaxCode)) = 'IVA 4' THEN '04'
                WHEN LTRIM(RTRIM(T1.TaxCode)) = 'IVA 2' THEN '03'
                WHEN LTRIM(RTRIM(T1.TaxCode)) = 'IVA 1' THEN '06'
                WHEN LTRIM(RTRIM(T1.TaxCode)) IN ('IVA 0.65','IV3') THEN '07'
                WHEN LTRIM(RTRIM(T1.TaxCode)) = 'ISC' THEN '10'
                ELSE NULL
            END AS DetalleServicio_ImpuestoCodigoTarifa
        FROM ZZTEST_SBO_LARCE.dbo.PCH1 T1
        WHERE T1.DocEntry = (
            SELECT DocEntry
            FROM ZZTEST_SBO_LARCE.dbo.OPCH
            WHERE DocNum = @DocNum
              AND (NULLIF(@Tipo,'') IS NULL OR DocType = @Tipo)
        )
          AND (
              EXISTS (SELECT 1 FROM ZZTEST_SBO_LARCE.dbo.OPCH H WHERE H.DocEntry = T1.DocEntry AND H.DocType = 'S')
              OR ISNULL(T1.TreeType, 'N') IN ('N','S')
          )
    ),

    ImpuestosEspecificos AS (
        SELECT
            T1.DocEntry,
            T1.LineNum,
            0.0 AS MontoImpuestoEspecifico,
            ISNULL(T1.Quantity, 0) AS DetalleServicio_ImpuestoEspecifico_CantidadUnidadMedida,
            0.0 AS DetalleServicio_ImpuestoEspecifico_Porcentaje,
            0.0 AS DetalleServicio_ImpuestoEspecifico_Proporcion,
            0.0 AS DetalleServicio_ImpuestoEspecifico_VolumenUnidadConsumo,
            0.0 AS DetalleServicio_ImpuestoEspecifico_ImpuestoUnidad
        FROM ZZTEST_SBO_LARCE.dbo.PCH1 T1
        WHERE T1.DocEntry = (
            SELECT DocEntry
            FROM ZZTEST_SBO_LARCE.dbo.OPCH
            WHERE DocNum = @DocNum
              AND (NULLIF(@Tipo,'') IS NULL OR DocType = @Tipo)
        )
          AND (
              EXISTS (SELECT 1 FROM ZZTEST_SBO_LARCE.dbo.OPCH H WHERE H.DocEntry = T1.DocEntry AND H.DocType = 'S')
              OR ISNULL(T1.TreeType, 'N') IN ('N','S')
          )
    ),

    /*==============================================================
      CTE PrecioLinea
      --------------------------------------------------------------
      Objetivo:
      Centralizar el precio unitario que debe usar todo el SP de FEC.

      Regla aplicada:
      1) Si la factura de proveedor está en moneda local (COL/CRC)
         y la línea fue digitada en USD, convierte el precio unitario a colones.
      2) Para convertir usa el tipo de cambio digitado en SAP:
           - Primero PCH1.Rate, porque corresponde al tipo de cambio de la línea.
           - Si PCH1.Rate viene 0 o NULL, usa OPCH.DocRate.
      3) Si la factura está en USD y la línea está en USD, NO convierte.
      4) Si la factura está en moneda local y la línea está en moneda local, NO convierte.

      Importante:
      Este CTE solo normaliza el precio unitario. Los cálculos posteriores
      usan PrecioUnitarioUsar por medio del alias PriceBefDi en DetalleServicioBase.
    ==============================================================*/
    PrecioLinea AS (
        SELECT
            T1.DocEntry,
            T1.LineNum,
            H.DocType,
            H.DocCur     AS MonedaFactura,
            T1.Currency  AS MonedaPrecioLinea,
            H.DocRate    AS TipoCambioFactura,
            T1.Rate      AS TipoCambioLinea,

            CASE
                WHEN H.DocType = 'S'
                    THEN CASE
                            WHEN ISNULL(T1.PriceBefDi, 0) > 0 THEN T1.PriceBefDi
                            WHEN ISNULL(T1.Quantity, 0) > 0 THEN T1.LineTotal / NULLIF(T1.Quantity, 0)
                            ELSE T1.LineTotal
                         END
                ELSE T1.PriceBefDi
            END AS PrecioBaseSAP,

            CASE
                WHEN H.DocCur IN ('COL', 'CRC')
                     AND UPPER(LTRIM(RTRIM(ISNULL(T1.Currency, '')))) = 'USD'
                THEN ROUND(
                        CASE
                            WHEN H.DocType = 'S'
                                THEN CASE
                                        WHEN ISNULL(T1.PriceBefDi, 0) > 0 THEN T1.PriceBefDi
                                        WHEN ISNULL(T1.Quantity, 0) > 0 THEN T1.LineTotal / NULLIF(T1.Quantity, 0)
                                        ELSE T1.LineTotal
                                     END
                            ELSE T1.PriceBefDi
                        END
                        * COALESCE(NULLIF(T1.Rate, 0), NULLIF(H.DocRate, 0), 1)
                     , 5)
                ELSE
                    CASE
                        WHEN H.DocType = 'S'
                            THEN CASE
                                    WHEN ISNULL(T1.PriceBefDi, 0) > 0 THEN T1.PriceBefDi
                                    WHEN ISNULL(T1.Quantity, 0) > 0 THEN T1.LineTotal / NULLIF(T1.Quantity, 0)
                                    ELSE T1.LineTotal
                                 END
                        ELSE T1.PriceBefDi
                    END
            END AS PrecioUnitarioUsar,

            CASE
                WHEN H.DocCur IN ('COL', 'CRC')
                     AND UPPER(LTRIM(RTRIM(ISNULL(T1.Currency, '')))) = 'USD'
                THEN 1
                ELSE 0
            END AS PrecioFueConvertido
        FROM ZZTEST_SBO_LARCE.dbo.PCH1 T1
        INNER JOIN ZZTEST_SBO_LARCE.dbo.OPCH H
            ON H.DocEntry = T1.DocEntry
        WHERE H.DocNum = @DocNum
          AND (NULLIF(@Tipo,'') IS NULL OR H.DocType = @Tipo)
          AND (
                H.DocType = 'S'
                OR ISNULL(T1.TreeType, 'N') IN ('N','S')
              )
    ),

    DetalleServicioBase AS (
        SELECT
            T1.DocEntry,
            T1.LineNum AS DetalleServicio_NumeroLinea,

            -- Si es factura de proveedor de servicio (OPCH.DocType = 'S'), SAP puede no traer ItemCode.
            -- En ese caso usamos el código de servicio definido en campos de usuario del detalle.
            CASE
                WHEN H.DocType = 'S'
                    THEN COALESCE(NULLIF(LTRIM(RTRIM(T1.AcctCode)), ''), 'SERVICIO')
                ELSE T1.ItemCode
            END AS ItemCode,

            T1.Dscription,

            -- En documentos de servicio normalmente la cantidad puede venir en 0 o NULL.
            -- Para FEC se requiere una cantidad mayor a cero, por eso se usa 1 como respaldo.
            CASE
                WHEN H.DocType = 'S' AND ISNULL(T1.Quantity, 0) <= 0 THEN 1
                ELSE ISNULL(T1.Quantity, 1)
            END AS Quantity,

            -- Precio unitario que debe usar todo el SP.
            -- Si la factura es local y la línea está en USD, ya viene convertido a colones.
            PL.PrecioUnitarioUsar AS PriceBefDi,

            T1.DiscPrcnt,
            T1.TaxOnly,
            T1.VatPrcnt,
            T1.U_LDT_TipoDesc,
            T1.U_LDT_NatuDesc,

            -- Artículos: se toma de OITM como actualmente.
            -- Servicios: se toma de PCH1 porque no hay maestro de artículo.
            CASE
                WHEN H.DocType = 'S' THEN NULLIF(LTRIM(RTRIM(T1.U_LDT_CABYS)), '')
                ELSE T4.U_LDT_CABYS
            END AS U_CodigoCabys,

            CASE
                WHEN H.DocType = 'S' THEN NULL
                ELSE T4.U_LDT_RegMed
            END AS U_RegistroMedicamento,

            CASE
                WHEN H.DocType = 'S' THEN NULL
                ELSE T4.U_LDT_ForFam
            END AS U_FormaFarmaceutica,

            CASE
                WHEN H.DocType = 'S'
                    THEN NULLIF(LTRIM(RTRIM(H.U_LDT_ActEconomica)), '')
                ELSE T4.U_LDT_ActEconom
            END AS U_CodigoActividadEconomica,

            CASE
                WHEN H.DocType = 'S'
                    THEN '04'  -- Servicio sin ItemCode: se usa código comercial interno por defecto
                ELSE T4.U_LDT_WMCodigoProducto
            END AS U_TipoCodigo,

            CASE
                WHEN H.DocType = 'S' THEN NULL
                ELSE T4.U_Part_Arancel
            END AS DetalleServicio_PartidaArancelaria,

            T1.U_LDT_TipoTrans,

            -- Unidad de medida:
            -- Servicio: se usa 'Os' por defecto porque no hay campo de usuario de unidad en PCH1.
            -- Artículo: se conserva lógica actual según CABYS.
            CASE
                WHEN H.DocType = 'S' THEN 'Os'
                WHEN LEFT(ISNULL(T4.U_LDT_CABYS,''),1) BETWEEN '5' AND '9' THEN 'Os'
                ELSE 'Unid'
            END AS UnidadMedidaCalculada,

            CASE
                WHEN H.DocType = 'S' THEN 'Os'
                WHEN LEFT(ISNULL(T4.U_LDT_CABYS,''),1) BETWEEN '5' AND '9' THEN 'Os'
                ELSE 'Unid'
            END AS UnidadMedidaComercialCalculada,

            0 AS EsAutoconsumo,

            ROUND(
                (CASE
                    WHEN H.DocType = 'S' AND ISNULL(T1.Quantity, 0) <= 0 THEN 1
                    ELSE ISNULL(T1.Quantity, 1)
                 END) * PL.PrecioUnitarioUsar
            , 5) AS DetalleServicio_MontoTotal,

            CASE
                WHEN T1.TaxOnly = 'Y'
                    THEN ROUND(
                        (CASE
                            WHEN H.DocType = 'S' AND ISNULL(T1.Quantity, 0) <= 0 THEN 1
                            ELSE ISNULL(T1.Quantity, 1)
                         END) * PL.PrecioUnitarioUsar
                    , 5)
                WHEN T1.DiscPrcnt > 0
                    THEN ROUND(
                        ROUND(
                            (CASE
                                WHEN H.DocType = 'S' AND ISNULL(T1.Quantity, 0) <= 0 THEN 1
                                ELSE ISNULL(T1.Quantity, 1)
                             END) * PL.PrecioUnitarioUsar
                        , 5) * T1.DiscPrcnt / 100.0
                    , 5)
                ELSE 0.0
            END AS DetalleServicio_MontoDescuento
        FROM ZZTEST_SBO_LARCE.dbo.PCH1 T1
        INNER JOIN ZZTEST_SBO_LARCE.dbo.OPCH H
            ON H.DocEntry = T1.DocEntry
        INNER JOIN PrecioLinea PL
            ON T1.DocEntry = PL.DocEntry
           AND T1.LineNum = PL.LineNum
        LEFT JOIN ZZTEST_SBO_LARCE.dbo.OITM T4
            ON T1.ItemCode = T4.ItemCode
        WHERE H.DocNum = @DocNum
          AND (NULLIF(@Tipo,'') IS NULL OR H.DocType = @Tipo)
          AND (
                H.DocType = 'S'
                OR ISNULL(T1.TreeType, 'N') IN ('N','S')
              )
    ),

    DetalleServicio AS (
        SELECT
            DB.DocEntry,
            DB.DetalleServicio_NumeroLinea,
            '0' AS DetalleServicio_PartidaArancelaria,
            RIGHT('00' + CAST(ISNULL(DB.U_TipoCodigo, '01') AS VARCHAR(2)), 2) AS DetalleServicio_TipoCodigo,
            ISNULL(DB.U_CodigoCabys, '0') AS DetalleServicio_CodigoProductoServicio,
            ISNULL(DB.U_RegistroMedicamento, '0') AS DetalleServicio_RegistroMedicamento,
            ISNULL(DB.U_FormaFarmaceutica, '0') AS DetalleServicio_FormaFarmaceutica,
            DB.ItemCode AS DetalleServicio_Codigo,
            DB.Quantity AS DetalleServicio_Cantidad,
            DB.UnidadMedidaCalculada AS DetalleServicio_UnidadMedida,
            DB.UnidadMedidaComercialCalculada AS DetalleServicio_UnidadMedidaComercial,
            DB.Dscription AS DetalleServicio_Detalle,
            ISNULL(NULLIF(RIGHT('00' + LTRIM(RTRIM(ISNULL(DB.U_LDT_TipoTrans,''))),2), '00'), '01') AS DetalleServicio_TipoTransaccion,
            '' AS DetalleServicio_NumeroVINoSerie,
            DB.PriceBefDi AS DetalleServicio_PrecioUnitario,
            DB.DetalleServicio_MontoTotal,
            DB.DetalleServicio_MontoDescuento,
            DB.PriceBefDi * DB.Quantity AS DetalleServicio_MontoGravadoBruto,
            DB.U_CodigoActividadEconomica AS DetalleServicio_CodigoActividadEconomica,
            CASE
                WHEN ROUND(ISNULL(DB.DetalleServicio_MontoDescuento,0),5) <= 0 THEN ''
                WHEN NULLIF(LTRIM(RTRIM(ISNULL(DB.U_LDT_NatuDesc,''))), '') IS NOT NULL THEN LTRIM(RTRIM(DB.U_LDT_NatuDesc))
                WHEN DB.TaxOnly = 'Y' OR DB.DiscPrcnt >= 100 THEN 'Bonificacion'
                ELSE 'Comercial'
            END AS DetalleServicio_NaturalezaDescuento,
            CASE
                WHEN ROUND(ISNULL(DB.DetalleServicio_MontoDescuento,0),5) <= 0 THEN ''
                WHEN NULLIF(LTRIM(RTRIM(ISNULL(DB.U_LDT_TipoDesc,''))), '') IS NOT NULL THEN RIGHT('00' + LTRIM(RTRIM(DB.U_LDT_TipoDesc)), 2)
                WHEN DB.TaxOnly = 'Y' OR DB.DiscPrcnt >= 100 THEN '02'
                ELSE '07'
            END AS DetalleServicio_CodigoDescuento,
            ROUND(DB.DetalleServicio_MontoTotal - DB.DetalleServicio_MontoDescuento,5) AS DetalleServicio_SubTotal,
            '0' AS DetalleServicio_IVACobradoFabrica,
            '0' AS DetalleServicio_ImpuestoAsumidoEmisorFabrica,
            CASE
                WHEN DB.TaxOnly = 'Y' OR DB.DiscPrcnt >= 100
                    THEN ROUND(DB.DetalleServicio_MontoTotal * DB.VatPrcnt / 100.0, 5)
                ELSE ROUND(ROUND(DB.DetalleServicio_MontoTotal - DB.DetalleServicio_MontoDescuento,5) * (1 + DB.VatPrcnt / 100.0), 5)
            END AS DetalleServicio_MontoTotalLinea
        FROM DetalleServicioBase DB
    ),

    ImpuestoBase AS (
        SELECT
            T1.DocEntry,
            T1.LineNum,
            CI.DetalleServicio_ImpuestoCodigoTarifa,
            CASE
                WHEN H.DocType = 'S'
                    THEN '01'  -- Servicio: impuesto IVA por defecto
                ELSE RIGHT('00' + ISNULL(T2.U_LDT_WMCodigoProducto, ''), 2)
            END AS DetalleServicio_ImpuestoCodigo,
            '0' AS DetalleServicio_ImpuestoCodigoImpuestoOTRO,
            CAST(COALESCE(T1.VatPrcnt,0) AS DECIMAL(18,5)) AS DetalleServicio_ImpuestoTarifa,
            ROUND(DS.DetalleServicio_SubTotal,5) AS DetalleServicio_BaseImponible,
            CASE
                WHEN ROUND(DS.DetalleServicio_SubTotal,5) > 0 AND COALESCE(T1.VatPrcnt,0) > 0
                    THEN ROUND(DS.DetalleServicio_SubTotal * T1.VatPrcnt / 100.0, 5)
                WHEN ROUND(DS.DetalleServicio_SubTotal,5) = 0
                     AND ROUND(DS.DetalleServicio_MontoDescuento,5) = ROUND(DS.DetalleServicio_MontoTotal,5)
                     AND COALESCE(T1.VatPrcnt,0) > 0
                    THEN ROUND(DS.DetalleServicio_MontoTotal * T1.VatPrcnt / 100.0, 5)
                ELSE 0.00
            END AS DetalleServicio_ImpuestoMonto,
            '0' AS DetalleServicio_ImpuestoFactorIVA,
            IE.MontoImpuestoEspecifico,
            IE.DetalleServicio_ImpuestoEspecifico_CantidadUnidadMedida,
            IE.DetalleServicio_ImpuestoEspecifico_Porcentaje,
            IE.DetalleServicio_ImpuestoEspecifico_Proporcion,
            IE.DetalleServicio_ImpuestoEspecifico_VolumenUnidadConsumo,
            IE.DetalleServicio_ImpuestoEspecifico_ImpuestoUnidad,
            DS.DetalleServicio_IVACobradoFabrica,
            DS.DetalleServicio_ImpuestoAsumidoEmisorFabrica,
            DS.DetalleServicio_SubTotal,
            T1.DiscPrcnt AS DetalleServicio_PorcentajeDescuento
        FROM ZZTEST_SBO_LARCE.dbo.PCH1 T1
        INNER JOIN ZZTEST_SBO_LARCE.dbo.OPCH H
            ON H.DocEntry = T1.DocEntry
        LEFT JOIN ZZTEST_SBO_LARCE.dbo.OITM T2
            ON T1.ItemCode = T2.ItemCode
        INNER JOIN DetalleServicio DS
            ON T1.DocEntry = DS.DocEntry
           AND T1.LineNum = DS.DetalleServicio_NumeroLinea
        LEFT JOIN CodigosImpuesto CI
            ON T1.DocEntry = CI.DocEntry
           AND T1.LineNum = CI.LineNum
        LEFT JOIN ImpuestosEspecificos IE
            ON T1.DocEntry = IE.DocEntry
           AND T1.LineNum = IE.LineNum
        WHERE T1.DocEntry = (
            SELECT DocEntry
            FROM ZZTEST_SBO_LARCE.dbo.OPCH
            WHERE DocNum = @DocNum
              AND (NULLIF(@Tipo,'') IS NULL OR DocType = @Tipo)
        )
          AND (
              EXISTS (SELECT 1 FROM ZZTEST_SBO_LARCE.dbo.OPCH H WHERE H.DocEntry = T1.DocEntry AND H.DocType = 'S')
              OR ISNULL(T1.TreeType, 'N') IN ('N','S')
          )
    ),

    Exoneracion AS (
        SELECT
            IB.DocEntry,
            IB.LineNum,
            NULL AS Exoneracion_TipoDocumento,
            NULL AS Exoneracion_NumeroDocumento,
            NULL AS Exoneracion_NombreInstitucion,
            NULL AS Exoneracion_NombreInstitucionOtros,
            NULL AS Exoneracion_FechaEmision,
            NULL AS Exoneracion_PorcentajeCompra,
            NULL AS Exoneracion_IvaExonerado,
            NULL AS Exoneracion_Articulo,
            NULL AS Exoneracion_Inciso,
            0.0 AS Exoneracion_MontoImpuesto,
            COALESCE(IB.DetalleServicio_ImpuestoMonto,0) AS Exoneracion_ImpuestoNeto,
            0.0 AS Exoneracion_TotalMercExonerada
        FROM ImpuestoBase IB
    ),

    Impuesto AS (
        SELECT
            IB.DocEntry,
            IB.LineNum,
            IB.DetalleServicio_ImpuestoCodigo,
            IB.DetalleServicio_ImpuestoCodigoImpuestoOTRO,
            IB.DetalleServicio_ImpuestoCodigoTarifa,
            IB.DetalleServicio_ImpuestoTarifa,
            '0' AS DetalleServicio_ImpuestoFactorIVA,
            IB.MontoImpuestoEspecifico,
            IB.DetalleServicio_ImpuestoEspecifico_CantidadUnidadMedida,
            IB.DetalleServicio_ImpuestoEspecifico_Porcentaje,
            IB.DetalleServicio_ImpuestoEspecifico_Proporcion,
            IB.DetalleServicio_ImpuestoEspecifico_VolumenUnidadConsumo,
            IB.DetalleServicio_ImpuestoEspecifico_ImpuestoUnidad,
            IB.DetalleServicio_IVACobradoFabrica,
            ROUND(IB.DetalleServicio_SubTotal + COALESCE(IB.MontoImpuestoEspecifico,0),5) AS DetalleServicio_BaseImponible,
            ROUND((IB.DetalleServicio_SubTotal + COALESCE(IB.MontoImpuestoEspecifico,0)) * COALESCE(IB.DetalleServicio_ImpuestoTarifa,0) / 100.0,5) AS DetalleServicio_ImpuestoMonto,
            ROUND(
                ROUND((IB.DetalleServicio_SubTotal + COALESCE(IB.MontoImpuestoEspecifico,0)) * COALESCE(IB.DetalleServicio_ImpuestoTarifa,0) / 100.0,5)
                - COALESCE(EXO.Exoneracion_MontoImpuesto,0),5
            ) AS DetalleServicio_ImpuestoNeto
        FROM ImpuestoBase IB
        LEFT JOIN Exoneracion EXO
            ON IB.DocEntry = EXO.DocEntry
           AND IB.LineNum = EXO.LineNum
    ),

    ResumenFactura AS (
        SELECT
            OPCH.DocEntry,
            ISNULL(OPCH.DocCur, 'CRC') AS ResumenFactura_CodigoMoneda,
            CASE
                WHEN ISNULL(OPCH.DocCur, 'CRC') = 'CRC' THEN 1
                ELSE ISNULL(OPCH.DocRate, 1)
            END AS ResumenFactura_TipoCambio,

            SUM(CASE
                WHEN LEFT(ISNULL(DS.DetalleServicio_CodigoProductoServicio,''),1) BETWEEN '5' AND '9'
                 AND COALESCE(IMP.DetalleServicio_ImpuestoMonto,0) > 0
                THEN DS.DetalleServicio_SubTotal ELSE 0 END) AS ResumenFactura_TotalServGravados,

            SUM(CASE
                WHEN LEFT(ISNULL(DS.DetalleServicio_CodigoProductoServicio,''),1) BETWEEN '5' AND '9'
                 AND COALESCE(IMP.DetalleServicio_ImpuestoMonto,0) = 0
                THEN DS.DetalleServicio_SubTotal ELSE 0 END) AS ResumenFactura_TotalServExentos,

            0.0 AS ResumenFactura_TotalServExonerado,
            0.0 AS ResumenFactura_TotalServNoSujeto,

            SUM(CASE
                WHEN LEFT(ISNULL(DS.DetalleServicio_CodigoProductoServicio,''),1) BETWEEN '0' AND '4'
                 AND COALESCE(IMP.DetalleServicio_ImpuestoMonto,0) > 0
                THEN DS.DetalleServicio_SubTotal ELSE 0 END) AS ResumenFactura_TotalMercanciasGravadas,

            SUM(CASE
                WHEN LEFT(ISNULL(DS.DetalleServicio_CodigoProductoServicio,''),1) BETWEEN '0' AND '4'
                 AND COALESCE(IMP.DetalleServicio_ImpuestoMonto,0) = 0
                THEN DS.DetalleServicio_SubTotal ELSE 0 END) AS ResumenFactura_TotalMercanciasExentas,

            0.0 AS ResumenFactura_TotalMercExonerada,
            0.0 AS ResumenFactura_TotalMercNoSujeta,

            SUM(CASE WHEN COALESCE(IMP.DetalleServicio_ImpuestoMonto,0) > 0 THEN DS.DetalleServicio_SubTotal ELSE 0 END) AS ResumenFactura_TotalGravado,
            SUM(CASE WHEN COALESCE(IMP.DetalleServicio_ImpuestoMonto,0) = 0 THEN DS.DetalleServicio_SubTotal ELSE 0 END) AS ResumenFactura_TotalExento,
            0.0 AS ResumenFactura_TotalExonerado,
            0.0 AS ResumenFactura_TotalNoSujeto,
            SUM(DS.DetalleServicio_MontoTotal) AS ResumenFactura_TotalVenta,
            SUM(DS.DetalleServicio_MontoDescuento) AS ResumenFactura_TotalDescuentos,
            SUM(DS.DetalleServicio_SubTotal) AS ResumenFactura_TotalVentaNeta,
            SUM(COALESCE(IMP.DetalleServicio_ImpuestoNeto,0)) AS ResumenFactura_TotalImpuesto,
            0.0 AS ResumenFactura_TotalIVADevuelto,
            0.0 AS ResumenFactura_TotalOtrosCargos,
            SUM(DS.DetalleServicio_SubTotal + COALESCE(IMP.DetalleServicio_ImpuestoNeto,0)) AS ResumenFactura_TotalComprobante,
            '' AS OtroContenido,
            0.0 AS OtroCargo_Monto,
            '' AS OtroCargo_Detalle,
            '' AS OtroCargo_TipoDocumento,
            0.0 AS OtroCargo_Porcentaje
        FROM ZZTEST_SBO_LARCE.dbo.OPCH OPCH
        INNER JOIN DetalleServicio DS
            ON OPCH.DocEntry = DS.DocEntry
        LEFT JOIN Impuesto IMP
            ON DS.DocEntry = IMP.DocEntry
           AND DS.DetalleServicio_NumeroLinea = IMP.LineNum
        WHERE OPCH.DocNum = @DocNum
          AND (NULLIF(@Tipo,'') IS NULL OR OPCH.DocType = @Tipo)
        GROUP BY OPCH.DocEntry, OPCH.DocCur, OPCH.DocRate
    ),

    TotalComprobanteFinal AS (
        SELECT
            ROUND(
                SUM(RF.ResumenFactura_TotalVentaNeta)
              + SUM(RF.ResumenFactura_TotalImpuesto)
              + SUM(RF.ResumenFactura_TotalOtrosCargos)
              - SUM(RF.ResumenFactura_TotalIVADevuelto)
            , 5) AS ResumenFactura_TotalComprobante
        FROM ResumenFactura RF
    )

    SELECT
        TF.CodigoTipoDocumento AS TipoComprobante,
        K.Clave,
        C.Consecutivo,
        E.Emisor_Numero AS ProveedorSistemas,

        CONVERT(VARCHAR(19),
          DATETIME2FROMPARTS(
            YEAR(T0.DocDate), MONTH(T0.DocDate), DAY(T0.DocDate),
            CAST(SUBSTRING(TTS.ts6,1,2) AS INT),
            CAST(SUBSTRING(TTS.ts6,3,2) AS INT),
            CAST(SUBSTRING(TTS.ts6,5,2) AS INT),
            0, 0
          ),
          126
        ) + '-06:00' AS Fecha,

        -- Emisor
        E.CodigoActividadEconomica AS CodigoActividadEconomica,
        E.Emisor_Nombre,
        E.Emisor_Tipo,
        E.Emisor_Numero,
        E.Emisor_NombreComercial,
        E.Emisor_Registrofiscal8707,
        E.Emisor_Provincia,
        E.Emisor_Canton,
        E.Emisor_Distrito,
        E.Emisor_Barrio,
        E.Emisor_OtrasSenas,
        E.Emisor_OtrasSenasExtranjero,
        E.Emisor_CodigoPais,
        E.Emisor_NumTelefono,
        E.Emisor_CorreoElectronico,

        -- Receptor
        R.CodigoActividadReceptor,
        R.Receptor_Nombre,
        R.Receptor_Tipo,
        R.Receptor_Numero,
        R.Receptor_IdentificacionExtranjero,
        R.Receptor_NombreComercial,
        DR.Receptor_Provincia,
        DR.Receptor_Canton,
        DR.Receptor_Distrito,
        DR.Receptor_Barrio,
        DR.Receptor_OtrasSenas,
        DR.Receptor_OtrasSenasExtranjero,
        R.Receptor_CodigoPais,
        R.Receptor_NumTelefono,
        R.Receptor_CorreoElectronico,

        -- Condición de venta
        CASE
            WHEN T0.GroupNum = -1 THEN '01'
            WHEN UPPER(LTRIM(RTRIM(ISNULL(G.PymntGroup, '')))) IN ('CONTADO', 'CASH BASIC') THEN '01'
            ELSE '02'
        END AS CondicionVenta,

        --NULLIF(LTRIM(RTRIM(ISNULL(T0.U_LDT_CondVentaOtros,''))), '') AS CondicionVentaOtros,

        CASE
            WHEN T0.GroupNum = -1 THEN NULL
            WHEN UPPER(LTRIM(RTRIM(ISNULL(G.PymntGroup, '')))) IN ('CONTADO', 'CASH BASIC') THEN NULL
            WHEN ISNULL(G.ExtraDays, 0) <= 0 AND ISNULL(G.ExtraMonth, 0) <= 0 THEN NULL
            ELSE CAST(ISNULL(G.ExtraDays, 0) + (ISNULL(G.ExtraMonth, 0) * 30) AS VARCHAR(5))
        END AS PlazoCredito,

        CASE
            WHEN NULLIF(LTRIM(RTRIM(CAST(ISNULL(T0.U_LDT_MedPag, '') AS VARCHAR(2)))), '') IS NULL THEN '01'
            ELSE RIGHT('00' + CAST(T0.U_LDT_MedPag AS VARCHAR(2)), 2)
        END AS MedioPago,
        'Otros' AS DescPago,

        -- Detalle
        DS.DetalleServicio_NumeroLinea,
        DS.DetalleServicio_PartidaArancelaria,
        DS.DetalleServicio_CodigoProductoServicio,
        DS.DetalleServicio_TipoCodigo,
        DS.DetalleServicio_Codigo,
        DS.DetalleServicio_Cantidad,
        DS.DetalleServicio_UnidadMedida,
        DS.DetalleServicio_UnidadMedidaComercial,
        DS.DetalleServicio_Detalle,
        DS.DetalleServicio_TipoTransaccion,
        DS.DetalleServicio_NumeroVINoSerie,
        DS.DetalleServicio_PrecioUnitario,
        DS.DetalleServicio_MontoTotal,
        DS.DetalleServicio_MontoDescuento,
        DS.DetalleServicio_CodigoDescuento,
        DS.DetalleServicio_NaturalezaDescuento,
        DS.DetalleServicio_SubTotal,
        DS.DetalleServicio_RegistroMedicamento,
        DS.DetalleServicio_FormaFarmaceutica,

        -- Impuesto
        IMP.DetalleServicio_ImpuestoCodigo,
        IMP.DetalleServicio_ImpuestoCodigoImpuestoOTRO,
        IMP.DetalleServicio_ImpuestoCodigoTarifa,
        IMP.DetalleServicio_ImpuestoTarifa,
        IMP.DetalleServicio_ImpuestoFactorIVA,
        IMP.DetalleServicio_BaseImponible,
        IMP.DetalleServicio_ImpuestoMonto,
        IMP.DetalleServicio_ImpuestoNeto,
        IMP.MontoImpuestoEspecifico,
        IMP.DetalleServicio_ImpuestoEspecifico_CantidadUnidadMedida,
        IMP.DetalleServicio_ImpuestoEspecifico_Porcentaje,
        IMP.DetalleServicio_ImpuestoEspecifico_Proporcion,
        IMP.DetalleServicio_ImpuestoEspecifico_VolumenUnidadConsumo,
        IMP.DetalleServicio_ImpuestoEspecifico_ImpuestoUnidad,

        -- Exoneración
        EXO.Exoneracion_TipoDocumento,
        EXO.Exoneracion_NumeroDocumento,
        EXO.Exoneracion_NombreInstitucion,
        EXO.Exoneracion_NombreInstitucionOtros,
        EXO.Exoneracion_FechaEmision,
        EXO.Exoneracion_PorcentajeCompra,
        EXO.Exoneracion_Articulo,
        EXO.Exoneracion_Inciso,
        EXO.Exoneracion_MontoImpuesto,

        DS.DetalleServicio_MontoTotalLinea,

        -- Resumen
        RF.ResumenFactura_CodigoMoneda,
        RF.ResumenFactura_TipoCambio,
        RF.ResumenFactura_TotalServGravados,
        RF.ResumenFactura_TotalServExentos,
        RF.ResumenFactura_TotalServExonerado,
        RF.ResumenFactura_TotalServNoSujeto,
        RF.ResumenFactura_TotalMercanciasGravadas,
        RF.ResumenFactura_TotalMercanciasExentas,
        RF.ResumenFactura_TotalMercExonerada,
        RF.ResumenFactura_TotalMercNoSujeta,
        RF.ResumenFactura_TotalGravado,
        RF.ResumenFactura_TotalExento,
        RF.ResumenFactura_TotalExonerado,
        RF.ResumenFactura_TotalNoSujeto,
        RF.ResumenFactura_TotalVenta,
        RF.ResumenFactura_TotalDescuentos,
        RF.ResumenFactura_TotalVentaNeta,
        RF.ResumenFactura_TotalImpuesto,
        RF.ResumenFactura_TotalIVADevuelto,
        RF.ResumenFactura_TotalOtrosCargos,
        TCF.ResumenFactura_TotalComprobante,
        RF.OtroContenido,
        RF.OtroCargo_Monto,
        RF.OtroCargo_Detalle,
        RF.OtroCargo_TipoDocumento,
        RF.OtroCargo_Porcentaje,

        -- Referencia
        IR.Referencia_Numero,
        IR.Referencia_TipoDoc,
        IR.Referencia_FechaEmision,
        IR.Referencia_HoraEmision,
        IR.Referencia_Codigo,
        IR.Referencia_Razon,

        -- Extras
        RIGHT('00000000' + LTRIM(RTRIM(CAST(T0.DocNum AS BIGINT))), 10) AS CodSeguridad,
        T0.SlpCode AS Agente,
        CONVERT(DATE, SUBSTRING(CONVERT(VARCHAR, T0.DocDate, 103), 0, 11), 103) AS FechaComprobante,
        T0.CreateTS AS HoraComprobante,
        '' AS EnviarComoTE,
        T1.TaxOnly,
        '' AS TextoXML,
        T0.Comments AS Observaciones,
        '' AS Adenda_Tipo,
        '' AS MontoEnLetras,
        T0.NumAtCard AS Param18

    FROM ZZTEST_SBO_LARCE.dbo.OPCH T0
    CROSS APPLY (
        SELECT RIGHT('000000' + CONVERT(VARCHAR(6), ISNULL(T0.CreateTS,0)), 6) AS ts6
    ) AS TTS
    INNER JOIN ZZTEST_SBO_LARCE.dbo.PCH1 T1
        ON T0.DocEntry = T1.DocEntry
    LEFT JOIN ZZTEST_SBO_LARCE.dbo.OITM T4
        ON T1.ItemCode = T4.ItemCode
    JOIN Emisor E
        ON 1 = 1
    JOIN Receptor R
        ON 1 = 1
    JOIN DireccionReceptor DR
        ON 1 = 1
    LEFT JOIN TipoFactura TF
        ON TF.DocEntry = T0.DocEntry
    LEFT JOIN Consecutivo C
        ON 1 = 1
    LEFT JOIN ClaveGenerada K
        ON K.DocNum = T0.DocNum
    LEFT JOIN DetalleServicio DS
        ON DS.DocEntry = T0.DocEntry
       AND DS.DetalleServicio_NumeroLinea = T1.LineNum
    LEFT JOIN Impuesto IMP
        ON IMP.DocEntry = T1.DocEntry
       AND IMP.LineNum = T1.LineNum
    LEFT JOIN Exoneracion EXO
        ON EXO.DocEntry = T0.DocEntry
       AND EXO.LineNum = T1.LineNum
    LEFT JOIN ResumenFactura RF
        ON RF.DocEntry = T0.DocEntry
    LEFT JOIN TotalComprobanteFinal TCF
        ON 1 = 1
    LEFT JOIN InfoReferencia IR
        ON IR.DocEntry = T0.DocEntry
    LEFT JOIN ZZTEST_SBO_LARCE.dbo.OCTG G
        ON G.GroupNum = T0.GroupNum
    WHERE (NULLIF(@Tipo,'') IS NULL OR T0.DocType = @Tipo)
      AND T0.DocNum = @DocNum
      AND (
          T0.DocType = 'S'
          OR ISNULL(T1.TreeType, 'N') IN ('N', 'S')
      )
    ORDER BY DS.DetalleServicio_NumeroLinea;

END
