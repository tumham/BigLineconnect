CREATE Procedure A_sp_SK_BedenHar_Kaydet(
@No as integer,			--Evrak Tipini Belirler
@sth_evrakno_seri as varchar(6),--Seri No
@sth_evrakno_sira as integer,	--Sýra No
@SipRecNo as uniqueidentifier,		--Sipariþe ait REC No
@Miktar as float,		--Teslim Miktarý
ISNULL(@VarGuid1, CAST(0x0 AS uniqueidentifier)) as integer,		--Beden No
@sth_satirno as integer)	--Satýr No,
@VarGuid1 as uniqueidentifier = NULL) --VarGuid
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

INSERT INTO dbo.BEDEN_HAREKETLERI(BdnHar_DBCno, BdnHar_Spec_Rec_no, BdnHar_iptal, BdnHar_fileid, BdnHar_hidden, BdnHar_kilitli, BdnHar_degisti, BdnHar_checksum, BdnHar_create_user, BdnHar_create_date, BdnHar_lastup_user, BdnHar_lastup_date, BdnHar_special1, BdnHar_special2, BdnHar_special3, BdnHar_Tipi, BdnHar_Har_uid, BdnHar_VaryantPNTR, BdnHar_HarGor, BdnHar_KnsIsGor, BdnHar_KnsFat, BdnHar_TesMik, BdnHar_Guid)
VALUES(0, 0, 0, 113, 0, 0, 0, 0, @sth_create_user, getdate(), 
@sth_create_user, getdate(), '','','',11, @rn, 
ISNULL(@VarGuid1, CAST(0x0 AS uniqueidentifier)), @Miktar, 0, 0, 0, NEWID())--*


IF @No<>4
BEGIN
	UPDATE dbo.BEDEN_HAREKETLERI
	SET BdnHar_TesMik=BdnHar_TesMik+@Miktar
	WHERE (BdnHar_Tipi=9 AND BdnHar_Har_uid=@SipRecNo AND BdnHar_VaryantPNTR=ISNULL(@VarGuid1, CAST(0x0 AS uniqueidentifier)))
END
ELSE
BEGIN
	UPDATE dbo.BEDEN_HAREKETLERI
	SET BdnHar_TesMik=BdnHar_TesMik+@Miktar
	WHERE (BdnHar_Tipi=1 AND BdnHar_Har_uid=@SipRecNo AND BdnHar_VaryantPNTR=ISNULL(@VarGuid1, CAST(0x0 AS uniqueidentifier)))
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
@sth_satirno as integer)	
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