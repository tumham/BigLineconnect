try {
    $connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    Write-Host "[+] Connected to SQL Server!"

    Write-Host "`n--- ALL CARI_HESAP_HAREKETLERI ROWS FOR (FIA26 - 14) ---"
    $sqlCari = @"
SELECT 
    cha_Guid, cha_evrak_tip, cha_evrakno_seri, cha_evrakno_sira, cha_satir_no,
    cha_kod, cha_meblag, cha_aratoplam, cha_d_cins, cha_d_kur, cha_altd_kur
FROM dbo.CARI_HESAP_HAREKETLERI WITH (NOLOCK)
WHERE cha_evrakno_seri = 'FIA26' AND cha_evrakno_sira = 14
"@
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sqlCari
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds)

    Write-Host "Total Cari Rows: $($ds.Tables[0].Rows.Count)"
    foreach ($r in $ds.Tables[0].Rows) {
        Write-Host "SatirNo: $($r['cha_satir_no']) | EvrakTip: $($r['cha_evrak_tip']) | Kod: $($r['cha_kod']) | Meblag: $($r['cha_meblag']) | AraToplam: $($r['cha_aratoplam']) | DCins: $($r['cha_d_cins']) | DKur: $($r['cha_d_kur'])"
    }

    Write-Host "`n--- ALL STOK_HAREKETLERI ROWS FOR (FIA26 - 14) ---"
    $sqlStok = @"
SELECT 
    sth_Guid, sth_satirno, sth_stok_kod, sth_miktar, sth_miktar2, sth_netagirlik,
    sth_birimfiyat, sth_tutar, sth_vergi, sth_har_doviz_cinsi, sth_har_doviz_kuru
FROM dbo.STOK_HAREKETLERI WITH (NOLOCK)
WHERE sth_evrakno_seri = 'FIA26' AND sth_evrakno_sira = 14
"@
    $cmd.CommandText = $sqlStok
    $ds2 = New-Object System.Data.DataSet
    $adapter.Fill($ds2)

    Write-Host "Total Stok Rows: $($ds2.Tables[0].Rows.Count)"
    foreach ($r in $ds2.Tables[0].Rows) {
        Write-Host "SatirNo: $($r['sth_satirno']) | Stok: $($r['sth_stok_kod']) | Miktar1: $($r['sth_miktar']) | Miktar2: $($r['sth_miktar2']) | NetAgirlik: $($r['sth_netagirlik']) | Tutar: $($r['sth_tutar']) | DKur: $($r['sth_har_doviz_kuru'])"
    }

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
