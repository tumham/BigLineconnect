$file_path = "C:\MIKRODESKTOP_V17\ONLINE_SAYIM_V16\ONLINE_SAYIM_V16\SQL\SAYIM_V16_SP.sql"
$enc = [System.Text.Encoding]::GetEncoding(1254)
$content = [System.IO.File]::ReadAllText($file_path, $enc)

$beden_pattern = "(?s)(CREATE FUNCTION dbo\.A_fn_Sym_BedenBul_V12 \(@Barkod as varchar\(25\)\)\s*RETURNS varchar\(25\)\s*with encryption\s*AS\s*BEGIN\s*)(Return '')(\s*END)"
$beden_replacement = "`$1DECLARE @Sonuc varchar(25) = ''
  SELECT TOP 1 @Sonuc = VK.VaryantKrlm_Isim
  FROM BARKOD_TANIMLARI BT WITH (NOLOCK)
  LEFT JOIN VARYANT_BAGLANTI_TANIMLARI VB WITH (NOLOCK) 
         ON VB.VBag_Guid IN (BT.bar_VarBaglantiUId1, BT.bar_VarBaglantiUId2, BT.bar_VarBaglantiUId3, BT.bar_VarBaglantiUId4, BT.bar_VarBaglantiUId5)
  LEFT JOIN VARYANT_KIRILIM_TANIMLARI VK WITH (NOLOCK) 
         ON VK.VaryantKrlm_Kod = VB.VBag_KirilimKod
  WHERE BT.bar_kodu = @Barkod 
    AND VB.VBag_VaryantKod = 'BEDEN'
  
  RETURN ISNULL(@Sonuc, '')`$3"

$content = [System.Text.RegularExpressions.Regex]::Replace($content, $beden_pattern, $beden_replacement, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

$renk_pattern = "(?s)(CREATE FUNCTION dbo\.A_fn_Sym_RenkBul_V12 \(@Barkod as varchar\(25\)\)\s*RETURNS varchar\(25\)\s*with encryption\s*AS\s*BEGIN\s*)(Return '')(\s*END)"
$renk_replacement = "`$1DECLARE @Sonuc varchar(25) = ''
  SELECT TOP 1 @Sonuc = VK.VaryantKrlm_Isim
  FROM BARKOD_TANIMLARI BT WITH (NOLOCK)
  LEFT JOIN VARYANT_BAGLANTI_TANIMLARI VB WITH (NOLOCK) 
         ON VB.VBag_Guid IN (BT.bar_VarBaglantiUId1, BT.bar_VarBaglantiUId2, BT.bar_VarBaglantiUId3, BT.bar_VarBaglantiUId4, BT.bar_VarBaglantiUId5)
  LEFT JOIN VARYANT_KIRILIM_TANIMLARI VK WITH (NOLOCK) 
         ON VK.VaryantKrlm_Kod = VB.VBag_KirilimKod
  WHERE BT.bar_kodu = @Barkod 
    AND VB.VBag_VaryantKod = 'RENK'
  
  RETURN ISNULL(@Sonuc, '')`$3"

$content = [System.Text.RegularExpressions.Regex]::Replace($content, $renk_pattern, $renk_replacement, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

[System.IO.File]::WriteAllText($file_path, $content, $enc)
Write-Host "Done"
