try {
    $connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT cha_satir_no, cha_evrak_tip, cha_cinsi, cha_kod, cha_meblag, cha_aratoplam FROM dbo.CARI_HESAP_HAREKETLERI WHERE cha_evrakno_seri = 'FIA26' AND cha_evrakno_sira = 14"
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds)

    Write-Host "--- CARI_HESAP_HAREKETLERI FOR FIA26-14 ---"
    foreach ($r in $ds.Tables[0].Rows) {
        Write-Host "SatirNo: $($r['cha_satir_no']) | Cinsi: $($r['cha_cinsi']) | Kod: $($r['cha_kod']) | Meblag: $($r['cha_meblag'])"
    }

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
