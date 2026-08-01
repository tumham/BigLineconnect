using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string[] files = { 
            @"C:\PROJEV6FORMEDIKAL\ONLINE_SIP_KAR_V16\PROJE\ONLINE_SIPARIS_KARSILAMA_V16\Classes\mdl_Main.vb",
            @"C:\PROJEV6FORMEDIKAL\ONLINE_SIP_KAR_V16\PROJE\ONLINE_SIPARIS_KARSILAMA_V16\frm_Login.vb"
        };
        Encoding enc = Encoding.GetEncoding(1254);
        
        foreach(var path in files) {
            string text = File.ReadAllText(path, enc);
            
            text = text.Replace("MikroDB_V16Conn()", "MikroDesktopConn()");
            text = text.Replace("MikroDB_V16_", "MikroDesktop_");
            text = text.Replace("Database=MikroDB_V16;", "Database=MikroDesktop;");
            
            File.WriteAllText(path, text, enc);
        }
    }
}