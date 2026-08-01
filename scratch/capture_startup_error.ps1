$connStr = "Server=213.142.159.18;Database=master;User Id=sa;Password=Bm1453;Encrypt=False;Connection Timeout=15;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)

try {
    $conn.Open()
    Write-Host "[+] Connected to SQL Server on 213.142.159.18"

    # Run Relay exe directly via cmd and pipe stdout/stderr to startup_log.txt
    $cmdRun = $conn.CreateCommand()
    $cmdRun.CommandText = "EXEC xp_cmdshell 'cd /d C:\BigLineconnect.Relay\publish_output && BigLineconnect.Relay.exe > C:\BigLineconnect.Relay\startup_log.txt 2>&1';"
    try { $cmdRun.ExecuteNonQuery() } catch {}

    # Read startup_log.txt
    $cmdRead = $conn.CreateCommand()
    $cmdRead.CommandText = "EXEC xp_cmdshell 'type C:\BigLineconnect.Relay\startup_log.txt';"
    $reader = $cmdRead.ExecuteReader()
    Write-Host "--- RELAY STARTUP LOG OUTPUT ---"
    while ($reader.Read()) {
        if (-not $reader.IsDBNull(0)) { Write-Host $reader.GetString(0) }
    }
    $reader.Close()

} catch {
    Write-Host "Err:" $_.Exception.Message
} finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
}
