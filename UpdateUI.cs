using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string[] files = { 
            @"C:\PROJEV6FORMEDIKAL\ONLINE_SIP_KAR_V16\PROJE\ONLINE_SIPARIS_KARSILAMA_V16\frm_Login.vb",
            @"C:\PROJEV6FORMEDIKAL\ONLINE_IRSALIYE_V16\PROJE\ONLINE_IRSALIYE_V16\frm_Login.vb"
        };
        Encoding enc = Encoding.GetEncoding(1254);
        
        foreach(var path in files) {
            if(File.Exists(path)) {
                string text = File.ReadAllText(path, enc);
                text = text.Replace("fgAltForm.Cols(1).Width = 70", "fgAltForm.Cols(1).Width = 55");
                text = text.Replace("fgAltForm.Cols(2).Width = 200", "fgAltForm.Cols(2).Width = 100");
                text = text.Replace("fgAltForm.Cols(2).Width = 80", "fgAltForm.Cols(2).Width = 50");
                text = text.Replace("fgAltForm.Cols(3).Width = 30", "fgAltForm.Cols(3).Width = 0");
                text = text.Replace("fgAltForm.Cols(4).Width = 70", "fgAltForm.Cols(4).Width = 50");
                File.WriteAllText(path, text, enc);
                Console.WriteLine("Patched " + path);
            }
        }
    }
}