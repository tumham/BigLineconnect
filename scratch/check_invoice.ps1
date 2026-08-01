try {
    $connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    Write-Host "[+] Connected to SQL Server!"

    Write-Host "`n--- STOK HAREKETLERI (FIA26 - 14) ---"
    $sqlStok = @"
SELECT 
    sth_evrakno_seri, sth_evrakno_sira, sth_stok_kod, sth_miktar, sth_miktar2, sth_netagirlik,
    sth_birimfiyat, sth_tutar, sth_vergi, sth_har_doviz_cinsi, sth_har_doviz_kuru
FROM dbo.STOK_HAREKETLERI WITH (NOLOCK)
WHERE sth_evrakno_seri = 'FIA26' AND sth_evrakno_sira = 14
"@
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sqlStok
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds)

    foreach ($r in $ds.Tables[0].Rows) {
        Write-Host "Stok: $($r['sth_stok_kod']) | Miktar1: $($r['sth_miktar']) | Miktar2: $($r['sth_miktar2']) | NetAgirlik: $($r['sth_netagirlik']) | BirimFiyat: $($r['sth_birimfiyat']) | Tutar(TL): $($r['sth_tutar']) | Vergi: $($r['sth_vergi'])"
    }

    Write-Host "`n--- CARI HESAP HAREKETLERI (FIA26 - 14) ---"
    $sqlCari = @"
SELECT 
    cha_evrakno_seri, cha_evrakno_sira, cha_kod, cha_meblag, cha_aratoplam,
    cha_doviz_cinsi, cha_doviz_kuru, cha_aracift_doviz_kuru
FROM dbo.CARI_HESAP_HAREKETLERI WITH (NOLOCK)
WHERE cha_evrakno_seri = 'FIA26' AND cha_evrakno_sira = 14
"@
    $cmd.CommandText = $sqlCari
    $ds2 = New-Object System.Data.DataSet
    $adapter.Fill($ds2)

    foreach ($r in $ds2.Tables[0].Rows) {
        Write-Host "Cari: $($r['cha_kod']) | Meblag: $($r['cha_meblag']) | AraToplam: $($r['cha_aratoplam']) | DovizCinsi: $($r['cha_doviz_cinsi']) | DovizKuru: $($r['cha_doviz_kuru'])"
    }

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
