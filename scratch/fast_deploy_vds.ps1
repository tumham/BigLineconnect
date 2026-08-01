$connStr = "Server=213.142.159.18;Database=master;User Id=sa;Password=Bm1453;Encrypt=False;Connection Timeout=30;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)

try {
    $conn.Open()
    Write-Host "[+] Connected to SQL Server on 213.142.159.18"

    # Stop service first
    $cmdStop = $conn.CreateCommand()
    $cmdStop.CommandText = "EXEC xp_cmdshell 'net stop BigLineconnectRelaySvc';"
    try { $cmdStop.ExecuteNonQuery() } catch {}

    # 1. Transfer admin.html
    $localAdminPath = "C:\Projev17YD\DUZ_V17_STD\BigLineconnect.Relay\wwwroot\admin.html"
    $adminBytes = [System.IO.File]::ReadAllBytes($localAdminPath)
    $adminB64 = [Convert]::ToBase64String($adminBytes)

    $cmdClearAdmin = $conn.CreateCommand()
    $cmdClearAdmin.CommandText = "EXEC xp_cmdshell 'powershell -Command ""Remove-Item -Path C:\BigLineconnect.Relay\wwwroot\admin_b64.txt -Force -ErrorAction SilentlyContinue""';"
    try { $cmdClearAdmin.ExecuteNonQuery() } catch {}

    # Send admin.html in 50KB chunks
    $chunkSize = 50000
    for ($i = 0; $i -lt $adminB64.Length; $i += $chunkSize) {
        $len = [Math]::Min($chunkSize, $adminB64.Length - $i)
        $chunk = $adminB64.Substring($i, $len)
        $cmdChunk = $conn.CreateCommand()
        $cmdChunk.CommandText = "EXEC xp_cmdshell 'powershell -Command ""Add-Content -Path C:\BigLineconnect.Relay\wwwroot\admin_b64.txt -Value ''$chunk'' -NoNewline""';"
        $cmdChunk.ExecuteNonQuery() | Out-Null
    }

    Write-Host "[+] Decoding admin.html..."
    $cmdDecodeAdmin = $conn.CreateCommand()
    $cmdDecodeAdmin.CommandText = "EXEC xp_cmdshell 'certutil -f -decode C:\BigLineconnect.Relay\wwwroot\admin_b64.txt C:\BigLineconnect.Relay\wwwroot\admin.html';"
    $cmdDecodeAdmin.ExecuteNonQuery() | Out-Null

    # 2. Transfer BigLineconnect.Relay.exe in 200KB chunks (FAST)
    $localExePath = "C:\Projev17YD\DUZ_V17_STD\BigLineconnect.Relay\bin\Release\net9.0\win-x64\publish\BigLineconnect.Relay.exe"
    $exeBytes = [System.IO.File]::ReadAllBytes($localExePath)
    $exeB64 = [Convert]::ToBase64String($exeBytes)

    Write-Host "[+] Fast transferring BigLineconnect.Relay.exe ($($exeBytes.Length) bytes, $($exeB64.Length) b64 chars)..."

    $cmdClearExe = $conn.CreateCommand()
    $cmdClearExe.CommandText = "EXEC xp_cmdshell 'powershell -Command ""Remove-Item -Path C:\BigLineconnect.Relay\relay_b64.txt -Force -ErrorAction SilentlyContinue""';"
    try { $cmdClearExe.ExecuteNonQuery() } catch {}

    # 150KB per chunk
    $exeChunkSize = 150000
    $totalChunks = [Math]::Ceiling($exeB64.Length / $exeChunkSize)
    
    for ($i = 0; $i -lt $exeB64.Length; $i += $exeChunkSize) {
        $currentChunkNum = ([Math]::Floor($i / $exeChunkSize)) + 1
        Write-Host "    --> Sending Chunk $currentChunkNum / $totalChunks..."
        $len = [Math]::Min($exeChunkSize, $exeB64.Length - $i)
        $chunk = $exeB64.Substring($i, $len)
        
        $psChunk = $chunk.Replace("'", "''")
        $cmdText = "powershell -Command ""[System.IO.File]::AppendAllText('C:\BigLineconnect.Relay\relay_b64.txt', '$psChunk')"""
        $sqlText = "EXEC xp_cmdshell '" + $cmdText.Replace("'", "''") + "';"
        
        $cmdChunk = $conn.CreateCommand()
        $cmdChunk.CommandText = $sqlText
        $cmdChunk.ExecuteNonQuery() | Out-Null
    }

    Write-Host "[+] Decoding new BigLineconnect.Relay.exe on VDS..."
    $cmdDecodeExe = $conn.CreateCommand()
    $cmdDecodeExe.CommandText = "EXEC xp_cmdshell 'certutil -f -decode C:\BigLineconnect.Relay\relay_b64.txt C:\BigLineconnect.Relay\BigLineconnect.Relay.exe';"
    $readerExe = $cmdDecodeExe.ExecuteReader()
    while ($readerExe.Read()) {
        if (-not $readerExe.IsDBNull(0)) { Write-Host $readerExe.GetString(0) }
    }
    $readerExe.Close()

    # Clean b64 temp file
    $cmdDelTempExe = $conn.CreateCommand()
    $cmdDelTempExe.CommandText = "EXEC xp_cmdshell 'powershell -Command ""Remove-Item -Path C:\BigLineconnect.Relay\relay_b64.txt -Force -ErrorAction SilentlyContinue""';"
    try { $cmdDelTempExe.ExecuteNonQuery() } catch {}

    # 3. Start Windows Service
    Write-Host "[+] Starting BigLineconnectRelaySvc Windows Service on VDS..."
    $cmdStart = $conn.CreateCommand()
    $cmdStart.CommandText = "EXEC xp_cmdshell 'net start BigLineconnectRelaySvc';"
    $readerStart = $cmdStart.ExecuteReader()
    while ($readerStart.Read()) {
        if (-not $readerStart.IsDBNull(0)) { Write-Host $readerStart.GetString(0) }
    }
    $readerStart.Close()

    Write-Host "[+] FAST DEPLOYMENT AND SERVICE START COMPLETED SUCCESSFULLY!"
} catch {
    Write-Host "[-] Fast Deploy Error:" $_.Exception.Message
} finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
}
