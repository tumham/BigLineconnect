namespace Bigus.Aktarici.WinApp
{
    partial class SqlAyar
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.toolTipController1 = new DevExpress.Utils.ToolTipController(this.components);
            this.GroupBox1 = new System.Windows.Forms.GroupBox();
            this.SqlA = new System.Windows.Forms.RadioButton();
            this.WindowsA = new System.Windows.Forms.RadioButton();
            this.Label2 = new System.Windows.Forms.Label();
            this.Label3 = new System.Windows.Forms.Label();
            this.Label4 = new System.Windows.Forms.Label();
            this.grb_baslik = new DevExpress.XtraEditors.GroupControl();
            this.simpleButton4 = new DevExpress.XtraEditors.SimpleButton();
            this.simpleButton1 = new DevExpress.XtraEditors.SimpleButton();
            this.simpleButton2 = new DevExpress.XtraEditors.SimpleButton();
            this.textEdit2 = new DevExpress.XtraEditors.MemoEdit();
            this.label1 = new System.Windows.Forms.Label();
            this.textEdit1 = new DevExpress.XtraEditors.TextEdit();
            this.label5 = new System.Windows.Forms.Label();
            this.smpkaydet = new DevExpress.XtraEditors.SimpleButton();
            this.pas = new DevExpress.XtraEditors.TextEdit();
            this.serv = new DevExpress.XtraEditors.TextEdit();
            this.kul = new DevExpress.XtraEditors.TextEdit();
            this.GroupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grb_baslik)).BeginInit();
            this.grb_baslik.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.textEdit2.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEdit1.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pas.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.serv.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.kul.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // GroupBox1
            // 
            this.GroupBox1.Controls.Add(this.SqlA);
            this.GroupBox1.Controls.Add(this.WindowsA);
            this.GroupBox1.Location = new System.Drawing.Point(79, 52);
            this.GroupBox1.Name = "GroupBox1";
            this.GroupBox1.Size = new System.Drawing.Size(142, 50);
            this.toolTipController1.SetSuperTip(this.GroupBox1, null);
            this.GroupBox1.TabIndex = 2;
            this.GroupBox1.TabStop = false;
            this.GroupBox1.Text = "Authentication";
            // 
            // SqlA
            // 
            this.SqlA.AutoSize = true;
            this.SqlA.Checked = true;
            this.SqlA.Location = new System.Drawing.Point(94, 19);
            this.SqlA.Name = "SqlA";
            this.SqlA.Size = new System.Drawing.Size(44, 17);
            this.toolTipController1.SetSuperTip(this.SqlA, null);
            this.SqlA.TabIndex = 3;
            this.SqlA.TabStop = true;
            this.SqlA.Text = "SQL";
            this.SqlA.UseVisualStyleBackColor = true;
            this.SqlA.CheckedChanged += new System.EventHandler(this.SqlA_CheckedChanged);
            // 
            // WindowsA
            // 
            this.WindowsA.AutoSize = true;
            this.WindowsA.Location = new System.Drawing.Point(6, 19);
            this.WindowsA.Name = "WindowsA";
            this.WindowsA.Size = new System.Drawing.Size(68, 17);
            this.toolTipController1.SetSuperTip(this.WindowsA, null);
            this.WindowsA.TabIndex = 4;
            this.WindowsA.TabStop = true;
            this.WindowsA.Text = "Windows";
            this.WindowsA.UseVisualStyleBackColor = true;
            this.WindowsA.CheckedChanged += new System.EventHandler(this.WindowsA_CheckedChanged);
            // 
            // Label2
            // 
            this.Label2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.Label2.ForeColor = System.Drawing.Color.Gray;
            this.Label2.Location = new System.Drawing.Point(5, 31);
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(56, 16);
            this.toolTipController1.SetSuperTip(this.Label2, null);
            this.Label2.TabIndex = 155;
            this.Label2.Text = "Server";
            this.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label3
            // 
            this.Label3.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.Label3.ForeColor = System.Drawing.Color.Gray;
            this.Label3.Location = new System.Drawing.Point(227, 62);
            this.Label3.Name = "Label3";
            this.Label3.Size = new System.Drawing.Size(64, 16);
            this.toolTipController1.SetSuperTip(this.Label3, null);
            this.Label3.TabIndex = 153;
            this.Label3.Text = "User";
            this.Label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // Label4
            // 
            this.Label4.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.Label4.ForeColor = System.Drawing.Color.Gray;
            this.Label4.Location = new System.Drawing.Point(227, 86);
            this.Label4.Name = "Label4";
            this.Label4.Size = new System.Drawing.Size(64, 16);
            this.toolTipController1.SetSuperTip(this.Label4, null);
            this.Label4.TabIndex = 154;
            this.Label4.Text = "Password";
            this.Label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // grb_baslik
            // 
            this.grb_baslik.Controls.Add(this.simpleButton4);
            this.grb_baslik.Controls.Add(this.simpleButton1);
            this.grb_baslik.Controls.Add(this.simpleButton2);
            this.grb_baslik.Controls.Add(this.textEdit2);
            this.grb_baslik.Controls.Add(this.label1);
            this.grb_baslik.Controls.Add(this.textEdit1);
            this.grb_baslik.Controls.Add(this.label5);
            this.grb_baslik.Controls.Add(this.Label2);
            this.grb_baslik.Controls.Add(this.GroupBox1);
            this.grb_baslik.Controls.Add(this.Label4);
            this.grb_baslik.Controls.Add(this.smpkaydet);
            this.grb_baslik.Controls.Add(this.Label3);
            this.grb_baslik.Controls.Add(this.pas);
            this.grb_baslik.Controls.Add(this.serv);
            this.grb_baslik.Controls.Add(this.kul);
            this.grb_baslik.Location = new System.Drawing.Point(12, 12);
            this.grb_baslik.Name = "grb_baslik";
            this.grb_baslik.Size = new System.Drawing.Size(450, 269);
            this.toolTipController1.SetSuperTip(this.grb_baslik, null);
            this.grb_baslik.TabIndex = 163;
            this.grb_baslik.Text = "SQL AYAR";
            // 
            // simpleButton4
            // 
            this.simpleButton4.ImageIndex = 4;
            this.simpleButton4.Location = new System.Drawing.Point(8, 229);
            this.simpleButton4.LookAndFeel.UseDefaultLookAndFeel = false;
            this.simpleButton4.LookAndFeel.UseWindowsXPTheme = true;
            this.simpleButton4.Name = "simpleButton4";
            this.simpleButton4.Size = new System.Drawing.Size(120, 32);
            this.simpleButton4.TabIndex = 170;
            this.simpleButton4.Text = "PROGRAMDAN �IK";
            this.simpleButton4.Click += new System.EventHandler(this.simpleButton4_Click);
            // 
            // simpleButton1
            // 
            this.simpleButton1.ImageIndex = 4;
            this.simpleButton1.Location = new System.Drawing.Point(321, 229);
            this.simpleButton1.LookAndFeel.UseDefaultLookAndFeel = false;
            this.simpleButton1.LookAndFeel.UseWindowsXPTheme = true;
            this.simpleButton1.Name = "simpleButton1";
            this.simpleButton1.Size = new System.Drawing.Size(120, 32);
            this.simpleButton1.TabIndex = 169;
            this.simpleButton1.Text = "TAMAM";
            this.simpleButton1.Click += new System.EventHandler(this.simpleButton1_Click_1);
            // 
            // simpleButton2
            // 
            this.simpleButton2.ImageIndex = 4;
            this.simpleButton2.Location = new System.Drawing.Point(8, 191);
            this.simpleButton2.LookAndFeel.UseDefaultLookAndFeel = false;
            this.simpleButton2.LookAndFeel.UseWindowsXPTheme = true;
            this.simpleButton2.Name = "simpleButton2";
            this.simpleButton2.Size = new System.Drawing.Size(120, 32);
            this.simpleButton2.TabIndex = 10;
            this.simpleButton2.Text = "BA�LANTIYI TEST ET";
            this.simpleButton2.ToolTip = "KAYDET";
            this.simpleButton2.Click += new System.EventHandler(this.simpleButton2_Click);
            // 
            // textEdit2
            // 
            this.textEdit2.Location = new System.Drawing.Point(131, 169);
            this.textEdit2.Name = "textEdit2";
            this.textEdit2.Properties.PasswordChar = '*';
            this.textEdit2.Properties.ReadOnly = true;
            this.textEdit2.Size = new System.Drawing.Size(310, 54);
            this.textEdit2.TabIndex = 168;
            // 
            // label1
            // 
            this.label1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label1.ForeColor = System.Drawing.Color.Gray;
            this.label1.Location = new System.Drawing.Point(128, 150);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(120, 16);
            this.toolTipController1.SetSuperTip(this.label1, null);
            this.label1.TabIndex = 165;
            this.label1.Text = "Connection String";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // textEdit1
            // 
            this.textEdit1.EnterMoveNextControl = true;
            this.textEdit1.Location = new System.Drawing.Point(297, 31);
            this.textEdit1.Name = "textEdit1";
            this.textEdit1.Properties.LookAndFeel.SkinName = "iMaginary";
            this.textEdit1.Properties.LookAndFeel.UseDefaultLookAndFeel = false;
            this.textEdit1.Properties.LookAndFeel.UseWindowsXPTheme = true;
            this.textEdit1.Size = new System.Drawing.Size(144, 20);
            this.textEdit1.TabIndex = 5;
            // 
            // label5
            // 
            this.label5.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.label5.ForeColor = System.Drawing.Color.Gray;
            this.label5.Location = new System.Drawing.Point(227, 32);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(64, 16);
            this.toolTipController1.SetSuperTip(this.label5, null);
            this.label5.TabIndex = 163;
            this.label5.Text = "Firma";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // smpkaydet
            // 
            this.smpkaydet.ImageIndex = 4;
            this.smpkaydet.Location = new System.Drawing.Point(278, 112);
            this.smpkaydet.LookAndFeel.UseDefaultLookAndFeel = false;
            this.smpkaydet.LookAndFeel.UseWindowsXPTheme = true;
            this.smpkaydet.Name = "smpkaydet";
            this.smpkaydet.Size = new System.Drawing.Size(163, 32);
            this.smpkaydet.TabIndex = 8;
            this.smpkaydet.Text = "BA�LANTI C�MLES� OLU�TUR";
            this.smpkaydet.ToolTip = "KAYDET";
            this.smpkaydet.Click += new System.EventHandler(this.smpkaydet_Click);
            // 
            // pas
            // 
            this.pas.EnterMoveNextControl = true;
            this.pas.Location = new System.Drawing.Point(297, 86);
            this.pas.Name = "pas";
            this.pas.Properties.LookAndFeel.SkinName = "iMaginary";
            this.pas.Properties.LookAndFeel.UseDefaultLookAndFeel = false;
            this.pas.Properties.LookAndFeel.UseWindowsXPTheme = true;
            this.pas.Properties.PasswordChar = '*';
            this.pas.Size = new System.Drawing.Size(144, 20);
            this.pas.TabIndex = 7;
            // 
            // serv
            // 
            this.serv.EnterMoveNextControl = true;
            this.serv.Location = new System.Drawing.Point(77, 31);
            this.serv.Name = "serv";
            this.serv.Properties.LookAndFeel.SkinName = "iMaginary";
            this.serv.Properties.LookAndFeel.UseDefaultLookAndFeel = false;
            this.serv.Properties.LookAndFeel.UseWindowsXPTheme = true;
            this.serv.Size = new System.Drawing.Size(144, 20);
            this.serv.TabIndex = 1;
            this.serv.EditValueChanged += new System.EventHandler(this.serv_EditValueChanged);
            // 
            // kul
            // 
            this.kul.EnterMoveNextControl = true;
            this.kul.Location = new System.Drawing.Point(297, 62);
            this.kul.Name = "kul";
            this.kul.Properties.LookAndFeel.SkinName = "iMaginary";
            this.kul.Properties.LookAndFeel.UseDefaultLookAndFeel = false;
            this.kul.Properties.LookAndFeel.UseWindowsXPTheme = true;
            this.kul.Size = new System.Drawing.Size(144, 20);
            this.kul.TabIndex = 6;
            // 
            // SqlAyar
            // 
            this.ClientSize = new System.Drawing.Size(474, 287);
            this.Controls.Add(this.grb_baslik);
            this.MaximizeBox = false;
            this.Name = "SqlAyar";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.toolTipController1.SetSuperTip(this, null);
            this.Text = "SQL AYAR";
            this.Load += new System.EventHandler(this.SqlAyar_Load);
            this.GroupBox1.ResumeLayout(false);
            this.GroupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grb_baslik)).EndInit();
            this.grb_baslik.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.textEdit2.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.textEdit1.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pas.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.serv.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.kul.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.Utils.ToolTipController toolTipController1;
        internal System.Windows.Forms.GroupBox GroupBox1;
        internal System.Windows.Forms.RadioButton SqlA;
        internal System.Windows.Forms.RadioButton WindowsA;
        internal DevExpress.XtraEditors.SimpleButton smpkaydet;
        internal DevExpress.XtraEditors.TextEdit pas;
        internal DevExpress.XtraEditors.TextEdit kul;
        internal DevExpress.XtraEditors.TextEdit serv;
        internal System.Windows.Forms.Label Label2;
        internal System.Windows.Forms.Label Label3;
        internal System.Windows.Forms.Label Label4;
        private DevExpress.XtraEditors.GroupControl grb_baslik;
        internal DevExpress.XtraEditors.TextEdit textEdit1;
        internal System.Windows.Forms.Label label5;
        internal System.Windows.Forms.Label label1;
        private DevExpress.XtraEditors.MemoEdit textEdit2;
        internal DevExpress.XtraEditors.SimpleButton simpleButton2;
        internal DevExpress.XtraEditors.SimpleButton simpleButton1;
        internal DevExpress.XtraEditors.SimpleButton simpleButton4;
    }
}