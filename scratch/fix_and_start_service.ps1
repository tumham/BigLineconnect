$connStr = "Server=213.142.159.18;Database=master;User Id=sa;Password=Bm1453;Encrypt=False;Connection Timeout=15;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)

try {
    $conn.Open()
    Write-Host "[+] Connected to SQL Server on 213.142.159.18"

    # Stop service if running
    $cmdStop = $conn.CreateCommand()
    $cmdStop.CommandText = "EXEC xp_cmdshell 'net stop BigLineconnectRelaySvc';"
    try { $cmdStop.ExecuteNonQuery() } catch {}

    # Read admin.html content locally
    $localAdminPath = "C:\Projev17YD\DUZ_V17_STD\BigLineconnect.Relay\wwwroot\admin.html"
    $bytes = [System.IO.File]::ReadAllBytes($localAdminPath)
    $base64 = [Convert]::ToBase64String($bytes)
    
    $chunkSize = 500
    
    $cmdClear = $conn.CreateCommand()
    $cmdClear.CommandText = "EXEC xp_cmdshell 'del /f /q C:\BigLineconnect.Relay\wwwroot\admin_b64.txt';"
    try { $cmdClear.ExecuteNonQuery() } catch {}

    Write-Host "[+] Transferring admin.html Base64 payload..."
    for ($i = 0; $i -lt $base64.Length; $i += $chunkSize) {
        $length = [Math]::Min($chunkSize, $base64.Length - $i)
        $chunk = $base64.Substring($i, $length)
        
        $cmdChunk = $conn.CreateCommand()
        $cmdChunk.CommandText = "EXEC xp_cmdshell 'echo $chunk >> C:\BigLineconnect.Relay\wwwroot\admin_b64.txt';"
        $cmdChunk.ExecuteNonQuery() | Out-Null
    }

    Write-Host "[+] Decoding admin.html to C:\BigLineconnect.Relay\wwwroot\admin.html..."
    $cmdDecodeAdmin = $conn.CreateCommand()
    $cmdDecodeAdmin.CommandText = "EXEC xp_cmdshell 'certutil -f -decode C:\BigLineconnect.Relay\wwwroot\admin_b64.txt C:\BigLineconnect.Relay\wwwroot\admin.html';"
    try { $cmdDecodeAdmin.ExecuteNonQuery() } catch {}

    # Read Relay executable locally
    $localExePath = "C:\Projev17YD\DUZ_V17_STD\BigLineconnect.Relay\bin\Release\net9.0\win-x64\publish\BigLineconnect.Relay.exe"
    $exeBytes = [System.IO.File]::ReadAllBytes($localExePath)
    $exeB64 = [Convert]::ToBase64String($exeBytes)
    
    Write-Host "[+] Transferring BigLineconnect.Relay.exe Base64 ($($exeBytes.Length) bytes)..."
    
    $cmdClearExe = $conn.CreateCommand()
    $cmdClearExe.CommandText = "EXEC xp_cmdshell 'del /f /q C:\BigLineconnect.Relay\relay_b64.txt';"
    try { $cmdClearExe.ExecuteNonQuery() } catch {}

    for ($i = 0; $i -lt $exeB64.Length; $i += $chunkSize) {
        $length = [Math]::Min($chunkSize, $exeB64.Length - $i)
        $chunk = $exeB64.Substring($i, $length)
        
        $cmdChunk = $conn.CreateCommand()
        $cmdChunk.CommandText = "EXEC xp_cmdshell 'echo $chunk >> C:\BigLineconnect.Relay\relay_b64.txt';"
        $cmdChunk.ExecuteNonQuery() | Out-Null
    }

    Write-Host "[+] Decoding new BigLineconnect.Relay.exe to C:\BigLineconnect.Relay\BigLineconnect.Relay.exe..."
    $cmdDecodeExe = $conn.CreateCommand()
    $cmdDecodeExe.CommandText = "EXEC xp_cmdshell 'certutil -f -decode C:\BigLineconnect.Relay\relay_b64.txt C:\BigLineconnect.Relay\BigLineconnect.Relay.exe';"
    $readerExe = $cmdDecodeExe.ExecuteReader()
    while ($readerExe.Read()) {
        if (-not $readerExe.IsDBNull(0)) { Write-Host $readerExe.GetString(0) }
    }
    $readerExe.Close()

    # Delete temp text file
    $cmdDelTempExe = $conn.CreateCommand()
    $cmdDelTempExe.CommandText = "EXEC xp_cmdshell 'del /f /q C:\BigLineconnect.Relay\relay_b64.txt';"
    try { $cmdDelTempExe.ExecuteNonQuery() } catch {}

    Write-Host "[+] Starting BigLineconnectRelaySvc Windows Service on VDS..."
    $cmdStart = $conn.CreateCommand()
    $cmdStart.CommandText = "EXEC xp_cmdshell 'net start BigLineconnectRelaySvc';"
    $readerStart = $cmdStart.ExecuteReader()
    while ($readerStart.Read()) {
        if (-not $readerStart.IsDBNull(0)) { Write-Host $readerStart.GetString(0) }
    }
    $readerStart.Close()

    Write-Host "[+] SERVICE FIX & START COMPLETED SUCCESSFULLY!"
} catch {
    Write-Host "[-] Error:" $_.Exception.Message
} finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
}
