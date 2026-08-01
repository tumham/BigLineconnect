using System;
using System.IO;

class Program
{
    static void Main()
    {
        string path = @"C:\PROJEV6FORMEDIKAL\ONLINE_SIP_KAR_V16\SIPARIS_KARSILAMA_V16\SQL\SIPARIS_KARSILAMA_V16.sql";
        string content = File.ReadAllText(path, System.Text.Encoding.GetEncoding(1254));
        
        string old_sig = "@Miktar as float,\t\t--Teslim Miktarý\r\n@BedenNo as integer,\t\t--Beden No\r\n@sth_satirno as integer)\t--Satýr No";
        string new_sig = "@Miktar as float,\t\t--Teslim Miktarý\r\n@BedenNo as integer,\t\t--Beden No\r\n@sth_satirno as integer,\t--Satýr No\r\n@VarGuid1 as uniqueidentifier = NULL)\t--VarGuid";
        content = content.Replace(old_sig, new_sig);

        string old_insert = "INSERT INTO dbo.BEDEN_HAREKETLERI(BdnHar_DBCno, BdnHar_Spec_Rec_no, BdnHar_iptal, BdnHar_fileid, BdnHar_hidden, BdnHar_kilitli, BdnHar_degisti, BdnHar_checksum, BdnHar_create_user, BdnHar_create_date, BdnHar_lastup_user, BdnHar_lastup_date, BdnHar_special1, BdnHar_special2, BdnHar_special3, BdnHar_Tipi, BdnHar_Har_uid, BdnHar_BedenNo, BdnHar_HarGor, BdnHar_KnsIsGor, BdnHar_KnsFat, BdnHar_TesMik)\r\nVALUES(0, 0, 0, 113, 0, 0, 0, 0, @sth_create_user, getdate(), \r\n@sth_create_user, getdate(), '','','',11, @rn, \r\n@BedenNo,@Miktar, 0, 0, 0)--*";
        string new_insert = "INSERT INTO dbo.BEDEN_HAREKETLERI(BdnHar_DBCno, BdnHar_Spec_Rec_no, BdnHar_iptal, BdnHar_fileid, BdnHar_hidden, BdnHar_kilitli, BdnHar_degisti, BdnHar_checksum, BdnHar_create_user, BdnHar_create_date, BdnHar_lastup_user, BdnHar_lastup_date, BdnHar_special1, BdnHar_special2, BdnHar_special3, BdnHar_Tipi, BdnHar_Har_uid, BdnHar_VaryantPNTR, BdnHar_HarGor, BdnHar_KnsIsGor, BdnHar_KnsFat, BdnHar_TesMik, BdnHar_Guid)\r\nVALUES(0, 0, 0, 113, 0, 0, 0, 0, @sth_create_user, getdate(), \r\n@sth_create_user, getdate(), '','','',11, @rn, \r\nISNULL(@VarGuid1, CAST(0x0 AS uniqueidentifier)),@Miktar, 0, 0, 0, NEWID())--*";
        content = content.Replace(old_insert, new_insert);

        string old_update1 = "	UPDATE dbo.BEDEN_HAREKETLERI\r\n	SET BdnHar_TesMik=BdnHar_TesMik+@Miktar\r\n	WHERE (BdnHar_Tipi=9 AND BdnHar_Har_uid=@SipRecNo AND BdnHar_BedenNo=@BedenNo)";
        string new_update1 = "	UPDATE dbo.BEDEN_HAREKETLERI\r\n	SET BdnHar_TesMik=BdnHar_TesMik+@Miktar\r\n	WHERE (BdnHar_Tipi=9 AND BdnHar_Har_uid=@SipRecNo AND BdnHar_VaryantPNTR=ISNULL(@VarGuid1, CAST(0x0 AS uniqueidentifier)))";
        content = content.Replace(old_update1, new_update1);

        string old_update2 = "	UPDATE dbo.BEDEN_HAREKETLERI\r\n	SET BdnHar_TesMik=BdnHar_TesMik+@Miktar\r\n	WHERE (BdnHar_Tipi=1 AND BdnHar_Har_uid=@SipRecNo AND BdnHar_BedenNo=@BedenNo)";
        string new_update2 = "	UPDATE dbo.BEDEN_HAREKETLERI\r\n	SET BdnHar_TesMik=BdnHar_TesMik+@Miktar\r\n	WHERE (BdnHar_Tipi=1 AND BdnHar_Har_uid=@SipRecNo AND BdnHar_VaryantPNTR=ISNULL(@VarGuid1, CAST(0x0 AS uniqueidentifier)))";
        content = content.Replace(old_update2, new_update2);

        string old_sig_2 = "@Miktar as float,\t\t--Teslim Miktarý\r\n@BedenNo as integer)\t\t--Beden No";
        string new_sig_2 = "@Miktar as float,\t\t--Teslim Miktarý\r\n@BedenNo as integer,\t\t--Beden No\r\n@VarGuid1 as uniqueidentifier = NULL)\t--VarGuid";
        content = content.Replace(old_sig_2, new_sig_2);

        File.WriteAllText(path, content, System.Text.Encoding.GetEncoding(1254));
        Console.WriteLine("SQL Update Complete!");
    }
}