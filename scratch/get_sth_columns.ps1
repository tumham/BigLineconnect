try {
    $connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=10;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 1 * FROM STOK_HAREKETLERI"
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds)
    
    Write-Host "Columns in STOK_HAREKETLERI:"
    foreach ($col in $ds.Tables[0].Columns) {
        Write-Host "- $($col.ColumnName)"
    }
    
    $conn.Close()
} catch {
    Write-Host "SQL Error: $_"
}
