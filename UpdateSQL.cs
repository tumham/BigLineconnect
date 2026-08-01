using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string path = @"C:\PROJEV6FORMEDIKAL\ONLINE_IRSALIYE_V16\ONLINE_IRSALIYE_V16\SQL\MEDIKAL_ONLINE_IRSALIYE_V17.sql";
        Encoding enc = Encoding.GetEncoding(1254);
        string sql = File.ReadAllText(path, enc);

        // 1. Replace A_fn_B_RenkBul
        string fnRenkStart = "CREATE FUNCTION dbo.A_fn_B_RenkBul (@BKod as varchar(25))\r\n  RETURNS varchar(10)  \r\n  with encryption\r\n  AS\r\n  BEGIN";
        string fnRenkEnd = "  Return @Sonuc\r\n  END";
        int startR = sql.IndexOf(fnRenkStart);
        if(startR != -1) {
            int endR = sql.IndexOf(fnRenkEnd, startR) + fnRenkEnd.Length;
            sql = sql.Substring(0, startR) + 
@"CREATE FUNCTION dbo.A_fn_B_RenkBul (@BKod as varchar(25))
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
  END" + sql.Substring(endR);
            Console.WriteLine("Replaced RenkBul");
        }

        // 2. Replace A_fn_B_BedenBul
        string fnBedenStart = "CREATE FUNCTION dbo.A_fn_B_BedenBul (@BKod as varchar(25))\r\n  RETURNS varchar(10)  \r\n  with encryption\r\n  AS\r\n  BEGIN";
        string fnBedenEnd = "  Return @Sonuc\r\n  END";
        int startB = sql.IndexOf(fnBedenStart);
        if(startB != -1) {
            int endB = sql.IndexOf(fnBedenEnd, startB) + fnBedenEnd.Length;
            sql = sql.Substring(0, startB) + 
@"CREATE FUNCTION dbo.A_fn_B_BedenBul (@BKod as varchar(25))
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
  END" + sql.Substring(endB);
            Console.WriteLine("Replaced BedenBul");
        }

        // 3. Inject VarGuid1 into A_sp_BedenHarKaydet_Barkod_Depo
        string insertTarget1 = "  IF @KayitKontrol <= 0  and @Tip=0\r\n  BEGIN\r\n  \r\n  INSERT INTO dbo.BEDEN_HAREKETLERI(BdnHar_DBCno";
        string replace1 = @"  IF @KayitKontrol <= 0  and @Tip=0
  BEGIN
  
  Declare @VarGuid1 as uniqueidentifier
  SELECT TOP 1 @VarGuid1 = VBag_Guid FROM dbo.VARYANT_BAGLANTI_TANIMLARI WHERE VBag_VaryantKod=@StokKod AND (VBag_KirilimKod=@Renk OR VBag_KirilimKod=@Beden) AND VBag_Tip=0
  IF @VarGuid1 IS NULL SET @VarGuid1 = '00000000-0000-0000-0000-000000000000'
  
  INSERT INTO dbo.BEDEN_HAREKETLERI(BdnHar_DBCno";
        
        sql = sql.Replace(insertTarget1, replace1);

        string insertTarget2 = "  IF @KayitKontrol <= 0  and @Miktar >0\r\n  BEGIN\r\n  \r\n  INSERT INTO dbo.BEDEN_HAREKETLERI(BdnHar_DBCno";
        string replace2 = @"  IF @KayitKontrol <= 0  and @Miktar >0
  BEGIN
  
  Declare @VarGuid1 as uniqueidentifier
  SELECT TOP 1 @VarGuid1 = VBag_Guid FROM dbo.VARYANT_BAGLANTI_TANIMLARI WHERE VBag_VaryantKod=@StokKod AND (VBag_KirilimKod=@Renk OR VBag_KirilimKod=@Beden) AND VBag_Tip=0
  IF @VarGuid1 IS NULL SET @VarGuid1 = '00000000-0000-0000-0000-000000000000'
  
  INSERT INTO dbo.BEDEN_HAREKETLERI(BdnHar_DBCno";
        
        sql = sql.Replace(insertTarget2, replace2);

        // Replace columns and values
        string oldCols = "BdnHar_Har_uid, BdnHar_BedenNo, BdnHar_HarGor, BdnHar_KnsIsGor, BdnHar_KnsFat, BdnHar_TesMik)";
        string newCols = "BdnHar_Har_uid, BdnHar_BedenNo, BdnHar_HarGor, BdnHar_KnsIsGor, BdnHar_KnsFat, BdnHar_TesMik, BdnHar_VarBaglantiUId1)";
        sql = sql.Replace(oldCols, newCols);

        string oldVals = "@BedenNo,@Miktar,0,0,0)";
        string newVals = "@BedenNo,@Miktar,0,0,0,@VarGuid1)";
        sql = sql.Replace(oldVals, newVals);

        File.WriteAllText(path, sql, enc);
        Console.WriteLine("Done SQL patches.");
    }
}