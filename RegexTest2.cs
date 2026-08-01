using System;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string input = ""<tabView:SfTabItem\n Title="";
        string output = Regex.Replace(input, @""(<tabView:SfTabItem[^>]*?)(\s+)Title="", ""="");
        Console.WriteLine(output);
    }
}