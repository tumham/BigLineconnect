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

            // Fix StokSiparisFoyuView.xaml.cs / StokProformaSiparisFoyuView.xaml.cs
            if(f.Contains("StokSiparisFoyuView.xaml.cs") || f.Contains("StokProformaSiparisFoyuView.xaml.cs")) {
                if(txt.Contains("e.RowData")) {
                    txt = txt.Replace("e.RowData", "e.DataItem");
                    changed = true;
                }
                if(txt.Contains("e.ItemData")) {
                    txt = txt.Replace("e.ItemData", "e.DataItem");
                    changed = true;
                }
                if(txt.Contains("DataDataGrid")) {
                    txt = txt.Replace("DataDataGrid", "DataGrid");
                    changed = true;
                }
                if(txt.Contains("SummaryType.CountAggregate")) {
                    txt = txt.Replace("SummaryType.CountAggregate", "Syncfusion.Maui.DataGrid.SummaryType.CountAggregate");
                    changed = true;
                }
            }

            // Fix DepoSonDurumRaporlariViewModel.cs
            if(f.Contains("DepoSonDurumRaporlariViewModel.cs")) {
                if(txt.Contains("GridTextColumn")) {
                    txt = txt.Replace("GridTextColumn", "DataGridTextColumn");
                    changed = true;
                }
                if(txt.Contains("GridNumericColumn")) {
                    txt = txt.Replace("GridNumericColumn", "DataGridNumericColumn");
                    changed = true;
                }
            }
            
            // Fix DepoSonDurumRaporlariView.xaml.cs
            if(f.Contains("DepoSonDurumRaporlariView.xaml.cs")) {
                if(txt.Contains("CrossMessaging")) {
                    txt = txt.Replace("CrossMessaging", "//CrossMessaging");
                    changed = true;
                }
                if(txt.Contains("DeviceInfo.")) {
                    txt = txt.Replace("DeviceInfo.", "Microsoft.Maui.Devices.DeviceInfo.Current.");
                    changed = true;
                }
                if(txt.Contains("DevicePlatform.")) {
                    txt = txt.Replace("DevicePlatform.", "Microsoft.Maui.Devices.DevicePlatform.");
                    changed = true;
                }
                if(txt.Contains("Microsoft.Maui.Networking.EmailAttachment")) {
                    txt = txt.Replace("Microsoft.Maui.Networking.EmailAttachment", "Microsoft.Maui.ApplicationModel.Email.EmailAttachment");
                    changed = true;
                }
            }
            
            // Fix BigusRoleUserListViewModel.cs
            if(f.Contains("BigusRoleUserListViewModel.cs")) {
                if(txt.Contains("Color.Yellow")) {
                    txt = txt.Replace("Color.Yellow", "Colors.Yellow");
                    changed = true;
                }
            }
            
            // Fix ViewModelBase.cs
            if(f.Contains("ViewModelBase.cs")) {
                if(txt.Contains("HideLoading()")) {
                    txt = txt.Replace("HideLoading()", "HideHud()");
                    changed = true;
                }
            }
            
            // Fix RaporTasarimView.xaml.cs
            if(f.Contains("RaporTasarimView.xaml.cs")) {
                if(txt.Contains("grid.Children.Add(stackTitle, 0, 0);")) {
                    txt = txt.Replace("grid.Children.Add(stackTitle, 0, 0);", "grid.Add(stackTitle, 0, 0);");
                    changed = true;
                }
                if(txt.Contains("GestureStatus")) {
                    txt = txt.Replace("GestureStatus", "GestureStatus/*not in MAUI PanUpdatedEventArgs*/");
                    changed = true;
                }
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}