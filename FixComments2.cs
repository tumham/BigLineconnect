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
            
            // Re-enable the active ones
            text = text.Replace(
                "                If dv_sip_detay.Rows(i)(12) > 0 And m > 0 Then ' -- BedenNo\r\n                    Dim objRB As New cls_RenkBeden\r\n        '            Dim vGuid_2 As String = objRB.VarGuid_Bul(dv_sip_detay.Rows(i)(8), dv_sip_detay.Rows(i)(11), dv_sip_detay.Rows(i)(2))\r\n        '            obj.BedenHarKaydet_2(n_Kaydet, n_SeriNo, n_SiraNo, rn, m, dv_sip_detay.Rows(i)(2), dv_sip_detay.Rows(i)(8), dv_sip_detay.Rows(i)(11), vGuid_2)\r\n                End If", 
                "                If dv_sip_detay.Rows(i)(12) > 0 And m > 0 Then ' -- BedenNo\r\n                    Dim objRB As New cls_RenkBeden\r\n                    Dim vGuid_2 As String = objRB.VarGuid_Bul(dv_sip_detay.Rows(i)(8), dv_sip_detay.Rows(i)(11), dv_sip_detay.Rows(i)(2))\r\n                    obj.BedenHarKaydet_2(n_Kaydet, n_SeriNo, n_SiraNo, rn, m, dv_sip_detay.Rows(i)(2), dv_sip_detay.Rows(i)(8), dv_sip_detay.Rows(i)(11), vGuid_2)\r\n                End If");
            
            text = text.Replace(
                "                If dv_sip_detay.Rows(i)(12) > 0 And m > 0 Then  ' -- BedenNo\r\n                    Dim objRB As New cls_RenkBeden\r\n        '            Dim vGuid_1 As String = objRB.VarGuid_Bul(dv_sip_detay.Rows(i)(8), dv_sip_detay.Rows(i)(11), dv_sip_detay.Rows(i)(2))\r\n        '            obj.BedenHarKaydet(n_Kaydet, n_SeriNo, n_SiraNo, rn, m, CInt(dv_sip_detay.Rows(i)(12)), vGuid_1)\r\n                End If", 
                "                If dv_sip_detay.Rows(i)(12) > 0 And m > 0 Then  ' -- BedenNo\r\n                    Dim objRB As New cls_RenkBeden\r\n                    Dim vGuid_1 As String = objRB.VarGuid_Bul(dv_sip_detay.Rows(i)(8), dv_sip_detay.Rows(i)(11), dv_sip_detay.Rows(i)(2))\r\n                    obj.BedenHarKaydet(n_Kaydet, n_SeriNo, n_SiraNo, rn, m, CInt(dv_sip_detay.Rows(i)(12)), vGuid_1)\r\n                End If");
                
            text = text.Replace(
                "                If dv_sip_detay.Rows(i)(12) > 0 And m > 0 Then ' -- BedenNo\r\n                    Dim objRB As New cls_RenkBeden\r\n        '            Dim vGuid_1 As String = objRB.VarGuid_Bul(dv_sip_detay.Rows(i)(8), dv_sip_detay.Rows(i)(11), dv_sip_detay.Rows(i)(2))\r\n        '            obj.BedenHarKaydet(n_Kaydet, n_SeriNo, n_SiraNo, rn, m, CInt(dv_sip_detay.Rows(i)(12)), vGuid_1)\r\n                End If", 
                "                If dv_sip_detay.Rows(i)(12) > 0 And m > 0 Then ' -- BedenNo\r\n                    Dim objRB As New cls_RenkBeden\r\n                    Dim vGuid_1 As String = objRB.VarGuid_Bul(dv_sip_detay.Rows(i)(8), dv_sip_detay.Rows(i)(11), dv_sip_detay.Rows(i)(2))\r\n                    obj.BedenHarKaydet(n_Kaydet, n_SeriNo, n_SiraNo, rn, m, CInt(dv_sip_detay.Rows(i)(12)), vGuid_1)\r\n                End If");                
            
            // Check if vGuid1 exists without underscore
            text = text.Replace("vGuid1", "vGuid_1");
            text = text.Replace("vGuid2", "vGuid_2");
            
            File.WriteAllText(path, text, enc);
        }
    }
}