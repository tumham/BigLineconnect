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
    public partial class frm_Main : Form
    {
        public frm_Main()
        {
            InitializeComponent();
        }

        private void frm_Main_Load(object sender, EventArgs e)
        {
            DatabaseFacade.SettingsOku();
            DatabaseFacade2.SettingsOku();
     
        }



        void Stok_SeriNo_Tanimlari_Aktarimi()
        {
            List<STOK_SERINO_TANIMLARI> ls_stok_serino = new List<STOK_SERINO_TANIMLARI>();


            ls_stok_serino = KartAktarimlari.Stok_SeriNo_Tanimlarini_Yukle(DatabaseFacade.ConnectionString());

            if (ls_stok_serino.Count > 0)
            {

                foreach (STOK_SERINO_TANIMLARI sst in ls_stok_serino)
                {
                    STOK_SERINO_TANIMLARI sst_1 = sst;
                    STOK_SERINO_TANIMLARI sst_2;

                    sst_2 = KartAktarimlari.Stok_SeriNo_Tanimlari_EvrakDetayGetir(sst_1.chz_stok_kodu, sst_1.chz_serino, DatabaseFacade2.ConnectionString());

                    if (sst_2 != null)
                    {
                        sst_1.chz_Guid = sst_2.chz_Guid;
                        //sst_1.chz_RECid_RECno = sst_2.chz_RECid_RECno;

                        sst_2 = KartAktarimlari.Stok_SeriNo_Tanimlari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());

                    }
                    else
                    {
                        sst_1.chz_Guid = Guid.Empty;
                        //sst_1.chz_RECid_RECno = -1;

                        sst_2 = KartAktarimlari.Stok_SeriNo_Tanimlari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
            }
        }

        void Cari_Hesap_Kartlari_Aktarimi()
        {
            List<CARI_HESAPLAR> ls = new List<CARI_HESAPLAR>();


            ls = KartAktarimlari.Cari_Hesaplari_Yukle(DatabaseFacade.ConnectionString());

            if (ls.Count > 0)
            {

                foreach (CARI_HESAPLAR sst in ls)
                {
                    CARI_HESAPLAR sst_1 = sst;
                    CARI_HESAPLAR sst_2;

                    sst_2 = KartAktarimlari.Cari_Hesaplar_EvrakDetayGetir(sst_1.cari_kod, DatabaseFacade2.ConnectionString());

                    if (sst_2 != null)
                    {
                        sst_1.cari_Guid = sst_2.cari_Guid;
                        //sst_1.cari_RECid_RECno = sst_2.cari_RECid_RECno;

                        sst_2 = KartAktarimlari.Cari_Hesaplari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());

                    }
                    else
                    {
                        sst_1.cari_Guid = Guid.Empty;
                        //sst_1.cari_RECid_RECno = -1;

                        sst_2 = KartAktarimlari.Cari_Hesaplari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
            }

        }


        
        private void simpleButton1_Click(object sender, EventArgs e)
        {
            simpleButton1.Visible = false;
            labelControl1.Text = DateTime.Now.ToLongTimeString();
            Cari_Hesap_Kartlari_Aktarimi();
            // Stok_SeriNo_Tanimlari_Aktarimi();
           // AktarimaBasla3();
            labelControl2.Text = DateTime.Now.ToLongTimeString();
            simpleButton1.Visible = true;

        }

        private void simpleButton2_Click(object sender, EventArgs e)
        {
            simpleButton2.Visible = false;
       
            simpleButton2.Visible = true;
        }


        byte CariHarTipToOdemeTip(byte tip)
        {
            if (tip == 1)
                return 0;
            else if (tip == 2)
                return 1;
            else if (tip == 3)
                return 2;
            else if (tip == 4)
                return 3;
            else if (tip == 17)
                return 4;
            else if (tip == 18)
                return 5;
            else if (tip == 19)
                return 6;
            else if (tip == 20)
                return 7;
            else if (tip == 21)
                return 8;
            else if (tip == 22)
                return 9;


            return 0;
        }

    }
}
