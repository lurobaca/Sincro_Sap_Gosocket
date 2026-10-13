$ErrorActionPreference = 'Stop'
$config = Get-Content 'Sincro_Sap_Gosocket/Sincro_Sap_Gosocket/appsettings.json' -Raw | ConvertFrom-Json
$builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder($config.ConnectionStrings.Sql)
$builder['Data Source'] = 'dilarceappsynch.com'
$builder['Initial Catalog'] = 'Pruebas_SincroSapGoSocket'
$builder['Connect Timeout'] = 10
$builder['Encrypt'] = $true
$builder['TrustServerCertificate'] = $true
$connection = New-Object System.Data.SqlClient.SqlConnection($builder.ConnectionString)
try {
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = "SELECT s.name AS Esquema, o.name, m.definition FROM sys.sql_modules m JOIN sys.objects o ON o.object_id=m.object_id JOIN sys.schemas s ON s.schema_id=o.schema_id WHERE o.name IN ('SP_Consulta_FE_FES_V44','SP_Consulta_NC_NCS_V44','SP_Consulta_ND_NDS_V44','SP_Consulta_FEC_V44')"
    $reader = $command.ExecuteReader()
    while ($reader.Read()) {
        $name = [string]$reader['name']
        $definition = [string]$reader['definition']
        [System.IO.File]::WriteAllText((Join-Path (Get-Location) "Auditoria/2026-10-08/$name.desplegado.txt"), $definition)
        Write-Output ("Definicion leida: {0}.{1}; caracteres={2}" -f $reader['Esquema'], $name, $definition.Length)
    }
    $reader.Close()
    $command.CommandText = "SELECT c.TABLE_NAME,c.COLUMN_NAME,c.DATA_TYPE,c.CHARACTER_MAXIMUM_LENGTH FROM SBO_LARCE.INFORMATION_SCHEMA.COLUMNS c WHERE (c.TABLE_NAME IN ('OCRD','OADM') AND c.COLUMN_NAME IN ('LicTradNum','TaxIdNum')) OR (c.TABLE_NAME IN ('OINV','ORIN','OPCH') AND c.COLUMN_NAME IN ('U_LDT_CodRef','U_LDT_TipoDocRef','U_LDT_FiscalDoc'))"
    $reader = $command.ExecuteReader()
    while ($reader.Read()) { Write-Output ("SAP solo lectura: {0}.{1}: {2}({3})" -f $reader.GetValue(0),$reader.GetValue(1),$reader.GetValue(2),$reader.GetValue(3)) }
    $reader.Close()
    $command.CommandText = "SELECT f.TableID,f.AliasID,v.FldValue,v.Descr FROM SBO_LARCE.dbo.CUFD f LEFT JOIN SBO_LARCE.dbo.UFD1 v ON v.TableID=f.TableID AND v.FieldID=f.FieldID WHERE f.TableID IN ('OINV','ORIN','OPCH') AND f.AliasID IN ('LDT_CodRef','LDT_TipoDocRef') ORDER BY f.TableID,f.AliasID,v.IndexID"
    $reader = $command.ExecuteReader()
    while ($reader.Read()) { Write-Output ("Catalogo SAP: {0}.{1}; {2}={3}" -f $reader.GetValue(0),$reader.GetValue(1),$reader.GetValue(2),$reader.GetValue(3)) }
    $reader.Close()
    $command.CommandText = "SELECT TABLE_SCHEMA,TABLE_NAME,COLUMN_NAME,DATA_TYPE,CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE COLUMN_NAME LIKE '%Clave%'"
    $reader = $command.ExecuteReader()
    while ($reader.Read()) { Write-Output ("Clave proyecto: {0}.{1}.{2}: {3}({4})" -f $reader.GetValue(0),$reader.GetValue(1),$reader.GetValue(2),$reader.GetValue(3),$reader.GetValue(4)) }
    $reader.Close()
} finally { $connection.Dispose() }
