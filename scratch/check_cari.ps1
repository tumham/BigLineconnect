try {
    $connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 1 * FROM dbo.CARI_HESAP_HAREKETLERI"
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds)

    Write-Host "Columns in CARI_HESAP_HAREKETLERI:"
    foreach ($col in $ds.Tables[0].Columns) {
        Write-Host "- $($col.ColumnName)"
    }

    Write-Host "`n--- CARI HESAP HAREKETLERI (FIA26 - 14) ---"
    $sqlCari = "SELECT cha_evrakno_seri, cha_evrakno_sira, cha_kod, cha_meblag, cha_aratoplam FROM dbo.CARI_HESAP_HAREKETLERI WHERE cha_evrakno_seri = 'FIA26' AND cha_evrakno_sira = 14"
    $cmd.CommandText = $sqlCari
    $ds2 = New-Object System.Data.DataSet
    $adapter.Fill($ds2)

    foreach ($r in $ds2.Tables[0].Rows) {
        Write-Host "Cari: $($r['cha_kod']) | Meblag: $($r['cha_meblag']) | AraToplam: $($r['cha_aratoplam'])"
    }

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
