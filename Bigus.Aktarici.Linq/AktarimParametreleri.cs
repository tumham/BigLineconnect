using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bigus.Aktarici.Linq
{
    public class AktarimParametreleri
    {

        public static AKTARIM_PARAMETRELERI Parametre { get; set; }

        public static List<AKTARIM_SERILERI> AktarimSerileri { get; set; }


        public static List<AKTARIM_PARAMETRELERI> Aktarim_Parametrelerini_Yukle(string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
           
            List<AKTARIM_PARAMETRELERI> ls = new List<AKTARIM_PARAMETRELERI>();
            ls = (from T in db.AKTARIM_PARAMETRELERIs select T).ToList<AKTARIM_PARAMETRELERI>();
            return ls;
            }
        }


        public static AKTARIM_PARAMETRELERI Aktarim_Parametrelerini_Kaydet(AKTARIM_PARAMETRELERI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.AKTARIM_PARAMETRELERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
          
            return ch;

            }
        }

        public static AKTARIM_PARAMETRELERI Aktarim_Parametrelerini_Guncelle(AKTARIM_PARAMETRELERI ch, string conn)
        {
            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                db.AKTARIM_PARAMETRELERIs.Attach(ch, true);
                db.SubmitChanges();

                return ch;
            }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }


        public static List<AKTARIM_SERILERI> Aktarim_Serilerini_Yukle(string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<AKTARIM_SERILERI> ls = new List<AKTARIM_SERILERI>();
            ls = (from T in db.AKTARIM_SERILERIs select T).ToList<AKTARIM_SERILERI>();
            return ls;
            }
        }

        public static AKTARIM_SERILERI Aktarim_Serileri_EvrakDetayGetir(string seri, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            AKTARIM_SERILERI tt = db.AKTARIM_SERILERIs.FirstOrDefault(t => t.ser_serino == seri);

            return tt;

            }
        }

        public static AKTARIM_SERILERI Aktarim_Serilerini_Kaydet(AKTARIM_SERILERI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.AKTARIM_SERILERIs.InsertOnSubmit(ch);
            db.SubmitChanges();

            return ch;

            }
        }

        public static AKTARIM_SERILERI Aktarim_Serilerini_Guncelle(AKTARIM_SERILERI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.AKTARIM_SERILERIs.Attach(ch, true);
            db.SubmitChanges();

            return ch;

            }
        }

        public static void Aktarim_Serilerini_Sil(Int32 no,string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            AKTARIM_SERILERI WK = db.AKTARIM_SERILERIs.Single(t => t.ser_no == no);
            db.AKTARIM_SERILERIs.DeleteOnSubmit(WK);
            db.SubmitChanges();
            }
        }


        #region AKTARIM KULLANICILARI
        public static List<AKTARIM_KULLANICILARI> AktarimKullanicilari { get; set; }

        public static List<AKTARIM_KULLANICILARI> Aktarim_Kullanicilarini_Yukle(string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<AKTARIM_KULLANICILARI> ls = new List<AKTARIM_KULLANICILARI>();
            ls = (from T in db.AKTARIM_KULLANICILARIs select T).ToList<AKTARIM_KULLANICILARI>();

            return ls;
        
            }
        }

        public static List<KULLANICILAR> Kullanicilari_Yukle()
        {
            BigusAktarimDataContext db = new BigusAktarimDataContext(DatabaseFacade2.ConnectionString("MikroDB_V16"));

            List<KULLANICILAR> ls = new List<KULLANICILAR>();
            ls = (from T in db.KULLANICILARs select T).ToList<KULLANICILAR>();

            return ls;

        }

        public static AKTARIM_KULLANICILARI Aktarim_Kullanicilari_DetayGetir(Int32 kul_no, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            AKTARIM_KULLANICILARI tt = db.AKTARIM_KULLANICILARIs.FirstOrDefault(t => t.kul_no == kul_no);

            return tt;

            }
        }

        public static AKTARIM_KULLANICILARI Aktarim_Kullanicilari_Kaydet(AKTARIM_KULLANICILARI kul, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.AKTARIM_KULLANICILARIs.InsertOnSubmit(kul);
            db.SubmitChanges();

            return kul;

            }
        }

        public static void Aktarim_Kullanicilarini_Sil(Int32 no, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            AKTARIM_KULLANICILARI WK = db.AKTARIM_KULLANICILARIs.Single(t => t.kul_no == no);
            db.AKTARIM_KULLANICILARIs.DeleteOnSubmit(WK);
            db.SubmitChanges();
            }
        }

        #endregion


    }
}
