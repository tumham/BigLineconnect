namespace Bigus.Aktarici.WinApp
{
    partial class frm_MesajPencere
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frm_MesajPencere));
            this.groupControl1 = new DevExpress.XtraEditors.GroupControl();
            this.buton2 = new DevExpress.XtraEditors.SimpleButton();
            this.buton1 = new DevExpress.XtraEditors.SimpleButton();
            this.panelControl1 = new DevExpress.XtraEditors.PanelControl();
            this.lblmesaj = new DevExpress.XtraEditors.MemoEdit();
            ((System.ComponentModel.ISupportInitialize)(this.groupControl1)).BeginInit();
            this.groupControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).BeginInit();
            this.panelControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.lblmesaj.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // groupControl1
            // 
            this.groupControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.groupControl1.Appearance.Options.UseFont = true;
            this.groupControl1.AppearanceCaption.BackColor = System.Drawing.Color.Red;
            this.groupControl1.AppearanceCaption.Options.UseBackColor = true;
            this.groupControl1.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Style3D;
            this.groupControl1.Controls.Add(this.buton2);
            this.groupControl1.Controls.Add(this.buton1);
            this.groupControl1.Controls.Add(this.panelControl1);
            this.groupControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupControl1.Location = new System.Drawing.Point(0, 0);
            this.groupControl1.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Office2003;
            this.groupControl1.LookAndFeel.UseDefaultLookAndFeel = false;
            this.groupControl1.Name = "groupControl1";
            this.groupControl1.Size = new System.Drawing.Size(271, 246);
            this.groupControl1.TabIndex = 0;
            this.groupControl1.Text = "UYARI MESAJI";
            // 
            // buton2
            // 
            this.buton2.Appearance.BackColor = System.Drawing.Color.Silver;
            this.buton2.Appearance.BackColor2 = System.Drawing.Color.Silver;
            this.buton2.Appearance.BorderColor = System.Drawing.SystemColors.MenuBar;
            this.buton2.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.buton2.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buton2.Appearance.Options.UseBackColor = true;
            this.buton2.Appearance.Options.UseBorderColor = true;
            this.buton2.Appearance.Options.UseFont = true;
            this.buton2.Appearance.Options.UseForeColor = true;
            this.buton2.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.Flat;
            this.buton2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buton2.Image = ((System.Drawing.Image)(resources.GetObject("buton2.Image")));
            this.buton2.Location = new System.Drawing.Point(164, 219);
            this.buton2.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Office2003;
            this.buton2.LookAndFeel.UseDefaultLookAndFeel = false;
            this.buton2.Name = "buton2";
            this.buton2.Size = new System.Drawing.Size(100, 20);
            this.buton2.TabIndex = 2;
            this.buton2.Click += new System.EventHandler(this.buton2_Click);
            // 
            // buton1
            // 
            this.buton1.Appearance.BackColor = System.Drawing.Color.Silver;
            this.buton1.Appearance.BackColor2 = System.Drawing.Color.Silver;
            this.buton1.Appearance.BorderColor = System.Drawing.SystemColors.MenuBar;
            this.buton1.Appearance.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.buton1.Appearance.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.buton1.Appearance.Options.UseBackColor = true;
            this.buton1.Appearance.Options.UseBorderColor = true;
            this.buton1.Appearance.Options.UseFont = true;
            this.buton1.Appearance.Options.UseForeColor = true;
            this.buton1.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.Flat;
            this.buton1.Image = ((System.Drawing.Image)(resources.GetObject("buton1.Image")));
            this.buton1.Location = new System.Drawing.Point(6, 219);
            this.buton1.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Office2003;
            this.buton1.LookAndFeel.UseDefaultLookAndFeel = false;
            this.buton1.Name = "buton1";
            this.buton1.Size = new System.Drawing.Size(100, 20);
            this.buton1.TabIndex = 1;
            this.buton1.Click += new System.EventHandler(this.buton1_Click);
            // 
            // panelControl1
            // 
            this.panelControl1.Controls.Add(this.lblmesaj);
            this.panelControl1.Location = new System.Drawing.Point(6, 24);
            this.panelControl1.Name = "panelControl1";
            this.panelControl1.Padding = new System.Windows.Forms.Padding(2);
            this.panelControl1.Size = new System.Drawing.Size(258, 189);
            this.panelControl1.TabIndex = 0;
            // 
            // lblmesaj
            // 
            this.lblmesaj.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblmesaj.Location = new System.Drawing.Point(2, 2);
            this.lblmesaj.Name = "lblmesaj";
            this.lblmesaj.Properties.Appearance.BackColor = System.Drawing.Color.Transparent;
            this.lblmesaj.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.lblmesaj.Properties.Appearance.ForeColor = System.Drawing.Color.IndianRed;
            this.lblmesaj.Properties.Appearance.Options.UseBackColor = true;
            this.lblmesaj.Properties.Appearance.Options.UseFont = true;
            this.lblmesaj.Properties.Appearance.Options.UseForeColor = true;
            this.lblmesaj.Properties.LookAndFeel.Style = DevExpress.LookAndFeel.LookAndFeelStyle.Flat;
            this.lblmesaj.Properties.LookAndFeel.UseDefaultLookAndFeel = false;
            this.lblmesaj.Properties.ReadOnly = true;
            this.lblmesaj.Size = new System.Drawing.Size(254, 185);
            this.lblmesaj.TabIndex = 0;
            this.lblmesaj.TabStop = false;
            this.lblmesaj.EditValueChanged += new System.EventHandler(this.lblmesaj_EditValueChanged);
            // 
            // frm_MesajPencere
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(271, 246);
            this.ControlBox = false;
            this.Controls.Add(this.groupControl1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frm_MesajPencere";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "UYARI MESAJI";
            this.Load += new System.EventHandler(this.frm_MesajPencere_Load);
            ((System.ComponentModel.ISupportInitialize)(this.groupControl1)).EndInit();
            this.groupControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).EndInit();
            this.panelControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.lblmesaj.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.GroupControl groupControl1;
        public DevExpress.XtraEditors.SimpleButton buton2;
        public DevExpress.XtraEditors.SimpleButton buton1;
        private DevExpress.XtraEditors.PanelControl panelControl1;
        private DevExpress.XtraEditors.MemoEdit lblmesaj;
    }
}