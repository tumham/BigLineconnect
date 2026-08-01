try {
    $connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    Write-Host "[+] Connected to SQL Server!"

    $tempTable = "CARI_FOYU_TEST_" + (Get-Date -Format "yyyyMMdd_HHmmss")
    $spSql = "EXEC dbo.msp_CariFoy N'0',0,N'USDM016',NULL,'20251231','20260101','20261231',0,N'',$tempTable"
    
    Write-Host "Executing SP: $spSql"
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $spSql
    $cmd.ExecuteNonQuery()

    $selectSql = "SELECT * FROM $tempTable"
    $cmd.CommandText = $selectSql
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds)

    Write-Host "[+] Cari Föy SP Result Rows: $($ds.Tables[0].Rows.Count)"
    foreach ($r in $ds.Tables[0].Rows) {
        Write-Host "--- ROW ---"
        foreach ($col in $ds.Tables[0].Columns) {
            $val = $r[$col.ColumnName]
            if ($val -and $val.ToString().Trim() -ne "" -and $val.ToString() -ne "0") {
                Write-Host "  $($col.ColumnName) : $val"
            }
        }
    }

    # Drop temp table
    $cmd.CommandText = "IF OBJECT_ID('$tempTable') IS NOT NULL DROP TABLE $tempTable"
    $cmd.ExecuteNonQuery()

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
