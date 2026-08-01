$file_path = "C:\MIKRODESKTOP_V17\ONLINE_SAYIM_V16\ONLINE_SAYIM_V16\SQL\SAYIM_V16_SP.sql"
$enc = [System.Text.Encoding]::GetEncoding(1254)
$content = [System.IO.File]::ReadAllText($file_path, $enc)

$beden_pattern = "(?s)(CREATE FUNCTION dbo\.A_fn_Sym_BedenBul_V12 \(@Barkod as varchar\(25\)\)\s*RETURNS varchar\(25\)\s*with encryption\s*AS\s*BEGIN\s*DECLARE @Sonuc varchar\(25\) = ''\s*SELECT TOP 1 @Sonuc = VK\.VaryantKrlm_Isim\s*FROM BARKOD_TANIMLARI BT WITH \(NOLOCK\)\s*LEFT JOIN VARYANT_BAGLANTI_TANIMLARI VB WITH \(NOLOCK\)\s*ON VB\.VBag_Guid IN \(BT\.bar_VarBaglantiUId1, BT\.bar_VarBaglantiUId2, BT\.bar_VarBaglantiUId3, BT\.bar_VarBaglantiUId4, BT\.bar_VarBaglantiUId5\)\s*LEFT JOIN VARYANT_KIRILIM_TANIMLARI VK WITH \(NOLOCK\)\s*ON VK\.VaryantKrlm_Kod = VB\.VBag_KirilimKod\s*WHERE BT\.bar_kodu = @Barkod\s*AND VB\.VBag_VaryantKod = 'BEDEN'\s*RETURN ISNULL\(@Sonuc, ''\)\s*END)"

$beden_replacement = "CREATE FUNCTION dbo.A_fn_Sym_BedenBul_V12 (@Barkod as varchar(25))
RETURNS varchar(25)  
with encryption
AS
BEGIN
  DECLARE @Sonuc varchar(25) = ''
  SELECT TOP 1 @Sonuc = VK.VaryantKrlm_Isim
  FROM BARKOD_TANIMLARI BT WITH (NOLOCK)
  LEFT JOIN VARYANT_BAGLANTI_TANIMLARI VB WITH (NOLOCK) 
         ON VB.VBag_Guid IN (BT.bar_VarBaglantiUId1, BT.bar_VarBaglantiUId2, BT.bar_VarBaglantiUId3, BT.bar_VarBaglantiUId4, BT.bar_VarBaglantiUId5)
        AND VB.VBag_Tip = 1 -- 1: BEDEN
  LEFT JOIN VARYANT_KIRILIM_TANIMLARI VK WITH (NOLOCK) 
         ON VK.VaryantKrlm_Kod = VB.VBag_KirilimKod 
        AND VK.VaryantKrlm_Tip = 1 -- 1: BEDEN
  WHERE BT.bar_kodu = @Barkod
  
  RETURN ISNULL(@Sonuc, '')
END"

$content = [System.Text.RegularExpressions.Regex]::Replace($content, $beden_pattern, $beden_replacement, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

$renk_pattern = "(?s)(CREATE FUNCTION dbo\.A_fn_Sym_RenkBul_V12 \(@Barkod as varchar\(25\)\)\s*RETURNS varchar\(25\)\s*with encryption\s*AS\s*BEGIN\s*DECLARE @Sonuc varchar\(25\) = ''\s*SELECT TOP 1 @Sonuc = VK\.VaryantKrlm_Isim\s*FROM BARKOD_TANIMLARI BT WITH \(NOLOCK\)\s*LEFT JOIN VARYANT_BAGLANTI_TANIMLARI VB WITH \(NOLOCK\)\s*ON VB\.VBag_Guid IN \(BT\.bar_VarBaglantiUId1, BT\.bar_VarBaglantiUId2, BT\.bar_VarBaglantiUId3, BT\.bar_VarBaglantiUId4, BT\.bar_VarBaglantiUId5\)\s*LEFT JOIN VARYANT_KIRILIM_TANIMLARI VK WITH \(NOLOCK\)\s*ON VK\.VaryantKrlm_Kod = VB\.VBag_KirilimKod\s*WHERE BT\.bar_kodu = @Barkod\s*AND VB\.VBag_VaryantKod = 'RENK'\s*RETURN ISNULL\(@Sonuc, ''\)\s*END)"

$renk_replacement = "CREATE FUNCTION dbo.A_fn_Sym_RenkBul_V12 (@Barkod as varchar(25))
RETURNS varchar(25)  
with encryption
AS
BEGIN
  DECLARE @Sonuc varchar(25) = ''
  SELECT TOP 1 @Sonuc = VK.VaryantKrlm_Isim
  FROM BARKOD_TANIMLARI BT WITH (NOLOCK)
  LEFT JOIN VARYANT_BAGLANTI_TANIMLARI VB WITH (NOLOCK) 
         ON VB.VBag_Guid IN (BT.bar_VarBaglantiUId1, BT.bar_VarBaglantiUId2, BT.bar_VarBaglantiUId3, BT.bar_VarBaglantiUId4, BT.bar_VarBaglantiUId5)
        AND VB.VBag_Tip = 0 -- 0: RENK
  LEFT JOIN VARYANT_KIRILIM_TANIMLARI VK WITH (NOLOCK) 
         ON VK.VaryantKrlm_Kod = VB.VBag_KirilimKod 
        AND VK.VaryantKrlm_Tip = 0 -- 0: RENK
  WHERE BT.bar_kodu = @Barkod
  
  RETURN ISNULL(@Sonuc, '')
END"

$content = [System.Text.RegularExpressions.Regex]::Replace($content, $renk_pattern, $renk_replacement, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

[System.IO.File]::WriteAllText($file_path, $content, $enc)
Write-Host "Done"
