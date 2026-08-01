try {
    $connStr = "Server=213.142.159.18\BIGUS;Database=master;User Id=sa;Password=Bm1453;Encrypt=False;Connection Timeout=10;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "EXEC sp_databases"
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds)
    
    Write-Host "Databases via sp_databases:"
    foreach ($row in $ds.Tables[0].Rows) {
        Write-Host "- $($row['DATABASE_NAME'])"
    }
    
    $conn.Close()
} catch {
    Write-Host "SQL Connection Error: $_"
}
