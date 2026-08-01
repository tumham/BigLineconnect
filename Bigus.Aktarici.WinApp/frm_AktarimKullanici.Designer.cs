namespace Bigus.Aktarici.WinApp
{
    partial class frm_AktarimKullanici
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frm_AktarimKullanici));
            this.simpleButton1 = new DevExpress.XtraEditors.SimpleButton();
            this.simpleButton2 = new DevExpress.XtraEditors.SimpleButton();
            this.lst_Kontrol = new System.Windows.Forms.ListBox();
            this.lst_Kullanici = new System.Windows.Forms.ListBox();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl3 = new DevExpress.XtraEditors.LabelControl();
            this.SuspendLayout();
            // 
            // simpleButton1
            // 
            this.simpleButton1.Image = ((System.Drawing.Image)(resources.GetObject("simpleButton1.Image")));
            this.simpleButton1.ImageAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.simpleButton1.Location = new System.Drawing.Point(201, 68);
            this.simpleButton1.Name = "simpleButton1";
            this.simpleButton1.Size = new System.Drawing.Size(70, 23);
            this.simpleButton1.TabIndex = 5;
            this.simpleButton1.Text = "EKLE";
            this.simpleButton1.Click += new System.EventHandler(this.simpleButton1_Click);
            // 
            // simpleButton2
            // 
            this.simpleButton2.Image = ((System.Drawing.Image)(resources.GetObject("simpleButton2.Image")));
            this.simpleButton2.ImageAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.simpleButton2.Location = new System.Drawing.Point(201, 97);
            this.simpleButton2.Name = "simpleButton2";
            this.simpleButton2.Size = new System.Drawing.Size(70, 23);
            this.simpleButton2.TabIndex = 7;
            this.simpleButton2.Text = "ÇIKAR";
            this.simpleButton2.Click += new System.EventHandler(this.simpleButton2_Click);
            // 
            // lst_Kontrol
            // 
            this.lst_Kontrol.FormattingEnabled = true;
            this.lst_Kontrol.Location = new System.Drawing.Point(277, 68);
            this.lst_Kontrol.Name = "lst_Kontrol";
            this.lst_Kontrol.Size = new System.Drawing.Size(188, 238);
            this.lst_Kontrol.TabIndex = 6;
            // 
            // lst_Kullanici
            // 
            this.lst_Kullanici.FormattingEnabled = true;
            this.lst_Kullanici.Location = new System.Drawing.Point(7, 68);
            this.lst_Kullanici.Name = "lst_Kullanici";
            this.lst_Kullanici.Size = new System.Drawing.Size(188, 238);
            this.lst_Kullanici.TabIndex = 8;
            // 
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.labelControl1.Appearance.ForeColor = System.Drawing.Color.Green;
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Appearance.Options.UseForeColor = true;
            this.labelControl1.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
            this.labelControl1.Location = new System.Drawing.Point(7, 6);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(458, 26);
            this.labelControl1.TabIndex = 9;
            this.labelControl1.Text = "KAYNAK VERİTABANINDA OLMAYAN KAYITLAR HEDEF VERİTABANINDAN SİLİNMEKTEDİR. HANGİ K" +
                "ULLANICIYA AİT KAYITLAR SİLİNSİN İSTİYORSUNUZ ?";
            // 
            // labelControl2
            // 
            this.labelControl2.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.labelControl2.Appearance.ForeColor = System.Drawing.Color.Red;
            this.labelControl2.Appearance.Options.UseFont = true;
            this.labelControl2.Appearance.Options.UseForeColor = true;
            this.labelControl2.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
            this.labelControl2.Location = new System.Drawing.Point(7, 49);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(188, 13);
            this.labelControl2.TabIndex = 10;
            this.labelControl2.Text = "KULLANICI LİSTESİ";
            // 
            // labelControl3
            // 
            this.labelControl3.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.labelControl3.Appearance.ForeColor = System.Drawing.Color.Red;
            this.labelControl3.Appearance.Options.UseFont = true;
            this.labelControl3.Appearance.Options.UseForeColor = true;
            this.labelControl3.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.Vertical;
            this.labelControl3.Location = new System.Drawing.Point(277, 38);
            this.labelControl3.Name = "labelControl3";
            this.labelControl3.Size = new System.Drawing.Size(188, 26);
            this.labelControl3.TabIndex = 11;
            this.labelControl3.Text = "KAYDI SİLİNECEK KULLANICILAR LİSTESİ";
            // 
            // frm_AktarimKullanici
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(471, 312);
            this.Controls.Add(this.labelControl3);
            this.Controls.Add(this.labelControl2);
            this.Controls.Add(this.labelControl1);
            this.Controls.Add(this.lst_Kullanici);
            this.Controls.Add(this.simpleButton1);
            this.Controls.Add(this.simpleButton2);
            this.Controls.Add(this.lst_Kontrol);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "frm_AktarimKullanici";
            this.Text = "HEDEF DB\'de KAYDI SİLİNECEK KULLANICILARIN LİSTESİ";
            this.Load += new System.EventHandler(this.frm_AktarimKullanici_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.SimpleButton simpleButton1;
        private DevExpress.XtraEditors.SimpleButton simpleButton2;
        private System.Windows.Forms.ListBox lst_Kontrol;
        private System.Windows.Forms.ListBox lst_Kullanici;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.LabelControl labelControl3;
    }
}