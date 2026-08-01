try {
    $connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    Write-Host "[+] Connected to SQL Server database MikroDesktop_MAHMUT!"

    $sql = @"
SELECT 
    tr.name AS TriggerName,
    OBJECT_DEFINITION(tr.object_id) AS TriggerDefinition,
    tr.is_disabled AS IsDisabled
FROM sys.triggers tr
INNER JOIN sys.tables tb ON tr.parent_id = tb.object_id
WHERE tb.name = 'STOK_HAREKETLERI'
"@

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds)
    $conn.Close()

    Write-Host "[+] Found $($ds.Tables[0].Rows.Count) trigger(s) on STOK_HAREKETLERI:"
    foreach ($r in $ds.Tables[0].Rows) {
        Write-Host "==================================================="
        Write-Host "TRIGGER NAME: $($r['TriggerName']) | Disabled: $($r['IsDisabled'])"
        Write-Host "==================================================="
        Write-Host $r['TriggerDefinition']
        Write-Host ""
    }
} catch {
    Write-Host "[!] SQL Error: $_"
}
