try {
    $connStr = "Server=213.142.159.18\BIGUS;Database=MikroDesktop_MAHMUT;User Id=sa;Password=Bm1453;Encrypt=False;Connection Timeout=10;"
    Write-Host "Connecting to SQL Server with SA credentials..."
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    Write-Host "SUCCESS! Connected to SQL Server 213.142.159.18\BIGUS Database MikroDesktop_MAHMUT!"
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 10 Record_uid, Kalite, Cilt, Tuy, renk, Urun, Diger FROM STOK_HAREKETLERI_USER"
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds)
    
    Write-Host "Sample Rows from STOK_HAREKETLERI_USER:"
    foreach ($row in $ds.Tables[0].Rows) {
        Write-Host "UID: $($row['Record_uid']) | Kalite: $($row['Kalite']) | Cilt: $($row['Cilt']) | Tuy: $($row['Tuy']) | Renk: $($row['renk']) | Urun: $($row['Urun']) | Diger: $($row['Diger'])"
    }
    
    $conn.Close()
} catch {
    Write-Host "SQL Connection Error: $_"
}
