$connStr = "Server=213.142.159.18;Database=master;User Id=sa;Password=Bm1453;Encrypt=False;Connection Timeout=15;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)

try {
    $conn.Open()
    Write-Host "[+] Connected to SQL Server on 213.142.159.18"

    # Stop service
    $cmdStop = $conn.CreateCommand()
    $cmdStop.CommandText = "EXEC xp_cmdshell 'net stop BigLineconnectRelaySvc';"
    try { $cmdStop.ExecuteNonQuery() } catch {}

    # Run EXE directly with timeout/pipe to capture console error
    $cmdRun = $conn.CreateCommand()
    $cmdRun.CommandText = "EXEC xp_cmdshell 'cd /d C:\BigLineconnect.Relay && BigLineconnect.Relay.exe --console > C:\BigLineconnect.Relay\debug.txt 2>&1';"
    try { $cmdRun.ExecuteNonQuery() } catch {}

    # Read debug.txt
    $cmdRead = $conn.CreateCommand()
    $cmdRead.CommandText = "EXEC xp_cmdshell 'type C:\BigLineconnect.Relay\debug.txt';"
    $reader = $cmdRead.ExecuteReader()
    Write-Host "--- DEBUG OUTPUT ---"
    while ($reader.Read()) {
        if (-not $reader.IsDBNull(0)) { Write-Host $reader.GetString(0) }
    }
    $reader.Close()

    # Read relay_crash.log
    $cmdCrash = $conn.CreateCommand()
    $cmdCrash.CommandText = "EXEC xp_cmdshell 'type C:\BigLineconnect.Relay\relay_crash.log';"
    $readerCrash = $cmdCrash.ExecuteReader()
    Write-Host "--- CRASH LOG OUTPUT ---"
    while ($readerCrash.Read()) {
        if (-not $readerCrash.IsDBNull(0)) { Write-Host $readerCrash.GetString(0) }
    }
    $readerCrash.Close()

} catch {
    Write-Host "Err:" $_.Exception.Message
} finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
}
