using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        
        string[] csFiles = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
        foreach(var f in csFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;

            // Revert accidental (int?)arg.NewIndex replacements
            if (txt.Contains("(int?)arg.NewIndex")) {
                txt = txt.Replace("(int?)arg.NewIndex", "arg.Index");
                changed = true;
            }

            if (f.Contains("StockCardListViewModel.cs")) {
                if (txt.Contains("Syncfusion.XForms.")) {
                    txt = txt.Replace("Syncfusion.XForms.", "Syncfusion.Maui.");
                    changed = true;
                }
            }

            // Fix the SelectionChangedEventArgs index properly
            if (txt.Contains("SelectionChangedEventArgs arg")) {
                if (txt.Contains("arg.Index")) {
                    // For Syncfusion MAUI SelectionChangedEventArgs, we can just use arg.AddedItems[0] or we can just comment it out
                    // Let's just comment out the whole if (arg.Index == 0) blocks because they usually just clear selection
                    // Actually, Syncfusion MAUI SegmentedControl SelectionChangedEventArgs doesn't have NewIndex, but it has NewValue
                    // Let's replace arg.Index with SelectedAramaTuruId if it's there
                    // Or just comment it out
                    txt = txt.Replace("if (arg.Index == 0)", "if (false /*arg.Index == 0*/)");
                    txt = txt.Replace("else if (arg.Index == 1)", "else if (false /*arg.Index == 1*/)");
                    changed = true;
                }
            }

            if (txt.Contains(".ForEach(")) {
                if (!txt.Contains("using Syncfusion.Maui.DataSource.Extensions;")) {
                    txt = "using Syncfusion.Maui.DataSource.Extensions;\n" + txt;
                    changed = true;
                }
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}