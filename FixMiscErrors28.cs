using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        
        string extFile = Path.Combine(dir, "Helpers", "ObservableCollectionExtensions.cs");
        if (File.Exists(extFile)) {
            File.Delete(extFile);
        }

        string[] csFiles = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
        foreach(var f in csFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;

            if (txt.Contains(".ToObservableCollection(")) {
                txt = txt.Replace(".ToObservableCollection(", ".ToObservableCollectionBigus(");
                changed = true;
            }
            if (txt.Contains("ToObservableCollection<T>")) {
                txt = txt.Replace("ToObservableCollection<T>", "ToObservableCollectionBigus<T>");
                changed = true;
            }

            if (f.Contains("OfferStockSearchIskontoPopupPage.xaml.cs")) {
                if (txt.Contains("Navigation.PopPopupAsync()")) {
                    txt = txt.Replace("Navigation.PopPopupAsync()", "Mopups.Services.MopupService.Instance.PopAsync()");
                    changed = true;
                }
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}