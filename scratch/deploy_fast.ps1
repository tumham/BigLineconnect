$connStr = "Server=213.142.159.18;Database=master;User Id=sa;Password=Bm1453;Encrypt=False;Connection Timeout=30;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)

function Exec-VdsCommand($sqlConn, $cmdString) {
    $cmd = $sqlConn.CreateCommand()
    $escaped = $cmdString.Replace("'", "''")
    $cmd.CommandText = "EXEC xp_cmdshell '$escaped';"
    $reader = $cmd.ExecuteReader()
    $out = @()
    while ($reader.Read()) {
        if (-not $reader.IsDBNull(0)) { $out += $reader.GetString(0) }
    }
    $reader.Close()
    return $out
}

try {
    $conn.Open()
    Write-Host "[+] Connected to SQL Server on 213.142.159.18"

    # 1. Stop service
    Write-Host "[+] Stopping BigLineconnectRelaySvc..."
    Exec-VdsCommand $conn "net stop BigLineconnectRelaySvc" | Out-Null

    # 2. Transfer admin.html
    $localAdminPath = "C:\Projev17YD\DUZ_V17_STD\BigLineconnect.Relay\wwwroot\admin.html"
    $adminBytes = [System.IO.File]::ReadAllBytes($localAdminPath)
    $adminB64 = [Convert]::ToBase64String($adminBytes)

    Exec-VdsCommand $conn "cmd /c del /f /q C:\BigLineconnect.Relay\wwwroot\admin_b64.txt" | Out-Null

    Write-Host "[+] Transferring admin.html..."
    $chunkSize = 500
    for ($i = 0; $i -lt $adminB64.Length; $i += $chunkSize) {
        $len = [Math]::Min($chunkSize, $adminB64.Length - $i)
        $chunk = $adminB64.Substring($i, $len)
        Exec-VdsCommand $conn "cmd /c echo $chunk >> C:\BigLineconnect.Relay\wwwroot\admin_b64.txt" | Out-Null
    }

    Write-Host "[+] Decoding admin.html..."
    Exec-VdsCommand $conn "certutil -f -decode C:\BigLineconnect.Relay\wwwroot\admin_b64.txt C:\BigLineconnect.Relay\wwwroot\admin.html" | Write-Host

    # 3. Transfer BigLineconnect.Relay.exe
    $localExePath = "C:\Projev17YD\DUZ_V17_STD\BigLineconnect.Relay\bin\Release\net9.0\win-x64\publish\BigLineconnect.Relay.exe"
    $exeBytes = [System.IO.File]::ReadAllBytes($localExePath)
    $exeB64 = [Convert]::ToBase64String($exeBytes)

    Write-Host "[+] Transferring BigLineconnect.Relay.exe ($($exeBytes.Length) bytes)..."
    Exec-VdsCommand $conn "cmd /c del /f /q C:\BigLineconnect.Relay\relay_b64.txt" | Out-Null

    $exeChunkSize = 500
    $totalChunks = [Math]::Ceiling($exeB64.Length / $exeChunkSize)
    $lastReportedPct = -1

    for ($i = 0; $i -lt $exeB64.Length; $i += $exeChunkSize) {
        $pct = [Math]::Floor(($i / $exeB64.Length) * 100)
        if ($pct % 10 -eq 0 -and $pct -ne $lastReportedPct) {
            Write-Host "    --> Progress: $pct%"
            $lastReportedPct = $pct
        }
        $len = [Math]::Min($exeChunkSize, $exeB64.Length - $i)
        $chunk = $exeB64.Substring($i, $len)
        Exec-VdsCommand $conn "cmd /c echo $chunk >> C:\BigLineconnect.Relay\relay_b64.txt" | Out-Null
    }

    Write-Host "[+] Decoding BigLineconnect.Relay.exe..."
    Exec-VdsCommand $conn "certutil -f -decode C:\BigLineconnect.Relay\relay_b64.txt C:\BigLineconnect.Relay\BigLineconnect.Relay.exe" | Write-Host

    Exec-VdsCommand $conn "cmd /c del /f /q C:\BigLineconnect.Relay\relay_b64.txt" | Out-Null

    # 4. Start Windows Service
    Write-Host "[+] Starting BigLineconnectRelaySvc Windows Service..."
    Exec-VdsCommand $conn "net start BigLineconnectRelaySvc" | Write-Host

    Write-Host "[+] DEPLOYMENT COMPLETED SUCCESSFULLY!"
} catch {
    Write-Host "[-] Deploy Error:" $_.Exception.Message
} finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
}
