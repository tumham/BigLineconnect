$connStr = "Server=213.142.159.18;Database=master;User Id=sa;Password=Bm1453;Encrypt=False;Connection Timeout=15;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)

try {
    $conn.Open()
    Write-Host "[+] Connected to SQL Server on 213.142.159.18"

    # Get recent Application Error events from Event Log
    $cmdLog = $conn.CreateCommand()
    $cmdLog.CommandText = "EXEC xp_cmdshell 'powershell -Command ""Get-EventLog -LogName Application -Newest 10 -EntryType Error,Warning | Format-Table -Wrap -AutoSize""';"
    $readerLog = $cmdLog.ExecuteReader()
    Write-Host "--- VDS WINDOWS EVENT LOG ERRORS ---"
    while ($readerLog.Read()) {
        if (-not $readerLog.IsDBNull(0)) { Write-Host $readerLog.GetString(0) }
    }
    $readerLog.Close()

} catch {
    Write-Host "Err:" $_.Exception.Message
} finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
}
