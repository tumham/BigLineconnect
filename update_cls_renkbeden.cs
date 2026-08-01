using System;
using System.IO;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string path = @"C:\PROJEV6FORMEDIKAL\ONLINE_SIP_KAR_V16\PROJE\ONLINE_SIPARIS_KARSILAMA_V16\Classes\cls_RenkBeden.vb";
        string content = File.ReadAllText(path, System.Text.Encoding.GetEncoding(1254));
        
        string newFunc = @"    Public Function VarGuid_Bul(ByVal Renk As String, ByVal Beden As String, ByVal StokKod As String) As String
        Dim rnk As String = ""
        Dim bdn As String = ""
        
        If Renk <> "" Then
            Dim commR As New SqlCommand("SELECT TOP 1 * FROM ( SELECT rnk_kodu, rnk_kirilim_1 as r FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_2 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_3 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_4 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_5 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_6 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_7 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_8 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_9 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_10 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_11 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_12 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_13 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_14 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_15 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_16 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_17 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_18 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_19 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_20 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_21 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_22 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_23 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_24 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_25 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_26 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_27 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_28 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_29 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_30 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_31 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_32 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_33 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_34 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_35 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_36 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_37 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_38 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_39 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_40 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_41 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_42 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_43 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_44 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_45 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_46 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_47 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_48 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_49 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_50 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_51 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_52 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_53 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_54 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_55 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_56 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_57 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_58 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_59 FROM STOK_RENK_TANIMLARI UNION ALL SELECT rnk_kodu, rnk_kirilim_60 FROM STOK_RENK_TANIMLARI ) AS X WHERE r = '" & Renk.Replace("'", "''") & "'", con)
            Try
                If con.State = ConnectionState.Closed Then con.Open()
                Dim val As Object = commR.ExecuteScalar()
                If val IsNot Nothing AndAlso Not DBNull.Value.Equals(val) Then
                    rnk = val.ToString()
                End If
            Catch ex As Exception
            End Try
        End If
        
        If Beden <> "" Then
            Dim commB As New SqlCommand("SELECT TOP 1 * FROM ( SELECT bdn_kodu, bdn_kirilim_1 as b FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_2 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_3 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_4 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_5 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_6 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_7 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_8 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_9 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_10 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_11 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_12 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_13 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_14 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_15 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_16 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_17 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_18 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_19 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_20 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_21 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_22 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_23 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_24 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_25 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_26 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_27 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_28 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_29 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_30 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_31 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_32 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_33 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_34 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_35 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_36 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_37 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_38 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_39 FROM STOK_BEDEN_TANIMLARI UNION ALL SELECT bdn_kodu, bdn_kirilim_40 FROM STOK_BEDEN_TANIMLARI ) AS X WHERE b = '" & Beden.Replace("'", "''") & "'", con)
            Try
                If con.State = ConnectionState.Closed Then con.Open()
                Dim val As Object = commB.ExecuteScalar()
                If val IsNot Nothing AndAlso Not DBNull.Value.Equals(val) Then
                    bdn = val.ToString()
                End If
            Catch ex As Exception
            End Try
        End If

        Dim VarGuid As String = Guid.Empty.ToString()
        Dim comm As New SqlCommand("SELECT CAST(vrg_Guid AS VARCHAR(36)) FROM dbo.VARYANT_BAGLANTI_TANIMLARI WHERE vrg_stok_kod='" & StokKod & "' AND ISNULL(vrg_renk_kodu,'')='" & rnk & "' AND ISNULL(vrg_beden_kodu,'')='" & bdn & "'", con)
        Try
            If con.State = ConnectionState.Closed Then con.Open()
            Dim val As Object = comm.ExecuteScalar()
            If val IsNot Nothing AndAlso Not DBNull.Value.Equals(val) Then
                VarGuid = val.ToString()
            End If
        Catch ex As Exception
        End Try
        If con.State = ConnectionState.Open Then con.Close()

        Return VarGuid
    End Function

#End Region"

        content = content.Replace("#End Region", newFunc);
        File.WriteAllText(path, content, System.Text.Encoding.GetEncoding(1254));
        Console.WriteLine("Added VarGuid_Bul");
    }
}