using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        
        string[] csFiles = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
        foreach(var f in csFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;

            if (txt.Contains("DataGridPdfExportOption")) {
                if (!txt.Contains("Syncfusion.Maui.DataGrid.Exporting.DataGridPdfExportOption")) {
                    txt = txt.Replace("new DataGridPdfExportOption()", "new Syncfusion.Maui.DataGrid.Exporting.DataGridPdfExportOption()");
                    changed = true;
                }
            }

            if (txt.Contains("PointF")) {
                if(txt.Contains("new PointF(")) {
                    txt = txt.Replace("new PointF(", "new Syncfusion.Drawing.PointF(");
                    changed = true;
                }
            }

            if (f.Contains("GenelAmacliSiparisRaporuView.xaml.cs")) {
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
                if(txt.Contains("e.ItemData")) {
                    txt = txt.Replace("e.ItemData", "e.DataItem");
                    changed = true;
                }
                if(txt.Contains("Color.LightSkyBlue")) {
                    txt = txt.Replace("Color.LightSkyBlue", "Colors.LightSkyBlue");
                    changed = true;
                }
                if(txt.Contains("Color.LightGray")) {
                    txt = txt.Replace("Color.LightGray", "Colors.LightGray");
                    changed = true;
                }
                
                // GroupSummaryRows
                if (txt.Contains("this.dataGrid.GroupSummaryRows.Add")) {
                    int startIdx = txt.IndexOf("this.dataGrid.GroupSummaryRows.Add");
                    while(startIdx != -1) {
                        if (startIdx > 0 && txt[startIdx - 1] != '/') {
                            int endIdx = txt.IndexOf("});", startIdx);
                            if (endIdx != -1) {
                                string toReplace = txt.Substring(startIdx, endIdx - startIdx + 3);
                                txt = txt.Replace(toReplace, "/*" + toReplace + "*/");
                                changed = true;
                            }
                        }
                        startIdx = txt.IndexOf("this.dataGrid.GroupSummaryRows.Add", startIdx + 34);
                    }
                }
            }
            
            if (f.Contains("OfferStockSearchIskontoPopupPage.xaml.cs")) {
                if (txt.Contains("Application.Current.MainPage.Navigation.PopPopupAsync()")) {
                    txt = txt.Replace("Application.Current.MainPage.Navigation.PopPopupAsync()", "Mopups.Services.MopupService.Instance.PopAsync()");
                    changed = true;
                }
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}