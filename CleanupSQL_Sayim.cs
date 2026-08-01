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

        // Remove the corrupted ELSE IF blocks in the functions
        string corruptRenk = @"ELSE IF @StokKod is not NULL  
BEGIN
  	Select  @renkDetayli=sto_renkDetayli ,@renk_kodu =sto_renk_kodu  
from dbo.STOKLAR   
where sto_kod=@StokKod
	
	IF @renkDetayli=0 SET @Sonuc=''
	ELSE IF @renkDetayli=1
	BEGIN
	SET @Sonuc = (Select dbo.fn_renk_kirilimi(@renkpntr,@renk_kodu)  
	from dbo.STOK_RENK_TANIMLARI 
	WHERE  rnk_kodu=@renk_kodu)
	END
END

Return @Sonuc
END
GO";
        sql = sql.Replace(corruptRenk, "GO");

        string corruptBeden = @"ELSE IF @StokKod is not NULL  
BEGIN
  	Select  @bedenDetayli=sto_bedenli_takip ,@beden_kodu =sto_beden_kodu  
from dbo.STOKLAR   
where sto_kod=@StokKod
	
	IF @bedenDetayli=0 SET @Sonuc=''
	ELSE IF @bedenDetayli=1
	BEGIN
	SET @Sonuc = (Select dbo.fn_beden_kirilimi(@bedenpntr,@beden_kodu)  
	from dbo.STOK_BEDEN_TANIMLARI 
	WHERE  bdn_kodu=@beden_kodu)
	END
END
Return @Sonuc
END
GO";
        sql = sql.Replace(corruptBeden, "GO");

        // Now inject VarGuid1 for the procedures
        // Barkodla:
        string barkodStart = @"Select @StokKod=bar_stokkodu,
@RenkNo=isnull(bar_renkpntr,0),
@BedenNo=isnull(bar_bedenpntr,0) 
from dbo.BARKOD_TANIMLARI";

        string barkodReplace = @"Declare @VarGuid1 as uniqueidentifier
Select @StokKod=bar_stokkodu,
@RenkNo=isnull(bar_renkpntr,0),
@BedenNo=isnull(bar_bedenpntr,0),
@VarGuid1=isnull(bar_VarBaglantiUId1, '00000000-0000-0000-0000-000000000000')
from dbo.BARKOD_TANIMLARI";
        sql = sql.Replace(barkodStart, barkodReplace);

        // CihazNoIle:
        string cihazStart = @"Select @StokKod=chz_stok_kodu 
from dbo.STOK_SERINO_TANIMLARI
where chz_serino=@SeriNo

SET @RenkNo=''
SET @BedenNo=''";

        string cihazReplace = @"Declare @VarGuid1 as uniqueidentifier
SET @VarGuid1 = '00000000-0000-0000-0000-000000000000'

Select @StokKod=chz_stok_kodu 
from dbo.STOK_SERINO_TANIMLARI
where chz_serino=@SeriNo

SET @RenkNo=''
SET @BedenNo=''";
        sql = sql.Replace(cihazStart, cihazReplace);

        File.WriteAllText(path, sql, enc);
        Console.WriteLine("Cleanup done.");
    }
}