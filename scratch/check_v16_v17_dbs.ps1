try {
    $connStr = "Server=213.142.159.18;Database=master;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    Write-Host "[+] Connected to SQL Server master database!"

    # 1. Veritabanlarını Listeleyelim
    $sqlDbs = "SELECT name FROM sys.databases WHERE name LIKE '%ZEYPORT%' OR name LIKE '%EYPORT%' OR name LIKE '%Mikro%'"
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sqlDbs
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds)

    Write-Host "`n--- RELEVANT DATABASES ON SERVER ---"
    foreach ($r in $ds.Tables[0].Rows) {
        Write-Host "Database: $($r['name'])"
    }

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
