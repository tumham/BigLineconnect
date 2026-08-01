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
    public partial class frm_AktarimKullanici : Form
    {
        public frm_AktarimKullanici()
        {
            InitializeComponent();
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            if (lst_Kullanici.Items.Count == 0)
                return;
            if (lst_Kullanici.SelectedIndex <0)
                return;

            AKTARIM_KULLANICILARI kul = AktarimParametreleri.Aktarim_Kullanicilari_DetayGetir(Convert.ToInt32(lst_Kullanici.SelectedValue), DatabaseFacade3.ConnectionString());
            if (kul != null)
                return;

            kul = new AKTARIM_KULLANICILARI();
            kul.kul_no = Convert.ToInt32(lst_Kullanici.SelectedValue);
            kul.kul_ad = lst_Kullanici.Text;

            AktarimParametreleri.Aktarim_Kullanicilari_Kaydet(kul, DatabaseFacade3.ConnectionString());
            Yukle();
        }

        private void simpleButton2_Click(object sender, EventArgs e)
        {
            if (lst_Kontrol.Items.Count > 0)
            {
                if (lst_Kontrol.SelectedIndex >= 0)
                {
                    Int32 no = Convert.ToInt32(lst_Kontrol.SelectedValue);

                    AktarimParametreleri.Aktarim_Kullanicilarini_Sil(no, DatabaseFacade3.ConnectionString());
                    Yukle();
                }

            }
        }

        void Yukle()
        {
            try
            {
                AktarimParametreleri.AktarimKullanicilari.Clear();
            }
            catch (Exception)
            {
            }
            AktarimParametreleri.AktarimKullanicilari=AktarimParametreleri.Aktarim_Kullanicilarini_Yukle(DatabaseFacade3.ConnectionString());

            lst_Kontrol.DataSource = AktarimParametreleri.Aktarim_Kullanicilarini_Yukle(DatabaseFacade3.ConnectionString());
            lst_Kontrol.DisplayMember = "kul_ad";
            lst_Kontrol.ValueMember = "kul_no";
        }

        void Yukle_V12_KULLANICILAR()
        {
            lst_Kullanici.DataSource = AktarimParametreleri.Kullanicilari_Yukle();
            lst_Kullanici.DisplayMember = "User_name";
            lst_Kullanici.ValueMember = "User_no";
        }

        private void frm_AktarimKullanici_Load(object sender, EventArgs e)
        {
            Yukle_V12_KULLANICILAR();
            Yukle();
        }
    }
}
