using System;
using System.IO;
using System.Globalization;

class Program
{
    static void Main()
    {
        string path = @"C:\Projev17YD\DUZ_V17_STD\ACILISYENISES.025";
        string[] lines = File.ReadAllLines(path, System.Text.Encoding.GetEncoding(1254));
        
        decimal sumCol4 = 0m;
        decimal sumCol5 = 0m;
        decimal sumCol6 = 0m;
        
        CultureInfo trCulture = new CultureInfo("tr-TR");

        foreach(string line in lines)
        {
            if(string.IsNullOrWhiteSpace(line)) continue;
            string[] parts = line.Split(';');
            if(parts.Length > 5)
            {
                if(decimal.TryParse(parts[3].Trim(), NumberStyles.Any, trCulture, out decimal val4))
                {
                    sumCol4 += val4;
                }
                if(decimal.TryParse(parts[4].Trim(), NumberStyles.Any, trCulture, out decimal val5))
                {
                    sumCol5 += val5;
                }
                if(decimal.TryParse(parts[5].Trim(), NumberStyles.Any, trCulture, out decimal val6))
                {
                    sumCol6 += val6;
                }
            }
        }
        
        Console.WriteLine(string.Format("Total Col4: {0}", sumCol4));
        Console.WriteLine(string.Format("Total Col5: {0}", sumCol5));
        Console.WriteLine(string.Format("Total Col6: {0}", sumCol6));
    }
}