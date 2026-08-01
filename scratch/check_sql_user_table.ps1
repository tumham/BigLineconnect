try {
    $connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    Write-Host "[+] Connected to SQL Server database MikroDesktop_MAHMUT!"

    $sql = "SELECT Record_uid, Kalite, Cilt, Tuy, renk, Urun, Diger FROM STOK_HAREKETLERI_USER WITH (NOLOCK)"
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sql
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds)

    Write-Host "[+] Found $($ds.Tables[0].Rows.Count) rows in STOK_HAREKETLERI_USER:"
    foreach ($r in $ds.Tables[0].Rows) {
        Write-Host "Record_uid: '$($r['Record_uid'])' | Kalite: '$($r['Kalite'])' | Cilt: '$($r['Cilt'])' | Tuy: '$($r['Tuy'])' | Renk: '$($r['renk'])' | Urun: '$($r['Urun'])' | Diger: '$($r['Diger'])'"
    }

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
