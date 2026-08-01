using System;
using System.Reflection;
class Program {
    static void Main() {
        Assembly asm = Assembly.LoadFrom(@"C:\PROJEV6FORMEDIKAL\ONLINE_SIP_KAR_V16\PROJE\ONLINE_SIPARIS_KARSILAMA_V16\bin\Debug\C1.CF.C1FlexGrid.dll");
        Type t = asm.GetType("C1.Win.C1FlexGrid.C1FlexGrid");
        foreach(var prop in t.GetProperties()) {
            if (prop.Name.Contains("Scroll") || prop.Name.Contains("Size") || prop.Name.Contains("Width")) {
                Console.WriteLine(prop.Name + " (" + prop.PropertyType.Name + ")");
            }
        }
    }
}