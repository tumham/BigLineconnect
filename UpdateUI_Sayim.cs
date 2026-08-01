using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string file = @"C:\PROJEV6FORMEDIKAL\ONLINE_SAYIM_V16\PROJE\ONLINE_SAYIM_V16\frm_Login.vb";
        Encoding enc = Encoding.GetEncoding(1254);
        string text = File.ReadAllText(file, enc);
        bool changed = false;
        
        if(text.Contains("fgAltForm.Cols(1).Width = 70")) { text = text.Replace("fgAltForm.Cols(1).Width = 70", "fgAltForm.Cols(1).Width = 55"); changed = true; }
        if(text.Contains("fgAltForm.Cols(2).Width = 200")) { text = text.Replace("fgAltForm.Cols(2).Width = 200", "fgAltForm.Cols(2).Width = 100"); changed = true; }
        if(text.Contains("fgAltForm.Cols(2).Width = 80")) { text = text.Replace("fgAltForm.Cols(2).Width = 80", "fgAltForm.Cols(2).Width = 50"); changed = true; }
        if(text.Contains("fgAltForm.Cols(3).Width = 30")) { text = text.Replace("fgAltForm.Cols(3).Width = 30", "fgAltForm.Cols(3).Width = 0"); changed = true; }
        if(text.Contains("fgAltForm.Cols(4).Width = 70")) { text = text.Replace("fgAltForm.Cols(4).Width = 70", "fgAltForm.Cols(4).Width = 50"); changed = true; }
        
        if(changed) {
            File.WriteAllText(file, text, enc);
            Console.WriteLine("Patched scrollbar issue in frm_Login.vb");
        }
    }
}