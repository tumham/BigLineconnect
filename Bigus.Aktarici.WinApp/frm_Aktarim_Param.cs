using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Bigus.Aktarici.Linq;
using Microsoft.Win32;


namespace Bigus.Aktarici.WinApp
{
    public partial class frm_Aktarim_Param : Form
    {
        public frm_Aktarim_Param()
        {
            InitializeComponent();
        }

        void ParametreleriYukle()
        {
            List<AKTARIM_PARAMETRELERI> ls = new List<AKTARIM_PARAMETRELERI>();

            ls = AktarimParametreleri.Aktarim_Parametrelerini_Yukle(DatabaseFacade3.ConnectionString());

            if (ls.Count == 0)
            {
                Tools.MesajPenceresi("AKTARIM PARAMETRELERİ TANIMLANMAMIŞ LÜTFEN TANIMLAMA YAPINIZ.");
                gc_par.Tag = null;
                return;
            }
            else
            {
                DetayDoldur(ls[0]);

            }


        }

        void DetayDoldur(AKTARIM_PARAMETRELERI par)
        {
            ce_kart_anlik.Checked = par.akt_kart_anlik.Value;
            ce_evrak_anlik.Checked = par.akt_evrak_anlik.Value;

            ce_kart_gunluk.Checked = par.akt_kart_gunluk.Value;
            ce_evrak_gunluk.Checked = par.akt_evrak_gunluk.Value;

            sp_sure.Text = par.akt_dakika.Value.ToString();
            tm_evrakzaman.Time = par.akt_evrak_aktarim_saat.Value;
            ce_beden.Checked = par.akt_beden_har.Value;
            ce_cihaz.Checked = par.akt_cihaz_har.Value;
            ce_satis.Checked = par.akt_satis_sart.Value;

            //ce_kart_gunluk.Checked = par.akt_kart_gunluk.Value;
            sp_kart_aktarim.Text = par.akt_kart_dakika.Value.ToString();
            tm_kart_gunlukzaman.Time = par.akt_kart_aktarim_saat.Value;

            chkIrstoFat.Checked = par.akt_hedef_irstofat.Value;

            cmb_carihesap.SelectedIndex = par.akt_cari_hesaplar.Value;
            cmb_caribolge.SelectedIndex = par.akt_cari_hesap_bolgeler.Value;
            cmb_carigrup.SelectedIndex = par.akt_cari_hesap_gruplari.Value;
            cmb_cariyetkili.SelectedIndex = par.akt_cari_hesap_yetkilileri.Value;
            cmb_cariadres.SelectedIndex = par.akt_cari_hesap_adresleri.Value;
            cmb_caritasitplaka.SelectedIndex = par.akt_cari_hesap_tasit_plakalari.Value;
            cmb_carisozlesme.SelectedIndex = par.akt_cari_hesap_sozlesmeleri.Value;
            cmb_carimustahsil.SelectedIndex = par.akt_cari_mustahsil_tanimlari.Value;

            cmb_stoklar.SelectedIndex = par.akt_stoklar.Value;
            cmb_stok_anagrup.SelectedIndex = par.akt_stok_ana_gruplari.Value;
            cmb_stokaltgrup.SelectedIndex = par.akt_stok_alt_gruplari.Value;
            cmb_stokmarkalari.SelectedIndex = par.akt_stok_markalari.Value;
            cmb_stokbeden.SelectedIndex = par.akt_stok_beden_tanimlari.Value;
            cmb_stokkalkon.SelectedIndex = par.akt_stok_kalkon.Value;
            cmb_stok_ambalaj.SelectedIndex = par.akt_stok_ambalajlari.Value;
            cmb_muh_grup.SelectedIndex = par.akt_stok_muhasebe_gruplari.Value;
            cmb_anahammadde.SelectedIndex = par.akt_stok_anahammaddeleri.Value;
            cmb_stok_reyonlar.SelectedIndex = par.akt_stok_reyonlari.Value;
            cmb_stok_kalite.SelectedIndex = par.akt_stok_kalite_kontrol_tanimlari.Value;
            cmb_stok_paket.SelectedIndex = par.akt_stok_paket_tanimlari.Value;
            cmb_stok_serino.SelectedIndex = par.akt_stok_serino_tanimlari.Value;
            cmb_stok_prim.SelectedIndex = par.akt_stok_prim_tanimlari.Value;
            cmb_stok_depo_detay.SelectedIndex = par.akt_stok_depo_detaylari.Value;
            cmb_rakip_depo_detay.SelectedIndex = par.akt_rakip_stok_depo_detaylari.Value;
            cmb_stok_sarf_Recete.SelectedIndex = par.akt_stok_sarf_receteleri.Value;
            cmb_rakip_stoklar.SelectedIndex = par.akt_rakip_stoklar.Value;
            cmb_stok_sektorleri.SelectedIndex = par.akt_stok_sektorleri.Value;
            cmb_stok_kategorileri.SelectedIndex = par.akt_stok_kategorileri.Value;
            cmb_stok_yil_sezon.SelectedIndex = par.akt_stok_yilsezon_tanimlari.Value;
            cmb_model_tanimlari.SelectedIndex = par.akt_stok_model_tanimlari.Value;
            cmb_stok_uretcileri.SelectedIndex = par.akt_stok_ureticileri.Value;
            cmb_stok_renk.SelectedIndex = par.akt_stok_renk_tanimlari.Value;
            cmb_stok_satis_fiyat_liste_tan.SelectedIndex = par.akt_stok_satis_fiyat_liste_tanimlari.Value;
            cmb_stok_satis_fiyat_liste.SelectedIndex = par.akt_stok_satis_fiyat_listeleri.Value;
            cmb_isemirleri.SelectedIndex = par.akt_isemirleri.Value;

            cmb_bankalar.SelectedIndex = par.akt_bankalar.Value;
            cmb_barkodlar.SelectedIndex = par.akt_barkod_tanimlari.Value;
            cmb_kasalar.SelectedIndex = par.akt_kasalar.Value;
            cmb_masraf_hesap.SelectedIndex = par.akt_masraf_hesaplari.Value;
            cmb_urunler.SelectedIndex = par.akt_urunler.Value;
            cmb_projeler.SelectedIndex = par.akt_projeler.Value;
            cmb_hizmet_hesap.SelectedIndex = par.akt_hizmet_hesaplari.Value;
            cmb_depolar.SelectedIndex = par.akt_depolar.Value;
            cmb_sormerk.SelectedIndex = par.akt_sorumluluk_merkezleri.Value;

            cmb_maliyil_beyan_par.SelectedIndex = par.akt_firma_maliyil_beyan_parametreleri.Value;
            cmb_maliyil_bilgi.SelectedIndex = par.akt_firma_maliyil_bilgileri.Value;
            cmb_uretim_param.SelectedIndex = par.akt_firma_maliyil_uretim_parametreleri.Value;
            cmb_firma_tem.SelectedIndex = par.akt_firma_temsilcileri.Value;
            cmb_firmalar.SelectedIndex = par.akt_firmalar.Value;
            cmb_muh_fis_grubu.SelectedIndex = par.akt_muh_fis_grubu_tanimlari.Value;
            cmb_muh_hesap_plan.SelectedIndex = par.akt_muhasebe_hesap_plani.Value;
            cmb_muh_hesap_grup.SelectedIndex = par.akt_muhasebe_hesap_gruplari.Value;
            cmb_ith_muh_grup.SelectedIndex = par.akt_ithalat_muh_gruplari.Value;
            cmb_ith_dosya.SelectedIndex = par.akt_ithalat_dosyalari.Value;
            cmb_ihr_dosyalari.SelectedIndex = par.akt_ihracat_dosyalari.Value;
            cmb_son_kul.SelectedIndex = par.akt_son_kullanicilari.Value;
            cmb_subeler.SelectedIndex = par.akt_subeler.Value;

            cmb_asorti_tanim.SelectedIndex = par.akt_asorti_tanimlari.Value;
            cmb_teslim_tur.SelectedIndex = par.akt_teslim_turleri.Value;
            cmb_cihaz_sorun.SelectedIndex = par.akt_cihaz_sorunlari.Value;

            cmb_pers.SelectedIndex = par.akt_personeller.Value;
            cmb_pers_bolge.SelectedIndex = par.akt_personel_bolgeleri.Value;
            cmb_pers_alinan_cez.SelectedIndex = par.akt_personel_alinan_cezalar.Value;
            cmb_pers_muh_grp.SelectedIndex = par.akt_personel_muhasebe_gruplari.Value;
            cmb_pers_tanim.SelectedIndex = par.akt_cari_personel_tanimlari.Value;

            cmb_demirbas_grup.SelectedIndex = par.akt_demirbas_gruplari.Value;
            cmb_demirbaslar.SelectedIndex = par.akt_demirbaslar.Value;
            cmb_demirbas_maliyil_tanimlari.SelectedIndex = par.akt_demirbas_maliyil_tanimlari.Value;
            cmb_stok_cari_isk.SelectedIndex = par.akt_stok_cari_iskonto_tanimlari.Value;
            cmb_partilot.SelectedIndex = par.akt_parti_lot.Value;
            cmb_kredi_sozlesmeleri.SelectedIndex = par.akt_kredi_sozlesme.Value;
            cmb_kredi_taksit_tanim.SelectedIndex = par.akt_taksit_tanim.Value;
            cmb_urunrecete.SelectedIndex = par.akt_urun_recete.Value;
            cmb_uretimmalzplanlama.SelectedIndex = par.akt_uretimmalzplanlama.Value;
            cmb_finansal_sozlesmeler.SelectedIndex = par.akt_finansal_sozlesmeler.Value;
            cmb_finansal_sozlesme_taksit.SelectedIndex = par.akt_finansal_sozlesme_taksit.Value;

            ceSipCinsBelirle.Checked = par.akt_sipTip_belirle.Value;
            txtSipTip.Text = par.akt_sip_tip;
            txtSipCins.Text = par.akt_sip_cins;
            ceSthTipBerlirle.Checked = par.akt_irsTip_belirle.Value;
            txtSthTip.Text = par.akt_sth_tip;
            txtSthCins.Text = par.akt_sth_cins;
            txtNormalIade.Text = par.akt_sth_normal_iade;
            txtEvrakTip.Text = par.akt_sth_evraktip;

            ceChaTipBerlirle.Checked = par.akt_chaTip_belirle.Value;
            txtChaTip.Text = par.akt_cha_tip;
            txtChaCinsi.Text = par.akt_cha_cins;
            txtChaNormalIade.Text = par.akt_cha_normal_iade;
            txtChaEvrakTip.Text = par.akt_cha_evraktip;

            ceSrmBelirle.Checked = par.akt_srm.Value;
            txtSrm.Text = par.akt_srm_merkzleri;
            cePrjBelirle.Checked = par.akt_prj.Value;
            txtPrj.Text = par.akt_prj_projeler;
            cmb_donyayhiz.SelectedIndex = par.akt_donyayhiz.Value;

            gc_par.Tag = par;


            ce_SilinenKayitKontrolu.Checked = par.akt_silinen_kayitlar.Value;
            cb_Kullanici.SelectedValue = par.akt_kullanici_no;

        }

        private void frm_Aktarim_Param_Load(object sender, EventArgs e)
        {
            AktarımKullanicisiYukle();
            ParametreleriYukle();
        }

        private void sb_kaydet_Click(object sender, EventArgs e)
        {
            AKTARIM_PARAMETRELERI par;
            if (gc_par.Tag == null)
            {
                par = new AKTARIM_PARAMETRELERI();
            }
            else
            {
                par = (AKTARIM_PARAMETRELERI)gc_par.Tag;
            }


            //par.akt_evrak_anlik = ce_anlik.Checked;

            par.akt_kart_anlik = ce_kart_anlik.Checked;
            par.akt_evrak_anlik = ce_evrak_anlik.Checked;

            par.akt_kart_gunluk = ce_kart_gunluk.Checked;
            par.akt_evrak_gunluk = ce_evrak_gunluk.Checked;

            par.akt_dakika = Convert.ToInt32(sp_sure.Text);
            par.akt_evrak_aktarim_saat = tm_evrakzaman.Time;
            par.akt_beden_har = ce_beden.Checked;
            par.akt_cihaz_har = ce_cihaz.Checked;
            par.akt_satis_sart = ce_satis.Checked;

            par.akt_kart_haftalik = ce_kart_haftalik.Checked;
            par.akt_kart_dakika = Convert.ToInt32(sp_kart_aktarim.Text);
            par.akt_kart_aktarim_saat = tm_kart_gunlukzaman.Time;

            par.akt_hedef_irstofat = chkIrstoFat.Checked;

            par.akt_cari_hesaplar = cmb_carihesap.SelectedIndex;
            par.akt_cari_hesap_bolgeler = cmb_caribolge.SelectedIndex;
            par.akt_cari_hesap_gruplari = cmb_carigrup.SelectedIndex;
            par.akt_cari_hesap_yetkilileri = cmb_cariyetkili.SelectedIndex;
            par.akt_cari_hesap_adresleri = cmb_cariadres.SelectedIndex;
            par.akt_cari_hesap_tasit_plakalari = cmb_caritasitplaka.SelectedIndex;
            par.akt_cari_hesap_sozlesmeleri = cmb_carisozlesme.SelectedIndex;
            par.akt_cari_mustahsil_tanimlari = cmb_carimustahsil.SelectedIndex;

            par.akt_stoklar = cmb_stoklar.SelectedIndex;
            par.akt_stok_ana_gruplari = cmb_stok_anagrup.SelectedIndex;
            par.akt_stok_alt_gruplari = cmb_stokaltgrup.SelectedIndex;
            par.akt_stok_markalari = cmb_stokmarkalari.SelectedIndex;
            par.akt_stok_beden_tanimlari = cmb_stokbeden.SelectedIndex;
            par.akt_stok_kalkon = cmb_stokkalkon.SelectedIndex;
            par.akt_stok_ambalajlari = cmb_stok_ambalaj.SelectedIndex;
            par.akt_stok_muhasebe_gruplari = cmb_muh_grup.SelectedIndex;
            par.akt_stok_anahammaddeleri = cmb_anahammadde.SelectedIndex;
            par.akt_stok_reyonlari = cmb_stok_reyonlar.SelectedIndex;
            par.akt_stok_kalite_kontrol_tanimlari = cmb_stok_kalite.SelectedIndex;
            par.akt_stok_paket_tanimlari = cmb_stok_paket.SelectedIndex;
            par.akt_stok_serino_tanimlari = cmb_stok_serino.SelectedIndex;
            par.akt_stok_prim_tanimlari = cmb_stok_prim.SelectedIndex;

            par.akt_stok_cari_iskonto_tanimlari = cmb_stok_cari_isk.SelectedIndex;
            par.akt_rakip_stok_depo_detaylari = cmb_rakip_depo_detay.SelectedIndex;
            par.akt_stok_depo_detaylari = cmb_stok_depo_detay.SelectedIndex;
            par.akt_stok_sarf_receteleri = cmb_stok_sarf_Recete.SelectedIndex;
            par.akt_rakip_stoklar = cmb_rakip_stoklar.SelectedIndex;
            par.akt_stok_sektorleri = cmb_stok_sektorleri.SelectedIndex;
            par.akt_stok_kategorileri = cmb_stok_kategorileri.SelectedIndex;
            par.akt_stok_yilsezon_tanimlari = cmb_stok_yil_sezon.SelectedIndex;
            par.akt_stok_model_tanimlari = cmb_model_tanimlari.SelectedIndex;
            par.akt_stok_ureticileri = cmb_stok_uretcileri.SelectedIndex;
            par.akt_stok_renk_tanimlari = cmb_stok_renk.SelectedIndex;
            par.akt_stok_satis_fiyat_liste_tanimlari = cmb_stok_satis_fiyat_liste_tan.SelectedIndex;
            par.akt_stok_satis_fiyat_listeleri = cmb_stok_satis_fiyat_liste.SelectedIndex;
            par.akt_isemirleri = cmb_isemirleri.SelectedIndex;

            par.akt_bankalar = cmb_bankalar.SelectedIndex;
            par.akt_barkod_tanimlari = cmb_barkodlar.SelectedIndex;
            par.akt_kasalar = cmb_kasalar.SelectedIndex;
            par.akt_masraf_hesaplari = cmb_masraf_hesap.SelectedIndex;
            par.akt_urunler = cmb_urunler.SelectedIndex;
            par.akt_projeler = cmb_projeler.SelectedIndex;
            par.akt_hizmet_hesaplari = cmb_hizmet_hesap.SelectedIndex;
            par.akt_depolar = cmb_depolar.SelectedIndex;
            par.akt_sorumluluk_merkezleri = cmb_sormerk.SelectedIndex;

            par.akt_firma_maliyil_beyan_parametreleri = cmb_maliyil_beyan_par.SelectedIndex;
            par.akt_firma_maliyil_bilgileri = cmb_maliyil_bilgi.SelectedIndex;
            par.akt_firma_maliyil_uretim_parametreleri = cmb_uretim_param.SelectedIndex;
            par.akt_firma_temsilcileri = cmb_firma_tem.SelectedIndex;
            par.akt_firmalar = cmb_firmalar.SelectedIndex;
            par.akt_muh_fis_grubu_tanimlari = cmb_muh_fis_grubu.SelectedIndex;
            par.akt_muhasebe_hesap_plani = cmb_muh_hesap_plan.SelectedIndex;
            par.akt_muhasebe_hesap_gruplari = cmb_muh_hesap_grup.SelectedIndex;
            par.akt_ithalat_muh_gruplari = cmb_ith_muh_grup.SelectedIndex;
            par.akt_ithalat_dosyalari = cmb_ith_dosya.SelectedIndex;
            par.akt_ihracat_dosyalari = cmb_ihr_dosyalari.SelectedIndex;
            par.akt_son_kullanicilari = cmb_son_kul.SelectedIndex;
            par.akt_subeler = cmb_subeler.SelectedIndex;
            par.akt_asorti_tanimlari = cmb_asorti_tanim.SelectedIndex;
            par.akt_cihaz_sorunlari = cmb_cihaz_sorun.SelectedIndex;

            par.akt_personeller = cmb_pers.SelectedIndex;
            par.akt_personel_bolgeleri = cmb_pers_bolge.SelectedIndex;
            par.akt_personel_alinan_cezalar = cmb_pers_alinan_cez.SelectedIndex;
            par.akt_personel_muhasebe_gruplari = cmb_pers_muh_grp.SelectedIndex;
            par.akt_cari_personel_tanimlari = cmb_pers_tanim.SelectedIndex;

            par.akt_demirbas_gruplari = cmb_demirbas_grup.SelectedIndex;
            par.akt_demirbaslar = cmb_demirbaslar.SelectedIndex;
            par.akt_demirbas_maliyil_tanimlari = cmb_demirbas_maliyil_tanimlari.SelectedIndex;
            par.akt_teslim_turleri = cmb_teslim_tur.SelectedIndex;
            par.akt_parti_lot = cmb_partilot.SelectedIndex;
            par.akt_kredi_sozlesme = cmb_kredi_sozlesmeleri.SelectedIndex;
            par.akt_taksit_tanim = cmb_kredi_taksit_tanim.SelectedIndex;

            par.akt_sipTip_belirle = ceSipCinsBelirle.Checked;
            par.akt_sip_tip = txtSipTip.Text;
            par.akt_sip_cins = txtSipCins.Text;
            par.akt_irsTip_belirle = ceSthTipBerlirle.Checked;
            par.akt_sth_tip = txtSthTip.Text;
            par.akt_sth_cins = txtSthCins.Text;
            par.akt_sth_normal_iade = txtNormalIade.Text;
            par.akt_sth_evraktip = txtEvrakTip.Text;

            par.akt_chaTip_belirle = ceChaTipBerlirle.Checked;
            par.akt_cha_tip = txtChaTip.Text;
            par.akt_cha_cins = txtChaCinsi.Text;
            par.akt_cha_normal_iade = txtChaNormalIade.Text;
            par.akt_cha_evraktip = txtChaEvrakTip.Text;

            par.akt_srm = ceSrmBelirle.Checked;
            par.akt_srm_merkzleri = txtSrm.Text;
            par.akt_prj = cePrjBelirle.Checked;
            par.akt_prj_projeler = txtPrj.Text;
            par.akt_stoklar = cmb_stoklar.SelectedIndex;
            par.akt_urun_recete = cmb_urunrecete.SelectedIndex;
            par.akt_uretimmalzplanlama = cmb_uretimmalzplanlama.SelectedIndex;
            par.akt_finansal_sozlesmeler = cmb_finansal_sozlesmeler.SelectedIndex;
            par.akt_finansal_sozlesme_taksit = cmb_finansal_sozlesme_taksit.SelectedIndex;

            par.akt_donyayhiz = cmb_donyayhiz.SelectedIndex;

            par.akt_silinen_kayitlar = ce_SilinenKayitKontrolu.Checked;
            par.akt_kullanici_no = Convert.ToInt32(cb_Kullanici.SelectedValue);

            if (par.akt_id > 0)
            {
                AktarimParametreleri.Aktarim_Parametrelerini_Guncelle(par, DatabaseFacade3.ConnectionString());
            }
            else
            {
                AktarimParametreleri.Aktarim_Parametrelerini_Kaydet(par, DatabaseFacade3.ConnectionString());
            }
            AktarimParametreleri.Parametre = par;

            this.Close();

        }


        private void sb_ekle_Click(object sender, EventArgs e)
        {
            AddLocalKey("Bigus.Aktarici.WinApp", System.Reflection.Assembly.GetEntryAssembly().Location);

        }

        void AddLocalKey(string name, string path)
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            key.SetValue(name, path);
        }


        void RemoveLocalKey(string name)
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            key.DeleteValue(name, false);

        }

        private void sb_cikar_Click(object sender, EventArgs e)
        {
            RemoveLocalKey("Bigus.Aktarici.WinApp");
        }

        #region Aktarım Kullanıcısı
        private void AktarımKullanicisiYukle()
        {
            List<KULLANICILAR> ls = new List<KULLANICILAR>();
            KULLANICILAR ls_ = new KULLANICILAR();
            ls_.User_no = 0;
            ls_.User_name = "KAYNAK DB ESAS KULLANICISI";
            ls.Add(ls_);

            List<KULLANICILAR> ls1 = new List<KULLANICILAR>();
            ls1 = AktarimParametreleri.Kullanicilari_Yukle();

            foreach (KULLANICILAR item in ls1)
            {
                item.User_name = item.User_name + "  " + item.User_LongName;
            }

            ls.AddRange(ls1);

            cb_Kullanici.ValueMember = "User_no";
            cb_Kullanici.DisplayMember = "User_name";
            cb_Kullanici.DataSource = ls;


        }
        #endregion
    }
}
