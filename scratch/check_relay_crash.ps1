$connStr = "Server=213.142.159.18;Database=master;User Id=sa;Password=Bm1453;Encrypt=False;Connection Timeout=10;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)

try {
    $conn.Open()
    
    # Read relay_crash.log if exists
    $cmdLog = $conn.CreateCommand()
    $cmdLog.CommandText = "EXEC xp_cmdshell 'type C:\BigLineconnect.Relay\relay_crash.log';"
    $readerLog = $cmdLog.ExecuteReader()
    Write-Host "--- RELAY CRASH LOG ---"
    while ($readerLog.Read()) {
        if (-not $readerLog.IsDBNull(0)) { Write-Host $readerLog.GetString(0) }
    }
    $readerLog.Close()

    # Also test running BigLineconnect.Relay.exe for 2 seconds to see console error output
    $cmdRun = $conn.CreateCommand()
    $cmdRun.CommandText = "EXEC xp_cmdshell 'C:\BigLineconnect.Relay\BigLineconnect.Relay.exe --test';"
    $readerRun = $cmdRun.ExecuteReader()
    Write-Host "--- DIRECT EXE RUN TEST ---"
    while ($readerRun.Read()) {
        if (-not $readerRun.IsDBNull(0)) { Write-Host $readerRun.GetString(0) }
    }
    $readerRun.Close()

} catch {
    Write-Host "Err:" $_.Exception.Message
} finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
}
