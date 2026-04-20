USE [SincroSapGoSocket];
GO

-- 1) Crear el usuario en esta base, mapeado al login de SAP (si no existe)
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'B1_5A5A544553545F53424F5F4C415243435_RW')
BEGIN
    CREATE USER [B1_5A5A544553545F53424F5F4C415243435_RW]
    FOR LOGIN [B1_5A5A544553545F53424F5F4C415243435_RW];
END
GO

-- 2) Dar permisos mínimos necesarios sobre la cola
GRANT INSERT ON OBJECT::[Integration].[DocumentQueue]
TO [B1_5A5A544553545F53424F5F4C415243435_RW];

-- (Opcional pero útil si tu servicio luego “lee” la cola desde SAP user)
GRANT SELECT ON OBJECT::[Integration].[DocumentQueue]
TO [B1_5A5A544553545F53424F5F4C415243435_RW];
GO
