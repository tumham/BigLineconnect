$connStr = "Server=213.142.159.18;Database=master;User Id=sa;Password=Bm1453;Encrypt=False;Connection Timeout=15;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)

try {
    $conn.Open()
    Write-Host "[+] Connected to SQL Server on 213.142.159.18"

    # Stop any running process first
    $cmdKill = $conn.CreateCommand()
    $cmdKill.CommandText = "EXEC xp_cmdshell 'taskkill /f /im BigLineconnect.Relay.exe';"
    try { $cmdKill.ExecuteNonQuery() } catch {}

    # Read admin.html content locally
    $localAdminPath = "C:\Projev17YD\DUZ_V17_STD\BigLineconnect.Relay\wwwroot\admin.html"
    $bytes = [System.IO.File]::ReadAllBytes($localAdminPath)
    $base64 = [Convert]::ToBase64String($bytes)
    
    $chunkSize = 500
    
    $cmdClear = $conn.CreateCommand()
    $cmdClear.CommandText = "EXEC xp_cmdshell 'del /f /q C:\BigLineconnect.Relay\admin_b64.txt';"
    try { $cmdClear.ExecuteNonQuery() } catch {}

    Write-Host "[+] Transferring admin.html Base64 payload..."
    for ($i = 0; $i -lt $base64.Length; $i += $chunkSize) {
        $length = [Math]::Min($chunkSize, $base64.Length - $i)
        $chunk = $base64.Substring($i, $length)
        
        $cmdChunk = $conn.CreateCommand()
        $cmdChunk.CommandText = "EXEC xp_cmdshell 'echo $chunk >> C:\BigLineconnect.Relay\admin_b64.txt';"
        $cmdChunk.ExecuteNonQuery() | Out-Null
    }

    Write-Host "[+] Decoding admin.html across all wwwroot folders on VDS..."
    $cmdDecode1 = $conn.CreateCommand()
    $cmdDecode1.CommandText = "EXEC xp_cmdshell 'certutil -f -decode C:\BigLineconnect.Relay\admin_b64.txt C:\BigLineconnect.Relay\wwwroot\admin.html';"
    try { $cmdDecode1.ExecuteNonQuery() } catch {}

    $cmdDecode2 = $conn.CreateCommand()
    $cmdDecode2.CommandText = "EXEC xp_cmdshell 'certutil -f -decode C:\BigLineconnect.Relay\admin_b64.txt C:\BigLineconnect.Relay\bin\Release\net9.0\win-x64\publish\wwwroot\admin.html';"
    try { $cmdDecode2.ExecuteNonQuery() } catch {}

    $cmdDecode3 = $conn.CreateCommand()
    $cmdDecode3.CommandText = "EXEC xp_cmdshell 'certutil -f -decode C:\BigLineconnect.Relay\admin_b64.txt C:\BigLineconnect.Relay\publish_output\wwwroot\admin.html';"
    try { $cmdDecode3.ExecuteNonQuery() } catch {}

    # Clean temp b64 file
    $cmdDelTemp = $conn.CreateCommand()
    $cmdDelTemp.CommandText = "EXEC xp_cmdshell 'del /f /q C:\BigLineconnect.Relay\admin_b64.txt';"
    try { $cmdDelTemp.ExecuteNonQuery() } catch {}

    Write-Host "[+] Starting Relay server process from publish_output directory on VDS..."
    $cmdStart1 = $conn.CreateCommand()
    $cmdStart1.CommandText = "EXEC xp_cmdshell 'cd /d C:\BigLineconnect.Relay\publish_output && start /b BigLineconnect.Relay.exe';"
    try { $cmdStart1.ExecuteNonQuery() } catch {}

    # Wait 2 seconds
    $cmdWait = $conn.CreateCommand()
    $cmdWait.CommandText = "EXEC xp_cmdshell 'ping 127.0.0.1 -n 3 >nul';"
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

    Write-Host "[+] RESTORE AND START COMPLETED!"
} catch {
    Write-Host "[-] Error:" $_.Exception.Message
} finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
}
