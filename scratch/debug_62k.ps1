try {
    $connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    Write-Host "[+] Connected to SQL Server!"

    Write-Host "`n--- CARI_HESAP_HAREKETLERI ROWS FOR FIA26-14 ---"
    $sqlCari = @"
SELECT 
    cha_satir_no, cha_evrak_tip, cha_cinsi, cha_kod, cha_meblag, cha_aratoplam, cha_d_kur
FROM dbo.CARI_HESAP_HAREKETLERI WITH (NOLOCK)
WHERE cha_evrakno_seri = 'FIA26' AND cha_evrakno_sira = 14
"@
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sqlCari
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds)

    $sumCari = 0
    foreach ($r in $ds.Tables[0].Rows) {
        $m = [double]$r['cha_meblag']
        $sumCari += $m
        Write-Host "SatirNo: $($r['cha_satir_no']) | Cinsi: $($r['cha_cinsi']) | Meblag: $m | AraToplam: $($r['cha_aratoplam']) | DKur: $($r['cha_d_kur'])"
    }
    Write-Host "SUM OF CARI MEBLAG: $sumCari"

    Write-Host "`n--- STOK_HAREKETLERI ROWS FOR FIA26-14 ---"
    $sqlStok = @"
SELECT 
    sth_satirno, sth_stok_kod, sth_cins, sth_miktar, sth_miktar2, sth_netagirlik,
    sth_birimfiyat, sth_tutar
FROM dbo.STOK_HAREKETLERI WITH (NOLOCK)
WHERE sth_evrakno_seri = 'FIA26' AND sth_evrakno_sira = 14
"@
    $cmd.CommandText = $sqlStok
    $ds2 = New-Object System.Data.DataSet
    $adapter.Fill($ds2)

    $sumStok = 0
    foreach ($r in $ds2.Tables[0].Rows) {
        $t = [double]$r['sth_tutar']
        $sumStok += $t
        Write-Host "SatirNo: $($r['sth_satirno']) | Stok: $($r['sth_stok_kod']) | Miktar1: $($r['sth_miktar']) | Miktar2: $($r['sth_miktar2']) | NetAgirlik: $($r['sth_netagirlik']) | BirimFiyat: $($r['sth_birimfiyat']) | Tutar: $t"
    }
    Write-Host "SUM OF STOK TUTAR: $sumStok"

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
