Imports System.Text
Imports System.IO
Imports System.Xml
Imports System.Text.RegularExpressions
Public Class myUp

    Private Shared loginFrm As New Login
    Private Shared AyarFrm As New Ayar
    Private Shared teg As New temsil

    Public Shared Sub Main()
        Application.EnableVisualStyles()
        Application.DoEvents()
        showLoginForm()
    End Sub
    Private Shared Sub showLoginForm()

        Dim pimp As String

        Dim g As New achead
        pimp = g.Rd

        Dim Sm As New News
        If Sm.Gt(pimp) = False Then

            If teg.ShowDialog() = DialogResult.OK Then

                If Sm.Gt(teg.mj) = True Then

                    Dim f As New achead
                    f.Wr(teg.mj, teg.nrun.Text)

                    mdAyar.BaglantiOku()
                    If Not mdAyar.serv = "" Then

                        Dim obj As New cls_Login
                        If obj.BaglantiKontrol = True Then
                            'mdLogin.LoginOku()
                            'If mdLogin.m_firma = "" Or mdLogin.m_kullanici = "" Then

                                Dim mainFrm As New Login
                                Application.Run(mainFrm)

                            'Else

                            '    Dim obj1 As New cls_Login
                            '    If mdLogin.m_sifre Is Nothing Then
                            '        mdLogin.m_sifre = ""
                            '    End If

                            '    Dim onay As Boolean = obj1.Login(mdLogin.m_kullanici, mdLogin.m_sifre)
                            '    If onay = True Then
                            '        Try
                            '            mdLogin.firma = mdLogin.m_firma
                            '            mdLogin.firmaadi = mdLogin.m_firmaadi
                            '            mdLogin.loginuser = mdLogin.m_kullanici
                            '            mdLogin.logindate = Now.Date

                            '            '---------------
                            '            'Dim mainFrm As New frm_SipTakip
                            '            'Application.Run(mainfrm)
                            '            '---------------

                            '        Catch ex As Exception
                            '            MessageBox.Show(ex.Message)
                            '        End Try

                            '    Else
                            '        MessageBox.Show("Kullanici adi veya sifreniz hatali")

                            '        Dim mainFrm As New Login
                            '        Application.Run(mainFrm)

                            '    End If
                            'End If
                        Else

                            Dim Sec As DialogResult
                            Sec = MessageBox.Show("Server ile baglanti kurulumadi." & vbNewLine & " Tekrar denemek istermisiniz?", "FÝRMA DEÐÝÞTÝR", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

                            If Sec = DialogResult.Yes Then
                                AyarFrm.ShowDialog()
                                showLoginForm()
                            Else

                            End If

                        End If

                    Else
                        AyarFrm.ShowDialog()
                        showLoginForm()
                    End If

                    '-----------------------------------------------
                    '-----------------------------------------------
                    '-----------------------------------------------

                Else
                    MessageBox.Show(cv)
                    showLoginForm()
                End If
            Else

            End If
        Else

            '-----------------------------------------------
            '-----------------------------------------------
            '-----------------------------------------------
            mdAyar.BaglantiOku()
            If Not mdAyar.serv = "" Then

                Dim obj As New cls_Login
                If obj.BaglantiKontrol = True Then
                    'mdLogin.LoginOku()
                    'If mdLogin.m_firma = "" Or mdLogin.m_kullanici = "" Then

                        Dim mainFrm As New Login
                        Application.Run(mainFrm)

                    'Else


                    '    Dim obj1 As New cls_Login
                    '    If mdLogin.m_sifre Is Nothing Then
                    '        mdLogin.m_sifre = ""
                    '    End If

                    '    Dim onay As Boolean = obj1.Login(mdLogin.m_kullanici, mdLogin.m_sifre)
                    '    If onay = True Then
                    '        Try
                    '            mdLogin.firma = mdLogin.m_firma
                    '            mdLogin.firmaadi = mdLogin.m_firmaadi
                    '            mdLogin.loginuser = mdLogin.m_kullanici
                    '            logindate = Now.Date

                    '            ' -------------------------------
                    '            'Dim mainFrm As New frm_SipTakip
                    '            'Application.Run(mainfrm)
                    '            ' -------------------------------

                    '        Catch ex As Exception
                    '            MessageBox.Show(ex.Message)

                    '        End Try

                    '    Else
                    '        MessageBox.Show("Kullanici adi veya sifreniz hatali")

                    '        Dim mainFrm As New Login
                    '        Application.Run(mainFrm)

                    '    End If
                    'End If
                Else

                    Dim Sec As DialogResult
                    Sec = MessageBox.Show("Server ile baglanti kurulumadi." & vbNewLine & " Tekrar denemek istermisiniz?", "FÝRMA DEÐÝÞTÝR", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

                    If Sec = DialogResult.Yes Then
                        AyarFrm.ShowDialog()
                        showLoginForm()
                    Else

                    End If

                End If

            Else
                AyarFrm.ShowDialog()
                showLoginForm()
            End If

            '-----------------------------------------------
            '-----------------------------------------------
            '-----------------------------------------------

        End If
    End Sub


End Class
