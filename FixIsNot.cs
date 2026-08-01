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
        
        text = text.Replace("val IsNot Nothing", "Not val Is Nothing");
        
        File.WriteAllText(path, text, enc);
    }
}