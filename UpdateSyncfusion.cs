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
            
            if(txt.Contains("Syncfusion.")) {
                txt = txt.Replace("using Syncfusion.SfDataGrid.XForms;", "using Syncfusion.Maui.DataGrid;");
                txt = txt.Replace("using Syncfusion.ListView.XForms;", "using Syncfusion.Maui.ListView;");
                txt = txt.Replace("using Syncfusion.XForms.Buttons;", "using Syncfusion.Maui.Buttons;");
                txt = txt.Replace("using Syncfusion.XForms.ComboBox;", "using Syncfusion.Maui.Inputs;");
                txt = txt.Replace("using Syncfusion.XForms.TabView;", "using Syncfusion.Maui.TabView;");
                txt = txt.Replace("using Syncfusion.SfNumericTextBox.XForms;", "using Syncfusion.Maui.Inputs;");
                txt = txt.Replace("using Syncfusion.SfNumericUpDown.XForms;", "using Syncfusion.Maui.Inputs;");
                txt = txt.Replace("using Syncfusion.SfChart.XForms;", "using Syncfusion.Maui.Charts;");
                txt = txt.Replace("using Syncfusion.SfPdfViewer.XForms;", "using Syncfusion.Maui.PdfViewer;");
                txt = txt.Replace("using Syncfusion.XForms.Border;", "using Syncfusion.Maui.Core;");
                txt = txt.Replace("using Syncfusion.XForms.Core;", "using Syncfusion.Maui.Core;");
                changed = true;
            }
            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
        
        string[] xamlFiles = Directory.GetFiles(dir, "*.xaml", SearchOption.AllDirectories);
        foreach(var f in xamlFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;
            if(txt.Contains("Syncfusion")) {
                txt = Regex.Replace(txt, @"clr-namespace:Syncfusion.SfDataGrid.XForms;assembly=Syncfusion.SfDataGrid.XForms", "clr-namespace:Syncfusion.Maui.DataGrid;assembly=Syncfusion.Maui.DataGrid");
                txt = Regex.Replace(txt, @"clr-namespace:Syncfusion.ListView.XForms;assembly=Syncfusion.SfListView.XForms", "clr-namespace:Syncfusion.Maui.ListView;assembly=Syncfusion.Maui.ListView");
                txt = Regex.Replace(txt, @"clr-namespace:Syncfusion.XForms.Buttons;assembly=Syncfusion.Buttons.XForms", "clr-namespace:Syncfusion.Maui.Buttons;assembly=Syncfusion.Maui.Buttons");
                txt = Regex.Replace(txt, @"clr-namespace:Syncfusion.XForms.ComboBox;assembly=Syncfusion.SfComboBox.XForms", "clr-namespace:Syncfusion.Maui.Inputs;assembly=Syncfusion.Maui.Inputs");
                txt = Regex.Replace(txt, @"clr-namespace:Syncfusion.XForms.TabView;assembly=Syncfusion.SfTabView.XForms", "clr-namespace:Syncfusion.Maui.TabView;assembly=Syncfusion.Maui.TabView");
                txt = Regex.Replace(txt, @"clr-namespace:Syncfusion.SfNumericTextBox.XForms;assembly=Syncfusion.SfNumericTextBox.XForms", "clr-namespace:Syncfusion.Maui.Inputs;assembly=Syncfusion.Maui.Inputs");
                txt = Regex.Replace(txt, @"clr-namespace:Syncfusion.SfNumericUpDown.XForms;assembly=Syncfusion.SfNumericUpDown.XForms", "clr-namespace:Syncfusion.Maui.Inputs;assembly=Syncfusion.Maui.Inputs");
                txt = Regex.Replace(txt, @"clr-namespace:Syncfusion.SfChart.XForms;assembly=Syncfusion.SfChart.XForms", "clr-namespace:Syncfusion.Maui.Charts;assembly=Syncfusion.Maui.Charts");
                txt = Regex.Replace(txt, @"clr-namespace:Syncfusion.SfPdfViewer.XForms;assembly=Syncfusion.SfPdfViewer.XForms", "clr-namespace:Syncfusion.Maui.PdfViewer;assembly=Syncfusion.Maui.PdfViewer");
                txt = Regex.Replace(txt, @"clr-namespace:Syncfusion.XForms.Border;assembly=Syncfusion.Core.XForms", "clr-namespace:Syncfusion.Maui.Core;assembly=Syncfusion.Maui.Core");
                changed = true;
            }
            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}