Public Class temin
    Public Shared Function tex()
        Dim gv As New MachineInfo.GetInfo


        Dim ma As String = gv.GetMACAddress
        Dim cp As String = gv.GetCPUId


        Dim mac_no As String
        Dim cpu_no As String
        Dim hd_no As String

        Dim a As Integer
        Dim M As String

        For a = 0 To Len(ma) - 2 Step 2
            M = Asc(ma.Substring(a, 2))
            mac_no = mac_no + M
        Next
        For a = 0 To Len(cp) - 2 Step 2
            M = Asc(cp.Substring(a, 2))
            cpu_no = cpu_no + M
        Next

        Return (CDbl(mac_no) + CDbl(cpu_no)) / 20

    End Function
End Class
