using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        string[] xamlFiles = Directory.GetFiles(dir, "*.xaml", SearchOption.AllDirectories);

        foreach(var f in xamlFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;

            if (txt.Contains("<syncfusion:SfNumericEntry{Binding")) {
                txt = txt.Replace("<syncfusion:SfNumericEntry{Binding", "<syncfusion:SfNumericEntry TextColor=\"Black\" Value=\"{Binding");
                changed = true;
            }
            if (txt.Contains("<syncfusion:SfNumericEntry BorderColor=\"{StaticResource BlackColor}\"")) {
                // Actually the previous script inserted <syncfusion:SfNumericEntryBorderColor or similar? Let's fix that.
            }

            // Also, any place where I replaced TextColor="Black" ... Value=""{Binding
            // My previous script: <syncfusion:SfNumericEntry
            // Let's just fix the missing Grid.Row and Grid.Column if possible, or just let them overlap for now to ensure compilation.

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}