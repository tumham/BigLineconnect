Imports System
Module Module1
    Sub Main()
        Try
            Console.WriteLine(CBool("1"))
        Catch ex As Exception
            Console.WriteLine("error 1")
        End Try
        Try
            Console.WriteLine(CBool("-1"))
        Catch ex As Exception
            Console.WriteLine("error -1")
        End Try
        Try
            Console.WriteLine(CBool("True"))
        Catch ex As Exception
            Console.WriteLine("error True")
        End Try
    End Sub
End Module