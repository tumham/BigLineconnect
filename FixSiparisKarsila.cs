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
        
        // Remove the badly placed VarGuid1 parameters
        text = text.Replace("comm.Parameters.Add(\"@Miktar\", Miktar)\r\n              comm.Parameters.Add(\"@VarGuid1\", New Guid(VarGuid1))", "comm.Parameters.Add(\"@Miktar\", Miktar)");
        text = text.Replace("comm.Parameters.Add(\"@Beden\", Beden)\r\n              comm.Parameters.Add(\"@VarGuid1\", New Guid(VarGuid1))", "comm.Parameters.Add(\"@Beden\", Beden)");
        
        File.WriteAllText(path, text, enc);
    }
}