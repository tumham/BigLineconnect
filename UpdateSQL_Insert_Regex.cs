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

        string logic = @"Declare @BedenNo as smallint
  SET @BedenNo=dbo.A_S_fn_HareketSatirNoBul (@StokKod,@Renk,@Beden)
  
  Declare @VarGuid1 as uniqueidentifier
  SELECT TOP 1 @VarGuid1 = VBag_Guid FROM dbo.VARYANT_BAGLANTI_TANIMLARI WHERE VBag_VaryantKod=@StokKod AND (VBag_KirilimKod=@Renk OR VBag_KirilimKod=@Beden) AND VBag_Tip=0
  IF @VarGuid1 IS NULL SET @VarGuid1 = '00000000-0000-0000-0000-000000000000'
";

        sql = Regex.Replace(sql, @"Declare @BedenNo as smallint\s*SET @BedenNo=dbo\.A_S_fn_HareketSatirNoBul \(@StokKod,@Renk,@Beden\)", logic);

        string oldInsert = @"INSERT INTO dbo\.BEDEN_HAREKETLERI\(BdnHar_DBCno, BdnHar_Spec_Rec_no, BdnHar_iptal, BdnHar_fileid, BdnHar_hidden, BdnHar_kilitli, BdnHar_degisti, BdnHar_checksum, BdnHar_create_user, BdnHar_create_date, BdnHar_lastup_user, BdnHar_lastup_date, BdnHar_special1, BdnHar_special2, BdnHar_special3, BdnHar_Tipi, BdnHar_Har_uid, BdnHar_BedenNo, BdnHar_HarGor, BdnHar_KnsIsGor, BdnHar_KnsFat, BdnHar_TesMik\)";
        string newInsert = "INSERT INTO dbo.BEDEN_HAREKETLERI(BdnHar_DBCno, BdnHar_Spec_Rec_no, BdnHar_iptal, BdnHar_fileid, BdnHar_hidden, BdnHar_kilitli, BdnHar_degisti, BdnHar_checksum, BdnHar_create_user, BdnHar_create_date, BdnHar_lastup_user, BdnHar_lastup_date, BdnHar_special1, BdnHar_special2, BdnHar_special3, BdnHar_Tipi, BdnHar_Har_uid, BdnHar_BedenNo, BdnHar_HarGor, BdnHar_KnsIsGor, BdnHar_KnsFat, BdnHar_TesMik, BdnHar_VarBaglantiUId1)";
        
        string oldVals = @"@BedenNo,@Miktar,0,0,0\)";
        string newVals = "@BedenNo,@Miktar,0,0,0,@VarGuid1)";

        sql = Regex.Replace(sql, oldInsert, newInsert);
        sql = Regex.Replace(sql, oldVals, newVals);

        File.WriteAllText(path, sql, enc);
        Console.WriteLine("Done.");
    }
}