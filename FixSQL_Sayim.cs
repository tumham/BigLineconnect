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

        // 1. Fix A_fn_Sym_RenkBul_V12
        int startR = sql.IndexOf("CREATE FUNCTION dbo.A_fn_Sym_RenkBul_V12");
        if(startR != -1) {
            int endR = sql.IndexOf("Return @Sonuc\r\n  END\r\n  GO", startR);
            if(endR == -1) endR = sql.IndexOf("Return @Sonuc\n  END\n  GO", startR);
            
            if(endR != -1) {
                endR += "Return @Sonuc\r\n  END\r\n  GO".Length;
                
                string newFunc = @"CREATE FUNCTION dbo.A_fn_Sym_RenkBul_V12 (@Barkod as varchar(25))
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
  END
  GO";
                sql = sql.Substring(0, startR) + newFunc + sql.Substring(endR);
                Console.WriteLine("Fixed RenkBul.");
            }
        }

        // 2. Fix A_fn_Sym_BedenBul_V12
        int startB = sql.IndexOf("CREATE FUNCTION dbo.A_fn_Sym_BedenBul_V12");
        if(startB != -1) {
            int endB = sql.IndexOf("Return @Sonuc\r\n  END\r\n  GO", startB);
            if(endB == -1) endB = sql.IndexOf("Return @Sonuc\n  END\n  GO", startB);
            
            if(endB != -1) {
                endB += "Return @Sonuc\r\n  END\r\n  GO".Length;
                
                string newFunc = @"CREATE FUNCTION dbo.A_fn_Sym_BedenBul_V12 (@Barkod as varchar(25))
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
  END
  GO";
                sql = sql.Substring(0, startB) + newFunc + sql.Substring(endB);
                Console.WriteLine("Fixed BedenBul.");
            }
        }

        // 3. Add Declare @VarGuid1 for Barkodla
        string search1 = @"Select @StokKod=bar_stokkodu,
  @RenkNo=isnull(bar_renkpntr,0),
  @BedenNo=isnull(bar_bedenpntr,0) 
  from dbo.BARKOD_TANIMLARI";
        string replace1 = @"Declare @VarGuid1 as uniqueidentifier
  Select @StokKod=bar_stokkodu,
  @RenkNo=isnull(bar_renkpntr,0),
  @BedenNo=isnull(bar_bedenpntr,0),
  @VarGuid1=isnull(bar_VarBaglantiUId1, '00000000-0000-0000-0000-000000000000')
  from dbo.BARKOD_TANIMLARI";
        
        if (sql.Contains(search1)) {
            sql = sql.Replace(search1, replace1);
            Console.WriteLine("Added VarGuid1 to Barkodla");
        } else {
            // Might have different newline chars
            search1 = search1.Replace("\r\n", "\n");
            replace1 = replace1.Replace("\r\n", "\n");
            if (sql.Contains(search1)) {
                sql = sql.Replace(search1, replace1);
                Console.WriteLine("Added VarGuid1 to Barkodla (LF)");
            }
        }

        // 4. Add Declare @VarGuid1 for CihazNoIle
        string search2 = @"Select @StokKod=chz_stok_kodu 
  from dbo.STOK_SERINO_TANIMLARI
  where chz_serino=@SeriNo
  
  SET @RenkNo=''
  SET @BedenNo=''";
        string replace2 = @"Declare @VarGuid1 as uniqueidentifier
  SET @VarGuid1 = '00000000-0000-0000-0000-000000000000'
  
  Select @StokKod=chz_stok_kodu 
  from dbo.STOK_SERINO_TANIMLARI
  where chz_serino=@SeriNo
  
  SET @RenkNo=''
  SET @BedenNo=''";

        if (sql.Contains(search2)) {
            sql = sql.Replace(search2, replace2);
            Console.WriteLine("Added VarGuid1 to CihazNoIle");
        } else {
            search2 = search2.Replace("\r\n", "\n");
            replace2 = replace2.Replace("\r\n", "\n");
            if (sql.Contains(search2)) {
                sql = sql.Replace(search2, replace2);
                Console.WriteLine("Added VarGuid1 to CihazNoIle (LF)");
            }
        }

        File.WriteAllText(path, sql, enc);
        Console.WriteLine("Done.");
    }
}