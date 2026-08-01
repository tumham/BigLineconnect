using System;
using System.IO;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string path = @"C:\Projev17YD\DUZ_V17_STD\A_sp_SK_BedenHar_Kaydet.sql";
        string content = File.ReadAllText(path, System.Text.Encoding.GetEncoding(1254));
        
        // 1. Add @VarGuid1
        content = Regex.Replace(content, 
            @"(@sth_satirno as integer)\)\s*--Sat.r No", 
            ",\r\n@VarGuid1 as uniqueidentifier = NULL)\t--VarGuid");
        
        // 2. Add BdnHar_Guid and replace BdnHar_BedenNo
        content = Regex.Replace(content,
            @"BdnHar_KnsIsGor, BdnHar_KnsFat, BdnHar_TesMik\)",
            "BdnHar_KnsIsGor, BdnHar_KnsFat, BdnHar_TesMik, BdnHar_Guid\)");
        content = content.Replace("BdnHar_BedenNo", "BdnHar_VaryantPNTR");
            
        // 3. Replace VALUES
        content = Regex.Replace(content,
            @"(@BedenNo),(@Miktar, 0, 0, 0)--\*",
            "ISNULL(@VarGuid1, CAST(0x0 AS uniqueidentifier)), , NEWID())--\*");
            
        // 4. Update the WHERE clause
        content = content.Replace("@BedenNo", "ISNULL(@VarGuid1, CAST(0x0 AS uniqueidentifier))");

        File.WriteAllText("C:\Projev17YD\DUZ_V17_STD\A_sp_SK_BedenHar_Kaydet_Modified.sql", content, System.Text.Encoding.GetEncoding(1254));
        Console.WriteLine("Done");
    }
}