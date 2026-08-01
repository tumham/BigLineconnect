using System;
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

            string oldTxt = txt;

            // Swipe Templates
            txt = txt.Replace("<syncfusion:SfListView.RightSwipeTemplate>", "<syncfusion:SfListView.EndSwipeTemplate>");
            txt = txt.Replace("</syncfusion:SfListView.RightSwipeTemplate>", "</syncfusion:SfListView.EndSwipeTemplate>");
            txt = txt.Replace("<syncfusion:SfListView.LeftSwipeTemplate>", "<syncfusion:SfListView.StartSwipeTemplate>");
            txt = txt.Replace("</syncfusion:SfListView.LeftSwipeTemplate>", "</syncfusion:SfListView.StartSwipeTemplate>");

            // TabHeaderBackgroundColor
            txt = Regex.Replace(txt, @"\s*TabHeaderBackgroundColor=""[^""]*""", "");

            // ColumnSizer -> ColumnWidthMode
            txt = txt.Replace("ColumnSizer=", "ColumnWidthMode=");

            // SfTabItem Title -> Header
            txt = txt.Replace("<tabView:SfTabItem Title=", "<tabView:SfTabItem Header=");
            txt = Regex.Replace(txt, @"\s*TitleFontAttributes=""[^""]*""", "");
            txt = Regex.Replace(txt, @"\s*SelectionColor=""[^""]*""", "");

            // SfNumericUpDown -> SfNumericEntry
            if (txt.Contains("SfNumericUpDown")) {
                txt = txt.Replace("numeric:SfNumericUpDown", "syncfusion:SfNumericEntry");
                txt = Regex.Replace(txt, @"\s*UpDownButtonColor=""[^""]*""", "");
                txt = Regex.Replace(txt, @"\s*SpinButtonAlignment=""[^""]*""", "");
            }

            if (txt != oldTxt) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}