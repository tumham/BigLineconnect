Imports System.Collections
Imports System.Management
Imports System.Windows.Forms

Public Class temsil
    Inherits System.Windows.Forms.Form

#Region " Windows Form Designer generated code "

    Public Sub New()
        MyBase.New()

        'This call is required by the Windows Form Designer.
        InitializeComponent()

        'Add any initialization after the InitializeComponent() call

    End Sub

    'Form overrides dispose to clean up the component list.
    Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing Then
            If Not (components Is Nothing) Then
                components.Dispose()
            End If
        End If
        MyBase.Dispose(disposing)
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    Friend WithEvents PictureBox1 As System.Windows.Forms.PictureBox
    Friend WithEvents Panel2 As System.Windows.Forms.Panel
    Friend WithEvents Panel3 As System.Windows.Forms.Panel
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents PanelControl1 As DevExpress.XtraEditors.PanelControl
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents txtcariad As DevExpress.XtraEditors.TextEdit
    Friend WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents lble As DevExpress.XtraEditors.TextEdit
    Friend WithEvents smpdoldur As DevExpress.XtraEditors.SimpleButton
    Friend WithEvents GroupBox2 As System.Windows.Forms.GroupBox
    Friend WithEvents s12 As DevExpress.XtraEditors.TextEdit
    Friend WithEvents s11 As DevExpress.XtraEditors.TextEdit
    Friend WithEvents s10 As DevExpress.XtraEditors.TextEdit
    Friend WithEvents s9 As DevExpress.XtraEditors.TextEdit
    Friend WithEvents s8 As DevExpress.XtraEditors.TextEdit
    Friend WithEvents s7 As DevExpress.XtraEditors.TextEdit
    Friend WithEvents s6 As DevExpress.XtraEditors.TextEdit
    Friend WithEvents s5 As DevExpress.XtraEditors.TextEdit
    Friend WithEvents s4 As DevExpress.XtraEditors.TextEdit
    Friend WithEvents s3 As DevExpress.XtraEditors.TextEdit
    Friend WithEvents s2 As DevExpress.XtraEditors.TextEdit
    Friend WithEvents s1 As DevExpress.XtraEditors.TextEdit
    Friend WithEvents Label7 As System.Windows.Forms.Label
    Friend WithEvents Label6 As System.Windows.Forms.Label
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents Label4 As System.Windows.Forms.Label
    Friend WithEvents SimpleButton1 As DevExpress.XtraEditors.SimpleButton
    Friend WithEvents nrun As DevExpress.XtraEditors.TextEdit
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(temsil))
        Me.PictureBox1 = New System.Windows.Forms.PictureBox
        Me.Panel2 = New System.Windows.Forms.Panel
        Me.Panel3 = New System.Windows.Forms.Panel
        Me.Label1 = New System.Windows.Forms.Label
        Me.PanelControl1 = New DevExpress.XtraEditors.PanelControl
        Me.nrun = New DevExpress.XtraEditors.TextEdit
        Me.Label4 = New System.Windows.Forms.Label
        Me.Label2 = New System.Windows.Forms.Label
        Me.txtcariad = New DevExpress.XtraEditors.TextEdit
        Me.GroupBox1 = New System.Windows.Forms.GroupBox
        Me.lble = New DevExpress.XtraEditors.TextEdit
        Me.smpdoldur = New DevExpress.XtraEditors.SimpleButton
        Me.GroupBox2 = New System.Windows.Forms.GroupBox
        Me.s12 = New DevExpress.XtraEditors.TextEdit
        Me.s11 = New DevExpress.XtraEditors.TextEdit
        Me.s10 = New DevExpress.XtraEditors.TextEdit
        Me.s9 = New DevExpress.XtraEditors.TextEdit
        Me.s8 = New DevExpress.XtraEditors.TextEdit
        Me.s7 = New DevExpress.XtraEditors.TextEdit
        Me.s6 = New DevExpress.XtraEditors.TextEdit
        Me.s5 = New DevExpress.XtraEditors.TextEdit
        Me.s4 = New DevExpress.XtraEditors.TextEdit
        Me.s3 = New DevExpress.XtraEditors.TextEdit
        Me.s2 = New DevExpress.XtraEditors.TextEdit
        Me.s1 = New DevExpress.XtraEditors.TextEdit
        Me.Label7 = New System.Windows.Forms.Label
        Me.Label6 = New System.Windows.Forms.Label
        Me.Label3 = New System.Windows.Forms.Label
        Me.SimpleButton1 = New DevExpress.XtraEditors.SimpleButton
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PanelControl1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.PanelControl1.SuspendLayout()
        CType(Me.nrun.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.txtcariad.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox1.SuspendLayout()
        CType(Me.lble.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox2.SuspendLayout()
        CType(Me.s12.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.s11.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.s10.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.s9.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.s8.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.s7.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.s6.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.s5.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.s4.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.s3.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.s2.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.s1.Properties, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'PictureBox1
        '
        Me.PictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.PictureBox1.Image = CType(resources.GetObject("PictureBox1.Image"), System.Drawing.Image)
        Me.PictureBox1.Location = New System.Drawing.Point(464, 3)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(96, 80)
        Me.PictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage
        Me.PictureBox1.TabIndex = 9
        Me.PictureBox1.TabStop = False
        '
        'Panel2
        '
        Me.Panel2.BackColor = System.Drawing.Color.SandyBrown
        Me.Panel2.ForeColor = System.Drawing.Color.Snow
        Me.Panel2.Location = New System.Drawing.Point(376, 35)
        Me.Panel2.Name = "Panel2"
        Me.Panel2.Size = New System.Drawing.Size(184, 16)
        Me.Panel2.TabIndex = 10
        '
        'Panel3
        '
        Me.Panel3.BackColor = System.Drawing.Color.Chocolate
        Me.Panel3.ForeColor = System.Drawing.Color.Snow
        Me.Panel3.Location = New System.Drawing.Point(392, 19)
        Me.Panel3.Name = "Panel3"
        Me.Panel3.Size = New System.Drawing.Size(168, 16)
        Me.Panel3.TabIndex = 11
        '
        'Label1
        '
        Me.Label1.Location = New System.Drawing.Point(8, 35)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(336, 16)
        Me.Label1.TabIndex = 8
        '
        'PanelControl1
        '
        Me.PanelControl1.Appearance.BackColor = System.Drawing.Color.SandyBrown
        Me.PanelControl1.Appearance.BackColor2 = System.Drawing.Color.WhiteSmoke
        Me.PanelControl1.Appearance.Options.UseBackColor = True
        Me.PanelControl1.Controls.Add(Me.SimpleButton1)
        Me.PanelControl1.Controls.Add(Me.nrun)
        Me.PanelControl1.Controls.Add(Me.Label4)
        Me.PanelControl1.Controls.Add(Me.Label2)
        Me.PanelControl1.Controls.Add(Me.txtcariad)
        Me.PanelControl1.Controls.Add(Me.GroupBox1)
        Me.PanelControl1.Controls.Add(Me.smpdoldur)
        Me.PanelControl1.Controls.Add(Me.GroupBox2)
        Me.PanelControl1.Controls.Add(Me.Label7)
        Me.PanelControl1.Controls.Add(Me.Label6)
        Me.PanelControl1.Location = New System.Drawing.Point(8, 51)
        Me.PanelControl1.LookAndFeel.SkinName = "Lilian"
        Me.PanelControl1.LookAndFeel.UseDefaultLookAndFeel = False
        Me.PanelControl1.LookAndFeel.UseWindowsXPTheme = True
        Me.PanelControl1.Name = "PanelControl1"
        Me.PanelControl1.Size = New System.Drawing.Size(552, 245)
        Me.PanelControl1.TabIndex = 13
        Me.PanelControl1.Text = "PanelControl1"
        '
        'nrun
        '
        Me.nrun.Location = New System.Drawing.Point(96, 144)
        Me.nrun.Name = "nrun"
        Me.nrun.Size = New System.Drawing.Size(200, 20)
        Me.nrun.TabIndex = 12
        '
        'Label4
        '
        Me.Label4.BackColor = System.Drawing.Color.Transparent
        Me.Label4.Location = New System.Drawing.Point(16, 144)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(72, 23)
        Me.Label4.TabIndex = 11
        Me.Label4.Text = "ÜRÜN NO:"
        Me.Label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'Label2
        '
        Me.Label2.BackColor = System.Drawing.Color.Transparent
        Me.Label2.Location = New System.Drawing.Point(16, 16)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(72, 23)
        Me.Label2.TabIndex = 0
        Me.Label2.Text = "FÝRMA ADI:"
        Me.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'txtcariad
        '
        Me.txtcariad.Location = New System.Drawing.Point(96, 16)
        Me.txtcariad.Name = "txtcariad"
        Me.txtcariad.Size = New System.Drawing.Size(200, 20)
        Me.txtcariad.TabIndex = 1
        '
        'GroupBox1
        '
        Me.GroupBox1.BackColor = System.Drawing.Color.Transparent
        Me.GroupBox1.Controls.Add(Me.lble)
        Me.GroupBox1.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.GroupBox1.Location = New System.Drawing.Point(8, 48)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(536, 48)
        Me.GroupBox1.TabIndex = 8
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "MAK ÝD"
        '
        'lble
        '
        Me.lble.EnterMoveNextControl = True
        Me.lble.Location = New System.Drawing.Point(8, 16)
        Me.lble.Name = "lble"
        Me.lble.Properties.Appearance.BackColor = System.Drawing.Color.Gainsboro
        Me.lble.Properties.Appearance.Options.UseBackColor = True
        Me.lble.Properties.ReadOnly = True
        Me.lble.Size = New System.Drawing.Size(520, 20)
        Me.lble.TabIndex = 20
        '
        'smpdoldur
        '
        Me.smpdoldur.Appearance.BackColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(192, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.smpdoldur.Appearance.BackColor2 = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(224, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.smpdoldur.Appearance.Options.UseBackColor = True
        Me.smpdoldur.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.smpdoldur.Location = New System.Drawing.Point(448, 200)
        Me.smpdoldur.LookAndFeel.SkinName = "Lilian"
        Me.smpdoldur.LookAndFeel.UseDefaultLookAndFeel = False
        Me.smpdoldur.LookAndFeel.UseWindowsXPTheme = True
        Me.smpdoldur.Name = "smpdoldur"
        Me.smpdoldur.Size = New System.Drawing.Size(96, 40)
        Me.smpdoldur.TabIndex = 3
        '
        'GroupBox2
        '
        Me.GroupBox2.BackColor = System.Drawing.Color.Transparent
        Me.GroupBox2.Controls.Add(Me.s12)
        Me.GroupBox2.Controls.Add(Me.s11)
        Me.GroupBox2.Controls.Add(Me.s10)
        Me.GroupBox2.Controls.Add(Me.s9)
        Me.GroupBox2.Controls.Add(Me.s8)
        Me.GroupBox2.Controls.Add(Me.s7)
        Me.GroupBox2.Controls.Add(Me.s6)
        Me.GroupBox2.Controls.Add(Me.s5)
        Me.GroupBox2.Controls.Add(Me.s4)
        Me.GroupBox2.Controls.Add(Me.s3)
        Me.GroupBox2.Controls.Add(Me.s2)
        Me.GroupBox2.Controls.Add(Me.s1)
        Me.GroupBox2.Location = New System.Drawing.Point(8, 168)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(392, 72)
        Me.GroupBox2.TabIndex = 2
        Me.GroupBox2.TabStop = False
        '
        's12
        '
        Me.s12.EnterMoveNextControl = True
        Me.s12.Location = New System.Drawing.Point(328, 40)
        Me.s12.Name = "s12"
        Me.s12.Properties.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.s12.Properties.MaxLength = 4
        Me.s12.Size = New System.Drawing.Size(56, 20)
        Me.s12.TabIndex = 13
        '
        's11
        '
        Me.s11.EnterMoveNextControl = True
        Me.s11.Location = New System.Drawing.Point(264, 40)
        Me.s11.Name = "s11"
        Me.s11.Properties.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.s11.Properties.MaxLength = 4
        Me.s11.Size = New System.Drawing.Size(56, 20)
        Me.s11.TabIndex = 12
        '
        's10
        '
        Me.s10.EnterMoveNextControl = True
        Me.s10.Location = New System.Drawing.Point(200, 40)
        Me.s10.Name = "s10"
        Me.s10.Properties.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.s10.Properties.MaxLength = 4
        Me.s10.Size = New System.Drawing.Size(56, 20)
        Me.s10.TabIndex = 11
        '
        's9
        '
        Me.s9.EnterMoveNextControl = True
        Me.s9.Location = New System.Drawing.Point(136, 40)
        Me.s9.Name = "s9"
        Me.s9.Properties.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.s9.Properties.MaxLength = 4
        Me.s9.Size = New System.Drawing.Size(56, 20)
        Me.s9.TabIndex = 10
        '
        's8
        '
        Me.s8.EnterMoveNextControl = True
        Me.s8.Location = New System.Drawing.Point(72, 40)
        Me.s8.Name = "s8"
        Me.s8.Properties.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.s8.Properties.MaxLength = 4
        Me.s8.Size = New System.Drawing.Size(56, 20)
        Me.s8.TabIndex = 9
        '
        's7
        '
        Me.s7.EnterMoveNextControl = True
        Me.s7.Location = New System.Drawing.Point(8, 40)
        Me.s7.Name = "s7"
        Me.s7.Properties.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.s7.Properties.MaxLength = 4
        Me.s7.Size = New System.Drawing.Size(56, 20)
        Me.s7.TabIndex = 8
        '
        's6
        '
        Me.s6.EnterMoveNextControl = True
        Me.s6.Location = New System.Drawing.Point(328, 16)
        Me.s6.Name = "s6"
        Me.s6.Properties.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.s6.Properties.MaxLength = 4
        Me.s6.Size = New System.Drawing.Size(56, 20)
        Me.s6.TabIndex = 7
        '
        's5
        '
        Me.s5.EnterMoveNextControl = True
        Me.s5.Location = New System.Drawing.Point(264, 16)
        Me.s5.Name = "s5"
        Me.s5.Properties.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.s5.Properties.MaxLength = 4
        Me.s5.Size = New System.Drawing.Size(56, 20)
        Me.s5.TabIndex = 6
        '
        's4
        '
        Me.s4.EnterMoveNextControl = True
        Me.s4.Location = New System.Drawing.Point(200, 16)
        Me.s4.Name = "s4"
        Me.s4.Properties.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.s4.Properties.MaxLength = 4
        Me.s4.Size = New System.Drawing.Size(56, 20)
        Me.s4.TabIndex = 5
        '
        's3
        '
        Me.s3.EnterMoveNextControl = True
        Me.s3.Location = New System.Drawing.Point(136, 16)
        Me.s3.Name = "s3"
        Me.s3.Properties.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.s3.Properties.MaxLength = 4
        Me.s3.Size = New System.Drawing.Size(56, 20)
        Me.s3.TabIndex = 4
        '
        's2
        '
        Me.s2.EnterMoveNextControl = True
        Me.s2.Location = New System.Drawing.Point(72, 16)
        Me.s2.Name = "s2"
        Me.s2.Properties.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.s2.Properties.MaxLength = 4
        Me.s2.Size = New System.Drawing.Size(56, 20)
        Me.s2.TabIndex = 3
        '
        's1
        '
        Me.s1.EnterMoveNextControl = True
        Me.s1.Location = New System.Drawing.Point(8, 16)
        Me.s1.Name = "s1"
        Me.s1.Properties.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper
        Me.s1.Properties.MaxLength = 4
        Me.s1.Size = New System.Drawing.Size(56, 20)
        Me.s1.TabIndex = 2
        '
        'Label7
        '
        Me.Label7.BackColor = System.Drawing.Color.Transparent
        Me.Label7.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label7.Font = New System.Drawing.Font("Microsoft Sans Serif", 16.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.Label7.Location = New System.Drawing.Point(352, 104)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(192, 32)
        Me.Label7.TabIndex = 10
        Me.Label7.Text = "0 (212) 275 47 00"
        Me.Label7.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'Label6
        '
        Me.Label6.BackColor = System.Drawing.Color.Transparent
        Me.Label6.Location = New System.Drawing.Point(8, 104)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(328, 32)
        Me.Label6.TabIndex = 9
        '
        'Label3
        '
        Me.Label3.Font = New System.Drawing.Font("Microsoft Sans Serif", 8.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(162, Byte))
        Me.Label3.ForeColor = System.Drawing.Color.Chocolate
        Me.Label3.Location = New System.Drawing.Point(8, 3)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(336, 32)
        Me.Label3.TabIndex = 12
        Me.Label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'SimpleButton1
        '
        Me.SimpleButton1.Appearance.BackColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(192, Byte), Integer), CType(CType(128, Byte), Integer))
        Me.SimpleButton1.Appearance.BackColor2 = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(224, Byte), Integer), CType(CType(192, Byte), Integer))
        Me.SimpleButton1.Appearance.Options.UseBackColor = True
        Me.SimpleButton1.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.SimpleButton1.Location = New System.Drawing.Point(448, 154)
        Me.SimpleButton1.LookAndFeel.SkinName = "Lilian"
        Me.SimpleButton1.LookAndFeel.UseDefaultLookAndFeel = False
        Me.SimpleButton1.LookAndFeel.UseWindowsXPTheme = True
        Me.SimpleButton1.Name = "SimpleButton1"
        Me.SimpleButton1.Size = New System.Drawing.Size(96, 40)
        Me.SimpleButton1.TabIndex = 13
        Me.SimpleButton1.Text = "DEMO"
        '
        'temsil
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
        Me.BackColor = System.Drawing.Color.WhiteSmoke
        Me.ClientSize = New System.Drawing.Size(568, 302)
        Me.Controls.Add(Me.PictureBox1)
        Me.Controls.Add(Me.Panel2)
        Me.Controls.Add(Me.Panel3)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.PanelControl1)
        Me.Controls.Add(Me.Label3)
        Me.Name = "temsil"
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PanelControl1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.PanelControl1.ResumeLayout(False)
        CType(Me.nrun.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.txtcariad.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox1.ResumeLayout(False)
        CType(Me.lble.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox2.ResumeLayout(False)
        CType(Me.s12.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.s11.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.s10.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.s9.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.s8.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.s7.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.s6.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.s5.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.s4.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.s3.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.s2.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.s1.Properties, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

#End Region

    Public mj As String

    Public ReadOnly Property urunkod() As String
        Get
            Return nrun.Text
        End Get
    End Property

    Private Sub oad(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load

        GroupBox2.Text = mvc
        Label6.Text = mvl
        smpdoldur.Text = mvb
        Me.Text = mv
        Label1.Text = mvt
        Label3.Text = mv
        Dim mak As String = temin.tex
        Dim m As String
        Dim a As Integer
        m = ""
        For a = 0 To Len(mak) - 3 Step 3
            m = m + "-" + mak.Substring(a, 3)
        Next

        lble.Text = m.Substring(1, Len(m) - 1)

    End Sub
    Private Sub smp(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles smpdoldur.Click

        If Not (s1.Text = "" Or s2.Text = "" Or s3.Text = "" Or s4.Text = "" Or s5.Text = "" Or s6.Text = "" Or s7.Text = "" _
        Or s8.Text = "" Or s9.Text = "" Or s10.Text = "" Or s11.Text = "" Or s12.Text = "") Then

            If Not (s1.Text.Length <> 4 Or s2.Text.Length <> 4 Or s3.Text.Length <> 4 Or s4.Text.Length <> 4 Or s5.Text.Length <> 4 Or s6.Text.Length <> 4 Or s7.Text.Length <> 4 _
Or s8.Text.Length <> 4 Or s9.Text.Length <> 4 Or s10.Text.Length <> 4 Or s11.Text.Length <> 4 Or s12.Text.Length <> 4) Then



                Dim n As New News

                Me.Cursor = Cursors.WaitCursor

                Dim tz As String

                tz = s1.Text & s2.Text & s3.Text & s4.Text & s5.Text & s6.Text & s7.Text & s8.Text & _
                s9.Text & s10.Text & s11.Text & s12.Text
                mj = tz
                If n.Gt(tz, "M-SIPARIS-TAKIP") = True Then
                    MessageBox.Show(bv)
                    Me.Close()
                Else
                    MessageBox.Show(cv)
                End If
                Me.Cursor = Cursors.Default

            Else
                MessageBox.Show("Eksik girdiniz!")
            End If
        Else
            MessageBox.Show("Eksik girdiniz!")
        End If
    End Sub

    Private Sub SimpleButton1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SimpleButton1.Click
        s1.Text = "0000"
        s2.Text = "0000"
        s3.Text = "0000"
        s4.Text = "0000"
        s5.Text = "0000"
        s6.Text = "0000"
        s7.Text = "0000"
        s8.Text = "0000"
        s9.Text = "0000"
        s10.Text = "0000"
        s11.Text = "0000"
        s12.Text = "0000"

    End Sub
End Class
