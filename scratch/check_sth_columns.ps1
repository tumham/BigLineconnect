try {
    $connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    Write-Host "[+] Connected to SQL Server!"

    $sql = "SELECT c.name FROM sys.columns c INNER JOIN sys.tables t ON c.object_id = t.object_id WHERE t.name = 'STOK_HAREKETLERI' AND (c.name LIKE '%REC%' OR c.name LIKE '%Guid%')"
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds)

    Write-Host "`n--- REC / GUID COLUMNS IN STOK_HAREKETLERI ---"
    foreach ($r in $ds.Tables[0].Rows) {
        Write-Host "Column: $($r['name'])"
    }

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
