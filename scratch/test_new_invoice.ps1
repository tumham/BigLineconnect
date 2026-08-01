try {
    $connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    Write-Host "[+] Connected to SQL Server!"

    # Row 2 in STOK_HAREKETLERI (Adet = 90) sth_netagirlik = 7 olarak güncelleyelim
    $updateSql = @"
UPDATE dbo.STOK_HAREKETLERI
SET sth_netagirlik = 7.0
WHERE sth_evrakno_seri = 'FIA26' AND sth_evrakno_sira = 14 AND sth_miktar = 90;
"@
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $updateSql
    $rowsAffected = $cmd.ExecuteNonQuery()
    Write-Host "[+] Updated Row 2 in STOK_HAREKETLERI ($rowsAffected row(s) updated to NetAgirlik = 7.0)."

    # msp_CariFoy prosedürünü çalıştıralım
    $tempTable = "CARI_FOYU_TEST_" + (Get-Date -Format "yyyyMMdd_HHmmss")
    $spSql = "EXEC dbo.msp_CariFoy N'0',0,N'USDM016',NULL,'20251231','20260101','20261231',0,N'',$tempTable"
    $cmd.CommandText = $spSql
    $cmd.ExecuteNonQuery()

    $selectSql = "SELECT * FROM $tempTable"
    $cmd.CommandText = $selectSql
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds)

    Write-Host "`n--- CARI FOY SP RESULT AFTER NEW INVOICE UPDATE ---"
    foreach ($r in $ds.Tables[0].Rows) {
        Write-Host "ANA TL BORC   : $($r['msg_S_0101\T']) TL"
        Write-Host "ALT EUR BORC  : $($r['msg_S_0105\T']) EUR"
        Write-Host "ORJ USD BORC  : $($r['msg_S_0109\T']) USD"
    }

    # Drop temp table
    $cmd.CommandText = "IF OBJECT_ID('$tempTable') IS NOT NULL DROP TABLE $tempTable"
    $cmd.ExecuteNonQuery()

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
