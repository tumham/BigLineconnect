using System;
using System.Windows.Forms;

namespace Bigus.Aktarici.WinApp
{
    public partial class frm_Hakkımızda : Form
    {
        public frm_Hakkımızda()
        {
            InitializeComponent();
        }

        private void frm_Hakkımızda_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void tmr_Tick(object sender, EventArgs e)
        {
            Int32 sayi = Convert.ToInt32(gerisay.Text);

            sayi -= 1;
            gerisay.Text = sayi.ToString();
            if (sayi == 0)
            {
                tmr.Enabled = false;
                this.Close();
            }
        }

        private void frm_Hakkımızda_Load(object sender, EventArgs e)
        {
            //lbl_makina_kod.Text = temin.tex().ToString();
                 
            //achead g = new achead();
            //lbl_lisans_kodu.Text = g.Rd();

            //News sm = new News();
            //sm.Rkey = "M-SIPARIS-TAKIP";

            //if (sm.Gt(lbl_lisans_kodu.Text) == true)
            //{
            //    pnl_lisansli.Visible = true;
            //}
  
        }
    }
}
