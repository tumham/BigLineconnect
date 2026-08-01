namespace Tugi.DepotManagement.UnHandledExceptionManagement
{
    partial class frmException
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmException));
            this.panel1 = new System.Windows.Forms.Panel();
            this.lblExceptionSource = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.txDetails = new System.Windows.Forms.TextBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.tmr = new System.Windows.Forms.Timer(this.components);
            this.gerisay = new System.Windows.Forms.Label();
            this.btn_exit = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.Controls.Add(this.btn_exit);
            this.panel1.Controls.Add(this.gerisay);
            this.panel1.Controls.Add(this.lblExceptionSource);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.button1);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.txDetails);
            this.panel1.Controls.Add(this.pictureBox1);
            this.panel1.Location = new System.Drawing.Point(12, 12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(436, 218);
            this.panel1.TabIndex = 0;
            // 
            // lblExceptionSource
            // 
            this.lblExceptionSource.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.lblExceptionSource.ForeColor = System.Drawing.Color.Red;
            this.lblExceptionSource.Location = new System.Drawing.Point(122, 7);
            this.lblExceptionSource.Name = "lblExceptionSource";
            this.lblExceptionSource.Size = new System.Drawing.Size(233, 64);
            this.lblExceptionSource.TabIndex = 13;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(5, 7);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(89, 13);
            this.label3.TabIndex = 12;
            this.label3.Text = "HATA KAYNAÐI:";
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.button1.ForeColor = System.Drawing.Color.OrangeRed;
            this.button1.Location = new System.Drawing.Point(361, 74);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(64, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "KAPAT";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 84);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(46, 13);
            this.label2.TabIndex = 11;
            this.label2.Text = "DETAY:";
            // 
            // txDetails
            // 
            this.txDetails.Location = new System.Drawing.Point(17, 103);
            this.txDetails.Multiline = true;
            this.txDetails.Name = "txDetails";
            this.txDetails.Size = new System.Drawing.Size(408, 100);
            this.txDetails.TabIndex = 10;
            this.txDetails.Text = "txDetails";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::Tugi.DepotManagement.UnHandledExceptionManagement.Properties.Resources.vlc;
            this.pictureBox1.Location = new System.Drawing.Point(361, 7);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(64, 64);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 9;
            this.pictureBox1.TabStop = false;
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
            this.gerisay.Location = new System.Drawing.Point(25, 38);
            this.gerisay.Name = "gerisay";
            this.gerisay.Size = new System.Drawing.Size(29, 20);
            this.gerisay.TabIndex = 14;
            this.gerisay.Text = "10";
            this.gerisay.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btn_exit
            // 
            this.btn_exit.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.btn_exit.ForeColor = System.Drawing.Color.OrangeRed;
            this.btn_exit.Location = new System.Drawing.Point(214, 74);
            this.btn_exit.Name = "btn_exit";
            this.btn_exit.Size = new System.Drawing.Size(141, 23);
            this.btn_exit.TabIndex = 15;
            this.btn_exit.Text = "PROGRAMDAN ÇIK";
            this.btn_exit.UseVisualStyleBackColor = true;
            this.btn_exit.Click += new System.EventHandler(this.btn_exit_Click);
            // 
            // frmException
            // 
            this.AcceptButton = this.button1;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.DimGray;
            this.ClientSize = new System.Drawing.Size(456, 242);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmException";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Ýstenmeyen bir hata meydana geldi.";
            this.Load += new System.EventHandler(this.frmException_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmException_FormClosing);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txDetails;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label lblExceptionSource;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label gerisay;
        private System.Windows.Forms.Timer tmr;
        private System.Windows.Forms.Button btn_exit;


    }
}