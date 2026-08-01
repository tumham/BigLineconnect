using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        string[] xamlFiles = Directory.GetFiles(dir, "*.xaml", SearchOption.AllDirectories);

        foreach(var f in xamlFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            
            // Replace multiple <syncfusion:SfNumericEntry separated by spaces/newlines with a single one
            string pattern = @"(<syncfusion:SfNumericEntry\s*){2,}";
            if (Regex.IsMatch(txt, pattern)) {
                txt = Regex.Replace(txt, pattern, "<syncfusion:SfNumericEntry\r\n");
                File.WriteAllText(f, txt, Encoding.UTF8);
            }

            // ALSO, remove <syncfusion:SfNumericEntry if it is inside the attributes of another tag.
            // How do we know it's inside attributes? If the PREVIOUS non-empty line DOES NOT end with > or /> 
            // Wait, what if we just remove <syncfusion:SfNumericEntry if it is directly before an attribute that ends with />?
            // Let's just fix the consecutive tags first.
        }
    }
}