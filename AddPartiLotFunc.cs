using System;
using System.IO;

class Program
{
    static void Main()
    {
        string path = @"C:\standart-v16 to V17-irsaliye\ONLINE_IRSALIYE_V16\Classes\cls_Irsaliye.vb";
        string content = File.ReadAllText(path, System.Text.Encoding.GetEncoding(1254));
        
        string newFunc = @"
    Public Function BarkoddanPartiLotDetay_Ara(ByVal BarkodKod As String, ByRef PartiKodu As String, ByRef LotNo As String) As Boolean
        Dim comm As New SqlCommand("SELECT bar_partikodu, bar_lotno FROM dbo.BARKOD_TANIMLARI WHERE bar_kodu='" & BarkodKod & "'", con)
        Try
            If con.State = ConnectionState.Closed Then
                con.Open()
            End If
            Dim rdr As SqlDataReader = comm.ExecuteReader()
            If rdr.Read() Then
                PartiKodu = IIf(IsDBNull(rdr("bar_partikodu")), "", rdr("bar_partikodu"))
                Dim LNo As Integer = IIf(IsDBNull(rdr("bar_lotno")), 0, rdr("bar_lotno"))
                LotNo = LNo.ToString()
                rdr.Close()
                con.Close()
                If PartiKodu <> "" Or LotNo <> "0" Then
                    Return True
                End If
            Else
                rdr.Close()
                con.Close()
            End If
            Return False
        Catch ex As SqlException
            If con.State = ConnectionState.Open Then
                con.Close()
            End If
            Return False
        End Try
    End Function
";
        
        int endClassIdx = content.LastIndexOf("End Class");
        if (endClassIdx != -1) {
            content = content.Insert(endClassIdx, newFunc + "\r\n");
            File.WriteAllText(path, content, System.Text.Encoding.GetEncoding(1254));
            Console.WriteLine("Added BarkoddanPartiLotDetay_Ara to cls_Irsaliye.vb");
        }
    }
}