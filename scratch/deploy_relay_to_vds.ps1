$connStr = "Server=213.142.159.18;Database=master;User Id=sa;Password=Bm1453;Encrypt=False;Connection Timeout=10;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
try {
    $conn.Open()
    Write-Host "[+] Connected to SQL Server on 213.142.159.18"

    # Enable xp_cmdshell if needed
    $cmd0 = $conn.CreateCommand()
    $cmd0.CommandText = "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'xp_cmdshell', 1; RECONFIGURE;"
    try { $cmd0.ExecuteNonQuery() } catch {}

    # Find running BigLineconnect.Relay process or service on VDS
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "EXEC xp_cmdshell 'wmic process where ""name like ''%Relay%''"" get ExecutablePath';"
    $reader = $cmd.ExecuteReader()
    Write-Host "--- Relay Process Path on VDS ---"
    while ($reader.Read()) {
        if (-not $reader.IsDBNull(0)) {
            Write-Host $reader.GetString(0)
        }
    }
    $reader.Close()

} catch {
    Write-Host "[-] Error:" $_.Exception.Message
} finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
}
