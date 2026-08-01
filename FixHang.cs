using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string path = @"C:\PROJEV6FORMEDIKAL\ONLINE_SIP_KAR_V16\PROJE\ONLINE_SIPARIS_KARSILAMA_V16\Classes\cls_RenkBeden.vb";
        Encoding enc = Encoding.GetEncoding(1254);
        string text = File.ReadAllText(path, enc);
        
        // Remove old Renk query
        int startRenk = text.IndexOf("Dim commR As New SqlCommand(\"Select rnk_kodu ,rnk_kirilim_1");
        int endRenk = text.IndexOf("\"", startRenk + 28);
        // Wait, the Renk query was long:
        // Dim commR As New SqlCommand("Select rnk_kodu ,rnk_kirilim_1...60 from dbo.STOK_RENK_TANIMLARI where rnk_kodu='" & RenkKod & "'", con)
        // No, that's Renk_Beden_Bul_Siparis!
        // Wait, what about VarGuid_Bul?
        
        // Let's rewrite the ENTIRE VarGuid_Bul function!
        int funcStart = text.IndexOf("Public Function VarGuid_Bul");
        int funcEnd = text.IndexOf("End Function", funcStart) + 12;
        
        string newFunc = 
@"Public Function VarGuid_Bul(ByVal Renk As String, ByVal Beden As String, ByVal StokKod As String) As String
        Dim rnk As String = """"
        Dim bdn As String = """"
        
        If Renk <> """" Then
            Dim sbR As New System.Text.StringBuilder()
            sbR.Append(""SELECT TOP 1 * FROM ( "")
            sbR.Append(""SELECT rnk_kodu, rnk_kirilim_1 as r FROM STOK_RENK_TANIMLARI"")
";
        for (int i=2; i<=60; i++) {
            newFunc += "            sbR.Append(\" UNION ALL SELECT rnk_kodu, rnk_kirilim_" + i + " FROM STOK_RENK_TANIMLARI\")\r\n";
        }
        newFunc += @"            sbR.Append("" ) AS X WHERE r = '"" & Renk.Replace(""'"", ""''"") & ""'"")
            Dim commR As New SqlCommand(sbR.ToString(), con)
            Try
                If con.State = ConnectionState.Closed Then con.Open()
                Dim val As Object = commR.ExecuteScalar()
                If Not val Is Nothing AndAlso Not DBNull.Value.Equals(val) Then
                    rnk = val.ToString()
                End If
            Catch ex As Exception
            End Try
        End If
        
        If Beden <> """" Then
            Dim sbB As New System.Text.StringBuilder()
            sbB.Append(""SELECT TOP 1 * FROM ( "")
            sbB.Append(""SELECT bdn_kodu, bdn_kirilim_1 as b FROM STOK_BEDEN_TANIMLARI"")
";
        for (int i=2; i<=40; i++) {
            newFunc += "            sbB.Append(\" UNION ALL SELECT bdn_kodu, bdn_kirilim_" + i + " FROM STOK_BEDEN_TANIMLARI\")\r\n";
        }
        newFunc += @"            sbB.Append("" ) AS X WHERE b = '"" & Beden.Replace(""'"", ""''"") & ""'"")
            Dim commB As New SqlCommand(sbB.ToString(), con)
            Try
                If con.State = ConnectionState.Closed Then con.Open()
                Dim val As Object = commB.ExecuteScalar()
                If Not val Is Nothing AndAlso Not DBNull.Value.Equals(val) Then
                    bdn = val.ToString()
                End If
            Catch ex As Exception
            End Try
        End If

        Dim VarGuid As String = Guid.Empty.ToString()
        Dim comm As New SqlCommand(""SELECT TOP 1 CAST(VBag_Guid AS VARCHAR(36)) FROM dbo.VARYANT_BAGLANTI_TANIMLARI WHERE VBag_VaryantKod='"" & StokKod & ""' AND (VBag_KirilimKod='"" & rnk & ""' OR VBag_KirilimKod='"" & bdn & ""') AND VBag_Tip=0"", con)
        Try
            If con.State = ConnectionState.Closed Then con.Open()
            Dim val As Object = comm.ExecuteScalar()
            If Not val Is Nothing AndAlso Not DBNull.Value.Equals(val) Then
                VarGuid = val.ToString()
            End If
        Catch ex As Exception
        End Try
        If con.State = ConnectionState.Open Then con.Close()

        Return VarGuid
    End Function";

        string replaced = text.Substring(0, funcStart) + newFunc + text.Substring(funcEnd);
        File.WriteAllText(path, replaced, enc);
    }
}