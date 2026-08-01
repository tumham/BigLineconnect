using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string path = @"C:\PROJEV6FORMEDIKAL\ONLINE_SIP_KAR_V16\PROJE\ONLINE_SIPARIS_KARSILAMA_V16\Classes\cls_RenkBeden.vb";
        Encoding enc = Encoding.GetEncoding(1254);
        string[] lines = File.ReadAllLines(path, enc);
        for(int i=150; i<235; i++) {
            if(i < lines.Length) {
                Console.WriteLine((i+1) + ": " + lines[i]);
            }
        }
    }
}