try {
    $connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()

    $testSql = @"
SELECT TOP 1 C.cha_RECno, S.sth_Guid, ISNULL(U.Kalite, '') AS Kalite, ISNULL(U.renk, '') AS Renk
FROM dbo.CARI_HESAP_HAREKETLERI C WITH (NOLOCK)
INNER JOIN dbo.STOK_HAREKETLERI S WITH (NOLOCK)
    ON S.sth_evrakno_seri = C.cha_evrakno_seri 
   AND S.sth_evrakno_sira = C.cha_evrakno_sira 
   AND S.sth_satir_no = C.cha_satir_no
INNER JOIN dbo.STOK_HAREKETLERI_USER U WITH (NOLOCK) 
    ON U.Record_uid = S.sth_Guid
WHERE C.cha_RECno = 54
"@

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $testSql
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds)

    Write-Host "[+] Query Test Success! Row Count: $($ds.Tables[0].Rows.Count)"
    foreach ($r in $ds.Tables[0].Rows) {
        Write-Host " cha_RECno: $($r['cha_RECno']) | GUID: $($r['sth_Guid']) | Kalite: '$($r['Kalite'])' | Renk: '$($r['Renk'])'"
    }

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
