using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string[] files = { 
            @"C:\PROJEV6FORMEDIKAL\ONLINE_SIP_KAR_V16\PROJE\ONLINE_SIPARIS_KARSILAMA_V16\frm_SipOnayla.vb",
            @"C:\PROJEV6FORMEDIKAL\ONLINE_SIP_KAR_V16\PROJE\ONLINE_SIPARIS_KARSILAMA_V16\frm_SipOnaylaDA.vb"
        };
        Encoding enc = Encoding.GetEncoding(1254);
        
        foreach (var path in files) {
            string text = File.ReadAllText(path, enc);
            
            // Fix the uncommented lines inside the commented blocks
            text = text.Replace("                    Dim vGuid_2 As String = objRB.VarGuid_Bul(dv_sip_detay.Rows(i)(8), dv_sip_detay.Rows(i)(11), dv_sip_detay.Rows(i)(2))", "        '            Dim vGuid_2 As String = objRB.VarGuid_Bul(dv_sip_detay.Rows(i)(8), dv_sip_detay.Rows(i)(11), dv_sip_detay.Rows(i)(2))");
            text = text.Replace("                    obj.BedenHarKaydet_2(n_Kaydet, n_SeriNo, n_SiraNo, rn, m, dv_sip_detay.Rows(i)(2), dv_sip_detay.Rows(i)(8), dv_sip_detay.Rows(i)(11), vGuid_2)", "        '            obj.BedenHarKaydet_2(n_Kaydet, n_SeriNo, n_SiraNo, rn, m, dv_sip_detay.Rows(i)(2), dv_sip_detay.Rows(i)(8), dv_sip_detay.Rows(i)(11), vGuid_2)");
            
            text = text.Replace("                    Dim vGuid_1 As String = objRB.VarGuid_Bul(dv_sip_detay.Rows(i)(8), dv_sip_detay.Rows(i)(11), dv_sip_detay.Rows(i)(2))", "        '            Dim vGuid_1 As String = objRB.VarGuid_Bul(dv_sip_detay.Rows(i)(8), dv_sip_detay.Rows(i)(11), dv_sip_detay.Rows(i)(2))");
            text = text.Replace("                    obj.BedenHarKaydet(n_Kaydet, n_SeriNo, n_SiraNo, rn, m, CInt(dv_sip_detay.Rows(i)(12)), vGuid_1)", "        '            obj.BedenHarKaydet(n_Kaydet, n_SeriNo, n_SiraNo, rn, m, CInt(dv_sip_detay.Rows(i)(12)), vGuid_1)");
            
            File.WriteAllText(path, text, enc);
        }
    }
}