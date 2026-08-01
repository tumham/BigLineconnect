using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string path = @"C:\PROJEV6FORMEDIKAL\ONLINE_SIP_KAR_V16\PROJE\ONLINE_SIPARIS_KARSILAMA_V16\Classes\cls_SiparisKarsila.vb";
        Encoding enc = Encoding.GetEncoding(1254);
        string text = File.ReadAllText(path, enc);
        
        // Add to BedenHarKaydet
        text = text.Replace("comm.Parameters.Add(\"@BedenNo\", BedenNo)", "comm.Parameters.Add(\"@BedenNo\", BedenNo)\r\n              comm.Parameters.Add(\"@VarGuid1\", New Guid(VarGuid1))");
        
        // Add to BedenHarKaydet_2
        text = text.Replace("comm.Parameters.Add(\"@Beden\", Beden)", "comm.Parameters.Add(\"@Beden\", Beden)\r\n              comm.Parameters.Add(\"@VarGuid1\", New Guid(VarGuid1))");
        
        File.WriteAllText(path, text, enc);
    }
}