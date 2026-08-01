using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string dir = @"C:\PROJEV6FORMEDIKAL\ONLINE_SAYIM_V16\PROJE\ONLINE_SAYIM_V16";
        Encoding enc = Encoding.GetEncoding(1254);
        foreach(var file in Directory.GetFiles(dir, "*.vb", SearchOption.AllDirectories)) {
            string text = File.ReadAllText(file, enc);
            bool changed = false;
            
            if(text.Contains("MikroDB_V16Conn")) { text = text.Replace("MikroDB_V16Conn", "MikroDesktopConn"); changed = true; }
            if(text.Contains("MikroDB_V16_")) { text = text.Replace("MikroDB_V16_", "MikroDesktop_"); changed = true; }
            if(text.Contains("Database=MikroDB_V16;")) { text = text.Replace("Database=MikroDB_V16;", "Database=MikroDesktop;"); changed = true; }
            
            if(changed) {
                File.WriteAllText(file, text, enc);
                Console.WriteLine("Patched DB in " + Path.GetFileName(file));
            }
        }
    }
}