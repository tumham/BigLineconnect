using Bigus.Aktarici.Linq;
using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
namespace Bigus.Aktarici.WinApp
{
    public partial class frm_Aktarim : Form
    {
        private void SetControlText(System.Windows.Forms.Control ctrl, string text)
        {
            if (ctrl.InvokeRequired)
            {
                ctrl.Invoke(new Action(() => SetControlText(ctrl, text)));
            }
            else
            {
                ctrl.Text = text;
            }
        }
        private NotifyIcon notifyicon;
        private ContextMenu menu;
        public frm_Aktarim()
        {
            InitializeComponent();
            // this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            NotifyIconYukle();
        }
        bool baglanti_durum = false;
        private void frm_Aktarim_Load(object sender, EventArgs e)
        {
            splash_ackapa(true);
            dateEdit1.DateTime = DateTime.Now.Date;
            dateEdit2.DateTime = DateTime.Now.Date;
            pb_anlik_akt_durum.Image = Image.FromFile(Application.StartupPath + "\\misc-green.png");
            new Thread(BaglantiKontrol).Start();
            //BaglantiKontrol();
            if (this.WindowState == FormWindowState.Minimized)
            {
                notifyicon.BalloonTipText = "Bigus Aktarıcı V17 DÜZ görev çubuğunda çalışıyor.";
                notifyicon.ShowBalloonTip(10);
            }
        }
        #region FONKSİYONLAR
        void splash_ackapa(bool drm)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => splash_ackapa(drm))); return; }
            if (drm == true)
            {
                pnl_splash.Dock = DockStyle.Fill;
                pnl_splash.Visible = true;
                pnl_splash.BringToFront();
            }
            else
            {
                pnl_splash.Visible = false;
                pnl_splash.Dock = DockStyle.None;
                pnl_splash.SendToBack();
            }
        }
        void DurumBildir(string text)
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => DurumBildir(text))); return; }
            SetControlText(lbldurum, text);
        }
        void BaglantiKontrol()
        {
            StringBuilder strb = new StringBuilder();
            DurumBildir("Bağlantı ayarları kontrol ediliyor...");
            if (DatabaseFacade3.SettingsOku() != true)
            {
                this.Invoke(new Action(() =>
                {
                    Tools.MesajPenceresi("AKTARIM PARAMETRELERİ veritabanı bağlantısı yapılamadı.");
                    SqlAyar frm = new SqlAyar();
                    frm.Baslik = "AKTARIM PARAMETRELERİ VERİTABANI BAĞLANTI AYARI";
                    frm.FilePath = "Settings3.xml";
                    frm.ShowDialog();
                }));
                BaglantiKontrol();
                return;
            }
            if (DatabaseFacade.SettingsOku() != true)
            {
                this.Invoke(new Action(() =>
                {
                    Tools.MesajPenceresi("KAYNAK veritabanı bağlantısı yapılamadı. ");
                    SqlAyar frm = new SqlAyar();
                    frm.Baslik = "KAYNAK VERİTABANI BAĞLANTI AYARI";
                    frm.FilePath = "Settings.xml";
                    frm.ShowDialog();
                }));
                BaglantiKontrol();
                return;
            }
            if (DatabaseFacade2.SettingsOku() != true)
            {
                this.Invoke(new Action(() =>
                {
                    Tools.MesajPenceresi("HEDEF veritabanı bağlantısı yapılamadı. ");
                    SqlAyar frm = new SqlAyar();
                    frm.Baslik = "HEDEF VERİTABANI BAĞLANTI AYARI";
                    frm.FilePath = "Settings2.xml";
                    frm.ShowDialog();
                }));
                BaglantiKontrol();
                return;
            }
            if (VeritabaniKontrol.TableKontrol("AKTARIM_PARAMETRELERI", DatabaseFacade3.ConnectionString()) != true)
            {
                DurumBildir("SQL tabloları yapılandırılıyor...");
                VeritabaniKontrol.KurulumYap("AKTARIM_PARAMETRELERI");
            }
            if (VeritabaniKontrol.TableKontrol("AKTARIM_SERILERI", DatabaseFacade3.ConnectionString()) != true)
            {
                DurumBildir("SQL tabloları yapılandırılıyor...");
                VeritabaniKontrol.KurulumYap("AKTARIM_SERILERI");
            }
            if (VeritabaniKontrol.TableKontrol("AKTARIM_KULLANICILARI", DatabaseFacade3.ConnectionString()) != true)
            {
                DurumBildir("SQL tabloları yapılandırılıyor...");
                VeritabaniKontrol.KurulumYap("AKTARIM_KULLANICILARI");
            }
            List<AKTARIM_PARAMETRELERI> ls_param = new List<AKTARIM_PARAMETRELERI>();
            ls_param = AktarimParametreleri.Aktarim_Parametrelerini_Yukle(DatabaseFacade3.ConnectionString());
            if (ls_param.Count == 0)
            {
                this.Invoke(new Action(() =>
                {
                    frm_Aktarim_Param _frm_param = new frm_Aktarim_Param();
                    _frm_param.ShowDialog();
                }));
                BaglantiKontrol();
                return;
            }
            AktarimParametreleri.Parametre = ls_param[0];
            List<AKTARIM_SERILERI> ls_seri = new List<AKTARIM_SERILERI>();
            ls_seri = AktarimParametreleri.Aktarim_Serilerini_Yukle(DatabaseFacade3.ConnectionString());
            if (ls_seri.Count == 0)
            {
                this.Invoke(new Action(() =>
                {
                    frm_AktarimSeri _frm_seri = new frm_AktarimSeri();
                    _frm_seri.ShowDialog();
                }));
                BaglantiKontrol();
                return;
            }
            AktarimParametreleri.AktarimSerileri = ls_seri;
            #region AKTARIM KULLANICILARI
            List<AKTARIM_KULLANICILARI> ls_kul = new List<AKTARIM_KULLANICILARI>();
            ls_kul = AktarimParametreleri.Aktarim_Kullanicilarini_Yukle(DatabaseFacade3.ConnectionString());
            //if (ls_kul.Count == 0)
            //{
            //    frm_AktarimKullanici _frm_kul = new frm_AktarimKullanici();
            //    _frm_kul.ShowDialog();
            //    BaglantiKontrol();
            //    ls_kul = AktarimParametreleri.Aktarim_Kullanicilarini_Yukle(DatabaseFacade3.ConnectionString());
            //}
            AktarimParametreleri.AktarimKullanicilari = ls_kul;
            #endregion
            FormuYapilandir();
            baglanti_durum = true;
            AnlikAktarimDurumuGoster();
            splash_ackapa(false);
        }
        void AnlikAktarimDurumuGoster()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => AnlikAktarimDurumuGoster())); return; }
            if (AktarimParametreleri.Parametre.akt_evrak_anlik == true ||
                AktarimParametreleri.Parametre.akt_kart_anlik == true)
            {
                pb_anlik_akt_durum.Image = Image.FromFile(Application.StartupPath + "\\misc-green.png");
                SetControlText(lbl_aktarim_durumu, "Anlık evrak - kart aktarıcısı AKTİF!");
                SetControlText(lbl_aktarim_sure, "Anlık  aktarım tekrarlama süresi: " + AktarimParametreleri.Parametre.akt_dakika.ToString() + " dakikadır");
            }
            else
            {
                pb_anlik_akt_durum.Image = Image.FromFile(Application.StartupPath + "\\misc-red.png");
                SetControlText(lbl_aktarim_durumu, "Anlık evrak - kart aktarıcısı PASİF!");
                SetControlText(lbl_aktarim_sure, "");
            }
            if (AktarimParametreleri.Parametre.akt_evrak_gunluk == true ||
                AktarimParametreleri.Parametre.akt_kart_gunluk == true)
            {
                pb_gunluk_aktarim_durumu.Image = Image.FromFile(Application.StartupPath + "\\misc-green.png");
                SetControlText(lbl_aktarim_durumu_gunluk, "Günlük evrak - kart aktarıcısı AKTİF!");
                SetControlText(lbl_aktarim_zaman, AktarimParametreleri.Parametre.akt_evrak_aktarim_saat.Value.ToLongTimeString());
            }
            else
            {
                pb_gunluk_aktarim_durumu.Image = Image.FromFile(Application.StartupPath + "\\misc-red.png");
                SetControlText(lbl_aktarim_durumu_gunluk, "Günlük evrak - kart aktarıcısı PASİF!");
                SetControlText(lbl_aktarim_zaman, "");
            }
        }
        void FormuYapilandir()
        {
            if (this.InvokeRequired) { this.Invoke(new Action(() => FormuYapilandir())); return; }
            lblkaynak.Text = DatabaseFacade.Database;
            lblhedef.Text = DatabaseFacade2.Database;
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
        void AktarimEkraniDondur(bool durum)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => AktarimEkraniDondur(durum)));
                return;
            }
            if (durum == true)
            {
                gc_manuel.Enabled = false;
                gc_bilgiler.Enabled = false;
            }
            else
            {
                gc_bilgiler.Enabled = true;
                gc_manuel.Enabled = true;
            }
        }
        #endregion
        #region EVRAK AKTARIMLARI
        void GuncellemeliAktarim()
        {
            try
            {
                bool anlik;
                if (AktarimParametreleri.Parametre.akt_evrak_anlik == true)
                    anlik = true;
                else
                    anlik = false;
                SetControlText(lbl_sure1, DateTime.Now.ToLongTimeString());
                SetControlText(lbl_süre2, "SÜRE");
                AktarimaBasla(dateEdit1.DateTime.Date, dateEdit2.DateTime.Date, anlik, AktarimParametreleri.Parametre, pbc_1, lbl_durum_1);
                AktarimEkraniDondur(false);
                SetControlText(lbl_süre2, DateTime.Now.ToLongTimeString());
                if (AktarimParametreleri.Parametre.akt_evrak_anlik == true ||
                    AktarimParametreleri.Parametre.akt_kart_anlik == true)
                {
                    aktarim_bittimi = true;
                }
                pbc_1.Text = "0";
            }
            catch (ThreadAbortException ex)
            {
                AktarimEkraniDondur(false);
                SetControlText(lbl_süre2, DateTime.Now.ToLongTimeString());
                if (AktarimParametreleri.Parametre.akt_evrak_anlik == true ||
                    AktarimParametreleri.Parametre.akt_kart_anlik == true)
                {
                    aktarim_bittimi = true;
                }
                SetControlText(lbl_durum_1, "Aktarım işlemi tamamlanamadı. Log dosyalarını kontrol ediniz...");
                pbc_1.Text = "0";
                Thread.CurrentThread.Abort();
            }
        }
        void AnlikAktarim()
        {
            try
            {
                bool anlik;
                if (AktarimParametreleri.Parametre.akt_evrak_anlik == true)
                    anlik = true;
                else
                    anlik = false;
                DateTime tarih1 = DateTime.Now;
                DateTime tarih0 = DateTime.Now;//Convert.ToDateTime("01.01.2017 00:00:00");
                DateTime tarih_1;
                DateTime tarih_2;
                AKTARIM_PARAMETRELERI par = AktarimParametreleri.Parametre;
                double eksi_dak = Convert.ToDouble((par.akt_dakika.Value + 30) * -1);
                double arti_dak = Convert.ToDouble(par.akt_dakika.Value + 30);
                pbc_1.Text = "0";
                SetControlText(lbl_durum_1, "Aktarım başlıyor...");
                tarih_1 = tarih0.AddMinutes(eksi_dak);
                tarih_2 = tarih1.AddMinutes(arti_dak);
                SetControlText(lbl_sure1, DateTime.Now.ToLongTimeString());
                SetControlText(lbl_süre2, "SÜRE");
                //if (par.akt_kart_gunluk == true)
                //{
                //    AktarimaBasla(tarih_1, tarih_2, true, AktarimParametreleri.Parametre, pbc_1, lbl_durum_1);
                //}
                if (par.akt_kart_anlik == true) // anlık kart aktarımı
                {
                    AnlikKartAktarimi();
                }
                if (par.akt_evrak_anlik == true) // anlık evrak aktarımı
                {
                    AktarimaBasla(tarih_1, tarih_2, anlik, AktarimParametreleri.Parametre, pbc_1, lbl_durum_1);
                }
                SetControlText(lbl_süre2, DateTime.Now.ToLongTimeString());
                aktarim_bittimi = true;
            }
            catch (ThreadAbortException ex)
            {
                AktarimEkraniDondur(false);
                SetControlText(lbl_süre2, DateTime.Now.ToLongTimeString());
                if (AktarimParametreleri.Parametre.akt_evrak_anlik == true ||
                    AktarimParametreleri.Parametre.akt_kart_anlik == true)
                {
                    aktarim_bittimi = true;
                }
                SetControlText(lbl_durum_1, "Aktarım işlemi tamamlanamadı. Log dosyalarını kontrol ediniz...");
                pbc_1.Text = "0";
                Thread.CurrentThread.Abort();
            }
        }
        void GunlukAktarim()
        {
            try
            {
                bool anlik;
                if (AktarimParametreleri.Parametre.akt_evrak_anlik == true)
                    anlik = true;
                else
                    anlik = false;
                DateTime tarih = DateTime.Now;
                DateTime tarih_1;
                DateTime tarih_2;
                AKTARIM_PARAMETRELERI par = AktarimParametreleri.Parametre;
                pbc_1.Text = "0";
                notifyicon.BalloonTipText = "Günlük aktarım başlıyor...";
                SetControlText(lbl_durum_1, "Aktarım başlıyor...");
                //tarih_1 = DateTime.Now.Date;
                tarih_1 = DateTime.Now.AddDays(-1).Date;
                tarih_2 = DateTime.Now.AddDays(1).Date;
                SetControlText(lbl_sure1, DateTime.Now.ToLongTimeString());
                SetControlText(lbl_süre2, "SÜRE");
                if (par.akt_kart_gunluk == true)
                {
                    GunlukKartAktarimi();
                }
                if (par.akt_evrak_gunluk == true)
                {
                    AktarimaBasla(tarih_1, tarih_2, anlik, AktarimParametreleri.Parametre, pbc_1, lbl_durum_1);
                }
                SetControlText(lbl_süre2, DateTime.Now.ToLongTimeString());
                aktarim_bittimi = true;
            }
            catch (ThreadAbortException ex)
            {
                AktarimEkraniDondur(false);
                SetControlText(lbl_süre2, DateTime.Now.ToLongTimeString());
                if (AktarimParametreleri.Parametre.akt_evrak_anlik == true)
                {
                    aktarim_bittimi = true;
                }
                SetControlText(lbl_durum_1, "Aktarım işlemi tamamlanamadı. Log dosyalarını kontrol ediniz...");
                pbc_1.Text = "0";
                Thread.CurrentThread.Abort();
            }
        }
        void AktarimaBasla(DateTime _tarih1, DateTime _tarih2, bool anlik_mi, AKTARIM_PARAMETRELERI par, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            _tarih2 = _tarih2.AddDays(1).Date;
            #region Kaynak DB'de silinen kayıtlar Hedef DB'den Silinsin
            if (par.akt_silinen_kayitlar == true)
            {
                DeletedRowsControl(_tarih1, _tarih2, anlik_mi, par, pbc, lbl_durum);
            }
            #endregion
            DateTime tarih1 = _tarih1;
            DateTime tarih2 = _tarih2;
            string[] serino = new string[AktarimParametreleri.AktarimSerileri.Count];
            int i = 0;
            foreach (AKTARIM_SERILERI ser in AktarimParametreleri.AktarimSerileri)
            {
                serino[i] = ser.ser_serino;
                i += 1;
            }
            string[] _sipTip = par.akt_sip_tip.Split(',');
            int[] sipTip = new int[_sipTip.Count()];
            i = 0;
            foreach (string item in _sipTip)
            {
                if (item != "")
                {
                    sipTip[i] = Convert.ToInt32(item);
                    i += 1;
                }
            }
            string[] _sipCins = par.akt_sip_cins.Split(',');
            int[] sipCins = new int[_sipCins.Count()];
            i = 0;
            foreach (string item in _sipCins)
            {
                if (item != "")
                {
                    sipCins[i] = Convert.ToInt32(item);
                    i += 1;
                }
            }
            string[] _sthTip = par.akt_sth_tip.Split(',');
            int[] sthTip = new int[_sthTip.Count()];
            i = 0;
            foreach (string item in _sthTip)
            {
                if (item != "")
                {
                    sthTip[i] = Convert.ToInt32(item);
                    i += 1;
                }
            }
            string[] _sthCins = par.akt_sth_cins.Split(',');
            int[] sthCins = new int[_sthCins.Count()];
            i = 0;
            foreach (string item in _sthCins)
            {
                if (item != "")
                {
                    sthCins[i] = Convert.ToInt32(item);
                    i += 1;
                }
            }
            string[] _sthNormalIade = par.akt_sth_normal_iade.Split(',');
            int[] sthNormalIade = new int[_sthNormalIade.Count()];
            i = 0;
            foreach (string item in _sthNormalIade)
            {
                if (item != "")
                {
                    sthNormalIade[i] = Convert.ToInt32(item);
                    i += 1;
                }
            }
            string[] _sthEvrakTip = par.akt_sth_evraktip.Split(',');
            int[] sthEvrakTip = new int[_sthEvrakTip.Count()];
            i = 0;
            foreach (string item in _sthEvrakTip)
            {
                if (item != "")
                {
                    sthEvrakTip[i] = Convert.ToInt32(item);
                    i += 1;
                }
            }
            string[] _chaTip = par.akt_cha_tip.Split(',');
            int[] chaTip = new int[_chaTip.Count()];
            i = 0;
            foreach (string item in _chaTip)
            {
                if (item != "")
                {
                    chaTip[i] = Convert.ToInt32(item);
                    i += 1;
                }
            }
            string[] _chaCins = par.akt_cha_cins.Split(',');
            int[] chaCins = new int[_chaCins.Count()];
            i = 0;
            foreach (string item in _chaCins)
            {
                if (item != "")
                {
                    chaCins[i] = Convert.ToInt32(item);
                    i += 1;
                }
            }
            string[] _chaNormalIade = par.akt_cha_normal_iade.Split(',');
            int[] chaNormalIade = new int[_chaNormalIade.Count()];
            i = 0;
            foreach (string item in _chaNormalIade)
            {
                if (item != "")
                {
                    chaNormalIade[i] = Convert.ToInt32(item);
                    i += 1;
                }
            }
            string[] _chaEvrakTip = par.akt_cha_evraktip.Split(',');
            int[] chaEvrakTip = new int[_chaEvrakTip.Count()];
            i = 0;
            foreach (string item in _chaEvrakTip)
            {
                if (item != "")
                {
                    chaEvrakTip[i] = Convert.ToInt32(item);
                    i += 1;
                }
            }
            string[] _srmMerkezleri = par.akt_srm_merkzleri.Split(',');
            string[] srmMerkezleri = new string[_srmMerkezleri.Count()];
            i = 0;
            foreach (string item in _srmMerkezleri)
            {
                srmMerkezleri[i] = item;
                i += 1;
            }
            string[] _projeler = par.akt_srm_merkzleri.Split(',');
            string[] projeler = new string[_projeler.Count()];
            i = 0;
            foreach (string item in _projeler)
            {
                projeler[i] = item;
                i += 1;
            }
            List<STOK_HAREKETLERI> ls = new List<STOK_HAREKETLERI>();
            if (anlik_mi == false)
                ls = Aktarimlar2.HareketleriYukle(serino, tarih1, tarih2, DatabaseFacade.ConnectionString(), (bool)par.akt_irsTip_belirle, sthTip, sthCins, sthNormalIade, sthEvrakTip, (bool)par.akt_srm, srmMerkezleri);
            else
                ls = Aktarimlar2.HareketleriYukle_anlik(serino, tarih1, tarih2, DatabaseFacade.ConnectionString(), (bool)par.akt_irsTip_belirle, sthTip, sthCins, sthNormalIade, sthEvrakTip, (bool)par.akt_srm, srmMerkezleri);
            pbc.Text = "0";
            pbc.Properties.Step = 1;
            pbc.Properties.Maximum = ls.Count;
            Int32 pb_i = 0;
            #region STOK HAREKETLERI AKTARIMI
            if (ls.Count > 0)
            {
                foreach (STOK_HAREKETLERI shar in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    SetControlText(lbl_durum, "Stok Hareketleri aktarılıyor...");
                    Evrak ev = new Evrak();
                    ev.RECno = shar.sth_Guid;
                    ev.EvrakTip = shar.sth_evraktip.Value;
                    ev.Tip = shar.sth_tip.Value;
                    ev.SeriNo = shar.sth_evrakno_seri;
                    ev.SiraNo = shar.sth_evrakno_sira.Value;
                    ev.SatirNo = shar.sth_satirno.Value;
                    STOK_HAREKETLERI st_1 = shar;
                    STOK_HAREKETLERI st_2;
                    Guid _st_recno = st_1.sth_Guid;
                    st_2 = Aktarimlar2.Stok_Hareket_EvrakDetayGetir_2(ev, DatabaseFacade2.ConnectionString());
                    if (st_2 != null)
                    {
                        #region STOK HAREKETLERI GUNCELLEME
                        CARI_HESAP_HAREKETLERI ch_1;
                        CARI_HESAP_HAREKETLERI ch_2;
                        SIPARISLER sip_1;
                        SIPARISLER sip_2;
                        STOK_HAREKETLERI_EK shek_1;
                        STOK_HAREKETLERI_EK shek_2;
                        CARI_HESAP_HAREKETLERI_EK chek_1;
                        CARI_HESAP_HAREKETLERI_EK chek_2;
                        #region CARİ HESAP HAREKETLERI KONTROLÜ
                        if (st_1.sth_fat_uid != Guid.Empty)
                        {
                            ch_1 = Aktarimlar2.Cari_Hesap_Hareket_EvrakDetayGetir(st_1.sth_fat_uid.Value, DatabaseFacade.ConnectionString());
                            chek_1 = Aktarimlar2.Cari_Hesap_HareketEK_EvrakDetayGetir(st_1.sth_fat_uid.Value, DatabaseFacade.ConnectionString());
                            if (ch_1 != null)
                            {
                                Evrak ch_ev = new Evrak();
                                ch_ev.RECno = ch_1.cha_Guid;
                                ch_ev.SatirNo = ch_1.cha_satir_no.Value;
                                ch_ev.SeriNo = ch_1.cha_evrakno_seri;
                                ch_ev.SiraNo = ch_1.cha_evrakno_sira.Value;
                                ch_ev.EvrakTip = ch_1.cha_evrak_tip.Value;
                                ch_ev.Tip = ch_1.cha_tip.Value;
                                ch_2 = Aktarimlar2.Cari_Hesap_Hareket_EvrakDetayGetir_2(ch_ev, DatabaseFacade2.ConnectionString());
                                if (ch_2 != null)
                                {
                                    chek_2 = Aktarimlar2.Cari_Hesap_HareketEK_EvrakDetayGetir_3(ch_2.cha_Guid, DatabaseFacade2.ConnectionString());
                                    ch_1.cha_Guid = ch_2.cha_Guid;
                                    //ch_1.cha_RECid_RECno = ch_2.cha_RECid_RECno;
                                    ch_2 = Aktarimlar2.Cari_Hesap_Hareket_Guncelle(ch_1, DatabaseFacade2.ConnectionString());
                                    if (chek_1 != null)
                                    {
                                        if (st_1.sth_Tevkifat_turu != 0)
                                        {
                                            if (chek_2 != null)
                                            {
                                                chek_1.chaek_Guid = chek_2.chaek_Guid;
                                                //chek_1.chaek_RECid_RECno = chek_2.chaek_RECid_RECno;
                                                chek_1.chaek_related_uid = ch_2.cha_Guid;
                                                chek_2 = Aktarimlar2.Cari_Hesap_HareketEK_Guncelle(chek_1, DatabaseFacade2.ConnectionString());
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    ch_1.cha_Guid = Guid.Empty;
                                    //ch_1.cha_RECid_RECno = -100;
                                    ch_2 = Aktarimlar2.Cari_Hesap_Hareket_Kaydet(ch_1, DatabaseFacade2.ConnectionString());
                                    if (chek_1 != null)
                                    {
                                        chek_1.chaek_Guid = Guid.Empty;
                                        //chek_1.chaek_RECid_RECno = -100;
                                        chek_1.chaek_related_uid = ch_2.cha_Guid;
                                        chek_2 = Aktarimlar2.Cari_Hesap_HareketEK_Kaydet(chek_1, DatabaseFacade2.ConnectionString());
                                    }
                                }
                                st_1.sth_fat_uid = ch_2.cha_Guid;
                            }
                        }
                        #endregion
                        #region SİPARİŞLER KONTROLÜ
                        if (st_1.sth_sip_uid != Guid.Empty)
                        {
                            sip_1 = Aktarimlar2.Siparisler_EvrakDetayGetir(st_1.sth_sip_uid.Value, DatabaseFacade.ConnectionString());
                            if (sip_1 != null)
                            {
                                Evrak sip_ev = new Evrak();
                                sip_ev.RECno = sip_1.sip_Guid;
                                sip_ev.EvrakTip = sip_1.sip_tip.Value;
                                sip_ev.Tip = sip_1.sip_cins.Value;
                                sip_ev.SeriNo = sip_1.sip_evrakno_seri;
                                sip_ev.SiraNo = sip_1.sip_evrakno_sira.Value;
                                sip_ev.SatirNo = sip_1.sip_satirno.Value;
                                Guid _recno = sip_1.sip_Guid;
                                sip_2 = Aktarimlar2.Siparis_EvrakDetayGetir_2(sip_ev, DatabaseFacade2.ConnectionString());
                                if (sip_2 != null)
                                {
                                    sip_1.sip_Guid = sip_2.sip_Guid;
                                    //sip_1.sip_RECid_RECno = sip_2.sip_RECid_RECno;
                                    sip_2 = Aktarimlar2.Siparis_Guncelle(sip_1, DatabaseFacade2.ConnectionString());
                                }
                                else
                                {
                                    sip_1.sip_Guid = Guid.Empty;
                                    //sip_1.sip_RECid_RECno = -100;
                                    sip_2 = Aktarimlar2.Siparis_Kaydet(sip_1, DatabaseFacade2.ConnectionString());
                                }
                                st_1.sth_sip_uid = sip_2.sip_Guid;
                                if (par.akt_beden_har == true)
                                {
                                    #region Sipariş BEDEN HAREKETLERI
                                    List<BEDEN_HAREKETLERI> ls_be_sip = new List<BEDEN_HAREKETLERI>();
                                    ls_be_sip = Aktarimlar2.BedenHareketleriYukle(Char.Parse("P"), _recno, DatabaseFacade.ConnectionString());
                                    if (ls_be_sip.Count > 0)
                                    {
                                        foreach (BEDEN_HAREKETLERI be in ls_be_sip)
                                        {
                                            BEDEN_HAREKETLERI be_1 = be;
                                            BEDEN_HAREKETLERI be_2;
                                            be_2 = Aktarimlar2.Beden_Hareketleri_EvrakDetayGetir(Char.Parse("P"), sip_2.sip_Guid, be.BdnHar_BedenNo.Value, DatabaseFacade2.ConnectionString());
                                            if (be_2 != null)
                                            {
                                                be_1.BdnHar_Guid = be_2.BdnHar_Guid;
                                                //be_1.BdnHar_RECid_RECno = be_2.BdnHar_RECno;
                                                be_1.BdnHar_Har_uid = be_2.BdnHar_Har_uid;
                                                be_2 = Aktarimlar2.BedenHareketleri_Guncelle(be_1, DatabaseFacade2.ConnectionString());
                                            }
                                            else
                                            {
                                                be_1.BdnHar_Guid = Guid.Empty;
                                                //be_1.BdnHar_RECid_RECno = -1;
                                                be_1.BdnHar_Har_uid = sip_2.sip_Guid;
                                                be_2 = Aktarimlar2.BedenHareketleri_Kaydet(be_1, DatabaseFacade2.ConnectionString());
                                            }
                                        }
                                    }
                                    #endregion
                                }
                            }
                        }
                        #endregion
                        st_1.sth_Guid = st_2.sth_Guid;
                        //st_1.sth_RECid_RECno = st_2.sth_RECid_RECno;
                        // aktarım Kontrol
                        var model = Aktarimlar2.AktarimParametreleriniGetir(DatabaseFacade3.ConnectionString());
                        if (Convert.ToBoolean(model.akt_hedef_irstofat))
                            st_1.sth_fat_uid = st_2.sth_fat_uid;
                        st_2 = Aktarimlar2.Stok_Hareketleri_Guncelle(st_1, DatabaseFacade2.ConnectionString());
                        #endregion
                    }
                    else
                    {
                        #region STOK HAREKETLERI YENİ KAYIT
                        CARI_HESAP_HAREKETLERI ch_1;
                        CARI_HESAP_HAREKETLERI ch_2;
                        SIPARISLER sip_1;
                        SIPARISLER sip_2;
                        STOK_HAREKETLERI_EK shek_1;
                        STOK_HAREKETLERI_EK shek_2;
                        CARI_HESAP_HAREKETLERI_EK chek_1;
                        CARI_HESAP_HAREKETLERI_EK chek_2;
                        #region CARİ HESAP HAREKETLERI KONTROLÜ
                        if (st_1.sth_fat_uid != Guid.Empty)
                        {
                            ch_1 = Aktarimlar2.Cari_Hesap_Hareket_EvrakDetayGetir(st_1.sth_fat_uid.Value, DatabaseFacade.ConnectionString());
                            chek_1 = Aktarimlar2.Cari_Hesap_HareketEK_EvrakDetayGetir(st_1.sth_fat_uid.Value, DatabaseFacade.ConnectionString());
                            if (ch_1 != null)
                            {
                                Evrak ch_ev = new Evrak();
                                ch_ev.RECno = ch_1.cha_Guid;
                                ch_ev.SatirNo = ch_1.cha_satir_no.Value;
                                ch_ev.SeriNo = ch_1.cha_evrakno_seri;
                                ch_ev.SiraNo = ch_1.cha_evrakno_sira.Value;
                                ch_ev.EvrakTip = ch_1.cha_evrak_tip.Value;
                                ch_ev.Tip = ch_1.cha_tip.Value;
                                ch_2 = Aktarimlar2.Cari_Hesap_Hareket_EvrakDetayGetir_2(ch_ev, DatabaseFacade2.ConnectionString());
                                chek_2 = Aktarimlar2.Cari_Hesap_HareketEK_EvrakDetayGetir_2(ch_ev, DatabaseFacade2.ConnectionString());
                                if (ch_2 != null)
                                {
                                    ch_1.cha_Guid = ch_2.cha_Guid;
                                    //ch_1.cha_RECid_RECno = ch_2.cha_RECid_RECno;
                                    ch_2 = Aktarimlar2.Cari_Hesap_Hareket_Guncelle(ch_1, DatabaseFacade2.ConnectionString());
                                    if (chek_1 != null)
                                    {
                                        if (st_1.sth_Tevkifat_turu != 0)
                                        {
                                            if (chek_2 != null)
                                            {
                                                chek_1.chaek_Guid = chek_2.chaek_Guid;
                                                //chek_1.chaek_RECid_RECno = chek_2.chaek_RECid_RECno;
                                                chek_1.chaek_related_uid = ch_2.cha_Guid;
                                                chek_2 = Aktarimlar2.Cari_Hesap_HareketEK_Guncelle(chek_1, DatabaseFacade2.ConnectionString());
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    ch_1.cha_Guid = Guid.Empty;
                                    //ch_1.cha_RECid_RECno = -100;
                                    ch_2 = Aktarimlar2.Cari_Hesap_Hareket_Kaydet(ch_1, DatabaseFacade2.ConnectionString());
                                    if (chek_1 != null)
                                    {
                                        chek_1.chaek_Guid = Guid.Empty;
                                        //chek_1.chaek_RECid_RECno = -100;
                                        chek_1.chaek_related_uid = ch_2.cha_Guid;
                                        chek_2 = Aktarimlar2.Cari_Hesap_HareketEK_Kaydet(chek_1, DatabaseFacade2.ConnectionString());
                                    }
                                }
                                st_1.sth_fat_uid = ch_2.cha_Guid;
                            }
                        }
                        #endregion
                        #region SİPARİŞLER KONTROLÜ
                        if (st_1.sth_sip_uid != Guid.Empty)
                        {
                            sip_1 = Aktarimlar2.Siparisler_EvrakDetayGetir(st_1.sth_sip_uid.Value, DatabaseFacade.ConnectionString());
                            if (sip_1 != null)
                            {
                                Evrak sip_ev = new Evrak();
                                sip_ev.RECno = sip_1.sip_Guid;
                                sip_ev.EvrakTip = sip_1.sip_tip.Value;
                                sip_ev.Tip = sip_1.sip_cins.Value;
                                sip_ev.SeriNo = sip_1.sip_evrakno_seri;
                                sip_ev.SiraNo = sip_1.sip_evrakno_sira.Value;
                                sip_ev.SatirNo = sip_1.sip_satirno.Value;
                                Guid _recno = sip_1.sip_Guid;
                                sip_2 = Aktarimlar2.Siparis_EvrakDetayGetir_2(sip_ev, DatabaseFacade2.ConnectionString());
                                if (sip_2 != null)
                                {
                                    sip_1.sip_Guid = sip_2.sip_Guid;
                                    //sip_1.sip_RECid_RECno = sip_2.sip_RECid_RECno;
                                    sip_2 = Aktarimlar2.Siparis_Guncelle(sip_1, DatabaseFacade2.ConnectionString());
                                }
                                else
                                {
                                    sip_1.sip_Guid = Guid.Empty;
                                    //sip_1.sip_RECid_RECno = -100;
                                    sip_2 = Aktarimlar2.Siparis_Kaydet(sip_1, DatabaseFacade2.ConnectionString());
                                }
                                st_1.sth_sip_uid = sip_2.sip_Guid;
                                if (par.akt_beden_har == true)
                                {
                                    #region Sipariş BEDEN HAREKETLERI
                                    List<BEDEN_HAREKETLERI> ls_be_sip = new List<BEDEN_HAREKETLERI>();
                                    ls_be_sip = Aktarimlar2.BedenHareketleriYukle(Char.Parse("P"), _recno, DatabaseFacade.ConnectionString());
                                    if (ls_be_sip.Count > 0)
                                    {
                                        foreach (BEDEN_HAREKETLERI be in ls_be_sip)
                                        {
                                            BEDEN_HAREKETLERI be_1 = be;
                                            BEDEN_HAREKETLERI be_2;
                                            be_2 = Aktarimlar2.Beden_Hareketleri_EvrakDetayGetir(Char.Parse("P"), sip_2.sip_Guid, be.BdnHar_BedenNo.Value, DatabaseFacade2.ConnectionString());
                                            if (be_2 != null)
                                            {
                                                be_1.BdnHar_Guid = be_2.BdnHar_Guid;
                                                //be_1.BdnHar_RECid_RECno = be_2.BdnHar_RECno;
                                                be_1.BdnHar_Har_uid = be_2.BdnHar_Har_uid;
                                                be_2 = Aktarimlar2.BedenHareketleri_Guncelle(be_1, DatabaseFacade2.ConnectionString());
                                            }
                                            else
                                            {
                                                be_1.BdnHar_Guid = Guid.Empty;
                                                //be_1.BdnHar_RECid_RECno = -1;
                                                be_1.BdnHar_Har_uid = sip_2.sip_Guid;
                                                be_2 = Aktarimlar2.BedenHareketleri_Kaydet(be_1, DatabaseFacade2.ConnectionString());
                                            }
                                        }
                                    }
                                    #endregion
                                }
                            }
                        }
                        #endregion
                        #region STOK HAREKETLERI EK KONTROLÜ
                        //if (st_1.sth_fat_recid_recno > 0)
                        //{
                        //    shek_1 = Aktarimlar2.Stok_HareketEK_EvrakDetayGetir(st_1.sth_fat_recid_recno.Value, DatabaseFacade.ConnectionString());
                        //    if (shek_1 != null)
                        //    {
                        //        Evrak shek_ev = new Evrak();
                        //        shek_ev.RECno = shek_1.sthek_RECno;
                        //        //ch_ev.SatirNo = ch_1.cha_satir_no.Value;
                        //        //ch_ev.SeriNo = ch_1.cha_evrakno_seri;
                        //        //ch_ev.SiraNo = ch_1.cha_evrakno_sira.Value;
                        //        //ch_ev.EvrakTip = ch_1.cha_evrak_tip.Value;
                        //        //ch_ev.Tip = ch_1.cha_tip.Value;
                        //        shek_2 = Aktarimlar2.Stok_HareketEK_EvrakDetayGetir_2(shek_ev, DatabaseFacade2.ConnectionString());
                        //        if (shek_2 != null)
                        //        {
                        //            shek_1.sthek_RECno = shek_2.sthek_RECno;
                        //            shek_1.sthek_RECid_RECno = shek_2.sthek_RECid_RECno;
                        //            shek_2 = Aktarimlar2.Stok_HareketEK_Guncelle(shek_1, DatabaseFacade2.ConnectionString());
                        //        }
                        //        else
                        //        {
                        //            shek_1.sthek_RECno = 0;
                        //            shek_1.sthek_RECid_RECno = -100;
                        //            shek_2 = Aktarimlar2.Stok_HareketEK_Kaydet(shek_1, DatabaseFacade2.ConnectionString());
                        //        }
                        //        shek_1.sthek_related_RECno = st_1.sth_fat_recid_recno;
                        //    }
                        //}
                        #endregion
                        st_1.sth_Guid = Guid.Empty;
                        //st_1.sth_RECid_RECno = -1;
                        //st_1.sth_sip_uid = Guid.Empty;
                        //st_1.sth_fat_uid = Guid.Empty;
                        st_2 = Aktarimlar2.Stok_Hareketleri_Kaydet(st_1, DatabaseFacade2.ConnectionString());
                        #endregion
                    }
                    if (par.akt_beden_har == true)
                    {
                        #region Stok Hareketleri BEDEN HAREKETLERI
                        List<BEDEN_HAREKETLERI> ls_be = new List<BEDEN_HAREKETLERI>();
                        ls_be = Aktarimlar2.BedenHareketleriYukle(Char.Parse("S"), _st_recno, DatabaseFacade.ConnectionString());
                        if (ls_be.Count > 0)
                        {
                            foreach (BEDEN_HAREKETLERI be in ls_be)
                            {
                                BEDEN_HAREKETLERI be_1 = be;
                                BEDEN_HAREKETLERI be_2;
                                be_2 = Aktarimlar2.Beden_Hareketleri_EvrakDetayGetir(Char.Parse("S"), st_2.sth_Guid, be.BdnHar_BedenNo.Value, DatabaseFacade2.ConnectionString());
                                if (be_2 != null)
                                {
                                    be_1.BdnHar_Guid = be_2.BdnHar_Guid;
                                    //be_1.BdnHar_RECid_RECno = be_2.BdnHar_RECno;
                                    be_1.BdnHar_Har_uid = be_2.BdnHar_Har_uid;
                                    be_2 = Aktarimlar2.BedenHareketleri_Guncelle(be_1, DatabaseFacade2.ConnectionString());
                                }
                                else
                                {
                                    be_1.BdnHar_Guid = Guid.Empty;
                                    //be_1.BdnHar_RECid_RECno = -1;
                                    be_1.BdnHar_Har_uid = st_2.sth_Guid;
                                    be_2 = Aktarimlar2.BedenHareketleri_Kaydet(be_1, DatabaseFacade2.ConnectionString());
                                }
                            }
                        }
                        #endregion
                    }
                    if (par.akt_cihaz_har == true)
                    {
                        #region Stok Hareketleri CIHAZ HAREKETLERI
                        List<CIHAZ_HAREKETLERI> ls_chz = new List<CIHAZ_HAREKETLERI>();
                        ls_chz = Aktarimlar2.CihazHareketleriYukle(Char.Parse("S"), _st_recno, DatabaseFacade.ConnectionString());
                        if (ls_chz.Count > 0)
                        {
                            foreach (CIHAZ_HAREKETLERI cz in ls_chz)
                            {
                                CIHAZ_HAREKETLERI chz_1 = cz;
                                CIHAZ_HAREKETLERI chz_2;
                                chz_2 = Aktarimlar2.Cihaz_Hareketleri_EvrakDetayGetir(Char.Parse("S"), st_2.sth_Guid, chz_1.ChHar_SeriNo, DatabaseFacade2.ConnectionString());
                                if (chz_2 != null)
                                {
                                    chz_1.ChHar_Guid = chz_2.ChHar_Guid;
                                    //chz_1.ChHar_RECid_RECno = chz_2.ChHar_RECid_RECno;
                                    chz_1.ChHar_master_uid = chz_2.ChHar_master_uid;
                                    chz_2 = Aktarimlar2.Cihaz_Hareketleri_Guncelle(chz_1, DatabaseFacade2.ConnectionString());
                                }
                                else
                                {
                                    chz_1.ChHar_Guid = Guid.Empty;
                                    //chz_1.ChHar_RECid_RECno = -1;
                                    chz_1.ChHar_master_uid = st_2.sth_Guid;
                                    chz_2 = Aktarimlar2.Cihaz_Hareketleri_Kaydet(chz_1, DatabaseFacade2.ConnectionString());
                                }
                            }
                        }
                        #endregion
                    }
                    Aktarimlar2.Stok_Hareketleri_Kilit_Guncelle(_st_recno, DatabaseFacade.ConnectionString());
                }
            }
            #endregion
            List<CARI_HESAP_HAREKETLERI> ls_2 = new List<CARI_HESAP_HAREKETLERI>();
            if (anlik_mi == false)
                ls_2 = Aktarimlar2.Cari_HareketleriYukle_2(serino, tarih1, tarih2, DatabaseFacade.ConnectionString(), (bool)par.akt_chaTip_belirle, chaTip, chaCins, chaNormalIade, chaEvrakTip, (bool)par.akt_srm, srmMerkezleri);
            else
                ls_2 = Aktarimlar2.Cari_HareketleriYukle_2_anlik(serino, tarih1, tarih2, DatabaseFacade.ConnectionString(), (bool)par.akt_chaTip_belirle, chaTip, chaCins, chaNormalIade, chaEvrakTip, (bool)par.akt_srm, srmMerkezleri);
            pbc.Properties.Maximum = ls_2.Count;
            pb_i = 0;
            #region FATURA OLMAYAN CARİ HESAP HAREKETLERI
            if (ls_2.Count > 0)
            {
                foreach (CARI_HESAP_HAREKETLERI cha in ls_2)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    SetControlText(lbl_durum, "Cari hesap hareketleri aktarılıyor.");
                    Evrak ev = new Evrak();
                    ev.RECno = cha.cha_Guid;
                    ev.EvrakTip = cha.cha_evrak_tip.Value;
                    ev.Tip = cha.cha_tip.Value;
                    ev.SeriNo = cha.cha_evrakno_seri;
                    ev.SiraNo = cha.cha_evrakno_sira.Value;
                    ev.SatirNo = cha.cha_satir_no.Value;
                    CARI_HESAP_HAREKETLERI ch_1;
                    CARI_HESAP_HAREKETLERI ch_2;
                    CARI_HESAP_HAREKETLERI_EK chek_1;
                    CARI_HESAP_HAREKETLERI_EK chek_2;
                    ch_1 = cha;
                    Guid _cha_recno = cha.cha_Guid;
                    ch_2 = Aktarimlar2.Cari_Hesap_Hareket_EvrakDetayGetir_2(ev, DatabaseFacade2.ConnectionString());
                    chek_1 = Aktarimlar2.Cari_Hesap_HareketEK_EvrakDetayGetir(ch_1.cha_Guid, DatabaseFacade.ConnectionString());
                    if (ch_2 != null)
                    {
                        ODEME_EMIRLERI od_1;
                        ODEME_EMIRLERI od_2;
                        ch_1.cha_Guid = ch_2.cha_Guid;
                        //ch_1.cha_RECid_RECno = ch_2.cha_RECid_RECno;
                        ch_2 = Aktarimlar2.Cari_Hesap_Hareket_Guncelle(ch_1, DatabaseFacade2.ConnectionString());
                        if (chek_1 != null)
                        {
                            try
                            {
                                chek_2 = Aktarimlar2.Cari_Hesap_HareketEK_EvrakDetayGetir_3(ch_2.cha_Guid, DatabaseFacade2.ConnectionString());
                                chek_1.chaek_Guid = chek_2.chaek_Guid;
                                //chek_1.chaek_RECid_RECno = chek_2.chaek_RECid_RECno;
                                chek_1.chaek_related_uid = ch_2.cha_Guid;
                                chek_2 = Aktarimlar2.Cari_Hesap_HareketEK_Guncelle(chek_1, DatabaseFacade2.ConnectionString());
                            }
                            catch (Exception)
                            {
                                chek_1.chaek_Guid = Guid.Empty;
                                //chek_1.chaek_RECid_RECno = -100;
                                chek_1.chaek_related_uid = ch_2.cha_Guid;
                                chek_2 = Aktarimlar2.Cari_Hesap_HareketEK_Kaydet(chek_1, DatabaseFacade2.ConnectionString());
                            }
                        }
                        #region ÖDEME EMİRLERİ AKTARIMI
                        od_1 = Aktarimlar2.OdemeEmirleri_EvrakDetayGetir(CariHarTipToOdemeTip(ch_1.cha_cinsi.Value), ch_1.cha_trefno, DatabaseFacade.ConnectionString());
                        if (od_1 != null)
                        {
                            od_2 = Aktarimlar2.OdemeEmirleri_EvrakDetayGetir(CariHarTipToOdemeTip(ch_1.cha_cinsi.Value), ch_1.cha_trefno, DatabaseFacade2.ConnectionString());
                            if (od_2 != null)
                            {
                                od_1.sck_Guid = od_2.sck_Guid;
                                //od_1.sck_RECid_RECno = od_2.sck_RECid_RECno;
                                od_2 = Aktarimlar2.OdemeEmirleri_Guncelle(od_1, DatabaseFacade2.ConnectionString());
                            }
                            else
                            {
                                od_1.sck_Guid = Guid.Empty;
                                //od_1.sck_RECid_RECno = -1;
                                od_2 = Aktarimlar2.OdemeEmirleri_Kaydet(od_1, DatabaseFacade2.ConnectionString());
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        ODEME_EMIRLERI od_1;
                        ODEME_EMIRLERI od_2;
                        ch_1.cha_Guid = Guid.Empty;
                        //ch_1.cha_RECid_RECno = -100;
                        ch_2 = Aktarimlar2.Cari_Hesap_Hareket_Kaydet(ch_1, DatabaseFacade2.ConnectionString());
                        if (chek_1 != null)
                        {
                            //    try
                            //    {
                            //        chek_2 = Aktarimlar2.Cari_Hesap_HareketEK_EvrakDetayGetir_3(ch_1.cha_RECno, DatabaseFacade2.ConnectionString());
                            //        chek_1.chaek_RECno = chek_2.chaek_RECno;
                            //        chek_1.chaek_RECid_RECno = chek_2.chaek_RECid_RECno;
                            //        chek_1.chaek_related_RECno = ch_2.cha_RECid_RECno;
                            //        chek_2 = Aktarimlar2.Cari_Hesap_HareketEK_Guncelle(chek_1, DatabaseFacade2.ConnectionString());
                            //    }
                            //    catch (Exception)
                            //    {
                            chek_1.chaek_Guid = Guid.Empty;
                            //chek_1.chaek_RECid_RECno = -100;
                            chek_1.chaek_related_uid = ch_2.cha_Guid;
                            chek_2 = Aktarimlar2.Cari_Hesap_HareketEK_Kaydet(chek_1, DatabaseFacade2.ConnectionString());
                            //    }
                        }
                        #region ODEME EMİRLERİ AKTARIMI
                        od_1 = Aktarimlar2.OdemeEmirleri_EvrakDetayGetir(CariHarTipToOdemeTip(ch_1.cha_cinsi.Value), ch_1.cha_trefno, DatabaseFacade.ConnectionString());
                        if (od_1 != null)
                        {
                            od_2 = Aktarimlar2.OdemeEmirleri_EvrakDetayGetir(CariHarTipToOdemeTip(ch_1.cha_cinsi.Value), ch_1.cha_trefno, DatabaseFacade2.ConnectionString());
                            if (od_2 != null)
                            {
                                od_1.sck_Guid = od_2.sck_Guid;
                                //od_1.sck_RECid_RECno = od_2.sck_RECid_RECno;
                                od_2 = Aktarimlar2.OdemeEmirleri_Guncelle(od_1, DatabaseFacade2.ConnectionString());
                            }
                            else
                            {
                                od_1.sck_Guid = Guid.Empty;
                                //od_1.sck_RECid_RECno = -1;
                                od_2 = Aktarimlar2.OdemeEmirleri_Kaydet(od_1, DatabaseFacade2.ConnectionString());
                            }
                        }
                        #endregion
                    }
                    Aktarimlar2.Cari_Hesap_Hareket_Kilit_Guncelle(_cha_recno, DatabaseFacade.ConnectionString());
                }
            }
            #endregion
            List<SIPARISLER> ls_sip = new List<SIPARISLER>();
            if (anlik_mi == false)
                ls_sip = Aktarimlar2.SiparisleriYukle(serino, tarih1, tarih2, DatabaseFacade.ConnectionString(), (bool)par.akt_sipTip_belirle, sipTip, sipCins, (bool)par.akt_srm, srmMerkezleri);
            else
                ls_sip = Aktarimlar2.SiparisleriYukle_anlik(serino, tarih1, tarih2, DatabaseFacade.ConnectionString(), (bool)par.akt_sipTip_belirle, sipTip, sipCins, (bool)par.akt_srm, srmMerkezleri);
            pbc.Properties.Maximum = ls_sip.Count;
            pb_i = 0;
            #region ÇIKIŞI YAPILMAMIŞ SIPARISLER AKTARIMI
            if (ls_sip.Count > 0)
            {
                foreach (SIPARISLER sip in ls_sip)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    SetControlText(lbl_durum, "Siparişler aktarılıyor...");
                    Evrak ev = new Evrak();
                    ev.RECno = sip.sip_Guid;
                    ev.EvrakTip = sip.sip_tip.Value;
                    ev.Tip = sip.sip_cins.Value;
                    ev.SeriNo = sip.sip_evrakno_seri;
                    ev.SiraNo = sip.sip_evrakno_sira.Value;
                    ev.SatirNo = sip.sip_satirno.Value;
                    SIPARISLER sip_1;
                    SIPARISLER sip_2;
                    sip_1 = sip;
                    Guid _recno = sip.sip_Guid;
                    Evrak sip_ev = new Evrak();
                    sip_ev.RECno = sip_1.sip_Guid;
                    sip_ev.EvrakTip = sip_1.sip_tip.Value;
                    sip_ev.Tip = sip_1.sip_cins.Value;
                    sip_ev.SeriNo = sip_1.sip_evrakno_seri;
                    sip_ev.SiraNo = sip_1.sip_evrakno_sira.Value;
                    sip_ev.SatirNo = sip_1.sip_satirno.Value;
                    sip_2 = Aktarimlar2.Siparis_EvrakDetayGetir_2(sip_ev, DatabaseFacade2.ConnectionString());
                    if (sip_2 != null)
                    {
                        sip_1.sip_Guid = sip_2.sip_Guid;
                        //sip_1.sip_RECid_RECno = sip_2.sip_RECid_RECno;
                        sip_2 = Aktarimlar2.Siparis_Guncelle(sip_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sip_1.sip_Guid = Guid.Empty;
                        // sip_1.sip_RECid_RECno = -100;
                        sip_2 = Aktarimlar2.Siparis_Kaydet(sip_1, DatabaseFacade2.ConnectionString());
                    }
                    if (par.akt_beden_har == true)
                    {
                        #region Sipariş BEDEN HAREKETLERI
                        List<BEDEN_HAREKETLERI> ls_be_sip = new List<BEDEN_HAREKETLERI>();
                        ls_be_sip = Aktarimlar2.BedenHareketleriYukle(Char.Parse("P"), _recno, DatabaseFacade.ConnectionString());
                        if (ls_be_sip.Count > 0)
                        {
                            foreach (BEDEN_HAREKETLERI be in ls_be_sip)
                            {
                                BEDEN_HAREKETLERI be_1 = be;
                                BEDEN_HAREKETLERI be_2;
                                be_2 = Aktarimlar2.Beden_Hareketleri_EvrakDetayGetir(Char.Parse("P"), sip_2.sip_Guid, be.BdnHar_BedenNo.Value, DatabaseFacade2.ConnectionString());
                                if (be_2 != null)
                                {
                                    be_1.BdnHar_Guid = be_2.BdnHar_Guid;
                                    //be_1.BdnHar_RECid_RECno = be_2.BdnHar_RECno;
                                    be_1.BdnHar_Har_uid = be_2.BdnHar_Har_uid;
                                    be_2 = Aktarimlar2.BedenHareketleri_Guncelle(be_1, DatabaseFacade2.ConnectionString());
                                }
                                else
                                {
                                    be_1.BdnHar_Guid = Guid.Empty;
                                    //be_1.BdnHar_RECid_RECno = -1;
                                    be_1.BdnHar_Har_uid = sip_2.sip_Guid;
                                    be_2 = Aktarimlar2.BedenHareketleri_Kaydet(be_1, DatabaseFacade2.ConnectionString());
                                }
                            }
                        }
                        #endregion
                    }
                }
            }
            #endregion
            if (par.akt_satis_sart == true)
            {
                List<SATIS_SARTLARI> ls_sat_sart = new List<SATIS_SARTLARI>();
                if (anlik_mi == false)
                    ls_sat_sart = Aktarimlar2.Satis_Sartlarini_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
                else
                    ls_sat_sart = Aktarimlar2.Satis_Sartlarini_Yukle_anlik(tarih1, tarih2, DatabaseFacade.ConnectionString());
                pbc.Properties.Maximum = ls_sip.Count;
                pb_i = 0;
                #region SATIŞ ŞARTLARI AKTARIMI
                if (ls_sat_sart.Count > 0)
                {
                    foreach (SATIS_SARTLARI be in ls_sat_sart)
                    {
                        pb_i += 1;
                        pbc.Text = pb_i.ToString();
                        SATIS_SARTLARI be_1 = be;
                        SATIS_SARTLARI be_2;
                        be_2 = Aktarimlar2.Satis_Sartlari_EvrakDetayGetir(be_1.sat_evrakno_seri, be_1.sat_evrakno_sira.Value, be_1.sat_satirno.Value, DatabaseFacade2.ConnectionString());
                        if (be_2 != null)
                        {
                            be_1.sat_Guid = be_2.sat_Guid;
                            //be_1.sat_RECid_RECno = be_2.sat_RECid_RECno;
                            be_2 = Aktarimlar2.Satis_Sartlari_Guncelle(be_1, DatabaseFacade2.ConnectionString());
                        }
                        else
                        {
                            be_1.sat_Guid = Guid.Empty;
                            //be_1.sat_RECid_RECno = -1;
                            be_2 = Aktarimlar2.Satis_Sartlari_Kaydet(be_1, DatabaseFacade2.ConnectionString());
                        }
                    }
                }
                #endregion
            }
            #region MUHASEBE FISLERI
            if (false)//(anlik_mi == false)
            {
                #region Eski kayıtları sil
                pbc.Properties.Maximum = 10;
                pb_i = 0;
                pb_i += 1;
                pbc.Text = pb_i.ToString();
                SetControlText(lbl_durum, "Muhasebe Fişleri silinen kayıtlar kontrol ediliyor...");
                Aktarimlar2.Muhasebe_Fisleri_Sil(serino, _tarih1.Date, _tarih2.Date, DatabaseFacade2.ConnectionString());
                pb_i += 10;
                pbc.Text = pb_i.ToString();
                #endregion
                List<MUHASEBE_FISLERI> ls_muh = new List<MUHASEBE_FISLERI>();
                ls_muh = Aktarimlar2.Muhasebe_Fisleri_Getir(serino, tarih1.Date, tarih2.Date, DatabaseFacade.ConnectionString());
                pbc.Properties.Maximum = ls_muh.Count();
                pb_i = 0;
                #region MUHASEBE_FISLERI AKTARIMI
                if (ls_muh.Count > 0)
                {
                    foreach (MUHASEBE_FISLERI mfis in ls_muh)
                    {
                        pb_i += 1;
                        pbc.Text = pb_i.ToString();
                        SetControlText(lbl_durum, "Muhasebe Fişleri aktarılıyor...");
                        MUHASEBE_FISLERI mf_1 = mfis;
                        MUHASEBE_FISLERI mf_2;
                        #region Faturaya bağlı muhasebe fişi
                        if (mfis.fis_ticari_uid != Guid.Empty)
                        {
                            Guid rn = Aktarimlar2.FisTicariRecNo_Getir(mfis, DatabaseFacade.ConnectionString(), DatabaseFacade2.ConnectionString());
                            if (rn != Guid.Empty)
                            {
                                mf_1.fis_ticari_uid = rn;
                                mf_2 = Aktarimlar2.Muhasebe_Fisleri_Kaydet(mf_1, DatabaseFacade2.ConnectionString());
                            }
                        }
                        #endregion
                        #region Bağımsız muhasebe fişleri
                        else if (mfis.fis_ticari_uid == Guid.Empty)
                        {
                            mf_2 = Aktarimlar2.Muhasebe_Fisleri_Kaydet(mf_1, DatabaseFacade2.ConnectionString());
                        }
                        #endregion
                    }
                }
                #endregion
            }
            #endregion
            SetControlText(lbl_durum, "Aktarım tamamlandı.");
            pb_i = 0;
            pbc.Text = "0";
        }
        void DeletedRowsControl(DateTime _tarih1, DateTime _tarih2, bool anlik_mi, AKTARIM_PARAMETRELERI par, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            DateTime tarih1 = _tarih1;
            DateTime tarih2 = _tarih2;
            #region Kayıtları Silinecek Kullanıcı No ları
            Int32[] kulno = new Int32[AktarimParametreleri.AktarimKullanicilari.Count];
            int ii = 0;
            foreach (AKTARIM_KULLANICILARI kul in AktarimParametreleri.AktarimKullanicilari)
            {
                kulno[ii] = kul.kul_no.Value;
                ii += 1;
            }
            #endregion
            #region Aktarımı Yapılacak Evrak Seri No ları
            string[] serino = new string[AktarimParametreleri.AktarimSerileri.Count];
            int i = 0;
            foreach (AKTARIM_SERILERI ser in AktarimParametreleri.AktarimSerileri)
            {
                serino[i] = ser.ser_serino;
                i += 1;
            }
            #endregion
            #region Hedef DB STOK_HAREKETLERI
            List<STOK_HAREKETLERI> ls = new List<STOK_HAREKETLERI>();
            if (anlik_mi == false)
                ls = Aktarimlar2.HareketleriYukle_Del(serino, kulno, tarih1, tarih2, DatabaseFacade2.ConnectionString());
            else
                ls = Aktarimlar2.HareketleriYukle_anlik_Del(serino, kulno, tarih1, tarih2, DatabaseFacade2.ConnectionString());
            //UNIQUE
            //sth_evraktip, sth_evrakno_seri, sth_evrakno_sira, sth_satirno
            var k = from s in ls
                    group s by new
                    {
                        s.sth_evraktip,
                        s.sth_evrakno_seri,
                        s.sth_evrakno_sira
                    } into g
                    select new { g.Key.sth_evrakno_seri, g.Key.sth_evrakno_sira, g.Key.sth_evraktip };
            #region Show State
            pbc.Text = "0";
            pbc.Properties.Step = 1;
            pbc.Properties.Maximum = k.Count();
            Int32 pb_i = 0;
            #endregion
            foreach (var e in k)
            {
                pb_i += 1;
                pbc.Text = pb_i.ToString();
                SetControlText(lbl_durum, "Stok Hareketleri silinen kayıtlar kontrol ediliyor...");
                int count_kaynak = 0;
                int count_hedef = 0;
                count_hedef = ls.Count(t => (t.sth_evraktip == e.sth_evraktip && t.sth_evrakno_seri == e.sth_evrakno_seri && t.sth_evrakno_sira == e.sth_evrakno_sira));
                count_kaynak = Aktarimlar2.Stok_Hareket_EvrakCount(e.sth_evrakno_seri, e.sth_evrakno_sira.Value, e.sth_evraktip.Value, DatabaseFacade.ConnectionString());
                if (count_kaynak < count_hedef)
                {
                    Aktarimlar2.Stok_Hareket_EvrakSil(e.sth_evrakno_seri, e.sth_evrakno_sira.Value, e.sth_evraktip.Value, DatabaseFacade2.ConnectionString());
                }
            }
            #endregion
            #region Hedef DB CARI_HESAP_HAREKETLERI
            List<CARI_HESAP_HAREKETLERI> ls_2 = new List<CARI_HESAP_HAREKETLERI>();
            if (anlik_mi == false)
                ls_2 = Aktarimlar2.Cari_HareketleriYukle_2_Del(serino, kulno, tarih1, tarih2, DatabaseFacade2.ConnectionString());
            else
                ls_2 = Aktarimlar2.Cari_HareketleriYukle_2_anlik_Del(serino, kulno, tarih1, tarih2, DatabaseFacade2.ConnectionString());
            //UNIQUE
            //cha_evrak_tip, cha_evrakno_seri, cha_evrakno_sira, cha_satir_no
            var k2 = from c in ls_2
                     group c by new
                     {
                         c.cha_evrak_tip,
                         c.cha_evrakno_seri,
                         c.cha_evrakno_sira
                     } into g
                     select new { g.Key.cha_evrak_tip, g.Key.cha_evrakno_seri, g.Key.cha_evrakno_sira };
            #region Show State
            pbc.Properties.Maximum = k2.Count();
            pb_i = 0;
            #endregion
            foreach (var e in k2)
            {
                pb_i += 1;
                pbc.Text = pb_i.ToString();
                SetControlText(lbl_durum, "Cari Hesap Hareketleri silinen kayıtlar kontrol ediliyor...");
                int count_kaynak = 0;
                int count_hedef = 0;
                count_hedef = ls_2.Count(t => (t.cha_evrak_tip == e.cha_evrak_tip && t.cha_evrakno_seri == e.cha_evrakno_seri && t.cha_evrakno_sira == e.cha_evrakno_sira));
                count_kaynak = Aktarimlar2.Cari_Hesap_Hareket_EvrakCount(e.cha_evrakno_seri, e.cha_evrakno_sira.Value, e.cha_evrak_tip.Value, DatabaseFacade.ConnectionString());
                if (count_kaynak < count_hedef)
                {
                    Aktarimlar2.Cari_Hesap_Hareket_EvrakSil(e.cha_evrakno_seri, e.cha_evrakno_sira.Value, e.cha_evrak_tip.Value, DatabaseFacade2.ConnectionString());
                }
            }
            #endregion
            #region Hedef DB SIPARISLER
            List<SIPARISLER> ls_sip = new List<SIPARISLER>();
            if (anlik_mi == false)
                ls_sip = Aktarimlar2.SiparisleriYukle_Del(serino, kulno, tarih1, tarih2, DatabaseFacade2.ConnectionString());
            else
                ls_sip = Aktarimlar2.SiparisleriYukle_anlik_Del(serino, kulno, tarih1, tarih2, DatabaseFacade2.ConnectionString());
            //UNIQUE
            //sip_tip, sip_cins, sip_evrakno_seri, sip_evrakno_sira, sip_satirno
            var k_sip = from c in ls_sip
                        group c by new
                        {
                            c.sip_tip,
                            c.sip_cins,
                            c.sip_evrakno_seri,
                            c.sip_evrakno_sira
                        } into g
                        select new { g.Key.sip_tip, g.Key.sip_cins, g.Key.sip_evrakno_seri, g.Key.sip_evrakno_sira };
            #region Show State
            pbc.Properties.Maximum = k_sip.Count();
            pb_i = 0;
            #endregion
            foreach (var e in k_sip)
            {
                pb_i += 1;
                pbc.Text = pb_i.ToString();
                SetControlText(lbl_durum, "Siparişler silinen kayıtlar kontrol ediliyor...");
                int count_kaynak = 0;
                int count_hedef = 0;
                count_hedef = ls_sip.Count(t => (t.sip_tip == e.sip_tip && t.sip_cins == e.sip_cins && t.sip_evrakno_seri == e.sip_evrakno_seri && t.sip_evrakno_sira == e.sip_evrakno_sira));
                count_kaynak = Aktarimlar2.Siparis_EvrakCount(e.sip_evrakno_seri, e.sip_evrakno_sira.Value, e.sip_tip.Value, e.sip_cins.Value, DatabaseFacade.ConnectionString());
                if (count_kaynak < count_hedef)
                {
                    Aktarimlar2.Siparis_EvrakSil(e.sip_evrakno_seri, e.sip_evrakno_sira.Value, e.sip_tip.Value, e.sip_cins.Value, DatabaseFacade2.ConnectionString());
                }
            }
            #endregion
            #region Satış Şartları
            if (par.akt_satis_sart == true)
            {
                List<SATIS_SARTLARI> ls_sat_sart = new List<SATIS_SARTLARI>();
                if (anlik_mi == false)
                    ls_sat_sart = Aktarimlar2.Satis_Sartlarini_Yukle_Del(kulno, tarih1, tarih2, DatabaseFacade2.ConnectionString());
                else
                    ls_sat_sart = Aktarimlar2.Satis_Sartlarini_Yukle_anlik_Del(kulno, tarih1, tarih2, DatabaseFacade2.ConnectionString());
                //UNIQUE
                //sat_evrakno_seri, sat_evrakno_sira, sat_satirno
                var k_sat = from c in ls_sat_sart
                            group c by new
                            {
                                c.sat_evrakno_seri,
                                c.sat_evrakno_sira
                            } into g
                            select new { g.Key.sat_evrakno_seri, g.Key.sat_evrakno_sira };
                #region Show State
                pbc.Properties.Maximum = k_sat.Count();
                pb_i = 0;
                #endregion
                foreach (var e in k_sat)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    SetControlText(lbl_durum, "Satış şartları silinen kayıtlar kontrol ediliyor...");
                    int count_kaynak = 0;
                    int count_hedef = 0;
                    count_hedef = ls_sat_sart.Count(t => (t.sat_evrakno_seri == e.sat_evrakno_seri && t.sat_evrakno_sira == e.sat_evrakno_sira));
                    count_kaynak = Aktarimlar2.Satis_Sartlari_EvrakCount(e.sat_evrakno_seri, e.sat_evrakno_sira.Value, DatabaseFacade.ConnectionString());
                    if (count_kaynak < count_hedef)
                    {
                        Aktarimlar2.Satis_Sartlari_EvrakSil(e.sat_evrakno_seri, e.sat_evrakno_sira.Value, DatabaseFacade.ConnectionString());
                    }
                }
            }
            #endregion
            #region Show State
            SetControlText(lbl_durum, "Silinen kayıt kontrolü tamamlandı.");
            pb_i = 0;
            pbc.Text = "0";
            #endregion
        }
        private void smb_evrak_aktar_Click(object sender, EventArgs e)
        {
            AktarimEkraniDondur(true);
            tmr_ev.Enabled = false;
            if (AktarimParametreleri.Parametre.akt_evrak_anlik == true)
            {
                timer_aktarim.Enabled = true;
            }
            SetControlText(lbl_son_aktarim_elle, DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString());
            new Thread(GuncellemeliAktarim).Start();
        }
        #endregion
        #region TİMER İŞLEMLERİ
        bool aktarim_bittimi = false;
        private void timer_kontrol_Tick(object sender, EventArgs e)
        {
            if (baglanti_durum == true)
            {
                AnlikAktarimTimerKontrol();
                timer_kontrol.Enabled = false;
            }
        }
        private void timer_aktarim_Tick(object sender, EventArgs e)
        {
            if (aktarim_bittimi == true)
            {
                AktarimEkraniDondur(false);
                AnlikAktarimTimerKontrol();
                timer_aktarim.Enabled = false;
                aktarim_bittimi = false;
            }
        }
        private void tmr_ev_Tick(object sender, EventArgs e)
        {
            if (AktarimParametreleri.Parametre.akt_evrak_gunluk == true ||
                AktarimParametreleri.Parametre.akt_kart_gunluk == true)
            {
                string zaman1 = AktarimParametreleri.Parametre.akt_evrak_aktarim_saat.Value.AddHours(-1).ToLongTimeString();
                string zaman2 = AktarimParametreleri.Parametre.akt_evrak_aktarim_saat.Value.AddHours(1).ToLongTimeString();
                string suanki_zaman = DateTime.Now.ToLongTimeString();
                if (Convert.ToDateTime(suanki_zaman) >= Convert.ToDateTime(zaman1) && Convert.ToDateTime(suanki_zaman) <= Convert.ToDateTime(zaman2))
                {
                    return;
                }
            }
            AktarimEkraniDondur(true);
            tmr_ev.Enabled = false;
            // tmr_kart.Enabled = false;
            timer_aktarim.Enabled = true;
            SetControlText(lbl_son_aktarim_anlik, DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString());
            notifyicon.BalloonTipText = "Anlık aktarım başlıyor...";
            // notifyicon.ShowBalloonTip(10);
            new Thread(AnlikAktarim).Start();
        }
        private void timer_gunluk_Tick(object sender, EventArgs e)
        {
            if (DateTime.Now.ToLongTimeString() == AktarimParametreleri.Parametre.akt_evrak_aktarim_saat.Value.ToLongTimeString())
            {
                AktarimEkraniDondur(true);
                tmr_ev.Enabled = false;
                if (AktarimParametreleri.Parametre.akt_evrak_gunluk == true ||
                    AktarimParametreleri.Parametre.akt_kart_gunluk == true)
                {
                    timer_aktarim.Enabled = true;
                }
                notifyicon.BalloonTipText = "Günlük aktarım başlıyor...";
                //  notifyicon.ShowBalloonTip(10);
                new Thread(GunlukAktarim).Start();
            }
        }
        void AnlikAktarimTimerKontrol()
        {
            if (AktarimParametreleri.Parametre.akt_evrak_anlik.Value == true ||
                AktarimParametreleri.Parametre.akt_kart_anlik.Value == true)
            {
                tmr_ev.Interval = 60000 * AktarimParametreleri.Parametre.akt_dakika.Value;
                tmr_ev.Enabled = true;
                tmr_ev.Start();
            }
            else
            {
                tmr_ev.Enabled = false;
            }
            if (AktarimParametreleri.Parametre.akt_evrak_gunluk.Value == true ||
                AktarimParametreleri.Parametre.akt_kart_gunluk.Value == true)
            {
                timer_gunluk.Enabled = true;
            }
            else
            {
                timer_gunluk.Enabled = false;
            }
        }
        #endregion
        #region KART AKTARMA İŞLEMLERİ
        private void sb_secili_kartlar_Click(object sender, EventArgs e)
        {
            Int32 sonuc = Tools.MesajPenceresi_Return("Bütün kartların aktarımı uzun bir zaman alabilir. Bu işlemi şimdi yapmak istediğinizden emin misiniz ?", "Hayır", "Evet");
            if (sonuc == 0)
            {
                return;
            }
            AktarimEkraniDondur(true);
            SetControlText(lbl_sure1, DateTime.Now.ToLongTimeString());
            SetControlText(lbl_süre2, "SÜRE");
            tmr_ev.Enabled = false;
            if (AktarimParametreleri.Parametre.akt_evrak_anlik == true)
            {
                timer_aktarim.Enabled = true;
            }
            SetControlText(lbl_son_aktarim_elle_kart, DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString());
            new Thread(ButunKartlarinAktarimi).Start();
        }
        private void sb_kart_aktar_Click(object sender, EventArgs e)
        {
            AktarimEkraniDondur(true);
            SetControlText(lbl_sure1, DateTime.Now.ToLongTimeString());
            SetControlText(lbl_süre2, "SÜRE");
            tmr_ev.Enabled = false;
            if (AktarimParametreleri.Parametre.akt_evrak_anlik == true)
            {
                timer_aktarim.Enabled = true;
            }
            SetControlText(lbl_son_aktarim_elle_kart, DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString());
            new Thread(TariheGoreKartlarinAktarimi).Start();
            //TariheGoreKartlarinAktarimi();
        }
        void TariheGoreKartlarinAktarimi()
        {
            try
            {
                DateTime tarih1 = dateEdit1.DateTime;
                DateTime tarih2 = dateEdit2.DateTime.AddDays(1).Date;
                AKTARIM_PARAMETRELERI par = AktarimParametreleri.Parametre;
                SeciliKartlarinAktarimi(tarih1, tarih2, par, true);
            }
            catch (ThreadAbortException ex)
            {
                AktarimEkraniDondur(false);
                SetControlText(lbl_süre2, DateTime.Now.ToLongTimeString());
                if (AktarimParametreleri.Parametre.akt_evrak_anlik == true)
                {
                    aktarim_bittimi = true;
                }
                SetControlText(lbl_durum_1, "Aktarım işlemi tamamlanamadı. Log dosyalarını kontrol ediniz...");
                pbc_1.Text = "0";
                Thread.CurrentThread.Abort();
            }
        }
        void ButunKartlarinAktarimi()
        {
            try
            {
                DateTime tarih1 = DateTime.Now.Date;
                DateTime tarih2 = DateTime.Now.Date;
                AKTARIM_PARAMETRELERI par = AktarimParametreleri.Parametre;
                SeciliKartlarinAktarimi(tarih1, tarih2, par, false);
            }
            catch (ThreadAbortException ex)
            {
                AktarimEkraniDondur(false);
                SetControlText(lbl_süre2, DateTime.Now.ToLongTimeString());
                if (AktarimParametreleri.Parametre.akt_evrak_anlik == true)
                {
                    aktarim_bittimi = true;
                }
                SetControlText(lbl_durum_1, "Aktarım işlemi tamamlanamadı. Log dosyalarını kontrol ediniz...");
                pbc_1.Text = "0";
                Thread.CurrentThread.Abort();
            }
        }
        bool kart_aktarim_bittimi;
        void AnlikKartAktarimi()
        {
            try
            {
                DateTime tarih_1 = DateTime.Now;
                DateTime tarih_0 = DateTime.Now;//Convert.ToDateTime("01.01.2017 00:00:00");
                DateTime tarih1;
                DateTime tarih2;
                AKTARIM_PARAMETRELERI par = AktarimParametreleri.Parametre;
                double eksi_dak = Convert.ToDouble((par.akt_dakika.Value + 60) * -1);
                double arti_dak = Convert.ToDouble(par.akt_dakika.Value + 10);
                pbc_1.Text = "0";
                SetControlText(lbl_durum_1, "Kart Aktarımı başlıyor...");
                tarih1 = tarih_0.AddMinutes(eksi_dak);
                tarih2 = tarih_1.AddMinutes(arti_dak);
                AnlikKartlarinAktarimi(tarih1, tarih2, par, true);
                kart_aktarim_bittimi = true;
            }
            catch (ThreadAbortException ex)
            {
                AktarimEkraniDondur(false);
                SetControlText(lbl_süre2, DateTime.Now.ToLongTimeString());
                if (AktarimParametreleri.Parametre.akt_evrak_anlik == true)
                {
                    aktarim_bittimi = true;
                }
                SetControlText(lbl_durum_1, "Aktarım işlemi tamamlanamadı. Log dosyalarını kontrol ediniz...");
                pbc_1.Text = "0";
                Thread.CurrentThread.Abort();
            }
        }
        void GunlukKartAktarimi()
        {
            try
            {
                DateTime tarih = DateTime.Now;
                DateTime tarih1;
                DateTime tarih2;
                AKTARIM_PARAMETRELERI par = AktarimParametreleri.Parametre;
                pbc_1.Text = "0";
                SetControlText(lbl_durum_1, "Kart Aktarımı başlıyor...");
                tarih1 = DateTime.Now.Date;
                tarih2 = DateTime.Now.AddDays(1).Date;
                GunlukKartlarinAktarimi(tarih1, tarih2, par, true);
                kart_aktarim_bittimi = true;
            }
            catch (ThreadAbortException ex)
            {
                AktarimEkraniDondur(false);
                SetControlText(lbl_süre2, DateTime.Now.ToLongTimeString());
                if (AktarimParametreleri.Parametre.akt_evrak_anlik == true)
                {
                    aktarim_bittimi = true;
                }
                SetControlText(lbl_durum_1, "Aktarım işlemi tamamlanamadı. Log dosyalarını kontrol ediniz...");
                pbc_1.Text = "0";
                Thread.CurrentThread.Abort();
            }
        }
        void SeciliKartlarinAktarimi(DateTime tarih1, DateTime tarih2, AKTARIM_PARAMETRELERI par, bool anlik_mi)
        {
            pbc_1.Text = "0";
            SetControlText(lbl_durum_1, "");
            if (par.akt_cari_hesaplar.Value >= 1)
            {
                Cari_Hesap_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_cari_hesap_adresleri >= 1)
            {
                Cari_Hesap_Adresleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_cari_hesap_bolgeler >= 1)
            {
                Cari_Hesap_Bolgeleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_cari_hesap_gruplari >= 1)
            {
                Cari_Hesap_Gruplari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_cari_hesap_yetkilileri >= 1)
            {
                Cari_Hesap_Yetkilileri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_cari_personel_tanimlari >= 1)
            {
                Cari_Personel_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stoklar >= 1)
            {
                Stok_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_serino_tanimlari >= 1)
            {
                Stok_SeriNo_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_sarf_receteleri >= 1)
            {
                Stok_Sarf_Receteleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_prim_tanimlari >= 1)
            {
                Stok_Prim_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_paket_tanimlari >= 1)
            {
                Stok_Paket_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_depo_detaylari >= 1)
            {
                Stok_Depo_Detayları_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_cari_iskonto_tanimlari >= 1)
            {
                Stok_Cari_iskonto_tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_alt_gruplari >= 1)
            {
                Stok_Alt_Gruplari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_ana_gruplari >= 1)
            {
                Stok_Ana_Gruplari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_ambalajlari >= 1)
            {
                Stok_Ambalajlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_anahammaddeleri >= 1)
            {
                Stok_Anahammaddeleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_beden_tanimlari >= 1)
            {
                Stok_BedenTanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_kalite_kontrol_tanimlari >= 1)
            {
                Stok_KaliteKontrol_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_kalkon >= 1)
            {
                Stok_Kalkon_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_kategorileri >= 1)
            {
                Stok_Kategorileri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_markalari >= 1)
            {
                Stok_Markalari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_model_tanimlari >= 1)
            {
                Stok_Model_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_muhasebe_gruplari >= 1)
            {
                Stok_Muhasebe_Gruplari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_renk_tanimlari >= 1)
            {
                Stok_Renk_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_reyonlari >= 1)
            {
                Stok_Reyon_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_sektorleri >= 1)
            {
                Stok_Sektorleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_ureticileri >= 1)
            {
                Stok_Üreticileri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_yilsezon_tanimlari >= 1)
            {
                Stok_YilSezonlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_satis_fiyat_liste_tanimlari >= 1)
            {
                Stok_FiyatListe_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_satis_fiyat_listeleri >= 1)
            {
                Stok_FiyatListeleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_isemirleri >= 1)
            {
                Isemirleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_personeller >= 1)
            {
                Personel_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_personel_bolgeleri >= 1)
            {
                Personel_Bolgeleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_asorti_tanimlari >= 1)
            {
                Asorti_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_depolar >= 1)
            {
                Depo_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_asorti_tanimlari >= 1)
            {
                Asorti_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_bankalar >= 1)
            {
                Banka_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_barkod_tanimlari >= 1)
            {
                Barkod_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_hizmet_hesaplari >= 1)
            {
                Hizmet_Hesaplari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_ithalat_muh_gruplari >= 1)
            {
                ithalat_muhasebe_gruplari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_kasalar >= 1)
            {
                Kasa_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_masraf_hesaplari >= 1)
            {
                Masraf_Hesaplari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_muh_fis_grubu_tanimlari >= 1)
            {
                Muhasebe_Fis_Grubu_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_muhasebe_hesap_gruplari >= 1)
            {
                Muhasebe_Hesap_Grubu_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_projeler >= 1)
            {
                Proje_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_son_kullanicilari >= 1)
            {
                Son_Kullanicilari_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_sorumluluk_merkezleri >= 1)
            {
                SorumlulukMerkezi_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_urunler >= 1)
            {
                Urun_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_urun_recete >= 1)
            {
                Urun_Receteleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_muhasebe_hesap_plani >= 1)
            {
                Muhasebe_Hesap_Plani_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_ihracat_dosyalari >= 1)
            {
                ihracat_dosyalari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_ithalat_dosyalari >= 1)
            {
                ithalat_dosyalari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_demirbaslar >= 1)
            {
                Demirbaslar_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_demirbas_gruplari >= 1)
            {
                Demirbas_Gruplari(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_demirbas_maliyil_tanimlari >= 1)
            {
                Demirbas_Mali_Yil_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_kredi_sozlesme >= 1)
            {
                kredi_sozlesmeleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_taksit_tanim >= 1)
            {
                kredi_taksit_tanim_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_donyayhiz >= 1)
            {
                donyay_tanim_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_parti_lot >= 1)
            {
                Partilot_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            AktarimEkraniDondur(false);
            SetControlText(lbl_süre2, DateTime.Now.ToLongTimeString());
            if (AktarimParametreleri.Parametre.akt_evrak_anlik == true ||
                AktarimParametreleri.Parametre.akt_kart_anlik == true)
            {
                aktarim_bittimi = true;
            }
            pbc_1.Text = "0";
            SetControlText(lbl_durum_1, "Kart aktarımı tamamlandı...");
        }
        void GunlukKartlarinAktarimi(DateTime tarih1, DateTime tarih2, AKTARIM_PARAMETRELERI par, bool anlik_mi)
        {
            pbc_1.Text = "0";
            SetControlText(lbl_durum_1, "");
            if (par.akt_urun_recete >= 2)
            {
                Urun_Receteleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_cari_hesaplar.Value >= 2)
            {
                Cari_Hesap_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_cari_hesap_adresleri >= 2)
            {
                Cari_Hesap_Adresleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_cari_hesap_bolgeler >= 2)
            {
                Cari_Hesap_Bolgeleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_cari_hesap_gruplari >= 2)
            {
                Cari_Hesap_Gruplari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_cari_hesap_yetkilileri >= 2)
            {
                Cari_Hesap_Yetkilileri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_cari_personel_tanimlari >= 2)
            {
                Cari_Personel_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stoklar >= 2)
            {
                Stok_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_serino_tanimlari >= 2)
            {
                Stok_SeriNo_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_sarf_receteleri >= 2)
            {
                Stok_Sarf_Receteleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_prim_tanimlari >= 2)
            {
                Stok_Prim_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_paket_tanimlari >= 2)
            {
                Stok_Paket_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_depo_detaylari >= 2)
            {
                Stok_Depo_Detayları_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_cari_iskonto_tanimlari >= 2)
            {
                Stok_Cari_iskonto_tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_alt_gruplari >= 2)
            {
                Stok_Alt_Gruplari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_ana_gruplari >= 2)
            {
                Stok_Ana_Gruplari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_ambalajlari >= 2)
            {
                Stok_Ambalajlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_anahammaddeleri >= 2)
            {
                Stok_Anahammaddeleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_beden_tanimlari >= 2)
            {
                Stok_BedenTanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_kalite_kontrol_tanimlari >= 2)
            {
                Stok_KaliteKontrol_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_kalkon >= 2)
            {
                Stok_Kalkon_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_kategorileri >= 2)
            {
                Stok_Kategorileri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_markalari >= 2)
            {
                Stok_Markalari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_model_tanimlari >= 2)
            {
                Stok_Model_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_muhasebe_gruplari >= 2)
            {
                Stok_Muhasebe_Gruplari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_renk_tanimlari >= 2)
            {
                Stok_Renk_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_reyonlari >= 2)
            {
                Stok_Reyon_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_sektorleri >= 2)
            {
                Stok_Sektorleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_ureticileri >= 2)
            {
                Stok_Üreticileri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_parti_lot >= 2)
            {
                Partilot_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_yilsezon_tanimlari >= 2)
            {
                Stok_YilSezonlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_satis_fiyat_liste_tanimlari >= 2)
            {
                Stok_FiyatListe_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_satis_fiyat_listeleri >= 2)
            {
                Stok_FiyatListeleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_isemirleri >= 2)
            {
                Isemirleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_personeller >= 2)
            {
                Personel_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_personel_bolgeleri >= 2)
            {
                Personel_Bolgeleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_asorti_tanimlari >= 2)
            {
                Asorti_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_depolar >= 2)
            {
                Depo_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_asorti_tanimlari >= 2)
            {
                Asorti_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_bankalar >= 2)
            {
                Banka_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_barkod_tanimlari >= 2)
            {
                Barkod_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_hizmet_hesaplari >= 2)
            {
                Hizmet_Hesaplari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_ithalat_muh_gruplari >= 2)
            {
                ithalat_muhasebe_gruplari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_kasalar >= 2)
            {
                Kasa_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_masraf_hesaplari >= 2)
            {
                Masraf_Hesaplari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_muh_fis_grubu_tanimlari >= 2)
            {
                Muhasebe_Fis_Grubu_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_muhasebe_hesap_gruplari >= 2)
            {
                Muhasebe_Hesap_Grubu_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_projeler >= 2)
            {
                Proje_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_son_kullanicilari >= 2)
            {
                Son_Kullanicilari_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_sorumluluk_merkezleri >= 2)
            {
                SorumlulukMerkezi_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_urunler >= 2)
            {
                Urun_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_muhasebe_hesap_plani >= 2)
            {
                Muhasebe_Hesap_Plani_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_ihracat_dosyalari >= 2)
            {
                ihracat_dosyalari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_ithalat_dosyalari >= 2)
            {
                ithalat_dosyalari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_demirbaslar >= 2)
            {
                Demirbaslar_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_demirbas_gruplari >= 2)
            {
                Demirbas_Gruplari(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_demirbas_maliyil_tanimlari >= 2)
            {
                Demirbas_Mali_Yil_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_kredi_sozlesme >= 2)
            {
                kredi_sozlesmeleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_taksit_tanim >= 2)
            {
                kredi_taksit_tanim_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            AktarimEkraniDondur(false);
            SetControlText(lbl_süre2, DateTime.Now.ToLongTimeString());
            if (AktarimParametreleri.Parametre.akt_evrak_anlik == true)
            {
                aktarim_bittimi = true;
            }
            pbc_1.Text = "0";
            SetControlText(lbl_durum_1, "Kart aktarımı tamamlandı...");
        }
        void AnlikKartlarinAktarimi(DateTime tarih1, DateTime tarih2, AKTARIM_PARAMETRELERI par, bool anlik_mi)
        {
            pbc_1.Text = "0";
            SetControlText(lbl_durum_1, "");
            if (par.akt_urun_recete == 1 || par.akt_urun_recete == 3)
            {
                Urun_Receteleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_cari_hesaplar.Value == 1 || par.akt_cari_hesaplar.Value == 3)
            {
                Cari_Hesap_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_cari_hesap_adresleri == 1 || par.akt_cari_hesap_adresleri == 3)
            {
                Cari_Hesap_Adresleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_cari_hesap_bolgeler == 1 || par.akt_cari_hesap_bolgeler == 3)
            {
                Cari_Hesap_Bolgeleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_cari_hesap_gruplari == 1 || par.akt_cari_hesap_gruplari == 3)
            {
                Cari_Hesap_Gruplari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_cari_hesap_yetkilileri == 1 || par.akt_cari_hesap_yetkilileri == 3)
            {
                Cari_Hesap_Yetkilileri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_cari_personel_tanimlari == 1 || par.akt_cari_personel_tanimlari == 3)
            {
                Cari_Personel_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stoklar == 1 || par.akt_stoklar == 3)
            {
                Stok_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_serino_tanimlari == 1 || par.akt_stok_serino_tanimlari == 3)
            {
                Stok_SeriNo_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_sarf_receteleri == 1 || par.akt_stok_sarf_receteleri == 3)
            {
                Stok_Sarf_Receteleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_prim_tanimlari == 1 || par.akt_stok_prim_tanimlari == 3)
            {
                Stok_Prim_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_paket_tanimlari == 1 || par.akt_stok_paket_tanimlari == 3)
            {
                Stok_Paket_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_depo_detaylari == 1 || par.akt_stok_depo_detaylari == 3)
            {
                Stok_Depo_Detayları_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_cari_iskonto_tanimlari == 1 || par.akt_stok_cari_iskonto_tanimlari == 3)
            {
                Stok_Cari_iskonto_tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_alt_gruplari == 1 || par.akt_stok_alt_gruplari == 3)
            {
                Stok_Alt_Gruplari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_ana_gruplari == 1 || par.akt_stok_ana_gruplari == 3)
            {
                Stok_Ana_Gruplari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_ambalajlari == 1 || par.akt_stok_ambalajlari == 3)
            {
                Stok_Ambalajlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_anahammaddeleri == 1 || par.akt_stok_anahammaddeleri == 3)
            {
                Stok_Anahammaddeleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_beden_tanimlari == 1 || par.akt_stok_beden_tanimlari == 3)
            {
                Stok_BedenTanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_kalite_kontrol_tanimlari == 1 || par.akt_stok_kalite_kontrol_tanimlari == 3)
            {
                Stok_KaliteKontrol_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_kalkon == 1 || par.akt_stok_kalkon == 3)
            {
                Stok_Kalkon_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_kategorileri == 1 || par.akt_stok_kategorileri == 3)
            {
                Stok_Kategorileri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_markalari == 1 || par.akt_stok_markalari == 3)
            {
                Stok_Markalari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_parti_lot == 1 || par.akt_parti_lot == 3)
            {
                Partilot_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_model_tanimlari == 1 || par.akt_stok_model_tanimlari == 3)
            {
                Stok_Model_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_muhasebe_gruplari == 1 || par.akt_stok_muhasebe_gruplari == 3)
            {
                Stok_Muhasebe_Gruplari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_renk_tanimlari == 1 || par.akt_stok_renk_tanimlari == 3)
            {
                Stok_Renk_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_reyonlari == 1 || par.akt_stok_reyonlari == 3)
            {
                Stok_Reyon_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_sektorleri == 1 || par.akt_stok_sektorleri == 3)
            {
                Stok_Sektorleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_ureticileri == 1 || par.akt_stok_ureticileri == 3)
            {
                Stok_Üreticileri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_yilsezon_tanimlari == 1 || par.akt_stok_yilsezon_tanimlari == 3)
            {
                Stok_YilSezonlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_satis_fiyat_liste_tanimlari == 1 || par.akt_stok_satis_fiyat_liste_tanimlari == 3)
            {
                Stok_FiyatListe_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_stok_satis_fiyat_listeleri == 1 || par.akt_stok_satis_fiyat_listeleri == 3)
            {
                Stok_FiyatListeleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_isemirleri == 1 || par.akt_isemirleri == 3)
            {
                Isemirleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_personeller == 1 || par.akt_personeller == 3)
            {
                Personel_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_personel_bolgeleri == 1 || par.akt_personel_bolgeleri == 3)
            {
                Personel_Bolgeleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_asorti_tanimlari == 1 || par.akt_asorti_tanimlari == 3)
            {
                Asorti_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_depolar == 1 || par.akt_depolar == 3)
            {
                Depo_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_asorti_tanimlari == 1 || par.akt_asorti_tanimlari == 3)
            {
                Asorti_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_bankalar == 1 || par.akt_bankalar == 3)
            {
                Banka_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_barkod_tanimlari == 1 || par.akt_barkod_tanimlari == 3)
            {
                Barkod_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_hizmet_hesaplari == 1 || par.akt_hizmet_hesaplari == 3)
            {
                Hizmet_Hesaplari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_ithalat_muh_gruplari == 1 || par.akt_ithalat_muh_gruplari == 3)
            {
                ithalat_muhasebe_gruplari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_kasalar == 1 || par.akt_kasalar == 3)
            {
                Kasa_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_masraf_hesaplari == 1 || par.akt_masraf_hesaplari == 3)
            {
                Masraf_Hesaplari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_muh_fis_grubu_tanimlari == 1 || par.akt_muh_fis_grubu_tanimlari == 3)
            {
                Muhasebe_Fis_Grubu_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_muhasebe_hesap_gruplari == 1 || par.akt_muhasebe_hesap_gruplari == 3)
            {
                Muhasebe_Hesap_Grubu_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_projeler == 1 || par.akt_projeler == 3)
            {
                Proje_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_son_kullanicilari == 1 || par.akt_son_kullanicilari == 3)
            {
                Son_Kullanicilari_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_sorumluluk_merkezleri == 1 || par.akt_sorumluluk_merkezleri == 3)
            {
                SorumlulukMerkezi_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_urunler == 1 || par.akt_urunler == 3)
            {
                Urun_Kartlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_muhasebe_hesap_plani == 1 || par.akt_muhasebe_hesap_plani == 3)
            {
                Muhasebe_Hesap_Plani_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_ihracat_dosyalari == 1 || par.akt_ihracat_dosyalari == 3)
            {
                ihracat_dosyalari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_ithalat_dosyalari == 1 || par.akt_ithalat_dosyalari == 3)
            {
                ithalat_dosyalari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_demirbaslar == 1 || par.akt_demirbaslar == 3)
            {
                Demirbaslar_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_demirbas_gruplari == 1 || par.akt_demirbas_gruplari == 3)
            {
                Demirbas_Gruplari(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_demirbas_maliyil_tanimlari == 1 || par.akt_demirbas_maliyil_tanimlari == 3)
            {
                Demirbas_Mali_Yil_Tanimlari_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_kredi_sozlesme == 1 || par.akt_kredi_sozlesme == 3)
            {
                kredi_sozlesmeleri_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            if (par.akt_taksit_tanim >= 2)
            {
                kredi_taksit_tanim_Aktarimi(tarih1, tarih2, anlik_mi, pbc_1, lbl_durum_1);
            }
            AktarimEkraniDondur(false);
            SetControlText(lbl_süre2, DateTime.Now.ToLongTimeString());
            if (AktarimParametreleri.Parametre.akt_evrak_anlik == true)
            {
                aktarim_bittimi = true;
            }
            pbc_1.Text = "0";
            SetControlText(lbl_durum_1, "Kart aktarımı tamamlandı...");
        }
        #region KART AKTARIMLARI
        void Stok_SeriNo_Tanimlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_SERINO_TANIMLARI> ls_stok_serino = new List<STOK_SERINO_TANIMLARI>();
            SetControlText(lbl_durum, "Stok seri no tanımları yükleniyor...");
            if (anlik_mi == true)
                ls_stok_serino = KartAktarimlari.Stok_SeriNo_Tanimlarini_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls_stok_serino = KartAktarimlari.Stok_SeriNo_Tanimlarini_Yukle(DatabaseFacade.ConnectionString());
            if (ls_stok_serino.Count > 0)
            {
                SetControlText(lbl_durum, "Stok serino tanımları  Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls_stok_serino.Count;
                Int32 pb_i = 0;
                foreach (STOK_SERINO_TANIMLARI sst in ls_stok_serino)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
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
                SetControlText(lbldurum, "Stok serino tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Cari_Hesap_Kartlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<CARI_HESAPLAR> ls = new List<CARI_HESAPLAR>();
            SetControlText(lbl_durum, "Cari Hesap kartları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Cari_Hesaplari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Cari_Hesaplari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Cari Hesap Kartları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (CARI_HESAPLAR sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
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
                SetControlText(lbldurum, "Cari Hesap Kartları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Cari_Hesap_Adresleri_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<CARI_HESAP_ADRESLERI> ls = new List<CARI_HESAP_ADRESLERI>();
            SetControlText(lbl_durum, "Cari Hesap kartları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Cari_Hesap_Adresleri_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Cari_Hesap_Adresleri_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Cari Hesap Adresleri Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (CARI_HESAP_ADRESLERI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    CARI_HESAP_ADRESLERI sst_1 = sst;
                    CARI_HESAP_ADRESLERI sst_2;
                    sst_2 = KartAktarimlari.Cari_Hesap_Adresleri_EvrakDetayGetir(sst_1.adr_cari_kod, sst.adr_adres_no.Value, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.adr_Guid = sst_2.adr_Guid;
                        //sst_1.adr_RECid_RECno = sst_2.adr_RECid_RECno;
                        sst_2 = KartAktarimlari.Cari_Hesap_Adresleri_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.adr_Guid = Guid.Empty;
                        //sst_1.adr_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Cari_Hesap_Adresleri_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbl_durum, "Cari Hesap Adresleri aktarıldı...");
            }
            else
                SetControlText(lbl_durum, "");
        }
        void Cari_Hesap_Bolgeleri_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<CARI_HESAP_BOLGELERI> ls = new List<CARI_HESAP_BOLGELERI>();
            SetControlText(lbl_durum, "Cari Hesap bölgeleri yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Cari_Hesap_Bolgeleri_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Cari_Hesap_Bolgeleri_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Cari Hesap bölgeleri Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (CARI_HESAP_BOLGELERI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    CARI_HESAP_BOLGELERI sst_1 = sst;
                    CARI_HESAP_BOLGELERI sst_2;
                    sst_2 = KartAktarimlari.Cari_Hesap_Bolgeleri_EvrakDetayGetir(sst_1.bol_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.bol_Guid = sst_2.bol_Guid;
                        //sst_1.bol_RECid_RECno = sst_2.bol_RECid_RECno;
                        sst_2 = KartAktarimlari.Cari_Hesap_Bolgeleri_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.bol_Guid = Guid.Empty;
                        //sst_1.bol_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Cari_Hesap_Bolgeleri_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Cari Hesap Bölgeleri aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Cari_Hesap_Gruplari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<CARI_HESAP_GRUPLARI> ls = new List<CARI_HESAP_GRUPLARI>();
            SetControlText(lbl_durum, "Cari Hesap grupları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Cari_Hesap_Gruplari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Cari_Hesap_Gruplari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Cari Hesap grupları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (CARI_HESAP_GRUPLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    CARI_HESAP_GRUPLARI sst_1 = sst;
                    CARI_HESAP_GRUPLARI sst_2;
                    sst_2 = KartAktarimlari.Cari_Hesap_Gruplari_EvrakDetayGetir(sst_1.crg_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.crg_Guid = sst_2.crg_Guid;
                        //sst_1.crg_RECid_RECno = sst_2.crg_RECid_RECno;
                        sst_2 = KartAktarimlari.Cari_Hesap_Gruplari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.crg_Guid = Guid.Empty;
                        //sst_1.crg_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Cari_Hesap_Gruplari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Cari Hesap grupları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Cari_Hesap_Yetkilileri_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<CARI_HESAP_YETKILILERI> ls = new List<CARI_HESAP_YETKILILERI>();
            SetControlText(lbl_durum, "Cari Hesap yetkilileri yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Cari_Hesap_Yetkilileri_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Cari_Hesap_Yetkilileri_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Cari Hesap yetkilileri Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (CARI_HESAP_YETKILILERI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    CARI_HESAP_YETKILILERI sst_1 = sst;
                    CARI_HESAP_YETKILILERI sst_2;
                    sst_2 = KartAktarimlari.Cari_Hesap_Yetkilileri_EvrakDetayGetir(sst_1.mye_cari_kod, sst_1.mye_adres_no.Value, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.mye_Guid = sst_2.mye_Guid;
                        //sst_1.mye_RECid_RECno = sst_2.mye_RECid_RECno;
                        sst_2 = KartAktarimlari.Cari_Hesap_Yetkilileri_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.mye_Guid = Guid.Empty;
                        //sst_1.mye_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Cari_Hesap_Yetkilileri_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Cari Hesap yetkilileri aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Cari_Personel_Tanimlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<CARI_PERSONEL_TANIMLARI> ls = new List<CARI_PERSONEL_TANIMLARI>();
            SetControlText(lbl_durum, "Cari Personel tanımları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Cari_Personel_Tanimlari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Cari_Personel_Tanimlari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Cari Personel tanımları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (CARI_PERSONEL_TANIMLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    CARI_PERSONEL_TANIMLARI sst_1 = sst;
                    CARI_PERSONEL_TANIMLARI sst_2;
                    sst_2 = KartAktarimlari.Cari_Personel_Tanimlari_EvrakDetayGetir(sst_1.cari_per_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.cari_per_Guid = sst_2.cari_per_Guid;
                        //sst_1.cari_per_RECid_RECno = sst_2.cari_per_RECid_RECno;
                        sst_2 = KartAktarimlari.Cari_Personel_Tanimlari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.cari_per_Guid = Guid.Empty;
                        //sst_1.cari_per_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Cari_Personel_Tanimlari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Cari Personel tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_Kartlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOKLAR> ls = new List<STOKLAR>();
            SetControlText(lbl_durum, "Stok kartları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stoklari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stoklari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok kartları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOKLAR sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOKLAR sst_1 = sst;
                    STOKLAR sst_2;
                    sst_2 = KartAktarimlari.Stoklar_EvrakDetayGetir(sst_1.sto_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.sto_Guid = sst_2.sto_Guid;
                        //sst_1.sto_RECid_RECno = sst_2.sto_RECid_RECno;
                        sst_2 = KartAktarimlari.Stoklari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.sto_Guid = Guid.Empty;
                        //sst_1.sto_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Stoklari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok kartları tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_Sarf_Receteleri_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_SARF_RECETELERI> ls = new List<STOK_SARF_RECETELERI>();
            SetControlText(lbl_durum, "Stok sarf receteleri yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stok_Sarf_Receteleri_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stok_Sarf_Receteleri_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok sarf receteleri Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_SARF_RECETELERI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_SARF_RECETELERI sst_1 = sst;
                    STOK_SARF_RECETELERI sst_2;
                    sst_2 = KartAktarimlari.Stok_Sarf_Receteleri_EvrakDetayGetir(sst_1.sr_anakod, sst_1.sr_satirno.Value, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.sr_Guid = sst_2.sr_Guid;
                        //sst_1.sr_sr_id_sr_no = sst_2.sr_sr_id_sr_no;
                        sst_2 = KartAktarimlari.Stok_Sarf_Receteleri_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.sr_Guid = Guid.Empty;
                        //sst_1.sr_sr_id_sr_no = -1;
                        sst_2 = KartAktarimlari.Stok_Sarf_Receteleri_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok sarf receteleri tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_Prim_Tanimlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_PRIM_TANIMLARI> ls = new List<STOK_PRIM_TANIMLARI>();
            SetControlText(lbl_durum, "Stok prim tanımları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stok_Prim_Tanimlari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stok_Prim_Tanimlari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok prim tanımları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_PRIM_TANIMLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_PRIM_TANIMLARI sst_1 = sst;
                    STOK_PRIM_TANIMLARI sst_2;
                    sst_2 = KartAktarimlari.Stok_Prim_Tanimlari_EvrakDetayGetir(sst_1.prim_kod, sst_1.prim_satirno.Value, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.prim_Guid = sst_2.prim_Guid;
                        //sst_1.prim_RECid_RECno = sst_2.prim_RECid_RECno;
                        sst_2 = KartAktarimlari.Stok_Prim_Tanimlari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.prim_Guid = Guid.Empty;
                        //sst_1.prim_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Stok_Prim_Tanimlari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok prim tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_Paket_Tanimlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_PAKET_TANIMLARI> ls = new List<STOK_PAKET_TANIMLARI>();
            SetControlText(lbl_durum, "Stok paket tanımları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stok_Paket_Tanimlari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stok_Paket_Tanimlari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok paket tanımları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_PAKET_TANIMLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_PAKET_TANIMLARI sst_1 = sst;
                    STOK_PAKET_TANIMLARI sst_2;
                    sst_2 = KartAktarimlari.Stok_Paket_Tanimlari_EvrakDetayGetir(sst_1.pak_kod, sst_1.pak_satirno.Value, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.pak_Guid = sst_2.pak_Guid;
                        //sst_1.pak_RECid_RECno = sst_2.pak_RECid_RECno;
                        sst_2 = KartAktarimlari.Stok_Paket_Tanimlari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.pak_Guid = Guid.Empty;
                        //sst_1.pak_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Stok_Paket_Tanimlari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok paket tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_Depo_Detayları_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_DEPO_DETAYLARI> ls = new List<STOK_DEPO_DETAYLARI>();
            SetControlText(lbl_durum, "Stok depo detayları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stok_Depo_Detaylari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stok_Depo_Detaylari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok depo detayları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_DEPO_DETAYLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_DEPO_DETAYLARI sst_1 = sst;
                    STOK_DEPO_DETAYLARI sst_2;
                    sst_2 = KartAktarimlari.Stok_Depo_Detaylari_EvrakDetayGetir(sst_1.sdp_depo_kod, sst_1.sdp_depo_no.Value, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.sdp_Guid = sst_2.sdp_Guid;
                        //sst_1.sdp_RECid_RECno = sst_2.sdp_RECid_RECno;
                        sst_2 = KartAktarimlari.Stok_Depo_Detaylari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.sdp_Guid = Guid.Empty;
                        //sst_1.sdp_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Stok_Depo_Detaylari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok depo detayları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_Cari_iskonto_tanimlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_CARI_ISKONTO_TANIMLARI> ls = new List<STOK_CARI_ISKONTO_TANIMLARI>();
            SetControlText(lbl_durum, "Stok cari iskonto tanımları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stok_Cari_Iskonto_Tanimlari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stok_Cari_Iskonto_Tanimlari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok cari iskonto tanımları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_CARI_ISKONTO_TANIMLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_CARI_ISKONTO_TANIMLARI sst_1 = sst;
                    STOK_CARI_ISKONTO_TANIMLARI sst_2;
                    sst_2 = KartAktarimlari.Stok_Cari_Iskonto_Tanimlari_EvrakDetayGetir(sst_1.isk_stok_kod, sst_1.isk_cari_kod, sst_1.isk_uygulama_odeme_plani.Value, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.isk_Guid = sst_2.isk_Guid;
                        //sst_1.isk_RECid_RECno = sst_2.isk_RECid_RECno;
                        sst_2 = KartAktarimlari.Stok_Cari_Iskonto_Tanimlari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.isk_Guid = Guid.Empty;
                        //sst_1.isk_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Stok_Cari_Iskonto_Tanimlari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok cari iskonto tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_Alt_Gruplari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_ALT_GRUPLARI> ls = new List<STOK_ALT_GRUPLARI>();
            SetControlText(lbl_durum, "Stok alt grupları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stok_Alt_Gruplari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stok_Alt_Gruplari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok alt grupları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_ALT_GRUPLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_ALT_GRUPLARI sst_1 = sst;
                    STOK_ALT_GRUPLARI sst_2;
                    sst_2 = KartAktarimlari.Stok_Alt_Gruplari_EvrakDetayGetir(sst_1.sta_kod, sst_1.sta_ana_grup_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.sta_Guid = sst_2.sta_Guid;
                        //sst_1.sta_RECid_RECno = sst_2.sta_RECid_RECno;
                        sst_2 = KartAktarimlari.Stok_Alt_Gruplari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.sta_Guid = Guid.Empty;
                        //sst_1.sta_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Stok_Alt_Gruplari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok alt grupları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_Ana_Gruplari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_ANA_GRUPLARI> ls = new List<STOK_ANA_GRUPLARI>();
            SetControlText(lbl_durum, "Stok ana grupları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stok_Ana_Gruplari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stok_Ana_Gruplari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok ana grupları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_ANA_GRUPLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_ANA_GRUPLARI sst_1 = sst;
                    STOK_ANA_GRUPLARI sst_2;
                    sst_2 = KartAktarimlari.Stok_Ana_Gruplari_EvrakDetayGetir(sst_1.san_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.san_Guid = sst_2.san_Guid;
                        //sst_1.san_RECid_RECno = sst_2.san_RECid_RECno;
                        sst_2 = KartAktarimlari.Stok_Ana_Gruplari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.san_Guid = Guid.Empty;
                        //sst_1.san_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Stok_Ana_Gruplari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok ana grupları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_Ambalajlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_AMBALAJLARI> ls = new List<STOK_AMBALAJLARI>();
            SetControlText(lbl_durum, "Stok ambalajları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stok_Ambalajlari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stok_Ambalajlari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok ambalajları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_AMBALAJLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_AMBALAJLARI sst_1 = sst;
                    STOK_AMBALAJLARI sst_2;
                    sst_2 = KartAktarimlari.Stok_Ambalajlari_EvrakDetayGetir(sst_1.amb_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.amb_Guid = sst_2.amb_Guid;
                        //sst_1.amb_RECid_RECno = sst_2.amb_RECid_RECno;
                        sst_2 = KartAktarimlari.Stok_Ambalajlari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.amb_Guid = Guid.Empty;
                        //sst_1.amb_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Stok_Ambalajlari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok ambalajları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_Anahammaddeleri_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_ANAHAMMADDELERI> ls = new List<STOK_ANAHAMMADDELERI>();
            SetControlText(lbl_durum, "Stok ana hammaddeleri yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stok_Anahammaddeleri_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stok_Anahammaddeleri_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok ana hammaddeleri Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_ANAHAMMADDELERI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_ANAHAMMADDELERI sst_1 = sst;
                    STOK_ANAHAMMADDELERI sst_2;
                    sst_2 = KartAktarimlari.Stok_Anahammaddeleri_EvrakDetayGetir(sst_1.ahm_kodu, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.ahm_Guid = sst_2.ahm_Guid;
                        //sst_1.ahm_RECid_RECno = sst_2.ahm_RECid_RECno;
                        sst_2 = KartAktarimlari.Stok_Anahammaddeleri_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.ahm_Guid = Guid.Empty;
                        //sst_1.ahm_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Stok_Anahammaddeleri_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok ana hammaddeleri aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_BedenTanimlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_BEDEN_TANIMLARI> ls = new List<STOK_BEDEN_TANIMLARI>();
            SetControlText(lbl_durum, "Stok beden tanımları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stok_Beden_Tanimlari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stok_Beden_Tanimlari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok beden tanımları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_BEDEN_TANIMLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_BEDEN_TANIMLARI sst_1 = sst;
                    STOK_BEDEN_TANIMLARI sst_2;
                    sst_2 = KartAktarimlari.Stok_Beden_Tanimlari_EvrakDetayGetir(sst_1.bdn_kodu, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.bdn_Guid = sst_2.bdn_Guid;
                        //sst_1.bdn_RECid_RECno = sst_2.bdn_RECid_RECno;
                        sst_2 = KartAktarimlari.Stok_Beden_Tanimlari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.bdn_Guid = Guid.Empty;
                        //sst_1.bdn_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Stok_Beden_Tanimlari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok beden tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_KaliteKontrol_Tanimlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_KALITE_KONTROL_TANIMLARI> ls = new List<STOK_KALITE_KONTROL_TANIMLARI>();
            SetControlText(lbl_durum, "Stok kalite kontrol tanımları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stok_Kalite_Kontrol_Tanimlari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stok_Kalite_Kontrol_Tanimlari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok kalite kontrol tanımları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_KALITE_KONTROL_TANIMLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_KALITE_KONTROL_TANIMLARI sst_1 = sst;
                    STOK_KALITE_KONTROL_TANIMLARI sst_2;
                    sst_2 = KartAktarimlari.Stok_Kalite_Kontrol_Tanimlari_EvrakDetayGetir(sst_1.KKon_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.KKon_Guid = sst_2.KKon_Guid;
                        //sst_1.KKon_RECid_RECno = sst_2.KKon_RECid_RECno;
                        sst_2 = KartAktarimlari.Stok_Kalite_Kontrol_Tanimlari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.KKon_Guid = Guid.Empty;
                        //sst_1.KKon_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Stok_Kalite_Kontrol_Tanimlari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok kalite kontrol tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_Kalkon_Tanimlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_KALKON> ls = new List<STOK_KALKON>();
            SetControlText(lbl_durum, "Stok kalkon tanımları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stok_Kalkon_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stok_Kalkon_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok kalkon tanımları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_KALKON sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_KALKON sst_1 = sst;
                    STOK_KALKON sst_2;
                    sst_2 = KartAktarimlari.Stok_Kalkon_EvrakDetayGetir(sst_1.skk_kodu, sst_1.skk_tipi, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.skk_Guid = sst_2.skk_Guid;
                        //sst_1.skk_RECid_RECno = sst_2.skk_RECid_RECno;
                        sst_2 = KartAktarimlari.Stok_Kalkon_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.skk_Guid = Guid.Empty;
                        //sst_1.skk_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Stok_Kalkon_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok kalkon tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_Kategorileri_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_KATEGORILERI> ls = new List<STOK_KATEGORILERI>();
            SetControlText(lbl_durum, "Stok kategori tanımları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stok_Kategorileri_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stok_Kategorileri_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok kategori tanımları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_KATEGORILERI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_KATEGORILERI sst_1 = sst;
                    STOK_KATEGORILERI sst_2;
                    sst_2 = KartAktarimlari.Stok_Kategorileri_EvrakDetayGetir(sst_1.ktg_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.ktg_Guid = sst_2.ktg_Guid;
                        //sst_1.ktg_RECid_RECno = sst_2.ktg_RECid_RECno;
                        sst_2 = KartAktarimlari.Stok_Kategorileri_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.ktg_Guid = Guid.Empty;
                        //sst_1.ktg_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Stok_Kategorileri_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok kategori tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_Markalari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_MARKALARI> ls = new List<STOK_MARKALARI>();
            SetControlText(lbl_durum, "Stok marka tanımları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stok_Markalari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stok_Markalari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok marka tanımları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_MARKALARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_MARKALARI sst_1 = sst;
                    STOK_MARKALARI sst_2;
                    sst_2 = KartAktarimlari.Stok_Markalari_EvrakDetayGetir(sst_1.mrk_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.mrk_Guid = sst_2.mrk_Guid;
                        //sst_1.mrk_RECid_RECno = sst_2.mrk_RECid_RECno;
                        sst_2 = KartAktarimlari.Stok_Markalari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.mrk_Guid = Guid.Empty;
                        //sst_1.mrk_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Stok_Markalari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok marka tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_Model_Tanimlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_MODEL_TANIMLARI> ls = new List<STOK_MODEL_TANIMLARI>();
            SetControlText(lbl_durum, "Stok model tanımları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stok_Model_Tanimlari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stok_Model_Tanimlari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok model tanımları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_MODEL_TANIMLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_MODEL_TANIMLARI sst_1 = sst;
                    STOK_MODEL_TANIMLARI sst_2;
                    sst_2 = KartAktarimlari.Stok_Model_Tanimlari_EvrakDetayGetir(sst_1.mdl_kodu, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.mdl_Guid = sst_2.mdl_Guid;
                        //sst_1.mdl_RECid_RECno = sst_2.mdl_RECid_RECno;
                        sst_2 = KartAktarimlari.Stok_Model_Tanimlari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.mdl_Guid = Guid.Empty;
                        //sst_1.mdl_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Stok_Model_Tanimlari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok model tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_Muhasebe_Gruplari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_MUHASEBE_GRUPLARI> ls = new List<STOK_MUHASEBE_GRUPLARI>();
            SetControlText(lbl_durum, "Stok muhasebe grup kodları tanımları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stok_Muhasebe_Gruplari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stok_Muhasebe_Gruplari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok muhasebe grup kodları tanımları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_MUHASEBE_GRUPLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_MUHASEBE_GRUPLARI sst_1 = sst;
                    STOK_MUHASEBE_GRUPLARI sst_2;
                    sst_2 = KartAktarimlari.Stok_Muhasebe_Gruplari_EvrakDetayGetir(sst_1.stmuh_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.stmuh_Guid = sst_2.stmuh_Guid;
                        //sst_1.stmuh_RECid_RECno = sst_2.stmuh_RECid_RECno;
                        sst_2 = KartAktarimlari.Stok_Muhasebe_Gruplari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.stmuh_Guid = Guid.Empty;
                        //sst_1.stmuh_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Stok_Muhasebe_Gruplari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok muhasebe grup kodları tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_Renk_Tanimlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_RENK_TANIMLARI> ls = new List<STOK_RENK_TANIMLARI>();
            SetControlText(lbl_durum, "Stok renk tanımları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stok_Renk_Tanimlari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stok_Renk_Tanimlari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok renk tanımları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_RENK_TANIMLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_RENK_TANIMLARI sst_1 = sst;
                    STOK_RENK_TANIMLARI sst_2;
                    sst_2 = KartAktarimlari.Stok_Renk_Tanimlari_EvrakDetayGetir(sst_1.rnk_kodu, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.rnk_Guid = sst_2.rnk_Guid;
                        //sst_1.rnk_RECid_RECno = sst_2.rnk_RECid_RECno;
                        sst_2 = KartAktarimlari.Stok_Renk_Tanimlari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.rnk_Guid = Guid.Empty;
                        //sst_1.rnk_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Stok_Renk_Tanimlari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok renk_tanimlari tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_Reyon_Tanimlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_REYONLARI> ls = new List<STOK_REYONLARI>();
            SetControlText(lbl_durum, "Stok reyon tanımları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stok_Reyonlari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stok_Reyonlari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok reyon tanımları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_REYONLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_REYONLARI sst_1 = sst;
                    STOK_REYONLARI sst_2;
                    sst_2 = KartAktarimlari.Stok_Reyonlari_EvrakDetayGetir(sst_1.ryn_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.ryn_Guid = sst_2.ryn_Guid;
                        //sst_1.ryn_RECid_RECno = sst_2.ryn_RECid_RECno;
                        sst_2 = KartAktarimlari.Stok_Reyonlari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.ryn_Guid = Guid.Empty;
                        //sst_1.ryn_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Stok_Reyonlari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok reyon tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_Sektorleri_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_SEKTORLERI> ls = new List<STOK_SEKTORLERI>();
            SetControlText(lbl_durum, "Stok sektör tanımları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stok_Sektorleri_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stok_Sektorleri_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok sektör tanımları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_SEKTORLERI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_SEKTORLERI sst_1 = sst;
                    STOK_SEKTORLERI sst_2;
                    sst_2 = KartAktarimlari.Stok_Sektorleri_EvrakDetayGetir(sst_1.sktr_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.sktr_Guid = sst_2.sktr_Guid;
                        //sst_1.sktr_RECid_RECno = sst_2.sktr_RECid_RECno;
                        sst_2 = KartAktarimlari.Stok_Sektorleri_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.sktr_Guid = Guid.Empty;
                        //sst_1.sktr_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Stok_Sektorleri_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok sektör tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_Üreticileri_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_URETICILERI> ls = new List<STOK_URETICILERI>();
            SetControlText(lbl_durum, "Stok üretici tanımları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stok_Ureticileri_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stok_Ureticileri_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok üretici tanımları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_URETICILERI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_URETICILERI sst_1 = sst;
                    STOK_URETICILERI sst_2;
                    sst_2 = KartAktarimlari.Stok_Ureticileri_EvrakDetayGetir(sst_1.urt_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.urt_Guid = sst_2.urt_Guid;
                        //sst_1.urt_RECid_RECno = sst_2.urt_RECid_RECno;
                        sst_2 = KartAktarimlari.Stok_Ureticileri_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.urt_Guid = Guid.Empty;
                        //sst_1.urt_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Stok_Ureticileri_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok üretici tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_YilSezonlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_YILSEZON_TANIMLARI> ls = new List<STOK_YILSEZON_TANIMLARI>();
            SetControlText(lbl_durum, "Stok yıl sezon tanımları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Stok_Yilsezon_Tanimlari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Stok_Yilsezon_Tanimlari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok yıl sezon tanımları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_YILSEZON_TANIMLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_YILSEZON_TANIMLARI sst_1 = sst;
                    STOK_YILSEZON_TANIMLARI sst_2;
                    sst_2 = KartAktarimlari.Stok_Yilsezon_Tanimlari_EvrakDetayGetir(sst_1.ysn_kodu, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.ysn_Guid = sst_2.ysn_Guid;
                        //sst_1.ysn_RECid_RECno = sst_2.ysn_RECid_RECno;
                        sst_2 = KartAktarimlari.Stok_Yilsezon_Tanimlari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.ysn_Guid = Guid.Empty;
                        //sst_1.ysn_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Stok_Yilsezon_Tanimlari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok yıl sezon tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_FiyatListe_Tanimlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_SATIS_FIYAT_LISTE_TANIMLARI> ls = new List<STOK_SATIS_FIYAT_LISTE_TANIMLARI>();
            SetControlText(lbl_durum, "Stok satış fiyat liste tanımları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.STOK_SATIS_FIYAT_LISTE_TANIMLARIi_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.STOK_SATIS_FIYAT_LISTE_TANIMLARIi_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok satış fiyat liste tanımları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_SATIS_FIYAT_LISTE_TANIMLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_SATIS_FIYAT_LISTE_TANIMLARI sst_1 = sst;
                    STOK_SATIS_FIYAT_LISTE_TANIMLARI sst_2;
                    sst_2 = KartAktarimlari.STOK_SATIS_FIYAT_LISTE_TANIMLARI_EvrakDetayGetir(sst_1.sfl_sirano.Value, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.sfl_Guid = sst_2.sfl_Guid;
                        //sst_1.sfl_RECid_RECno = sst_2.sfl_RECid_RECno;
                        sst_2 = KartAktarimlari.STOK_SATIS_FIYAT_LISTE_TANIMLARIi_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.sfl_Guid = Guid.Empty;
                        //sst_1.sfl_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.STOK_SATIS_FIYAT_LISTE_TANIMLARIi_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok satış fiyat liste tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Stok_FiyatListeleri_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<STOK_SATIS_FIYAT_LISTELERI> ls = new List<STOK_SATIS_FIYAT_LISTELERI>();
            SetControlText(lbl_durum, "Stok satış fiyat listeleri yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.STOK_SATIS_FIYAT_LISTELERIi_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.STOK_SATIS_FIYAT_LISTELERIi_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Stok satış fiyat listeleri Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (STOK_SATIS_FIYAT_LISTELERI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    STOK_SATIS_FIYAT_LISTELERI sst_1 = sst;
                    STOK_SATIS_FIYAT_LISTELERI sst_2;
                    sst_2 = KartAktarimlari.STOK_SATIS_FIYAT_LISTELERI_EvrakDetayGetir(sst_1.sfiyat_stokkod, sst_1.sfiyat_listesirano.Value, sst_1.sfiyat_deposirano.Value, sst_1.sfiyat_odemeplan.Value, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.sfiyat_Guid = sst_2.sfiyat_Guid;
                        //sst_1.sfiyat_RECid_RECno = sst_2.sfiyat_RECid_RECno;
                        sst_2 = KartAktarimlari.STOK_SATIS_FIYAT_LISTELERIi_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.sfiyat_Guid = Guid.Empty;
                        //sst_1.sfiyat_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.STOK_SATIS_FIYAT_LISTELERIi_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Stok satış fiyat listeleri aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Isemirleri_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<ISEMIRLERI> ls = new List<ISEMIRLERI>();
            SetControlText(lbl_durum, "İş emirleri yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.ISEMIRLERI_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.ISEMIRLERI_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "İş emirleri Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (ISEMIRLERI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    ISEMIRLERI sst_1 = sst;
                    ISEMIRLERI sst_2;
                    sst_2 = KartAktarimlari.ISEMIRLERI_EvrakDetayGetir(sst_1.is_Kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.is_Guid = sst_2.is_Guid;
                        //sst_1.sfiyat_RECid_RECno = sst_2.sfiyat_RECid_RECno;
                        sst_2 = KartAktarimlari.ISEMIRLERI_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.is_Guid = Guid.Empty;
                        //sst_1.sfiyat_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.ISEMIRLERI_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "İş emirleri aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Personel_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<PERSONELLER> ls = new List<PERSONELLER>();
            SetControlText(lbl_durum, "Personeller yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Personelleri_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Personelleri_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Personeller Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (PERSONELLER sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    PERSONELLER sst_1 = sst;
                    PERSONELLER sst_2;
                    sst_2 = KartAktarimlari.Personeller_EvrakDetayGetir(sst_1.per_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.per_Guid = sst_2.per_Guid;
                        //sst_1.per_RECid_RECno = sst_2.per_RECid_RECno;
                        sst_2 = KartAktarimlari.Personeller_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.per_Guid = Guid.Empty;
                        //sst_1.per_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Personeller_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Personeller aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Personel_Bolgeleri_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<PERSONEL_BOLGELERI> ls = new List<PERSONEL_BOLGELERI>();
            SetControlText(lbl_durum, "Personel bölgeleri yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Personel_Bolgeleri_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Personel_Bolgeleri_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Personel bölgeleri Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (PERSONEL_BOLGELERI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    PERSONEL_BOLGELERI sst_1 = sst;
                    PERSONEL_BOLGELERI sst_2;
                    sst_2 = KartAktarimlari.Personel_Bolgeleri_EvrakDetayGetir(sst_1.pbl_bolge_kodu, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.pbl_Guid = sst_2.pbl_Guid;
                        //sst_1.pbl_RECid_RECno = sst_2.pbl_RECid_RECno;
                        sst_2 = KartAktarimlari.Personel_Bolgeleri_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.pbl_Guid = Guid.Empty;
                        //sst_1.pbl_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Personel_Bolgeleri_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Personel bölgeleri aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Personel_Muhasebe_Gruplari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<PERSONEL_MUHASEBE_GRUPLARI> ls = new List<PERSONEL_MUHASEBE_GRUPLARI>();
            SetControlText(lbl_durum, "Personel muhasebe grupları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Personel_Muhasebe_Gruplari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Personel_Muhasebe_Gruplari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Personel muhasebe grupları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (PERSONEL_MUHASEBE_GRUPLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    PERSONEL_MUHASEBE_GRUPLARI sst_1 = sst;
                    PERSONEL_MUHASEBE_GRUPLARI sst_2;
                    sst_2 = KartAktarimlari.Personel_Muhasebe_Gruplari_EvrakDetayGetir(sst_1.pmg_kodu, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.pmg_Guid = sst_2.pmg_Guid;
                        //sst_1.pmg_RECid_RECno = sst_2.pmg_RECid_RECno;
                        sst_2 = KartAktarimlari.Personel_Muhasebe_Gruplari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.pmg_Guid = Guid.Empty;
                        //sst_1.pmg_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Personel_Muhasebe_Gruplari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Personel muhasebe grupları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Depo_Kartlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<DEPOLAR> ls = new List<DEPOLAR>();
            SetControlText(lbl_durum, "Depo kartları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Depolari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Depolari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Depo kartları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (DEPOLAR sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    DEPOLAR sst_1 = sst;
                    DEPOLAR sst_2;
                    sst_2 = KartAktarimlari.Depolar_EvrakDetayGetir(sst_1.dep_no.Value, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.dep_Guid = sst_2.dep_Guid;
                        //sst_1.dep_RECid_RECno = sst_2.dep_RECid_RECno;
                        sst_2 = KartAktarimlari.Depolari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.dep_Guid = Guid.Empty;
                        //sst_1.dep_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Depolari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Depo kartları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Asorti_Kartlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<ASORTI_TANIMLARI> ls = new List<ASORTI_TANIMLARI>();
            SetControlText(lbl_durum, "Asorti kartları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.Asorti_Tanimlari_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.Asorti_Tanimlari_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Asorti kartları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (ASORTI_TANIMLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    ASORTI_TANIMLARI sst_1 = sst;
                    ASORTI_TANIMLARI sst_2;
                    sst_2 = KartAktarimlari.Asorti_Tanimlari_EvrakDetayGetir(sst_1.Asorti_StokKodu, sst_1.Asorti_BedenNo.Value, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.Asorti_Guid = sst_2.Asorti_Guid;
                        //sst_1.Asorti_RECid_RECno = sst_2.Asorti_RECid_RECno;
                        sst_2 = KartAktarimlari.Asorti_Tanimlari_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.Asorti_Guid = Guid.Empty;
                        //sst_1.Asorti_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.Asorti_Tanimlari_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Asorti kartları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Banka_Kartlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<BANKALAR> ls = new List<BANKALAR>();
            SetControlText(lbl_durum, "Banka kartları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.BANKALARi_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.BANKALARi_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Banka kartları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (BANKALAR sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    BANKALAR sst_1 = sst;
                    BANKALAR sst_2;
                    sst_2 = KartAktarimlari.BANKALAR_EvrakDetayGetir(sst_1.ban_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.ban_Guid = sst_2.ban_Guid;
                        //sst_1.ban_RECid_RECno = sst_2.ban_RECid_RECno;
                        sst_2 = KartAktarimlari.BANKALARi_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.ban_Guid = Guid.Empty;
                        //sst_1.ban_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.BANKALARi_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Banka kartları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Barkod_Tanimlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<BARKOD_TANIMLARI> ls = new List<BARKOD_TANIMLARI>();
            SetControlText(lbl_durum, "Barkod tanımları kartları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.BARKOD_TANIMLARIi_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.BARKOD_TANIMLARIi_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Barkod tanımları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (BARKOD_TANIMLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    BARKOD_TANIMLARI sst_1 = sst;
                    BARKOD_TANIMLARI sst_2;
                    sst_2 = KartAktarimlari.BARKOD_TANIMLARI_EvrakDetayGetir(sst_1.bar_kodu, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.bar_Guid = sst_2.bar_Guid;
                        //sst_1.bar_RECid_RECno = sst_2.bar_RECid_RECno;
                        sst_2 = KartAktarimlari.BARKOD_TANIMLARIi_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.bar_Guid = Guid.Empty;
                        //sst_1.bar_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.BARKOD_TANIMLARIi_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Barkon tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Hizmet_Hesaplari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<HIZMET_HESAPLARI> ls = new List<HIZMET_HESAPLARI>();
            SetControlText(lbl_durum, "Hizmet hesapları kartları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.HIZMET_HESAPLARIi_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.HIZMET_HESAPLARIi_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Hizmet hesapları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (HIZMET_HESAPLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    HIZMET_HESAPLARI sst_1 = sst;
                    HIZMET_HESAPLARI sst_2;
                    sst_2 = KartAktarimlari.HIZMET_HESAPLARI_EvrakDetayGetir(sst_1.hiz_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.hiz_Guid = sst_2.hiz_Guid;
                        //sst_1.hiz_RECid_RECno = sst_2.hiz_RECid_RECno;
                        sst_2 = KartAktarimlari.HIZMET_HESAPLARIi_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.hiz_Guid = Guid.Empty;
                        //sst_1.hiz_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.HIZMET_HESAPLARIi_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Hizmet hesapları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void ithalat_muhasebe_gruplari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<ITHALAT_MUHASEBE_GRUPLARI> ls = new List<ITHALAT_MUHASEBE_GRUPLARI>();
            SetControlText(lbl_durum, "İthalat muhasebe grupları kartları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.ITHALAT_MUHASEBE_GRUPLARIi_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.ITHALAT_MUHASEBE_GRUPLARIi_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "İthalat muhasebe grupları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (ITHALAT_MUHASEBE_GRUPLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    ITHALAT_MUHASEBE_GRUPLARI sst_1 = sst;
                    ITHALAT_MUHASEBE_GRUPLARI sst_2;
                    sst_2 = KartAktarimlari.ITHALAT_MUHASEBE_GRUPLARI_EvrakDetayGetir(sst_1.IthMuh_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.IthMuh_Guid = sst_2.IthMuh_Guid;
                        //sst_1.IthMuh_RECid_RECno = sst_2.IthMuh_RECid_RECno;
                        sst_2 = KartAktarimlari.ITHALAT_MUHASEBE_GRUPLARIi_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.IthMuh_Guid = Guid.Empty;
                        //sst_1.IthMuh_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.ITHALAT_MUHASEBE_GRUPLARIi_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "İthalat muhasebe grupları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Kasa_Kartlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<KASALAR> ls = new List<KASALAR>();
            SetControlText(lbl_durum, "Kasa kartları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.KASALARi_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.KASALARi_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Kasa kartları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (KASALAR sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    KASALAR sst_1 = sst;
                    KASALAR sst_2;
                    sst_2 = KartAktarimlari.KASALAR_EvrakDetayGetir(sst_1.kas_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.kas_Guid = sst_2.kas_Guid;
                        //sst_1.kas_RECid_RECno = sst_2.kas_RECid_RECno;
                        sst_2 = KartAktarimlari.KASALARi_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.kas_Guid = Guid.Empty;
                        //sst_1.kas_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.KASALARi_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Kasa kartları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Son_Kullanicilari_Kartlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<SON_KULLANICILAR> ls = new List<SON_KULLANICILAR>();
            SetControlText(lbl_durum, "Son Kullanıcı kartları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.SON_KULLANICILARi_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.SON_KULLANICILARi_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Son kullanıcı kartları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (SON_KULLANICILAR sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    SON_KULLANICILAR sst_1 = sst;
                    SON_KULLANICILAR sst_2;
                    sst_2 = KartAktarimlari.SON_KULLANICILAR_EvrakDetayGetir(sst_1.tuk_kodu, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.tuk_Guid = sst_2.tuk_Guid;
                        //sst_1.tuk_RECid_RECno = sst_2.tuk_RECid_RECno;
                        sst_2 = KartAktarimlari.SON_KULLANICILARi_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.tuk_Guid = Guid.Empty;
                        //sst_1.tuk_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.SON_KULLANICILARi_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Son kullanıcı kartları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Masraf_Hesaplari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<MASRAF_HESAPLARI> ls = new List<MASRAF_HESAPLARI>();
            SetControlText(lbl_durum, "Masraf hesapları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.MASRAF_HESAPLARIi_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.MASRAF_HESAPLARIi_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Masraf hesapları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (MASRAF_HESAPLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    MASRAF_HESAPLARI sst_1 = sst;
                    MASRAF_HESAPLARI sst_2;
                    sst_2 = KartAktarimlari.MASRAF_HESAPLARI_EvrakDetayGetir(sst_1.his_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.his_Guid = sst_2.his_Guid;
                        //sst_1.his_RECid_RECno = sst_2.his_RECid_RECno;
                        sst_2 = KartAktarimlari.MASRAF_HESAPLARIi_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.his_Guid = Guid.Empty;
                        //sst_1.his_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.MASRAF_HESAPLARIi_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Masraf hesapları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Muhasebe_Fis_Grubu_Tanimlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<MUHASEBE_FIS_GRUBU_TANIMLARI> ls = new List<MUHASEBE_FIS_GRUBU_TANIMLARI>();
            SetControlText(lbl_durum, "Muhasebe fiş grubu tanımları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.MUHASEBE_FIS_GRUBU_TANIMLARIi_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.MUHASEBE_FIS_GRUBU_TANIMLARIi_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Muhasebe fiş grubu tanımları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (MUHASEBE_FIS_GRUBU_TANIMLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    MUHASEBE_FIS_GRUBU_TANIMLARI sst_1 = sst;
                    MUHASEBE_FIS_GRUBU_TANIMLARI sst_2;
                    sst_2 = KartAktarimlari.MUHASEBE_FIS_GRUBU_TANIMLARI_EvrakDetayGetir(sst_1.mfg_kodu, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.mfg_Guid = sst_2.mfg_Guid;
                        //sst_1.mfg_RECid_RECno = sst_2.mfg_RECid_RECno;
                        sst_2 = KartAktarimlari.MUHASEBE_FIS_GRUBU_TANIMLARIi_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.mfg_Guid = Guid.Empty;
                        //sst_1.mfg_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.MUHASEBE_FIS_GRUBU_TANIMLARIi_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Muhasebe fiş grubu tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Muhasebe_Hesap_Grubu_Tanimlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<MUHASEBE_HESAP_GRUPLARI> ls = new List<MUHASEBE_HESAP_GRUPLARI>();
            SetControlText(lbl_durum, "Muhasebe hesap grubu tanımları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.MUHASEBE_HESAP_GRUPLARIi_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.MUHASEBE_HESAP_GRUPLARIi_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Muhasebe hesap grubu tanımları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (MUHASEBE_HESAP_GRUPLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    MUHASEBE_HESAP_GRUPLARI sst_1 = sst;
                    MUHASEBE_HESAP_GRUPLARI sst_2;
                    sst_2 = KartAktarimlari.MUHASEBE_HESAP_GRUPLARI_EvrakDetayGetir(sst_1.mhg_kodu, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.mhg_Guid = sst_2.mhg_Guid;
                        //sst_1.mhg_RECid_RECno = sst_2.mhg_RECid_RECno;
                        sst_2 = KartAktarimlari.MUHASEBE_HESAP_GRUPLARIi_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.mhg_Guid = Guid.Empty;
                        //sst_1.mhg_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.MUHASEBE_HESAP_GRUPLARIi_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Muhasebe hesap grubu tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Proje_Kartlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<PROJELER> ls = new List<PROJELER>();
            SetControlText(lbl_durum, "Proje kartları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.PROJELERi_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.PROJELERi_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Proje kartları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (PROJELER sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    PROJELER sst_1 = sst;
                    PROJELER sst_2;
                    sst_2 = KartAktarimlari.PROJELER_EvrakDetayGetir(sst_1.pro_kodu, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.pro_Guid = sst_2.pro_Guid;
                        //sst_1.pro_RECid_RECno = sst_2.pro_RECid_RECno;
                        sst_2 = KartAktarimlari.PROJELERi_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.pro_Guid = Guid.Empty;
                        //sst_1.pro_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.PROJELERi_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Proje kartları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void SorumlulukMerkezi_Kartlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<SORUMLULUK_MERKEZLERI> ls = new List<SORUMLULUK_MERKEZLERI>();
            SetControlText(lbl_durum, "Sorumluluk Merkezi kartları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.SORUMLULUK_MERKEZLERIi_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.SORUMLULUK_MERKEZLERIi_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Sorumluluk merkezi kartları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (SORUMLULUK_MERKEZLERI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    SORUMLULUK_MERKEZLERI sst_1 = sst;
                    SORUMLULUK_MERKEZLERI sst_2;
                    sst_2 = KartAktarimlari.SORUMLULUK_MERKEZLERI_EvrakDetayGetir(sst_1.som_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.som_Guid = sst_2.som_Guid;
                        //sst_1.som_RECid_RECno = sst_2.som_RECid_RECno;
                        sst_2 = KartAktarimlari.SORUMLULUK_MERKEZLERIi_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.som_Guid = Guid.Empty;
                        //sst_1.som_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.SORUMLULUK_MERKEZLERIi_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Sorumluluk merkezi kartları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Urun_Kartlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<URUNLER> ls = new List<URUNLER>();
            SetControlText(lbl_durum, "Ürün kartları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.URUNLERi_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.URUNLERi_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Ürün kartları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (URUNLER sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    URUNLER sst_1 = sst;
                    URUNLER sst_2;
                    sst_2 = KartAktarimlari.URUNLER_EvrakDetayGetir(sst_1.uru_stok_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.uru_Guid = sst_2.uru_Guid;
                        //sst_1.uru_RECid_RECno = sst_2.uru_RECid_RECno;
                        sst_2 = KartAktarimlari.URUNLERi_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.uru_Guid = Guid.Empty;
                        //sst_1.uru_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.URUNLERi_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Ürün kartları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Finansal_Sozlesme_Taksit_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<FINANSAL_SOZLESME_TAKSITLERI> ls = new List<FINANSAL_SOZLESME_TAKSITLERI>();
            SetControlText(lbl_durum, "Finansal sözleşme taksitleri yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.FINANSAL_SOZLESME_TAKSITLERI_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.FINANSAL_SOZLESME_TAKSITLERI_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Finansal sözleşme taksitleri Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (FINANSAL_SOZLESME_TAKSITLERI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    FINANSAL_SOZLESME_TAKSITLERI sst_1 = sst;
                    FINANSAL_SOZLESME_TAKSITLERI sst_2;
                    sst_2 = KartAktarimlari.FINANSAL_SOZLESME_TAKSITLERI_EvrakDetayGetir(sst_1.FST_sozkodu, sst_1.FST_taksitno, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.FST_Guid = sst_2.FST_Guid;
                        //sst_1.uru_RECid_RECno = sst_2.uru_RECid_RECno;
                        sst_2 = KartAktarimlari.FINANSAL_SOZLESME_TAKSITLERI_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.FST_Guid = Guid.Empty;
                        //sst_1.uru_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.FINANSAL_SOZLESME_TAKSITLERI_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Finansal sözleşme taksitleri aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Uretim_Malzeme_Planlama_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<URETIM_MALZEME_PLANLAMA> ls = new List<URETIM_MALZEME_PLANLAMA>();
            SetControlText(lbl_durum, "Üretim Malzeme Planlama yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.URETIMMALZPLANLAMA_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.URETIMMALZPLANLAMA_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Üretim Malzeme Planlama Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (URETIM_MALZEME_PLANLAMA sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    URETIM_MALZEME_PLANLAMA sst_1 = sst;
                    URETIM_MALZEME_PLANLAMA sst_2;
                    sst_2 = KartAktarimlari.URETIMMALZPLANLAMA_EvrakDetayGetir(sst_1.upl_isemri, Convert.ToInt32(sst_1.upl_satirno), DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.upl_Guid = sst_2.upl_Guid;
                        //sst_1.uru_RECid_RECno = sst_2.uru_RECid_RECno;
                        sst_2 = KartAktarimlari.URETIMMALZPLANLAMA_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.upl_Guid = Guid.Empty;
                        //sst_1.uru_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.URETIMMALZPLANLAMA_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Üretim Malzeme Planlama aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Finansal_Sozlesmeler_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<FINANSAL_SOZLESMELER> ls = new List<FINANSAL_SOZLESMELER>();
            SetControlText(lbl_durum, "Finansal Sözleşmeler yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.FINANSAL_SOZLESMELER_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.FINANSAL_SOZLESMELER_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Finansal Sözleşmeler Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (FINANSAL_SOZLESMELER sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    FINANSAL_SOZLESMELER sst_1 = sst;
                    FINANSAL_SOZLESMELER sst_2;
                    sst_2 = KartAktarimlari.FINANSAL_SOZLESMELER_EvrakDetayGetir(sst_1.FS_sozkodu, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.FS_Guid = sst_2.FS_Guid;
                        //sst_1.uru_RECid_RECno = sst_2.uru_RECid_RECno;
                        sst_2 = KartAktarimlari.FINANSAL_SOZLESMELER_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.FS_Guid = Guid.Empty;
                        //sst_1.uru_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.FINANSAL_SOZLESMELER_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Finansal Sözleşmeler aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Urun_Receteleri_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<URUN_RECETELERI> ls = new List<URUN_RECETELERI>();
            SetControlText(lbl_durum, "Ürün reçeteleri yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.URUNRECETE_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.URUNRECETE_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Ürün reçeteleri Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (URUN_RECETELERI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    URUN_RECETELERI sst_1 = sst;
                    URUN_RECETELERI sst_2;
                    sst_2 = KartAktarimlari.URUNRECETE_EvrakDetayGetir(sst_1.rec_anakod, sst_1.rec_tuketim_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.rec_Guid = sst_2.rec_Guid;
                        //sst_1.uru_RECid_RECno = sst_2.uru_RECid_RECno;
                        sst_2 = KartAktarimlari.URUNRECETE_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.rec_Guid = Guid.Empty;
                        //sst_1.uru_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.URUNRECETE_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Ürün reçeteleri aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Muhasebe_Hesap_Plani_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<MUHASEBE_HESAP_PLANI> ls = new List<MUHASEBE_HESAP_PLANI>();
            SetControlText(lbl_durum, "Muhasebe hesap planları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.MUHASEBE_HESAP_PLANIi_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.MUHASEBE_HESAP_PLANIi_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Muhasebe hesap planları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (MUHASEBE_HESAP_PLANI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    MUHASEBE_HESAP_PLANI sst_1 = sst;
                    MUHASEBE_HESAP_PLANI sst_2;
                    sst_2 = KartAktarimlari.MUHASEBE_HESAP_PLANI_EvrakDetayGetir(sst_1.muh_hesap_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.muh_Guid = sst_2.muh_Guid;
                        //sst_1.muh_RECid_RECno = sst_2.muh_RECid_RECno;
                        sst_2 = KartAktarimlari.MUHASEBE_HESAP_PLANIi_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.muh_Guid = Guid.Empty;
                        //sst_1.muh_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.MUHASEBE_HESAP_PLANIi_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Muhasebe hesap planı aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void ihracat_dosyalari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<IHRACAT_DOSYALARI> ls = new List<IHRACAT_DOSYALARI>();
            SetControlText(lbl_durum, "İhracat dosyaları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.IHRACAT_DOSYALARIi_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.IHRACAT_DOSYALARIi_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "İhracat dosyaları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (IHRACAT_DOSYALARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    IHRACAT_DOSYALARI sst_1 = sst;
                    IHRACAT_DOSYALARI sst_2;
                    sst_2 = KartAktarimlari.IHRACAT_DOSYALARI_EvrakDetayGetir(sst_1.ihr_kodu, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.ihr_Guid = sst_2.ihr_Guid;
                        //sst_1.ihr_RECid_RECno = sst_2.ihr_RECid_RECno;
                        sst_2 = KartAktarimlari.IHRACAT_DOSYALARIi_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.ihr_Guid = Guid.Empty;
                        //sst_1.ihr_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.IHRACAT_DOSYALARIi_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "İhracat dosyaları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void ithalat_dosyalari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<ITHALAT_DOSYALARI> ls = new List<ITHALAT_DOSYALARI>();
            SetControlText(lbl_durum, "İthalat dosyaları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.ITHALAT_DOSYALARIi_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.ITHALAT_DOSYALARIi_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "İthalat dosyaları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (ITHALAT_DOSYALARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    ITHALAT_DOSYALARI sst_1 = sst;
                    ITHALAT_DOSYALARI sst_2;
                    sst_2 = KartAktarimlari.ITHALAT_DOSYALARI_EvrakDetayGetir(sst_1.ith_kodu, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.ith_Guid = sst_2.ith_Guid;
                        //sst_1.ith_RECid_RECno = sst_2.ith_RECid_RECno;
                        sst_2 = KartAktarimlari.ITHALAT_DOSYALARIi_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.ith_Guid = Guid.Empty;
                        //sst_1.ith_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.ITHALAT_DOSYALARIi_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "İthalat dosyaları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Partilot_Kartlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<PARTILOT> ls = new List<PARTILOT>();
            SetControlText(lbl_durum, "Partilot kartlari yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.PARTILOT_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.PARTILOT_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Partilot kartlari Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (PARTILOT sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    PARTILOT sst_1 = sst;
                    PARTILOT sst_2;
                    sst_2 = KartAktarimlari.PARTILOT_EvrakDetayGetir(sst_1.pl_partikodu, sst_1.pl_lotno.Value, sst_1.pl_stokkodu, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.pl_Guid = sst_2.pl_Guid;
                        //sst_1.pl_RECid_RECno = sst_2.pl_RECid_RECno;
                        sst_2 = KartAktarimlari.PARTILOT_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.pl_Guid = Guid.Empty;
                        //sst_1.pl_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.PARTILOT_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Partilot kartlari aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Demirbaslar_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<DEMIRBASLAR> ls = new List<DEMIRBASLAR>();
            SetControlText(lbl_durum, "Demirbaşlar yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.DEMIRBASLARi_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.DEMIRBASLARi_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Demirbaşlar Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (DEMIRBASLAR sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    DEMIRBASLAR sst_1 = sst;
                    DEMIRBASLAR sst_2;
                    sst_2 = KartAktarimlari.DEMIRBASLAR_EvrakDetayGetir(sst_1.dem_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.dem_Guid = sst_2.dem_Guid;
                        //sst_1.dem_RECid_RECno = sst_2.dem_RECid_RECno;
                        sst_2 = KartAktarimlari.DEMIRBASLARi_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.dem_Guid = Guid.Empty;
                        //sst_1.dem_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.DEMIRBASLARi_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Demirbaşlar aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Demirbas_Mali_Yil_Tanimlari_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<DEMIRBAS_MALIYIL_TANIMLARI> ls = new List<DEMIRBAS_MALIYIL_TANIMLARI>();
            SetControlText(lbl_durum, "Demirbaş mali yıl tanımları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.DEMIRBAS_MALIYIL_TANIMLARIi_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.DEMIRBAS_MALIYIL_TANIMLARIi_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Demirbaş mali yıl tanımları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (DEMIRBAS_MALIYIL_TANIMLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    DEMIRBAS_MALIYIL_TANIMLARI sst_1 = sst;
                    DEMIRBAS_MALIYIL_TANIMLARI sst_2;
                    sst_2 = KartAktarimlari.DEMIRBAS_MALIYIL_TANIMLARI_EvrakDetayGetir(sst_1.amy_kod, sst_1.amy_maliyil.Value, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.amy_Guid = sst_2.amy_Guid;
                        //sst_1.amy_RECid_RECno = sst_2.amy_RECid_RECno;
                        sst_2 = KartAktarimlari.DEMIRBAS_MALIYIL_TANIMLARIi_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.amy_Guid = Guid.Empty;
                        //sst_1.amy_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.DEMIRBAS_MALIYIL_TANIMLARIi_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Demirbaş mali yıl tanımları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void Demirbas_Gruplari(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<DEMIRBAS_GRUPLARI> ls = new List<DEMIRBAS_GRUPLARI>();
            SetControlText(lbl_durum, "Demirbaş grupları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.DEMIRBAS_GRUPLARIi_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.DEMIRBAS_GRUPLARIi_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Demirbaş Grupları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (DEMIRBAS_GRUPLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    DEMIRBAS_GRUPLARI sst_1 = sst;
                    DEMIRBAS_GRUPLARI sst_2;
                    sst_2 = KartAktarimlari.DEMIRBAS_GRUPLARI_EvrakDetayGetir(sst_1.grp_kod, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.grp_Guid = sst_2.grp_Guid;
                        //sst_1.grp_RECid_RECno = sst_2.grp_RECid_RECno;
                        sst_2 = KartAktarimlari.DEMIRBAS_GRUPLARIi_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.grp_Guid = Guid.Empty;
                        //sst_1.grp_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.DEMIRBAS_GRUPLARIi_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Demirbaş grupları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void kredi_sozlesmeleri_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<KREDI_SOZLESMESI_TANIMLARI> ls = new List<KREDI_SOZLESMESI_TANIMLARI>();
            SetControlText(lbl_durum, "Kredi sözleşme dosyaları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.KREDI_SOZLESMESI_TANIMLARI_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.KREDI_SOZLESMESI_TANIMLARI_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Kredi sözleşme dosyaları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (KREDI_SOZLESMESI_TANIMLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    KREDI_SOZLESMESI_TANIMLARI sst_1 = sst;
                    KREDI_SOZLESMESI_TANIMLARI sst_2;
                    sst_2 = KartAktarimlari.KREDI_SOZLESMESI_TANIMLARI_EvrakDetayGetir(sst_1.krsoz_kodu, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.krsoz_Guid = sst_2.krsoz_Guid;
                        //sst_1.krsoz_RECid_RECno = sst_2.krsoz_RECid_RECno;
                        sst_2 = KartAktarimlari.KREDI_SOZLESMESI_TANIMLARI_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.krsoz_Guid = Guid.Empty;
                        //sst_1.krsoz_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.KREDI_SOZLESMESI_TANIMLARI_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Kredi sözleşme dosyaları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void kredi_taksit_tanim_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<KREDI_SOZLESMESI_TAKSIT_TANIMLARI> ls = new List<KREDI_SOZLESMESI_TAKSIT_TANIMLARI>();
            SetControlText(lbl_durum, "Kredi taksit tanımları dosyaları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.KREDI_TAKSIT_TANIMLARI_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.KREDI_TAKSIT_TANIMLARI_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Kredi taksit tanımları dosyaları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (KREDI_SOZLESMESI_TAKSIT_TANIMLARI sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    KREDI_SOZLESMESI_TAKSIT_TANIMLARI sst_1 = sst;
                    KREDI_SOZLESMESI_TAKSIT_TANIMLARI sst_2;
                    sst_2 = KartAktarimlari.KREDI_TAKSIT_TANIMLARI_EvrakDetayGetir(sst_1.krsoztaksit_sozkodu, (int)sst_1.krsoztaksit_taksitno, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.krsoztaksit_Guid = sst_2.krsoztaksit_Guid;
                        //sst_1.krsoztaksit_RECid_RECno = sst_2.krsoztaksit_RECid_RECno;
                        sst_2 = KartAktarimlari.KREDI_TAKSIT_TANIMLARI_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.krsoztaksit_Guid = Guid.Empty;
                        //sst_1.krsoztaksit_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.KREDI_TAKSIT_TANIMLARI_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Kredi taksit tanımları dosyaları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        void donyay_tanim_Aktarimi(DateTime tarih1, DateTime tarih2, bool anlik_mi, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            List<DONEMLERE_YAYILAN_HIZMETLER> ls = new List<DONEMLERE_YAYILAN_HIZMETLER>();
            SetControlText(lbl_durum, "Dönemlere yayılan hizmet tanımları dosyaları yükleniyor...");
            if (anlik_mi == true)
                ls = KartAktarimlari.DONEMLERE_YAYILAN_HIZMETLER_Yukle(tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = KartAktarimlari.DONEMLERE_YAYILAN_HIZMETLER_Yukle(DatabaseFacade.ConnectionString());
            if (ls.Count > 0)
            {
                SetControlText(lbl_durum, "Dönemlere yayılan hizmet tanımları dosyaları Aktarılıyor...");
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = ls.Count;
                Int32 pb_i = 0;
                foreach (DONEMLERE_YAYILAN_HIZMETLER sst in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    DONEMLERE_YAYILAN_HIZMETLER sst_1 = sst;
                    DONEMLERE_YAYILAN_HIZMETLER sst_2;
                    sst_2 = KartAktarimlari.DONEMLERE_YAYILAN_HIZMETLER_EvrakDetayGetir(sst_1.dyh_kodu, DatabaseFacade2.ConnectionString());
                    if (sst_2 != null)
                    {
                        sst_1.dyh_Guid = sst_2.dyh_Guid;
                        //sst_1.dyh_RECid_RECno = sst_2.dyh_RECid_RECno;
                        sst_2 = KartAktarimlari.DONEMLERE_YAYILAN_HIZMETLER_Guncelle(sst_1, DatabaseFacade2.ConnectionString());
                    }
                    else
                    {
                        sst_1.dyh_Guid = Guid.Empty;
                        //sst_1.dyh_RECid_RECno = -1;
                        sst_2 = KartAktarimlari.DONEMLERE_YAYILAN_HIZMETLER_Kaydet(sst_1, DatabaseFacade2.ConnectionString());
                    }
                }
                SetControlText(lbldurum, "Dönemlere yayılan hizmet tanımları tanımları dosyaları aktarıldı..");
            }
            else
                SetControlText(lbldurum, "");
        }
        #endregion
        #endregion
        #region AKTARIM AYAR - PARAMETRE İŞLEMLERİ
        private void pb_AktarimParam_Click(object sender, EventArgs e)
        {
            frm_Aktarim_Param frm = new frm_Aktarim_Param();
            frm.ShowDialog();
            AnlikAktarimDurumuGoster();
            AnlikAktarimTimerKontrol();
        }
        private void sb_kaynak_Click(object sender, EventArgs e)
        {
            SqlAyar frm = new SqlAyar();
            frm.Baslik = "KAYNAK VERİTABANI BAĞLANTI AYARI";
            frm.FilePath = Application.StartupPath + "\\Settings.xml";
            frm.ShowDialog();
            BaglantiKontrol();
        }
        private void sb_hedef_Click(object sender, EventArgs e)
        {
            SqlAyar frm = new SqlAyar();
            frm.Baslik = "HEDEF VERİTABANI BAĞLANTI AYARI";
            frm.FilePath = Application.StartupPath + "\\Settings2.xml";
            frm.ShowDialog();
            BaglantiKontrol();
        }
        private void sb_parametre_Click(object sender, EventArgs e)
        {
            //Tools.MesajPenceresi("AKTARIM PARAMETRELERİ veritabanı bağlantısı yapılamadı.");
            SqlAyar frm = new SqlAyar();
            frm.Baslik = "AKTARIM PARAMETRELERİ VERİTABANI BAĞLANTI AYARI";
            frm.FilePath = Application.StartupPath + "\\Settings3.xml";
            frm.ShowDialog();
            BaglantiKontrol();
        }
        private void pb_aktarim_Serileri_Click(object sender, EventArgs e)
        {
            frm_AktarimSeri frm = new frm_AktarimSeri();
            frm.ShowDialog();
        }
        #endregion
        void NotifyIconYukle()
        {
            notifyicon = new NotifyIcon(); //Yeni bir NotifyIcon tanımladık
            notifyicon.Text = "Bigus Aktarıcı V17 DÜZ"; //Mouse ile uzerine geldiğimizde olusacak yazı
            notifyicon.Visible = true; //Gorunur ozelligi
            notifyicon.Icon = new Icon(Application.StartupPath + "\\bigus.ico"); //Iconumuzu belirledik
            notifyicon.BalloonTipIcon = ToolTipIcon.Info;
            notifyicon.BalloonTipTitle = "BİGUS AKTARICI V17 DÜZ";
            notifyicon.BalloonTipText = "GÖSTER";
            menu = new ContextMenu(); //Yeni bir ContextMenu tanımladık
            menu.MenuItems.Add(0, new MenuItem("Goster", new System.EventHandler(Goster_Click))); //Menuye eklemeler yapıyoruz.
            menu.MenuItems.Add(1, new MenuItem("Gizle", new System.EventHandler(Gizle_Click)));
            menu.MenuItems.Add(2, new MenuItem("Kapat", new System.EventHandler(Kapat_Click)));
            this.notifyicon.DoubleClick += new System.EventHandler(this.notifyicon_Click);
            notifyicon.ContextMenu = menu;
        }
        protected void Goster_Click(object sender, System.EventArgs e)
        {
            this.WindowState = FormWindowState.Normal; //Formumuzu normal ebatlara getircek
            this.Activate();
        }
        protected void Gizle_Click(object sender, System.EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized; // Formumuzu minimize edecek
        }
        protected void Kapat_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }
        private void notifyicon_Click(object sender, System.EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }
        private void frm_Aktarim_Deactivate(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                notifyicon.BalloonTipText = "Bigus Aktarıcı V17 DÜZ görev çubuğunda çalışıyor.";
                notifyicon.ShowBalloonTip(10);
            }
        }
        private void frm_Aktarim_FormClosing(object sender, FormClosingEventArgs e)
        {
            Int32 sonuc = Tools.MesajPenceresi_Return("Bigus Aktarıcı'dan çıkmak istediğinizden emin misiniz?", "Hayır", "Evet");
            if (sonuc == 0)
            {
                e.Cancel = true;
            }
        }
        private void groupControl1_Paint(object sender, PaintEventArgs e)
        {
        }
        private void pb_hakkimizda_Click(object sender, EventArgs e)
        {
            frm_Hakkımızda frm = new frm_Hakkımızda();
            frm.ShowDialog();
        }
        private void pnl_splash_Paint(object sender, PaintEventArgs e)
        {
        }
        private void sb_SeriNoSecimi_Click(object sender, EventArgs e)
        {
            frm_AktarimSeri frm = new frm_AktarimSeri();
            frm.ShowDialog();
        }
        private void sb_Ayarlar_Click(object sender, EventArgs e)
        {
            frm_Aktarim_Param frm = new frm_Aktarim_Param();
            frm.ShowDialog();
            #region AKTARIM PARAMETRELERİNİ YÜKLE
            List<AKTARIM_PARAMETRELERI> ls_param = new List<AKTARIM_PARAMETRELERI>();
            ls_param = AktarimParametreleri.Aktarim_Parametrelerini_Yukle(DatabaseFacade3.ConnectionString());
            if (ls_param.Count == 0)
            {
                frm_Aktarim_Param _frm_param = new frm_Aktarim_Param();
                _frm_param.ShowDialog();
                BaglantiKontrol();
                ls_param = AktarimParametreleri.Aktarim_Parametrelerini_Yukle(DatabaseFacade3.ConnectionString());
            }
            AktarimParametreleri.Parametre = ls_param[0];
            #endregion
            AnlikAktarimDurumuGoster();
            AnlikAktarimTimerKontrol();
        }
        private void sb_Kullanici_Click(object sender, EventArgs e)
        {
            frm_AktarimKullanici frm = new frm_AktarimKullanici();
            frm.ShowDialog();
        }
        #region MUHASEBE_FISLERI AKTARIMI
        private void sb_MuhasebeFisleriAktarimi_Click(object sender, EventArgs e)
        {
            AktarimEkraniDondur(true);
            tmr_ev.Enabled = false;
            if (AktarimParametreleri.Parametre.akt_evrak_anlik == true)
            {
                timer_aktarim.Enabled = true;
            }
            SetControlText(lbl_son_aktarim_elle, DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString());
            new Thread(GuncellemeliAktarim_Muh).Start();
        }
        void GuncellemeliAktarim_Muh()
        {
            try
            {
                SetControlText(lbl_sure1, DateTime.Now.ToLongTimeString());
                SetControlText(lbl_süre2, "SÜRE");
                AktarimaBasla_Muh(dateEdit1.DateTime.Date, dateEdit2.DateTime.Date, false, AktarimParametreleri.Parametre, pbc_1, lbl_durum_1);
                AktarimEkraniDondur(false);
                SetControlText(lbl_süre2, DateTime.Now.ToLongTimeString());
                if (AktarimParametreleri.Parametre.akt_evrak_anlik == true)
                {
                    aktarim_bittimi = true;
                }
                pbc_1.Text = "0";
            }
            catch (ThreadAbortException ex)
            {
                AktarimEkraniDondur(false);
                SetControlText(lbl_süre2, DateTime.Now.ToLongTimeString());
                if (AktarimParametreleri.Parametre.akt_evrak_anlik == true)
                {
                    aktarim_bittimi = true;
                }
                SetControlText(lbl_durum_1, "Aktarım işlemi tamamlanamadı. Log dosyalarını kontrol ediniz...");
                pbc_1.Text = "0";
                Thread.CurrentThread.Abort();
            }
        }
        void AktarimaBasla_Muh(DateTime _tarih1, DateTime _tarih2, bool anlik_mi, AKTARIM_PARAMETRELERI par, ProgressBarControl pbc, LabelControl lbl_durum)
        {
            DateTime tarih1 = _tarih1;
            DateTime tarih2 = _tarih2;
            string[] serino = new string[AktarimParametreleri.AktarimSerileri.Count];
            int i = 0;
            foreach (AKTARIM_SERILERI ser in AktarimParametreleri.AktarimSerileri)
            {
                serino[i] = ser.ser_serino;
                i += 1;
            }
            #region Eski kayıtları sil
            if (anlik_mi == false)
            {
                pbc.Text = "0";
                pbc.Properties.Step = 1;
                pbc.Properties.Maximum = 100;
                Int32 pb_i1 = 0;
                pb_i1 += 1;
                pbc.Text = pb_i1.ToString();
                SetControlText(lbl_durum, "Muhasebe Fişleri silinen kayıtlar kontrol ediliyor...");
                Aktarimlar2.Muhasebe_Fisleri_Sil(serino, _tarih1, _tarih2, DatabaseFacade2.ConnectionString());
                SetControlText(lbl_durum, "Muhasebe Fişleri silinen kayıtlar kontrol edildi.");
                pb_i1 = 0;
                pbc.Text = "0";
            }
            #endregion
            List<MUHASEBE_FISLERI> ls = new List<MUHASEBE_FISLERI>();
            if (anlik_mi == false)
                ls = Aktarimlar2.Muhasebe_Fisleri_Getir(serino, tarih1, tarih2, DatabaseFacade.ConnectionString());
            else
                ls = Aktarimlar2.Muhasebe_Fisleri_Getir(serino, tarih1, tarih2, DatabaseFacade.ConnectionString());
            pbc.Text = "0";
            pbc.Properties.Step = 1;
            pbc.Properties.Maximum = ls.Count;
            Int32 pb_i = 0;
            #region MUHASEBE_FISLERI AKTARIMI
            if (ls.Count > 0)
            {
                foreach (MUHASEBE_FISLERI mfis in ls)
                {
                    pb_i += 1;
                    pbc.Text = pb_i.ToString();
                    SetControlText(lbl_durum, "Muhasebe Fişleri aktarılıyor...");
                    MUHASEBE_FISLERI mf_1 = mfis;
                    MUHASEBE_FISLERI mf_2;
                    Guid rn = Aktarimlar2.FisTicariRecNo_Getir(mfis, DatabaseFacade.ConnectionString(), DatabaseFacade2.ConnectionString());
                    if (rn != Guid.Empty)
                    {
                        mf_1.fis_ticari_uid = rn;
                        mf_2 = Aktarimlar2.Muhasebe_Fisleri_Kaydet(mf_1, DatabaseFacade2.ConnectionString());
                    }
                }
            }
            #endregion
            SetControlText(lbl_durum, "Aktarım tamamlandı.");
            pb_i = 0;
            pbc.Text = "0";
        }
        #endregion
    }
}
