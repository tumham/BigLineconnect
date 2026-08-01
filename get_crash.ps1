Get-EventLog -LogName Application -EntryType Error -Newest 10 | Where-Object { $_.TimeGenerated -gt (Get-Date).AddMinutes(-5) } | Format-List
