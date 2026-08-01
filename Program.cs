using System;
class Program {
    static void Main() {
        Exception ex = new Exception("Timeout expired.");
        string since = "2023-01-01";
        string resultMessage = ex.Message + " " + ex.InnerException != null ? (ex.InnerException != null ? ex.InnerException.Message : "") : "" + "Param:" + since;
        Console.WriteLine("RESULT: '" + resultMessage + "'");
    }
}
