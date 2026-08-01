$servers = @(
    "213.142.159.18",
    "213.142.159.18\SQLEXPRESS",
    "213.142.159.18,1433",
    "localhost",
    "localhost\SQLEXPRESS",
    "localhost\BIGUS"
)

foreach ($srv in $servers) {
    try {
        $connStr = "Server=$srv;Database=master;User Id=sa;Password=Bm1453;Encrypt=False;Connection Timeout=3;"
        $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
        $conn.Open()
        
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = "SELECT name FROM sys.databases WHERE name NOT IN ('master','model','msdb','tempdb')"
        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
        $ds = New-Object System.Data.DataSet
        $adapter.Fill($ds)
        
        Write-Host "Server: $srv - FOUND DATABASES:"
        foreach ($row in $ds.Tables[0].Rows) {
            Write-Host "  ---> $($row['name'])"
        }
        $conn.Close()
    } catch {
        Write-Host "Server: $srv - Could not connect: $($_.Exception.Message)"
    }
}
