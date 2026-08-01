$file_path = "C:\MIKRODESKTOP_V17\ONLINE_SAYIM_V16\ONLINE_SAYIM_V16\SQL\SAYIM_V16_SP.sql"
$enc = [System.Text.Encoding]::GetEncoding(1254)
$content = [System.IO.File]::ReadAllText($file_path, $enc)

$content = $content -replace "dbo\.A_fn_Sym_BedenBul_V12\(sym_barkod\) as Beden", "dbo.A_fn_Sym_BedenBul_V12(sym_barkod) as Desen"

[System.IO.File]::WriteAllText($file_path, $content, $enc)
Write-Host "Done"
