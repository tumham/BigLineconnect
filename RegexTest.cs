using System;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string text = ""C:\\BigMobil\\Bigus_MAUI\\Client\\Mobile\\Bigus.BigMobil\\Bigus.BigMobil\\Views\\SatinalmaVeSatisYonetimi\\Evraklar\\Siparis\\DisTicaretSiparisEvraki\\DTSiparislerFisiNewCardView.xaml(1,1): error MAUIG1001: An error occured while parsing Xaml: Name cannot begin with the '<' character, hexadecimal value 0x3C. Line 1892, position 37.."";
        var m = Regex.Match(text, @""(C:\\BigMobil\\[^\(]+)\(\d+,\d+\): error MAUIG1001: An error occured while parsing Xaml: Name cannot begin with the '<' character.*?Line (\d+)"");
        Console.WriteLine(m.Success);
    }
}