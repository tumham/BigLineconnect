try {
    $connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    Write-Host "[+] Connected to SQL Server!"

    $sql = @"
SELECT 
    sh.sth_Guid, sh.sth_evrakno_seri, sh.sth_evrakno_sira, sh.sth_tarih, sh.sth_stok_kod,
    usr.Kalite, usr.Cilt, usr.Tuy, usr.renk
FROM STOK_HAREKETLERI sh WITH (NOLOCK)
INNER JOIN STOK_HAREKETLERI_USER usr WITH (NOLOCK) ON sh.sth_Guid = usr.Record_uid
"@
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds)

    Write-Host "[+] Found $($ds.Tables[0].Rows.Count) rows joined with STOK_HAREKETLERI_USER:"
    foreach ($r in $ds.Tables[0].Rows) {
        Write-Host "GUID: $($r['sth_Guid']) | Evrak: $($r['sth_evrakno_seri'])-$($r['sth_evrakno_sira']) | Tarih: $($r['sth_tarih']) | Stok: $($r['sth_stok_kod']) | Renk: $($r['renk']) | Kalite: $($r['Kalite'])"
    }

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
