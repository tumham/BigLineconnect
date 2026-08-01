try {
    $connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    Write-Host "[+] Connected to SQL Server!"

    Write-Host "`n--- STOK_HAREKETLERI (FIA26 - 14) ALL ROWS ---"
    $sqlStok = @"
SELECT 
    sth_satirno, sth_stok_kod, sth_cins, sth_miktar, sth_miktar2, sth_netagirlik,
    sth_birimfiyat, sth_tutar, sth_vergi
FROM dbo.STOK_HAREKETLERI WITH (NOLOCK)
WHERE sth_evrakno_seri = 'FIA26' AND sth_evrakno_sira = 14
"@
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sqlStok
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds)

    $sumTutar = 0
    foreach ($r in $ds.Tables[0].Rows) {
        $t = [double]$r['sth_tutar']
        $sumTutar += $t
        Write-Host "Satir: $($r['sth_satirno']) | Cins: $($r['sth_cins']) | Stok: $($r['sth_stok_kod']) | Miktar: $($r['sth_miktar']) | NetAgirlik: $($r['sth_netagirlik']) | Tutar: $t"
    }
    Write-Host "TOTAL STOK TUTAR SUM: $sumTutar"

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
