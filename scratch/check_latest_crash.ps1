$connStr = "Server=213.142.159.18;Database=master;User Id=sa;Password=Bm1453;Encrypt=False;Connection Timeout=15;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)

try {
    $conn.Open()
    Write-Host "[+] Connected to SQL Server on 213.142.159.18"

    # Get newest 3 Application event log entries
    $cmdLog = $conn.CreateCommand()
    $cmdLog.CommandText = "EXEC xp_cmdshell 'powershell -Command ""Get-EventLog -LogName Application -Newest 3 | Format-List TimeGenerated, Source, Message""';"
    $readerLog = $cmdLog.ExecuteReader()
    Write-Host "--- LATEST APPLICATION EVENT LOG ENTRIES ---"
    while ($readerLog.Read()) {
        if (-not $readerLog.IsDBNull(0)) { Write-Host $readerLog.GetString(0) }
    }
    $readerLog.Close()

    # Also check service status right now
    $cmdSvc = $conn.CreateCommand()
    $cmdSvc.CommandText = "EXEC xp_cmdshell 'sc query BigLineconnectRelaySvc';"
    $readerSvc = $cmdSvc.ExecuteReader()
    Write-Host "--- SERVICE STATUS ---"
    while ($readerSvc.Read()) {
        if (-not $readerSvc.IsDBNull(0)) { Write-Host $readerSvc.GetString(0) }
    }
    $readerSvc.Close()

} catch {
    Write-Host "Err:" $_.Exception.Message
} finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
}
