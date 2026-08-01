If exists(select name from sysobjects where name='cihaz_insert')
	DROP Procedure cihaz_insert
GO
CREATE Procedure dbo.cihaz_insert(
@cihaz as varchar(25),
@StokKod as varchar(25))
--with encryption
AS
Declare @RecNo as int
BEGIN

INSERT INTO [dbo].[STOK_SERINO_TANIMLARI]([chz_DBCno], [chz_Spec_Rec_no], [chz_iptal],
 [chz_fileid], [chz_hidden], [chz_kilitli], [chz_degisti], [chz_checksum], 
[chz_create_user], [chz_create_date],
[chz_lastup_user], [chz_lastup_date], 
[chz_special1], [chz_special2], [chz_special3], [chz_serino], [chz_stok_kodu], [chz_Tuktckodu], 
[chz_GrnBasTarihi], [chz_GrnBitTarihi],
 [chz_aciklama1], [chz_aciklama2],
 [chz_al_tarih], [chz_al_evr_seri], [chz_al_evr_sira], [chz_al_cari_kodu], [chz_st_tarih], [chz_st_evr_seri], [chz_st_evr_sira], [chz_st_cari_kodu], [chz_al_fiati_ana], [chz_al_fiati_alt], [chz_al_fiati_orj], [chz_st_fiati_ana], [chz_st_fiati_alt], [chz_st_fiati_orj])
VALUES( 0,0,0,94,0,0,'',0,						
1,getdate(),		
1,getdate(),
'','','',@cihaz,@StokKod,'','1899-12-31 00:00:00.000',
'1899-12-31 00:00:00.000','','',
'1899-12-31 00:00:00.000','',0,'',
'1899-12-31 00:00:00.000','',0,'',
0,0,0,0,0,0)


end

GO

If exists(select name from sysobjects where name='URETIM_MALZEME_PLANLAMA_BGS')
	DROP View URETIM_MALZEME_PLANLAMA_BGS
GO
CREATE VIEW [dbo].[URETIM_MALZEME_PLANLAMA_BGS]
AS
SELECT TOP 100 PERCENT
upl_Guid AS [KayitNo] /* KAYIT NO */ ,
upl_isemri AS [IsEmriKodu] /* ÝÞ EMRÝ KODU */ ,
upl_uretim_tuket AS [HTip],
CASE
WHEN upl_uretim_tuket=0 THEN dbo.fn_GetResource('E',0818,DEFAULT)
ELSE dbo.fn_GetResource('E',0816,DEFAULT)
END AS [HTipAdi] /* HAREKET TÝP */ ,
upl_kodu AS [StokKod] /* ÜRETÝLECEK ÜRÜN KODU */ ,
sto_isim AS [StokAdi] /* ÜRETÝLECEK ÜRÜN ÝSMÝ */ ,
sto_anagrup_kod AS [StokAnaGrup] /* ANA GRUP KODU */ ,
sto_altgrup_kod AS [StokAltGrup] /* ALT GRUP KODU */ ,
sto_kategori_kodu AS [StokKategori] /* KATEGORÝ KODU */ ,
upl_parti_kod AS [Parti] /* PARTÝ KODU */ ,
upl_lotno AS [Lot] /* LOT NO */ ,
upl_miktar AS [PMiktar] /* PLANLANAN */ ,
CASE
WHEN upl_uretim_tuket=1 THEN ish_uret_miktar - ish_uretiade_miktar
ELSE ish_sevk_miktar
END AS [GMiktar] /* GERÇEKLEÞEN */ ,
upl_miktar - CASE
WHEN upl_uretim_tuket=1 THEN ish_uret_miktar - ish_uretiade_miktar
ELSE ish_sevk_miktar
END
AS [KMiktar] /* KALAN */
From (
select
upl_Guid,
upl_isemri,
upl_uretim_tuket,
upl_kodu,
sto_isim,
sto_anagrup_kod,
sto_altgrup_kod,
sto_kategori_kodu,
upl_parti_kod,
upl_lotno,
upl_miktar,
(Select isnull(SUM(ish_uret_miktar), 0)     From dbo.ISEMRI_MALZEME_DURUMLARI where (ish_isemri=upl_isemri) and (ish_stokhizm_gid_kod=upl_kodu) and (ish_stok_hizm_gider = 0)) AS [ish_uret_miktar],
(Select isnull(SUM(ish_uretiade_miktar), 0) From dbo.ISEMRI_MALZEME_DURUMLARI where (ish_isemri=upl_isemri) and (ish_stokhizm_gid_kod=upl_kodu) and (ish_stok_hizm_gider = 0)) AS [ish_uretiade_miktar],
(Select isnull(SUM(ish_sevk_miktar), 0)     From dbo.ISEMRI_MALZEME_DURUMLARI where (ish_isemri=upl_isemri) and (ish_stokhizm_gid_kod=upl_kodu) and (ish_stok_hizm_gider = 0)) AS [ish_sevk_miktar]
FROM dbo.URETIM_MALZEME_PLANLAMA
LEFT OUTER JOIN dbo.STOKLAR ON (sto_kod=upl_kodu)
) UPC4
GO




IF EXISTS (
SELECT * FROM   sysobjects 
WHERE  name = 'A_fn_StokYIsmi')
DROP FUNCTION A_fn_StokYIsmi
GO

CREATE FUNCTION dbo.A_fn_StokYIsmi (@StokKod as varchar(25))
RETURNS varchar(50) 
with encryption
AS
BEGIN
Declare @Sonuc as varchar(50)

Select @Sonuc=isnull(sto_yabanci_isim,'') 
from dbo.STOKLAR  
WHERE sto_kod=@StokKod

Return @Sonuc

END
GO

If exists(select name from sysobjects where name='A_sp_SK_GetPrintData')
	DROP Procedure A_sp_SK_GetPrintData
GO

Create Procedure dbo.A_sp_SK_GetPrintData(
@No as integer,
@SeriNo as varchar(6),
@SiraNo as int)
with encryption
as
BEGIN
Declare @tip as tinyint
Declare @cins as tinyint
Declare @evtip as tinyint

IF @No=1 -- TAÝ
BEGIN
Set @tip=0
Set @cins=0
Set @evtip=13
END
ELSE IF @No=2  --TSÝ
BEGIN
Set @tip=1
Set @cins=0
Set @evtip=1
END
ELSE IF @No=3 --ATSF
BEGIN
Set @tip=1
Set @cins=0
Set @evtip=4
END
ELSE IF @No=16 -- ATAF
BEGIN
SET @tip=0
SET @cins=0
SET @evtip=3
END
ELSE IF @No=4 --DASF
BEGIN
Set @tip=2
Set @cins=6
Set @evtip=2
END
ELSE IF @No=5 --II -- Ýthalat Ýrsaliyesi
BEGIN
Set @tip=0
Set @cins=12
Set @evtip=13
END
ELSE IF @No=6 --II -- Ýhracat Ýrsaliyesi
BEGIN
Set @tip=1
Set @cins=12
Set @evtip=1
END


Select sth_stok_kod as sk,dbo.fn_StokIsmi(sth_stok_kod) as s,
dbo.A_fn_StokYIsmi(sth_stok_kod) as yi,
dbo.fn_StokBirimi(sth_stok_kod,1) as b,sth_miktar as m,
sth_tutar as t,
sth_iskonto1 as i1,
sth_iskonto2 as i2,
sth_iskonto3 as i3,
sth_iskonto4 as i4,
sth_iskonto5 as i5,
sth_iskonto6 as i6,
sth_masraf1 as m1,
sth_masraf2 as m2,
sth_masraf3 as m3,
sth_masraf4 as m4,
sth_vergi as v,
sth_masraf_vergi as mv,
dbo.fn_StokSatisFiyati(sth_stok_kod,2,0,1) as f2
from dbo.STOK_HAREKETLERI
where sth_tip=@tip and
sth_cins=@cins and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo


END
GO

IF EXISTS (
SELECT * FROM   sysobjects 
WHERE  name = 'A_fn_B_StokKoduBul')
DROP FUNCTION A_fn_B_StokKoduBul
GO

CREATE FUNCTION dbo.A_fn_B_StokKoduBul (@BKod as varchar(25))
RETURNS varchar(25) 
with encryption
AS
BEGIN
  Declare @Sonuc as varchar(25)
  Select @Sonuc=bar_stokkodu from dbo.BARKOD_TANIMLARI  
  WHERE bar_kodu=@BKod
  IF @Sonuc is NULL SET @Sonuc = ''
  Return @Sonuc
END
GO

If exists(select name from sysobjects where name='A_sp_B_DetayTakipTipiBul')
	DROP Procedure A_sp_B_DetayTakipTipiBul
GO

Create Procedure A_sp_B_DetayTakipTipiBul
@Barkod as varchar(25)
with encryption
as

Select sto_detay_takip from dbo.STOKLAR 
where sto_kod=dbo.A_fn_B_StokKoduBul (@Barkod)
GO

If exists(select name from sysobjects where name='A_sp_SK_SiraNoBul')
	DROP Procedure A_sp_SK_SiraNoBul
GO

Create Procedure dbo.A_sp_SK_SiraNoBul(
@No as integer,
@SeriNo as varchar(6))
with encryption
as
BEGIN
Declare @tip as tinyint
Declare @cins as tinyint
Declare @evtip as tinyint

IF @No=1 -- TAÝ
BEGIN
Set @tip=0
Set @cins=0
Set @evtip=13
END
ELSE IF @No=2  --TSÝ
BEGIN
Set @tip=1
Set @cins=0
Set @evtip=1
END
ELSE IF @No=3 --ATSF
BEGIN
Set @tip=1
Set @cins=0
Set @evtip=4
END
ELSE IF @No=4 --DASF
BEGIN
Set @tip=2
Set @cins=6
Set @evtip=2
END
ELSE IF @No=5 --II -- Ýthalat Ýrsaliyesi
BEGIN
Set @tip=0
Set @cins=12
Set @evtip=13
END
ELSE IF @No=6 -- Ýhracat Ýrsaliyesi
BEGIN
Set @tip=1
Set @cins=12
Set @evtip=1
END
ELSE IF @No=16 --ATAF
BEGIN
Set @tip=1
Set @cins=0
Set @evtip=4
END
ELSE IF @No=11 --KSI
BEGIN
Set @tip=1
Set @evtip=1
END
ELSE IF @No=12 --UCF
BEGIN
Set @tip=1
Set @cins=7
Set @evtip=0
END

IF @No=1 or @No=2 or @No=4 or @No=5 or @No=6 or @No=12
BEGIN
Select isnull(max(sth_evrakno_sira),0)+1 
from dbo.STOK_HAREKETLERI
where sth_tip=@tip and
--sth_cins=@cins and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo
END
ELSE IF @No=3
BEGIN
Select isnull(MAX(cha_evrakno_sira),0) + 1 FROM 
CARI_HESAP_HAREKETLERI  WITH (NOLOCK) 
WHERE (cha_evrak_tip=63) AND (cha_evrakno_seri=@SeriNo)
END
ELSE IF @No=16
BEGIN
Select isnull(MAX(cha_evrakno_sira),0) + 1 FROM 
CARI_HESAP_HAREKETLERI  WITH (NOLOCK) 
WHERE (cha_evrak_tip=0) AND (cha_evrakno_seri=@SeriNo)
END
ELSE IF @No=11
BEGIN
Select isnull(MAX(kon_evrakno_sira),0) + 1 FROM 
KONSINYE_HAREKETLERI WITH (NOLOCK) 
WHERE (kon_tip=@tip) AND (kon_evraktip=@evtip) AND (kon_evrakno_seri=@SeriNo) AND (kon_normal_iade=0)
END

END
GO

--Renk Bul
IF EXISTS (
SELECT * FROM   sysobjects 
WHERE  name = 'A_S_fn_RenkBul')
DROP FUNCTION A_S_fn_RenkBul
GO

CREATE FUNCTION dbo.A_S_fn_RenkBul ( 
@BedenNo as smallint,
@StokKod as varchar(25))
RETURNS varchar(10) 
with encryption
AS
BEGIN
Declare @r as integer
Declare @b as integer
SET @r = @BedenNo / 40
SET @b = (@BedenNo - (@r * 40))

IF @r<>0 and @b<>0
set @r=@r+1
IF @r = 0
Set @r=1

Declare @Renk as varchar(10)
Declare @RenkKodu as varchar(25)
Select @RenkKodu=sto_varyant_kod_arr1
from dbo.STOKLAR
where sto_kod=@StokKod


IF @r=1
Select @Renk=rnk_kirilim_1 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=2
Select @Renk=rnk_kirilim_2 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=3
Select @Renk=rnk_kirilim_3 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=4
Select @Renk=rnk_kirilim_4 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=5
Select @Renk=rnk_kirilim_5 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=6
Select @Renk=rnk_kirilim_6 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=7
Select @Renk=rnk_kirilim_7 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=8
Select @Renk=rnk_kirilim_8 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=9
Select @Renk=rnk_kirilim_9 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=10
Select @Renk=rnk_kirilim_10 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=11
Select @Renk=rnk_kirilim_11 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=12
Select @Renk=rnk_kirilim_12 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=13
Select @Renk=rnk_kirilim_13 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=14
Select @Renk=rnk_kirilim_14 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=15
Select @Renk=rnk_kirilim_15 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=16
Select @Renk=rnk_kirilim_16 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=17
Select @Renk=rnk_kirilim_17 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=18
Select @Renk=rnk_kirilim_18 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=19
Select @Renk=rnk_kirilim_19 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=20
Select @Renk=rnk_kirilim_20 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=21
Select @Renk=rnk_kirilim_21 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=22
Select @Renk=rnk_kirilim_22 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=23
Select @Renk=rnk_kirilim_23 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=24
Select @Renk=rnk_kirilim_24 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=25
Select @Renk=rnk_kirilim_25 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=26
Select @Renk=rnk_kirilim_26 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=27
Select @Renk=rnk_kirilim_27 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=28
Select @Renk=rnk_kirilim_28 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=29
Select @Renk=rnk_kirilim_29 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=30
Select @Renk=rnk_kirilim_30 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=31
Select @Renk=rnk_kirilim_31 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=32
Select @Renk=rnk_kirilim_32 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=33
Select @Renk=rnk_kirilim_33 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=34
Select @Renk=rnk_kirilim_34 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=35
Select @Renk=rnk_kirilim_35 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=36
Select @Renk=rnk_kirilim_36 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=37
Select @Renk=rnk_kirilim_37 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=38
Select @Renk=rnk_kirilim_38 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=39
Select @Renk=rnk_kirilim_39 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=40
Select @Renk=rnk_kirilim_40 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=41
Select @Renk=rnk_kirilim_41 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=42
Select @Renk=rnk_kirilim_42 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=43
Select @Renk=rnk_kirilim_43 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=44
Select @Renk=rnk_kirilim_44 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=45
Select @Renk=rnk_kirilim_45 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=46
Select @Renk=rnk_kirilim_46 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=47
Select @Renk=rnk_kirilim_47 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=48
Select @Renk=rnk_kirilim_48 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=49
Select @Renk=rnk_kirilim_49 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=50
Select @Renk=rnk_kirilim_50 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=51
Select @Renk=rnk_kirilim_51 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=52
Select @Renk=rnk_kirilim_52 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=53
Select @Renk=rnk_kirilim_53 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=54
Select @Renk=rnk_kirilim_54 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=55
Select @Renk=rnk_kirilim_55 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=56
Select @Renk=rnk_kirilim_56 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=57
Select @Renk=rnk_kirilim_57 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=58
Select @Renk=rnk_kirilim_58 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=59
Select @Renk=rnk_kirilim_59 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
ELSE IF @r=60
Select @Renk=rnk_kirilim_60 from dbo.STOK_RENK_TANIMLARI where rnk_kodu=@RenkKodu
  IF @Renk is null set @Renk=''
  Return @Renk
END
GO

--Beden Bul
IF EXISTS (
SELECT * FROM   sysobjects 
WHERE  name = 'A_S_fn_BedenBul')
DROP FUNCTION A_S_fn_BedenBul
GO

CREATE FUNCTION dbo.A_S_fn_BedenBul ( 
@BedenNo as smallint,
@StokKod as varchar(25))
RETURNS varchar(10) 
with encryption
AS
BEGIN

Declare @r as integer
Declare @b as integer
SET @r = @BedenNo / 40
SET @b = (@BedenNo - (@r * 40))

Declare @Beden as varchar(10)
Declare @BedenKodu as varchar(25)
Select @BedenKodu=sto_varyant_kod_arr2
from dbo.STOKLAR
where sto_kod=@StokKod

IF @b=1
Select @Beden=bdn_kirilim_1 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=2
Select @Beden=bdn_kirilim_2 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=3
Select @Beden=bdn_kirilim_3 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=4
Select @Beden=bdn_kirilim_4 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=5
Select @Beden=bdn_kirilim_5 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=6
Select @Beden=bdn_kirilim_6 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=7
Select @Beden=bdn_kirilim_7 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=8
Select @Beden=bdn_kirilim_8 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=9
Select @Beden=bdn_kirilim_9 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=10
Select @Beden=bdn_kirilim_10 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=11
Select @Beden=bdn_kirilim_11 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=12
Select @Beden=bdn_kirilim_12 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=13
Select @Beden=bdn_kirilim_13 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=14
Select @Beden=bdn_kirilim_14 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=15
Select @Beden=bdn_kirilim_15 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=16
Select @Beden=bdn_kirilim_16 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=17
Select @Beden=bdn_kirilim_17 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=18
Select @Beden=bdn_kirilim_18 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=19
Select @Beden=bdn_kirilim_19 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=20
Select @Beden=bdn_kirilim_20 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=21
Select @Beden=bdn_kirilim_21 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=22
Select @Beden=bdn_kirilim_22 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=23
Select @Beden=bdn_kirilim_23 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=24
Select @Beden=bdn_kirilim_24 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=25
Select @Beden=bdn_kirilim_25 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=26
Select @Beden=bdn_kirilim_26 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=27
Select @Beden=bdn_kirilim_27 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=28
Select @Beden=bdn_kirilim_28 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=29
Select @Beden=bdn_kirilim_29 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=30
Select @Beden=bdn_kirilim_30 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=31
Select @Beden=bdn_kirilim_31 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=32
Select @Beden=bdn_kirilim_32 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=33
Select @Beden=bdn_kirilim_33 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=34
Select @Beden=bdn_kirilim_34 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=35
Select @Beden=bdn_kirilim_35 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=36
Select @Beden=bdn_kirilim_36 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=37
Select @Beden=bdn_kirilim_37 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=38
Select @Beden=bdn_kirilim_38 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=39
Select @Beden=bdn_kirilim_39 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu
ELSE IF @b=40
Select @Beden=bdn_kirilim_40 from dbo.STOK_BEDEN_TANIMLARI where bdn_kodu=@BedenKodu

  IF @Beden is null set @Beden=''
  Return @Beden
END
GO

IF EXISTS (
SELECT * FROM   sysobjects 
WHERE  name = 'A_fn_RenkKod_Bul')
DROP FUNCTION A_fn_RenkKod_Bul
GO

CREATE FUNCTION dbo.A_fn_RenkKod_Bul(
@StokKod as varchar(25)
)
RETURNS varchar(25) 
with encryption
AS
BEGIN
Declare @sonuc as varchar(25)

Select @sonuc=sto_varyant_kod_arr1
from dbo.STOKLAR 
where sto_kod=@StokKod

if @sonuc is null set @sonuc=''
Return @sonuc
END
GO

IF EXISTS (
SELECT * FROM   sysobjects 
WHERE  name = 'A_fn_BedenKod_Bul')
DROP FUNCTION A_fn_BedenKod_Bul
GO

CREATE FUNCTION dbo.A_fn_BedenKod_Bul(
@StokKod as varchar(25)
)
RETURNS varchar(25) 
with encryption
AS
BEGIN
Declare @sonuc as varchar(25)

Select @sonuc=sto_varyant_kod_arr2
from dbo.STOKLAR 
where sto_kod=@StokKod

if @sonuc is null set @sonuc=''
Return @sonuc
END
GO

IF EXISTS (
SELECT * FROM   sysobjects 
WHERE  name = 'A_fn_RenkDetay_Bul')
DROP FUNCTION A_fn_RenkDetay_Bul
GO

CREATE FUNCTION dbo.A_fn_RenkDetay_Bul(
@StokKod as varchar(25)
)
RETURNS bit 
with encryption
AS
BEGIN
Declare @sonuc as bit
Set @sonuc=0
Select @sonuc=sto_varyant_detayli_fl1 
from dbo.STOKLAR 
where sto_kod=@StokKod

Return @sonuc
END
GO

IF EXISTS (
SELECT * FROM   sysobjects 
WHERE  name = 'A_fn_BedenDetay_Bul')
DROP FUNCTION A_fn_BedenDetay_Bul
GO

CREATE FUNCTION dbo.A_fn_BedenDetay_Bul(
@StokKod as varchar(25)
)
RETURNS bit 
with encryption
AS
BEGIN
Declare @sonuc as bit
Set @sonuc=0
Select @sonuc=sto_varyant_detayli_fl2 
from dbo.STOKLAR 
where sto_kod=@StokKod

Return @sonuc
END
GO

IF EXISTS (
SELECT * FROM   sysobjects 
WHERE  name = 'A_fn_DetayTakipTipi_Bul')
DROP FUNCTION A_fn_DetayTakipTipi_Bul
GO

CREATE FUNCTION dbo.A_fn_DetayTakipTipi_Bul(
@StokKod as varchar(25)
)
RETURNS int 
with encryption
AS
BEGIN
Declare @sonuc as integer
Set @sonuc=0
Select @sonuc=sto_detay_takip 
from dbo.STOKLAR 
where sto_kod=@StokKod

Return @sonuc
END
GO

IF EXISTS (
SELECT * FROM   sysobjects 
WHERE  name = 'A_fn_StokIsmi_Bul')
DROP FUNCTION A_fn_StokIsmi_Bul
GO

CREATE FUNCTION dbo.A_fn_StokIsmi_Bul (@StokKod as varchar(25))
RETURNS varchar(50) 
with encryption
AS
BEGIN
Declare @Sonuc as varchar(50)

Select @Sonuc=isnull(sto_isim,'') 
from dbo.STOKLAR  
WHERE sto_kod=@StokKod

Return @Sonuc

END
GO

IF EXISTS (
SELECT * FROM   sysobjects 
WHERE  name = 'A_fn_B_RenkBul')
DROP FUNCTION A_fn_B_RenkBul
GO

CREATE FUNCTION dbo.A_fn_B_RenkBul (@BKod as varchar(25))
RETURNS varchar(25)  
with encryption
AS
BEGIN
  Declare @stokod as varchar(25)	--Barkod tablosundan oku
  Declare @renkpntr as tinyint		--Barkod tablosundan oku
  Declare @renkDetayli as bit		--Stok tablosundan oku
  Declare @renk_kodu as varchar(25)	--Stok tablosundan oku
  Declare @Sonuc as varchar(25)  	--Stok Rengi
  Declare @sayi as tinyint
Set @renkDetayli=0
--Barkoddan stok koduna ulaþýlýr.
 Select @stokod=bar_stokkodu,@renkpntr=bar_renkpntr from dbo.BARKOD_TANIMLARI 
WHERE bar_kodu=@BKod

IF  @stokod is null
set @Sonuc=''

ELSE IF @stokod is not NULL  
  	Select  @renkDetayli=sto_varyant_detayli_fl1 ,@renk_kodu =sto_varyant_kod_arr1  from dbo.STOKLAR   where sto_kod=@stokod
	IF @renkDetayli=0 SET @Sonuc=''
	ELSE IF @renkDetayli=1
	SET @Sonuc = (Select dbo.fn_renk_kirilimi(@renkpntr,@renk_kodu)  from dbo.STOK_RENK_TANIMLARI 
	WHERE  rnk_kodu=@renk_kodu)
Return @Sonuc
END
GO

IF EXISTS (
SELECT * FROM   sysobjects 
WHERE  name = 'A_fn_B_BedenBul')
DROP FUNCTION A_fn_B_BedenBul
GO

CREATE FUNCTION dbo.A_fn_B_BedenBul (@BKod as varchar(25))
RETURNS varchar(25)  
with encryption
AS
BEGIN
  Declare @stokod as varchar(25)	--Barkod tablosundan oku
  Declare @bedenpntr as tinyint		--Barkod tablosundan oku
  Declare @bedenli_Takip as bit		--Stok tablosundan oku
  Declare @beden_kodu as varchar(25)	--Stok tablosundan oku
  Declare @Sonuc as varchar(25) 	--Stok Bedeni
Set @bedenli_Takip=0
--Barkoddan stok koduna ulaþýlýr.
  Select @stokod=bar_stokkodu,@bedenpntr =bar_bedenpntr from dbo.BARKOD_TANIMLARI 
WHERE bar_kodu=@BKod
  
 IF @stokod is NULL SET @Sonuc = ''
 ELSE IF @stokod is not NULL 
  	Select @bedenli_Takip=sto_varyant_detayli_fl2,@beden_kodu =sto_varyant_kod_arr2 from dbo.STOKLAR   
	where sto_kod=@stokod
	IF @bedenli_Takip=0 SET @Sonuc=''
	ELSE IF @bedenli_Takip=1
	SET @Sonuc = (Select dbo.fn_beden_kirilimi (@bedenpntr,@beden_kodu)  from dbo.STOK_BEDEN_TANIMLARI
	WHERE  bdn_kodu=@beden_kodu)
Return @Sonuc
END
GO 

--dbo.BEDEN_HAREKETLERI Miktar Getir
If exists(select name from sysobjects where name='A_sp_SK_BedenHar_DataGetir')
	DROP Procedure A_sp_SK_BedenHar_DataGetir
GO

CREATE PROCEDURE A_sp_SK_BedenHar_DataGetir 
@RecNo as uniqueidentifier,
@StokKod as varchar(25),
@sip_tip as int
with encryption 
AS
BEGIN
if @sip_tip=2
BEGIN
Select dbo.A_S_fn_RenkBul (BdnHar_BedenNo ,@StokKod) as Renk,
dbo.A_S_fn_BedenBul (BdnHar_BedenNo ,@StokKod) as Beden,
BdnHar_BedenNo as BedenNo,
BdnHar_HarGor as Miktar
from dbo.BEDEN_HAREKETLERI
where BdnHar_Tipi=1 and 
BdnHar_Har_uid=@RecNo 
END
ELSE
BEGIN
Select dbo.A_S_fn_RenkBul (BdnHar_BedenNo ,@StokKod) as Renk,
dbo.A_S_fn_BedenBul (BdnHar_BedenNo ,@StokKod) as Beden,
BdnHar_BedenNo as BedenNo,
BdnHar_HarGor as Miktar
from dbo.BEDEN_HAREKETLERI
where BdnHar_Tipi=9 and 
BdnHar_Har_uid=@RecNo 
END

END
GO

If exists(select name from sysobjects where name='A_sp_SK_Sip_DataGetir_V11')
	DROP Procedure A_sp_SK_Sip_DataGetir_V11
GO

CREATE Procedure dbo.A_sp_SK_Sip_DataGetir_V11(
@sip_tip as tinyint,
@SeriNo as varchar(6),
@SiraNo as integer,
@BelgeNo as varchar(15),
@CariKod as varchar(25),
@CariyeGore as bit)
with encryption
AS
BEGIN
IF @sip_tip=2
BEGIN
Select Cast(ssip_Guid as varchar(50)) as rn,
ssip_satirno as sn,
ssip_stok_kod as Kod,
dbo.A_fn_StokIsmi_Bul(ssip_stok_kod) as Stok,
ssip_miktar as Miktar,
ssip_teslim_miktar as Teslim,
'' as PartiKod,
0 as LotNo,
--0 as dtt,
--0 as rd,
--'' as rk,
--0 as bd,
--'' as bk
dbo.A_fn_DetayTakipTipi_Bul(ssip_stok_kod) as dtt,
dbo.A_fn_RenkDetay_Bul(ssip_stok_kod) as rd,
dbo.A_fn_RenkKod_Bul(ssip_stok_kod) as rk,
dbo.A_fn_BedenDetay_Bul(ssip_stok_kod) as bd,
dbo.A_fn_BedenKod_Bul(ssip_stok_kod) as bk
from dbo.DEPOLAR_ARASI_SIPARISLER
where 
ssip_kapat_fl = 0 and
ssip_evrakno_seri=@SeriNo and
ssip_evrakno_sira=@SiraNo 
END
ELSE IF @sip_tip=3
BEGIN
if @CariyeGore=1
Begin
	Select Cast(sip_Guid as varchar(50)) as rn,
	sip_satirno as sn,
	sip_stok_kod as Kod,
	dbo.A_fn_StokIsmi_Bul(sip_stok_kod) as Stok,
	sip_miktar as Miktar,
	sip_teslim_miktar as Teslim,
	sip_parti_kodu as PartiKod,
	sip_lot_no as LotNo,
	dbo.A_fn_DetayTakipTipi_Bul(sip_stok_kod) as dtt,
	dbo.A_fn_RenkDetay_Bul(sip_stok_kod) as rd,
	dbo.A_fn_RenkKod_Bul(sip_stok_kod) as rk,
	dbo.A_fn_BedenDetay_Bul(sip_stok_kod) as bd,
	dbo.A_fn_BedenKod_Bul(sip_stok_kod) as bk
	from dbo.SIPARISLER 
	where sip_tip=1 and 
	sip_cins=3 and 
	sip_OnaylayanKulNo > 0 and 
	sip_kapat_fl = 0 and
	sip_musteri_kod=@CariKod

End
Else
Begin
	Select Cast(sip_Guid as varchar(50)) as rn,
	sip_satirno as sn,
	sip_stok_kod as Kod,
	dbo.A_fn_StokIsmi_Bul(sip_stok_kod) as Stok,
	sip_miktar as Miktar,
	sip_teslim_miktar as Teslim,
	sip_parti_kodu as PartiKod,
	sip_lot_no as LotNo,
	dbo.A_fn_DetayTakipTipi_Bul(sip_stok_kod) as dtt,
	dbo.A_fn_RenkDetay_Bul(sip_stok_kod) as rd,
	dbo.A_fn_RenkKod_Bul(sip_stok_kod) as rk,
	dbo.A_fn_BedenDetay_Bul(sip_stok_kod) as bd,
	dbo.A_fn_BedenKod_Bul(sip_stok_kod) as bk
	from dbo.SIPARISLER 
	where sip_tip=1 and 
	sip_cins=3 and 
	sip_OnaylayanKulNo > 0 and 
	sip_kapat_fl = 0 and
	sip_evrakno_seri=@SeriNo and
	sip_evrakno_sira=@SiraNo 

End

END
ELSE IF @sip_tip=4
BEGIN
if @CariyeGore=1
Begin
	Select Cast(sip_Guid as varchar(50)) as rn,
	sip_satirno as sn,
	sip_stok_kod as Kod,
	dbo.A_fn_StokIsmi_Bul(sip_stok_kod) as Stok,
	sip_miktar as Miktar,
	sip_teslim_miktar as Teslim,
	sip_parti_kodu as PartiKod,
	sip_lot_no as LotNo,
	dbo.A_fn_DetayTakipTipi_Bul(sip_stok_kod) as dtt,
	dbo.A_fn_RenkDetay_Bul(sip_stok_kod) as rd,
	dbo.A_fn_RenkKod_Bul(sip_stok_kod) as rk,
	dbo.A_fn_BedenDetay_Bul(sip_stok_kod) as bd,
	dbo.A_fn_BedenKod_Bul(sip_stok_kod) as bk
	from dbo.SIPARISLER 
	where sip_tip=0 and 
	sip_cins=3 and 
	sip_OnaylayanKulNo > 0 and 
	sip_kapat_fl = 0 and
	sip_musteri_kod=@CariKod

End
Else
Begin
	Select Cast(sip_Guid as varchar(50)) as rn,
	sip_satirno as sn,
	sip_stok_kod as Kod,
	dbo.A_fn_StokIsmi_Bul(sip_stok_kod) as Stok,
	sip_miktar as Miktar,
	sip_teslim_miktar as Teslim,
	sip_parti_kodu as PartiKod,
	sip_lot_no as LotNo,
	dbo.A_fn_DetayTakipTipi_Bul(sip_stok_kod) as dtt,
	dbo.A_fn_RenkDetay_Bul(sip_stok_kod) as rd,
	dbo.A_fn_RenkKod_Bul(sip_stok_kod) as rk,
	dbo.A_fn_BedenDetay_Bul(sip_stok_kod) as bd,
	dbo.A_fn_BedenKod_Bul(sip_stok_kod) as bk
	from dbo.SIPARISLER 
	where sip_tip=0 and 
	sip_cins=3 and 
	sip_OnaylayanKulNo > 0 and 
	sip_kapat_fl = 0 and
	sip_evrakno_seri=@SeriNo and
	sip_evrakno_sira=@SiraNo 

End

END
ELSE IF @sip_tip=5
BEGIN
if @CariyeGore=1
Begin
	Select Cast(sip_Guid as varchar(50)) as rn,
	sip_satirno as sn,
	sip_stok_kod as Kod,
	dbo.A_fn_StokIsmi_Bul(sip_stok_kod) as Stok,
	sip_miktar as Miktar,
	sip_teslim_miktar as Teslim,
	sip_parti_kodu as PartiKod,
	sip_lot_no as LotNo,
	dbo.A_fn_DetayTakipTipi_Bul(sip_stok_kod) as dtt,
	dbo.A_fn_RenkDetay_Bul(sip_stok_kod) as rd,
	dbo.A_fn_RenkKod_Bul(sip_stok_kod) as rk,
	dbo.A_fn_BedenDetay_Bul(sip_stok_kod) as bd,
	dbo.A_fn_BedenKod_Bul(sip_stok_kod) as bk
	from dbo.SIPARISLER 
	where sip_tip=0 and 
	sip_cins=1 and 
	sip_OnaylayanKulNo > 0 and 
	sip_kapat_fl = 0 and
	sip_musteri_kod=@CariKod

End
Else
Begin
	Select Cast(sip_Guid as varchar(50)) as rn,
	sip_satirno as sn,
	sip_stok_kod as Kod,
	dbo.A_fn_StokIsmi_Bul(sip_stok_kod) as Stok,
	sip_miktar as Miktar,
	sip_teslim_miktar as Teslim,
	sip_parti_kodu as PartiKod,
	sip_lot_no as LotNo,
	dbo.A_fn_DetayTakipTipi_Bul(sip_stok_kod) as dtt,
	dbo.A_fn_RenkDetay_Bul(sip_stok_kod) as rd,
	dbo.A_fn_RenkKod_Bul(sip_stok_kod) as rk,
	dbo.A_fn_BedenDetay_Bul(sip_stok_kod) as bd,
	dbo.A_fn_BedenKod_Bul(sip_stok_kod) as bk
	from dbo.SIPARISLER 
	where sip_tip=0 and 
	sip_cins=1 and 
	sip_OnaylayanKulNo > 0 and 
	sip_kapat_fl = 0 and
	sip_evrakno_seri=@SeriNo and
	sip_evrakno_sira=@SiraNo 

End

END

ELSE IF @sip_tip=6
BEGIN
if @CariyeGore=1
Begin
	Select Cast(upl_Guid as varchar(50)) as rn,
	upl_satirno as sn,
	upl_kodu as Kod,
	dbo.A_fn_StokIsmi_Bul(upl_kodu) as Stok,
	upl_miktar as Miktar,
	(Select sum(GMiktar) From URETIM_MALZEME_PLANLAMA_BGS Where IsEmriKodu=upl_isemri and StokKod=upl_kodu and HTip=0) as Teslim,
	upl_parti_kod as PartiKod,
	upl_lotno as LotNo,
	dbo.A_fn_DetayTakipTipi_Bul(upl_kodu) as dtt,
	dbo.A_fn_RenkDetay_Bul(upl_kodu) as rd,
	dbo.A_fn_RenkKod_Bul(upl_kodu) as rk,
	dbo.A_fn_BedenDetay_Bul(upl_kodu) as bd,
	dbo.A_fn_BedenKod_Bul(upl_kodu) as bk
	from dbo.URETIM_MALZEME_PLANLAMA 
	where (Select Count(IsEmriKodu) From URETIM_MALZEME_PLANLAMA_BGS Where IsEmriKodu=upl_isemri and StokKod=upl_kodu and KMiktar>0 and HTip=0)>0 and
	upl_isemri=@CariKod

End
Else
Begin
	Select Cast(upl_Guid as varchar(50)) as rn,
	upl_satirno as sn,
	upl_kodu as Kod,
	dbo.A_fn_StokIsmi_Bul(upl_kodu) as Stok,
	upl_miktar as Miktar,
	(Select sum(GMiktar) From URETIM_MALZEME_PLANLAMA_BGS Where IsEmriKodu=upl_isemri and StokKod=upl_kodu and HTip=0) as Teslim,
	upl_parti_kod as PartiKod,
	upl_lotno as LotNo,
	dbo.A_fn_DetayTakipTipi_Bul(upl_kodu) as dtt,
	dbo.A_fn_RenkDetay_Bul(upl_kodu) as rd,
	dbo.A_fn_RenkKod_Bul(upl_kodu) as rk,
	dbo.A_fn_BedenDetay_Bul(upl_kodu) as bd,
	dbo.A_fn_BedenKod_Bul(upl_kodu) as bk
	from dbo.URETIM_MALZEME_PLANLAMA 
	where (Select Count(IsEmriKodu) From URETIM_MALZEME_PLANLAMA_BGS Where IsEmriKodu=upl_isemri and StokKod=upl_kodu and KMiktar>0 and HTip=0)>0 
	and upl_isemri=@CariKod
End

END
ELSE

BEGIN
if @CariyeGore=1
Begin

	Select Cast(sip_Guid as varchar(50)) as rn,
	sip_satirno as sn,
	sip_stok_kod as Kod,
	dbo.A_fn_StokIsmi_Bul(sip_stok_kod) as Stok,
	sip_miktar as Miktar,
	sip_teslim_miktar as Teslim,
	sip_parti_kodu as PartiKod,
	sip_lot_no as LotNo,
	dbo.A_fn_DetayTakipTipi_Bul(sip_stok_kod) as dtt,
	dbo.A_fn_RenkDetay_Bul(sip_stok_kod) as rd,
	dbo.A_fn_RenkKod_Bul(sip_stok_kod) as rk,
	dbo.A_fn_BedenDetay_Bul(sip_stok_kod) as bd,
	dbo.A_fn_BedenKod_Bul(sip_stok_kod) as bk
	from dbo.SIPARISLER 
	where sip_tip=@sip_tip and 
	sip_cins=0 and 
	sip_OnaylayanKulNo > 0 and 
	sip_kapat_fl = 0 and
	sip_musteri_kod=@CariKod
End
Else
Begin

	Select Cast(sip_Guid as varchar(50)) as rn,
	sip_satirno as sn,
	sip_stok_kod as Kod,
	dbo.A_fn_StokIsmi_Bul(sip_stok_kod) as Stok,
	sip_miktar as Miktar,
	sip_teslim_miktar as Teslim,
	sip_parti_kodu as PartiKod,
	sip_lot_no as LotNo,
	dbo.A_fn_DetayTakipTipi_Bul(sip_stok_kod) as dtt,
	dbo.A_fn_RenkDetay_Bul(sip_stok_kod) as rd,
	dbo.A_fn_RenkKod_Bul(sip_stok_kod) as rk,
	dbo.A_fn_BedenDetay_Bul(sip_stok_kod) as bd,
	dbo.A_fn_BedenKod_Bul(sip_stok_kod) as bk
	from dbo.SIPARISLER 
	where sip_tip=@sip_tip and 
	sip_cins=0 and 
	sip_OnaylayanKulNo > 0 and 
	sip_kapat_fl = 0 and
	sip_evrakno_seri=@SeriNo and
	sip_evrakno_sira=@SiraNo 

End

END

END
GO

If exists(select name from sysobjects where name='A_sp_SK_StokHar_DataGetir_V11')
	DROP Procedure A_sp_SK_StokHar_DataGetir_V11
GO

CREATE Procedure dbo.A_sp_SK_StokHar_DataGetir_V11(
@StokKod as varchar(25),
@sip_RecNo as uniqueidentifier,
@dtt as integer,
@rd as bit,
@bd as bit,
@Tip as integer)
with encryption
AS
BEGIN
declare @isemri as varchar(25)
Declare @sth_RecNo as uniqueidentifier
IF @Tip=4
BEGIN
SELECT @sth_RecNo=sthek_related_uid FROM dbo.STOK_HAREKETLERI_EK WHERE sth_subesip_uid=@sip_RecNo
/*Select @sth_RecNo=sth_RECno
from dbo.STOK_HAREKETLERI
where sth_subesip_recid_recno=@sip_RecNo*/
END
ELSE IF @Tip=11
BEGIN
Select @sth_RecNo=kon_Guid
from dbo.KONSINYE_HAREKETLERI
where kon_sip_uid=@sip_RecNo
END
ELSE IF @Tip=12
BEGIN
Select @isemri=upl_isemri
from dbo.URETIM_MALZEME_PLANLAMA
where upl_Guid=@sip_RecNo
END
ELSE
BEGIN
Select @sth_RecNo=sth_Guid
from dbo.STOK_HAREKETLERI
where sth_sip_uid=@sip_RecNo
END

IF @rd=1 or @bd=1
BEGIN
	IF @dtt<>3
	BEGIN
	Select dbo.A_S_fn_RenkBul (BdnHar_BedenNo ,@StokKod) as Renk,
dbo.A_S_fn_BedenBul (BdnHar_BedenNo ,@StokKod) as Beden,
BdnHar_BedenNo as BedenNo,
BdnHar_HarGor as Miktar,
'' as CihazNo
from dbo.BEDEN_HAREKETLERI
where BdnHar_Tipi=11 and 
BdnHar_Har_uid=@sth_RecNo 
	END
	--ELSE IF @dtt=3
	--BEGIN
	-- ??? 
	--END
END
ELSE
BEGIN
	IF @dtt<>3
	BEGIN
IF @Tip=4
BEGIN
Select '' as Renk,
'' as Beden,
0 as BedenNo,
sth_miktar as Miktar,
'' as CihazNo 
from dbo.STOK_HAREKETLERI
where sth_Guid=(SELECT sthek_related_uid FROM dbo.STOK_HAREKETLERI_EK WHERE sth_subesip_uid=@sip_RecNo)
--sth_subesip_recid_recno=@sip_RecNo
END
ELSE IF @Tip=11
BEGIN
	Select '' as Renk,
'' as Beden,
0 as BedenNo,
kon_miktar as Miktar,
'' as CihazNo 
from dbo.KONSINYE_HAREKETLERI
where kon_sip_uid=@sip_RecNo
END
ELSE IF @Tip=12
BEGIN
	Select '' as Renk,
'' as Beden,
0 as BedenNo,
sth_miktar as Miktar,
'' as CihazNo 
from dbo.STOK_HAREKETLERI
where sth_isemri_gider_kodu=@isemri and sth_stok_kod=@StokKod and 
sth_tip=1 And sth_cins=7 and sth_normal_iade=0 And sth_evraktip=0
END
ELSE
BEGIN
	Select '' as Renk,
'' as Beden,
0 as BedenNo,
sth_miktar as Miktar,
'' as CihazNo 
from dbo.STOK_HAREKETLERI
where sth_sip_uid=@sip_RecNo
END
	END
	ELSE IF @dtt=3
	BEGIN
	Select '' as Renk,
'' as Beden,
0 as BedenNo,
1 as Miktar,
ChHar_SeriNo as CihazNo 
from dbo.CIHAZ_HAREKETLERI
where ChHar_master_tablo=0 and
ChHar_master_uid=@sth_RecNo
	END
END

END
GO

--Sipariþe ait bilgileri getirir
If exists(select name from sysobjects where name='A_sp_SK_Sip_InfoGetir_V11')
	DROP Procedure A_sp_SK_Sip_InfoGetir_V11
GO

CREATE Procedure dbo.A_sp_SK_Sip_InfoGetir_V11(@sip_tip as tinyint,
@SeriNo as varchar(6),
@SiraNo as integer,
@BelgeNo as varchar(15),
@isemri as varchar(25))
with encryption
AS
BEGIN

Declare @CariKod as varchar(25)
Declare @Cari as varchar(30)
Declare @PerKod as varchar(25)
Declare @Per as varchar(50)
Declare @SormMerkKod as varchar(25)
Declare @SormMerk as varchar(40)
Declare @DepoNo as integer
Declare @Depo as varchar(50)
Declare @Tarih as datetime
Declare @Kur as float
Declare @DvzCins as tinyint
Declare @OpNo as integer
Declare @Op as varchar(30)

IF @sip_tip=3
BEGIN
Select @Tarih=sip_tarih, 
@CariKod=sip_musteri_kod,
@PerKod=sip_satici_kod,
@SormMerkKod=sip_cari_sormerk,
@DepoNo=sip_depono,
@Kur=sip_doviz_kuru,
@DvzCins=sip_doviz_cinsi,
@OpNo=sip_opno 
from dbo.SIPARISLER 
where 
sip_tip=1 and
sip_cins=3 and
sip_OnaylayanKulNo > 0 and
sip_kapat_fl=0 and
sip_evrakno_seri=@SeriNo and
sip_evrakno_sira=@SiraNo
END
ELSE IF @sip_tip=4
BEGIN
Select @Tarih=sip_tarih, 
@CariKod=sip_musteri_kod,
@PerKod=sip_satici_kod,
@SormMerkKod=sip_cari_sormerk,
@DepoNo=sip_depono,
@Kur=sip_doviz_kuru,
@DvzCins=sip_doviz_cinsi,
@OpNo=sip_opno 
from dbo.SIPARISLER 
where 
sip_tip=0 and
sip_cins=3 and
sip_OnaylayanKulNo > 0 and
sip_kapat_fl=0 and
sip_evrakno_seri=@SeriNo and
sip_evrakno_sira=@SiraNo
END
ELSE IF @sip_tip=5
BEGIN
Select @Tarih=sip_tarih, 
@CariKod=sip_musteri_kod,
@PerKod=sip_satici_kod,
@SormMerkKod=sip_cari_sormerk,
@DepoNo=sip_depono,
@Kur=sip_doviz_kuru,
@DvzCins=sip_doviz_cinsi,
@OpNo=sip_opno 
from dbo.SIPARISLER 
where 
sip_tip=0 and
sip_cins=1 and
sip_OnaylayanKulNo > 0 and
sip_kapat_fl=0 and
sip_evrakno_seri=@SeriNo and
sip_evrakno_sira=@SiraNo
END
ELSE IF @sip_tip=6
BEGIN
Select @Tarih=upl_lastup_date, 
@CariKod=upl_isemri,
@PerKod='',
@SormMerkKod='',
@DepoNo=upl_depno,
@Kur=1,
@DvzCins=0,
@OpNo=0 
from dbo.URETIM_MALZEME_PLANLAMA 
where upl_isemri=@isemri and
(Select Count(IsEmriKodu) From URETIM_MALZEME_PLANLAMA_BGS Where IsEmriKodu=upl_isemri and StokKod=upl_kodu and KMiktar>0 and HTip=0)>0
END
ELSE
BEGIN
Select @Tarih=sip_tarih, 
@CariKod=sip_musteri_kod,
@PerKod=sip_satici_kod,
@SormMerkKod=sip_cari_sormerk,
@DepoNo=sip_depono,
@Kur=sip_doviz_kuru,
@DvzCins=sip_doviz_cinsi,
@OpNo=sip_opno 
from dbo.SIPARISLER 
where 
sip_tip=@sip_tip and
sip_cins=0 and
sip_OnaylayanKulNo > 0 and
sip_kapat_fl=0 and
sip_evrakno_seri=@SeriNo and
sip_evrakno_sira=@SiraNo
END
--------------------------------------------
If @OpNo = 0 
BEGIN
set @Op = 'PEÞÝN'
END
Else If @OpNo> 0 
BEGIN
Select @Op=odp_adi 
from dbo.ODEME_PLANLARI 
where odp_no=@OpNo
END
Else If @OpNo < 0 
BEGIN
set @Op = cast(Abs(@OpNo) as varchar(10)) + ' Gün'
END
   
--------------------------------------------
IF @sip_tip=6
Begin
Select @Cari=is_Ismi from dbo.ISEMIRLERI 
where is_Kod=@CariKod
End
Else
Begin
Select @Cari=cari_unvan1 from dbo.CARI_HESAPLAR 
where cari_kod=@CariKod
End

Select @Per=per_adi from dbo.PERSONELLER
where per_kod=@PerKod

Select @SormMerk=som_isim from dbo.SORUMLULUK_MERKEZLERI
where som_kod=@SormMerkKod

Select @Depo=dep_adi from dbo.DEPOLAR
where dep_no=@DepoNo

Select  isnull(@CariKod,'') as ck,
isnull(@Cari,'') as c,
isnull(@PerKod,'') as pk,
isnull(@Per,'') as p,
isnull(@SormMerkKod,'') as sk,
isnull(@SormMerk,'') as s,
isnull(@DepoNo,0) as dn,
isnull(@Depo,'') as d,
@Tarih as t,
isnull(@OpNo,0) as opno,
@Op as op,
@DvzCins as dc,
@Kur as k

END
GO

--Depodaki stoklarý getir
If exists(select name from sysobjects where name='A_sp_Irs_DepoStokGetir')
	DROP Procedure A_sp_Irs_DepoStokGetir
GO

CREATE Procedure dbo.A_sp_Irs_DepoStokGetir(
@DepoNo as integer)
with encryption
AS
BEGIN

Select sto_kod as Kod,sto_isim as Stok,sto_anagrup_kod as AnaGrup,
sto_altgrup_kod as AltGrup,sto_varyant_detayli_fl2,sto_varyant_kod_arr2,
sto_varyant_kod_arr1,sto_varyant_detayli_fl1,sto_detay_takip,
isnull(dbo.fn_DepodakiMiktar(sto_kod,@DepoNo,getdate()),0) as Miktar 
from dbo.STOKLAR

END
GO

--Depodaki cihaz nolu stoklarý getir
If exists(select name from sysobjects where name='A_sp_CihazGetir')
	DROP Procedure A_sp_CihazGetir
GO

CREATE Procedure dbo.A_sp_CihazGetir(
@DepoNo as integer,
@StokKod as varchar(25))
with encryption
AS
BEGIN

SELECT C.chz_stok_kodu as Kod,
B.ChHar_SeriNo as CihazNo,
SUM(
CASE
      WHEN (sth_tip=0) OR ((sth_tip=2) AND (sth_giris_depo_no=@DepoNo)) THEN 1
      WHEN (sth_tip=1) OR ((sth_tip=2) AND (sth_cikis_depo_no=@DepoNo)) THEN -1
      END
    ) as Miktar
from dbo.STOK_HAREKETLERI AS A WITH (NOLOCK) 
INNER JOIN dbo.CIHAZ_HAREKETLERI  AS B WITH (NOLOCK) ON 
(A.sth_Guid = B.ChHar_master_uid)
INNER JOIN dbo.CIHAZ as C ON 
B.ChHar_SeriNo=C.chz_serino
INNER JOIN dbo.STOK as S ON
S.sto_kod=C.chz_stok_kodu
WHERE  
--(sth_stok_kod=@StokKod) AND
((sth_tarih<=getdate()) OR (getdate()<='1900-1-1') OR (getdate() is NULL)) AND
(sth_miktar<>0) AND
(
((sth_tip=0) and ((sth_giris_depo_no=@DepoNo) OR (@DepoNo=0))) OR
((sth_tip=1) and ((sth_cikis_depo_no=@DepoNo) OR (@DepoNo=0))) OR
((sth_tip=2) AND (sth_giris_depo_no=@DepoNo) AND (sth_giris_depo_no<>sth_cikis_depo_no)) OR
((sth_tip=2) AND (sth_cikis_depo_no=@DepoNo) AND (sth_giris_depo_no<>sth_cikis_depo_no))
) AND 
(dbo.fn_DegerFarki_mi(sth_cins)=0) AND 
C.chz_st_evr_sira=0 and
C.chz_stok_kodu=@StokKod
group by B.ChHar_SeriNo,C.chz_stok_kodu,S.sto_isim

END
GO

--Depodaki renk beden detayý olan stoðun miktarýný getir
If exists(select name from sysobjects where name='A_sp_DepoStokGetir_RB')
	DROP Procedure A_sp_DepoStokGetir_RB
GO

CREATE Procedure dbo.A_sp_DepoStokGetir_RB(
@DepoNo as integer,
@StokKod as varchar(25),
@Renk as varchar(10),
@Beden as varchar(10))
with encryption
AS
BEGIN

Declare @BedenNo as integer
SET @BedenNo=dbo.A_S_fn_HareketSatirNoBul (@StokKod,@Renk,@Beden)

SELECT
isnull(
  SUM(CASE
        WHEN (sth_tip=0) OR ((sth_tip=2) AND (sth_giris_depo_no=@DepoNo)) THEN B.BdnHar_HarGor
        WHEN (sth_tip=1) OR ((sth_tip=2) AND (sth_cikis_depo_no=@DepoNo)) THEN (-1) * B.BdnHar_HarGor
        ELSE 0
      END
     ),0) AS [MIKTAR]
  from dbo.STOK_HAREKETLERI AS A WITH (NOLOCK) INNER JOIN dbo.BEDEN_HAREKETLERI  AS B WITH (NOLOCK) ON (A.sth_Guid = B.BdnHar_Har_uid) AND (B.BdnHar_Tipi=11)
  WHERE (B.BdnHar_BedenNo=@BedenNo) AND
	(sth_stok_kod=@StokKod) AND
         ((sth_tarih<=getdate()) OR (getdate()<='1900-1-1') OR (getdate() is NULL)) AND
         (sth_miktar<>0) AND
         (
           ((sth_tip=0) and ((sth_giris_depo_no=@DepoNo) OR (@DepoNo=0))) OR
           ((sth_tip=1) and ((sth_cikis_depo_no=@DepoNo) OR (@DepoNo=0))) OR
           ((sth_tip=2) AND (sth_giris_depo_no=@DepoNo) AND (sth_giris_depo_no<>sth_cikis_depo_no)) OR
           ((sth_tip=2) AND (sth_cikis_depo_no=@DepoNo) AND (sth_giris_depo_no<>sth_cikis_depo_no))
         )
         AND (dbo.fn_DegerFarki_mi(sth_cins)=0)
  GROUP BY B.BdnHar_BedenNo

END
GO

--Depodaki partilot takibi olan stoðun miktarýný getir
If exists(select name from sysobjects where name='A_sp_DepoStokGetir_PL')
	DROP Procedure A_sp_DepoStokGetir_PL
GO

CREATE Procedure dbo.A_sp_DepoStokGetir_PL(
@DepoNo as integer,
@StokKod as varchar(25),
@PartiKod as varchar(25),
@LotNo as integer)
with encryption
AS
BEGIN

SELECT isnull(SUM(CASE
                       WHEN sth_tip=0 THEN sth_miktar
                       WHEN sth_tip=1 THEN (-1) * sth_miktar
                       WHEN (sth_tip=2) AND
                            (sth_giris_depo_no=@DepoNo) THEN sth_miktar
                       WHEN (sth_tip=2) AND
                            (sth_cikis_depo_no=@DepoNo) THEN (-1) * sth_miktar
                       ELSE 0
                       END
                    ),0)
         FROM   dbo.STOK_HAREKETLERI WITH (NOLOCK)
         WHERE  (sth_stok_kod=@StokKod) AND
                (sth_parti_kodu=@PartiKod) AND
                (sth_lot_no=@LotNo) AND
                ((sth_tarih<=getdate()) OR (getdate()<='1900-1-1') OR (getdate() is NULL)) AND
                ( (@DepoNo=0) OR
                  ((sth_tip=0) and (sth_giris_depo_no=@DepoNo)) OR
                  ((sth_tip=1) and (sth_cikis_depo_no=@DepoNo)) OR
                  ((sth_tip=2) AND (sth_giris_depo_no=@DepoNo) AND (sth_giris_depo_no<>sth_cikis_depo_no)) OR
                  ((sth_tip=2) AND (sth_cikis_depo_no=@DepoNo) AND (sth_giris_depo_no<>sth_cikis_depo_no))
                )
                AND (dbo.fn_DegerFarki_mi(sth_cins)=0)

END
GO

--Depodaki serino detayý olan stoðun miktarýný getir
If exists(select name from sysobjects where name='A_sp_DepoStokGetir_C')
	DROP Procedure A_sp_DepoStokGetir_C
GO

CREATE Procedure dbo.A_sp_DepoStokGetir_C(
@DepoNo as integer,
@StokKod as varchar(25),
@SeriNo varchar(25))
with encryption
AS
BEGIN

SELECT isnull(
SUM(
CASE
      WHEN (sth_tip=0) OR ((sth_tip=2) AND (sth_giris_depo_no=@DepoNo)) THEN 1
      WHEN (sth_tip=1) OR ((sth_tip=2) AND (sth_cikis_depo_no=@DepoNo)) THEN -1
      END
    ),0) as miktar
from dbo.STOK_HAREKETLERI AS A WITH (NOLOCK) 
INNER JOIN dbo.CIHAZ_HAREKETLERI  AS B WITH (NOLOCK) ON 
(A.sth_Guid = B.ChHar_master_uid)
INNER JOIN dbo.STOK_SERINO_TANIMLARI as C ON 
B.ChHar_SeriNo=C.chz_serino
INNER JOIN dbo.STOKLAR as S ON
S.sto_kod=C.chz_stok_kodu
WHERE  
(B.ChHar_SeriNo=@SeriNo) AND
(sth_stok_kod=@StokKod) AND
((sth_tarih<=getdate()) OR (getdate()<='1900-1-1') OR (getdate() is NULL)) AND
(sth_miktar<>0) AND
(
((sth_tip=0) and ((sth_giris_depo_no=@DepoNo) OR (@DepoNo=0))) OR
((sth_tip=1) and ((sth_cikis_depo_no=@DepoNo) OR (@DepoNo=0))) OR
((sth_tip=2) AND (sth_giris_depo_no=@DepoNo) AND (sth_giris_depo_no<>sth_cikis_depo_no)) OR
((sth_tip=2) AND (sth_cikis_depo_no=@DepoNo) AND (sth_giris_depo_no<>sth_cikis_depo_no))
) AND 
(dbo.fn_DegerFarki_mi(sth_cins)=0) AND 
C.chz_st_evr_sira=0
group by B.ChHar_SeriNo,C.chz_stok_kodu,S.sto_isim

END
GO

IF EXISTS (
SELECT * FROM   sysobjects 
WHERE  name = 'A_D_fn_StokDovizKuruBul')
DROP FUNCTION A_D_fn_StokDovizKuruBul
GO

CREATE FUNCTION dbo.A_D_fn_StokDovizKuruBul(
@StokKod as varchar(25),
@CariKod as varchar(25))
RETURNS float 
with encryption
AS
BEGIN
  Declare @Sonuc as float
  Declare @StokDovizCinsi as tinyint
  Declare @CariKurHesapSekli as tinyint

  Select @StokDovizCinsi=sto_doviz_cinsi
from dbo.STOKLAR where sto_kod=@StokKod

 Select @CariKurHesapSekli=cari_KurHesapSekli
from dbo.CARI_HESAPLAR where cari_kod=@CariKod

IF @StokDovizCinsi=0
SET @Sonuc=1
ELSE
BEGIN
 if @CariKurHesapSekli=1
	BEGIN 
	Select @Sonuc=dov_fiyat1 from MikroDB_V15.dbo.DOVIZ_KURLARI 
where dov_no=@StokDovizCinsi
	END
  else if @CariKurHesapSekli=2
	BEGIN
	Select @Sonuc=dov_fiyat2 from MikroDB_V15.dbo.DOVIZ_KURLARI 
where dov_no=@StokDovizCinsi
	END
  else if @CariKurHesapSekli=3
	BEGIN 
	Select @Sonuc=dov_fiyat3 from MikroDB_V15.dbo.DOVIZ_KURLARI 
where dov_no=@StokDovizCinsi
	END
  else if @CariKurHesapSekli=4
	BEGIN 
	Select @Sonuc=dov_fiyat4 from MikroDB_V15.dbo.DOVIZ_KURLARI 
where dov_no=@StokDovizCinsi
	END
END
  IF @Sonuc is null set @Sonuc=0
  Return @Sonuc
END
GO

If exists(select name from sysobjects where name='A_sp_SK_StokHar_Kaydet')
	DROP Procedure A_sp_SK_StokHar_Kaydet
GO

CREATE Procedure A_sp_SK_StokHar_Kaydet(
@No as integer,			--Evrak Tipini Belirler
@sth_evrakno_seri as varchar(6),--Seri No
@sth_evrakno_sira as integer,	--Sýra No
@SipRecNoStr as  varchar(36),		--Sipariþe ait REC No
@Miktar as float,		--Teslim Miktarý
@SatirNo as integer,		--Satýr No
@sth_plasiyer_kodu varchar (25),
@sth_srm_merkezi varchar (25),
@DepoNo as integer,
@SatirNoGeri as integer OUTPUT)           
with encryption  
AS
BEGIN

Declare @sth_tip as tinyint
Declare @sth_cins as tinyint
Declare @sth_normal_iade as tinyint
Declare @sth_evraktip as tinyint
Declare @sth_create_user smallint 
Declare @sth_isk_mas1 tinyint  
Declare @sth_isk_mas2 tinyint  
Declare @sth_isk_mas3 tinyint
Declare @sth_isk_mas4 tinyint
Declare @sth_isk_mas5 tinyint
Declare @sth_isk_mas6 tinyint
Declare @sth_isk_mas7 tinyint
Declare @sth_isk_mas8 tinyint
Declare @sth_isk_mas9 tinyint
Declare @sth_isk_mas10 tinyint
Declare @sth_sat_iskmas1 bit
Declare @sth_sat_iskmas2 bit
Declare @sth_sat_iskmas3 bit
Declare @sth_sat_iskmas4 bit
Declare @sth_sat_iskmas5 bit
Declare @sth_sat_iskmas6 bit
Declare @sth_sat_iskmas7 bit
Declare @sth_sat_iskmas8 bit
Declare @sth_sat_iskmas9 bit
Declare @sth_sat_iskmas10 bit
Declare @sth_stok_kod varchar (25)
Declare @sth_cari_kodu varchar (25)
Declare @sth_har_doviz_cinsi tinyint
Declare @sth_har_doviz_kuru float
Declare @sth_alt_doviz_kuru float
Declare @sth_tutar float
Declare @sth_iskonto1 float
Declare @sth_iskonto2 float
Declare @sth_iskonto3 float
Declare @sth_iskonto4 float
Declare @sth_iskonto5 float
Declare @sth_iskonto6 float
Declare @sth_masraf1 float
Declare @sth_masraf2 float
Declare @sth_masraf3 float
Declare @sth_masraf4 float
Declare @sth_vergi_pntr tinyint
Declare @sth_vergi float
Declare @sth_masraf_vergi_pntr tinyint
Declare @sth_masraf_vergi float
Declare @sth_odeme_op int
Declare @sth_aciklama varchar (50)
Declare @sth_vergisiz_fl bit
Declare @sth_adres_no int
Declare @sth_parti_kodu varchar (25)
Declare @sth_lot_no int
-- stok döviz cinsi ve kuru bulunur
Declare @sth_stok_doviz_cinsi tinyint
Declare @sth_stok_doviz_kuru float
-- evraða ait satýr no bulunur
Declare @sth_satirno int
-- depo numaralarý evrak tipine göre belirlenir
Declare @sth_giris_depo_no int
Declare @sth_cikis_depo_no int
Declare @SSipRecNo uniqueidentifier
Declare @sth_utsbildirimturu as tinyint

Declare @SipRecNo uniqueidentifier
Set @SipRecNo=Cast(@SipRecNoStr as uniqueidentifier)

IF @No=4
BEGIN
Select 
@sth_giris_depo_no=ssip_girdepo,
@sth_cikis_depo_no=ssip_cikdepo,
@sth_create_user=ssip_create_user,
@sth_cari_kodu='',
@sth_stok_kod=ssip_stok_kod,
@sth_tutar=ssip_tutar,
@sth_iskonto1=0,
@sth_iskonto2=0,
@sth_iskonto3=0,
@sth_iskonto4=0,
@sth_iskonto5=0,
@sth_iskonto6=0,
@sth_masraf1=0,	
@sth_masraf2=0,	
@sth_masraf3=0,	
@sth_masraf4=0,	
@sth_vergi_pntr=0,
@sth_vergi=0,
@sth_masraf_vergi_pntr=0,
@sth_masraf_vergi=0,	
@sth_odeme_op=0,	
@sth_aciklama=ssip_aciklama,
@sth_vergisiz_fl=0,
@sth_har_doviz_cinsi=0,
@sth_har_doviz_kuru=1,
@sth_alt_doviz_kuru=1,
@sth_adres_no=1,
@sth_isk_mas1=1,
@sth_isk_mas2=1,
@sth_isk_mas3=1,
@sth_isk_mas4=1,
@sth_isk_mas5=1,
@sth_isk_mas6=1,
@sth_isk_mas7=1,
@sth_isk_mas8=1,
@sth_isk_mas9=1,
@sth_isk_mas10=1,
@sth_sat_iskmas1=0,
@sth_sat_iskmas2=0,
@sth_sat_iskmas3=0,
@sth_sat_iskmas4=0,
@sth_sat_iskmas5=0,
@sth_sat_iskmas6=0,
@sth_sat_iskmas7=0,
@sth_sat_iskmas8=0,
@sth_sat_iskmas9=0,
@sth_sat_iskmas10=0,
@sth_parti_kodu='',
@sth_lot_no=0
from dbo.DEPOLAR_ARASI_SIPARISLER
where ssip_Guid=@SipRecNo

SET @SSipRecNo =@SipRecNo
SET @SipRecNo=cast((0x0) as uniqueidentifier)

END
ELSE
BEGIN
SET @SSipRecNo =cast((0x0) as uniqueidentifier)
Select @sth_create_user=sip_create_user,
@sth_cari_kodu=sip_musteri_kod,
@sth_stok_kod=sip_stok_kod,
@sth_tutar=(sip_tutar*@Miktar)/sip_miktar,
@sth_iskonto1=(sip_iskonto_1*@Miktar)/sip_miktar,
@sth_iskonto2=(sip_iskonto_2*@Miktar)/sip_miktar,
@sth_iskonto3=(sip_iskonto_3*@Miktar)/sip_miktar,
@sth_iskonto4=(sip_iskonto_4*@Miktar)/sip_miktar,
@sth_iskonto5=(sip_iskonto_5*@Miktar)/sip_miktar,
@sth_iskonto6=(sip_iskonto_6*@Miktar)/sip_miktar,
@sth_masraf1=(sip_masraf_1*@Miktar)/sip_miktar,	
@sth_masraf2=(sip_masraf_2*@Miktar)/sip_miktar,	
@sth_masraf3=(sip_masraf_3*@Miktar)/sip_miktar,	
@sth_masraf4=(sip_masraf_4*@Miktar)/sip_miktar,	
@sth_vergi_pntr=sip_vergi_pntr,
@sth_vergi=(sip_vergi*@Miktar)/sip_miktar,
@sth_masraf_vergi_pntr=sip_masvergi_pntr,
@sth_masraf_vergi=(sip_masvergi*@Miktar)/sip_miktar,	
@sth_odeme_op=sip_opno,	
@sth_aciklama=sip_aciklama,
@sth_vergisiz_fl=sip_vergisiz_fl,
@sth_har_doviz_cinsi=sip_doviz_cinsi,
@sth_har_doviz_kuru=sip_doviz_kuru,
@sth_alt_doviz_kuru=sip_alt_doviz_kuru,
@sth_adres_no=sip_adresno,
@sth_isk_mas1=sip_iskonto1,
@sth_isk_mas2=sip_iskonto2,
@sth_isk_mas3=sip_iskonto3,
@sth_isk_mas4=sip_iskonto4,
@sth_isk_mas5=sip_iskonto5,
@sth_isk_mas6=sip_iskonto6,
@sth_isk_mas7=sip_masraf1,
@sth_isk_mas8=sip_masraf2,
@sth_isk_mas9=sip_masraf3,
@sth_isk_mas10=sip_masraf4,
@sth_sat_iskmas1=sip_isk1,
@sth_sat_iskmas2=sip_isk2,
@sth_sat_iskmas3=sip_isk3,
@sth_sat_iskmas4=sip_isk4,
@sth_sat_iskmas5=sip_isk5,
@sth_sat_iskmas6=sip_isk6,
@sth_sat_iskmas7=sip_mas1,
@sth_sat_iskmas8=sip_mas2,
@sth_sat_iskmas9=sip_mas3,
@sth_sat_iskmas10=sip_mas4,
@sth_parti_kodu=sip_parti_kodu,
@sth_lot_no=sip_lot_no
from dbo.SIPARISLER 
where sip_Guid=@SipRecNo
END

--Stok Döviz Cinsi ve Döviz Kuru Atamasý
Select @sth_stok_doviz_cinsi=sto_doviz_cinsi
from dbo.STOKLAR 
where sto_kod=@sth_stok_kod
SET @sth_stok_doviz_kuru=dbo.A_D_fn_StokDovizKuruBul(@sth_stok_kod,@sth_cari_kodu)

-- Depo Atamasý  & Evrak Belirleyici Deðiþken Atamalarý
IF @No=1 -- TAÝ
BEGIN
SET @sth_giris_depo_no=@DepoNo
SET @sth_cikis_depo_no=1
SET @sth_tip=0
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=13
SET @sth_utsbildirimturu=1
END
ELSE IF @No=16 -- ATAF
BEGIN
SET @sth_giris_depo_no=@DepoNo
SET @sth_cikis_depo_no=1
SET @sth_tip=0
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=3
SET @sth_utsbildirimturu=1
END
ELSE IF @No=2 --TSÝ
BEGIN
SET @sth_giris_depo_no=1
SET @sth_cikis_depo_no=@DepoNo
SET @sth_tip=1
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=1
SET @sth_utsbildirimturu=10
END
ELSE IF @No=3 --ATSF
BEGIN
SET @sth_giris_depo_no=1
SET @sth_cikis_depo_no=@DepoNo
SET @sth_tip=1
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=4	
SET @sth_utsbildirimturu=10
END
ELSE IF @No=4 --DASF
BEGIN
SET @sth_tip=2
SET @sth_cins=6
SET @sth_normal_iade=0
SET @sth_evraktip=2	
SET @sth_utsbildirimturu=0
END
ELSE IF @No=5 --II giriþ ÝTHALAT
BEGIN
SET @sth_giris_depo_no=@DepoNo
SET @sth_cikis_depo_no=1
SET @sth_tip=0
SET @sth_cins=12
SET @sth_normal_iade=0
SET @sth_evraktip=13	
SET @sth_utsbildirimturu=6
END
ELSE IF @No=6 --II çýkýþ ÝHRACAT
BEGIN
SET @sth_giris_depo_no=1
SET @sth_cikis_depo_no=@DepoNo
SET @sth_tip=1
SET @sth_cins=12
SET @sth_normal_iade=0
SET @sth_evraktip=1	
SET @sth_utsbildirimturu=2
END

Declare @sth_exim_kodu as varchar(25)
Declare @sth_disticaret_turu as tinyint

IF @No=5 or @No=6
BEGIN
Select @sth_exim_kodu=sip_Exp_Imp_Kodu
from dbo.SIPARISLER 
where sip_Guid=@SipRecNo
set @sth_disticaret_turu=3
END
ELSE
BEGIN
set @sth_exim_kodu=''
set @sth_disticaret_turu=0
END

declare @Kontrol as int
declare @Kontrol2 as int

Select @Kontrol2=Count(sth_satirno) From STOK_HAREKETLERI Where 
sth_tip=@sth_tip And 
sth_cins=@sth_cins And
sth_normal_iade=@sth_normal_iade And
sth_evraktip=@sth_evraktip And
sth_evrakno_seri=@sth_evrakno_seri And
sth_evrakno_sira=@sth_evrakno_sira And
sth_sip_uid=@SipRecNo And
sth_stok_kod=@sth_stok_kod

Select @Kontrol=sth_satirno From STOK_HAREKETLERI Where 
sth_tip=@sth_tip And 
sth_cins=@sth_cins And
sth_normal_iade=@sth_normal_iade And
sth_evraktip=@sth_evraktip And
sth_evrakno_seri=@sth_evrakno_seri And
sth_evrakno_sira=@sth_evrakno_sira And
sth_sip_uid=@SipRecNo And
sth_stok_kod=@sth_stok_kod

if @Kontrol2>0
Begin

Update dbo.STOK_HAREKETLERI
set 
sth_miktar=sth_miktar + @Miktar,
sth_miktar2=sth_miktar2 + @Miktar,
sth_tutar=sth_tutar + @sth_tutar,
sth_iskonto1=sth_iskonto1 + @sth_iskonto1,
sth_iskonto2=sth_iskonto2 + @sth_iskonto2,
sth_iskonto3=sth_iskonto3 + @sth_iskonto3,
sth_iskonto4=sth_iskonto4 + @sth_iskonto4,
sth_iskonto5=sth_iskonto5 + @sth_iskonto5,
sth_iskonto6=sth_iskonto6 + @sth_iskonto6,
sth_masraf1=sth_masraf1 + @sth_masraf1,
sth_masraf2=sth_masraf2 + @sth_masraf2,
sth_masraf3=sth_masraf3 + @sth_masraf3,
sth_masraf4=sth_masraf4 + @sth_masraf4,
sth_vergi=sth_vergi + @sth_vergi,
sth_masraf_vergi=sth_masraf_vergi + @sth_masraf_vergi
where sth_tip=@sth_tip And 
sth_cins=@sth_cins And
sth_normal_iade=@sth_normal_iade And
sth_evraktip=@sth_evraktip And
sth_evrakno_seri=@sth_evrakno_seri And
sth_evrakno_sira=@sth_evrakno_sira And
sth_sip_uid=@SipRecNo And
sth_stok_kod=@sth_stok_kod
 
Set @SatirNoGeri=@Kontrol

End
Else
Begin
Declare @sth_recNo uniqueidentifier
Set @sth_recNo=NEWID()

INSERT INTO dbo.STOK_HAREKETLERI(sth_Guid,sth_DBCno, sth_SpecRECno, sth_iptal, sth_fileid, sth_hidden, sth_kilitli, sth_degisti, sth_checksum, sth_create_user, sth_create_date, sth_lastup_user, sth_lastup_date, sth_special1, sth_special2, sth_special3, sth_firmano, sth_subeno, sth_tarih, sth_tip, sth_cins, sth_normal_iade, sth_evraktip, sth_evrakno_seri, sth_evrakno_sira, sth_satirno, sth_belge_no, sth_belge_tarih, sth_stok_kod, 
sth_isk_mas1, sth_isk_mas2, sth_isk_mas3, sth_isk_mas4, sth_isk_mas5, sth_isk_mas6, sth_isk_mas7, sth_isk_mas8, sth_isk_mas9, sth_isk_mas10, sth_sat_iskmas1, sth_sat_iskmas2, sth_sat_iskmas3, sth_sat_iskmas4, sth_sat_iskmas5, sth_sat_iskmas6, sth_sat_iskmas7, sth_sat_iskmas8, sth_sat_iskmas9, sth_sat_iskmas10, 
sth_pos_satis, sth_promosyon_fl, sth_cari_cinsi, sth_cari_kodu, sth_cari_grup_no, sth_isemri_gider_kodu, 
sth_plasiyer_kodu, sth_har_doviz_cinsi, sth_har_doviz_kuru, sth_alt_doviz_kuru, sth_stok_doviz_cinsi, sth_stok_doviz_kuru, sth_miktar, sth_miktar2, sth_birim_pntr, sth_tutar, sth_iskonto1, sth_iskonto2, sth_iskonto3, sth_iskonto4, sth_iskonto5, sth_iskonto6, sth_masraf1, sth_masraf2, sth_masraf3, sth_masraf4, sth_vergi_pntr, sth_vergi, sth_masraf_vergi_pntr, sth_masraf_vergi, sth_netagirlik, sth_odeme_op, sth_aciklama, sth_sip_uid, sth_fat_uid, sth_giris_depo_no, sth_cikis_depo_no, sth_malkbl_sevk_tarihi, sth_cari_srm_merkezi, sth_stok_srm_merkezi, sth_fis_tarihi, sth_fis_sirano, sth_vergisiz_fl, sth_maliyet_ana, sth_maliyet_alternatif, sth_maliyet_orjinal, sth_adres_no, 
sth_parti_kodu, sth_lot_no, sth_kons_uid,   
sth_proje_kodu, sth_exim_kodu, 
sth_otv_pntr, sth_otv_vergi, 
sth_brutagirlik, 
sth_disticaret_turu, sth_otvtutari, sth_otvvergisiz_fl,  
sth_oiv_pntr, sth_oiv_vergi, sth_oivvergisiz_fl,sth_fiyat_liste_no,
sth_oivtutari,sth_Tevkifat_turu,
sth_nakliyedeposu,sth_nakliyedurumu,
sth_yetkili_uid,sth_taxfree_fl
,sth_ilave_edilecek_kdv,sth_ismerkezi_kodu,sth_HareketGrupKodu1,sth_HareketGrupKodu2,sth_HareketGrupKodu3,sth_Olcu1,sth_Olcu2,sth_Olcu3,sth_Olcu4,sth_Olcu5,sth_FormulMiktarNo,sth_FormulMiktar)
VALUES(@sth_recNo,0, 0, 0, 16, 0, 0, 0, 0, 
@sth_create_user, getdate(),@sth_create_user, getdate(), '','','', 
0,--sth_firmano
0,--sth_subeno,int
convert(char(10),getdate(),102), @sth_tip,@sth_cins,@sth_normal_iade,@sth_evraktip,
@sth_evrakno_seri,@sth_evrakno_sira,@SatirNo,'', convert(char(10),getdate(),102), 
@sth_stok_kod, @sth_isk_mas1,@sth_isk_mas2,@sth_isk_mas3,@sth_isk_mas4,
@sth_isk_mas5,@sth_isk_mas6,@sth_isk_mas7,@sth_isk_mas8,@sth_isk_mas9,@sth_isk_mas10,
@sth_sat_iskmas1,@sth_sat_iskmas2,@sth_sat_iskmas3,@sth_sat_iskmas4,@sth_sat_iskmas5,
@sth_sat_iskmas6,@sth_sat_iskmas7,@sth_sat_iskmas8,@sth_sat_iskmas9,@sth_sat_iskmas10,
0,0,0,@sth_cari_kodu, 
0,--sth_cari_grup_no
'', @sth_plasiyer_kodu,
@sth_har_doviz_cinsi,@sth_har_doviz_kuru,
@sth_alt_doviz_kuru,@sth_stok_doviz_cinsi,
@sth_stok_doviz_kuru, @Miktar, 0, 1, @sth_tutar, 
@sth_iskonto1,@sth_iskonto2,@sth_iskonto3,@sth_iskonto4,@sth_iskonto5,
@sth_iskonto6,@sth_masraf1,@sth_masraf2,@sth_masraf3,@sth_masraf4,
@sth_vergi_pntr,@sth_vergi,@sth_masraf_vergi_pntr,@sth_masraf_vergi, 
0, @sth_odeme_op, @sth_aciklama, @SipRecNo, cast((0x0) as uniqueidentifier), 
@sth_giris_depo_no, @sth_cikis_depo_no, convert(char(10),getdate(),102), 
@sth_srm_merkezi, 
'',--sth_stok_srm_merkezi 
'1899-12-30 00:00:00', 0, 0, 0, 0, 0, @sth_adres_no, 
@sth_parti_kodu, @sth_lot_no, cast((0x0) as uniqueidentifier), 
'', @sth_exim_kodu, 
0, 0, 
0, --sth_brutagirlik
@sth_disticaret_turu, 
0, 0, --sth_otvvergisiz_fl 
0, 0, 0, 1, --sth_fiyat_liste_no
0,0,
0,0,
cast((0x0) as uniqueidentifier),0
,0,'','','','',0,0,0,0,0,0,0)

--SELECT @sth_recNo=SCOPE_IDENTITY()

INSERT INTO dbo.STOK_HAREKETLERI_EK
(sthek_DBCno,sthek_SpecRECno,sthek_iptal,sthek_fileid,sthek_hidden,sthek_kilitli,sthek_degisti,sthek_checksum,sthek_create_user,sthek_create_date,sthek_lastup_user,sthek_lastup_date,sthek_special1,sthek_special2,sthek_special3,
sthek_related_uid,sth_subesip_uid,sth_bkm_uid,sth_karsikons_uid,sth_rez_uid,sth_optamam_uid,sth_iadeTlp_uid,sth_HalSatis_uid,
sth_ciroprim_uid,sth_iade_evrak_seri,sth_iade_evrak_sira,sth_yat_tes_kodu,sth_ihracat_kredi_kodu,sth_diib_belge_no,sth_diib_satir_no,sth_mensey_ulke_tipi,sth_mensey_ulke_kodu,sth_halrehmiktari,sth_halrehfiyati,sth_halsandikmiktari,sth_halsandikfiyati,
sth_halsandikkdvtutari,sth_HalKomisyonuKdv,sth_HalRusum,sth_satistipi,sth_vardiya_tarihi,sth_vardiya_no,sth_direkt_iscilik_1,sth_direkt_iscilik_2,sth_direkt_iscilik_3,sth_direkt_iscilik_4,sth_direkt_iscilik_5,sth_genel_uretim_1,sth_genel_uretim_2,sth_genel_uretim_3,sth_genel_uretim_4,sth_genel_uretim_5,sth_fis_tarihi2,sth_fis_sirano2,sth_fiyfark_esas_evrak_seri,sth_fiyfark_esas_evrak_sira,sth_fiyfark_esas_satir_no
,sth_istisna,sth_otv_tevkifat_turu,sth_otv_tevkifat_tutari,sth_servishar_uid,sth_bakimsarf_uid
,sth_utsbildirimturu,sth_utshekzayiatturu,sth_utsimhabertarafgerekcesi,sth_utsdigergerekceaciklamasi)
VALUES
(0,0,0,590,0,0,0,0,@sth_create_user,getdate(),@sth_create_user,getdate(),'','','',
@sth_recNo,
cast((0x0) as uniqueidentifier),
cast((0x0) as uniqueidentifier),cast((0x0) as uniqueidentifier),cast((0x0) as uniqueidentifier),
cast((0x0) as uniqueidentifier),cast((0x0) as uniqueidentifier),cast((0x0) as uniqueidentifier),cast((0x0) as uniqueidentifier),'',0,'','','',0,0,'',0,0,0,0,0,0,0,0,'1899-12-30 00:00:00',
0,0,0,0,0,0,0,0,0,0,0,'1899-12-30 00:00:00',0,'',0,0
,'',0,0,cast((0x0) as uniqueidentifier),cast((0x0) as uniqueidentifier)
,@sth_utsbildirimturu,0,0,'')



Set @SatirNoGeri=@SatirNo

End

END
GO

If exists(select name from sysobjects where name='A_sp_SK_BedenHar_Kaydet')
	DROP Procedure A_sp_SK_BedenHar_Kaydet
GO

CREATE Procedure A_sp_SK_BedenHar_Kaydet(
@No as integer,			--Evrak Tipini Belirler
@sth_evrakno_seri as varchar(6),--Seri No
@sth_evrakno_sira as integer,	--Sýra No
@SipRecNo as uniqueidentifier,		--Sipariþe ait REC No
@Miktar as float,		--Teslim Miktarý
@BedenNo as integer,		--Beden No
@sth_satirno as integer, @VarGuid1 as uniqueidentifier = NULL)	--Satýr No
with encryption  
AS
BEGIN

Declare @sth_tip as tinyint
Declare @sth_cins as tinyint
Declare @sth_normal_iade as tinyint
Declare @sth_evraktip as tinyint
Declare @sth_create_user smallint 

Select @sth_create_user=sip_create_user
from dbo.SIPARISLER 
where sip_Guid=@SipRecNo

-- Evrak Belirleyici deðiþken Atamalarý
IF @No=1 -- TAÝ
BEGIN
SET @sth_tip=0
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=13
END
ELSE IF @No=2 --TSÝ
BEGIN
SET @sth_tip=1
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=1
END
ELSE IF @No=16 -- ATAF
BEGIN
SET @sth_tip=0
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=3
END
ELSE IF @No=3 --ATSF
BEGIN
SET @sth_tip=1
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=4	
END
ELSE IF @No=4 --DASF
BEGIN
SET @sth_tip=2
SET @sth_cins=6
SET @sth_normal_iade=0
SET @sth_evraktip=2	
END
ELSE IF @No=5 --ithalat
BEGIN
SET @sth_tip=0
SET @sth_cins=12
SET @sth_normal_iade=0
SET @sth_evraktip=13	
END
ELSE IF @No=6 --ihracat
BEGIN
SET @sth_tip=1
SET @sth_cins=12
SET @sth_normal_iade=0
SET @sth_evraktip=1	
END

Declare @rn as uniqueidentifier -- Stok Hareketleri REC nosu


Select @rn=sth_Guid 
from dbo.STOK_HAREKETLERI
where sth_tip=@sth_tip and
sth_cins=@sth_cins and
sth_normal_iade=@sth_normal_iade and
sth_evraktip=@sth_evraktip and
sth_evrakno_seri=@sth_evrakno_seri and
sth_evrakno_sira=@sth_evrakno_sira and
sth_satirno=@sth_satirno 

INSERT INTO dbo.BEDEN_HAREKETLERI(BdnHar_Guid, BdnHar_DBCno, BdnHar_Spec_Rec_no, BdnHar_iptal, BdnHar_fileid, BdnHar_hidden, BdnHar_kilitli, BdnHar_degisti, BdnHar_checksum, BdnHar_create_user, BdnHar_create_date, BdnHar_lastup_user, BdnHar_lastup_date, BdnHar_special1, BdnHar_special2, BdnHar_special3, BdnHar_MainProgramNo, BdnHar_VersionNo, BdnHar_MenuNo, BdnHar_MikroSpecial1, BdnHar_MikroSpecial2, BdnHar_MikroSpecial3, BdnHar_ExternalProgramType, BdnHar_ExternalProgramId, BdnHar_Hash, BdnHar_Tipi, BdnHar_Har_uid, BdnHar_BedenNo, BdnHar_HarGor, BdnHar_KnsIsGor, BdnHar_KnsFat, BdnHar_TesMik, BdnHar_rezervasyon_miktari, BdnHar_rezerveden_teslim_edilen, BdnHar_VarSatirNo, BdnHar_VarBaglantiUId1, BdnHar_VarBaglantiUId2, BdnHar_VarBaglantiUId3, BdnHar_VarBaglantiUId4, BdnHar_VarBaglantiUId5, BdnHar_BirimFiyat, BdnHar_Tutar, BdnHar_FiyatListeNo)
VALUES(NEWID(), 0, 0, 0, 113, 0, 0, 0, 0, @sth_create_user, getdate(), @sth_create_user, getdate(), '','','',0,'','','','','',0,'',0,11, @rn, 0, @Miktar, 0, 0, 0, 0, 0, 0, ISNULL(@VarGuid1, CAST(0x0 AS uniqueidentifier)), CAST(0x0 AS uniqueidentifier), CAST(0x0 AS uniqueidentifier), CAST(0x0 AS uniqueidentifier), CAST(0x0 AS uniqueidentifier), 0, 0, 0)--*


IF @No<>4
BEGIN
	UPDATE dbo.BEDEN_HAREKETLERI
	SET BdnHar_TesMik=BdnHar_TesMik+@Miktar
	WHERE (BdnHar_Tipi=9 AND BdnHar_Har_uid=@SipRecNo AND BdnHar_VarBaglantiUId1=ISNULL(@VarGuid1, CAST(0x0 AS uniqueidentifier)))
END
ELSE
BEGIN
	UPDATE dbo.BEDEN_HAREKETLERI
	SET BdnHar_TesMik=BdnHar_TesMik+@Miktar
	WHERE (BdnHar_Tipi=1 AND BdnHar_Har_uid=@SipRecNo AND BdnHar_VarBaglantiUId1=ISNULL(@VarGuid1, CAST(0x0 AS uniqueidentifier)))
END	
	

END
GO

If exists(select name from sysobjects where name='A_sp_SK_CihazHar_Kaydet')
	DROP Procedure A_sp_SK_CihazHar_Kaydet
GO

CREATE Procedure A_sp_SK_CihazHar_Kaydet(
@No as integer,			--Evrak Tipini Belirler
@sth_evrakno_seri as varchar(6),--Seri No
@sth_evrakno_sira as integer,	--Sýra No
@SipRecNo as uniqueidentifier,		--Sipariþe ait REC No
@Miktar as float,		--Teslim Miktarý
@dtt as integer,		--Detay Takip Tipi
@CihazNo as varchar (25),	--Cihaz No
@sth_satirno as integer, @VarGuid1 as uniqueidentifier = NULL)	
with encryption  
AS
BEGIN

Declare @sth_tip as tinyint
Declare @sth_cins as tinyint
Declare @sth_normal_iade as tinyint
Declare @sth_evraktip as tinyint
Declare @sth_create_user smallint 
Declare @sth_stok_kod varchar (25)

Select @sth_create_user=sip_create_user,
@sth_stok_kod=sip_stok_kod
from dbo.SIPARISLER 
where sip_Guid=@SipRecNo

-- Evrak Belirleyici deðiþken Atamalarý
IF @No=1 -- TAÝ
BEGIN
SET @sth_tip=0
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=13
END
ELSE IF @No=2 --TSÝ
BEGIN
SET @sth_tip=1
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=1
END
ELSE IF @No=16 -- ATAF
BEGIN
SET @sth_tip=0
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=3
END
ELSE IF @No=3 --ATSF
BEGIN
SET @sth_tip=1
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=4	
END
ELSE IF @No=5 --II ithalat
BEGIN
SET @sth_tip=0
SET @sth_cins=12
SET @sth_normal_iade=0
SET @sth_evraktip=13	
END
ELSE IF @No=6 --II ihracat
BEGIN
SET @sth_tip=1
SET @sth_cins=12
SET @sth_normal_iade=0
SET @sth_evraktip=1
END

Declare @ChHar_master_recno as uniqueidentifier-- Stok Hareketleri REC nosu
Select @ChHar_master_recno=sth_Guid 
from dbo.STOK_HAREKETLERI
where sth_tip=@sth_tip and
sth_cins=@sth_cins and
sth_normal_iade=@sth_normal_iade and
sth_evraktip=@sth_evraktip and
sth_evrakno_seri=@sth_evrakno_seri and
sth_evrakno_sira=@sth_evrakno_sira and
sth_satirno=@sth_satirno

INSERT INTO dbo.CIHAZ_HAREKETLERI(ChHar_DBCno, ChHar_Spec_Rec_no, ChHar_iptal, ChHar_fileid, ChHar_hidden, ChHar_kilitli, ChHar_degisti, ChHar_checksum, ChHar_create_user, ChHar_create_date, ChHar_lastup_user, ChHar_lastup_date, ChHar_special1, ChHar_special2, ChHar_special3, ChHar_SeriNo, ChHar_StokKodu, ChHar_master_tablo, ChHar_master_uid)
VALUES(0, 0, 0, 98, 0, 0, 0, 0, @sth_create_user, getdate(), 
@sth_create_user, getdate(), '','','', @CihazNo, @sth_stok_kod, 
0, @ChHar_master_recno) --*


END
GO

If exists(select name from sysobjects where name='A_sp_SK_CariHar_Kaydet')
	DROP Procedure A_sp_SK_CariHar_Kaydet
GO

CREATE Procedure dbo.A_sp_SK_CariHar_Kaydet(
@SeriNo as varchar(6), 
@SiraNo as int,
@No as int,
@BelgeNo as varchar(15))
with encryption
AS
BEGIN

Declare @tip as tinyint
Declare @cins as tinyint
Declare @normal as tinyint
Declare @evtip as tinyint

If @No=16
Begin
SET  @tip = 0
SET  @cins = 0
SET  @normal = 0
SET  @evtip = 3
End
Else
Begin
SET  @tip = 1
SET  @cins = 0
SET  @normal = 0
SET  @evtip = 4
End

Declare @cha_tip as tinyint
Declare @cha_cinsi as tinyint
Declare @cha_normal_Iade as tinyint
Declare @cha_evrak_tip as tinyint

If @No=16
Begin
set @cha_tip=1
set @cha_cinsi=6
set @cha_normal_Iade=0
set @cha_evrak_tip=0
End
Else
Begin
set @cha_tip=0
set @cha_cinsi=6
set @cha_normal_Iade=0
set @cha_evrak_tip=63
End

Declare @cha_meblag as float
Declare @cha_ft_iskonto1 as float
Declare @cha_ft_iskonto2 as float
Declare @cha_ft_iskonto3 as float
Declare @cha_ft_iskonto4 as float
Declare @cha_ft_iskonto5 as float
Declare @cha_ft_iskonto6 as float
Declare @cha_ft_masraf1 as float
Declare @cha_ft_masraf2 as float
Declare @cha_ft_masraf3 as float
Declare @cha_ft_masraf4 as float
Declare @cha_aratoplam as float
Declare @cha_vade as int


--cha_meblag
Select @cha_meblag=sum( sth_tutar - 
(sth_iskonto1+sth_iskonto2+sth_iskonto3+sth_iskonto4+sth_iskonto5+sth_iskonto6) + 
(sth_vergi) + (sth_masraf1+sth_masraf2+sth_masraf3+sth_masraf4)+(sth_masraf_vergi)) 
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_belge_no=@BelgeNo

print 'cha_meblag'
print @cha_meblag

--cha_ft_iskonto1
Select @cha_ft_iskonto1=sum(sth_iskonto1)  
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_belge_no=@BelgeNo

print 'cha_ft_iskonto1'
print @cha_ft_iskonto1

--cha_ft_iskonto2
Select @cha_ft_iskonto2=sum(sth_iskonto2)  
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_belge_no=@BelgeNo

print 'cha_ft_iskonto2'
print @cha_ft_iskonto2

--cha_ft_iskonto3
Select @cha_ft_iskonto3=sum(sth_iskonto3)  
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_belge_no=@BelgeNo

print 'cha_ft_iskonto3'
print @cha_ft_iskonto3

--cha_ft_iskonto4
Select @cha_ft_iskonto4=sum(sth_iskonto4)  
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_belge_no=@BelgeNo

print 'cha_ft_iskonto4'
print @cha_ft_iskonto4

--cha_ft_iskonto5
Select @cha_ft_iskonto5=sum(sth_iskonto5) 
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_belge_no=@BelgeNo

print 'cha_ft_iskonto5'
print @cha_ft_iskonto5

--cha_ft_iskonto6
Select @cha_ft_iskonto6=sum(sth_iskonto6)  
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_belge_no=@BelgeNo

print 'cha_ft_iskonto6'
print @cha_ft_iskonto6

--cha_ft_masraf1
Select @cha_ft_masraf1=sum(sth_masraf1)  
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_belge_no=@BelgeNo

print 'cha_ft_masraf1'
print @cha_ft_masraf1

--cha_ft_masraf2
Select @cha_ft_masraf2=sum(sth_masraf2) 
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_belge_no=@BelgeNo

print 'cha_ft_masraf2'
print @cha_ft_masraf2

--cha_ft_masraf3
Select @cha_ft_masraf3=sum(sth_masraf3)  
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_belge_no=@BelgeNo

print 'cha_ft_masraf3'
print @cha_ft_masraf3

--cha_ft_masraf4
Select @cha_ft_masraf4=sum(sth_masraf4)  
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_belge_no=@BelgeNo

print 'cha_ft_masraf4'
print @cha_ft_masraf4

--cha_vade
Select @cha_vade=sum(sth_odeme_op)  
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_belge_no=@BelgeNo

print 'cha_vade'
print @cha_vade

SET @cha_aratoplam=@cha_meblag

Declare @cha_srmrkkodu as varchar(25)
Declare @cha_satici_kodu as varchar(25)
Declare @vergi_pntr as tinyint
Declare @vergi as float
Declare @cha_vergi1 as float
Declare @cha_vergi2 as float
Declare @cha_vergi3 as float
Declare @cha_vergi4 as float
Declare @cha_vergi5 as float
Declare @cha_vergi6 as float
Declare @cha_vergi7 as float
Declare @cha_vergi8 as float
Declare @cha_vergi9 as float
Declare @cha_vergi10 as float
Declare @cha_kod as varchar(25)
Declare @UserNo as integer
Declare @Tarih as datetime
Declare @cha_d_cins as tinyint
Declare @cha_d_kur as float
Declare @cha_altd_kur as float

Select 
@cha_d_cins=sth_har_doviz_cinsi,
@cha_d_kur=sth_har_doviz_kuru,
@cha_altd_kur=sth_alt_doviz_kuru,
@Tarih=sth_create_date,
@UserNo=sth_create_user,
@cha_kod=sth_cari_kodu ,
@cha_srmrkkodu=sth_cari_srm_merkezi,
@cha_satici_kodu=sth_plasiyer_kodu,
@vergi_pntr=sth_vergi_pntr
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_belge_no=@BelgeNo and 
sth_satirno=0

--vergi
--vergi
Select @cha_vergi1=isnull((sum(sth_vergi)+sum(sth_masraf_vergi)),0) 
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_vergi_pntr=1

Select @cha_vergi2=isnull((sum(sth_vergi)+sum(sth_masraf_vergi)),0) 
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_vergi_pntr=2

Select @cha_vergi3=isnull((sum(sth_vergi)+sum(sth_masraf_vergi)),0) 
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_vergi_pntr=3

Select @cha_vergi4=isnull((sum(sth_vergi)+sum(sth_masraf_vergi)),0) 
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_vergi_pntr=4

Select @cha_vergi5=isnull((sum(sth_vergi)+sum(sth_masraf_vergi)),0) 
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_vergi_pntr=5

Select @cha_vergi6=isnull((sum(sth_vergi)+sum(sth_masraf_vergi)),0) 
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_vergi_pntr=6

Select @cha_vergi7=isnull((sum(sth_vergi)+sum(sth_masraf_vergi)),0) 
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_vergi_pntr=7

Select @cha_vergi8=isnull((sum(sth_vergi)+sum(sth_masraf_vergi)),0) 
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_vergi_pntr=8

Select @cha_vergi9=isnull((sum(sth_vergi)+sum(sth_masraf_vergi)),0) 
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_vergi_pntr=9

Select @cha_vergi10=isnull((sum(sth_vergi)+sum(sth_masraf_vergi)),0) 
from dbo.STOK_HAREKETLERI 
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_vergi_pntr=10

print 'Vergi Tutarlarý'
print @cha_vergi1
print @cha_vergi2
print @cha_vergi3
print @cha_vergi4
print @cha_vergi5
print @cha_vergi6
print @cha_vergi7
print @cha_vergi8
print @cha_vergi9
print @cha_vergi10


Declare @KayitKontrol as integer

Select @KayitKontrol=count(cha_Guid)
from dbo.CARI_HESAP_HAREKETLERI
where cha_evrakno_seri=@SeriNo and
cha_evrakno_sira=@SiraNo and
cha_belge_no=@BelgeNo and
cha_tip=@cha_tip and
cha_cinsi=@cha_cinsi and
cha_normal_Iade=@cha_normal_Iade and
cha_evrak_tip=@cha_evrak_tip

IF @KayitKontrol>=0
BEGIN

INSERT INTO dbo.CARI_HESAP_HAREKETLERI(cha_DBCno, cha_SpecRecNo, cha_iptal, cha_fileid, cha_hidden, cha_kilitli, cha_degisti, cha_CheckSum, cha_create_user, cha_create_date, cha_lastup_user, cha_lastup_date, cha_special1, cha_special2, cha_special3, cha_firmano, cha_subeno, cha_tarihi, cha_tip, cha_cinsi, cha_normal_Iade, cha_evrak_tip, cha_satir_no, cha_evrakno_seri, cha_evrakno_sira, cha_belge_no, cha_belge_tarih, cha_cari_cins, cha_kod, 
cha_kasa_hizmet, cha_kasa_hizkod, cha_d_cins, cha_d_kur, cha_altd_kur, cha_grupno, cha_meblag, cha_vade, cha_fis_tarih, cha_fis_sirano, cha_ft_iskonto1, cha_ft_iskonto2, cha_ft_iskonto3, cha_ft_iskonto4, cha_ft_iskonto5, cha_ft_iskonto6, cha_ft_masraf1, cha_ft_masraf2, cha_ft_masraf3, cha_ft_masraf4, 
cha_isk_mas1,cha_isk_mas2,cha_isk_mas3,cha_isk_mas4,cha_isk_mas5,cha_isk_mas6,cha_isk_mas7,cha_isk_mas8,cha_isk_mas9,cha_isk_mas10,
cha_sat_iskmas1,cha_sat_iskmas2,cha_sat_iskmas3,cha_sat_iskmas4,cha_sat_iskmas5,cha_sat_iskmas6,cha_sat_iskmas7,cha_sat_iskmas8,cha_sat_iskmas9,cha_sat_iskmas10,
cha_vergi1, cha_vergi2, cha_vergi3, cha_vergi4, cha_vergi5, cha_vergi6, cha_vergi7, cha_vergi8, cha_vergi9, cha_vergi10, cha_yuvarlama, cha_tpoz, cha_aciklama, cha_trefno, cha_sntck_poz, cha_karsidcinsi, cha_karsid_kur, cha_karsidgrupno, 
cha_srmrkkodu, cha_reftarihi, 
cha_miktari, cha_aratoplam, cha_vergipntr, cha_istisnakodu, cha_stopaj, cha_savsandesfonu, cha_avansmak_damgapul, cha_vergisiz_fl, 
cha_satici_kodu, 
cha_StFonPntr, cha_pos_hareketi, cha_vardiya_tarihi, cha_vardiya_no, cha_vardiya_evrak_ti, 
cha_Vade_Farki_Yuz, cha_karsisrmrkkodu, cha_EXIMkodu, 
cha_ticaret_turu, cha_otvtutari, cha_otvvergisiz_fl, cha_projekodu, 
cha_yat_tes_kodu, cha_ciro_cari_kodu, cha_oiv_pntr,cha_oivtutari,cha_oiv_vergi, cha_oivergisiz_fl, cha_meblag_ana_doviz_icin_gecersiz_fl, cha_meblag_alt_doviz_icin_gecersiz_fl, cha_meblag_orj_doviz_icin_gecersiz_fl,
cha_sip_uid, cha_kirahar_uid, cha_ebelge_turu, cha_tevkifat_toplam
,cha_ilave_edilecek_kdv1,cha_ilave_edilecek_kdv2,cha_ilave_edilecek_kdv3,cha_ilave_edilecek_kdv4,cha_ilave_edilecek_kdv5,cha_ilave_edilecek_kdv6,cha_ilave_edilecek_kdv7,cha_ilave_edilecek_kdv8,cha_ilave_edilecek_kdv9,cha_ilave_edilecek_kdv10,cha_e_islem_turu,cha_fatura_belge_turu,cha_diger_belge_adi,cha_uuid,cha_adres_no,cha_vergifon_toplam,cha_ilk_belge_tarihi,cha_ilk_belge_doviz_kuru,cha_HareketGrupKodu1,cha_HareketGrupKodu2,cha_HareketGrupKodu3)
Select 0, 0, 0, 51, 0, 0, 0, 0, 
@UserNo, @Tarih, @UserNo, @Tarih, 
'','','',
0,--cha_firmano
0,--cha_subeno
convert(char(10),@Tarih,102), 
@cha_tip,@cha_cinsi,@cha_normal_Iade,@cha_evrak_tip,
0, @SeriNo, @SiraNo, @BelgeNo, convert(char(10),@Tarih,102), 
0, @cha_kod, 0, '', 
@cha_d_cins,
@cha_d_kur,
@cha_altd_kur,
0, 
isnull(@cha_meblag,0),
isnull(@cha_vade,0),
'1899-12-30 00:00:00', 0, 
isnull(@cha_ft_iskonto1,0),isnull(@cha_ft_iskonto2,0),isnull(@cha_ft_iskonto3,0),
isnull(@cha_ft_iskonto4,0),isnull(@cha_ft_iskonto5,0),isnull(@cha_ft_iskonto6,0),
isnull(@cha_ft_masraf1,0),isnull(@cha_ft_masraf2,0),isnull(@cha_ft_masraf3,0),
isnull(@cha_ft_masraf4,0),
0,0,0,0,0,0,0,0,0,0,
0,0,0,0,0,0,0,0,0,0,
@cha_vergi1,@cha_vergi2,@cha_vergi3,@cha_vergi4,
@cha_vergi5,@cha_vergi6,@cha_vergi7,@cha_vergi8,@cha_vergi9,@cha_vergi10,
0, 0, '', '', 0, 0, 1, 0, 
isnull(@cha_srmrkkodu,''),
'1899-12-30 00:00:00', --cha_reftarihi
0, 
isnull(@cha_meblag,0), 
0, 0, 0, 0, 0, 0, 
isnull(@cha_satici_kodu,''), 
0, 0, '1899-12-30 00:00:00', 0, 0, 
0, '', '', --cha_EXIMkodu
0, 0, 0, '', --cha_projekodu 
'', '', 0, 0, 0, 0, 0, 0, 0,
cast((0x0) as uniqueidentifier), cast((0x0) as uniqueidentifier), 0, 0  --*
,0,0,0,0,0,0,0,0,0,0,0,0,'','',0,0,'1899-12-30 00:00:00',0,'','',''



Declare @recno as uniqueidentifier
Select @recno=cha_Guid
from dbo.CARI_HESAP_HAREKETLERI
where cha_evrakno_seri=@SeriNo and
cha_evrakno_sira=@SiraNo and
cha_belge_no=@BelgeNo and
cha_tip=@cha_tip and
cha_cinsi=@cha_cinsi and
cha_normal_Iade=@cha_normal_Iade and
cha_evrak_tip=@cha_evrak_tip

Update dbo.STOK_HAREKETLERI
set sth_fat_uid=@recno
where sth_tip=@tip and
sth_cins=@cins and
sth_normal_iade=@normal and
sth_evraktip=@evtip and
sth_evrakno_seri=@SeriNo and
sth_evrakno_sira=@SiraNo and
sth_belge_no=@BelgeNo
END

ELSE IF @KayitKontrol>0
BEGIN
UPDATE dbo.CARI_HESAP_HAREKETLERI set 
cha_meblag=isnull(@cha_meblag,0),
cha_vade=isnull(@cha_vade,0),
cha_ft_iskonto1=isnull(@cha_ft_iskonto1,0),
cha_ft_iskonto2=isnull(@cha_ft_iskonto2,0),
cha_ft_iskonto3=isnull(@cha_ft_iskonto3,0),
cha_ft_iskonto4=isnull(@cha_ft_iskonto4,0),
cha_ft_iskonto5=isnull(@cha_ft_iskonto5,0),
cha_ft_iskonto6=isnull(@cha_ft_iskonto6,0),
cha_ft_masraf1=isnull(@cha_ft_masraf1,0),
cha_ft_masraf2=isnull(@cha_ft_masraf2,0),
cha_ft_masraf3=isnull(@cha_ft_masraf3,0),
cha_ft_masraf4=isnull(@cha_ft_masraf4,0),
cha_vergi1=@cha_vergi1,
cha_vergi2=@cha_vergi2,
cha_vergi3=@cha_vergi3,
cha_vergi4=@cha_vergi4,
cha_vergi5=@cha_vergi5,
cha_vergi6=@cha_vergi6,
cha_vergi7=@cha_vergi7,
cha_vergi8=@cha_vergi8,
cha_vergi9=@cha_vergi9,
cha_vergi10=@cha_vergi10,
cha_aratoplam=isnull(@cha_meblag,0)
where cha_evrakno_seri=@SeriNo and
cha_evrakno_sira=@SiraNo and
cha_belge_no=@BelgeNo and
cha_tip=@cha_tip and
cha_cinsi=@cha_cinsi and
cha_normal_Iade=@cha_normal_Iade and
cha_evrak_tip=@cha_evrak_tip

END

END
GO

If exists(select name from sysobjects where name='A_sp_SK_StokHar_Kaydet_2')
	DROP Procedure A_sp_SK_StokHar_Kaydet_2
GO

CREATE Procedure A_sp_SK_StokHar_Kaydet_2(
@No as integer,			--Evrak Tipini Belirler
@sth_evrakno_seri as varchar(6),--Seri No
@sth_evrakno_sira as integer,	--Sýra No
@SipTip as tinyint,		--Sipariþ Tip No
@SipSeriNo as varchar(6),	--Sipariþ Seri No
@SipSiraNo as integer,		--Sipariþ Sýra No
@Miktar as float,		--Teslim Miktarý
@SatirNo as integer,		--Satýr No
@sth_stok_kod as varchar (25),	--Stok Kod
@sth_parti_kodu as varchar (25),--Parti Kodu
@sth_lot_no as int,            	--Lot No
@sth_plasiyer_kodu varchar (25),
@sth_srm_merkezi varchar (10),
@DepoNo as integer,
@SSipRecNo as varchar(36),
@SatirNoGeri as integer OUTPUT)           
with encryption  
AS
BEGIN

Declare @sth_tip as tinyint
Declare @sth_cins as tinyint
Declare @sth_normal_iade as tinyint
Declare @sth_evraktip as tinyint
Declare @sth_create_user smallint 
Declare @sth_cari_kodu varchar (25)
Declare @sth_har_doviz_cinsi tinyint
Declare @sth_har_doviz_kuru float
Declare @sth_alt_doviz_kuru float
Declare @sth_odeme_op int
Declare @sth_aciklama varchar (50)
Declare @sth_vergisiz_fl bit
Declare @sth_adres_no int
-- stok döviz cinsi ve kuru bulunur
Declare @sth_stok_doviz_cinsi tinyint
Declare @sth_stok_doviz_kuru float
-- depo numaralarý evrak tipine göre belirlenir
Declare @sth_giris_depo_no int
Declare @sth_cikis_depo_no int
Declare @BirimF float
Declare @Tutar float
Declare @sth_vergi_pntr tinyint
Declare @sth_vergi float

Declare @sth_otvvergisiz_fl as bit
Declare @sth_isemri_gider_kodu as varchar(25)
Set @sth_isemri_gider_kodu=''
Declare @SipRecNo uniqueidentifier
Set @SipRecNo=Cast(@SSipRecNo as uniqueidentifier)
Declare @sth_utsbildirimturu as tinyint

IF @No=4
BEGIN
Select @sth_create_user=ssip_create_user,
@sth_cari_kodu='',
@sth_odeme_op=0,	
@sth_giris_depo_no=ssip_girdepo,
@sth_cikis_depo_no=ssip_cikdepo,
@sth_aciklama=ssip_aciklama,
@sth_vergisiz_fl=0,
@sth_har_doviz_cinsi=0,
@sth_har_doviz_kuru=0,
@sth_alt_doviz_kuru=1,
@sth_adres_no=1,
@sth_vergi_pntr=0,
@sth_vergi=0,
@sth_otvvergisiz_fl=0 
from dbo.DEPOLAR_ARASI_SIPARISLER 
where 
ssip_kapat_fl = 0 and
ssip_evrakno_seri=@SipSeriNo and
ssip_evrakno_sira=@SipSiraNo and
--ssip_satirno=0
ssip_Guid=@SipRecNo
END
ELSE IF @No=5
BEGIN
Select @sth_create_user=sip_create_user,
@sth_cari_kodu=sip_musteri_kod,
@sth_odeme_op=sip_opno,	
@sth_giris_depo_no=sip_depono,
@sth_cikis_depo_no=sip_depono,
@sth_aciklama=sip_aciklama,
@sth_vergisiz_fl=sip_vergisiz_fl,
@sth_vergi_pntr=sip_vergi_pntr,
@sth_vergi=(sip_vergi*@Miktar)/sip_miktar,
@sth_har_doviz_cinsi=sip_doviz_cinsi,
@sth_har_doviz_kuru=sip_doviz_kuru,
@sth_alt_doviz_kuru=sip_alt_doviz_kuru,
@sth_adres_no=sip_adresno,
@sth_otvvergisiz_fl=sip_OtvVergisiz_Fl 
from dbo.SIPARISLER 
where sip_tip=1 and 
sip_cins=3 and 
sip_OnaylayanKulNo > 0 and 
sip_kapat_fl = 0 and
sip_evrakno_seri=@SipSeriNo and
sip_evrakno_sira=@SipSiraNo and
--sip_satirno=0
sip_Guid=@SipRecNo
END
ELSE IF @No=6
BEGIN
Select @sth_create_user=sip_create_user,
@sth_cari_kodu=sip_musteri_kod,
@sth_odeme_op=sip_opno,	
@sth_giris_depo_no=sip_depono,
@sth_cikis_depo_no=sip_depono,
@sth_aciklama=sip_aciklama,
@sth_vergisiz_fl=sip_vergisiz_fl,
@sth_vergi_pntr=sip_vergi_pntr,
@sth_vergi=(sip_vergi*@Miktar)/sip_miktar,
@sth_har_doviz_cinsi=sip_doviz_cinsi,
@sth_har_doviz_kuru=sip_doviz_kuru,
@sth_alt_doviz_kuru=sip_alt_doviz_kuru,
@sth_adres_no=sip_adresno,
@sth_otvvergisiz_fl=sip_OtvVergisiz_Fl 
from dbo.SIPARISLER 
where sip_tip=0 and 
sip_cins=3 and 
sip_OnaylayanKulNo > 0 and 
sip_kapat_fl = 0 and
sip_evrakno_seri=@SipSeriNo and
sip_evrakno_sira=@SipSiraNo and
--sip_satirno=0
sip_Guid=@SipRecNo
END
ELSE IF @No=11
BEGIN
Select @sth_create_user=sip_create_user,
@sth_cari_kodu=sip_musteri_kod,
@sth_odeme_op=sip_opno,	
@sth_giris_depo_no=sip_depono,
@sth_cikis_depo_no=sip_depono,
@sth_aciklama=sip_aciklama,
@sth_vergisiz_fl=sip_vergisiz_fl,
@sth_vergi_pntr=sip_vergi_pntr,
@sth_vergi=(sip_vergi*@Miktar)/sip_miktar,
@sth_har_doviz_cinsi=sip_doviz_cinsi,
@sth_har_doviz_kuru=sip_doviz_kuru,
@sth_alt_doviz_kuru=sip_alt_doviz_kuru,
@sth_adres_no=sip_adresno,
@sth_otvvergisiz_fl=sip_OtvVergisiz_Fl 
from dbo.SIPARISLER 
where sip_tip=0 and 
sip_cins=1 and 
sip_OnaylayanKulNo > 0 and 
sip_kapat_fl = 0 and
sip_evrakno_seri=@SipSeriNo and
sip_evrakno_sira=@SipSiraNo and
--sip_satirno=0
sip_Guid=@SipRecNo
END
ELSE IF @No=12
BEGIN
Select @sth_create_user=upl_create_user,
@sth_cari_kodu='',
@sth_odeme_op=0,	
@sth_giris_depo_no=upl_depno,
@sth_cikis_depo_no=upl_depno,
@sth_aciklama='',
@sth_vergisiz_fl=0,
@sth_vergi_pntr=0,
@sth_vergi=0,
@sth_har_doviz_cinsi=0,
@sth_har_doviz_kuru=1,
@sth_alt_doviz_kuru=1,
@sth_adres_no=0,
@sth_otvvergisiz_fl=0 ,
@sth_isemri_gider_kodu=upl_isemri
from dbo.URETIM_MALZEME_PLANLAMA 
where upl_Guid=@SipRecNo
END
ELSE
BEGIN
Select @sth_create_user=sip_create_user,
@sth_cari_kodu=sip_musteri_kod,
@sth_odeme_op=sip_opno,	
@sth_giris_depo_no=sip_depono,
@sth_cikis_depo_no=sip_depono,
@sth_aciklama=sip_aciklama,
@sth_vergisiz_fl=sip_vergisiz_fl,
@sth_vergi_pntr=sip_vergi_pntr,
@sth_vergi=(sip_vergi*@Miktar)/sip_miktar,
@sth_har_doviz_cinsi=sip_doviz_cinsi,
@sth_har_doviz_kuru=sip_doviz_kuru,
@sth_alt_doviz_kuru=sip_alt_doviz_kuru,
@sth_adres_no=sip_adresno,
@sth_otvvergisiz_fl=sip_OtvVergisiz_Fl 
from dbo.SIPARISLER 
where sip_tip=@SipTip and 
sip_cins=0 and 
sip_OnaylayanKulNo > 0 and 
sip_kapat_fl = 0 and
sip_evrakno_seri=@SipSeriNo and
sip_evrakno_sira=@SipSiraNo and
--sip_satirno=0
sip_Guid=@SipRecNo
END


--Stok Döviz Cinsi ve Döviz Kuru Atamasý
Select @sth_stok_doviz_cinsi=sto_doviz_cinsi
from dbo.STOKLAR 
where sto_kod=@sth_stok_kod
SET @sth_stok_doviz_kuru=dbo.A_D_fn_StokDovizKuruBul(@sth_stok_kod,@sth_cari_kodu)

-- Depo Atamasý  & Evrak Belirleyici Deðiþken Atamalarý
IF @No=1 -- TAÝ
BEGIN
SET @sth_giris_depo_no=@DepoNo
SET @sth_cikis_depo_no=1
SET @sth_tip=0
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=13
SET @sth_utsbildirimturu=1
END
ELSE IF @No=16 -- ATAF
BEGIN
SET @sth_giris_depo_no=@DepoNo
SET @sth_cikis_depo_no=1
SET @sth_tip=0
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=3
SET @sth_utsbildirimturu=1
END
ELSE IF @No=2 --TSÝ
BEGIN

SET @sth_cikis_depo_no=@DepoNo --@sth_giris_depo_no
SET @sth_giris_depo_no=1
SET @sth_tip=1
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=1
SET @sth_utsbildirimturu=10
END
ELSE IF @No=3 --ATSF
BEGIN
SET @sth_cikis_depo_no=@DepoNo
SET @sth_giris_depo_no=1

SET @sth_tip=1
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=4	
SET @sth_utsbildirimturu=10
END
ELSE IF @No=4 --DASF
BEGIN
SET @sth_tip=2
SET @sth_cins=6
SET @sth_normal_iade=0
SET @sth_evraktip=2	
SET @sth_utsbildirimturu=0
Set @SipRecNo=cast((0x0) as uniqueidentifier)
END
ELSE IF @No=5 -- ÝÝ ÝTHALAT
BEGIN
SET @sth_giris_depo_no=@DepoNo
SET @sth_cikis_depo_no=1
SET @sth_tip=0
SET @sth_cins=12
SET @sth_normal_iade=0
SET @sth_evraktip=13
SET @sth_utsbildirimturu=6
END
ELSE IF @No=6 -- ÝÝ ÝHRACAT
BEGIN
SET @sth_cikis_depo_no=@DepoNo
SET @sth_giris_depo_no=1
SET @sth_tip=1
SET @sth_cins=12
SET @sth_normal_iade=0
SET @sth_evraktip=1
SET @sth_utsbildirimturu=2
END
ELSE IF @No=11 --KSÝ
BEGIN

SET @sth_cikis_depo_no=@DepoNo
SET @sth_giris_depo_no=1
SET @sth_tip=1
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=1
SET @sth_utsbildirimturu=10
END
ELSE IF @No=12 --UCF
BEGIN

SET @sth_cikis_depo_no=@DepoNo --@sth_giris_depo_no
SET @sth_giris_depo_no=1
SET @sth_tip=1
SET @sth_cins=7
SET @sth_normal_iade=0
SET @sth_evraktip=0
SET @sth_utsbildirimturu=10
Set @SipRecNo=cast((0x0) as uniqueidentifier)
END

Declare @sth_exim_kodu as varchar(25)
Declare @sth_disticaret_turu as tinyint
Declare @sth_oivvergisiz_fl as tinyint
set @sth_oivvergisiz_fl=0

IF @No=5
BEGIN
Select @sth_exim_kodu=sip_Exp_Imp_Kodu
from dbo.SIPARISLER 
where sip_tip=1 and 
sip_cins=3 and 
sip_OnaylayanKulNo > 0 and 
sip_kapat_fl = 0 and
sip_evrakno_seri=@SipSeriNo and
sip_evrakno_sira=@SipSiraNo and
--sip_satirno=0
sip_Guid=@SipRecNo
set @sth_disticaret_turu=3
set @sth_oivvergisiz_fl=1
END
ELSE IF @No=6
BEGIN
Select @sth_exim_kodu=sip_Exp_Imp_Kodu
from dbo.SIPARISLER 
where sip_tip=0 and 
sip_cins=3 and 
sip_OnaylayanKulNo > 0 and 
sip_kapat_fl = 0 and
sip_evrakno_seri=@SipSeriNo and
sip_evrakno_sira=@SipSiraNo and
--sip_satirno=0
sip_Guid=@SipRecNo
set @sth_disticaret_turu=3
set @sth_oivvergisiz_fl=1
END
ELSE
BEGIN
set @sth_exim_kodu=''
set @sth_disticaret_turu=0
END

Select @BirimF=dbo.A_fn_BirimFiyat(@sth_cari_kodu,@sth_stok_kod,getdate(),@sth_cikis_depo_no)
Set	@Tutar=@Miktar *@BirimF

if @Tutar is null Set @Tutar=0

declare @knt as int
declare @tplan as float

declare @Kontrol as int
declare @Kontrol2 as int
Select @Kontrol2=Count(sth_satirno) From STOK_HAREKETLERI Where 
sth_tip=@sth_tip And 
sth_cins=@sth_cins And
sth_normal_iade=@sth_normal_iade And
sth_evraktip=@sth_evraktip And
sth_evrakno_seri=@sth_evrakno_seri And
sth_evrakno_sira=@sth_evrakno_sira And
sth_sip_uid =@SipRecNo And
sth_stok_kod=@sth_stok_kod and
(
((Select sto_detay_takip from dbo.STOKLAR where sto_kod=@sth_stok_kod)=2
and sth_parti_kodu=@sth_parti_kodu AND sth_lot_no=@sth_lot_no)
or ((Select sto_detay_takip from dbo.STOKLAR where sto_kod=@sth_stok_kod)=1
and sth_parti_kodu=@sth_parti_kodu)
or ((Select sto_detay_takip from dbo.STOKLAR where sto_kod=@sth_stok_kod)=0)
)


Select @Kontrol=sth_satirno From STOK_HAREKETLERI Where 
sth_tip=@sth_tip And 
sth_cins=@sth_cins And
sth_normal_iade=@sth_normal_iade And
sth_evraktip=@sth_evraktip And
sth_evrakno_seri=@sth_evrakno_seri And
sth_evrakno_sira=@sth_evrakno_sira And
sth_sip_uid=@SipRecNo And
sth_stok_kod=@sth_stok_kod

IF @No=12
BEGIN 
Select @Kontrol2=Count(sth_satirno) From STOK_HAREKETLERI Where 
sth_tip=@sth_tip And 
sth_cins=@sth_cins And
sth_normal_iade=@sth_normal_iade And
sth_evraktip=@sth_evraktip And
sth_evrakno_seri=@sth_evrakno_seri And
sth_evrakno_sira=@sth_evrakno_sira And
sth_isemri_gider_kodu =@sth_isemri_gider_kodu And
sth_stok_kod=@sth_stok_kod and
(
((Select sto_detay_takip from dbo.STOKLAR where sto_kod=@sth_stok_kod)=2
and sth_parti_kodu=@sth_parti_kodu AND sth_lot_no=@sth_lot_no)
or ((Select sto_detay_takip from dbo.STOKLAR where sto_kod=@sth_stok_kod)=1
and sth_parti_kodu=@sth_parti_kodu)
or ((Select sto_detay_takip from dbo.STOKLAR where sto_kod=@sth_stok_kod)=0)
)


Select @Kontrol=sth_satirno From STOK_HAREKETLERI Where 
sth_tip=@sth_tip And 
sth_cins=@sth_cins And
sth_normal_iade=@sth_normal_iade And
sth_evraktip=@sth_evraktip And
sth_evrakno_seri=@sth_evrakno_seri And
sth_evrakno_sira=@sth_evrakno_sira And
sth_isemri_gider_kodu=@sth_isemri_gider_kodu And
sth_stok_kod=@sth_stok_kod
END
ELSE IF @No=11
BEGIN
Select @Kontrol2=Count(kon_satirno) From KONSINYE_HAREKETLERI Where 
kon_tip=@sth_tip And 
--sth_cins=@sth_cins And
kon_normal_iade=@sth_normal_iade And
kon_evraktip=@sth_evraktip And
kon_evrakno_seri=@sth_evrakno_seri And
kon_evrakno_sira=@sth_evrakno_sira And
kon_sip_uid =@SipRecNo And
kon_stok_kod=@sth_stok_kod and
(
((Select sto_detay_takip from dbo.STOKLAR where sto_kod=@sth_stok_kod)=2
and kons_parti_kodu=@sth_parti_kodu AND kons_lot_no=@sth_lot_no)
or ((Select sto_detay_takip from dbo.STOKLAR where sto_kod=@sth_stok_kod)=1
and kons_parti_kodu=@sth_parti_kodu)
or ((Select sto_detay_takip from dbo.STOKLAR where sto_kod=@sth_stok_kod)=0)
)

Select @Kontrol=kon_satirno From KONSINYE_HAREKETLERI Where 
kon_tip=@sth_tip And 
--sth_cins=@sth_cins And
kon_normal_iade=@sth_normal_iade And
kon_evraktip=@sth_evraktip And
kon_evrakno_seri=@sth_evrakno_seri And
kon_evrakno_sira=@sth_evrakno_sira And
kon_sip_uid=@SipRecNo And
kon_stok_kod=@sth_stok_kod

END

if @Kontrol2>0
Begin

IF @No=11
BEGIN

Update dbo.KONSINYE_HAREKETLERI
set 
kon_miktar=kon_miktar + @Miktar,
kon_miktar2=kon_miktar2 + @Miktar,
kons_tutar=kons_tutar + @Tutar

where kon_tip=@sth_tip And 
--sth_cins=@sth_cins And
kon_normal_iade=@sth_normal_iade And
kon_evraktip=@sth_evraktip And
kon_evrakno_seri=@sth_evrakno_seri And
kon_evrakno_sira=@sth_evrakno_sira And
kon_sip_uid=@SipRecNo And
kon_stok_kod=@sth_stok_kod
 
Set @SatirNoGeri=@Kontrol

END
ELSE IF @No=12
BEGIN

Update dbo.STOK_HAREKETLERI
set 
sth_miktar=sth_miktar + @Miktar,
sth_miktar2=sth_miktar2 + @Miktar,
sth_tutar=sth_tutar + @Tutar

where sth_tip=@sth_tip And 
sth_cins=@sth_cins And
sth_normal_iade=@sth_normal_iade And
sth_evraktip=@sth_evraktip And
sth_evrakno_seri=@sth_evrakno_seri And
sth_evrakno_sira=@sth_evrakno_sira And
sth_isemri_gider_kodu=@sth_isemri_gider_kodu And
sth_stok_kod=@sth_stok_kod
 
Set @SatirNoGeri=@Kontrol



SELECT @knt=Count(*) FROM ISEMRI_MALZEME_DURUMLARI WITH ( NOLOCK , INDEX = NDX_ISEMRI_MALZEME_DURUMLARI_02 )  
WHERE (ish_stok_hizm_gider=0 AND ish_stokhizm_gid_kod=@sth_stok_kod AND ish_isemri=@sth_isemri_gider_kodu AND ish_fasoncu='')

if @knt>0
Begin
UPDATE ISEMRI_MALZEME_DURUMLARI SET ish_lastup_date=getdate(),ish_sevk_miktar=ish_sevk_miktar+@Miktar 
WHERE (ish_stok_hizm_gider=0 AND ish_stokhizm_gid_kod=@sth_stok_kod AND ish_isemri=@sth_isemri_gider_kodu AND ish_fasoncu='')
End
Else
Begin


Select @tplan=upl_miktar from URETIM_MALZEME_PLANLAMA where upl_isemri=@sth_isemri_gider_kodu and upl_kodu=@sth_stok_kod and upl_uretim_tuket=0

INSERT INTO dbo.ISEMRI_MALZEME_DURUMLARI
(ish_DBCno,ish_SpecRecNo,ish_iptal,ish_fileid,ish_hidden,ish_kilitli,ish_degisti,ish_checksum,ish_create_user,ish_create_date,ish_lastup_user,ish_lastup_date,ish_ozelkod1,ish_ozelkod2,ish_ozelkod3
,ish_stok_hizm_gider,ish_stokhizm_gid_kod,ish_isemri,ish_fasoncu,ish_sevk_miktar
,ish_sevk_deger0,ish_sevk_deger1,ish_sevk_deger2,ish_iade_miktar,ish_iade_deger0,ish_iade_deger1,ish_iade_deger2,ish_tuket_miktar,ish_tuket_deger0,ish_tuket_deger1,ish_tuket_deger2
,ish_uret_miktar,ish_uret_deger0,ish_uret_deger1,ish_uret_deger2,ish_uretiade_miktar,ish_uretiade_deg0,ish_uretiade_deg1,ish_uretiade_deg2
,ish_plan_sevkmiktar,ish_planuretim,ish_GenelUretimMaliyeti_Ana,ish_GenelUretimMaliyeti_Alt,ish_GenelUretimMaliyeti_Orj,ish_DirektIscilikMaliyeti_Ana,ish_DirektIscilikMaliyeti_Alt,ish_DirektIscilikMaliyeti_Orj)
VALUES
(0,0,0,1013,0,0,0,0,@sth_create_user, getdate(),@sth_create_user, getdate(),'','',''
,0,@sth_stok_kod,@sth_isemri_gider_kodu,'',@Miktar
,0,0,0,0,0,0,0,0,0,0,0
,0,0,0,0,0,0,0,0
,@tplan,0,0,0,0,0,0,0)

End

END
ELSE
BEGIN

Update dbo.STOK_HAREKETLERI
set 
sth_miktar=sth_miktar + @Miktar,
sth_miktar2=sth_miktar2 + @Miktar,
sth_tutar=sth_tutar + @Tutar

where sth_tip=@sth_tip And 
sth_cins=@sth_cins And
sth_normal_iade=@sth_normal_iade And
sth_evraktip=@sth_evraktip And
sth_evrakno_seri=@sth_evrakno_seri And
sth_evrakno_sira=@sth_evrakno_sira And
sth_sip_uid=@SipRecNo And
sth_stok_kod=@sth_stok_kod
 
Set @SatirNoGeri=@Kontrol

END

End
Else
Begin

Declare @sth_recNo uniqueidentifier
Set @sth_recNo=NEWID()

IF @No=11
BEGIN


INSERT INTO KONSINYE_HAREKETLERI
(kon_Guid,kon_DBCno,kon_SpecRecno,kon_iptal,kon_fileid,kon_hidden,kon_kilitli,kon_degisti,kon_checksum,kon_create_user,kon_create_date,kon_lastup_user,kon_lastup_date
,kon_special1,kon_special2,kon_special3,kon_firmano,kon_subeno,kon_tarih
,kon_tip,kon_normal_iade,kon_evrakno_seri,kon_evrakno_sira,kon_satirno,kon_belge_no,kon_belge_tarih
,kon_stok_kod,kon_cari_kod,kon_satici_kod,kon_miktar,kon_faturalanan,kon_aciklama,kon_giris_depo_no,kon_cikis_depo_no
,kon_malkabul_tarih,kon_sip_uid,kon_islemgoren,kon_karkon_uid,kon_netagirlik,kon_brutagirlik,kon_rehinmiktari,kon_rehinfiyati
,kon_miktar2,kon_islemgoren2,kon_sandikmiktari,kon_sandikfiyati,kon_sevk_adresno,kon_cari_srm_merkez,kon_stok_srm_merkez
,kons_parti_kodu,kons_lot_no,kons_projekodu,kons_har_doviz_cinsi,kons_har_doviz_kuru,kons_alt_doviz_kuru,kons_stok_doviz_cinsi,kons_stok_doviz_kuru
,kons_odeme_op,kons_birim_pntr,kons_tutar
,kons_isk_mas1,kons_isk_mas2,kons_isk_mas3,kons_isk_mas4,kons_isk_mas5,kons_isk_mas6,kons_isk_mas7,kons_isk_mas8,kons_isk_mas9,kons_isk_mas10
,kons_sat_iskmas1,kons_sat_iskmas2,kons_sat_iskmas3,kons_sat_iskmas4,kons_sat_iskmas5,kons_sat_iskmas6,kons_sat_iskmas7,kons_sat_iskmas8,kons_sat_iskmas9,kons_sat_iskmas10
,kons_iskonto1,kons_iskonto2,kons_iskonto3,kons_iskonto4,kons_iskonto5,kons_iskonto6,kons_masraf1,kons_masraf2,kons_masraf3,kons_masraf4
,kons_vergi_pntr,kons_vergi,kons_masraf_vergi_pntr,kons_masraf_vergi,kons_vergisiz_fl
,kons_otv_pntr,kons_otv_vergi,kons_otvtutari,kons_otvvergisiz_fl,kons_oiv_pntr,kons_oiv_vergi,kons_oivvergisiz_fl
,kons_fiyat_liste_no,kon_cins,kon_evraktip,kon_gider_kodu,kons_oivtutari,kon_irs_uid,kon_yetkili_uid,kon_nakliyedeposu,kon_nakliyedurumu)
VALUES(@sth_recNo,0, 0, 0, 46, 0, 0, 0, 0, 
@sth_create_user, getdate(), @sth_create_user, getdate(),
'','','',0,0, convert(char(10),getdate(),102), 
@sth_tip,@sth_normal_iade,  --@sth_cins,
@sth_evrakno_seri,@sth_evrakno_sira,@SatirNo, '',convert(char(10),getdate(),102), 
@sth_stok_kod, @sth_cari_kodu, '', @Miktar, 0, @sth_aciklama, @sth_giris_depo_no, @sth_cikis_depo_no,
convert(char(10),getdate(),102),@SipRecNo, 0, cast((0x0) as uniqueidentifier), 0,0,0,0, 
@Miktar,0,0,0,@sth_adres_no,'',@sth_srm_merkezi,
@sth_parti_kodu, @sth_lot_no, '', @sth_har_doviz_cinsi, @sth_har_doviz_kuru, @sth_alt_doviz_kuru, @sth_stok_doviz_cinsi, @sth_stok_doviz_kuru,
@sth_odeme_op,1,@Tutar,
1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 
0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
@sth_vergi_pntr,@sth_vergi, 0, 0, 0, 
0, 0, 0, @sth_otvvergisiz_fl, 0, 0, @sth_oivvergisiz_fl,
1, 0, @sth_evraktip, '', 0, cast((0x0) as uniqueidentifier), cast((0x0) as uniqueidentifier), 0, 0)

Set @SatirNoGeri=@SatirNo

END
ELSE
BEGIN

INSERT INTO dbo.STOK_HAREKETLERI(sth_Guid,sth_DBCno, sth_SpecRECno, sth_iptal, sth_fileid, sth_hidden, sth_kilitli, sth_degisti, sth_checksum, sth_create_user, sth_create_date, sth_lastup_user, sth_lastup_date, sth_special1, sth_special2, sth_special3, sth_firmano, sth_subeno, sth_tarih, sth_tip, sth_cins, sth_normal_iade, sth_evraktip, sth_evrakno_seri, sth_evrakno_sira, sth_satirno, sth_belge_no, sth_belge_tarih, sth_stok_kod, sth_isk_mas1, sth_isk_mas2, sth_isk_mas3, sth_isk_mas4, sth_isk_mas5, sth_isk_mas6, sth_isk_mas7, sth_isk_mas8, sth_isk_mas9, sth_isk_mas10, sth_sat_iskmas1, sth_sat_iskmas2, sth_sat_iskmas3, sth_sat_iskmas4, sth_sat_iskmas5, sth_sat_iskmas6, sth_sat_iskmas7, sth_sat_iskmas8, sth_sat_iskmas9, sth_sat_iskmas10, 
sth_pos_satis, sth_promosyon_fl, sth_cari_cinsi, sth_cari_kodu, sth_cari_grup_no, 
sth_isemri_gider_kodu, sth_plasiyer_kodu, sth_har_doviz_cinsi, sth_har_doviz_kuru, sth_alt_doviz_kuru, sth_stok_doviz_cinsi, sth_stok_doviz_kuru, 
sth_miktar, sth_miktar2, sth_birim_pntr, sth_tutar, sth_iskonto1, sth_iskonto2, sth_iskonto3, sth_iskonto4, sth_iskonto5, sth_iskonto6, sth_masraf1, sth_masraf2, sth_masraf3, sth_masraf4, 
sth_vergi_pntr, sth_vergi, sth_masraf_vergi_pntr, sth_masraf_vergi, sth_netagirlik, sth_odeme_op, sth_aciklama, 
sth_sip_uid, sth_fat_uid, sth_giris_depo_no, sth_cikis_depo_no, sth_malkbl_sevk_tarihi, sth_cari_srm_merkezi, 
sth_stok_srm_merkezi, sth_fis_tarihi, sth_fis_sirano, sth_vergisiz_fl, sth_maliyet_ana, sth_maliyet_alternatif, sth_maliyet_orjinal, sth_adres_no, 
sth_parti_kodu, sth_lot_no, sth_kons_uid, 
sth_proje_kodu, sth_exim_kodu, sth_otv_pntr, sth_otv_vergi,  
sth_brutagirlik,sth_disticaret_turu, sth_otvtutari, sth_otvvergisiz_fl, 
sth_oiv_pntr, sth_oiv_vergi, sth_oivvergisiz_fl,sth_fiyat_liste_no,
sth_oivtutari,sth_Tevkifat_turu,
sth_nakliyedeposu,sth_nakliyedurumu,
sth_yetkili_uid,sth_taxfree_fl
,sth_ilave_edilecek_kdv,sth_ismerkezi_kodu,sth_HareketGrupKodu1,sth_HareketGrupKodu2,sth_HareketGrupKodu3,sth_Olcu1,sth_Olcu2,sth_Olcu3,sth_Olcu4,sth_Olcu5,sth_FormulMiktarNo,sth_FormulMiktar)
VALUES(@sth_recNo,0, 0, 0, 16, 0, 0, 0, 0, 
@sth_create_user, getdate(), @sth_create_user, getdate(),
'','','',0,0, convert(char(10),getdate(),102), 
@sth_tip,@sth_cins,@sth_normal_iade,@sth_evraktip,
@sth_evrakno_seri,@sth_evrakno_sira,@SatirNo, '',
convert(char(10),getdate(),102), 
@sth_stok_kod, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 
0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, @sth_cari_kodu, 0, 
@sth_isemri_gider_kodu, @sth_plasiyer_kodu, 
@sth_har_doviz_cinsi, @sth_har_doviz_kuru, 
@sth_alt_doviz_kuru, @sth_stok_doviz_cinsi, 
@sth_stok_doviz_kuru, @Miktar, @Miktar, 1, @Tutar, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
0, 
@sth_vergi_pntr,@sth_vergi, 0, 0, 0, @sth_odeme_op, @sth_aciklama, 
@SipRecNo, cast((0x0) as uniqueidentifier), @sth_giris_depo_no, @sth_cikis_depo_no, 
convert(char(10),getdate(),102), @sth_srm_merkezi, 
'', '1899-12-30 00:00:00', 0, @sth_vergisiz_fl, 0, 0, 0, @sth_adres_no, 
@sth_parti_kodu, @sth_lot_no, cast((0x0) as uniqueidentifier), 
'', @sth_exim_kodu, 0, 0, 
0,@sth_disticaret_turu, 0, @sth_otvvergisiz_fl, 
0, 0, @sth_oivvergisiz_fl,1, --sth_fiyat_liste_no
0,0,
0,0,
cast((0x0) as uniqueidentifier),0
,0,'','','','',0,0,0,0,0,0,0)

INSERT INTO dbo.STOK_HAREKETLERI_EK
(sthek_DBCno,sthek_SpecRECno,sthek_iptal,sthek_fileid,sthek_hidden,sthek_kilitli,sthek_degisti,sthek_checksum,sthek_create_user,sthek_create_date,sthek_lastup_user,sthek_lastup_date,sthek_special1,sthek_special2,sthek_special3,
sthek_related_uid,sth_subesip_uid,sth_bkm_uid,sth_karsikons_uid,sth_rez_uid,sth_optamam_uid,sth_iadeTlp_uid,sth_HalSatis_uid,
sth_ciroprim_uid,sth_iade_evrak_seri,sth_iade_evrak_sira,sth_yat_tes_kodu,sth_ihracat_kredi_kodu,sth_diib_belge_no,sth_diib_satir_no,sth_mensey_ulke_tipi,sth_mensey_ulke_kodu,sth_halrehmiktari,sth_halrehfiyati,sth_halsandikmiktari,sth_halsandikfiyati,
sth_halsandikkdvtutari,sth_HalKomisyonuKdv,sth_HalRusum,sth_satistipi,sth_vardiya_tarihi,sth_vardiya_no,sth_direkt_iscilik_1,sth_direkt_iscilik_2,sth_direkt_iscilik_3,sth_direkt_iscilik_4,sth_direkt_iscilik_5,sth_genel_uretim_1,sth_genel_uretim_2,sth_genel_uretim_3,sth_genel_uretim_4,sth_genel_uretim_5,sth_fis_tarihi2,sth_fis_sirano2,sth_fiyfark_esas_evrak_seri,sth_fiyfark_esas_evrak_sira,sth_fiyfark_esas_satir_no
,sth_istisna,sth_otv_tevkifat_turu,sth_otv_tevkifat_tutari,sth_servishar_uid,sth_bakimsarf_uid
,sth_utsbildirimturu,sth_utshekzayiatturu,sth_utsimhabertarafgerekcesi,sth_utsdigergerekceaciklamasi)
VALUES
(0,0,0,590,0,0,0,0,@sth_create_user,getdate(),@sth_create_user,getdate(),'','','',
@sth_recNo,
cast((0x0) as uniqueidentifier),
cast((0x0) as uniqueidentifier),cast((0x0) as uniqueidentifier),cast((0x0) as uniqueidentifier),
cast((0x0) as uniqueidentifier),cast((0x0) as uniqueidentifier),cast((0x0) as uniqueidentifier),cast((0x0) as uniqueidentifier),'',0,'','','',0,0,'',0,0,0,0,0,0,0,0,'1899-12-30 00:00:00',
0,0,0,0,0,0,0,0,0,0,0,'1899-12-30 00:00:00',0,'',0,0
,'',0,0,cast((0x0) as uniqueidentifier),cast((0x0) as uniqueidentifier)
,@sth_utsbildirimturu,0,0,'')



Set @SatirNoGeri=@SatirNo

IF @No=12
Begin
SELECT @knt=Count(*) FROM ISEMRI_MALZEME_DURUMLARI WITH ( NOLOCK , INDEX = NDX_ISEMRI_MALZEME_DURUMLARI_02 )  
WHERE (ish_stok_hizm_gider=0 AND ish_stokhizm_gid_kod=@sth_stok_kod AND ish_isemri=@sth_isemri_gider_kodu AND ish_fasoncu='')

if @knt>0
Begin
UPDATE ISEMRI_MALZEME_DURUMLARI SET ish_lastup_date=getdate(),ish_sevk_miktar=ish_sevk_miktar+@Miktar 
WHERE (ish_stok_hizm_gider=0 AND ish_stokhizm_gid_kod=@sth_stok_kod AND ish_isemri=@sth_isemri_gider_kodu AND ish_fasoncu='')
End
Else
Begin


Select @tplan=upl_miktar from URETIM_MALZEME_PLANLAMA where upl_isemri=@sth_isemri_gider_kodu and upl_kodu=@sth_stok_kod and upl_uretim_tuket=0

INSERT INTO dbo.ISEMRI_MALZEME_DURUMLARI
(ish_DBCno,ish_SpecRecNo,ish_iptal,ish_fileid,ish_hidden,ish_kilitli,ish_degisti,ish_checksum,ish_create_user,ish_create_date,ish_lastup_user,ish_lastup_date,ish_ozelkod1,ish_ozelkod2,ish_ozelkod3
,ish_stok_hizm_gider,ish_stokhizm_gid_kod,ish_isemri,ish_fasoncu,ish_sevk_miktar
,ish_sevk_deger0,ish_sevk_deger1,ish_sevk_deger2,ish_iade_miktar,ish_iade_deger0,ish_iade_deger1,ish_iade_deger2,ish_tuket_miktar,ish_tuket_deger0,ish_tuket_deger1,ish_tuket_deger2
,ish_uret_miktar,ish_uret_deger0,ish_uret_deger1,ish_uret_deger2,ish_uretiade_miktar,ish_uretiade_deg0,ish_uretiade_deg1,ish_uretiade_deg2
,ish_plan_sevkmiktar,ish_planuretim,ish_GenelUretimMaliyeti_Ana,ish_GenelUretimMaliyeti_Alt,ish_GenelUretimMaliyeti_Orj,ish_DirektIscilikMaliyeti_Ana,ish_DirektIscilikMaliyeti_Alt,ish_DirektIscilikMaliyeti_Orj)
VALUES
(0,0,0,1013,0,0,0,0,@sth_create_user, getdate(),@sth_create_user, getdate(),'','',''
,0,@sth_stok_kod,@sth_isemri_gider_kodu,'',@Miktar
,0,0,0,0,0,0,0,0,0,0,0
,0,0,0,0,0,0,0,0
,@tplan,0,0,0,0,0,0,0)

End

End --12

END

End

END
GO

If exists(select name from sysobjects where name='A_sp_SK_BedenHar_Kaydet_2')
	DROP Procedure A_sp_SK_BedenHar_Kaydet_2
GO

CREATE Procedure A_sp_SK_BedenHar_Kaydet_2(
@No as integer,			--Evrak Tipini Belirler
@sth_evrakno_seri as varchar(6),--Seri No
@sth_evrakno_sira as integer,	--Sýra No
@SipTip as tinyint,		--Sipariþ Tip No
@SipSeriNo as varchar(6),	--Sipariþ Seri No
@SipSiraNo as integer,		--Sipariþ Sýra No
@Miktar as float,		--Teslim Miktarý
@StokKod as varchar(25),
@Renk as varchar(10),
@Beden as varchar(10),
@sth_satirno as integer, @VarGuid1 as uniqueidentifier = NULL)	--Satýr No
with encryption  
AS
BEGIN

Declare @sth_tip as tinyint
Declare @sth_cins as tinyint
Declare @sth_normal_iade as tinyint
Declare @sth_evraktip as tinyint
Declare @sth_create_user smallint 

Select @sth_create_user=sip_create_user
from dbo.SIPARISLER 
where sip_tip=@SipTip and 
sip_cins=0 and 
sip_OnaylayanKulNo > 0 and 
sip_kapat_fl = 0 and
sip_evrakno_seri=@SipSeriNo and
sip_evrakno_sira=@SipSiraNo and
sip_satirno=0

-- Evrak Belirleyici deðiþken Atamalarý
IF @No=1 -- TAÝ
BEGIN
SET @sth_tip=0
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=13
END
ELSE IF @No=2 --TSÝ
BEGIN
SET @sth_tip=1
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=1
END
ELSE IF @No=3 --ATSF
BEGIN
SET @sth_tip=1
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=4	
END
ELSE IF @No=16 -- ATAF
BEGIN
SET @sth_tip=0
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=3
END
ELSE IF @No=4 --DASF
BEGIN
SET @sth_tip=2
SET @sth_cins=6
SET @sth_normal_iade=0
SET @sth_evraktip=2	
END
ELSE IF @No=5 --II ÝTHALAT
BEGIN
SET @sth_tip=0
SET @sth_cins=12
SET @sth_normal_iade=0
SET @sth_evraktip=13	
END
ELSE IF @No=6 --II ÝHRACAT
BEGIN
SET @sth_tip=1
SET @sth_cins=12
SET @sth_normal_iade=0
SET @sth_evraktip=1
END

Declare @rn as uniqueidentifier -- Stok Hareketleri REC nosu
Select @rn=sth_Guid 
from dbo.STOK_HAREKETLERI
where sth_tip=@sth_tip and
sth_cins=@sth_cins and
sth_normal_iade=@sth_normal_iade and
sth_evraktip=@sth_evraktip and
sth_evrakno_seri=@sth_evrakno_seri and
sth_evrakno_sira=@sth_evrakno_sira and
sth_satirno=@sth_satirno 

INSERT INTO dbo.BEDEN_HAREKETLERI(BdnHar_Guid, BdnHar_DBCno, BdnHar_Spec_Rec_no, BdnHar_iptal, BdnHar_fileid, BdnHar_hidden, BdnHar_kilitli, BdnHar_degisti, BdnHar_checksum, BdnHar_create_user, BdnHar_create_date, BdnHar_lastup_user, BdnHar_lastup_date, BdnHar_special1, BdnHar_special2, BdnHar_special3, BdnHar_MainProgramNo, BdnHar_VersionNo, BdnHar_MenuNo, BdnHar_MikroSpecial1, BdnHar_MikroSpecial2, BdnHar_MikroSpecial3, BdnHar_ExternalProgramType, BdnHar_ExternalProgramId, BdnHar_Hash, BdnHar_Tipi, BdnHar_Har_uid, BdnHar_BedenNo, BdnHar_HarGor, BdnHar_KnsIsGor, BdnHar_KnsFat, BdnHar_TesMik, BdnHar_rezervasyon_miktari, BdnHar_rezerveden_teslim_edilen, BdnHar_VarSatirNo, BdnHar_VarBaglantiUId1, BdnHar_VarBaglantiUId2, BdnHar_VarBaglantiUId3, BdnHar_VarBaglantiUId4, BdnHar_VarBaglantiUId5, BdnHar_BirimFiyat, BdnHar_Tutar, BdnHar_FiyatListeNo)
VALUES(NEWID(), 0, 0, 0, 113, 0, 0, 0, 0, @sth_create_user, getdate(), @sth_create_user, getdate(), '','','',0,'','','','','',0,'',0,11, @rn, 0, @Miktar, 0, 0, 0, 0, 0, 0, ISNULL(@VarGuid1, CAST(0x0 AS uniqueidentifier)), CAST(0x0 AS uniqueidentifier), CAST(0x0 AS uniqueidentifier), CAST(0x0 AS uniqueidentifier), CAST(0x0 AS uniqueidentifier), 0, 0, 0)--*


END
GO

If exists(select name from sysobjects where name='A_sp_SK_CihazHar_Kaydet_2')
	DROP Procedure A_sp_SK_CihazHar_Kaydet_2
GO

CREATE Procedure A_sp_SK_CihazHar_Kaydet_2(
@No as integer,			--Evrak Tipini Belirler
@sth_evrakno_seri as varchar(6),--Seri No
@sth_evrakno_sira as integer,	--Sýra No
@SipTip as tinyint,		--Sipariþ Tip No
@SipSeriNo as varchar(6),	--Sipariþ Seri No
@SipSiraNo as integer,		--Sipariþ Sýra No
@Miktar as float,		--Teslim Miktarý
@dtt as integer,		--Detay Takip Tipi
@CihazNo as varchar (25),	--Cihaz No
@sth_satirno as integer, @VarGuid1 as uniqueidentifier = NULL)	
with encryption  
AS
BEGIN

Declare @sth_tip as tinyint
Declare @sth_cins as tinyint
Declare @sth_normal_iade as tinyint
Declare @sth_evraktip as tinyint
Declare @sth_create_user smallint 
Declare @sth_stok_kod varchar (25)

Select @sth_create_user=sip_create_user,
@sth_stok_kod=sip_stok_kod
from dbo.SIPARISLER 
where sip_tip=@SipTip and 
sip_cins=0 and 
sip_OnaylayanKulNo > 0 and 
sip_kapat_fl = 0 and
sip_evrakno_seri=@SipSeriNo and
sip_evrakno_sira=@SipSiraNo and
sip_satirno=0

-- Evrak Belirleyici deðiþken Atamalarý
IF @No=1 -- TAÝ
BEGIN
SET @sth_tip=0
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=13
END
ELSE IF @No=2 --TSÝ
BEGIN
SET @sth_tip=1
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=1
END
ELSE IF @No=3 --ATSF
BEGIN
SET @sth_tip=1
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=4	
END
ELSE IF @No=16 -- ATAF
BEGIN
SET @sth_tip=0
SET @sth_cins=0
SET @sth_normal_iade=0
SET @sth_evraktip=3
END
ELSE IF @No=5 --II ÝTHALAT
BEGIN
SET @sth_tip=0
SET @sth_cins=12
SET @sth_normal_iade=0
SET @sth_evraktip=13	
END
ELSE IF @No=6 --II ÝHRACAT
BEGIN
SET @sth_tip=1
SET @sth_cins=12
SET @sth_normal_iade=0
SET @sth_evraktip=1
END

Declare @ChHar_master_recno as uniqueidentifier-- Stok Hareketleri REC nosu
Select @ChHar_master_recno=sth_Guid 
from dbo.STOK_HAREKETLERI
where sth_tip=@sth_tip and
sth_cins=@sth_cins and
sth_normal_iade=@sth_normal_iade and
sth_evraktip=@sth_evraktip and
sth_evrakno_seri=@sth_evrakno_seri and
sth_evrakno_sira=@sth_evrakno_sira and
sth_satirno=@sth_satirno

INSERT INTO dbo.CIHAZ_HAREKETLERI(ChHar_DBCno, ChHar_Spec_Rec_no, ChHar_iptal, ChHar_fileid, ChHar_hidden, ChHar_kilitli, ChHar_degisti, ChHar_checksum, ChHar_create_user, ChHar_create_date, ChHar_lastup_user, ChHar_lastup_date, ChHar_special1, ChHar_special2, ChHar_special3, ChHar_SeriNo, ChHar_StokKodu, ChHar_master_tablo, ChHar_master_uid)
VALUES(0, 0, 0, 98, 0, 0, 0, 0, @sth_create_user, getdate(), 
@sth_create_user, getdate(), '','','', @CihazNo, @sth_stok_kod, 
0, @ChHar_master_recno) --*


END
GO

--sth_plasiyer_kodu
--sth_vergisiz_fl=1
--sth_otvvergisiz_fl=1
--sth_oivvergisiz_fl=1




IF EXISTS (
SELECT * FROM   sysobjects 
WHERE  name = 'A_fn_BirimFiyat')
DROP FUNCTION A_fn_BirimFiyat
GO

CREATE FUNCTION dbo.A_fn_BirimFiyat (@CariKod as varchar(25),@StokKod as varchar(25),@Tarih as datetime,@DepoNo as int)
RETURNS float 
with encryption
AS
BEGIN

Declare @Fiyat as float

IF @StokKod='' or @StokKod is null
BEGIN
SET @Fiyat=0
END

IF @StokKod <> '' 
BEGIN -- KOÞUL
-------------------------------------------------------------------------------------------
Declare @cari_satis_fk as int -- Stoðun hangi nolu fiyatýnýn alýnacaðýný belirler

Declare @cari_doviz_cinsi as tinyint
Declare @cari_satis_isk_kod as varchar(4)
Declare @cari_KurHesapSekli as tinyint

Select 
@cari_KurHesapSekli=cari_KurHesapSekli,
@cari_satis_fk=cari_satis_fk,
@cari_doviz_cinsi=cari_doviz_cinsi,
@cari_satis_isk_kod=cari_satis_isk_kod
--@cari_satis_fk=cari_satis_fk
from dbo.CARI_HESAPLAR
where cari_kod=@CariKod

if @cari_satis_fk=0 or @cari_satis_fk is null
SET @cari_satis_fk=1


Declare @Dolar as float
Declare @Euro as float

Set @Dolar = dbo.A_S_fn_KurBul (@cari_KurHesapSekli,@Tarih,1)
Set @Euro = dbo.A_S_fn_KurBul (@cari_KurHesapSekli,@Tarih,2)
--====================================
--Stok Deðerleri Getir
--====================================
Declare @sto_fiat_tutar as float
Declare @sto_fiat_doviz as tinyint
Declare @sto_fiat_iskonto as varchar(4)

Declare @Kontrol as varchar(25)
Declare @s as integer
SET @s=0 -- Satýþ Þartý Yok
Set @Kontrol=''

--Satýþ Þartý Varmý
Set @Kontrol = (Select TOP 1 @StokKod
from dbo.SATIS_SARTLARI 
WHERE (sat_cari_kod=@CariKod AND 
sat_stok_kod=@StokKod AND 
sat_basla_tarih<=@Tarih AND (sat_bitis_tarih>=@Tarih OR sat_bitis_tarih<='19101231')  AND 
sat_depo_no=@DepoNo   ) ORDER BY sat_basla_tarih DESC, sat_bitis_tarih DESC)

IF @Kontrol<>'' --Depoya ait satýþ þartý varsa
BEGIN

SET @s=1
Select TOP 1 @sto_fiat_tutar=(sat_brut_fiyat-(sat_det_isk_miktar1+sat_det_isk_miktar2+sat_det_isk_miktar3+
sat_det_isk_miktar4+sat_det_isk_miktar5+sat_det_isk_miktar6)+
(sat_det_mas_miktar1+sat_det_mas_miktar2+sat_det_mas_miktar3+sat_det_mas_miktar4)),
@sto_fiat_doviz=sat_doviz_cinsi
from dbo.SATIS_SARTLARI 
WHERE (sat_cari_kod=@CariKod AND 
sat_stok_kod=@StokKod AND 
sat_basla_tarih<=@Tarih AND (sat_bitis_tarih>=@Tarih OR sat_bitis_tarih<='19101231')  AND 
sat_depo_no=@DepoNo   ) ORDER BY sat_basla_tarih DESC, sat_bitis_tarih DESC

END
ELSE --Depoya ait satýþ þartý yoksa
BEGIN

SET @Kontrol=''
Set @Kontrol = (Select TOP 1 @StokKod
from dbo.SATIS_SARTLARI 
where (sat_cari_kod=@CariKod AND 
sat_stok_kod=@StokKod AND 
sat_basla_tarih<=@Tarih AND (sat_bitis_tarih>=@Tarih OR sat_bitis_tarih<='19101231')  AND 
(sat_depo_no=0 or sat_depo_no is NULL)) ORDER BY sat_basla_tarih DESC, sat_bitis_tarih DESC)

IF @Kontrol<>'' --Genel satýþ þartý varsa
BEGIN
SET @s=1
Select TOP 1 @sto_fiat_tutar=(sat_brut_fiyat-(sat_det_isk_miktar1+sat_det_isk_miktar2+sat_det_isk_miktar3+
sat_det_isk_miktar4+sat_det_isk_miktar5+sat_det_isk_miktar6)+
(sat_det_mas_miktar1+sat_det_mas_miktar2+sat_det_mas_miktar3+sat_det_mas_miktar4)),
@sto_fiat_doviz=sat_doviz_cinsi
from dbo.SATIS_SARTLARI 
where (sat_cari_kod=@CariKod AND 
sat_stok_kod=@StokKod AND 
sat_basla_tarih<=@Tarih AND (sat_bitis_tarih>=@Tarih OR sat_bitis_tarih<='19101231')  AND 
(sat_depo_no=0 or sat_depo_no is NULL)) ORDER BY sat_basla_tarih DESC, sat_bitis_tarih DESC
END
ELSE -- Genel satýþ þartý yoksa (Satýþ Fiyat Listeleri)
BEGIN
SET @s=0
SET @sto_fiat_tutar=dbo.fn_StokSatisFiyati(@StokKod,@cari_satis_fk,@DepoNo,1)
SET @sto_fiat_doviz=dbo.fn_StokFiyatDovizCinsi(@StokKod,@cari_satis_fk,@DepoNo,1)
SET @sto_fiat_iskonto=dbo.A_fn_StokSatisIskKod(@StokKod,@cari_satis_fk,@DepoNo)

--SET @sto_fiat_tutar=dbo.fn_StokSatisFiyati(@StokKod,@cari_satis_fk,@DepoNo)
--SET @sto_fiat_doviz=dbo.fn_StokFiyatDovizCinsi(@StokKod,@cari_satis_fk,@DepoNo)
Declare @KDV as bit
Select @KDV=sfl_kdvdahil from dbo.STOK_SATIS_FIYAT_LISTE_TANIMLARI
where sfl_sirano=@cari_satis_fk

Declare @vergi_pntr as tinyint
Declare @vergi as float
Select  @vergi_pntr=sto_toptan_vergi
from dbo.STOKLAR
where sto_kod=@StokKod
--==============================================
--Vergi Bul
--==============================================
Declare @VergiYuzde as float
if @vergi_pntr=1
Set @VergiYuzde=0
if @vergi_pntr=2
Set @VergiYuzde=0.01
if @vergi_pntr=3
Set @VergiYuzde=0.08
if @vergi_pntr=4
Set @VergiYuzde=0.18
if @vergi_pntr=5
Set @VergiYuzde=0.26

IF @KDV=1
BEGIN
SET @Fiyat=(@sto_fiat_tutar / (1 + @VergiYuzde))
            
If @cari_doviz_cinsi = 0 And @sto_fiat_doviz = 1 
                set @Fiyat = @Fiyat * @Dolar

Else If @cari_doviz_cinsi = 0 And @sto_fiat_doviz = 2 
                set @Fiyat = @Fiyat * @Euro

Else If @cari_doviz_cinsi = 1 And @sto_fiat_doviz = 0 
                set @Fiyat = @Fiyat / @Dolar
              
Else If @cari_doviz_cinsi = 1 And @sto_fiat_doviz = 2 
                set @Fiyat = (@Fiyat * @Euro) / @Dolar
              
Else If @cari_doviz_cinsi = 2 And @sto_fiat_doviz = 0  
                set @Fiyat = @Fiyat / @Euro
               
Else If @cari_doviz_cinsi = 2 And @sto_fiat_doviz = 1 
                set @Fiyat = (@Fiyat * @Dolar) / @Euro
               
END
ELSE
BEGIN 
            If @cari_doviz_cinsi = 0 And @sto_fiat_doviz = 0
                set @Fiyat = @sto_fiat_tutar

            Else If @cari_doviz_cinsi = 0 And @sto_fiat_doviz = 1 
                set @Fiyat = @sto_fiat_tutar * @Dolar

            Else If @cari_doviz_cinsi = 0 And @sto_fiat_doviz = 2 
                set @Fiyat = @sto_fiat_tutar * @Euro

            Else If @cari_doviz_cinsi = 1 And @sto_fiat_doviz = 0 
                set @Fiyat = @sto_fiat_tutar / @Dolar
              
            Else If @cari_doviz_cinsi = 1 And @sto_fiat_doviz = 1  
                set @Fiyat = @sto_fiat_tutar
               
            Else If @cari_doviz_cinsi = 1 And @sto_fiat_doviz = 2 
                set @Fiyat = (@sto_fiat_tutar * @Euro) / @Dolar
              
            Else If @cari_doviz_cinsi = 2 And @sto_fiat_doviz = 0  
                set @Fiyat = @sto_fiat_tutar / @Euro
               
            Else If @cari_doviz_cinsi = 2 And @sto_fiat_doviz = 1 
                set @Fiyat = (@sto_fiat_tutar * @Dolar) / @Euro
               
            Else If @cari_doviz_cinsi = 2 And @sto_fiat_doviz = 2  
                set @Fiyat = @sto_fiat_tutar
                       
END

END

END

--set @Fiyat = @sto_fiat_tutar

-------------------------------------------------------------------------------------------
END

Return @Fiyat

END
GO


If exists(select name from sysobjects where name='A_sp_PartiLotKartiKaydet_V16')
	DROP Procedure A_sp_PartiLotKartiKaydet_V16
GO

CREATE Procedure A_sp_PartiLotKartiKaydet_V16(
@PartiKodu as varchar(25),
--@LotNo as int,
@StokKodu as varchar(25),
@UserNo as integer,
@Tarih as datetime,
@Tip as integer,
@UretimTarih as datetime,
@SonKTarih as datetime,
@Kod1 as varchar(25),
@Kod2 as varchar(25),
@Kod3 as varchar(25)
) with encryption  
AS
BEGIN
Declare @Kontrol as int
Declare @LotNo as int
Declare @Kod10 as varchar(25)


IF @Tip=0 
BEGIN
SET  @Kod10 = 'PARTÝ TAKÝPLÝ'
END
ELSE IF @Tip=1 
BEGIN 
SET  @Kod10 = 'SERÝ TAKÝPLÝ'
END
ELSE IF @Tip=2 
BEGIN
SET  @Kod10 = 'PARTÝ+SERÝ TAKÝPLÝ'
END

Set @Kod10 =''

/*
Select @LotNo=isnull(max(pl_lotno),0)
from dbo.PARTILOT 
where pl_stokkodu=@StokKodu and pl_partikodu=@PartiKodu and ((@Kod1='' or @Kod1 is null) or pl_kod1=@Kod1) and pl_kod10=@Kod10
*/

Select @LotNo=isnull(max(pl_lotno),0)+1
from dbo.PARTILOT 
where pl_partikodu=@PartiKodu --and pl_stokkodu=@StokKodu 

INSERT INTO dbo.PARTILOT (pl_DBCno, pl_SpecRECno, 
pl_iptal, pl_fileid, pl_hidden, pl_kilitli, pl_degisti, pl_checksum, 
pl_create_user, pl_create_date, pl_lastup_user, pl_lastup_date, pl_ozelkod1, 
pl_ozelkod2, pl_ozelkod3, pl_partikodu, pl_lotno, pl_stokkodu, pl_aciklama, 
pl_olckalkdeg_deg1, pl_olckalkdeg_deg2, pl_olckalkdeg_deg3, pl_olckalkdeg_deg4, 
pl_olckalkdeg_deg5, pl_olckalkdeg_deg6, pl_olckalkdeg_deg7, pl_olckalkdeg_deg8, 
pl_olckalkdeg_deg9, pl_olckalkdeg_deg10, pl_DaraliKilo, 
pl_SafiKilo, pl_En, pl_Boy, pl_Yukseklik, pl_OzgulAgirlik, pl_kod1, pl_kod2, 
pl_kod3, pl_kod4, pl_kod5, pl_kod6, pl_kod7, pl_kod8, pl_kod9, pl_kod10
,pl_olckalkdeg_aciklama1,pl_olckalkdeg_aciklama2,pl_olckalkdeg_aciklama3,pl_olckalkdeg_aciklama4,pl_olckalkdeg_aciklama5,pl_olckalkdeg_aciklama6,pl_olckalkdeg_aciklama7,pl_olckalkdeg_aciklama8,pl_olckalkdeg_aciklama9,pl_olckalkdeg_aciklama10
,pl_son_kullanim_tar,pl_uretim_tar)
VALUES(0, 0, 0, 153, 0, 0, 0, 0, @UserNo, @Tarih, @UserNo, @Tarih, 
'', '', '', @PartiKodu, @LotNo, @StokKodu, '', 0, 0, 0, 0, 
0, 0,0, 0, 0, 0, 0, 0, 0, 0, 
0, 0, @Kod1, @Kod2, @Kod3, '', '', '', '', '', '', @Kod10
,'','','','','','','','','',''
,@SonKTarih,@UretimTarih)

Select isnull(pl_lotno,0) as Lot
from dbo.PARTILOT 
where pl_stokkodu=@StokKodu and pl_partikodu=@PartiKodu and pl_lotno=@LotNo

END
GO
