$connStr = "Server=213.142.159.18;Database=master;User Id=sa;Password=Bm1453;Encrypt=False;Connection Timeout=15;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)

try {
    $conn.Open()
    Write-Host "[+] Connected to SQL Server on 213.142.159.18"

    # Stop any existing process
    $cmdKill = $conn.CreateCommand()
    $cmdKill.CommandText = "EXEC xp_cmdshell 'taskkill /f /im BigLineconnect.Relay.exe';"
    try { $cmdKill.ExecuteNonQuery() } catch {}

    # Use PowerShell Start-Process to launch decoupled process
    $cmdStart = $conn.CreateCommand()
    $cmdStart.CommandText = "EXEC xp_cmdshell 'powershell -Command ""Start-Process -FilePath C:\BigLineconnect.Relay\publish_output\BigLineconnect.Relay.exe -WorkingDirectory C:\BigLineconnect.Relay\publish_output""';"
    $cmdStart.ExecuteNonQuery() | Out-Null

    # Wait 3 seconds
    $cmdWait = $conn.CreateCommand()
    $cmdWait.CommandText = "EXEC xp_cmdshell 'ping 127.0.0.1 -n 4 >nul';"
    try { $cmdWait.ExecuteNonQuery() } catch {}

    # Verify process
    $cmdProc = $conn.CreateCommand()
    $cmdProc.CommandText = "EXEC xp_cmdshell 'wmic process where ""name like ''%Relay%''"" get ProcessId, ExecutablePath';"
    $readerProc = $cmdProc.ExecuteReader()
    Write-Host "--- ACTIVE RELAY PROCESSES ---"
    while ($readerProc.Read()) {
        if (-not $readerProc.IsDBNull(0)) { Write-Host $readerProc.GetString(0) }
    }
    $readerProc.Close()

    Write-Host "[+] PERSISTENT START COMPLETED!"
} catch {
    Write-Host "[-] Error:" $_.Exception.Message
} finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
}
