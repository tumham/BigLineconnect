using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string path = @"C:\PROJEV6FORMEDIKAL\ONLINE_SAYIM_V16\ONLINE_SAYIM_V16\SQL\SAYIM_V16_SP.sql";
        Encoding enc = Encoding.GetEncoding(1254);
        string sql = File.ReadAllText(path, enc);

        // 1. Replace A_fn_Sym_RenkBul_V12
        sql = Regex.Replace(sql, @"CREATE FUNCTION dbo\.A_fn_Sym_RenkBul_V12.*?END", @"CREATE FUNCTION dbo.A_fn_Sym_RenkBul_V12 (@Barkod as varchar(25))
  RETURNS varchar(25)  
  with encryption
  AS
  BEGIN
    Declare @sonuc as varchar(25)
    Select @sonuc = v.VBag_KirilimKod 
    from dbo.BARKOD_TANIMLARI b 
    left join dbo.VARYANT_BAGLANTI_TANIMLARI v on v.VBag_Guid = b.bar_VarBaglantiUId1 
    where b.bar_kodu = @Barkod
    return ISNULL(@sonuc, '')
  END", RegexOptions.Singleline);
  
        // 2. Replace A_fn_Sym_BedenBul_V12
        sql = Regex.Replace(sql, @"CREATE FUNCTION dbo\.A_fn_Sym_BedenBul_V12.*?END", @"CREATE FUNCTION dbo.A_fn_Sym_BedenBul_V12 (@Barkod as varchar(25))
  RETURNS varchar(25)  
  with encryption
  AS
  BEGIN
    Declare @sonuc as varchar(25)
    Select @sonuc = v.VBag_KirilimKod 
    from dbo.BARKOD_TANIMLARI b 
    left join dbo.VARYANT_BAGLANTI_TANIMLARI v on v.VBag_Guid = b.bar_VarBaglantiUId2 
    where b.bar_kodu = @Barkod
    return ISNULL(@sonuc, '')
  END", RegexOptions.Singleline);

        // 3. Inject VarGuid into Barkodla insert procedure
        string selectBarkod = @"Select @StokKod=bar_stokkodu,
  @RenkNo=isnull(bar_renkpntr,0),
  @BedenNo=isnull(bar_bedenpntr,0) 
  from dbo.BARKOD_TANIMLARI";
        string replaceBarkod = @"Declare @VarGuid1 as uniqueidentifier
  Select @StokKod=bar_stokkodu,
  @RenkNo=isnull(bar_renkpntr,0),
  @BedenNo=isnull(bar_bedenpntr,0),
  @VarGuid1=isnull(bar_VarBaglantiUId1, '00000000-0000-0000-0000-000000000000')
  from dbo.BARKOD_TANIMLARI";
        sql = sql.Replace(selectBarkod, replaceBarkod);

        // 4. Inject VarGuid into CihazNoIle insert procedure
        string selectCihaz = @"Select @StokKod=chz_stok_kodu 
  from dbo.STOK_SERINO_TANIMLARI
  where chz_serino=@SeriNo
  
  SET @RenkNo=''
  SET @BedenNo=''";
        string replaceCihaz = @"Declare @VarGuid1 as uniqueidentifier
  SET @VarGuid1 = '00000000-0000-0000-0000-000000000000'
  
  Select @StokKod=chz_stok_kodu 
  from dbo.STOK_SERINO_TANIMLARI
  where chz_serino=@SeriNo
  
  SET @RenkNo=''
  SET @BedenNo=''";
        sql = sql.Replace(selectCihaz, replaceCihaz);

        // 5. Replace INSERT columns and values
        string oldInsertCols = "sym_miktar5, sym_barkod, sym_renkno, sym_bedenno, sym_parti_kodu, sym_lot_no, sym_serino)";
        string newInsertCols = "sym_miktar5, sym_barkod, sym_renkno, sym_bedenno, sym_parti_kodu, sym_lot_no, sym_serino, sym_VarBaglantiUId1)";
        sql = sql.Replace(oldInsertCols, newInsertCols);

        // Values replacements
        // 1. CihazNoIle (Tip 0)
        string oldCihazVal0 = "'',0,@SeriNo)";
        string newCihazVal0 = "'',0,@SeriNo,@VarGuid1)";
        sql = sql.Replace(oldCihazVal0, newCihazVal0);

        // 2. Barkodla (Tip 0/1)
        string oldBarkodVal = "@PartiKodu,@LotNo,@SeriNo)";
        string newBarkodVal = "@PartiKodu,@LotNo,@SeriNo,@VarGuid1)";
        sql = sql.Replace(oldBarkodVal, newBarkodVal);

        File.WriteAllText(path, sql, enc);
        Console.WriteLine("Done SQL patches for Sayim.");
    }
}