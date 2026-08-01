using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string path = @"C:\PROJEV6FORMEDIKAL\ONLINE_SIP_KAR_V16\PROJE\ONLINE_SIPARIS_KARSILAMA_V16\Classes\cls_RenkBeden.vb";
        Encoding enc = Encoding.GetEncoding(1254);
        string text = File.ReadAllText(path, enc);
        
        // Remove my previous bad attempt
        text = text.Replace("\" & vbCrLf & \" UNION ALL SELECT ", " UNION ALL SELECT ");
        
        // Now split it properly using VB.NET line continuations
        text = text.Replace(" UNION ALL SELECT ", "\" & _\r\n            \" UNION ALL SELECT ");
        
        File.WriteAllText(path, text, enc);
    }
}