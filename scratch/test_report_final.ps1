try {
    $connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()

    # Test 1: String comparison (CAST Record_uid to VARCHAR)
    $sql1 = "SELECT TOP 1 ISNULL(U.Kalite, '') AS Kalite FROM dbo.STOK_HAREKETLERI_USER U WITH (NOLOCK) WHERE CAST(U.Record_uid AS VARCHAR(36)) = '56b2c92e-b8c7-4cca-b716-9d87cf763117'"
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql1
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds1 = New-Object System.Data.DataSet
    $adapter.Fill($ds1)

    Write-Host "[+] Test 1 Success! Result: '$($ds1.Tables[0].Rows[0]['Kalite'])'"

    # Test 2: Evrak Seri, Sıra, Satırno JOIN
    $sql2 = @"
SELECT TOP 1 ISNULL(U.Kalite, '') AS Kalite 
FROM dbo.STOK_HAREKETLERI S WITH (NOLOCK)
INNER JOIN dbo.STOK_HAREKETLERI_USER U WITH (NOLOCK) ON U.Record_uid = S.sth_Guid
WHERE S.sth_evrakno_seri = 'FFA26' AND S.sth_evrakno_sira = 339 AND S.sth_satirno = 0
"@
    $cmd.CommandText = $sql2
    $ds2 = New-Object System.Data.DataSet
    $adapter.Fill($ds2)

    Write-Host "[+] Test 2 Success! Result: '$($ds2.Tables[0].Rows[0]['Kalite'])'"

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
