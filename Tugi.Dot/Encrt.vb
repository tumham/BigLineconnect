Imports System.Security.Cryptography
Imports System.IO
Imports System.Text
Public Class Encrt

    Public Function CariHesKontrol(ByVal srm As String, _
                                        ByVal odp As String) As String

        Dim bytValue() As Byte
        Dim bytKey() As Byte
        Dim bytEncoded() As Byte
        Dim bytIV() As Byte = {121, 241, 10, 1, 132, 74, 11, 39, 255, 91, 45, 78, 14, 211, 22, 62}
        Dim intLength As Integer
        Dim intRemaining As Integer
        Dim objMemoryStream As New MemoryStream
        Dim objCryptoStream As CryptoStream
        Dim objRijndaelManaged As RijndaelManaged




        srm = adresler(srm)



        bytValue = Encoding.ASCII.GetBytes(srm.ToCharArray)

        intLength = Len(odp)



        If intLength >= 32 Then
            odp = Strings.Left(odp, 32)
        Else
            intLength = Len(odp)
            intRemaining = 32 - intLength
            odp = odp & Strings.StrDup(intRemaining, "X")
        End If

        bytKey = Encoding.ASCII.GetBytes(odp.ToCharArray)

        objRijndaelManaged = New RijndaelManaged



        Try

            objCryptoStream = New CryptoStream(objMemoryStream, _
              objRijndaelManaged.CreateEncryptor(bytKey, bytIV), _
              CryptoStreamMode.Write)
            objCryptoStream.Write(bytValue, 0, bytValue.Length)

            objCryptoStream.FlushFinalBlock()

            bytEncoded = objMemoryStream.ToArray
            objMemoryStream.Close()
            objCryptoStream.Close()
        Catch
        End Try

        Return Convert.ToBase64String(bytEncoded)

    End Function

    Public Function Cariisim(ByVal chkd As String, _
                                        ByVal recno As String) As String

        Dim bytDataToBeDecrypted() As Byte
        Dim bytTemp() As Byte
        Dim bytIV() As Byte = {121, 241, 10, 1, 132, 74, 11, 39, 255, 91, 45, 78, 14, 211, 22, 62}
        Dim objRijndaelManaged As New RijndaelManaged
        Dim objMemoryStream As MemoryStream
        Dim objCryptoStream As CryptoStream
        Dim bytDecryptionKey() As Byte

        Dim intLength As Integer
        Dim intRemaining As Integer
        Dim intCtr As Integer
        Dim strReturnString As String = String.Empty
        Dim achrCharacterArray() As Char
        Dim intIndex As Integer



        bytDataToBeDecrypted = Convert.FromBase64String(chkd)

        intLength = Len(recno)

        If intLength >= 32 Then
            recno = Strings.Left(recno, 32)
        Else
            intLength = Len(recno)
            intRemaining = 32 - intLength
            recno = recno & Strings.StrDup(intRemaining, "X")
        End If

        bytDecryptionKey = Encoding.ASCII.GetBytes(recno.ToCharArray)

        ReDim bytTemp(bytDataToBeDecrypted.Length)

        objMemoryStream = New MemoryStream(bytDataToBeDecrypted)


        Try

            objCryptoStream = New CryptoStream(objMemoryStream, _
               objRijndaelManaged.CreateDecryptor(bytDecryptionKey, bytIV), _
               CryptoStreamMode.Read)

            objCryptoStream.Read(bytTemp, 0, bytTemp.Length)

            objCryptoStream.FlushFinalBlock()
            objMemoryStream.Close()
            objCryptoStream.Close()

        Catch

        End Try



        Return adresler(Encoding.ASCII.GetString(bytTemp))

    End Function

    Public Function adresler(ByVal cadde As String) As String

        Dim intPosition As Integer
        Dim strStringWithOutNulls As String

        intPosition = 1
        strStringWithOutNulls = cadde

        Do While intPosition > 0
            intPosition = InStr(intPosition, cadde, vbNullChar)

            If intPosition > 0 Then
                strStringWithOutNulls = Left$(strStringWithOutNulls, intPosition - 1) & _
                                  Right$(strStringWithOutNulls, Len(strStringWithOutNulls) - intPosition)
            End If

            If intPosition > strStringWithOutNulls.Length Then
                Exit Do
            End If
        Loop

        Return strStringWithOutNulls

    End Function


End Class
