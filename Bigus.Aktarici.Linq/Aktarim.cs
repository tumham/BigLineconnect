using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;


namespace Bigus.Aktarici.Linq
{
    public class Aktarimlar
    {
        public static List<Evrak> HareketleriYukle(string SeriNo, DateTime Tarih1, DateTime Tarih2,string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {


            var tt = from T in db.STOK_HAREKETLERIs where T.sth_evrakno_seri == SeriNo && (T.sth_tarih >= Tarih1 && T.sth_tarih <= Tarih2) select new { T.sth_evraktip, T.sth_tip, T.sth_evrakno_seri, T.sth_evrakno_sira, T.sth_satirno,T.sth_Guid };

            List<Evrak> ls = new List<Evrak>();
            Evrak ev;
            foreach (var t in tt)
            {
                ev = new Evrak();
                ev.EvrakTip = t.sth_evraktip.Value;
                ev.Tip = t.sth_tip.Value;
                ev.SeriNo = t.sth_evrakno_seri;
                ev.SiraNo = t.sth_evrakno_sira.Value;
                ev.SatirNo = t.sth_satirno.Value;
                ev.RECno = t.sth_Guid;
                ls.Add(ev);
            }

            return ls;

            }
        }

        public static List<STOK_HAREKETLERI> Stok_Hareketlerini_Yukle(Guid recno ,string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {


            //var tt = from T in db.STOK_HAREKETLERIs where T.sth_fat_recid_recno == recno select new { T.sth_evraktip, T.sth_tip, T.sth_evrakno_seri, T.sth_evrakno_sira, T.sth_satirno, T.sth_RECno };

            List<STOK_HAREKETLERI> ls = new List<STOK_HAREKETLERI>();

           ls= (from T in db.STOK_HAREKETLERIs where T.sth_fat_uid == recno select T).ToList<STOK_HAREKETLERI>();

            return ls;

            }
        }

        public static List<Evrak> Stok_Hareketleri_Yukle_2(string SeriNo, DateTime Tarih1, DateTime Tarih2, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {


            var tt = from T in db.STOK_HAREKETLERIs where T.sth_evrakno_seri == SeriNo && (T.sth_tarih >= Tarih1 && T.sth_tarih <= Tarih2) && T.sth_fat_uid == Guid.Empty select new { T.sth_evraktip, T.sth_tip, T.sth_evrakno_seri, T.sth_evrakno_sira, T.sth_satirno, T.sth_Guid };

            List<Evrak> ls = new List<Evrak>();
            Evrak ev;
            foreach (var t in tt)
            {
                ev = new Evrak();
                ev.EvrakTip = t.sth_evraktip.Value;
                ev.Tip = t.sth_tip.Value;
                ev.SeriNo = t.sth_evrakno_seri;
                ev.SiraNo = t.sth_evrakno_sira.Value;
                ev.SatirNo = t.sth_satirno.Value;
                ev.RECno = t.sth_Guid;
                ls.Add(ev);
            }

            return ls;

            }
        }

        public static List<Evrak> Cari_HareketleriYukle(string SeriNo, DateTime Tarih1, DateTime Tarih2, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
           

            var tx   = from T in db.CARI_HESAP_HAREKETLERIs where T.cha_evrakno_seri == SeriNo && (T.cha_tarihi >= Tarih1 && T.cha_tarihi <= Tarih2) && (T.cha_evrak_tip == 0 || T.cha_evrak_tip == 61 ||T.cha_evrak_tip==63) select new { T.cha_evrak_tip, T.cha_tip, T.cha_evrakno_seri, T.cha_evrakno_sira, T.cha_satir_no, T.cha_Guid };
   

  
            List<Evrak> ls = new List<Evrak>();
            Evrak ev;
            foreach (var t in tx)
            {
                ev = new Evrak();
                ev.EvrakTip = t.cha_evrak_tip.Value;
                ev.Tip = t.cha_tip.Value;
                ev.SeriNo = t.cha_evrakno_seri;
                ev.SiraNo = t.cha_evrakno_sira.Value;
                ev.SatirNo = t.cha_satir_no.Value;
                ev.RECno = t.cha_Guid;
                ls.Add(ev);
            }

            return ls;

            }
        }

        public static List<Evrak> Cari_HareketleriYukle_2(string SeriNo, DateTime Tarih1, DateTime Tarih2,  string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {


                var tx = from T in db.CARI_HESAP_HAREKETLERIs where T.cha_evrakno_seri == SeriNo && (T.cha_tarihi >= Tarih1 && T.cha_tarihi <= Tarih2) && (T.cha_evrak_tip != 0 && T.cha_evrak_tip != 61 && T.cha_evrak_tip != 63) select new { T.cha_evrak_tip, T.cha_tip, T.cha_evrakno_seri, T.cha_evrakno_sira, T.cha_satir_no, T.cha_Guid };
         
            //var tt = from T in db.STOK_HAREKETLERIs where T.sth_evrakno_seri == SeriNo && (T.sth_tarih >= Tarih1 && T.sth_tarih <= Tarih2) select T;

            //List<STOK_HAREKETLERI> ls = new List<STOK_HAREKETLERI>();
            //ls =tt.ToList<STOK_HAREKETLERI>();
            //return ls;
            List<Evrak> ls = new List<Evrak>();
            Evrak ev;
            foreach (var t in tx)
            {
                ev = new Evrak();
                ev.EvrakTip = t.cha_evrak_tip.Value;
                ev.Tip = t.cha_tip.Value;
                ev.SeriNo = t.cha_evrakno_seri;
                ev.SiraNo = t.cha_evrakno_sira.Value;
                ev.SatirNo = t.cha_satir_no.Value;
                ev.RECno = t.cha_Guid;
                ls.Add(ev);
            }

            return ls;

            }
        }

        public static List<Evrak> SiparisleriYukle(string SeriNo, DateTime Tarih1, DateTime Tarih2, string conn)
        {
             using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
             {
             var tx = from T in db.SIPARISLERs where T.sip_evrakno_seri == SeriNo && (T.sip_tarih >= Tarih1 && T.sip_tarih <= Tarih2)  select new { T.sip_tip, T.sip_cins, T.sip_evrakno_seri, T.sip_evrakno_sira, T.sip_satirno, T.sip_Guid };


             List<Evrak> ls = new List<Evrak>();
             Evrak ev;
             foreach (var t in tx)
             {
                 ev = new Evrak();
                 ev.EvrakTip = t.sip_tip.Value;
                 ev.Tip = t.sip_cins.Value;
                 ev.SeriNo = t.sip_evrakno_seri;
                 ev.SiraNo = t.sip_evrakno_sira.Value;
                 ev.SatirNo = t.sip_satirno.Value;
                 ev.RECno = t.sip_Guid;
                 ls.Add(ev);
             }

             return ls;

            }
        }

        public static bool Stok_Hareket_EvrakKontrol(Evrak ev,string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            var tt = db.STOK_HAREKETLERIs.Count(T => (T.sth_evraktip == ev.EvrakTip && T.sth_tip == ev.Tip && T.sth_evrakno_seri == ev.SeriNo && T.sth_evrakno_sira == ev.SiraNo && T.sth_satirno == ev.SatirNo));

            Int32 sayi =  Convert.ToInt32(tt);
            if (sayi > 0)
                return true;
            else
                return false;

            }
        }

        public static bool Cari_Hareket_EvrakKontrol(Evrak ev, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            var tt = db.CARI_HESAP_HAREKETLERIs.Count(T => (T.cha_evrak_tip== ev.EvrakTip && T.cha_tip == ev.Tip && T.cha_evrakno_seri == ev.SeriNo && T.cha_evrakno_sira == ev.SiraNo && T.cha_satir_no == ev.SatirNo));

            Int32 sayi = Convert.ToInt32(tt);
            if (sayi > 0)
                return true;
            else
                return false;

            }
        }

        public static bool Siparis_EvrakKontrol(Evrak ev, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            var tt = db.SIPARISLERs.Count(T => (T.sip_tip == ev.EvrakTip && T.sip_cins == ev.Tip && T.sip_evrakno_seri == ev.SeriNo && T.sip_evrakno_sira == ev.SiraNo && T.sip_satirno == ev.SatirNo));

            Int32 sayi = Convert.ToInt32(tt);
            if (sayi > 0)
                return true;
            else
                return false;

            }
        }

        public static bool OdemeEmirleri_Kontrol(byte odemetip,string refno, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            var tt = db.ODEME_EMIRLERIs.Count(T => (T.sck_tip == odemetip && T.sck_refno == refno));

            Int32 sayi = Convert.ToInt32(tt);
            if (sayi > 0)
                return true;
            else
                return false;
            }
        }



        public static STOK_HAREKETLERI Stok_Hareket_EvrakDetayGetir(Guid RECno, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_HAREKETLERI tt = db.STOK_HAREKETLERIs.Single(t => t.sth_Guid == RECno);

            return tt;

            }
        }

        public static STOK_HAREKETLERI Stok_Hareket_EvrakDetayGetir_2(Evrak ev, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_HAREKETLERI tt = db.STOK_HAREKETLERIs.FirstOrDefault(T => (T.sth_evraktip == ev.EvrakTip && T.sth_tip == ev.Tip && T.sth_evrakno_seri == ev.SeriNo && T.sth_evrakno_sira == ev.SiraNo && T.sth_satirno == ev.SatirNo));
            return tt;

            }
        }

   
        public static CARI_HESAP_HAREKETLERI Cari_Hesap_Hareket_EvrakDetayGetir(Guid RECno, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            CARI_HESAP_HAREKETLERI tt = db.CARI_HESAP_HAREKETLERIs.Single(t => t.cha_Guid == RECno);

            return tt;

            }
        }

        public static CARI_HESAP_HAREKETLERI Cari_Hesap_Hareket_EvrakDetayGetir_2(Evrak ev, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            CARI_HESAP_HAREKETLERI tt = db.CARI_HESAP_HAREKETLERIs.FirstOrDefault(T => T.cha_evrak_tip == ev.EvrakTip && T.cha_tip == ev.Tip && T.cha_evrakno_seri == ev.SeriNo && T.cha_evrakno_sira == ev.SiraNo && T.cha_satir_no == ev.SatirNo);

            return tt;

            }
        }

        public static ODEME_EMIRLERI OdemeEmirleri_EvrakDetayGetir(byte odemetip, string refno, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            ODEME_EMIRLERI tt = db.ODEME_EMIRLERIs.FirstOrDefault(T => (T.sck_tip == odemetip && T.sck_refno == refno));

            return tt;
            }
        }


        public static SIPARISLER Siparis_EvrakDetayGetir(Guid RECno, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            SIPARISLER tt = db.SIPARISLERs.Single(t => t.sip_Guid == RECno);

            return tt;

            }
        }

        public static SIPARISLER Siparis_EvrakDetayGetir_2(Evrak ev, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            SIPARISLER tt = db.SIPARISLERs.First(T => (T.sip_tip == ev.EvrakTip && T.sip_cins == ev.Tip && T.sip_evrakno_seri == ev.SeriNo && T.sip_evrakno_sira == ev.SiraNo && T.sip_satirno == ev.SatirNo));

            return tt;
            }
        }

        public static SIPARISLER Siparisler_EvrakDetayGetir(Guid RECno, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            SIPARISLER tt = db.SIPARISLERs.Single(t => t.sip_Guid == RECno);

            return tt;

            }
        }


        public static CARI_HESAP_HAREKETLERI Cari_Hesap_Hareket_Kaydet(CARI_HESAP_HAREKETLERI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.cha_fis_sirano = 0;
            ch.cha_special1 = "BGS";
            db.CARI_HESAP_HAREKETLERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.cha_RECid_RECno = ch.cha_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        public static CARI_HESAP_HAREKETLERI Cari_Hesap_Hareket_Guncelle(CARI_HESAP_HAREKETLERI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            ch.cha_special1 = "BGS";
            db.CARI_HESAP_HAREKETLERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.cha_RECid_RECno = ch.cha_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

 
        public static SIPARISLER Siparis_Kaydet(SIPARISLER ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            ch.sip_special1 = "BGS";
            db.SIPARISLERs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.sip_RECid_RECno = ch.sip_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        public static SIPARISLER Siparis_Guncelle(SIPARISLER ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            ch.sip_special1 = "BGS";
            db.SIPARISLERs.Attach(ch, true);
            db.SubmitChanges();
            //ch.sip_RECid_RECno = ch.sip_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }


        public static STOK_HAREKETLERI Stok_Hareketleri_Kaydet(STOK_HAREKETLERI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            ch.sth_special1 = "BGS";
            db.STOK_HAREKETLERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.sth_RECid_RECno = ch.sth_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        public static STOK_HAREKETLERI Stok_Hareketleri_Guncelle(STOK_HAREKETLERI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            ch.sth_special1 = "BGS";
            db.STOK_HAREKETLERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.sth_RECid_RECno = ch.sth_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        public static ODEME_EMIRLERI OdemeEmirleri_Kaydet(ODEME_EMIRLERI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            ch.sck_special1 = "BGS";
            db.ODEME_EMIRLERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.sck_RECid_RECno = ch.sck_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }


        public static ODEME_EMIRLERI OdemeEmirleri_Guncelle(ODEME_EMIRLERI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            ch.sck_special1 = "BGS";
            db.ODEME_EMIRLERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.sck_RECid_RECno = ch.sck_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }


    }
}
