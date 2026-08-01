using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Bigus.Aktarici.Linq;

namespace Bigus.Aktarici.WinApp
{
    public partial class frm_AktarimSeri : Form
    {
        public frm_AktarimSeri()
        {
            InitializeComponent();
        }

        private void frm_AktarimSeri_Load(object sender, EventArgs e)
        {
            Yukle();
        }

        void Yukle()
        {
            try
            {
                AktarimParametreleri.AktarimSerileri.Clear();
            }
            catch (Exception)
            {
            }
            AktarimParametreleri.AktarimSerileri = AktarimParametreleri.Aktarim_Serilerini_Yukle(DatabaseFacade3.ConnectionString());

            lst_serino.DataSource = AktarimParametreleri.Aktarim_Serilerini_Yukle(DatabaseFacade3.ConnectionString());
            lst_serino.DisplayMember = "ser_serino";
            lst_serino.ValueMember = "ser_no";
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            AKTARIM_SERILERI sr = AktarimParametreleri.Aktarim_Serileri_EvrakDetayGetir(textEdit1.Text, DatabaseFacade3.ConnectionString());
            if (sr != null)
                return;

            sr = new AKTARIM_SERILERI();

            sr.ser_serino = textEdit1.Text;

            AktarimParametreleri.Aktarim_Serilerini_Kaydet(sr, DatabaseFacade3.ConnectionString());
            Yukle();
            textEdit1.Text = "";
        }

        private void simpleButton2_Click(object sender, EventArgs e)
        {
            if (lst_serino.Items.Count > 0)
            {
                if (lst_serino.SelectedIndex >= 0)
                {
                    Int32 no = Convert.ToInt32(lst_serino.SelectedValue);

                    AktarimParametreleri.Aktarim_Serilerini_Sil(no, DatabaseFacade3.ConnectionString());
                    Yukle();
                }

            }
        }
    }
}
