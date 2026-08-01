using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string path = @"C:\PROJEV6FORMEDIKAL\ONLINE_IRSALIYE_V16\PROJE\ONLINE_IRSALIYE_V16\Classes\cls_Irsaliye.vb";
        Encoding enc = Encoding.GetEncoding(1254);
        string text = File.ReadAllText(path, enc);
        if (text.Contains("comm.Parameters.Add(\"@Beden\", Beden)")) {
            Console.WriteLine("Found comm.Parameters.Add(\"@Beden\", Beden)");
        }
    }
}