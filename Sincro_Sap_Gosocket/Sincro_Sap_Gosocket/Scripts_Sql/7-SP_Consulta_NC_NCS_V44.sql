USE [Pruebas_SincroSapGoSocket]
GO
/****** Object:  StoredProcedure [dbo].[SP_Consulta_NC_NCS_V44]    Script Date: 22/05/2026 17:58:42 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[SP_Consulta_NC_NCS_V44]
    @DocNum VARCHAR(50),
    @Situacion_de_Comprobante VARCHAR(1),
    @Tipo VARCHAR(1) 
AS
BEGIN
    SET NOCOUNT ON;

    WITH Emisor AS (
        SELECT TOP 1
            OADM.CompnyName                                  AS Emisor_Nombre,
            ISNULL(OADM.TaxIdNum, '')                        AS Emisor_Numero,

				-- Provincia
		RIGHT(
			'0' + 
			ISNULL(
				CASE 
					WHEN OADM.U_LDT_State LIKE '%-%' 
						THEN RIGHT(OADM.U_LDT_State, CHARINDEX('-', REVERSE(OADM.U_LDT_State) + '-') - 1)
					ELSE OADM.U_LDT_State
				END
			, '0')
		, 1) AS Emisor_Provincia,

		-- Cantón
		RIGHT(
			'00' + 
			ISNULL(
				CASE 
					WHEN OADM.U_LDT_County LIKE '%-%' 
						THEN RIGHT(OADM.U_LDT_County, CHARINDEX('-', REVERSE(OADM.U_LDT_County) + '-') - 1)
					ELSE OADM.U_LDT_County
				END
			, '0')
		, 2) AS Emisor_Canton,

		-- Distrito
		RIGHT(
			'00' + 
			ISNULL(
				CASE 
					WHEN OADM.U_LDT_District LIKE '%-%' 
						THEN RIGHT(OADM.U_LDT_District, CHARINDEX('-', REVERSE(OADM.U_LDT_District) + '-') - 1)
					ELSE OADM.U_LDT_District
				END
			, '0')
		, 2) AS Emisor_Distrito,

		-- Barrio (normalmente no es código numérico, se deja igual)
		ISNULL(OADM.U_LDT_Nom_NeighB, '') AS Emisor_Barrio,

            --ISNULL(OADM.U_LDT_State, '')                     AS Emisor_Provincia,
            --RIGHT('00' + ISNULL(OADM.U_LDT_County,''), 2)    AS Emisor_Canton,
            --RIGHT('00' + ISNULL(OADM.U_LDT_District,''), 2)  AS Emisor_Distrito,
            --OADM.U_LDT_Nom_NeighB                            AS Emisor_Barrio,
            ISNULL(OADM.U_LDT_Nom_State, '')                 AS Emisor_Provincia_Nombre,
            ISNULL(OADM.U_LDT_Nom_County, '')                AS Emisor_Canton_Nombre,
            ISNULL(OADM.U_LDT_Nom_District,'')               AS Emisor_Distrito_Nombre,
            ISNULL(OADM.U_LDT_Nom_NeighB, '')                AS Emisor_Barrio_Nombre,
            --ISNULL(OADM.U_LDT_Direccion, '')                 AS Emisor_OtrasSenas,
			 ISNULL(OADM.CompnyAddr, '')                 AS Emisor_OtrasSenas,
            '506'                                            AS Emisor_CodigoPais,
            ISNULL(OADM.Phone1,'')                           AS Emisor_NumTelefono,
            ISNULL(OADM.Fax,'')                              AS Emisor_Fax,
            ISNULL(OADM.E_Mail,'')                           AS Emisor_CorreoElectronico,
            NULLIF(OADM.U_LDT_ActEconomica, '')              AS CodigoActividadEconomica,
            NULLIF(RIGHT('00' + LTRIM(RTRIM(CONVERT(VARCHAR(10), ISNULL(OADM.U_LDT_IDType, '')))), 2), '00') AS Emisor_Tipo,
            ISNULL(OADM.AliasName, OADM.CompnyName)          AS Emisor_NombreComercial,
            ''                                               AS Emisor_Registrofiscal8707
        FROM ZZTEST_SBO_LARCE.dbo.OADM AS OADM
    ),

    Receptor AS (
        SELECT
            RIN.U_LDT_ActEcoRec AS CodigoActividadReceptor,     
            NULLIF(RIGHT('00' + LTRIM(RTRIM(CONVERT(VARCHAR(10), ISNULL(T0.U_LDT_IDType, '')))), 2), '00') AS Receptor_Tipo,
            T0.CardCode         AS CodCliente,
            T0.CardName         AS Receptor_Nombre,
		    CONVERT(BIGINT, REPLACE(REPLACE(LTRIM(RTRIM(T0.LicTradNum)), '-', ''), ' ', '')) AS Receptor_Numero,
            T0.CardFName        AS Receptor_NombreComercial,
            T0.Phone1           AS Receptor_NumTelefono,
            T0.Phone2           AS Receptor_Fax,
            CASE 
				WHEN ISNULL(LTRIM(RTRIM(T0.E_Mail)), '') <> '' 
					 AND ISNULL(LTRIM(RTRIM(T0.IntrntSite)), '') <> ''
					THEN T0.E_Mail + ';' + T0.IntrntSite

				WHEN ISNULL(LTRIM(RTRIM(T0.E_Mail)), '') <> ''
					THEN T0.E_Mail

				WHEN ISNULL(LTRIM(RTRIM(T0.IntrntSite)), '') <> ''
					THEN T0.IntrntSite

				ELSE ''
			END AS Receptor_CorreoElectronico,
            '506'               AS Receptor_CodigoPais,
            ''                  AS Receptor_IdentificacionExtranjero
        FROM ZZTEST_SBO_LARCE.dbo.OCRD AS T0
        INNER JOIN ZZTEST_SBO_LARCE.dbo.ORIN RIN ON RIN.CardCode = T0.CardCode AND RIN.DocNum = @DocNum
    ),
    --DireccionReceptor AS (
    --    SELECT 
    --        CardCode,
    --        U_LDT_State                         AS Receptor_Provincia,
    --        RIGHT('00' + ISNULL(U_LDT_County,''), 2)   AS Receptor_Canton,
    --        RIGHT('00' + ISNULL(U_LDT_District,''), 2) AS Receptor_Distrito,
    --        U_LDT_Nom_NeighB                   AS Receptor_Barrio,
    --        ''                                 AS Receptor_OtrasSenasExtranjero,
    --        U_LDT_Direccion                    AS Receptor_OtrasSenas
    --    FROM ZZTEST_SBO_LARCE.dbo.OCRD
    --), 
	 DireccionReceptor AS (
    SELECT 
        CardCode,

        -- Provincia: toma el último segmento si viene con guiones; si no, el valor completo
        RIGHT(
            '0' + ISNULL(
                CASE 
                    WHEN ISNULL(U_LDT_State, '') LIKE '%-%'
                        THEN RIGHT(U_LDT_State, CHARINDEX('-', REVERSE(U_LDT_State) + '-') - 1)
                    ELSE U_LDT_State
                END
            , '0')
        , 1) AS Receptor_Provincia,

        -- Cantón: toma el último segmento si viene con guiones; si no, el valor completo
        RIGHT(
            '00' + ISNULL(
                CASE 
                    WHEN ISNULL(U_LDT_County, '') LIKE '%-%'
                        THEN RIGHT(U_LDT_County, CHARINDEX('-', REVERSE(U_LDT_County) + '-') - 1)
                    ELSE U_LDT_County
                END
            , '0')
        , 2) AS Receptor_Canton,

        -- Distrito: toma el último segmento si viene con guiones; si no, el valor completo
        RIGHT(
            '00' + ISNULL(
                CASE 
                    WHEN ISNULL(U_LDT_District, '') LIKE '%-%'
                        THEN RIGHT(U_LDT_District, CHARINDEX('-', REVERSE(U_LDT_District) + '-') - 1)
                    ELSE U_LDT_District
                END
            , '0')
        , 2) AS Receptor_Distrito,
		U_LDT_Nom_NeighB                           AS Receptor_Barrio,
        ---- Barrio: toma el último segmento si viene con guiones; si no, el valor completo
        --RIGHT(
        --    '00' + ISNULL(
        --        CASE 
        --            WHEN ISNULL(U_LDT_Nom_NeighB, '') LIKE '%-%'
        --                THEN RIGHT(U_LDT_Nom_NeighB, CHARINDEX('-', REVERSE(U_LDT_Nom_NeighB) + '-') - 1)
        --            ELSE U_LDT_Nom_NeighB
        --        END
        --    , '0')
        --, 2) AS Receptor_Barrio,

        '' AS Receptor_OtrasSenasExtranjero,
        U_LDT_Direccion AS Receptor_OtrasSenas
    FROM ZZTEST_SBO_LARCE.dbo.OCRD
),
	 DocumentoFiscal AS (
        SELECT TOP 1
            CASE 
                WHEN LTRIM(RTRIM(ISNULL(E.Emisor_Numero, ''))) = LTRIM(RTRIM(ISNULL(CAST(R.Receptor_Numero AS VARCHAR(20)), '')))
                THEN 1
                ELSE 0
            END AS EsMismoContribuyente
        FROM Emisor E
        CROSS JOIN Receptor R
    ),
 
 ReferenciaTipoDocumento AS (
    SELECT
        NC.DocEntry,

        BD.BaseType,
        BD.BaseEntry,

        RefInv.DocEntry  AS RefDocEntryManual,
        RefInv.DocNum    AS RefDocNumManual,

        NULLIF(LTRIM(RTRIM(NC.NumAtCard COLLATE DATABASE_DEFAULT)), '') AS NumAtCard,

        COALESCE(
            -- 1. Copiar a / DocEntry / sistema nuevo
            CONVERT(VARCHAR(50), DPBase.Clave) COLLATE DATABASE_DEFAULT,

            -- 2. Copiar a / DocEntry / sistema viejo
            CASE
                WHEN LEN(NULLIF(LTRIM(RTRIM(RefBase.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT)), '')) = 50
                 AND NULLIF(LTRIM(RTRIM(RefBase.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT)), '') NOT LIKE '%[^0-9]%'
                THEN NULLIF(LTRIM(RTRIM(RefBase.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT)), '')
            END,

            -- 3. NumAtCard / sistema nuevo
            CONVERT(VARCHAR(50), DPManual.Clave) COLLATE DATABASE_DEFAULT,

            -- 4. NumAtCard / sistema viejo
            CASE
                WHEN LEN(NULLIF(LTRIM(RTRIM(RefInv.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT)), '')) = 50
                 AND NULLIF(LTRIM(RTRIM(RefInv.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT)), '') NOT LIKE '%[^0-9]%'
                THEN NULLIF(LTRIM(RTRIM(RefInv.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT)), '')
            END
        ) AS Referencia_Clave,

        CASE
            WHEN COALESCE(
                    CONVERT(VARCHAR(50), DPBase.Clave) COLLATE DATABASE_DEFAULT,
                    CASE
                        WHEN LEN(NULLIF(LTRIM(RTRIM(RefBase.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT)), '')) = 50
                         AND NULLIF(LTRIM(RTRIM(RefBase.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT)), '') NOT LIKE '%[^0-9]%'
                        THEN NULLIF(LTRIM(RTRIM(RefBase.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT)), '')
                    END,
                    CONVERT(VARCHAR(50), DPManual.Clave) COLLATE DATABASE_DEFAULT,
                    CASE
                        WHEN LEN(NULLIF(LTRIM(RTRIM(RefInv.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT)), '')) = 50
                         AND NULLIF(LTRIM(RTRIM(RefInv.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT)), '') NOT LIKE '%[^0-9]%'
                        THEN NULLIF(LTRIM(RTRIM(RefInv.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT)), '')
                    END
                 ) IS NOT NULL
            THEN SUBSTRING(
                    COALESCE(
                        CONVERT(VARCHAR(50), DPBase.Clave) COLLATE DATABASE_DEFAULT,
                        CASE
                            WHEN LEN(NULLIF(LTRIM(RTRIM(RefBase.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT)), '')) = 50
                             AND NULLIF(LTRIM(RTRIM(RefBase.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT)), '') NOT LIKE '%[^0-9]%'
                            THEN NULLIF(LTRIM(RTRIM(RefBase.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT)), '')
                        END,
                        CONVERT(VARCHAR(50), DPManual.Clave) COLLATE DATABASE_DEFAULT,
                        CASE
                            WHEN LEN(NULLIF(LTRIM(RTRIM(RefInv.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT)), '')) = 50
                             AND NULLIF(LTRIM(RTRIM(RefInv.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT)), '') NOT LIKE '%[^0-9]%'
                            THEN NULLIF(LTRIM(RTRIM(RefInv.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT)), '')
                        END
                    ),
                    30,
                    2
                 )

            WHEN BD.BaseType = 14 THEN '03'
            WHEN BD.BaseType = 203 THEN '02'
            WHEN BD.BaseType = 13 THEN '01'

            WHEN RefInv.DocEntry IS NOT NULL THEN
                CASE WHEN ISNULL(RefInv.DocSubType, '') = 'DN' THEN '02' ELSE '01' END

            ELSE '01'
        END AS Referencia_TipoDocReal,

        COALESCE(DPBase.TaxDate, RefBase.DocDate, DPManual.TaxDate, RefInv.DocDate) AS Referencia_DocDate,
        COALESCE(DPBase.CreateDateTime, RefBase.CreateDate, DPManual.CreateDateTime, RefInv.CreateDate) AS Referencia_CreateDate,
        COALESCE(RefBase.CreateTS, RefInv.CreateTS) AS Referencia_CreateTS,

        CASE
            WHEN COALESCE(
                    CONVERT(VARCHAR(50), DPBase.Clave) COLLATE DATABASE_DEFAULT,
                    CONVERT(VARCHAR(50), DPManual.Clave) COLLATE DATABASE_DEFAULT,
                    RefBase.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT,
                    RefInv.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT
                 ) IS NOT NULL
             AND SUBSTRING(
                    COALESCE(
                        CONVERT(VARCHAR(50), DPBase.Clave) COLLATE DATABASE_DEFAULT,
                        CONVERT(VARCHAR(50), DPManual.Clave) COLLATE DATABASE_DEFAULT,
                        RefBase.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT,
                        RefInv.U_LDT_FiscalDoc COLLATE DATABASE_DEFAULT
                    ),
                    30,
                    2
                 ) = '04'
            THEN 1
            ELSE 0
        END AS Referencia_EsTiquete

    FROM ZZTEST_SBO_LARCE.dbo.ORIN NC

    OUTER APPLY (
        SELECT TOP 1
            R1.BaseType,
            R1.BaseEntry,
            R1.BaseLine
        FROM ZZTEST_SBO_LARCE.dbo.RIN1 R1
        WHERE R1.DocEntry = NC.DocEntry
          AND R1.BaseEntry IS NOT NULL
        ORDER BY R1.LineNum
    ) BD

    OUTER APPLY (
        SELECT TOP 1
            I.DocEntry,
            I.DocNum,
            I.DocDate,
            I.CreateDate,
            I.CreateTS,
            I.DocSubType,
            I.U_LDT_FiscalDoc
        FROM ZZTEST_SBO_LARCE.dbo.OINV I
        WHERE I.DocEntry = BD.BaseEntry
          AND BD.BaseType = 13
    ) RefBase

    OUTER APPLY (
        SELECT TOP 1
            I.DocEntry,
            I.DocNum,
            I.DocDate,
            I.CreateDate,
            I.CreateTS,
            I.DocSubType,
            I.U_LDT_FiscalDoc
        FROM ZZTEST_SBO_LARCE.dbo.OINV I
        WHERE I.DocNum = TRY_CONVERT(INT, NULLIF(LTRIM(RTRIM(NC.NumAtCard)), ''))
        ORDER BY I.DocEntry DESC
    ) RefInv

    LEFT JOIN [Pruebas_SincroSapGoSocket].[Integration].[DocumentosPendientes] DPBase
        ON DPBase.DocEntry = BD.BaseEntry
       AND DPBase.ObjType = CONVERT(VARCHAR(10), BD.BaseType)
       AND DPBase.Clave IS NOT NULL

    LEFT JOIN [Pruebas_SincroSapGoSocket].[Integration].[DocumentosPendientes] DPManual
        ON DPManual.DocEntry = RefInv.DocEntry
       AND DPManual.ObjType = '13'
       AND DPManual.Clave IS NOT NULL

    WHERE NC.DocNum = @DocNum
      AND NC.DocType = @Tipo
),

InfoReferencia AS (
    SELECT
        RTD.DocEntry,

        COALESCE(
            RTD.Referencia_Clave,
            RIGHT(
                '00000000' +
                LTRIM(RTRIM(CONVERT(VARCHAR(20), TRY_CONVERT(BIGINT, RTD.NumAtCard))))
            , 8)
        ) AS Referencia_Numero,

        ISNULL(
            NULLIF(LTRIM(RTRIM(NC.U_LDT_TipoDocRef COLLATE DATABASE_DEFAULT)), ''),
            RTD.Referencia_TipoDocReal
        ) AS Referencia_TipoDoc,

        CASE
            WHEN RTD.Referencia_DocDate IS NOT NULL
                THEN CONVERT(VARCHAR(19), CAST(RTD.Referencia_DocDate AS DATETIME), 126) + '-06:00'
            ELSE NULL
        END AS Referencia_FechaEmision,

        CASE
            WHEN RTD.Referencia_CreateTS IS NOT NULL
                THEN CAST(
                        STUFF(
                            STUFF(
                                RIGHT('000000' + CONVERT(VARCHAR(6), ISNULL(RTD.Referencia_CreateTS,0)), 6),
                                3, 0, ':'
                            ),
                            6, 0, ':'
                        ) AS TIME
                     )
            ELSE NULL
        END AS Referencia_HoraEmision,

        RIGHT('00' + ISNULL(NULLIF(LTRIM(RTRIM(NC.U_LDT_CodRef COLLATE DATABASE_DEFAULT)), ''), '01'), 2) AS Referencia_Codigo,

        CASE ISNULL(NULLIF(NC.U_LDT_CodRef COLLATE DATABASE_DEFAULT, ''), '01')
            WHEN '01' THEN 'Anula documento de referencia'
            WHEN '02' THEN 'Corrige texto del documento'
            WHEN '03' THEN 'Corrige monto'
            WHEN '04' THEN 'Referencia a otro documento'
            WHEN '05' THEN 'Sustituye comprobante provisional por contingencia'
            WHEN '06' THEN 'Devolución de mercancía'
            WHEN '07' THEN 'Sustituye comprobante electrónico'
            WHEN '08' THEN 'Factura endosada'
            WHEN '09' THEN 'Nota de crédito financiera'
            WHEN '10' THEN 'Nota de débito financiera'
            WHEN '11' THEN 'Proveedor no domiciliado'
            WHEN '12' THEN 'Crédito por exoneración posterior a la facturación'
            WHEN '99' THEN 'Otros'
            ELSE 'Otros'
        END AS Referencia_Razon,

        RTD.Referencia_EsTiquete

    FROM ReferenciaTipoDocumento RTD
    INNER JOIN ZZTEST_SBO_LARCE.dbo.ORIN NC
        ON NC.DocEntry = RTD.DocEntry
),
--ReferenciaTipoDocumento AS (
--    SELECT
--        NC.DocEntry,

--        -- referencia por vínculo SAP (línea base)
--        BD.BaseType,
--        BD.BaseEntry,

--        -- referencia manual
--        RefInv.DocEntry  AS RefDocEntryManual,
--        RefInv.DocNum    AS RefDocNumManual,
--        RefInv.DocDate   AS Referencia_DocDate,
--        RefInv.CreateTS  AS Referencia_CreateTS,
--        NULLIF(LTRIM(RTRIM(NC.NumAtCard)), '') AS NumAtCard,

--        -- clave preferida: primero la del vínculo real, si no la manual
--        COALESCE(DPBase.Clave, DPManual.Clave) AS Referencia_Clave,

--        CASE
--            WHEN DPBase.Clave IS NOT NULL AND LEN(DPBase.Clave) >= 31
--                THEN SUBSTRING(DPBase.Clave, 30, 2)

--            WHEN DPManual.Clave IS NOT NULL AND LEN(DPManual.Clave) >= 31
--                THEN SUBSTRING(DPManual.Clave, 30, 2)

--            WHEN BD.BaseType = 14 THEN '03'
--            WHEN BD.BaseType = 203 THEN '02'
--            WHEN BD.BaseType = 13 THEN '01'

--            -- fallback manual por OINV
--            WHEN RefInv.DocEntry IS NOT NULL THEN
--                CASE WHEN ISNULL(RefInv.DocSubType, '') = 'DN' THEN '02' ELSE '01' END

--            ELSE '01'
--        END AS Referencia_TipoDocReal,

--        CASE
--            WHEN DPBase.Clave IS NOT NULL
--                 AND LEN(DPBase.Clave) >= 31
--                 AND SUBSTRING(DPBase.Clave, 30, 2) = '04'
--                THEN 1
--            WHEN DPManual.Clave IS NOT NULL
--                 AND LEN(DPManual.Clave) >= 31
--                 AND SUBSTRING(DPManual.Clave, 30, 2) = '04'
--                THEN 1
--            ELSE 0
--        END AS Referencia_EsTiquete
--    FROM ZZTEST_SBO_LARCE.dbo.ORIN NC

--    OUTER APPLY (
--        SELECT TOP 1
--            R1.BaseType,
--            R1.BaseEntry,
--            R1.BaseLine
--        FROM ZZTEST_SBO_LARCE.dbo.RIN1 R1
--        WHERE R1.DocEntry = NC.DocEntry
--          AND R1.BaseEntry IS NOT NULL
--        ORDER BY R1.LineNum
--    ) BD

--    OUTER APPLY (
--        SELECT TOP 1
--            I.DocEntry,
--            I.DocNum,
--            I.DocDate,
--            I.CreateTS,
--            I.DocSubType
--        FROM ZZTEST_SBO_LARCE.dbo.OINV I
--        WHERE I.DocNum = TRY_CONVERT(INT, NULLIF(LTRIM(RTRIM(NC.NumAtCard)), ''))
--        ORDER BY I.DocEntry DESC
--    ) RefInv

--    LEFT JOIN [Pruebas_SincroSapGoSocket].[Integration].[DocumentosPendientes] DPBase
--        ON DPBase.DocEntry = BD.BaseEntry
--       AND DPBase.ObjType = CONVERT(VARCHAR(10), BD.BaseType)
--       AND DPBase.Clave IS NOT NULL

--    LEFT JOIN [Pruebas_SincroSapGoSocket].[Integration].[DocumentosPendientes] DPManual
--        ON DPManual.DocEntry = RefInv.DocEntry
--       AND DPManual.ObjType = '13'
--       AND DPManual.Clave IS NOT NULL

--    WHERE NC.DocNum = @DocNum
--      AND NC.DocType = @Tipo
--),
 
--InfoReferencia AS (
--    SELECT
--        RTD.DocEntry,

--        COALESCE(
--            RTD.Referencia_Clave,
--            RIGHT(
--                '00000000' +
--                LTRIM(RTRIM(CONVERT(VARCHAR(20), TRY_CONVERT(BIGINT, RTD.NumAtCard))))
--            , 8)
--        ) AS Referencia_Numero,

--        ISNULL(
--            NULLIF(LTRIM(RTRIM(NC.U_LDT_TipoDocRef)), ''),
--            RTD.Referencia_TipoDocReal
--        ) AS Referencia_TipoDoc,

--        CASE
--            WHEN DPBase.TaxDate IS NOT NULL
--                THEN CONVERT(VARCHAR(19), CAST(DPBase.TaxDate AS DATETIME), 126) + '-06:00'
--            WHEN RTD.Referencia_DocDate IS NOT NULL
--                THEN CONVERT(VARCHAR(19), CAST(RTD.Referencia_DocDate AS DATETIME), 126) + '-06:00'
--            ELSE NULL
--        END AS Referencia_FechaEmision,

--        CASE
--            WHEN DPBase.CreateDateTime IS NOT NULL
--                THEN CAST(DPBase.CreateDateTime AS DATETIME2(0))
--            WHEN RTD.Referencia_CreateTS IS NOT NULL
--                THEN CAST(
--                        STUFF(
--                            STUFF(
--                                RIGHT('000000' + CONVERT(VARCHAR(6), ISNULL(RTD.Referencia_CreateTS,0)), 6),
--                                3, 0, ':'
--                            ),
--                            6, 0, ':'
--                        ) AS TIME
--                     )
--            ELSE NULL
--        END AS Referencia_HoraEmision,

--        RIGHT('00' + ISNULL(NULLIF(LTRIM(RTRIM(NC.U_LDT_CodRef)), ''), '01'), 2) AS Referencia_Codigo,

--        CASE ISNULL(NULLIF(NC.U_LDT_CodRef, ''), '01')
--            WHEN '01' THEN 'Anula documento de referencia'
--            WHEN '02' THEN 'Corrige texto del documento'
--            WHEN '03' THEN 'Corrige monto'
--            WHEN '04' THEN 'Referencia a otro documento'
--            WHEN '05' THEN 'Sustituye comprobante provisional por contingencia'
--            WHEN '06' THEN 'Devolución de mercancía'
--            WHEN '07' THEN 'Sustituye comprobante electrónico'
--            WHEN '08' THEN 'Factura endosada'
--            WHEN '09' THEN 'Nota de crédito financiera'
--            WHEN '10' THEN 'Nota de débito financiera'
--            WHEN '11' THEN 'Proveedor no domiciliado'
--            WHEN '12' THEN 'Crédito por exoneración posterior a la facturación'
--            WHEN '99' THEN 'Otros'
--            ELSE 'Otros'
--        END AS Referencia_Razon,

--        RTD.Referencia_EsTiquete
--    FROM ReferenciaTipoDocumento RTD
--    INNER JOIN ZZTEST_SBO_LARCE.dbo.ORIN NC
--        ON NC.DocEntry = RTD.DocEntry
--    LEFT JOIN [Pruebas_SincroSapGoSocket].[Integration].[DocumentosPendientes] DPBase
--        ON DPBase.DocEntry = RTD.BaseEntry
--       AND DPBase.ObjType = CONVERT(VARCHAR(10), RTD.BaseType)
--       AND DPBase.Clave IS NOT NULL
--),

--ReferenciaTipoDocumento AS (
--    SELECT
--        NC.DocEntry,

--        BD.BaseType,
--        BD.BaseEntry,
--        BD.BaseLine,

--        RefInv.DocEntry  AS RefDocEntryManual,
--        RefInv.DocNum    AS RefDocNumManual,
--        RefInv.DocDate   AS Referencia_DocDateManual,
--        RefInv.CreateTS  AS Referencia_CreateTSManual,
--        RefInv.DocSubType AS Referencia_DocSubTypeManual,

--        RefBase.DocEntry AS RefDocEntryBase,
--        RefBase.DocNum   AS RefDocNumBase,
--        RefBase.DocDate  AS Referencia_DocDateBase,
--        RefBase.CreateTS AS Referencia_CreateTSBase,
--        RefBase.DocSubType AS Referencia_DocSubTypeBase,

--        NULLIF(LTRIM(RTRIM(NC.NumAtCard)), '') AS NumAtCard,

--        -- Mantiene fallback actual cuando no hay clave
--        CASE
--            WHEN BD.BaseType = 14 THEN '03'
--            WHEN BD.BaseType = 203 THEN '02'
--            WHEN BD.BaseType = 13 THEN '01'
--            WHEN RefInv.DocEntry IS NOT NULL THEN
--                CASE WHEN ISNULL(RefInv.DocSubType, '') = 'DN' THEN '02' ELSE '01' END
--            ELSE '01'
--        END AS Referencia_TipoDocFallback

--    FROM ZZTEST_SBO_LARCE.dbo.ORIN NC
--	--Escenario cuando se usa la opcion de COPIAR A
--    OUTER APPLY (
--        SELECT TOP 1
--            R1.BaseType,
--            R1.BaseEntry,
--            R1.BaseLine
--        FROM ZZTEST_SBO_LARCE.dbo.RIN1 R1
--        WHERE R1.DocEntry = NC.DocEntry
--          AND R1.BaseEntry IS NOT NULL
--        ORDER BY R1.LineNum
--    ) BD
--		--Escenario cuando se usa la opcion de COPIAR A para obtener la clave del campo viejo U_LDT_FiscalDoc
--    OUTER APPLY (
--        SELECT TOP 1
--            I.DocEntry,
--            I.DocNum,
--            I.DocDate,
--            I.CreateTS,
--            I.DocSubType,
--            I.U_LDT_FiscalDoc
--        FROM ZZTEST_SBO_LARCE.dbo.OINV I
--        WHERE I.DocEntry = BD.BaseEntry
--          AND BD.BaseType = 13
--    ) RefBase
--	--Escenario cuando se usa la opcion de NumAtCard una NC manual
--    OUTER APPLY (
--        SELECT TOP 1
--            I.DocEntry,
--            I.DocNum,
--            I.DocDate,
--            I.CreateTS,
--            I.DocSubType,
--            I.U_LDT_FiscalDoc
--        FROM ZZTEST_SBO_LARCE.dbo.OINV I
--        WHERE I.DocNum = TRY_CONVERT(INT, NULLIF(LTRIM(RTRIM(NC.NumAtCard)), ''))
--        ORDER BY I.DocEntry DESC
--    ) RefInv

--    WHERE NC.DocNum = @DocNum
--      AND NC.DocType = @Tipo
--),

--ClaveReferencia AS (
--    SELECT
--        RTD.*,

--        COALESCE(
--            -- 1. Clave sistema nuevo por vínculo SAP Copiar a
--            DPBase.Clave,

--            -- 2. Clave sistema viejo por vínculo SAP Copiar a
--            CASE
--                WHEN LEN(NULLIF(LTRIM(RTRIM(RefBase.U_LDT_FiscalDoc)), '')) = 50
--                 AND NULLIF(LTRIM(RTRIM(RefBase.U_LDT_FiscalDoc)), '') NOT LIKE '%[^0-9]%'
--                THEN NULLIF(LTRIM(RTRIM(RefBase.U_LDT_FiscalDoc)), '')
--            END,

--            -- 3. Clave sistema nuevo por NumAtCard
--            DPManual.Clave,

--            -- 4. Clave sistema viejo por NumAtCard
--            CASE
--                WHEN LEN(NULLIF(LTRIM(RTRIM(RefManual.U_LDT_FiscalDoc)), '')) = 50
--                 AND NULLIF(LTRIM(RTRIM(RefManual.U_LDT_FiscalDoc)), '') NOT LIKE '%[^0-9]%'
--                THEN NULLIF(LTRIM(RTRIM(RefManual.U_LDT_FiscalDoc)), '')
--            END
--        ) AS ClaveReferencia,

--        CASE
--            WHEN DPBase.Clave IS NOT NULL THEN 'DP_BASE'
--            WHEN LEN(NULLIF(LTRIM(RTRIM(RefBase.U_LDT_FiscalDoc)), '')) = 50
--             AND NULLIF(LTRIM(RTRIM(RefBase.U_LDT_FiscalDoc)), '') NOT LIKE '%[^0-9]%' THEN 'LEGACY_BASE'
--            WHEN DPManual.Clave IS NOT NULL THEN 'DP_MANUAL'
--            WHEN LEN(NULLIF(LTRIM(RTRIM(RefManual.U_LDT_FiscalDoc)), '')) = 50
--             AND NULLIF(LTRIM(RTRIM(RefManual.U_LDT_FiscalDoc)), '') NOT LIKE '%[^0-9]%' THEN 'LEGACY_MANUAL'
--            ELSE 'SIN_CLAVE'
--        END AS OrigenClaveReferencia,

--        COALESCE(DPBase.TaxDate, DPManual.TaxDate) AS Referencia_TaxDateDP,
--        COALESCE(DPBase.CreateDateTime, DPManual.CreateDateTime) AS Referencia_CreateDateTimeDP

--    FROM ReferenciaTipoDocumento RTD

--    LEFT JOIN [Pruebas_SincroSapGoSocket].[Integration].[DocumentosPendientes] DPBase
--        ON DPBase.DocEntry = RTD.BaseEntry
--       AND DPBase.ObjType = CONVERT(VARCHAR(10), RTD.BaseType)
--       AND DPBase.Clave IS NOT NULL

--    LEFT JOIN ZZTEST_SBO_LARCE.dbo.OINV RefBase
--        ON RefBase.DocEntry = RTD.BaseEntry
--       AND RTD.BaseType = 13

--    LEFT JOIN [Pruebas_SincroSapGoSocket].[Integration].[DocumentosPendientes] DPManual
--        ON DPManual.DocEntry = RTD.RefDocEntryManual
--       AND DPManual.ObjType = '13'
--       AND DPManual.Clave IS NOT NULL

--    LEFT JOIN ZZTEST_SBO_LARCE.dbo.OINV RefManual
--        ON RefManual.DocEntry = RTD.RefDocEntryManual
--),

--InfoReferencia AS (
--    SELECT
--        CR.DocEntry,

--CAST(
--    COALESCE(
--        CAST(CR.ClaveReferencia AS VARCHAR(50)) COLLATE DATABASE_DEFAULT,
--        RIGHT(
--            '00000000' COLLATE DATABASE_DEFAULT +
--            LTRIM(RTRIM(CONVERT(VARCHAR(20), TRY_CONVERT(BIGINT, CR.NumAtCard)))) COLLATE DATABASE_DEFAULT
--        , 8)
--    )
--AS VARCHAR(50)) AS Referencia_Numero,

-- CAST(
--    ISNULL(
--        NULLIF(
--            LTRIM(RTRIM(CAST(NC.U_LDT_TipoDocRef AS VARCHAR(10)) COLLATE DATABASE_DEFAULT)),
--            '' COLLATE DATABASE_DEFAULT
--        ),
--        CAST(
--            CASE
--                WHEN CR.ClaveReferencia IS NOT NULL
--                 AND LEN(CAST(CR.ClaveReferencia AS VARCHAR(50)) COLLATE DATABASE_DEFAULT) >= 31
--                THEN SUBSTRING(CAST(CR.ClaveReferencia AS VARCHAR(50)) COLLATE DATABASE_DEFAULT, 30, 2)

--                ELSE CAST(CR.Referencia_TipoDocFallback AS VARCHAR(2)) COLLATE DATABASE_DEFAULT
--            END
--        AS VARCHAR(2)) COLLATE DATABASE_DEFAULT
--    )
--AS VARCHAR(2)) AS Referencia_TipoDoc,

--        CASE
--            WHEN CR.Referencia_TaxDateDP IS NOT NULL
--                THEN CONVERT(VARCHAR(19), CAST(CR.Referencia_TaxDateDP AS DATETIME), 126) + '-06:00'

--            WHEN CR.Referencia_DocDateBase IS NOT NULL
--                THEN CONVERT(VARCHAR(19), CAST(CR.Referencia_DocDateBase AS DATETIME), 126) + '-06:00'

--            WHEN CR.Referencia_DocDateManual IS NOT NULL
--                THEN CONVERT(VARCHAR(19), CAST(CR.Referencia_DocDateManual AS DATETIME), 126) + '-06:00'

--            ELSE NULL
--        END AS Referencia_FechaEmision,

--        CASE
--            WHEN CR.Referencia_CreateDateTimeDP IS NOT NULL
--                THEN CAST(CR.Referencia_CreateDateTimeDP AS DATETIME2(0))

--            WHEN CR.Referencia_CreateTSBase IS NOT NULL
--                THEN CAST(
--                    STUFF(
--                        STUFF(
--                            RIGHT('000000' + CONVERT(VARCHAR(6), ISNULL(CR.Referencia_CreateTSBase, 0)), 6),
--                            3, 0, ':'
--                        ),
--                        6, 0, ':'
--                    ) AS TIME
--                )

--            WHEN CR.Referencia_CreateTSManual IS NOT NULL
--                THEN CAST(
--                    STUFF(
--                        STUFF(
--                            RIGHT('000000' + CONVERT(VARCHAR(6), ISNULL(CR.Referencia_CreateTSManual, 0)), 6),
--                            3, 0, ':'
--                        ),
--                        6, 0, ':'
--                    ) AS TIME
--                )

--            ELSE NULL
--        END AS Referencia_HoraEmision,

--        RIGHT('00' + ISNULL(NULLIF(LTRIM(RTRIM(NC.U_LDT_CodRef)), ''), '01'), 2) AS Referencia_Codigo,

--        CASE ISNULL(NULLIF(NC.U_LDT_CodRef, ''), '01')
--            WHEN '01' THEN 'Anula documento de referencia'
--            WHEN '02' THEN 'Corrige texto del documento'
--            WHEN '03' THEN 'Corrige monto'
--            WHEN '04' THEN 'Referencia a otro documento'
--            WHEN '05' THEN 'Sustituye comprobante provisional por contingencia'
--            WHEN '06' THEN 'Devolución de mercancía'
--            WHEN '07' THEN 'Sustituye comprobante electrónico'
--            WHEN '08' THEN 'Factura endosada'
--            WHEN '09' THEN 'Nota de crédito financiera'
--            WHEN '10' THEN 'Nota de débito financiera'
--            WHEN '11' THEN 'Proveedor no domiciliado'
--            WHEN '12' THEN 'Crédito por exoneración posterior a la facturación'
--            WHEN '99' THEN 'Otros'
--            ELSE 'Otros'
--        END AS Referencia_Razon,

--        CASE
--            WHEN CR.ClaveReferencia IS NOT NULL
--             AND LEN(CR.ClaveReferencia) >= 31
--             AND SUBSTRING(CR.ClaveReferencia, 30, 2) = '04'
--            THEN 1
--            ELSE 0
--        END AS Referencia_EsTiquete,

--        CR.OrigenClaveReferencia

--    FROM ClaveReferencia CR
--    INNER JOIN ZZTEST_SBO_LARCE.dbo.ORIN NC
--        ON NC.DocEntry = CR.DocEntry
--),
    TipoFactura AS (
        SELECT
            T0.DocEntry,
			--CASE
			--	WHEN MAX(ISNULL(C.U_LDT_TipoDocElect, '')) = '04' THEN '09' -- Exportación
			--	ELSE
			--	'03'  
			--END AS CodigoTipoDocumento	  
            --CASE
            --    WHEN SUM(CASE WHEN Cabys.TipoCabys = 'S' THEN 1 ELSE 0 END) = COUNT(*) THEN 'NCS'
            --    ELSE 'NC'
            --END AS TipoFactura,
            '03' AS CodigoTipoDocumento	  

        FROM ZZTEST_SBO_LARCE.dbo.ORIN T0
        INNER JOIN ZZTEST_SBO_LARCE.dbo.RIN1 T1
            ON T0.DocEntry = T1.DocEntry
        INNER JOIN ZZTEST_SBO_LARCE.dbo.OITM T4
            ON T1.ItemCode = T4.ItemCode
        INNER JOIN ZZTEST_SBO_LARCE.dbo.OCRD C
            ON T0.CardCode = C.CardCode
        CROSS APPLY (
            SELECT CASE
                WHEN LEFT(ISNULL(T4.U_LDT_CABYS,''),1) BETWEEN '0' AND '4' THEN 'M'
                WHEN LEFT(ISNULL(T4.U_LDT_CABYS,''),1) BETWEEN '5' AND '9' THEN 'S'
                ELSE ''
            END AS TipoCabys
        ) Cabys
        WHERE T0.DocNum = @DocNum
          AND ISNULL(T1.TreeType, 'N') IN ('N', 'S', 'P')
        GROUP BY T0.DocEntry
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
        '506' + 
        SUBSTRING(CONVERT(varchar, T0.DocDate, 103), 1, 2) +
        SUBSTRING(CONVERT(varchar, T0.DocDate, 103), 4, 2) +
        SUBSTRING(CONVERT(varchar, T0.DocDate, 103), 9, 2) +
        RIGHT('000000000000' + LTRIM(RTRIM(CAST(E.Emisor_Numero AS BIGINT))),12) +
        C.Consecutivo +  
        @Situacion_de_Comprobante +
        RIGHT('00000000' + LTRIM(RTRIM(CAST(T0.DocNum AS BIGINT))), 8) AS Clave
    FROM ZZTEST_SBO_LARCE.dbo.ORIN T0
    CROSS JOIN Emisor E
    CROSS JOIN Consecutivo C
    WHERE T0.DocNum = @DocNum 
          AND T0.DocType = @Tipo
    
), 

CodigosImpuesto AS (
    SELECT 
        T1.DocEntry,
        T1.LineNum,
        CASE 
            WHEN LTRIM(RTRIM(T1.TaxCode)) IN ('EX','EXE','EXES') THEN '10'

            WHEN LTRIM(RTRIM(T1.TaxCode)) LIKE 'EXO%' THEN
                CASE
                    WHEN TarifaCalc.TarifaTotal = 13   THEN '08'
                    WHEN TarifaCalc.TarifaTotal = 4    THEN '04'
                    WHEN TarifaCalc.TarifaTotal = 2    THEN '03'
                    WHEN TarifaCalc.TarifaTotal = 1    THEN '06'
                    WHEN TarifaCalc.TarifaTotal = 0.5  THEN '09'
                    ELSE NULL
                END

            WHEN LTRIM(RTRIM(T1.TaxCode)) IN ('IVA 13','IV','IV1','IVACN13') THEN '08'
            WHEN LTRIM(RTRIM(T1.TaxCode)) = 'IVA 4' THEN '04'
            WHEN LTRIM(RTRIM(T1.TaxCode)) = 'IVA 2' THEN '03'
            WHEN LTRIM(RTRIM(T1.TaxCode)) = 'IVA 1' THEN '06'
            WHEN LTRIM(RTRIM(T1.TaxCode)) IN ('IVA 0.65','IV3') THEN '07'
            WHEN LTRIM(RTRIM(T1.TaxCode)) = 'ISC' THEN '10'
            ELSE NULL
        END AS DetalleServicio_ImpuestoCodigoTarifa
    FROM ZZTEST_SBO_LARCE.dbo.RIN1 T1
    OUTER APPLY (
        SELECT
            CASE
                WHEN LTRIM(RTRIM(T1.TaxCode)) LIKE 'EXO%'
                     AND TRY_CONVERT(decimal(10,5), REPLACE(SUBSTRING(LTRIM(RTRIM(T1.TaxCode)), 4, 50), ',', '.')) IS NOT NULL
                THEN TRY_CONVERT(decimal(10,5), REPLACE(SUBSTRING(LTRIM(RTRIM(T1.TaxCode)), 4, 50), ',', '.'))
                     + COALESCE(T1.VatPrcnt, 0)
                ELSE COALESCE(T1.VatPrcnt, 0)
            END AS TarifaTotal
    ) TarifaCalc
    WHERE T1.DocEntry = (
        SELECT DocEntry
        FROM ZZTEST_SBO_LARCE.dbo.ORIN
        WHERE DocNum = @DocNum
          AND DocType = @Tipo
    )
    AND ISNULL(T1.TreeType, 'N') IN ('N', 'S', 'P')
),
    ImpuestosEspecificos AS (
        SELECT 
            T1.DocEntry,
            T1.LineNum,
            0 AS MontoImpuestoEspecifico,
            ISNULL(T1.Quantity, 0) AS DetalleServicio_ImpuestoEspecifico_CantidadUnidadMedida,
            0.0 AS DetalleServicio_ImpuestoEspecifico_Porcentaje,
            0.0 AS DetalleServicio_ImpuestoEspecifico_Proporcion,
            0.0 AS DetalleServicio_ImpuestoEspecifico_VolumenUnidadConsumo,
            0.0 AS DetalleServicio_ImpuestoEspecifico_ImpuestoUnidad,
            0.0 AS DetalleServicio_ImpuestoEspecifico_UnidadMedida
        FROM ZZTEST_SBO_LARCE.dbo.RIN1 T1
        INNER JOIN ZZTEST_SBO_LARCE.dbo.OITM OITM ON T1.ItemCode = OITM.ItemCode
        WHERE T1.DocEntry = (
            SELECT DocEntry
            FROM ZZTEST_SBO_LARCE.dbo.ORIN
            WHERE DocNum = @DocNum AND DocType = @Tipo
        ) AND ISNULL(T1.TreeType, 'N') IN ('N', 'S', 'P')
    ),

  /*==============================================================
  CTE PrecioLinea
  --------------------------------------------------------------
  Objetivo:
  Centralizar el precio unitario que debe usar todo el SP.

  Regla aplicada:
  1) Si la factura está en moneda local (COL/CRC) y la línea fue digitada en USD,
     convierte el precio unitario a colones.
  2) Para convertir usa el tipo de cambio digitado en SAP:
        - Primero RIN1.Rate, porque corresponde al tipo de cambio de la línea.
        - Si RIN1.Rate viene 0 o NULL, usa ORIN.DocRate.
  3) Si la factura está en USD y la línea está en USD, NO convierte.
  4) Si la factura está en moneda local y la línea está en moneda local, NO convierte.

  Importante:
  Este CTE solo normaliza el precio unitario. Todos los cálculos posteriores
  deben usar PrecioUnitarioUsar por medio del alias PriceBefDi en DetalleServicioBase.
==============================================================*/
   
	PrecioLinea AS (
    SELECT
        T1.DocEntry,
        T1.LineNum,

        T0.DocCur     AS MonedaFactura,
        T1.Currency   AS MonedaPrecioLinea,
        T0.DocRate    AS TipoCambioFactura,
        T1.Rate       AS TipoCambioLinea,
        T1.PriceBefDi AS PrecioDigitadoSAP,

        CASE
            WHEN T0.DocCur IN ('COL', 'CRC')
                 AND UPPER(LTRIM(RTRIM(ISNULL(T1.Currency, '')))) = 'USD'
            THEN ROUND(
                    T1.PriceBefDi * COALESCE(NULLIF(T1.Rate, 0), NULLIF(T0.DocRate, 0), 1)
                 , 5)
            ELSE T1.PriceBefDi
        END AS PrecioUnitarioUsar,

        CASE
            WHEN T0.DocCur IN ('COL', 'CRC')
                 AND UPPER(LTRIM(RTRIM(ISNULL(T1.Currency, '')))) = 'USD'
            THEN 1
            ELSE 0
        END AS PrecioFueConvertido
    FROM ZZTEST_SBO_LARCE.dbo.ORIN T0
    INNER JOIN ZZTEST_SBO_LARCE.dbo.RIN1 T1
        ON T0.DocEntry = T1.DocEntry
    WHERE T0.DocNum = @DocNum
      AND T0.DocType = @Tipo
      AND ISNULL(T1.TreeType, 'N') IN ('N', 'S', 'P')
), 
 DetalleServicioBase AS (
    SELECT 
        T1.DocEntry,
        T1.LineNum AS DetalleServicio_NumeroLinea,
        T1.ItemCode,
        T1.Dscription,
        T1.Quantity,
        -- Precio unitario que debe usar todo el SP.
        -- Si la factura es local y la línea está en USD, ya viene convertido a colones.
        PL.PrecioUnitarioUsar AS PriceBefDi,
        T1.DiscPrcnt,
        T1.TaxOnly,
        T1.VatPrcnt,
        T1.U_LDT_TipoDesc,
        T1.U_LDT_NatuDesc,
        T4.U_LDT_CABYS            AS U_CodigoCabys, 
        T4.[U_LDT_RegMed]         AS U_RegistroMedicamento,
        T4.[U_LDT_ForFam]         AS U_FormaFarmaceutica,
        T4.U_LDT_ActEconom        AS U_CodigoActividadEconomica,
        T4.U_LDT_WMCodigoProducto AS U_TipoCodigo,
        T4.U_Part_Arancel         AS DetalleServicio_PartidaArancelaria,        
        T1.U_LDT_TipoTrans,
        CASE 
            WHEN DF.EsMismoContribuyente = 1 AND T1.TaxOnly = 'Y' THEN 1
            ELSE 0
        END AS EsAutoconsumo,
        ROUND(T1.Quantity * PL.PrecioUnitarioUsar,5) AS DetalleServicio_MontoTotal,

        CASE 
            --WHEN LTRIM(RTRIM(ISNULL(T1.TaxCode,''))) LIKE 'EXO%' THEN 0.0
            WHEN DF.EsMismoContribuyente = 1 AND T1.TaxOnly = 'Y' THEN 0.0
            WHEN T1.TaxOnly = 'Y' THEN ROUND(T1.Quantity * PL.PrecioUnitarioUsar,5)
            WHEN T1.DiscPrcnt > 0 THEN ROUND(ROUND(T1.Quantity * PL.PrecioUnitarioUsar,5) * T1.DiscPrcnt / 100.0,5)
            ELSE 0.0 
        END AS DetalleServicio_MontoDescuento

    FROM ZZTEST_SBO_LARCE.dbo.RIN1 T1
    INNER JOIN PrecioLinea PL
        ON T1.DocEntry = PL.DocEntry
       AND T1.LineNum = PL.LineNum
    INNER JOIN ZZTEST_SBO_LARCE.dbo.OITM T4 ON T1.ItemCode = T4.ItemCode 
    CROSS JOIN DocumentoFiscal DF
    WHERE T1.DocEntry = (
        SELECT DocEntry
        FROM ZZTEST_SBO_LARCE.dbo.ORIN
        WHERE DocNum = @DocNum AND DocType = @Tipo
    )
    AND ISNULL(T1.TreeType, 'N') IN ('N', 'S', 'P')
),
    DetalleServicio AS (
        SELECT 
            DB.DocEntry,
            DB.DetalleServicio_NumeroLinea,
			-- CASE 
			--WHEN TF.CodigoTipoDocumento = '09'
			--	 AND LEFT(ISNULL(DB.U_CodigoCabys, ''), 1) IN ('0','1','2','3','4')
			--	 AND LEN(LTRIM(RTRIM(ISNULL(DB.DetalleServicio_PartidaArancelaria,''))))<> ''
			--THEN LTRIM(RTRIM(DB.DetalleServicio_PartidaArancelaria))
			--ELSE '0'
		 --   END AS DetalleServicio_PartidaArancelaria,
		   CASE 
				WHEN (
						TF.CodigoTipoDocumento = '09'
						OR IR.Referencia_TipoDoc = '09'
					 )
					 AND LEFT(ISNULL(DB.U_CodigoCabys, ''), 1) IN ('0','1','2','3','4')
					 AND LEN(LTRIM(RTRIM(ISNULL(DB.DetalleServicio_PartidaArancelaria, '')))) <> 0
				THEN LTRIM(RTRIM(DB.DetalleServicio_PartidaArancelaria))
				ELSE '0'
			END AS DetalleServicio_PartidaArancelaria,
            RIGHT('00' + CAST(DB.U_TipoCodigo AS VARCHAR(2)), 2) AS DetalleServicio_TipoCodigo,
            ISNULL(DB.U_CodigoCabys, '0') AS DetalleServicio_CodigoProductoServicio,
            ISNULL(DB.U_RegistroMedicamento, '0') AS DetalleServicio_RegistroMedicamento,
            ISNULL(DB.U_FormaFarmaceutica, '0') AS DetalleServicio_FormaFarmaceutica,
            DB.ItemCode AS DetalleServicio_Codigo,
            DB.Quantity AS DetalleServicio_Cantidad,
            CASE 
                WHEN LEFT(ISNULL(DB.U_CodigoCabys,''),1) BETWEEN '5' AND '9' THEN 'Os'
                ELSE 'Unid'
            END AS DetalleServicio_UnidadMedida,
            CASE 
                WHEN LEFT(ISNULL(DB.U_CodigoCabys,''),1) BETWEEN '5' AND '9' THEN 'Os'
                ELSE 'Unid'
            END AS DetalleServicio_UnidadMedidaComercial,
            DB.Dscription AS DetalleServicio_Detalle,

           	CASE
			WHEN NULLIF(LTRIM(RTRIM(ISNULL(DB.U_LDT_TipoTrans, ''))), '') IS NOT NULL
				 AND RIGHT('00' + LTRIM(RTRIM(DB.U_LDT_TipoTrans)), 2) IN
					 ('01','02','03','04','05','06','07','08','09','10','11','12','13')
			THEN RIGHT('00' + LTRIM(RTRIM(DB.U_LDT_TipoTrans)), 2)

			WHEN DB.EsAutoconsumo = 1
				 AND LEFT(ISNULL(DB.U_CodigoCabys,''),1) BETWEEN '0' AND '4' THEN '03'

			WHEN DB.EsAutoconsumo = 1
				 AND LEFT(ISNULL(DB.U_CodigoCabys,''),1) BETWEEN '5' AND '9' THEN '05'

			ELSE '01'
		    END AS DetalleServicio_TipoTransaccion,

            '' AS DetalleServicio_NumeroVINoSerie,
            DB.PriceBefDi AS DetalleServicio_PrecioUnitario,
            DB.DetalleServicio_MontoTotal,
            DB.DetalleServicio_MontoDescuento,
            DB.PriceBefDi * DB.Quantity AS DetalleServicio_MontoGravadoBruto,
            DB.U_CodigoActividadEconomica AS DetalleServicio_CodigoActividadEconomica,
 					
			CASE 
				WHEN DB.EsAutoconsumo = 1 THEN ''
				WHEN ROUND(ISNULL(DB.DetalleServicio_MontoDescuento,0),5) <= 0 THEN ''
				WHEN NULLIF(LTRIM(RTRIM(ISNULL(DB.U_LDT_NatuDesc,''))), '') IS NOT NULL
					 THEN LTRIM(RTRIM(DB.U_LDT_NatuDesc))
				WHEN DB.TaxOnly = 'Y' OR DB.DiscPrcnt >= 100 THEN 'Bonificacion'
				ELSE 'Comercial'
		    END AS DetalleServicio_NaturalezaDescuento,

			CASE 
				WHEN DB.EsAutoconsumo = 1 THEN ''
				WHEN ROUND(ISNULL(DB.DetalleServicio_MontoDescuento,0),5) <= 0 THEN ''
				WHEN NULLIF(LTRIM(RTRIM(ISNULL(DB.U_LDT_TipoDesc,''))), '') IS NOT NULL
					 THEN RIGHT('00' + LTRIM(RTRIM(DB.U_LDT_TipoDesc)), 2)
				WHEN DB.TaxOnly = 'Y' OR DB.DiscPrcnt >= 100 THEN '02'
				ELSE '07'
			END AS DetalleServicio_CodigoDescuento,

            ROUND(DB.DetalleServicio_MontoTotal - DB.DetalleServicio_MontoDescuento,5) AS DetalleServicio_SubTotal,
            
			'0' AS DetalleServicio_IVACobradoFabrica,
            '0' AS DetalleServicio_ImpuestoAsumidoEmisorFabrica,

            CASE 
				WHEN DB.EsAutoconsumo = 1
					 THEN ROUND(DB.DetalleServicio_MontoTotal * (1 + DB.VatPrcnt / 100.0), 5)
				WHEN DB.TaxOnly = 'Y' OR DB.DiscPrcnt >= 100
					 THEN ROUND(DB.DetalleServicio_MontoTotal * DB.VatPrcnt / 100.0, 5)
				ELSE ROUND(ROUND(DB.DetalleServicio_MontoTotal - DB.DetalleServicio_MontoDescuento,5) * (1 + DB.VatPrcnt / 100.0), 5)
		    END AS DetalleServicio_MontoTotalLinea,

            DB.TaxOnly,
		    DB.EsAutoconsumo
        FROM DetalleServicioBase DB
		 INNER JOIN TipoFactura TF 
        ON DB.DocEntry = TF.DocEntry		
		 LEFT JOIN InfoReferencia IR 
        ON IR.DocEntry = DB.DocEntry
    ),

  ImpuestoBase AS (
    SELECT 
        T1.DocEntry,
        T1.LineNum,
        CI.DetalleServicio_ImpuestoCodigoTarifa,
        RIGHT('00' + ISNULL(T2.U_LDT_WMCodigoProducto, ''), 2) AS DetalleServicio_ImpuestoCodigo,
        '0' AS DetalleServicio_ImpuestoCodigoImpuestoOTRO,
        TarifaImp.TarifaIVAReal AS DetalleServicio_ImpuestoTarifa,
        CASE
            WHEN DS.EsAutoconsumo = 1 THEN ROUND(DS.DetalleServicio_SubTotal,5)
            WHEN CI.DetalleServicio_ImpuestoCodigoTarifa IN ('02','04','05')
                 THEN CASE WHEN T1.TaxOnly='Y' THEN ROUND(DS.DetalleServicio_MontoTotal,5)
                           ELSE ROUND(DS.DetalleServicio_SubTotal,5)
                      END
            ELSE ROUND(DS.DetalleServicio_SubTotal,5)
        END AS DetalleServicio_BaseImponible,

        CASE
            WHEN DS.EsAutoconsumo = 1
                 AND COALESCE(TarifaImp.TarifaIVAReal,0) > 0
            THEN ROUND(DS.DetalleServicio_SubTotal * TarifaImp.TarifaIVAReal / 100.0, 5)

            WHEN (ROUND(DS.DetalleServicio_SubTotal,5) = 0
                  AND ROUND(DS.DetalleServicio_MontoDescuento,5) = ROUND(DS.DetalleServicio_MontoTotal,5)
                  AND COALESCE(TarifaImp.TarifaIVAReal,0) > 0)
            THEN ROUND(DS.DetalleServicio_MontoTotal * TarifaImp.TarifaIVAReal / 100.0, 5)

            WHEN ROUND(DS.DetalleServicio_SubTotal,5) > 0
                 AND COALESCE(TarifaImp.TarifaIVAReal,0) > 0
            THEN ROUND(DS.DetalleServicio_SubTotal * TarifaImp.TarifaIVAReal / 100.0, 5)

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
        T1.DiscPrcnt AS DetalleServicio_PorcentajeDescuento,
        DS.TaxOnly
    FROM ZZTEST_SBO_LARCE.dbo.RIN1 T1
    LEFT JOIN ZZTEST_SBO_LARCE.dbo.OITM T2 ON T1.ItemCode = T2.ItemCode
    INNER JOIN DetalleServicio DS ON T1.DocEntry = DS.DocEntry AND T1.LineNum = DS.DetalleServicio_NumeroLinea
    LEFT JOIN CodigosImpuesto CI ON T1.DocEntry = CI.DocEntry AND T1.LineNum = CI.LineNum
    LEFT JOIN ImpuestosEspecificos IE ON T1.DocEntry = IE.DocEntry AND T1.LineNum = IE.LineNum
    OUTER APPLY (
        SELECT
            CASE
                WHEN LTRIM(RTRIM(T1.TaxCode)) LIKE 'EXO%'
                     AND TRY_CONVERT(decimal(10,5), REPLACE(SUBSTRING(LTRIM(RTRIM(T1.TaxCode)), 4, 50), ',', '.')) IS NOT NULL
                THEN TRY_CONVERT(decimal(10,5), REPLACE(SUBSTRING(LTRIM(RTRIM(T1.TaxCode)), 4, 50), ',', '.'))
                     + COALESCE(T1.VatPrcnt, 0)
                WHEN COALESCE(T1.VatPrcnt,0) > 0 THEN CAST(T1.VatPrcnt AS DECIMAL(18,5))
                WHEN LTRIM(RTRIM(T1.TaxCode)) = 'EXO13' THEN CAST(13 AS DECIMAL(18,5))
                WHEN LTRIM(RTRIM(T1.TaxCode)) = 'EXO4'  THEN CAST(4  AS DECIMAL(18,5))
                WHEN LTRIM(RTRIM(T1.TaxCode)) = 'EXO2'  THEN CAST(2  AS DECIMAL(18,5))
                WHEN LTRIM(RTRIM(T1.TaxCode)) = 'EXO1'  THEN CAST(1  AS DECIMAL(18,5))
                WHEN LTRIM(RTRIM(T1.TaxCode)) = 'EXO0.5' THEN CAST(0.5 AS DECIMAL(18,5))
                ELSE 0
            END AS TarifaIVAReal
    ) TarifaImp
    WHERE T1.DocEntry = (
        SELECT DocEntry
        FROM ZZTEST_SBO_LARCE.dbo.ORIN
        WHERE DocNum = @DocNum
          AND DocType = @Tipo
    )
    AND ISNULL(T1.TreeType, 'N') IN ('N', 'S', 'P')
),
 Exoneracion AS (
    SELECT
        T1.DocEntry,
        T1.LineNum,
        CASE WHEN TF.CodigoTipoDocumento = '09' THEN NULL
             ELSE NULLIF(RIGHT('00' + LTRIM(RTRIM(ISNULL(T1.U_LDT_TipoExo,''))), 2), '00')
        END AS Exoneracion_TipoDocumento,

        CASE WHEN TF.CodigoTipoDocumento = '09' THEN NULL
             ELSE NULLIF(LTRIM(RTRIM(ISNULL(T1.U_LDT_NumExo,''))), '')
        END AS Exoneracion_NumeroDocumento,

        CASE WHEN TF.CodigoTipoDocumento = '09' THEN NULL
             ELSE NULLIF(LTRIM(RTRIM(ISNULL(T1.U_LDT_CodInstExon,''))), '')
        END AS Exoneracion_NombreInstitucion,

        CASE WHEN TF.CodigoTipoDocumento = '09' THEN NULL
             ELSE NULLIF(LTRIM(RTRIM(ISNULL(T1.U_LDT_NomInstitucionExo,''))), '')
        END AS Exoneracion_NombreInstitucionOtros,

        CASE WHEN TF.CodigoTipoDocumento = '09' THEN NULL
             ELSE CONVERT(VARCHAR(19), TRY_CONVERT(DATETIME, T1.U_LDT_FechaExo), 126)
        END AS Exoneracion_FechaEmision,

        CASE WHEN TF.CodigoTipoDocumento = '09' THEN NULL
             ELSE ROUND(COALESCE(ExoCalc.PorcExo,0), 5)
        END AS Exoneracion_PorcentajeCompra,

        CASE WHEN TF.CodigoTipoDocumento = '09' THEN NULL
             ELSE ExoCalc.PorcExo
        END AS Exoneracion_IvaExonerado,

        CASE WHEN TF.CodigoTipoDocumento = '09' THEN NULL
             ELSE CASE
                    WHEN RIGHT('00' + LTRIM(RTRIM(ISNULL(T1.U_LDT_TipoExo,''))), 2) IN ('02','03','06','07','08')
                    THEN NULLIF(LTRIM(RTRIM(ISNULL(T1.U_LDT_ArticExon,''))), '')
                    ELSE NULL
                  END
        END AS Exoneracion_Articulo,

        CASE WHEN TF.CodigoTipoDocumento = '09' THEN NULL
             ELSE CASE
                    WHEN RIGHT('00' + LTRIM(RTRIM(ISNULL(T1.U_LDT_TipoExo,''))), 2) IN ('02','03','06','07','08')
                    THEN NULLIF(LTRIM(RTRIM(ISNULL(T1.U_LDT_IncisoExon,''))), '')
                    ELSE NULL
                  END
        END AS Exoneracion_Inciso,

        CASE WHEN TF.CodigoTipoDocumento = '09' THEN NULL
             ELSE CASE
                    WHEN NULLIF(LTRIM(RTRIM(ISNULL(T1.U_LDT_TipoExo,''))), '') IS NOT NULL
                         AND COALESCE(IB.DetalleServicio_ImpuestoMonto,0) > 0
                    THEN ROUND(
                           COALESCE(IB.DetalleServicio_ImpuestoMonto,0) *
                           CASE
                               WHEN COALESCE(ExoCalc.PorcExo,0) <= 0 THEN 0
                               WHEN COALESCE(TarifaCalc.TarifaEfectivaIVA,0) <= 0 THEN 0
                               WHEN ExoCalc.PorcExo >= TarifaCalc.TarifaEfectivaIVA THEN 1
                               ELSE ExoCalc.PorcExo / TarifaCalc.TarifaEfectivaIVA
                           END
                         ,5)
                    ELSE 0.0
                  END
        END AS Exoneracion_MontoImpuesto,

        CASE WHEN TF.CodigoTipoDocumento = '09' THEN NULL
             ELSE CASE
                    WHEN NULLIF(LTRIM(RTRIM(ISNULL(T1.U_LDT_TipoExo,''))), '') IS NOT NULL
                    THEN ROUND(
                        CASE
                            WHEN COALESCE(IB.DetalleServicio_ImpuestoMonto,0) -
                                 COALESCE(ROUND(
                                     COALESCE(IB.DetalleServicio_ImpuestoMonto,0) *
                                     CASE
                                         WHEN COALESCE(ExoCalc.PorcExo,0) <= 0 THEN 0
                                         WHEN COALESCE(TarifaCalc.TarifaEfectivaIVA,0) <= 0 THEN 0
                                         WHEN ExoCalc.PorcExo >= TarifaCalc.TarifaEfectivaIVA THEN 1
                                         ELSE ExoCalc.PorcExo / TarifaCalc.TarifaEfectivaIVA
                                     END
                                 ,5),0) < 0
                            THEN 0
                            ELSE COALESCE(IB.DetalleServicio_ImpuestoMonto,0) -
                                 COALESCE(ROUND(
                                     COALESCE(IB.DetalleServicio_ImpuestoMonto,0) *
                                     CASE
                                         WHEN COALESCE(ExoCalc.PorcExo,0) <= 0 THEN 0
                                         WHEN COALESCE(TarifaCalc.TarifaEfectivaIVA,0) <= 0 THEN 0
                                         WHEN ExoCalc.PorcExo >= TarifaCalc.TarifaEfectivaIVA THEN 1
                                         ELSE ExoCalc.PorcExo / TarifaCalc.TarifaEfectivaIVA
                                     END
                                 ,5),0)
                        END
                    ,5)
                    ELSE COALESCE(IB.DetalleServicio_ImpuestoMonto,0)
                  END
        END AS Exoneracion_ImpuestoNeto,

        CASE WHEN TF.CodigoTipoDocumento = '09' THEN NULL
             ELSE ISNULL(
                    ROUND(
                        DS.DetalleServicio_MontoTotal *
                        CASE
                            WHEN COALESCE(ExoCalc.PorcExo,0) <= 0 THEN 0
                            WHEN COALESCE(TarifaCalc.TarifaEfectivaIVA,0) <= 0 THEN 0
                            WHEN ExoCalc.PorcExo >= TarifaCalc.TarifaEfectivaIVA THEN 1
                            ELSE ExoCalc.PorcExo / TarifaCalc.TarifaEfectivaIVA
                        END
                    ,5)
                  ,0)
        END AS Exoneracion_TotalMercExonerada
    FROM ZZTEST_SBO_LARCE.dbo.RIN1 T1
    INNER JOIN ZZTEST_SBO_LARCE.dbo.ORIN H  ON H.DocEntry = T1.DocEntry
    INNER JOIN ZZTEST_SBO_LARCE.dbo.OCRD T2 ON H.CardCode = T2.CardCode
    INNER JOIN DetalleServicio DS ON T1.DocEntry = DS.DocEntry AND T1.LineNum = DS.DetalleServicio_NumeroLinea
    INNER JOIN ImpuestoBase IB ON T1.DocEntry = IB.DocEntry AND T1.LineNum = IB.LineNum
    INNER JOIN TipoFactura TF ON T1.DocEntry = TF.DocEntry
    --OUTER APPLY (
    --    SELECT TOP 1
    --        CASE
    --            WHEN LTRIM(RTRIM(T1.TaxCode)) IN ('EX','EXE','EXES') THEN 0
    --            WHEN LTRIM(RTRIM(T1.TaxCode)) LIKE 'EXO%'
    --                 AND TRY_CONVERT(decimal(10,5), REPLACE(SUBSTRING(LTRIM(RTRIM(T1.TaxCode)), 4, 50), ',', '.')) IS NOT NULL
    --            THEN TRY_CONVERT(decimal(10,5), REPLACE(SUBSTRING(LTRIM(RTRIM(T1.TaxCode)), 4, 50), ',', '.'))
    --            ELSE 0
    --        END AS PorcExo
    --    FROM ZZTEST_SBO_LARCE.dbo.CRD1 C1
    --    WHERE C1.CardCode = H.CardCode
    --      AND ((H.PayToCode IS NOT NULL AND H.PayToCode <> '' AND C1.Address = H.PayToCode)
    --           OR (H.PayToCode IS NULL OR H.PayToCode = ''))
    --    ORDER BY CASE WHEN C1.Address = H.PayToCode THEN 0 ELSE 1 END, C1.LineNum
    --) ExoCalc
	CROSS APPLY (
    SELECT
        CASE 
            WHEN LTRIM(RTRIM(T1.TaxCode)) IN ('EX','EXE','EXES') THEN 0
            WHEN LTRIM(RTRIM(T1.TaxCode)) LIKE 'EXO%' 
                 AND TRY_CONVERT(decimal(10,5), REPLACE(SUBSTRING(LTRIM(RTRIM(T1.TaxCode)), 4, 50), ',', '.')) IS NOT NULL
            THEN TRY_CONVERT(decimal(10,5), REPLACE(SUBSTRING(LTRIM(RTRIM(T1.TaxCode)), 4, 50), ',', '.'))
            ELSE 0
        END AS PorcExo
) ExoCalc
    OUTER APPLY (
        SELECT
            CASE
                WHEN LTRIM(RTRIM(T1.TaxCode)) LIKE 'EXO%'
                     AND TRY_CONVERT(decimal(10,5), REPLACE(SUBSTRING(LTRIM(RTRIM(T1.TaxCode)), 4, 50), ',', '.')) IS NOT NULL
                THEN TRY_CONVERT(decimal(10,5), REPLACE(SUBSTRING(LTRIM(RTRIM(T1.TaxCode)), 4, 50), ',', '.'))
                     + COALESCE(T1.VatPrcnt, 0)

                WHEN COALESCE(T1.VatPrcnt,0) > 0 THEN CAST(T1.VatPrcnt AS DECIMAL(18,5))
                WHEN COALESCE(ExoCalc.PorcExo,0) > 0
                     AND COALESCE(IB.DetalleServicio_ImpuestoMonto,0) = 0
                THEN CAST(ExoCalc.PorcExo AS DECIMAL(18,5))
                WHEN COALESCE(IB.DetalleServicio_ImpuestoMonto,0) > 0
                     AND COALESCE(DS.DetalleServicio_SubTotal,0) > 0
                THEN ROUND((IB.DetalleServicio_ImpuestoMonto / DS.DetalleServicio_SubTotal) * 100.0, 5)
                ELSE 0
            END AS TarifaEfectivaIVA
    ) TarifaCalc
    WHERE T1.DocEntry = (
        SELECT DocEntry
        FROM ZZTEST_SBO_LARCE.dbo.ORIN
        WHERE DocNum = @DocNum
          AND DocType = @Tipo
    )
    AND ISNULL(T1.TreeType, 'N') IN ('N', 'S', 'P')
),
    Impuesto AS (
        SELECT 
            IB.DocEntry,
            IB.LineNum,
            IB.DetalleServicio_ImpuestoCodigo,
            IB.DetalleServicio_ImpuestoCodigoImpuestoOTRO,
           	IB.DetalleServicio_ImpuestoCodigoTarifa   AS DetalleServicio_ImpuestoCodigoTarifa, 
		    IB.DetalleServicio_ImpuestoTarifa AS DetalleServicio_ImpuestoTarifa,
            '0' AS DetalleServicio_ImpuestoFactorIVA,
            IB.MontoImpuestoEspecifico,
            IB.DetalleServicio_ImpuestoEspecifico_CantidadUnidadMedida,
            IB.DetalleServicio_ImpuestoEspecifico_Porcentaje,
            IB.DetalleServicio_ImpuestoEspecifico_Proporcion,
            IB.DetalleServicio_ImpuestoEspecifico_VolumenUnidadConsumo,
            IB.DetalleServicio_ImpuestoEspecifico_ImpuestoUnidad,
            IB.DetalleServicio_IVACobradoFabrica,
            IB.DetalleServicio_ImpuestoAsumidoEmisorFabrica,
            IB.DetalleServicio_BaseImponible,
            IB.DetalleServicio_ImpuestoMonto,
            ROUND(
                CASE
                    WHEN COALESCE(IB.DetalleServicio_ImpuestoMonto, 0) - COALESCE(Ex.Exoneracion_MontoImpuesto, 0) < 0 THEN 0
                    ELSE COALESCE(IB.DetalleServicio_ImpuestoMonto, 0) - COALESCE(Ex.Exoneracion_MontoImpuesto, 0)
                END
            , 5) AS DetalleServicio_ImpuestoNeto
        FROM ImpuestoBase IB
        LEFT JOIN Exoneracion Ex ON IB.DocEntry = Ex.DocEntry AND IB.LineNum = Ex.LineNum
        INNER JOIN TipoFactura TF ON IB.DocEntry = TF.DocEntry 
	),

    TotalesBaseResumen AS (
        SELECT
            SUM(CASE
                WHEN Cabys.TipoCabys = 'S'
                     AND Impuesto.DetalleServicio_ImpuestoTarifa > 0
                     AND ISNULL(Impuesto.DetalleServicio_ImpuestoCodigoTarifa,'') NOT IN ('10')
                THEN ROUND(
                       DS.DetalleServicio_MontoTotal *
                       (1 - COALESCE(
                              CASE
                                WHEN NULLIF(Impuesto.DetalleServicio_ImpuestoMonto,0) IS NULL THEN 0
                                ELSE Ex.Exoneracion_MontoImpuesto / Impuesto.DetalleServicio_ImpuestoMonto
                              END
                            ,0))
                     ,5)
                ELSE 0
            END) AS ResumenFactura_TotalServGravados,

            SUM(CASE
                WHEN Cabys.TipoCabys = 'S'
                     AND (
                          Impuesto.DetalleServicio_ImpuestoCodigoTarifa = '10'
                          OR T1.TaxCode IN ('EXE','INS')
                     )
                THEN DS.DetalleServicio_MontoTotal
                ELSE 0
            END) AS ResumenFactura_TotalServExentos,

            --SUM(CASE
            --    WHEN Cabys.TipoCabys = 'S'
            --         AND COALESCE(Ex.Exoneracion_PorcentajeCompra,0) > 0
            --    THEN ROUND(
            --           DS.DetalleServicio_MontoTotal
            --           * (COALESCE(Ex.Exoneracion_PorcentajeCompra,0) / 100.0)
            --         ,5)
            --    ELSE 0
            --END) AS ResumenFactura_TotalServExonerado,
					SUM(CASE
			WHEN Cabys.TipoCabys = 'S'
				 AND COALESCE(Ex.Exoneracion_MontoImpuesto,0) > 0
			THEN ROUND(
				   DS.DetalleServicio_MontoTotal *
				   COALESCE(
					   CASE
						   WHEN NULLIF(Impuesto.DetalleServicio_ImpuestoMonto,0) IS NULL THEN 0
						   ELSE Ex.Exoneracion_MontoImpuesto / Impuesto.DetalleServicio_ImpuestoMonto
					   END
				   ,0)
				 ,5)
			ELSE 0
		END) AS ResumenFactura_TotalServExonerado,

            SUM(CASE
                WHEN Cabys.TipoCabys = 'S'
                     AND DS.DetalleServicio_MontoTotal > 0
                     AND T1.TaxOnly <> 'Y'
                     AND (ISNULL(CASE WHEN T1.TaxCode = 'NS' THEN '07' ELSE '' END, '') = '07')
                THEN DS.DetalleServicio_MontoTotal
                ELSE 0
            END) AS ResumenFactura_TotalServNoSujeto,

            SUM(CASE
                WHEN Cabys.TipoCabys = 'M'
                     AND Impuesto.DetalleServicio_ImpuestoTarifa > 0
                     AND ISNULL(Impuesto.DetalleServicio_ImpuestoCodigoTarifa,'') NOT IN ('10')
                THEN ROUND(
                       DS.DetalleServicio_MontoTotal *
                       (1 - COALESCE(
                              CASE
                                WHEN NULLIF(Impuesto.DetalleServicio_ImpuestoMonto,0) IS NULL THEN 0
                                ELSE Ex.Exoneracion_MontoImpuesto / Impuesto.DetalleServicio_ImpuestoMonto
                              END
                            ,0))
                     ,5)
                ELSE 0
            END) AS ResumenFactura_TotalMercanciasGravadas,

            SUM(CASE
                WHEN Cabys.TipoCabys = 'M'
                     AND (
                            Impuesto.DetalleServicio_ImpuestoCodigoTarifa = '10'
                          OR T1.TaxCode IN ('EXE','INS')
                     )
                THEN DS.DetalleServicio_MontoTotal
                ELSE 0
            END) AS ResumenFactura_TotalMercanciasExentas,

            --SUM(CASE
            --    WHEN Cabys.TipoCabys = 'M'
            --         AND COALESCE(Ex.Exoneracion_PorcentajeCompra,0) > 0
            --    THEN ROUND(
            --           DS.DetalleServicio_MontoTotal
            --           * (COALESCE(Ex.Exoneracion_PorcentajeCompra,0) / 100.0)
            --         ,5)
            --    ELSE 0
            --END) AS ResumenFactura_TotalMercanciasExonerada,
				SUM(CASE
			WHEN Cabys.TipoCabys = 'M'
				 AND COALESCE(Ex.Exoneracion_MontoImpuesto,0) > 0
			THEN ROUND(
				   DS.DetalleServicio_MontoTotal *
				   COALESCE(
					   CASE
						   WHEN NULLIF(Impuesto.DetalleServicio_ImpuestoMonto,0) IS NULL THEN 0
						   ELSE Ex.Exoneracion_MontoImpuesto / Impuesto.DetalleServicio_ImpuestoMonto
					   END
				   ,0)
				 ,5)
			ELSE 0
		END) AS ResumenFactura_TotalMercanciasExonerada,

            SUM(CASE
                WHEN Cabys.TipoCabys = 'M'
                     AND DS.DetalleServicio_MontoTotal > 0
                     AND T1.TaxOnly <> 'Y'
                     AND ISNULL(CASE WHEN T1.TaxCode = 'NS' THEN '07' ELSE '' END, '') = '07'
                THEN DS.DetalleServicio_MontoTotal
                ELSE 0
            END) AS ResumenFactura_TotalMercanciasNoSujeto

        FROM ZZTEST_SBO_LARCE.dbo.ORIN T0
        INNER JOIN ZZTEST_SBO_LARCE.dbo.RIN1 T1 ON T0.DocEntry = T1.DocEntry
        INNER JOIN ZZTEST_SBO_LARCE.dbo.OITM T4 ON T1.ItemCode = T4.ItemCode
        INNER JOIN ZZTEST_SBO_LARCE.dbo.OITB OITB ON T4.ItmsGrpCod = OITB.ItmsGrpCod
        INNER JOIN DetalleServicio DS ON T1.DocEntry = DS.DocEntry AND T1.LineNum = DS.DetalleServicio_NumeroLinea
        CROSS APPLY (
            SELECT 
			  CASE
                WHEN LEFT(ISNULL(DS.DetalleServicio_CodigoProductoServicio,''),1) BETWEEN '0' AND '4' THEN 'M'
                WHEN LEFT(ISNULL(DS.DetalleServicio_CodigoProductoServicio,''),1) BETWEEN '5' AND '9' THEN 'S'
                ELSE ''
            END AS TipoCabys
        ) Cabys
        LEFT JOIN Exoneracion Ex ON T1.DocEntry = Ex.DocEntry AND T1.LineNum = Ex.LineNum
        LEFT JOIN Impuesto Impuesto ON Impuesto.DocEntry = T1.DocEntry AND Impuesto.LineNum = T1.LineNum
        WHERE T1.DocEntry = (
            SELECT DocEntry
            FROM ZZTEST_SBO_LARCE.dbo.ORIN
            WHERE DocNum = @DocNum AND DocType = @Tipo
        )AND ISNULL(T1.TreeType, 'N') IN ('N', 'S', 'P')
    )
	,ResumenFactura AS (
        SELECT
            CASE WHEN MAX(T0.DocCur) = 'COL' THEN 'CRC' ELSE MAX(T0.DocCur) END AS ResumenFactura_CodigoMoneda,
            MAX(T0.DocRate) AS ResumenFactura_TipoCambio,
            MAX(TB.ResumenFactura_TotalServGravados)       AS ResumenFactura_TotalServGravados,
            MAX(TB.ResumenFactura_TotalServExentos)        AS ResumenFactura_TotalServExentos,
            MAX(TB.ResumenFactura_TotalServExonerado)      AS ResumenFactura_TotalServExonerado,
            MAX(TB.ResumenFactura_TotalServNoSujeto)       AS ResumenFactura_TotalServNoSujeto,
            MAX(TB.ResumenFactura_TotalMercanciasGravadas) AS ResumenFactura_TotalMercanciasGravadas,
            MAX(TB.ResumenFactura_TotalMercanciasExentas)  AS ResumenFactura_TotalMercanciasExentas,
            MAX(TB.ResumenFactura_TotalMercanciasExonerada) AS ResumenFactura_TotalMercanciasExonerada,
            MAX(TB.ResumenFactura_TotalMercanciasNoSujeto) AS ResumenFactura_TotalMercanciasNoSujeto,

            ROUND(MAX(TB.ResumenFactura_TotalServGravados) + MAX(TB.ResumenFactura_TotalMercanciasGravadas), 5) AS ResumenFactura_TotalGravado,
            ROUND(MAX(TB.ResumenFactura_TotalServExentos) + MAX(TB.ResumenFactura_TotalMercanciasExentas), 5) AS ResumenFactura_TotalExento,
           
		    CASE WHEN MAX(TF.CodigoTipoDocumento) = '09' THEN 0
			ELSE 
				ROUND(MAX(TB.ResumenFactura_TotalServExonerado)  + MAX(TB.ResumenFactura_TotalMercanciasExonerada), 5)
			END 
			AS ResumenFactura_TotalExonerado,

			ROUND(MAX(TB.ResumenFactura_TotalServNoSujeto)   + MAX(TB.ResumenFactura_TotalMercanciasNoSujeto), 5) AS ResumenFactura_TotalNoSujeto,
			           
		    ROUND(
                (MAX(TB.ResumenFactura_TotalServGravados) + MAX(TB.ResumenFactura_TotalMercanciasGravadas)) +
                (MAX(TB.ResumenFactura_TotalServExentos) + MAX(TB.ResumenFactura_TotalMercanciasExentas)) +
                (MAX(TB.ResumenFactura_TotalServExonerado) + MAX(TB.ResumenFactura_TotalMercanciasExonerada)) +
                (MAX(TB.ResumenFactura_TotalServNoSujeto) + MAX(TB.ResumenFactura_TotalMercanciasNoSujeto))
            ,5) AS ResumenFactura_TotalVenta,

            SUM(ISNULL(DS.DetalleServicio_MontoDescuento, 0)) AS ResumenFactura_TotalDescuentos,
            
			ROUND(
                 (
                    MAX(TB.ResumenFactura_TotalServGravados) + MAX(TB.ResumenFactura_TotalMercanciasGravadas) +
                    MAX(TB.ResumenFactura_TotalServExentos) + MAX(TB.ResumenFactura_TotalMercanciasExentas) +
                    MAX(TB.ResumenFactura_TotalServExonerado) + MAX(TB.ResumenFactura_TotalMercanciasExonerada) +
                    MAX(TB.ResumenFactura_TotalServNoSujeto) + MAX(TB.ResumenFactura_TotalMercanciasNoSujeto)
                ) 
				- 
				SUM(ISNULL(DS.DetalleServicio_MontoDescuento, 0))
            ,5) AS ResumenFactura_TotalVentaNeta,

            SUM(
                COALESCE(
                    CASE 
                        WHEN Ex.Exoneracion_TipoDocumento IS NOT NULL 
                          OR DS.DetalleServicio_IVACobradoFabrica IN ('01','02')
                        THEN Impuesto.DetalleServicio_ImpuestoNeto
                        ELSE Impuesto.DetalleServicio_ImpuestoMonto
                    END
                , 0)
            ) AS ResumenFactura_TotalImpuesto,

            CAST(0 AS decimal(18,5)) AS ResumenFactura_TotalIVADevuelto,
            CAST(0 AS decimal(18,5)) AS ResumenFactura_TotalOtrosCargos,
            ''                       AS OtroContenido,
            CAST(0 AS decimal(18,5)) AS OtroCargo_Monto,
            ''                       AS OtroCargo_Detalle,
            ''                       AS OtroCargo_TipoDocumento,
            CAST(0 AS decimal(18,5)) AS OtroCargo_Porcentaje 

        FROM TotalesBaseResumen TB
        CROSS JOIN ZZTEST_SBO_LARCE.dbo.ORIN T0
        INNER JOIN ZZTEST_SBO_LARCE.dbo.RIN1 T1 ON T0.DocEntry = T1.DocEntry
        INNER JOIN DetalleServicio DS ON T1.DocEntry = DS.DocEntry AND T1.LineNum = DS.DetalleServicio_NumeroLinea
        LEFT JOIN Exoneracion Ex ON T1.DocEntry = Ex.DocEntry AND T1.LineNum = Ex.LineNum
        LEFT JOIN Impuesto Impuesto ON T1.DocEntry = Impuesto.DocEntry AND T1.LineNum = Impuesto.LineNum
        LEFT JOIN TipoFactura TF ON TF.DocEntry = T0.DocEntry
		WHERE T1.DocEntry = (SELECT DocEntry FROM ZZTEST_SBO_LARCE.dbo.ORIN WHERE DocNum = @DocNum AND DocType = @Tipo)
        AND ISNULL(T1.TreeType, 'N') IN ('N', 'S', 'P')
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
			E.Emisor_Numero as ProveedorSistemas, 

			CONVERT(varchar(19),
			  DATETIME2FROMPARTS(
				YEAR(T0.DocDate), MONTH(T0.DocDate), DAY(T0.DocDate),
				CAST(SUBSTRING(TTS.ts6,1,2) AS int),  -- HH
				CAST(SUBSTRING(TTS.ts6,3,2) AS int),  -- mm
				CAST(SUBSTRING(TTS.ts6,5,2) AS int),  -- ss
				0, 0
			  ),
			  126  -- yyyy-MM-ddTHH:mm:ss
			) + '-06:00' AS Fecha,

		-- Emisor
		    E.CodigoActividadEconomica AS CodigoActividadEconomica,
		    --E.CodigoActividadEconomica,
			E.Emisor_Nombre, E.Emisor_Tipo, E.Emisor_Numero, E.Emisor_NombreComercial,E.Emisor_Registrofiscal8707, E.Emisor_Provincia,
			E.Emisor_Canton, E.Emisor_Distrito, E.Emisor_Barrio, E.Emisor_OtrasSenas, E.Emisor_CodigoPais,
			E.Emisor_NumTelefono, E.Emisor_Fax, E.Emisor_CorreoElectronico,

		-- Receptor
			R.CodigoActividadReceptor,R.CodCliente,
			R.Receptor_Nombre, R.Receptor_Tipo, R.Receptor_Numero, R.Receptor_IdentificacionExtranjero,
			R.Receptor_NombreComercial, DR.Receptor_Provincia, DR.Receptor_Canton, DR.Receptor_Distrito,
			DR.Receptor_Barrio, DR.Receptor_OtrasSenas,DR.Receptor_OtrasSenasExtranjero, R.Receptor_CodigoPais,
			R.Receptor_NumTelefono, R.Receptor_Fax, R.Receptor_CorreoElectronico,

		-- Condición de venta
			CASE
				WHEN T0.GroupNum = -1 THEN '01'   -- Cash Basic
				WHEN UPPER(LTRIM(RTRIM(ISNULL(G.PymntGroup, '')))) IN ('CONTADO', 'CASH BASIC')
					 THEN '01'
				ELSE '02'
			END AS CondicionVenta,
	
			CASE
			WHEN T0.GroupNum = -1 THEN NULL

			WHEN UPPER(LTRIM(RTRIM(ISNULL(G.PymntGroup, '')))) 
				 IN ('CONTADO', 'CASH BASIC') THEN NULL

			WHEN ISNULL(G.ExtraDays, 0) <= 0 
				 AND ISNULL(G.ExtraMonth, 0) <= 0 THEN NULL

			ELSE 
				CAST(
					ISNULL(G.ExtraDays, 0) + (ISNULL(G.ExtraMonth, 0) * 30)
				AS VARCHAR(5))
		END AS PlazoCredito,
 

		-- Medio de pago por defecto
				CASE
				WHEN NULLIF(LTRIM(RTRIM(CAST(ISNULL(T0.U_LDT_MedPag, '') AS VARCHAR(2)))), '') IS NULL THEN '01'
				ELSE RIGHT('00' + CAST(T0.U_LDT_MedPag AS VARCHAR(2)), 2)
			END AS MedioPago,

			'Otros' AS DescPago,
		-- DetalleServicio		 
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
			Impuesto.DetalleServicio_ImpuestoCodigo,					 
			Impuesto.DetalleServicio_ImpuestoCodigoTarifa,			
			Impuesto.DetalleServicio_ImpuestoTarifa,			
			Impuesto.DetalleServicio_ImpuestoMonto,
			Impuesto.DetalleServicio_BaseImponible,
			Impuesto.DetalleServicio_ImpuestoFactorIVA,
			Impuesto.DetalleServicio_ImpuestoCodigoImpuestoOTRO,
            Impuesto.DetalleServicio_ImpuestoNeto,	
			ROUND(
				DS.DetalleServicio_SubTotal
			  + ROUND(Impuesto.DetalleServicio_ImpuestoNeto, 5)
			  + ROUND(COALESCE(Impuesto.MontoImpuestoEspecifico, 0), 5)
			, 5) AS DetalleServicio_MontoTotalLinea,

			DS.DetalleServicio_IVACobradoFabrica,
			DS.DetalleServicio_ImpuestoAsumidoEmisorFabrica,
			0 AS TotalIVADevuelto,
       
			Ex.Exoneracion_TipoDocumento, Ex.Exoneracion_NumeroDocumento, Ex.Exoneracion_MontoImpuesto,	 
			Ex.Exoneracion_NombreInstitucion,Ex.Exoneracion_NombreInstitucionOtros,Ex.Exoneracion_FechaEmision, 
			Ex.Exoneracion_PorcentajeCompra, Ex.Exoneracion_IvaExonerado,Ex.Exoneracion_ImpuestoNeto, Ex.Exoneracion_TotalMercExonerada, 
			Ex.Exoneracion_Articulo,ex.Exoneracion_Inciso,

			RF.ResumenFactura_CodigoMoneda,
			RF.ResumenFactura_TipoCambio,
			RF.ResumenFactura_TotalServGravados,
			RF.ResumenFactura_TotalServExentos,
			RF.ResumenFactura_TotalServExonerado,
			RF.ResumenFactura_TotalServNoSujeto,
			RF.ResumenFactura_TotalMercanciasGravadas,
			RF.ResumenFactura_TotalMercanciasExentas,
			RF.ResumenFactura_TotalMercanciasExonerada,
			RF.ResumenFactura_TotalMercanciasNoSujeto,
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
	        
		-- Información de Referencia
			IR.Referencia_Numero, IR.Referencia_TipoDoc,IR.Referencia_FechaEmision,
			IR.Referencia_HoraEmision,IR.Referencia_Codigo,IR.Referencia_Razon,
		    IR.Referencia_EsTiquete  AS Referencia_EsTiquete,
		
		-- Información Extra
			RIGHT('00000000' + LTRIM(RTRIM(CAST(T0.DocNum AS BIGINT))), 10) AS CodSeguridad,
			T0.SlpCode as Agente,
			CONVERT(date,  SUBSTRING(CONVERT(varchar, T0.DocDate, 103), 0, 11) , 103)as FechaComprobante,
			T0.CreateTS as HoraComprobante,
			--'' as PartidaArancelaria,
			'' AS EnviarComoTE,
			T1.[TaxOnly],
			'' AS TextoXML,
			T0.Comments as Observaciones,
			'' as Adenda_Tipo,
			'' as MontoEnLetras,
			T0.NumAtCard as Param18		,	     
	         T0.NumAtCard AS OrdenCompra     
	
    FROM ZZTEST_SBO_LARCE.dbo.ORIN T0
	CROSS APPLY (
	  SELECT RIGHT('000000' + CONVERT(varchar(6), ISNULL(T0.CreateTS,0)), 6) AS ts6
	) AS TTS
    INNER JOIN ZZTEST_SBO_LARCE.dbo.RIN1 T1 ON T0.DocEntry = T1.DocEntry
	INNER JOIN ZZTEST_SBO_LARCE.dbo.OCRD AS T2 ON T0.CardCode = T2.CardCode 
    INNER JOIN ZZTEST_SBO_LARCE.dbo.OITM T4 ON T1.ItemCode = T4.ItemCode
    INNER JOIN Receptor R ON T0.CardCode = R.CodCliente
    LEFT JOIN DireccionReceptor DR ON T0.CardCode = DR.CardCode
	LEFT JOIN TipoFactura TF ON TF.DocEntry = T0.DocEntry
    JOIN Emisor E ON 1 = 1
	LEFT JOIN Consecutivo C ON 1 = 1
	LEFT JOIN ResumenFactura RF ON 1 = 1  
	LEFT JOIN InfoReferencia IR ON IR.DocEntry = T0.DocEntry
	LEFT JOIN DetalleServicio DS ON DS.DocEntry = T0.DocEntry AND DS.DetalleServicio_NumeroLinea = T1.LineNum
	LEFT JOIN Exoneracion Ex ON Ex.DocEntry = T0.DocEntry AND Ex.LineNum = T1.LineNum
	LEFT JOIN ClaveGenerada K ON K.DocNum = T0.DocNum  
	LEFT JOIN Impuesto Impuesto ON Impuesto.DocEntry = T1.DocEntry AND Impuesto.LineNum = T1.LineNum
	LEFT JOIN TotalComprobanteFinal TCF ON 1 = 1
	LEFT JOIN ZZTEST_SBO_LARCE.dbo.OCTG G    ON G.GroupNum = T0.GroupNum
    WHERE T0.DocType = @Tipo AND T0.DocNum = @DocNum AND ISNULL(T1.TreeType, 'N') IN ('N', 'S', 'P')
END
