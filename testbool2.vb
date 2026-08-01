Imports System
Module Module1
    Sub Main()
        Try
            Console.WriteLine(CBool("1"))
        Catch ex As Exception
            Console.WriteLine("Error parsing 1: " & ex.Message)
        End Try
        Try
            Console.WriteLine(CBool("True"))
        Catch ex As Exception
            Console.WriteLine("Error parsing True")
        End Try
    End Sub
End Module