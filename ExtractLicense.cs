using System;
using System.IO;
using System.Reflection;

class Program
{
    static void Main()
    {
        string exePath = @"C:\PROJEV6FORMEDIKAL\ONLINE_SIP_KAR_V16\PROJE\ONLINE_SIPARIS_KARSILAMA_V16\bin\Release\SIPARIS_KARSILAMA_V16.exe";
        Assembly asm = Assembly.LoadFrom(exePath);
        string[] names = asm.GetManifestResourceNames();
        foreach (string name in names)
        {
            Console.WriteLine(name);
            if (name.EndsWith(".licenses"))
            {
                using (Stream stream = asm.GetManifestResourceStream(name))
                using (FileStream fs = new FileStream(@"C:\Projev17YD\DUZ_V17_STD\" + name, FileMode.Create))
                {
                    stream.CopyTo(fs);
                    Console.WriteLine("Extracted " + name);
                }
            }
        }
    }
}