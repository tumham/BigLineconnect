$lines = Get-Content 'frm_Main.cs'
$newLines = @()
$newLines += $lines[0..26]
$newLines += $lines[624..($lines.Count-1)]
$newLines | Set-Content 'frm_Main.cs'
