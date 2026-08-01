$connStr = "Server=213.142.159.18;Database=master;User Id=sa;Password=Bm1453;Encrypt=False;Connection Timeout=15;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)

try {
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = "EXEC xp_cmdshell 'powershell -Command ""Get-EventLog -LogName Application -EntryType Error -Newest 3 | Format-List Source, Message""';"
    $reader = $cmd.ExecuteReader()
    Write-Host "--- EXACT APPLICATION ERRORS ---"
    while ($reader.Read()) {
        if (-not $reader.IsDBNull(0)) { Write-Host $reader.GetString(0) }
    }
    $reader.Close()
} catch {
    Write-Host "Err:" $_.Exception.Message
} finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
}
