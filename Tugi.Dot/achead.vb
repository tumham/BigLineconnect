Imports Microsoft.Win32
Public Class achead
    Dim oKey As RegistryKey
    Public Function Rd() As String
        oKey = Registry.LocalMachine.OpenSubKey("SOFTWARE", True).OpenSubKey("VIA BSG", True).OpenSubKey("ZETSIP", True).OpenSubKey("v1.0.0.0", True)

        If CStr(oKey.GetValue("a")) = String.Empty Then
            Return ""
        Else
            Return CStr(Body(oKey.GetValue("a")))
        End If

    End Function

    Public Function Wr(ByVal Kod As String, ByVal rn As String)
        oKey = Registry.LocalMachine.OpenSubKey("SOFTWARE", True).OpenSubKey("VIA BSG", True).OpenSubKey("ZETSIP", True).OpenSubKey("v1.0.0.0", True)
        oKey.SetValue("a", Body(Kod))
        oKey.SetValue("b", Body(rn))
    End Function

    Public Function Rd(ByVal klasor1 As String, ByVal klasor2 As String) As String
        oKey = Registry.LocalMachine.OpenSubKey("SOFTWARE", True).OpenSubKey(klasor1, True).OpenSubKey(klasor2, True)

        If CStr(oKey.GetValue("a")) = String.Empty Then
            Return ""
        Else
            Return CStr(Body(oKey.GetValue("a")))
        End If

    End Function

    Public Function Wr(ByVal Kod As String, ByVal rn As String, ByVal klasor1 As String, ByVal klasor2 As String)
        Registry.LocalMachine.OpenSubKey("SOFTWARE", True).CreateSubKey(klasor1)
        Registry.LocalMachine.OpenSubKey("SOFTWARE", True).OpenSubKey(klasor1, True).CreateSubKey(klasor2)
        oKey = Registry.LocalMachine.OpenSubKey("SOFTWARE", True).OpenSubKey(klasor1, True).OpenSubKey(klasor2, True)
        oKey.SetValue("a", Body(Kod))
        oKey.SetValue("b", Body(rn))
    End Function

    Public Function RdDate() As String
        oKey = Registry.LocalMachine.OpenSubKey("SOFTWARE", True).OpenSubKey("VIA BSG", True).OpenSubKey("ZETSIP", True).OpenSubKey("v1.0.0.0", True)

        If CStr(oKey.GetValue("d")) = String.Empty Then
            Return DateTime.MinValue.ToShortDateString()
        Else
            Return CStr(Body(oKey.GetValue("d")))
        End If

    End Function

    Public Function WrDate(ByVal Kod As String)
        oKey = Registry.LocalMachine.OpenSubKey("SOFTWARE", True).OpenSubKey("VIA BSG", True).OpenSubKey("ZETSIP", True).OpenSubKey("v1.0.0.0", True)
        oKey.SetValue("d", Body(Kod))
    End Function

    Private Function Body( _
    ByVal Text As String) As String
        Dim strTempChar As String, i As Integer
        For i = 1 To Len(Text)
            If Asc(Mid$(Text, i, 1)) < 128 Then
                strTempChar = _
          CType(Asc(Mid$(Text, i, 1)) + 128, String)
            ElseIf Asc(Mid$(Text, i, 1)) > 128 Then
                strTempChar = _
          CType(Asc(Mid$(Text, i, 1)) - 128, String)
            End If
            Mid$(Text, i, 1) = _
                Chr(CType(strTempChar, Integer))
        Next i
        Return Text
    End Function
End Class
