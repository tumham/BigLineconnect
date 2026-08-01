using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string path = @"C:\PROJEV6FORMEDIKAL\ONLINE_SAYIM_V16\PROJE\ONLINE_SAYIM_V16\frm_Login.vb";
        Encoding enc = Encoding.GetEncoding(1254);
        string text = File.ReadAllText(path, enc);
        
        string oldCode = "If Not obj2.Kontrol(\"Program Files\\BIGUS_STOCKMAN_V16\\\") Then";
        string newCode = @"Dim appPath As String = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase)
        If appPath.StartsWith(" + "\"\\\"" + @") Then appPath = appPath.Substring(1)
        If Not appPath.EndsWith(" + "\"\\\"" + @") Then appPath &= " + "\"\\\"" + @"
        If Not obj2.Kontrol(appPath) Then";
        
        text = text.Replace(oldCode, newCode);
        File.WriteAllText(path, text, enc);
        Console.WriteLine("Done.");
    }
}