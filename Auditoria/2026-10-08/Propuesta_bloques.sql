-- PROPUESTA PARA REVISION. NO EJECUTADA. NO ES UN SCRIPT DE DESPLIEGUE.
-- Aplicar solamente dentro de los SP existentes de Pruebas_SincroSapGoSocket,
-- despues de autorizacion y de resolver las dependencias del informe.
-- No contiene escrituras en SAP ni cambios en tablas/catalogos SAP.

-- A. ClaveGenerada en FE, NC y ND: sustituir SOLO el segmento de identificacion.
-- Mantener la conversion actual para identificaciones numericas y otros tipos.
-- Actual: RIGHT('000000000000' + LTRIM(RTRIM(CAST(E.Emisor_Numero AS BIGINT))),12)
-- Propuesto:
RIGHT('000000000000' +
    CASE WHEN RIGHT('00' + LTRIM(RTRIM(E.Emisor_Tipo)), 2) = '02'
              AND LTRIM(RTRIM(E.Emisor_Numero)) COLLATE Latin1_General_100_BIN2 LIKE '%[A-Za-z]%'
         THEN LTRIM(RTRIM(E.Emisor_Numero))
         ELSE LTRIM(RTRIM(CAST(E.Emisor_Numero AS BIGINT)))
    END, 12)
-- Validar previamente longitud 10 y caracteres ASCII para el caso juridico.
-- FEC ya construye este segmento como texto: no sustituir su formula.

-- B. InfoReferencia: incorporar en CASE de razon NC y ND, conservando otras ramas.
-- Reemplazar la rama 12; agregar 13-16 antes de 99.
-- WHEN '12' THEN 'Nota de credito financiera por exoneracion posterior a la facturacion'
-- WHEN '13' THEN 'Anula documento de referencia por error material'
-- WHEN '14' THEN 'Corrige monto por error material'
-- WHEN '15' THEN 'Sustituye comprobante electronico por error material'
-- WHEN '16' THEN 'Sustituye comprobante electronico rechazado'
-- 17 = Pago a comprobante electronico; catalogo reconocido, uso exclusivo REP.
-- No habilitar su emision en FE/NC/ND/FEC. Validarlo en ConstruirReferenciasDesdeSp.

-- C. NC: reemplazar cada filtro exclusivamente numerico de U_LDT_FiscalDoc
-- por este predicado estructural, usando RefBase o RefInv segun el bloque:
LEN(LTRIM(RTRIM(RefBase.U_LDT_FiscalDoc))) = 50
AND LEFT(LTRIM(RTRIM(RefBase.U_LDT_FiscalDoc)), 9) COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'
AND SUBSTRING(LTRIM(RTRIM(RefBase.U_LDT_FiscalDoc)), 10, 12) COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9A-Za-z]%'
AND SUBSTRING(LTRIM(RTRIM(RefBase.U_LDT_FiscalDoc)), 22, 29) COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'
-- Mantener COALESCE, COLLATE DATABASE_DEFAULT, joins y prioridades existentes.
-- Esta validacion estructural no certifica identidad ni aceptacion fiscal.

-- D. NC: traducir el tipo extraido de posiciones 30-31 de la clave.
-- Actual: SUBSTRING(clave_resuelta,30,2).
-- Propuesto: CASE WHEN SUBSTRING(clave_resuelta,30,2) = '09' THEN '19'
--                 ELSE SUBSTRING(clave_resuelta,30,2) END.
-- clave_resuelta representa la expresion COALESCE actual, NO una columna nueva.
-- Conservar 09 historico; ampliar las dos condiciones de exportacion:
-- IR.Referencia_TipoDoc = '09' -> IR.Referencia_TipoDoc IN ('09','19')
-- ISNULL(IR.Referencia_TipoDoc,'') <> '09'
--     -> ISNULL(IR.Referencia_TipoDoc,'') NOT IN ('09','19')
-- No interpretar 19 manual como exportacion hasta resolver el catalogo SAP "Otros".

-- E. Proyeccion de tipo manual existente NC/ND/FEC: normalizar 1 a 01,
-- conservando NULL/vacio para los fallback actuales. 19 y 20 ya caben en varchar(2).
NULLIF(RIGHT('00' + NULLIF(LTRIM(RTRIM(NC.U_LDT_TipoDocRef)), ''), 2), '')
-- Verificar longitud antes de RIGHT para no truncar datos invalidos.

-- F. FE/FEE/TE: agregar InfoReferencia y su LEFT JOIN solo para codigo 15/16
-- (y 07 si se autoriza para sustitucion ordinaria), usando UDF existentes.
-- Dependencia: fijar fuente de clave original y fecha original con un caso real.
-- No usar U_LDT_FiscalDoc del documento nuevo como si fuera la clave original.
-- No se propone ALTER PROCEDURE completo mientras esta dependencia siga abierta.
