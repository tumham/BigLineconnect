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
        
        string[] lines = text.Split(new[] { "\r\n" }, StringSplitOptions.None);
        for(int i=0; i<lines.Length; i++) {
            if(lines[i].Length > 1000) {
                Console.WriteLine("Long line at " + (i+1));
            }
        }
        Console.WriteLine("Done checking.");
    }
}