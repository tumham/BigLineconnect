namespace Bigus.Aktarici.WinApp
{
    partial class frm_Hakkımızda
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frm_Hakkımızda));
            this.labelControl3 = new DevExpress.XtraEditors.LabelControl();
            this.tmr = new System.Windows.Forms.Timer(this.components);
            this.gerisay = new System.Windows.Forms.Label();
            this.pnl_lisansli = new System.Windows.Forms.Panel();
            this.lbl_lisans_kodu = new DevExpress.XtraEditors.MemoEdit();
            this.lbl_makina_kod = new DevExpress.XtraEditors.TextEdit();
            this.labelControl7 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl6 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl5 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl4 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.pb_hakkimizda = new System.Windows.Forms.PictureBox();
            this.pnl_lisansli.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lbl_lisans_kodu.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.lbl_makina_kod.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_hakkimizda)).BeginInit();
            this.SuspendLayout();
            // 
            // labelControl3
            // 
            this.labelControl3.Appearance.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Bold);
            this.labelControl3.Appearance.Options.UseFont = true;
            this.labelControl3.Location = new System.Drawing.Point(12, 113);
            this.labelControl3.Name = "labelControl3";
            this.labelControl3.Size = new System.Drawing.Size(103, 16);
            this.labelControl3.TabIndex = 43;
            this.labelControl3.Text = "BİGUS AKTARICI";
            // 
            // tmr
            // 
            this.tmr.Enabled = true;
            this.tmr.Interval = 1000;
            this.tmr.Tick += new System.EventHandler(this.tmr_Tick);
            // 
            // gerisay
            // 
            this.gerisay.AutoSize = true;
            this.gerisay.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.gerisay.Location = new System.Drawing.Point(360, 110);
            this.gerisay.Name = "gerisay";
            this.gerisay.Size = new System.Drawing.Size(29, 20);
            this.gerisay.TabIndex = 46;
            this.gerisay.Text = "60";
            this.gerisay.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.gerisay.Visible = false;
            // 
            // pnl_lisansli
            // 
            this.pnl_lisansli.Controls.Add(this.lbl_lisans_kodu);
            this.pnl_lisansli.Controls.Add(this.lbl_makina_kod);
            this.pnl_lisansli.Controls.Add(this.labelControl7);
            this.pnl_lisansli.Controls.Add(this.labelControl6);
            this.pnl_lisansli.Location = new System.Drawing.Point(12, 135);
            this.pnl_lisansli.Name = "pnl_lisansli";
            this.pnl_lisansli.Size = new System.Drawing.Size(377, 100);
            this.pnl_lisansli.TabIndex = 47;
            this.pnl_lisansli.Visible = false;
            // 
            // lbl_lisans_kodu
            // 
            this.lbl_lisans_kodu.Location = new System.Drawing.Point(104, 37);
            this.lbl_lisans_kodu.Name = "lbl_lisans_kodu";
            this.lbl_lisans_kodu.Properties.ReadOnly = true;
            this.lbl_lisans_kodu.Size = new System.Drawing.Size(269, 58);
            this.lbl_lisans_kodu.TabIndex = 48;
            // 
            // lbl_makina_kod
            // 
            this.lbl_makina_kod.Location = new System.Drawing.Point(104, 11);
            this.lbl_makina_kod.Name = "lbl_makina_kod";
            this.lbl_makina_kod.Properties.ReadOnly = true;
            this.lbl_makina_kod.Size = new System.Drawing.Size(269, 20);
            this.lbl_makina_kod.TabIndex = 47;
            // 
            // labelControl7
            // 
            this.labelControl7.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.labelControl7.Appearance.ForeColor = System.Drawing.Color.OrangeRed;
            this.labelControl7.Appearance.Options.UseFont = true;
            this.labelControl7.Appearance.Options.UseForeColor = true;
            this.labelControl7.Location = new System.Drawing.Point(12, 40);
            this.labelControl7.Name = "labelControl7";
            this.labelControl7.Size = new System.Drawing.Size(69, 13);
            this.labelControl7.TabIndex = 1;
            this.labelControl7.Text = "Lisans Kodu:";
            // 
            // labelControl6
            // 
            this.labelControl6.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.labelControl6.Appearance.ForeColor = System.Drawing.Color.OrangeRed;
            this.labelControl6.Appearance.Options.UseFont = true;
            this.labelControl6.Appearance.Options.UseForeColor = true;
            this.labelControl6.Location = new System.Drawing.Point(12, 14);
            this.labelControl6.Name = "labelControl6";
            this.labelControl6.Size = new System.Drawing.Size(75, 13);
            this.labelControl6.TabIndex = 0;
            this.labelControl6.Text = "Makina Kodu:";
            // 
            // labelControl5
            // 
            this.labelControl5.Appearance.Options.UseTextOptions = true;
            this.labelControl5.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.labelControl5.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Top;
            this.labelControl5.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.labelControl5.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.labelControl5.Location = new System.Drawing.Point(97, 86);
            this.labelControl5.Name = "labelControl5";
            this.labelControl5.Size = new System.Drawing.Size(240, 17);
            this.labelControl5.TabIndex = 52;
            this.labelControl5.Text = "info@bigus.com.tr";
            // 
            // labelControl4
            // 
            this.labelControl4.Appearance.Options.UseTextOptions = true;
            this.labelControl4.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.labelControl4.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Top;
            this.labelControl4.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.labelControl4.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.labelControl4.Location = new System.Drawing.Point(97, 63);
            this.labelControl4.Name = "labelControl4";
            this.labelControl4.Size = new System.Drawing.Size(240, 17);
            this.labelControl4.TabIndex = 51;
            this.labelControl4.Text = "(212) 275 47 00 pbx - (212) 275 47 40 Fax ";
            // 
            // labelControl2
            // 
            this.labelControl2.Appearance.Options.UseTextOptions = true;
            this.labelControl2.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.labelControl2.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Top;
            this.labelControl2.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.labelControl2.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.labelControl2.Location = new System.Drawing.Point(97, 31);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(240, 26);
            this.labelControl2.TabIndex = 50;
            this.labelControl2.Text = "Fulya Mah.  Ortaklar Cad.  Onur Apt.No:2  kat:5  Daire:7 34394  Mecidiyeköy / İst" +
                "anbul";
            // 
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.labelControl1.Appearance.ForeColor = System.Drawing.Color.OrangeRed;
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Appearance.Options.UseForeColor = true;
            this.labelControl1.Location = new System.Drawing.Point(73, 12);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(312, 13);
            this.labelControl1.TabIndex = 49;
            this.labelControl1.Text = "Bigus Bilişim Geliştirme ve Uygulama Sistemleri Ltd. Şti.";
            // 
            // pb_hakkimizda
            // 
            this.pb_hakkimizda.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pb_hakkimizda.Image = ((System.Drawing.Image)(resources.GetObject("pb_hakkimizda.Image")));
            this.pb_hakkimizda.Location = new System.Drawing.Point(8, 12);
            this.pb_hakkimizda.Name = "pb_hakkimizda";
            this.pb_hakkimizda.Size = new System.Drawing.Size(59, 45);
            this.pb_hakkimizda.TabIndex = 48;
            this.pb_hakkimizda.TabStop = false;
            // 
            // frm_Hakkımızda
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(397, 243);
            this.Controls.Add(this.labelControl5);
            this.Controls.Add(this.labelControl4);
            this.Controls.Add(this.labelControl2);
            this.Controls.Add(this.labelControl1);
            this.Controls.Add(this.pb_hakkimizda);
            this.Controls.Add(this.pnl_lisansli);
            this.Controls.Add(this.gerisay);
            this.Controls.Add(this.labelControl3);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frm_Hakkımızda";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "BİGUS AKTARICI";
            this.Load += new System.EventHandler(this.frm_Hakkımızda_Load);
            this.Click += new System.EventHandler(this.frm_Hakkımızda_Click);
            this.pnl_lisansli.ResumeLayout(false);
            this.pnl_lisansli.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lbl_lisans_kodu.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.lbl_makina_kod.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pb_hakkimizda)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevExpress.XtraEditors.LabelControl labelControl3;
        private System.Windows.Forms.Timer tmr;
        private System.Windows.Forms.Label gerisay;
        private System.Windows.Forms.Panel pnl_lisansli;
        private DevExpress.XtraEditors.LabelControl labelControl7;
        private DevExpress.XtraEditors.LabelControl labelControl6;
        private DevExpress.XtraEditors.MemoEdit lbl_lisans_kodu;
        private DevExpress.XtraEditors.TextEdit lbl_makina_kod;
        private DevExpress.XtraEditors.LabelControl labelControl5;
        private DevExpress.XtraEditors.LabelControl labelControl4;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private System.Windows.Forms.PictureBox pb_hakkimizda;
    }
}