Imports System.Collections
Imports System.Management
Public Class News

    Private _rkey As String

    Public Property Rkey() As String
        Get
            Return _rkey
        End Get
        Set(ByVal value As String)
            _rkey = value
        End Set
    End Property

    Public Function Gt(ByVal v As String, ByVal ss As String) As Boolean
        Dim gv As New MachineInfo.GetInfo
        Dim z As String
        z = temin.tex

        Dim crt As New Encrt
        Dim alac As String

        Dim _h_key As String
        Dim _h As String
        Dim a As Integer
        If v.Length < 48 Then
            _h_key = ""
        End If
        For a = 0 To v.Length - 2 Step 2
            _h = v.Substring(a, 2)
            Dim value As Long
            Try
                value = Long.Parse(_h, Globalization.NumberStyles.HexNumber)
            Catch ex As Exception
                Return False
            End Try

            _h = ChrW(value)
            _h_key = _h_key + _h
        Next
        If _h_key.Length < 24 Then
            Return False
        End If
        '----- DEÐÝÞTÝR  "M-SIPARIS-TAKIP"
        alac = crt.Cariisim(_h_key, ss)

        If z = alac Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Function Gt(ByVal v As String, ByVal ss As String, ByVal ky As String) As Boolean
        Dim gv As New MachineInfo.GetInfo

        Dim z As String
        z = ky 'temin.tex

        Dim crt As New Encrt
        Dim alac As String

        Dim _h_key As String
        Dim _h As String
        Dim a As Integer
        If v.Length < 48 Then
            _h_key = ""
        End If
        For a = 0 To v.Length - 2 Step 2
            _h = v.Substring(a, 2)
            Dim value As Long
            Try
                value = Long.Parse(_h, Globalization.NumberStyles.HexNumber)
            Catch ex As Exception
                Return False
            End Try

            _h = ChrW(value)
            _h_key = _h_key + _h
        Next
        'If _h_key.Length < 24 Then
        '    Return False
        'End If
        '----- DEÐÝÞTÝR  "M-SIPARIS-TAKIP"
        alac = crt.Cariisim(_h_key, ss)

        If z = alac Then
            Return True
        Else
            Return False
        End If
    End Function


    Public Function KodUret(ByVal Kod As String, ByVal Key As String) As String

        Dim m_key As String

        m_key = Key


        Dim obj As New Encrt
        Dim _key As String
        _key = obj.CariHesKontrol(Kod, m_key)


        Dim _h_key As String
        Dim _h As String
        Dim a As Integer
        For a = 0 To _key.Length - 1
            _h = _key.Substring(a, 1)
            _h = Asc(_h)
            _h_key = _h_key + Hex(CInt(_h))
        Next

        Dim _x As String
        Dim _x_key As String

        _h_key = _h_key.Substring(0, _h_key.Length - 4)

        _x = _x & "-" & _h_key.Substring(0, 8)

        _h_key = _h_key.Substring(8, _h_key.Length - 8)



        For a = 0 To _h_key.Length - 6 Step 6
            _x = _x & "-" & _h_key.Substring(a, 6)
        Next

        Return _x.Substring(1, Len(_x) - 1)
    End Function


End Class
