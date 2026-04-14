USE [ZZTEST_SBO_LARCE]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER TRIGGER [dbo].[TR_OPCH_Enqueue]
ON [dbo].[OPCH]
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    /*==============================================================
      1. INSERT: encolar facturas de compra nuevas de OPCH
         solo si U_EnviarHacienda = 1
         TipoCE = FEC
    ==============================================================*/
    IF EXISTS (SELECT 1 FROM inserted)
       AND NOT EXISTS (SELECT 1 FROM deleted)
    BEGIN
        INSERT INTO [SincroSapGoSocket].[Integration].[DocumentosPendientes]
        (
            SourceSystem,
            TipoCE,
            ObjType,
            DocEntry,
            DocNum,
            DocSubType,
            DocType,
            CardCode,
            TaxDate,
            Status
        )
        SELECT
            'SAP' AS SourceSystem,
            'FEC' AS TipoCE,
            '18' AS ObjType,
            i.DocEntry,
            i.DocNum,
            i.DocSubType,
            i.DocType,
            i.CardCode,
            i.TaxDate,
            'PENDING' AS Status
        FROM inserted i
        LEFT JOIN [SincroSapGoSocket].[Integration].[DocumentosPendientes] q
               ON q.ObjType = '18'
              AND q.DocEntry = i.DocEntry
        WHERE q.DocumentosPendientes_Id IS NULL
          AND ISNULL(i.CANCELED, 'N') = 'N'
          AND ISNULL(i.U_EnviarHacienda, '0') = '1';
    END

    /*==============================================================
      2. UPDATE: si cambia U_Reintenta, reactivar en cola
         solo para FEC ya encoladas
    ==============================================================*/
    IF EXISTS (SELECT 1 FROM inserted)
       AND EXISTS (SELECT 1 FROM deleted)
    BEGIN
        ;WITH DocsReintento AS
        (
            SELECT
                i.DocEntry,
                i.DocNum,
                i.U_Reintenta AS U_Reintenta_Nuevo,
                d.U_Reintenta AS U_Reintenta_Anterior
            FROM inserted i
            INNER JOIN deleted d
                    ON d.DocEntry = i.DocEntry
            WHERE ISNULL(i.U_Reintenta, '') <> ISNULL(d.U_Reintenta, '')
        )
        UPDATE q
           SET q.Status        = 'PENDING',
               q.NextAttemptAt = NULL,
               q.LastAttemptAt = NULL,
               q.LockedBy      = NULL,
               q.LockedAt      = NULL,
               q.LastError     = NULL
               -- opcional:
               -- ,q.AttemptCount = 0
        FROM [SincroSapGoSocket].[Integration].[DocumentosPendientes] q
        INNER JOIN DocsReintento r
                ON q.ObjType = '18'
               AND q.DocEntry = r.DocEntry
        WHERE q.TipoCE = 'FEC';
    END

    /*==============================================================
      3. UPDATE: si U_EnviarHacienda cambia de 0 a 1,
         encolar el documento si todavía no existe en cola
    ==============================================================*/
    IF EXISTS (SELECT 1 FROM inserted)
       AND EXISTS (SELECT 1 FROM deleted)
    BEGIN
        INSERT INTO [SincroSapGoSocket].[Integration].[DocumentosPendientes]
        (
            SourceSystem,
            TipoCE,
            ObjType,
            DocEntry,
            DocNum,
            DocSubType,
            DocType,
            CardCode,
            TaxDate,
            Status
        )
        SELECT
            'SAP' AS SourceSystem,
            'FEC' AS TipoCE,
            '18' AS ObjType,
            i.DocEntry,
            i.DocNum,
            i.DocSubType,
            i.DocType,
            i.CardCode,
            i.TaxDate,
            'PENDING' AS Status
        FROM inserted i
        INNER JOIN deleted d
                ON d.DocEntry = i.DocEntry
        LEFT JOIN [SincroSapGoSocket].[Integration].[DocumentosPendientes] q
               ON q.ObjType = '18'
              AND q.DocEntry = i.DocEntry
        WHERE q.DocumentosPendientes_Id IS NULL
          AND ISNULL(i.CANCELED, 'N') = 'N'
          AND ISNULL(d.U_EnviarHacienda, '0') <> '1'
          AND ISNULL(i.U_EnviarHacienda, '0') = '1';
    END
END
GO