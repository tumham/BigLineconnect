$file_path = "C:\MIKRODESKTOP_V17\ONLINE_SAYIM_V16\ONLINE_SAYIM_V16\SQL\SAYIM_V16_SP.sql"
$enc = [System.Text.Encoding]::GetEncoding(1254)
$content = [System.IO.File]::ReadAllText($file_path, $enc)

$content = $content -replace "dbo\.A_fn_BedenKod_Bul_Karttan\(sym_Stokkodu\)", "dbo.A_fn_Sym_BedenBul_V12(sym_barkod)"
$content = $content -replace "dbo\.A_fn_RenkKod_Bul_Karttan\(sym_Stokkodu\)", "dbo.A_fn_Sym_RenkBul_V12(sym_barkod)"

[System.IO.File]::WriteAllText($file_path, $content, $enc)
Write-Host "Done"
