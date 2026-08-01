try {
    $connStr = "Server=213.142.159.18\BIGUS;Database=master;User Id=sa;Password=Bm1453;Encrypt=False;Connection Timeout=10;"
    Write-Host "Connecting to master database..."
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    Write-Host "CONNECTED TO SQL SERVER MASTER!"
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT name FROM sys.databases ORDER BY name"
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds)
    
    Write-Host "Available Databases:"
    foreach ($row in $ds.Tables[0].Rows) {
        Write-Host "- $($row['name'])"
    }
    
    $conn.Close()
} catch {
    Write-Host "SQL Connection Error: $_"
}
