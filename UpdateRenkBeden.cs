using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string path = @"C:\PROJEV6FORMEDIKAL\ONLINE_IRSALIYE_V16\PROJE\ONLINE_IRSALIYE_V16\Classes\cls_RenkBeden.vb";
        Encoding enc = Encoding.GetEncoding(1254);
        string text = File.ReadAllText(path, enc);
        
        string oldSorgu = "Select sto_bedenli_takip,sto_beden_kodu,sto_renk_kodu,sto_renkDetayli from dbo.STOKLAR where sto_kod=";
        string newSorgu = "Select isnull(sto_varyant_detayli_fl2,0) as sto_bedenli_takip, '' as sto_beden_kodu, '' as sto_renk_kodu, isnull(sto_varyant_detayli_fl1,0) as sto_renkDetayli from dbo.STOKLAR where sto_kod=";
        
        if (text.Contains(oldSorgu)) {
            text = text.Replace(oldSorgu, newSorgu);
            File.WriteAllText(path, text, enc);
            Console.WriteLine("Patched RenkBedenDetay_Ara.");
        } else {
            Console.WriteLine("oldSorgu not found.");
        }
    }
}