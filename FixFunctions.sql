IF EXISTS (SELECT * FROM sysobjects WHERE name = 'A_fn_RenkDetay_Bul') DROP FUNCTION A_fn_RenkDetay_Bul
GO
CREATE FUNCTION dbo.A_fn_RenkDetay_Bul (@SKod as varchar(25))
RETURNS bit
with encryption
AS
BEGIN
  Declare @sonuc as bit
  Select @sonuc=ISNULL(sto_varyant_detayli_fl1, 0) from dbo.STOKLAR where sto_kod=@SKod
  if @sonuc is null set @sonuc=0
  Return @sonuc
END
GO

IF EXISTS (SELECT * FROM sysobjects WHERE name = 'A_fn_BedenDetay_Bul') DROP FUNCTION A_fn_BedenDetay_Bul
GO
CREATE FUNCTION dbo.A_fn_BedenDetay_Bul (@SKod as varchar(25))
RETURNS bit
with encryption
AS
BEGIN
  Declare @sonuc as bit
  Select @sonuc=ISNULL(sto_varyant_detayli_fl2, 0) from dbo.STOKLAR where sto_kod=@SKod
  if @sonuc is null set @sonuc=0
  Return @sonuc
END
GO

IF EXISTS (SELECT * FROM sysobjects WHERE name = 'A_fn_RenkKod_Bul') DROP FUNCTION A_fn_RenkKod_Bul
GO
CREATE FUNCTION dbo.A_fn_RenkKod_Bul (@SKod as varchar(25))
RETURNS varchar(25)  
with encryption
AS
BEGIN
  Declare @sonuc as varchar(25)
  Select @sonuc=ISNULL(sto_varyant_kod_arr1, '') from dbo.STOKLAR where sto_kod=@SKod
  Return @sonuc
END
GO

IF EXISTS (SELECT * FROM sysobjects WHERE name = 'A_fn_BedenKod_Bul') DROP FUNCTION A_fn_BedenKod_Bul
GO
CREATE FUNCTION dbo.A_fn_BedenKod_Bul (@SKod as varchar(25))
RETURNS varchar(25)  
with encryption
AS
BEGIN
  Declare @sonuc as varchar(25)
  Select @sonuc=ISNULL(sto_varyant_kod_arr2, '') from dbo.STOKLAR where sto_kod=@SKod
  Return @sonuc
END
GO

IF EXISTS (SELECT * FROM sysobjects WHERE name = 'A_fn_B_RenkBul') DROP FUNCTION A_fn_B_RenkBul
GO
CREATE FUNCTION dbo.A_fn_B_RenkBul (@BKod as varchar(25))
RETURNS varchar(25)  
with encryption
AS
BEGIN
  Declare @sonuc as varchar(25)
  Return ''
END
GO

IF EXISTS (SELECT * FROM sysobjects WHERE name = 'A_fn_B_BedenBul') DROP FUNCTION A_fn_B_BedenBul
GO
CREATE FUNCTION dbo.A_fn_B_BedenBul (@BKod as varchar(25))
RETURNS varchar(25)  
with encryption
AS
BEGIN
  Declare @sonuc as varchar(25)
  Return ''
END
GO

IF EXISTS (SELECT * FROM sysobjects WHERE name = 'A_sp_Irs_DepoStokGetir') DROP PROCEDURE A_sp_Irs_DepoStokGetir
GO
CREATE Procedure dbo.A_sp_Irs_DepoStokGetir(
@DepoNo as integer)
with encryption
AS
BEGIN
Select sto_kod as Kod,sto_isim as Stok,sto_anagrup_kod as AnaGrup,
sto_altgrup_kod as AltGrup,sto_varyant_detayli_fl2 as sto_bedenli_takip,sto_varyant_kod_arr2 as sto_beden_kodu,
sto_varyant_kod_arr1 as sto_renk_kodu,sto_varyant_detayli_fl1 as sto_renkDetayli,sto_detay_takip,
isnull(dbo.fn_DepodakiMiktar(sto_kod,@DepoNo,getdate()),0) as Miktar 
from dbo.STOKLAR
END
GO