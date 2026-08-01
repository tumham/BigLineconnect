using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string path = @"C:\PROJEV6FORMEDIKAL\ONLINE_IRSALIYE_V16\ONLINE_IRSALIYE_V16\SQL\MEDIKAL_ONLINE_IRSALIYE_V17.sql";
        Encoding enc = Encoding.GetEncoding(1254);
        string sql = File.ReadAllText(path, enc);

        sql = Regex.Replace(sql, @"CREATE FUNCTION dbo\.A_fn_B_RenkBul.*?END", @"CREATE FUNCTION dbo.A_fn_B_RenkBul (@BKod as varchar(25))
  RETURNS varchar(25)  
  with encryption
  AS
  BEGIN
    Declare @sonuc as varchar(25)
    Select @sonuc = v.VBag_KirilimKod 
    from dbo.BARKOD_TANIMLARI b 
    left join dbo.VARYANT_BAGLANTI_TANIMLARI v on v.VBag_Guid = b.bar_VarBaglantiUId1 
    where b.bar_kodu = @BKod
    return ISNULL(@sonuc, '')
  END", RegexOptions.Singleline);
  
        sql = Regex.Replace(sql, @"CREATE FUNCTION dbo\.A_fn_B_BedenBul.*?END", @"CREATE FUNCTION dbo.A_fn_B_BedenBul (@BKod as varchar(25))
  RETURNS varchar(25)  
  with encryption
  AS
  BEGIN
    Declare @sonuc as varchar(25)
    Select @sonuc = v.VBag_KirilimKod 
    from dbo.BARKOD_TANIMLARI b 
    left join dbo.VARYANT_BAGLANTI_TANIMLARI v on v.VBag_Guid = b.bar_VarBaglantiUId2 
    where b.bar_kodu = @BKod
    return ISNULL(@sonuc, '')
  END", RegexOptions.Singleline);

        File.WriteAllText(path, sql, enc);
        Console.WriteLine("Done Regex patches.");
    }
}