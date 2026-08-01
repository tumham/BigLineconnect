try {
    $connStr = "Server=localhost;Database=MikroDB_V16;Integrated Security=True;Encrypt=False;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    Write-Host "Connected to SQL Server!"
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "SELECT TOP 1 * FROM STOK_HAREKETLERI_USER"
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds)
    $table = $ds.Tables[0]
    foreach ($col in $table.Columns) {
        Write-Host $col.ColumnName
    }
    $conn.Close()
} catch {
    Write-Host "SQL Error: $_"
}
