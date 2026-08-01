$file_path = "C:\MIKRODESKTOP_V17\ONLINE_SAYIM_V16\ONLINE_SAYIM_V16\SQL\SAYIM_V16_SP.sql"
$enc = [System.Text.Encoding]::GetEncoding(1254)
$content = [System.IO.File]::ReadAllText($file_path, $enc)

$select_pattern = "(?s)Select @StokKod=bar_stokkodu,\s*@RenkNo=isnull\(bar_renkpntr,0\),\s*@BedenNo=isnull\(bar_bedenpntr,0\)\s*from dbo\.BARKOD_TANIMLARI\s*where bar_kodu=@Barkod"

$select_replacement = "Declare @VUid1 uniqueidentifier
Declare @VUid2 uniqueidentifier
Declare @VUid3 uniqueidentifier
Declare @VUid4 uniqueidentifier
Declare @VUid5 uniqueidentifier

Select @StokKod=bar_stokkodu,
@RenkNo=isnull(bar_renkpntr,0),
@BedenNo=isnull(bar_bedenpntr,0),
@VUid1=bar_VarBaglantiUId1,
@VUid2=bar_VarBaglantiUId2,
@VUid3=bar_VarBaglantiUId3,
@VUid4=bar_VarBaglantiUId4,
@VUid5=bar_VarBaglantiUId5
from dbo.BARKOD_TANIMLARI
where bar_kodu=@Barkod"

$content = [System.Text.RegularExpressions.Regex]::Replace($content, $select_pattern, $select_replacement, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)


$insert_pattern = "(?s)INSERT INTO dbo\.SAYIM_SONUCLARI\s*\(\s*sym_DBCno, sym_SpecRECno, sym_iptal, sym_fileid,\s*sym_hidden, sym_kilitli, sym_degisti, sym_checksum, sym_create_user, sym_create_date,\s*sym_lastup_user, sym_lastup_date, sym_special1, sym_special2, sym_special3,\s*sym_tarihi, sym_depono, sym_evrakno, sym_satirno, sym_Stokkodu, sym_reyonkodu,\s*sym_koridorkodu, sym_rafkodu, sym_miktar1, sym_miktar2, sym_miktar3, sym_miktar4,\s*sym_miktar5, sym_barkod, sym_renkno, sym_bedenno, sym_parti_kodu, sym_lot_no, sym_serino\)\s*VALUES\s*\(\s*0,\s*0,\s*0,\s*28,\s*0,\s*0,\s*0,\s*0,\s*@UserNo,\s*getdate\(\),\s*@UserNo,getdate\(\),\s*'',\s*'',\s*'',\s*convert\(char\(10\),@Tarih,102\),\s*@DepoNo,\s*@EvrakNo,\s*@SatirNo,\s*@StokKod,\s*@ReyonKod,\s*@KoridorKod,\s*@RafKod,\s*@Miktar1,\s*@Miktar2,@Miktar3,@Miktar4,@Miktar5,\s*@Barkod,\s*@RenkNo,\s*@BedenNo,\s*@PartiKodu,@LotNo,@SeriNo\)"

$insert_replacement = "INSERT INTO dbo.SAYIM_SONUCLARI
(sym_DBCno, sym_SpecRECno, sym_iptal, sym_fileid, 
sym_hidden, sym_kilitli, sym_degisti, sym_checksum, sym_create_user, sym_create_date, 
sym_lastup_user, sym_lastup_date, sym_special1, sym_special2, sym_special3, 
sym_tarihi, sym_depono, sym_evrakno, sym_satirno, sym_Stokkodu, sym_reyonkodu, 
sym_koridorkodu, sym_rafkodu, sym_miktar1, sym_miktar2, sym_miktar3, sym_miktar4, 
sym_miktar5, sym_barkod, sym_renkno, sym_bedenno, sym_parti_kodu, sym_lot_no, sym_serino,
sym_VarBaglantiUId1, sym_VarBaglantiUId2, sym_VarBaglantiUId3, sym_VarBaglantiUId4, sym_VarBaglantiUId5)
VALUES(0, 0, 0, 28, 0, 0, 0, 0, @UserNo, getdate(), @UserNo,getdate(),
'', '', '', convert(char(10),@Tarih,102), @DepoNo, @EvrakNo, @SatirNo, @StokKod, @ReyonKod, @KoridorKod,
@RafKod, @Miktar1, @Miktar2,@Miktar3,@Miktar4,@Miktar5, @Barkod, @RenkNo, @BedenNo,
@PartiKodu,@LotNo,@SeriNo, @VUid1, @VUid2, @VUid3, @VUid4, @VUid5)"

$content = [System.Text.RegularExpressions.Regex]::Replace($content, $insert_pattern, $insert_replacement, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

[System.IO.File]::WriteAllText($file_path, $content, $enc)
Write-Host "Done"
