try {
    $connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()

    $sql1 = "SELECT c.name FROM sys.columns c INNER JOIN sys.tables t ON c.object_id = t.object_id WHERE t.name = 'CARI_HESAP_HAREKETLERI' AND (c.name LIKE '%REC%' OR c.name LIKE '%satir%')"
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql1
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds1 = New-Object System.Data.DataSet
    $adapter.Fill($ds1)

    Write-Host "--- CARI_HESAP_HAREKETLERI COLUMNS ---"
    foreach ($r in $ds1.Tables[0].Rows) { Write-Host "Column: $($r['name'])" }

    $sql2 = "SELECT c.name FROM sys.columns c INNER JOIN sys.tables t ON c.object_id = t.object_id WHERE t.name = 'STOK_HAREKETLERI' AND (c.name LIKE '%satir%')"
    $cmd.CommandText = $sql2
    $ds2 = New-Object System.Data.DataSet
    $adapter.Fill($ds2)

    Write-Host "`n--- STOK_HAREKETLERI SATIR COLUMNS ---"
    foreach ($r in $ds2.Tables[0].Rows) { Write-Host "Column: $($r['name'])" }

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
