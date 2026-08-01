using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        
        // Clean obj and bin
        if(Directory.Exists(Path.Combine(dir, "obj"))) Directory.Delete(Path.Combine(dir, "obj"), true);
        if(Directory.Exists(Path.Combine(dir, "bin"))) Directory.Delete(Path.Combine(dir, "bin"), true);

        string[] csFiles = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
        foreach(var f in csFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;
            
            if(txt.Contains("Syncfusion.XForms")) {
                txt = txt.Replace("Syncfusion.XForms.Buttons", "Syncfusion.Maui.Buttons");
                changed = true;
            }
            if(txt.Contains("Xamarin.Forms")) {
                txt = txt.Replace("Xamarin.Forms", "Microsoft.Maui.Controls");
                changed = true;
            }
            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}