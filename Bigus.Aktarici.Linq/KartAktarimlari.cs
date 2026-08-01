using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Bigus.Aktarici.Linq
{
    public class KartAktarimlari
    {

        #region CARÝ HESAPLAR

      
        #region CARÝ HESAP KARTLARI

        public static List<CARI_HESAPLAR> Cari_Hesaplari_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<CARI_HESAPLAR> ls = new List<CARI_HESAPLAR>();

            ls = (from T in db.CARI_HESAPLARs select T).ToList<CARI_HESAPLAR>();

            return ls;
        
            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<CARI_HESAPLAR> Cari_Hesaplari_Yukle(DateTime Tarih1,DateTime Tarih2, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<CARI_HESAPLAR> ls = new List<CARI_HESAPLAR>();

            ls = (from T in db.CARI_HESAPLARs where T.cari_lastup_date >= Tarih1 && T.cari_lastup_date <= Tarih2 select T).ToList<CARI_HESAPLAR>();

            return ls;
            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }



        public static CARI_HESAPLAR Cari_Hesaplar_EvrakDetayGetir(string carikod, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            CARI_HESAPLAR tt = db.CARI_HESAPLARs.FirstOrDefault(t => t.cari_kod == carikod);
            return tt;
            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);              
                System.Threading.Thread.CurrentThread.Abort();
                return null;
              
            }
 

        }

        public static CARI_HESAPLAR Cari_Hesaplari_Kaydet(CARI_HESAPLAR ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.cari_RECid_RECno = RandomDondur();
            db.CARI_HESAPLARs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.cari_RECid_RECno = ch.cari_Guid;
            //db.SubmitChanges();
            return ch;

            }
        }

        public static CARI_HESAPLAR Cari_Hesaplari_Guncelle(CARI_HESAPLAR ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.cari_RECid_RECno = ch.cari_RECno;
            db.CARI_HESAPLARs.Attach(ch, true);
            db.SubmitChanges();
            //ch.cari_RECid_RECno = ch.cari_RECno;
            //db.SubmitChanges();
            return ch;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }
        #endregion

        #region CARÝ HESAP ADRES KARTLARI

        public static List<CARI_HESAP_ADRESLERI> Cari_Hesap_Adresleri_Yukle(string conn)
        {
            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<CARI_HESAP_ADRESLERI> ls = new List<CARI_HESAP_ADRESLERI>();

            ls = (from T in db.CARI_HESAP_ADRESLERIs select T).ToList<CARI_HESAP_ADRESLERI>();

            return ls;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<CARI_HESAP_ADRESLERI> Cari_Hesap_Adresleri_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<CARI_HESAP_ADRESLERI> ls = new List<CARI_HESAP_ADRESLERI>();

            ls = (from T in db.CARI_HESAP_ADRESLERIs where T.adr_lastup_date >= Tarih1 && T.adr_lastup_date <= Tarih2 select T).ToList<CARI_HESAP_ADRESLERI>();

            return ls;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static CARI_HESAP_ADRESLERI Cari_Hesap_Adresleri_EvrakDetayGetir(string carikod,Int32 adresno, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            CARI_HESAP_ADRESLERI tt = db.CARI_HESAP_ADRESLERIs.FirstOrDefault(t => t.adr_cari_kod == carikod && t.adr_adres_no == adresno);

            return tt;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static CARI_HESAP_ADRESLERI Cari_Hesap_Adresleri_Kaydet(CARI_HESAP_ADRESLERI ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.adr_RECid_RECno = RandomDondur();
            db.CARI_HESAP_ADRESLERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.adr_RECid_RECno = ch.adr_RECno;
            //db.SubmitChanges();
            return ch;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static CARI_HESAP_ADRESLERI Cari_Hesap_Adresleri_Guncelle(CARI_HESAP_ADRESLERI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.adr_RECid_RECno = ch.adr_RECno;
            db.CARI_HESAP_ADRESLERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.adr_RECid_RECno = ch.adr_RECno;
            //db.SubmitChanges();
            return ch;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }
        #endregion

        #region CARI_HESAP_BOLGELERI KARTLARI
      
        public static List<CARI_HESAP_BOLGELERI> Cari_Hesap_Bolgeleri_Yukle(string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<CARI_HESAP_BOLGELERI> ls = new List<CARI_HESAP_BOLGELERI>();

            ls = (from T in db.CARI_HESAP_BOLGELERIs select T).ToList<CARI_HESAP_BOLGELERI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<CARI_HESAP_BOLGELERI> Cari_Hesap_Bolgeleri_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<CARI_HESAP_BOLGELERI> ls = new List<CARI_HESAP_BOLGELERI>();

            ls = (from T in db.CARI_HESAP_BOLGELERIs where T.bol_lastup_date >= Tarih1 && T.bol_lastup_date <= Tarih2 select T).ToList<CARI_HESAP_BOLGELERI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static CARI_HESAP_BOLGELERI Cari_Hesap_Bolgeleri_EvrakDetayGetir(string bolkod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            CARI_HESAP_BOLGELERI tt = db.CARI_HESAP_BOLGELERIs.FirstOrDefault(t => t.bol_kod == bolkod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static CARI_HESAP_BOLGELERI Cari_Hesap_Bolgeleri_Kaydet(CARI_HESAP_BOLGELERI ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.bol_RECid_RECno = RandomDondur(); 
            db.CARI_HESAP_BOLGELERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.bol_RECid_RECno = ch.bol_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static CARI_HESAP_BOLGELERI Cari_Hesap_Bolgeleri_Guncelle(CARI_HESAP_BOLGELERI ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
           // ch.bol_RECid_RECno = ch.bol_RECno;
            db.CARI_HESAP_BOLGELERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.bol_RECid_RECno = ch.bol_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }
            
        #endregion

        #region CARÝ HESAP GRUPLARI KARTLARI

        
        public static List<CARI_HESAP_GRUPLARI> Cari_Hesap_Gruplari_Yukle(string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<CARI_HESAP_GRUPLARI> ls = new List<CARI_HESAP_GRUPLARI>();

            ls = (from T in db.CARI_HESAP_GRUPLARIs select T).ToList<CARI_HESAP_GRUPLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<CARI_HESAP_GRUPLARI> Cari_Hesap_Gruplari_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<CARI_HESAP_GRUPLARI> ls = new List<CARI_HESAP_GRUPLARI>();

            ls = (from T in db.CARI_HESAP_GRUPLARIs where T.crg_lastup_date >= Tarih1 && T.crg_lastup_date <= Tarih2 select T).ToList<CARI_HESAP_GRUPLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }



        }

        public static CARI_HESAP_GRUPLARI Cari_Hesap_Gruplari_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            CARI_HESAP_GRUPLARI tt = db.CARI_HESAP_GRUPLARIs.FirstOrDefault(t => t.crg_kod == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static CARI_HESAP_GRUPLARI Cari_Hesap_Gruplari_Kaydet(CARI_HESAP_GRUPLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.crg_RECid_RECno = RandomDondur();
            db.CARI_HESAP_GRUPLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.crg_RECid_RECno = ch.crg_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static CARI_HESAP_GRUPLARI Cari_Hesap_Gruplari_Guncelle(CARI_HESAP_GRUPLARI ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.crg_RECid_RECno = ch.crg_RECno;
            db.CARI_HESAP_GRUPLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.crg_RECid_RECno = ch.crg_RECno;
            //db.SubmitChanges();
            return ch;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }
        #endregion

        #region CARÝ HESAP YETKÝLÝLERÝ KARTLARI

        
        public static List<CARI_HESAP_YETKILILERI> Cari_Hesap_Yetkilileri_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<CARI_HESAP_YETKILILERI> ls = new List<CARI_HESAP_YETKILILERI>();

            ls = (from T in db.CARI_HESAP_YETKILILERIs select T).ToList<CARI_HESAP_YETKILILERI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }


        public static List<CARI_HESAP_YETKILILERI> Cari_Hesap_Yetkilileri_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<CARI_HESAP_YETKILILERI> ls = new List<CARI_HESAP_YETKILILERI>();

            ls = (from T in db.CARI_HESAP_YETKILILERIs where T.mye_lastup_date >= Tarih1 && T.mye_lastup_date <= Tarih2 select T).ToList<CARI_HESAP_YETKILILERI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static CARI_HESAP_YETKILILERI Cari_Hesap_Yetkilileri_EvrakDetayGetir(string carikod,Int32 adresno, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            CARI_HESAP_YETKILILERI tt = db.CARI_HESAP_YETKILILERIs.FirstOrDefault(t => t.mye_cari_kod == carikod && t.mye_adres_no==adresno);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static CARI_HESAP_YETKILILERI Cari_Hesap_Yetkilileri_Kaydet(CARI_HESAP_YETKILILERI ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.mye_RECid_RECno = RandomDondur();
            db.CARI_HESAP_YETKILILERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.mye_RECid_RECno = ch.mye_RECno;
            //db.SubmitChanges();
            return ch;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static CARI_HESAP_YETKILILERI Cari_Hesap_Yetkilileri_Guncelle(CARI_HESAP_YETKILILERI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.mye_RECid_RECno = ch.mye_RECno;
            db.CARI_HESAP_YETKILILERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.mye_RECid_RECno = ch.mye_RECno;
            //db.SubmitChanges();
            return ch;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }
      
        
        #endregion

        #region CARÝ PERSONEL TANIMLARI

        public static List<CARI_PERSONEL_TANIMLARI> Cari_Personel_Tanimlari_Yukle(string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<CARI_PERSONEL_TANIMLARI> ls = new List<CARI_PERSONEL_TANIMLARI>();

            ls = (from T in db.CARI_PERSONEL_TANIMLARIs select T).ToList<CARI_PERSONEL_TANIMLARI>();

            return ls;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<CARI_PERSONEL_TANIMLARI> Cari_Personel_Tanimlari_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<CARI_PERSONEL_TANIMLARI> ls = new List<CARI_PERSONEL_TANIMLARI>();

            ls = (from T in db.CARI_PERSONEL_TANIMLARIs where T.cari_per_lastup_date >= Tarih1 && T.cari_per_lastup_date <= Tarih2 select T).ToList<CARI_PERSONEL_TANIMLARI>();

            return ls;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static CARI_PERSONEL_TANIMLARI Cari_Personel_Tanimlari_EvrakDetayGetir(string kod, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            CARI_PERSONEL_TANIMLARI tt = db.CARI_PERSONEL_TANIMLARIs.FirstOrDefault(t => t.cari_per_kod == kod);

            return tt;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static CARI_PERSONEL_TANIMLARI Cari_Personel_Tanimlari_Kaydet(CARI_PERSONEL_TANIMLARI ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.cari_per_RECid_RECno = RandomDondur();
            db.CARI_PERSONEL_TANIMLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.cari_per_RECid_RECno = ch.cari_per_RECno;
            //db.SubmitChanges();
            return ch;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static CARI_PERSONEL_TANIMLARI Cari_Personel_Tanimlari_Guncelle(CARI_PERSONEL_TANIMLARI ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.cari_per_RECid_RECno = ch.cari_per_RECno;
            db.CARI_PERSONEL_TANIMLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.cari_per_RECid_RECno = ch.cari_per_RECno;
            //db.SubmitChanges();
            return ch;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }


        #endregion

        // YOK
        #region CARÝ HESAP TAÞIT PLAKALARI

        
        public static List<CARI_HESAP_TASIT_PLAKALARI> Cari_Hesap_Tasit_Plakalari_Yukle(string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<CARI_HESAP_TASIT_PLAKALARI> ls = new List<CARI_HESAP_TASIT_PLAKALARI>();

            ls = (from T in db.CARI_HESAP_TASIT_PLAKALARIs select T).ToList<CARI_HESAP_TASIT_PLAKALARI>();

            return ls;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static CARI_HESAP_TASIT_PLAKALARI Cari_Hesap_Tasit_Plakalari_EvrakDetayGetir(string kod, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            CARI_HESAP_TASIT_PLAKALARI tt = db.CARI_HESAP_TASIT_PLAKALARIs.FirstOrDefault(t => t.plk_kod == kod);

            return tt;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static CARI_HESAP_TASIT_PLAKALARI Cari_Hesap_Tasit_Plakalari_Kaydet(CARI_HESAP_TASIT_PLAKALARI ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.plk_RECid_RECno = RandomDondur();
            db.CARI_HESAP_TASIT_PLAKALARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.plk_RECid_RECno = ch.plk_RECno;
            //db.SubmitChanges();
            return ch;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static CARI_HESAP_TASIT_PLAKALARI Cari_Hesap_Tasit_Plakalari_Guncelle(CARI_HESAP_TASIT_PLAKALARI ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.plk_RECid_RECno = ch.plk_RECno;
            db.CARI_HESAP_TASIT_PLAKALARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.plk_RECid_RECno  = ch.plk_RECno;
            //db.SubmitChanges();
            return ch;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }
        #endregion  // 

        // YOK
        #region CARÝ MUSTAHSÝL TANIMLARI 

        public static List<CARI_MUSTAHSIL_TANIMLARI> Cari_Mustahsil_Tanimlari_Yukle(string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<CARI_MUSTAHSIL_TANIMLARI> ls = new List<CARI_MUSTAHSIL_TANIMLARI>();

            ls = (from T in db.CARI_MUSTAHSIL_TANIMLARIs select T).ToList<CARI_MUSTAHSIL_TANIMLARI>();

            return ls;

            }
        }

        public static CARI_MUSTAHSIL_TANIMLARI Cari_Mustahsil_Tanimlari_EvrakDetayGetir(string carikod, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            CARI_MUSTAHSIL_TANIMLARI tt = db.CARI_MUSTAHSIL_TANIMLARIs.FirstOrDefault(t => t.Cm_carikodu == carikod);

            return tt;

            }
        }

        public static CARI_MUSTAHSIL_TANIMLARI Cari_Mustahsil_Tanimlari_Kaydet(CARI_MUSTAHSIL_TANIMLARI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.CARI_MUSTAHSIL_TANIMLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.Cm_RECid_RECno = ch.Cm_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        public static CARI_MUSTAHSIL_TANIMLARI Cari_Mustahsil_Tanimlari_Guncelle(CARI_MUSTAHSIL_TANIMLARI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.CARI_MUSTAHSIL_TANIMLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.Cm_RECid_RECno = ch.Cm_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }
       
        #endregion

        #endregion
      
        #region STOKLAR

        #region STOK TANITIM KARTLARI

        public static List<STOKLAR> Stoklari_Yukle(string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOKLAR> ls = new List<STOKLAR>();

            ls = (from T in db.STOKLARs select T).ToList<STOKLAR>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static List<STOKLAR> Stoklari_Yukle(DateTime Tarih1, DateTime Tarih2,string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOKLAR> ls = new List<STOKLAR>();

            ls = (from T in db.STOKLARs where T.sto_lastup_date >= Tarih1 && T.sto_lastup_date <= Tarih2 select T).ToList<STOKLAR>();

            return ls;
            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static STOKLAR Stoklar_EvrakDetayGetir(string kod, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOKLAR tt = db.STOKLARs.FirstOrDefault(t => t.sto_kod == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOKLAR Stoklari_Kaydet(STOKLAR ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.sto_RECid_RECno = RandomDondur();
            db.STOKLARs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.sto_RECid_RECno = ch.sto_RECno;
            //db.SubmitChanges();
            return ch;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOKLAR Stoklari_Guncelle(STOKLAR ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.sto_RECid_RECno = ch.sto_RECno;
            db.STOKLARs.Attach(ch, true);
            db.SubmitChanges();
            //ch.sto_RECid_RECno = ch.sto_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region STOK SERÝNO TANIMLARI


        public static List<STOK_SERINO_TANIMLARI> Stok_SeriNo_Tanimlarini_Yukle(string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_SERINO_TANIMLARI> ls = new List<STOK_SERINO_TANIMLARI>();

            ls = (from T in db.STOK_SERINO_TANIMLARIs select T).ToList<STOK_SERINO_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }


        public static List<STOK_SERINO_TANIMLARI> Stok_SeriNo_Tanimlarini_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_SERINO_TANIMLARI> ls = new List<STOK_SERINO_TANIMLARI>();

            ls = (from T in db.STOK_SERINO_TANIMLARIs where T.chz_lastup_date >= Tarih1 && T.chz_lastup_date <= Tarih2 select T).ToList<STOK_SERINO_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_SERINO_TANIMLARI Stok_SeriNo_Tanimlari_EvrakDetayGetir(string stokkod, string serino, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_SERINO_TANIMLARI tt = db.STOK_SERINO_TANIMLARIs.FirstOrDefault(t => t.chz_stok_kodu == stokkod && t.chz_serino == serino);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }



        public static STOK_SERINO_TANIMLARI Stok_SeriNo_Tanimlari_Kaydet(STOK_SERINO_TANIMLARI ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.chz_RECid_RECno = RandomDondur();
            db.STOK_SERINO_TANIMLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.chz_RECid_RECno = ch.chz_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static STOK_SERINO_TANIMLARI Stok_SeriNo_Tanimlari_Guncelle(STOK_SERINO_TANIMLARI ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.chz_RECid_RECno = ch.chz_RECno;
            db.STOK_SERINO_TANIMLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.chz_RECid_RECno = ch.chz_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }



        #endregion
       
        #region STOK SARF RECETELERÝ KARTLARI

        public static List<STOK_SARF_RECETELERI> Stok_Sarf_Receteleri_Yukle(string conn)
        {
            try

            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_SARF_RECETELERI> ls = new List<STOK_SARF_RECETELERI>();

            ls = (from T in db.STOK_SARF_RECETELERIs select T).ToList<STOK_SARF_RECETELERI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<STOK_SARF_RECETELERI> Stok_Sarf_Receteleri_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_SARF_RECETELERI> ls = new List<STOK_SARF_RECETELERI>();

            ls = (from T in db.STOK_SARF_RECETELERIs where T.sr_lastup_date >= Tarih1 && T.sr_lastup_date <= Tarih2 select T).ToList<STOK_SARF_RECETELERI>();

            return ls;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_SARF_RECETELERI Stok_Sarf_Receteleri_EvrakDetayGetir(string kod,Int32 satirno, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_SARF_RECETELERI tt = db.STOK_SARF_RECETELERIs.FirstOrDefault(t => t.sr_anakod  == kod && t.sr_satirno == satirno);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static STOK_SARF_RECETELERI Stok_Sarf_Receteleri_Kaydet(STOK_SARF_RECETELERI ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.sr_sr_id_sr_no = RandomDondur();
            db.STOK_SARF_RECETELERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.sr_sr_id_sr_no = ch.sr_sr_no;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_SARF_RECETELERI Stok_Sarf_Receteleri_Guncelle(STOK_SARF_RECETELERI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.sr_sr_id_sr_no = ch.sr_sr_no;
            db.STOK_SARF_RECETELERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.sr_sr_id_sr_no = ch.sr_sr_no;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region STOK PRÝM TANITIM  KARTLARI

      
        public static List<STOK_PRIM_TANIMLARI> Stok_Prim_Tanimlari_Yukle(string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_PRIM_TANIMLARI> ls = new List<STOK_PRIM_TANIMLARI>();

            ls = (from T in db.STOK_PRIM_TANIMLARIs select T).ToList<STOK_PRIM_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<STOK_PRIM_TANIMLARI> Stok_Prim_Tanimlari_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_PRIM_TANIMLARI> ls = new List<STOK_PRIM_TANIMLARI>();

            ls = (from T in db.STOK_PRIM_TANIMLARIs where T.prim_lastup_date >= Tarih1 && T.prim_lastup_date <= Tarih2 select T).ToList<STOK_PRIM_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_PRIM_TANIMLARI Stok_Prim_Tanimlari_EvrakDetayGetir(string kod,Int32 satirno, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_PRIM_TANIMLARI tt = db.STOK_PRIM_TANIMLARIs.FirstOrDefault(t => t.prim_kod == kod && t.prim_satirno == satirno);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_PRIM_TANIMLARI Stok_Prim_Tanimlari_Kaydet(STOK_PRIM_TANIMLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.prim_RECid_RECno = RandomDondur();
            db.STOK_PRIM_TANIMLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.prim_RECid_RECno = ch.prim_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_PRIM_TANIMLARI Stok_Prim_Tanimlari_Guncelle(STOK_PRIM_TANIMLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.prim_RECid_RECno = ch.prim_RECno;
            db.STOK_PRIM_TANIMLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.prim_RECid_RECno = ch.prim_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region STOK PAKET TANITIM KARTLARI

        public static List<STOK_PAKET_TANIMLARI> Stok_Paket_Tanimlari_Yukle(string conn)
        {

            try
            {


            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_PAKET_TANIMLARI> ls = new List<STOK_PAKET_TANIMLARI>();

            ls = (from T in db.STOK_PAKET_TANIMLARIs select T).ToList<STOK_PAKET_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<STOK_PAKET_TANIMLARI> Stok_Paket_Tanimlari_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_PAKET_TANIMLARI> ls = new List<STOK_PAKET_TANIMLARI>();

            ls = (from T in db.STOK_PAKET_TANIMLARIs where T.pak_lastup_date >= Tarih1 && T.pak_lastup_date <= Tarih2 select T).ToList<STOK_PAKET_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_PAKET_TANIMLARI Stok_Paket_Tanimlari_EvrakDetayGetir(string kod,Int32 satirno, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_PAKET_TANIMLARI tt = db.STOK_PAKET_TANIMLARIs.FirstOrDefault(t => t.pak_kod == kod && t.pak_satirno == satirno);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_PAKET_TANIMLARI Stok_Paket_Tanimlari_Kaydet(STOK_PAKET_TANIMLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.pak_RECid_RECno = RandomDondur();
            db.STOK_PAKET_TANIMLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.pak_RECid_RECno = ch.pak_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_PAKET_TANIMLARI Stok_Paket_Tanimlari_Guncelle(STOK_PAKET_TANIMLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.pak_RECid_RECno = ch.pak_RECno;
            db.STOK_PAKET_TANIMLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.pak_RECid_RECno = ch.pak_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region STOK DEPO DETAYLARI KARTLARI

        
        public static List<STOK_DEPO_DETAYLARI> Stok_Depo_Detaylari_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_DEPO_DETAYLARI> ls = new List<STOK_DEPO_DETAYLARI>();

            ls = (from T in db.STOK_DEPO_DETAYLARIs select T).ToList<STOK_DEPO_DETAYLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<STOK_DEPO_DETAYLARI> Stok_Depo_Detaylari_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_DEPO_DETAYLARI> ls = new List<STOK_DEPO_DETAYLARI>();

            ls = (from T in db.STOK_DEPO_DETAYLARIs where T.sdp_lastup_date >= Tarih1 && T.sdp_lastup_date <= Tarih2 select T).ToList<STOK_DEPO_DETAYLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_DEPO_DETAYLARI Stok_Depo_Detaylari_EvrakDetayGetir(string kod,Int32 depono, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_DEPO_DETAYLARI tt = db.STOK_DEPO_DETAYLARIs.FirstOrDefault(t => t.sdp_depo_kod == kod && t.sdp_depo_no==depono);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_DEPO_DETAYLARI Stok_Depo_Detaylari_Kaydet(STOK_DEPO_DETAYLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.sdp_RECid_RECno = RandomDondur();
            db.STOK_DEPO_DETAYLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.sdp_RECid_RECno = ch.sdp_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_DEPO_DETAYLARI Stok_Depo_Detaylari_Guncelle(STOK_DEPO_DETAYLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.sdp_RECid_RECno = ch.sdp_RECno;
            db.STOK_DEPO_DETAYLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.sdp_RECid_RECno = ch.sdp_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region STOK CARÝ ÝSKONTO TANIMLARI KARTLARI

        
        public static List<STOK_CARI_ISKONTO_TANIMLARI> Stok_Cari_Iskonto_Tanimlari_Yukle(string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_CARI_ISKONTO_TANIMLARI> ls = new List<STOK_CARI_ISKONTO_TANIMLARI>();

            ls = (from T in db.STOK_CARI_ISKONTO_TANIMLARIs select T).ToList<STOK_CARI_ISKONTO_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }
        public static List<STOK_CARI_ISKONTO_TANIMLARI> Stok_Cari_Iskonto_Tanimlari_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_CARI_ISKONTO_TANIMLARI> ls = new List<STOK_CARI_ISKONTO_TANIMLARI>();

            ls = (from T in db.STOK_CARI_ISKONTO_TANIMLARIs where T.isk_lastup_date >= Tarih1 && T.isk_lastup_date <= Tarih2 select T).ToList<STOK_CARI_ISKONTO_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_CARI_ISKONTO_TANIMLARI Stok_Cari_Iskonto_Tanimlari_EvrakDetayGetir(string stokkod,string carikod,Int32 odemeplani, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_CARI_ISKONTO_TANIMLARI tt = db.STOK_CARI_ISKONTO_TANIMLARIs.FirstOrDefault(t => t.isk_stok_kod == stokkod && t.isk_cari_kod == carikod && t.isk_uygulama_odeme_plani == odemeplani);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_CARI_ISKONTO_TANIMLARI Stok_Cari_Iskonto_Tanimlari_Kaydet(STOK_CARI_ISKONTO_TANIMLARI ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.isk_RECid_RECno = RandomDondur();
            db.STOK_CARI_ISKONTO_TANIMLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.isk_RECid_RECno = ch.isk_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_CARI_ISKONTO_TANIMLARI Stok_Cari_Iskonto_Tanimlari_Guncelle(STOK_CARI_ISKONTO_TANIMLARI ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.isk_RECid_RECno = ch.isk_RECno;
            db.STOK_CARI_ISKONTO_TANIMLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.isk_RECid_RECno = ch.isk_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        // YOK
        #region RAKÝP STOK DEPO DETAYLARI KARTLARI
        
        public static List<RAKIP_STOK_DEPO_DETAYLARI> Rakip_Stok_Depo_Detaylari_Yukle(string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<RAKIP_STOK_DEPO_DETAYLARI> ls = new List<RAKIP_STOK_DEPO_DETAYLARI>();

            ls = (from T in db.RAKIP_STOK_DEPO_DETAYLARIs select T).ToList<RAKIP_STOK_DEPO_DETAYLARI>();

            return ls;

            }
        }

        public static RAKIP_STOK_DEPO_DETAYLARI Rakip_Stok_Depo_Detaylari_EvrakDetayGetir(string kod,Int32 depono, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            RAKIP_STOK_DEPO_DETAYLARI tt = db.RAKIP_STOK_DEPO_DETAYLARIs.FirstOrDefault(t => t.rsdp_depo_kod == kod && t.rsdp_depo_no == depono );

            return tt;

            }
        }

        public static RAKIP_STOK_DEPO_DETAYLARI Rakip_Stok_Depo_Detaylari_Kaydet(RAKIP_STOK_DEPO_DETAYLARI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.RAKIP_STOK_DEPO_DETAYLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.rsdp_RECid_RECno = ch.rsdp_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        public static RAKIP_STOK_DEPO_DETAYLARI Rakip_Stok_Depo_Detaylari_Guncelle(RAKIP_STOK_DEPO_DETAYLARI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.RAKIP_STOK_DEPO_DETAYLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.rsdp_RECid_RECno = ch.rsdp_RECno;
            //db.SubmitChanges();
            return ch;
            }
        }

        #endregion

        // YOK
        #region RAKÝP STOK TANITIM KARTLARI
        
        public static List<RAKIP_STOKLAR> Rakip_Stoklari_Yukle(string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<RAKIP_STOKLAR> ls = new List<RAKIP_STOKLAR>();

            ls = (from T in db.RAKIP_STOKLARs select T).ToList<RAKIP_STOKLAR>();

            return ls;

            }
        }

        public static RAKIP_STOKLAR Rakip_Stoklari_EvrakDetayGetir(string kod, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            RAKIP_STOKLAR tt = db.RAKIP_STOKLARs.FirstOrDefault(t => t.raks_kod == kod);

            return tt;

            }
        }

        public static RAKIP_STOKLAR Rakip_Stoklari_Kaydet(RAKIP_STOKLAR ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.RAKIP_STOKLARs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.raks_RECid_RECno = ch.raks_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        public static RAKIP_STOKLAR Rakip_Stoklari_Guncelle(RAKIP_STOKLAR ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.RAKIP_STOKLARs.Attach(ch, true);
            db.SubmitChanges();
            //ch.raks_RECid_RECno = ch.raks_RECno;
            //db.SubmitChanges();
            return ch;
            }
        }

        #endregion

        #region STOK ALT GRUPLARI KARTLARI
        
        public static List<STOK_ALT_GRUPLARI> Stok_Alt_Gruplari_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_ALT_GRUPLARI> ls = new List<STOK_ALT_GRUPLARI>();

            ls = (from T in db.STOK_ALT_GRUPLARIs select T).ToList<STOK_ALT_GRUPLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<STOK_ALT_GRUPLARI> Stok_Alt_Gruplari_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_ALT_GRUPLARI> ls = new List<STOK_ALT_GRUPLARI>();

            ls = (from T in db.STOK_ALT_GRUPLARIs where T.sta_lastup_date >= Tarih1 && T.sta_lastup_date <= Tarih2  select T).ToList<STOK_ALT_GRUPLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static STOK_ALT_GRUPLARI Stok_Alt_Gruplari_EvrakDetayGetir(string kod,string ana_grup_kod,string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_ALT_GRUPLARI tt = db.STOK_ALT_GRUPLARIs.FirstOrDefault(t => t.sta_kod == kod && t.sta_ana_grup_kod == ana_grup_kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_ALT_GRUPLARI Stok_Alt_Gruplari_Kaydet(STOK_ALT_GRUPLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.sta_RECid_RECno = RandomDondur();
            db.STOK_ALT_GRUPLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.sta_RECid_RECno = ch.sta_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_ALT_GRUPLARI Stok_Alt_Gruplari_Guncelle(STOK_ALT_GRUPLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.sta_RECid_RECno = ch.sta_RECno;
            db.STOK_ALT_GRUPLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.sta_RECid_RECno = ch.sta_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region STOK ANA GRUPLARI TANITIM KARTLARI

        
        public static List<STOK_ANA_GRUPLARI> Stok_Ana_Gruplari_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_ANA_GRUPLARI> ls = new List<STOK_ANA_GRUPLARI>();

            ls = (from T in db.STOK_ANA_GRUPLARIs select T).ToList<STOK_ANA_GRUPLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static List<STOK_ANA_GRUPLARI> Stok_Ana_Gruplari_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_ANA_GRUPLARI> ls = new List<STOK_ANA_GRUPLARI>();

            ls = (from T in db.STOK_ANA_GRUPLARIs where T.san_lastup_date >= Tarih1 && T.san_lastup_date <= Tarih2 select T).ToList<STOK_ANA_GRUPLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_ANA_GRUPLARI Stok_Ana_Gruplari_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_ANA_GRUPLARI tt = db.STOK_ANA_GRUPLARIs.FirstOrDefault(t => t.san_kod == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_ANA_GRUPLARI Stok_Ana_Gruplari_Kaydet(STOK_ANA_GRUPLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.san_RECid_RECno = RandomDondur();
            db.STOK_ANA_GRUPLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.san_RECid_RECno = ch.san_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_ANA_GRUPLARI Stok_Ana_Gruplari_Guncelle(STOK_ANA_GRUPLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.san_RECid_RECno = ch.san_RECno;
            db.STOK_ANA_GRUPLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.san_RECid_RECno = ch.san_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region STOK AMBALAJLARI KARTLARI

        public static List<STOK_AMBALAJLARI> Stok_Ambalajlari_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_AMBALAJLARI> ls = new List<STOK_AMBALAJLARI>();

            ls = (from T in db.STOK_AMBALAJLARIs select T).ToList<STOK_AMBALAJLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<STOK_AMBALAJLARI> Stok_Ambalajlari_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_AMBALAJLARI> ls = new List<STOK_AMBALAJLARI>();

            ls = (from T in db.STOK_AMBALAJLARIs where T.amb_lastup_date >= Tarih1 && T.amb_lastup_date <= Tarih2 select T).ToList<STOK_AMBALAJLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_AMBALAJLARI Stok_Ambalajlari_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_AMBALAJLARI tt = db.STOK_AMBALAJLARIs.FirstOrDefault(t => t.amb_kod == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_AMBALAJLARI Stok_Ambalajlari_Kaydet(STOK_AMBALAJLARI ch, string conn)
        {

            try
            {
            //ch.amb_RECid_RECno = RandomDondur();
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.STOK_AMBALAJLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.amb_RECid_RECno = ch.amb_RECno;
            db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_AMBALAJLARI Stok_Ambalajlari_Guncelle(STOK_AMBALAJLARI ch, string conn)
        {
            try
            {

            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.amb_RECid_RECno = ch.amb_RECno;
            db.STOK_AMBALAJLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.amb_RECid_RECno = ch.amb_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        #endregion

        #region STOK ANA HAMMADDELERI KARTLARI

        public static List<STOK_ANAHAMMADDELERI> Stok_Anahammaddeleri_Yukle(string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_ANAHAMMADDELERI> ls = new List<STOK_ANAHAMMADDELERI>();

            ls = (from T in db.STOK_ANAHAMMADDELERIs select T).ToList<STOK_ANAHAMMADDELERI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static List<STOK_ANAHAMMADDELERI> Stok_Anahammaddeleri_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_ANAHAMMADDELERI> ls = new List<STOK_ANAHAMMADDELERI>();

            ls = (from T in db.STOK_ANAHAMMADDELERIs where T.ahm_lastup_date >= Tarih1 && T.ahm_lastup_date <= Tarih2 select T).ToList<STOK_ANAHAMMADDELERI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }


        public static STOK_ANAHAMMADDELERI Stok_Anahammaddeleri_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_ANAHAMMADDELERI tt = db.STOK_ANAHAMMADDELERIs.FirstOrDefault(t => t.ahm_kodu == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_ANAHAMMADDELERI Stok_Anahammaddeleri_Kaydet(STOK_ANAHAMMADDELERI ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.ahm_RECid_RECno = ch.ahm_RECno;
            db.STOK_ANAHAMMADDELERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.ahm_RECid_RECno = ch.ahm_RECno;
            //db.SubmitChanges();
            return ch;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_ANAHAMMADDELERI Stok_Anahammaddeleri_Guncelle(STOK_ANAHAMMADDELERI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.ahm_RECid_RECno = ch.ahm_RECno;
            db.STOK_ANAHAMMADDELERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.ahm_RECid_RECno = ch.ahm_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region STOK BEDEN TANIMLARI KARTLARI
        
        public static List<STOK_BEDEN_TANIMLARI> Stok_Beden_Tanimlari_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_BEDEN_TANIMLARI> ls = new List<STOK_BEDEN_TANIMLARI>();

            ls = (from T in db.STOK_BEDEN_TANIMLARIs select T).ToList<STOK_BEDEN_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<STOK_BEDEN_TANIMLARI> Stok_Beden_Tanimlari_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_BEDEN_TANIMLARI> ls = new List<STOK_BEDEN_TANIMLARI>();

            ls = (from T in db.STOK_BEDEN_TANIMLARIs where T.bdn_lastup_date >= Tarih1 && T.bdn_lastup_date <= Tarih2 select T).ToList<STOK_BEDEN_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_BEDEN_TANIMLARI Stok_Beden_Tanimlari_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_BEDEN_TANIMLARI tt = db.STOK_BEDEN_TANIMLARIs.FirstOrDefault(t => t.bdn_kodu == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static STOK_BEDEN_TANIMLARI Stok_Beden_Tanimlari_Kaydet(STOK_BEDEN_TANIMLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.bdn_RECid_RECno = ch.bdn_RECno;
            db.STOK_BEDEN_TANIMLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.bdn_RECid_RECno = ch.bdn_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_BEDEN_TANIMLARI Stok_Beden_Tanimlari_Guncelle(STOK_BEDEN_TANIMLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.bdn_RECid_RECno = ch.bdn_RECno;
            db.STOK_BEDEN_TANIMLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.bdn_RECid_RECno = ch.bdn_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region STOK KALÝTE KONTROL TANIMLARI KARTLARI

        public static List<STOK_KALITE_KONTROL_TANIMLARI> Stok_Kalite_Kontrol_Tanimlari_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try

            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_KALITE_KONTROL_TANIMLARI> ls = new List<STOK_KALITE_KONTROL_TANIMLARI>();

            ls = (from T in db.STOK_KALITE_KONTROL_TANIMLARIs where T.KKon_lastup_date >= Tarih1 && T.KKon_lastup_date <= Tarih2 select T).ToList<STOK_KALITE_KONTROL_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<STOK_KALITE_KONTROL_TANIMLARI> Stok_Kalite_Kontrol_Tanimlari_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_KALITE_KONTROL_TANIMLARI> ls = new List<STOK_KALITE_KONTROL_TANIMLARI>();

            ls = (from T in db.STOK_KALITE_KONTROL_TANIMLARIs select T).ToList<STOK_KALITE_KONTROL_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_KALITE_KONTROL_TANIMLARI Stok_Kalite_Kontrol_Tanimlari_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_KALITE_KONTROL_TANIMLARI tt = db.STOK_KALITE_KONTROL_TANIMLARIs.FirstOrDefault(t => t.KKon_kod == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_KALITE_KONTROL_TANIMLARI Stok_Kalite_Kontrol_Tanimlari_Kaydet(STOK_KALITE_KONTROL_TANIMLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.KKon_RECid_RECno = RandomDondur();
            db.STOK_KALITE_KONTROL_TANIMLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.KKon_RECid_RECno = ch.KKon_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_KALITE_KONTROL_TANIMLARI Stok_Kalite_Kontrol_Tanimlari_Guncelle(STOK_KALITE_KONTROL_TANIMLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.KKon_RECid_RECno = ch.KKon_RECno;
            db.STOK_KALITE_KONTROL_TANIMLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.KKon_RECid_RECno = ch.KKon_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region STOK KALKON KARTLARI
        
        public static List<STOK_KALKON> Stok_Kalkon_Yukle(string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_KALKON> ls = new List<STOK_KALKON>();

            ls = (from T in db.STOK_KALKONs select T).ToList<STOK_KALKON>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<STOK_KALKON> Stok_Kalkon_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_KALKON> ls = new List<STOK_KALKON>();

            ls = (from T in db.STOK_KALKONs where T.skk_lastup_date >= Tarih1 && T.skk_lastup_date <= Tarih2 select T).ToList<STOK_KALKON>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_KALKON Stok_Kalkon_EvrakDetayGetir(string kod, byte tip, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_KALKON tt = db.STOK_KALKONs.FirstOrDefault(t => t.skk_kodu == kod && t.skk_tipi==tip);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_KALKON Stok_Kalkon_Kaydet(STOK_KALKON ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.skk_RECid_RECno = RandomDondur();
            db.STOK_KALKONs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.skk_RECid_RECno = ch.skk_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_KALKON Stok_Kalkon_Guncelle(STOK_KALKON ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.skk_RECid_RECno = ch.skk_RECno;
            db.STOK_KALKONs.Attach(ch, true);
            db.SubmitChanges();
            //ch.skk_RECid_RECno = ch.skk_RECno;
            //db.SubmitChanges();
            return ch;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region STOK KATEGORÝLERÝ KARTLARI
        
        public static List<STOK_KATEGORILERI> Stok_Kategorileri_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_KATEGORILERI> ls = new List<STOK_KATEGORILERI>();

            ls = (from T in db.STOK_KATEGORILERIs select T).ToList<STOK_KATEGORILERI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<STOK_KATEGORILERI> Stok_Kategorileri_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_KATEGORILERI> ls = new List<STOK_KATEGORILERI>();

            ls = (from T in db.STOK_KATEGORILERIs where T.ktg_lastup_date >= Tarih1 && T.ktg_lastup_date <= Tarih2 select T).ToList<STOK_KATEGORILERI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_KATEGORILERI Stok_Kategorileri_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_KATEGORILERI tt = db.STOK_KATEGORILERIs.FirstOrDefault(t => t.ktg_kod == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_KATEGORILERI Stok_Kategorileri_Kaydet(STOK_KATEGORILERI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.ktg_RECid_RECno = RandomDondur();
            db.STOK_KATEGORILERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.ktg_RECid_RECno = ch.ktg_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_KATEGORILERI Stok_Kategorileri_Guncelle(STOK_KATEGORILERI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.ktg_RECid_RECno = ch.ktg_RECno;
            db.STOK_KATEGORILERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.ktg_RECid_RECno = ch.ktg_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region STOK MARKALARI KARTLARI
        
        public static List<STOK_MARKALARI> Stok_Markalari_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_MARKALARI> ls = new List<STOK_MARKALARI>();

            ls = (from T in db.STOK_MARKALARIs select T).ToList<STOK_MARKALARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<STOK_MARKALARI> Stok_Markalari_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_MARKALARI> ls = new List<STOK_MARKALARI>();

            ls = (from T in db.STOK_MARKALARIs where T.mrk_lastup_date >= Tarih1 && T.mrk_lastup_date <= Tarih2 select T).ToList<STOK_MARKALARI>();
            
            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_MARKALARI Stok_Markalari_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_MARKALARI tt = db.STOK_MARKALARIs.FirstOrDefault(t => t.mrk_kod == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_MARKALARI Stok_Markalari_Kaydet(STOK_MARKALARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.mrk_RECid_RECno = RandomDondur();
            db.STOK_MARKALARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.mrk_RECid_RECno = ch.mrk_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static STOK_MARKALARI Stok_Markalari_Guncelle(STOK_MARKALARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.mrk_RECid_RECno = ch.mrk_RECno;
            db.STOK_MARKALARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.mrk_RECid_RECno = ch.mrk_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }



        #endregion

        #region STOK MODEL TANIMLARI KARTLARI
        
        public static List<STOK_MODEL_TANIMLARI> Stok_Model_Tanimlari_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_MODEL_TANIMLARI> ls = new List<STOK_MODEL_TANIMLARI>();

            ls = (from T in db.STOK_MODEL_TANIMLARIs select T).ToList<STOK_MODEL_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<STOK_MODEL_TANIMLARI> Stok_Model_Tanimlari_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_MODEL_TANIMLARI> ls = new List<STOK_MODEL_TANIMLARI>();

            ls = (from T in db.STOK_MODEL_TANIMLARIs where T.mdl_lastup_date >= Tarih1 && T.mdl_lastup_date <= Tarih2 select T).ToList<STOK_MODEL_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
             
        }

        public static STOK_MODEL_TANIMLARI Stok_Model_Tanimlari_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_MODEL_TANIMLARI tt = db.STOK_MODEL_TANIMLARIs.FirstOrDefault(t => t.mdl_kodu == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_MODEL_TANIMLARI Stok_Model_Tanimlari_Kaydet(STOK_MODEL_TANIMLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.mdl_RECid_RECno = RandomDondur();
            db.STOK_MODEL_TANIMLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.mdl_RECid_RECno = ch.mdl_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_MODEL_TANIMLARI Stok_Model_Tanimlari_Guncelle(STOK_MODEL_TANIMLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.mdl_RECid_RECno = ch.mdl_RECno;
            db.STOK_MODEL_TANIMLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.mdl_RECid_RECno = ch.mdl_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region STOK MUHASEBE GRUP TANITIM KARTLARI
        
        public static List<STOK_MUHASEBE_GRUPLARI> Stok_Muhasebe_Gruplari_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_MUHASEBE_GRUPLARI> ls = new List<STOK_MUHASEBE_GRUPLARI>();

            ls = (from T in db.STOK_MUHASEBE_GRUPLARIs select T).ToList<STOK_MUHASEBE_GRUPLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<STOK_MUHASEBE_GRUPLARI> Stok_Muhasebe_Gruplari_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_MUHASEBE_GRUPLARI> ls = new List<STOK_MUHASEBE_GRUPLARI>();

            ls = (from T in db.STOK_MUHASEBE_GRUPLARIs where T.stmuh_lastup_date >= Tarih1 && T.stmuh_lastup_date <= Tarih2 select T).ToList<STOK_MUHASEBE_GRUPLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_MUHASEBE_GRUPLARI Stok_Muhasebe_Gruplari_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_MUHASEBE_GRUPLARI tt = db.STOK_MUHASEBE_GRUPLARIs.FirstOrDefault(t => t.stmuh_kod == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_MUHASEBE_GRUPLARI Stok_Muhasebe_Gruplari_Kaydet(STOK_MUHASEBE_GRUPLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.stmuh_RECid_RECno = RandomDondur();
            db.STOK_MUHASEBE_GRUPLARIs.InsertOnSubmit(ch);

            db.SubmitChanges();
            //ch.stmuh_RECid_RECno = ch.stmuh_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_MUHASEBE_GRUPLARI Stok_Muhasebe_Gruplari_Guncelle(STOK_MUHASEBE_GRUPLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.stmuh_RECid_RECno = ch.stmuh_RECno;
            db.STOK_MUHASEBE_GRUPLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.stmuh_RECid_RECno = ch.stmuh_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region STOK RENK TANITIM KARTLARI
        
        public static List<STOK_RENK_TANIMLARI> Stok_Renk_Tanimlari_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_RENK_TANIMLARI> ls = new List<STOK_RENK_TANIMLARI>();

            ls = (from T in db.STOK_RENK_TANIMLARIs select T).ToList<STOK_RENK_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static List<STOK_RENK_TANIMLARI> Stok_Renk_Tanimlari_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_RENK_TANIMLARI> ls = new List<STOK_RENK_TANIMLARI>();

            ls = (from T in db.STOK_RENK_TANIMLARIs where T.rnk_lastup_date >= Tarih1 && T.rnk_lastup_date <= Tarih2 select T).ToList<STOK_RENK_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_RENK_TANIMLARI Stok_Renk_Tanimlari_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_RENK_TANIMLARI tt = db.STOK_RENK_TANIMLARIs.FirstOrDefault(t => t.rnk_kodu == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_RENK_TANIMLARI Stok_Renk_Tanimlari_Kaydet(STOK_RENK_TANIMLARI ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.rnk_RECid_RECno = RandomDondur();
            db.STOK_RENK_TANIMLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.rnk_RECid_RECno = ch.rnk_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_RENK_TANIMLARI Stok_Renk_Tanimlari_Guncelle(STOK_RENK_TANIMLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.rnk_RECid_RECno = ch.rnk_RECno;
            db.STOK_RENK_TANIMLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.rnk_RECid_RECno = ch.rnk_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region STOK REYON TANITIM KARTLARI
        
        public static List<STOK_REYONLARI> Stok_Reyonlari_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_REYONLARI> ls = new List<STOK_REYONLARI>();

            ls = (from T in db.STOK_REYONLARIs select T).ToList<STOK_REYONLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }


        public static List<STOK_REYONLARI> Stok_Reyonlari_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_REYONLARI> ls = new List<STOK_REYONLARI>();

            ls = (from T in db.STOK_REYONLARIs where T.ryn_lastup_date >= Tarih1 && T.ryn_lastup_date <= Tarih2 select T).ToList<STOK_REYONLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_REYONLARI Stok_Reyonlari_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_REYONLARI tt = db.STOK_REYONLARIs.FirstOrDefault(t => t.ryn_kod == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_REYONLARI Stok_Reyonlari_Kaydet(STOK_REYONLARI ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.ryn_RECid_RECno = RandomDondur();
            db.STOK_REYONLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.ryn_RECid_RECno = ch.ryn_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_REYONLARI Stok_Reyonlari_Guncelle(STOK_REYONLARI ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.ryn_RECid_RECno = ch.ryn_RECno;
            db.STOK_REYONLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.ryn_RECid_RECno = ch.ryn_RECno;
            //db.SubmitChanges();
            return ch;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region STOK SEKTÖRLERÝ KARTLARI

        public static List<STOK_SEKTORLERI> Stok_Sektorleri_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_SEKTORLERI> ls = new List<STOK_SEKTORLERI>();

            ls = (from T in db.STOK_SEKTORLERIs select T).ToList<STOK_SEKTORLERI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<STOK_SEKTORLERI> Stok_Sektorleri_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_SEKTORLERI> ls = new List<STOK_SEKTORLERI>();

            ls = (from T in db.STOK_SEKTORLERIs where T.sktr_lastup_date >= Tarih1 && T.sktr_lastup_date <= Tarih2 select T).ToList<STOK_SEKTORLERI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_SEKTORLERI Stok_Sektorleri_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_SEKTORLERI tt = db.STOK_SEKTORLERIs.FirstOrDefault(t => t.sktr_kod == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_SEKTORLERI Stok_Sektorleri_Kaydet(STOK_SEKTORLERI ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.sktr_RECid_RECno = RandomDondur();
            db.STOK_SEKTORLERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.sktr_RECid_RECno = ch.sktr_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_SEKTORLERI Stok_Sektorleri_Guncelle(STOK_SEKTORLERI ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.sktr_RECid_RECno = ch.sktr_RECno;
            db.STOK_SEKTORLERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.sktr_RECid_RECno = ch.sktr_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region STOK ÜRETÝCÝLERÝ KARTLARI
        
        public static List<STOK_URETICILERI> Stok_Ureticileri_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_URETICILERI> ls = new List<STOK_URETICILERI>();

            ls = (from T in db.STOK_URETICILERIs select T).ToList<STOK_URETICILERI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<STOK_URETICILERI> Stok_Ureticileri_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_URETICILERI> ls = new List<STOK_URETICILERI>();

            ls = (from T in db.STOK_URETICILERIs where T.urt_lastup_date >= Tarih1 && T.urt_lastup_date <= Tarih2 select T).ToList<STOK_URETICILERI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static STOK_URETICILERI Stok_Ureticileri_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_URETICILERI tt = db.STOK_URETICILERIs.FirstOrDefault(t => t.urt_kod == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_URETICILERI Stok_Ureticileri_Kaydet(STOK_URETICILERI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.urt_RECid_RECno = RandomDondur();
            db.STOK_URETICILERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.urt_RECid_RECno = ch.urt_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_URETICILERI Stok_Ureticileri_Guncelle(STOK_URETICILERI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.urt_RECid_RECno = ch.urt_RECno;
            db.STOK_URETICILERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.urt_RECid_RECno = ch.urt_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region STOK YIL SEZON TANITIM KARTLARI
        
        public static List<STOK_YILSEZON_TANIMLARI> Stok_Yilsezon_Tanimlari_Yukle(string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_YILSEZON_TANIMLARI> ls = new List<STOK_YILSEZON_TANIMLARI>();

            ls = (from T in db.STOK_YILSEZON_TANIMLARIs select T).ToList<STOK_YILSEZON_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<STOK_YILSEZON_TANIMLARI> Stok_Yilsezon_Tanimlari_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_YILSEZON_TANIMLARI> ls = new List<STOK_YILSEZON_TANIMLARI>();

            ls = (from T in db.STOK_YILSEZON_TANIMLARIs where T.ysn_lastup_date >= Tarih1 && T.ysn_lastup_date <= Tarih2 select T).ToList<STOK_YILSEZON_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }


        public static STOK_YILSEZON_TANIMLARI Stok_Yilsezon_Tanimlari_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_YILSEZON_TANIMLARI tt = db.STOK_YILSEZON_TANIMLARIs.FirstOrDefault(t => t.ysn_kodu == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_YILSEZON_TANIMLARI Stok_Yilsezon_Tanimlari_Kaydet(STOK_YILSEZON_TANIMLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.ysn_RECid_RECno = RandomDondur();
            db.STOK_YILSEZON_TANIMLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.ysn_RECid_RECno = ch.ysn_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static STOK_YILSEZON_TANIMLARI Stok_Yilsezon_Tanimlari_Guncelle(STOK_YILSEZON_TANIMLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.ysn_RECid_RECno = ch.ysn_RECno;
            db.STOK_YILSEZON_TANIMLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.ysn_RECid_RECno = ch.ysn_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region STOK FÝYAT LÝSTE TANIMLARI TANITIM KARTLARI
        
        public static List<STOK_SATIS_FIYAT_LISTE_TANIMLARI> STOK_SATIS_FIYAT_LISTE_TANIMLARIi_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_SATIS_FIYAT_LISTE_TANIMLARI> ls = new List<STOK_SATIS_FIYAT_LISTE_TANIMLARI>();

            ls = (from T in db.STOK_SATIS_FIYAT_LISTE_TANIMLARIs select T).ToList<STOK_SATIS_FIYAT_LISTE_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static List<STOK_SATIS_FIYAT_LISTE_TANIMLARI> STOK_SATIS_FIYAT_LISTE_TANIMLARIi_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_SATIS_FIYAT_LISTE_TANIMLARI> ls = new List<STOK_SATIS_FIYAT_LISTE_TANIMLARI>();

            ls = (from T in db.STOK_SATIS_FIYAT_LISTE_TANIMLARIs  where T.sfl_lastup_date >= Tarih1 && T.sfl_lastup_date <= Tarih2 select T).ToList<STOK_SATIS_FIYAT_LISTE_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }


        public static STOK_SATIS_FIYAT_LISTE_TANIMLARI STOK_SATIS_FIYAT_LISTE_TANIMLARI_EvrakDetayGetir(Int32 no, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_SATIS_FIYAT_LISTE_TANIMLARI tt = db.STOK_SATIS_FIYAT_LISTE_TANIMLARIs.FirstOrDefault(t => t.sfl_sirano == no);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_SATIS_FIYAT_LISTE_TANIMLARI STOK_SATIS_FIYAT_LISTE_TANIMLARIi_Kaydet(STOK_SATIS_FIYAT_LISTE_TANIMLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.sfl_RECid_RECno = RandomDondur();
            db.STOK_SATIS_FIYAT_LISTE_TANIMLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.sfl_RECid_RECno = ch.sfl_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_SATIS_FIYAT_LISTE_TANIMLARI STOK_SATIS_FIYAT_LISTE_TANIMLARIi_Guncelle(STOK_SATIS_FIYAT_LISTE_TANIMLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.sfl_RECid_RECno = ch.sfl_RECno;
            db.STOK_SATIS_FIYAT_LISTE_TANIMLARIs.Attach(ch, true);

            db.SubmitChanges();
            //ch.sfl_RECid_RECno = ch.sfl_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region STOK SATIS FÝYAT LÝSTELERÝ KARTLARI
        
        public static List<STOK_SATIS_FIYAT_LISTELERI> STOK_SATIS_FIYAT_LISTELERIi_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_SATIS_FIYAT_LISTELERI> ls = new List<STOK_SATIS_FIYAT_LISTELERI>();

            ls = (from T in db.STOK_SATIS_FIYAT_LISTELERIs select T).ToList<STOK_SATIS_FIYAT_LISTELERI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<STOK_SATIS_FIYAT_LISTELERI> STOK_SATIS_FIYAT_LISTELERIi_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_SATIS_FIYAT_LISTELERI> ls = new List<STOK_SATIS_FIYAT_LISTELERI>();

            ls = (from T in db.STOK_SATIS_FIYAT_LISTELERIs where T.sfiyat_lastup_date >= Tarih1 && T.sfiyat_lastup_date <= Tarih2 select T).ToList<STOK_SATIS_FIYAT_LISTELERI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_SATIS_FIYAT_LISTELERI STOK_SATIS_FIYAT_LISTELERI_EvrakDetayGetir(string kod,Int32 sirano,Int32 deposirano,Int32 odemeplan, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_SATIS_FIYAT_LISTELERI tt = db.STOK_SATIS_FIYAT_LISTELERIs.FirstOrDefault(t => t.sfiyat_stokkod == kod && t.sfiyat_listesirano == sirano && t.sfiyat_deposirano == deposirano && t.sfiyat_odemeplan == odemeplan );

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_SATIS_FIYAT_LISTELERI STOK_SATIS_FIYAT_LISTELERIi_Kaydet(STOK_SATIS_FIYAT_LISTELERI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.sfiyat_RECid_RECno = RandomDondur();
            db.STOK_SATIS_FIYAT_LISTELERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.sfiyat_RECid_RECno = ch.sfiyat_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static STOK_SATIS_FIYAT_LISTELERI STOK_SATIS_FIYAT_LISTELERIi_Guncelle(STOK_SATIS_FIYAT_LISTELERI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.sfiyat_RECid_RECno = ch.sfiyat_RECno;
            db.STOK_SATIS_FIYAT_LISTELERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.sfiyat_RECid_RECno = ch.sfiyat_RECid_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region PARTÝLOT KARTLARI
        
        public static List<PARTILOT> PARTILOT_Yukle(string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<PARTILOT> ls = new List<PARTILOT>();

                ls = (from T in db.PARTILOTs select T).ToList<PARTILOT>();

                return ls;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<PARTILOT> PARTILOT_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<PARTILOT> ls = new List<PARTILOT>();

                ls = (from T in db.PARTILOTs where T.pl_lastup_date >= Tarih1 && T.pl_lastup_date <= Tarih2 select T).ToList<PARTILOT>();

                return ls;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static PARTILOT PARTILOT_EvrakDetayGetir(string kod, Int32 lotno, string stokkod, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                PARTILOT tt = db.PARTILOTs.FirstOrDefault(t => t.pl_partikodu == kod && t.pl_lotno == lotno && t.pl_stokkodu == stokkod);
                return tt;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static PARTILOT PARTILOT_Kaydet(PARTILOT ch, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                //ch.pl_RECid_RECno = RandomDondur();
                db.PARTILOTs.InsertOnSubmit(ch);
                db.SubmitChanges();
                //ch.pl_RECid_RECno = ch.pl_RECno;
                //db.SubmitChanges();
                return ch;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static PARTILOT PARTILOT_Guncelle(PARTILOT ch, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                //ch.pl_RECid_RECno = ch.pl_RECno;
                db.PARTILOTs.Attach(ch, true);
                db.SubmitChanges();
                //ch.pl_RECid_RECno = ch.pl_RECno;
                //db.SubmitChanges();
                return ch;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }



        #endregion


        #endregion

        #region PERSONELLER

        #region PERSONEL TANITIM KARTLARI

        public static List<PERSONELLER> Personelleri_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<PERSONELLER> ls = new List<PERSONELLER>();

            ls = (from T in db.PERSONELLERs where T.per_lastup_date >= Tarih1 && T.per_lastup_date <= Tarih2 select T).ToList<PERSONELLER>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<PERSONELLER> Personelleri_Yukle(string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<PERSONELLER> ls = new List<PERSONELLER>();

            ls = (from T in db.PERSONELLERs select T).ToList<PERSONELLER>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static PERSONELLER Personeller_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            PERSONELLER tt = db.PERSONELLERs.FirstOrDefault(t => t.per_kod == kod);

            return tt;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static PERSONELLER Personeller_Kaydet(PERSONELLER ch, string conn)
        {
            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
            //ch.per_RECid_RECno= RandomDondur();
            db.PERSONELLERs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.per_RECid_RECno = ch.per_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static PERSONELLER Personeller_Guncelle(PERSONELLER ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.per_RECid_RECno = ch.per_RECno;
            db.PERSONELLERs.Attach(ch, true);
            db.SubmitChanges();
            //ch.per_RECid_RECno = ch.per_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region PERSONEL BÖLGELERÝ TANITIM KARTLARI
        
        public static List<PERSONEL_BOLGELERI> Personel_Bolgeleri_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<PERSONEL_BOLGELERI> ls = new List<PERSONEL_BOLGELERI>();

            ls = (from T in db.PERSONEL_BOLGELERIs select T).ToList<PERSONEL_BOLGELERI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<PERSONEL_BOLGELERI> Personel_Bolgeleri_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<PERSONEL_BOLGELERI> ls = new List<PERSONEL_BOLGELERI>();

            ls = (from T in db.PERSONEL_BOLGELERIs where T.pbl_lastup_date >= Tarih1 && T.pbl_lastup_date <= Tarih2 select T).ToList<PERSONEL_BOLGELERI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }


        public static PERSONEL_BOLGELERI Personel_Bolgeleri_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            PERSONEL_BOLGELERI tt = db.PERSONEL_BOLGELERIs.FirstOrDefault(t => t.pbl_bolge_kodu == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static PERSONEL_BOLGELERI Personel_Bolgeleri_Kaydet(PERSONEL_BOLGELERI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.pbl_RECid_RECno = RandomDondur();
            db.PERSONEL_BOLGELERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.pbl_RECid_RECno = ch.pbl_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static PERSONEL_BOLGELERI Personel_Bolgeleri_Guncelle(PERSONEL_BOLGELERI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.pbl_RECid_RECno = ch.pbl_RECno;
            db.PERSONEL_BOLGELERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.pbl_RECid_RECno = ch.pbl_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region PERSONEL MUHASEBE GRUPLARI TANITIM KARTLARI

        public static List<PERSONEL_MUHASEBE_GRUPLARI> Personel_Muhasebe_Gruplari_Yukle(string conn)
        {

            try
            {

            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<PERSONEL_MUHASEBE_GRUPLARI> ls = new List<PERSONEL_MUHASEBE_GRUPLARI>();

            ls = (from T in db.PERSONEL_MUHASEBE_GRUPLARIs select T).ToList<PERSONEL_MUHASEBE_GRUPLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<PERSONEL_MUHASEBE_GRUPLARI> Personel_Muhasebe_Gruplari_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<PERSONEL_MUHASEBE_GRUPLARI> ls = new List<PERSONEL_MUHASEBE_GRUPLARI>();

            ls = (from T in db.PERSONEL_MUHASEBE_GRUPLARIs where T.pmg_lastup_date >= Tarih1 && T.pmg_lastup_date <= Tarih2 select T).ToList<PERSONEL_MUHASEBE_GRUPLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static PERSONEL_MUHASEBE_GRUPLARI Personel_Muhasebe_Gruplari_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            PERSONEL_MUHASEBE_GRUPLARI tt = db.PERSONEL_MUHASEBE_GRUPLARIs.FirstOrDefault(t => t.pmg_kodu == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static PERSONEL_MUHASEBE_GRUPLARI Personel_Muhasebe_Gruplari_Kaydet(PERSONEL_MUHASEBE_GRUPLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.pmg_RECid_RECno = RandomDondur();
            db.PERSONEL_MUHASEBE_GRUPLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.pmg_RECid_RECno = ch.pmg_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static PERSONEL_MUHASEBE_GRUPLARI Personel_Muhasebe_Gruplari_Guncelle(PERSONEL_MUHASEBE_GRUPLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.pmg_RECid_RECno = ch.pmg_RECno;
            db.PERSONEL_MUHASEBE_GRUPLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.pmg_RECid_RECno = ch.pmg_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #endregion

        #region GENEL TANIMLAR

        #region DEPO TANITIM KARTLARI


        public static List<DEPOLAR> Depolari_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<DEPOLAR> ls = new List<DEPOLAR>();

            ls = (from T in db.DEPOLARs select T).ToList<DEPOLAR>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }


        public static List<DEPOLAR> Depolari_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<DEPOLAR> ls = new List<DEPOLAR>();

            ls = (from T in db.DEPOLARs where T.dep_lastup_date >= Tarih1 && T.dep_lastup_date <= Tarih2 select T).ToList<DEPOLAR>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static DEPOLAR Depolar_EvrakDetayGetir(Int32 no, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            DEPOLAR tt = db.DEPOLARs.FirstOrDefault(t => t.dep_no == no);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static DEPOLAR Depolari_Kaydet(DEPOLAR ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.dep_RECid_RECno = RandomDondur();
            db.DEPOLARs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.dep_RECid_RECno = ch.dep_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static DEPOLAR Depolari_Guncelle(DEPOLAR ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.dep_RECid_RECno = ch.dep_RECno;
            db.DEPOLARs.Attach(ch, true);
            db.SubmitChanges();
            //ch.dep_RECid_RECno = ch.dep_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region ASORTÝ TANITIM KARTLARI
        

        public static List<ASORTI_TANIMLARI> Asorti_Tanimlari_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<ASORTI_TANIMLARI> ls = new List<ASORTI_TANIMLARI>();

            ls = (from T in db.ASORTI_TANIMLARIs select T).ToList<ASORTI_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static List<ASORTI_TANIMLARI> Asorti_Tanimlari_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<ASORTI_TANIMLARI> ls = new List<ASORTI_TANIMLARI>();

            ls = (from T in db.ASORTI_TANIMLARIs where T.Asorti_lastup_date >= Tarih1 && T.Asorti_lastup_date <= Tarih2 select T).ToList<ASORTI_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static ASORTI_TANIMLARI Asorti_Tanimlari_EvrakDetayGetir(string stokkod,Int32 bedenno, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            ASORTI_TANIMLARI tt = db.ASORTI_TANIMLARIs.FirstOrDefault(t => t.Asorti_StokKodu == stokkod && t.Asorti_BedenNo == bedenno);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static ASORTI_TANIMLARI Asorti_Tanimlari_Kaydet(ASORTI_TANIMLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.Asorti_RECid_RECno = RandomDondur();
            db.ASORTI_TANIMLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.Asorti_RECid_RECno = ch.Asorti_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static ASORTI_TANIMLARI Asorti_Tanimlari_Guncelle(ASORTI_TANIMLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.Asorti_RECid_RECno = ch.Asorti_RECno;
            db.ASORTI_TANIMLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.Asorti_RECid_RECno = ch.Asorti_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        #endregion

        #region BANKA TANITIM KARTLARI

        
        public static List<BANKALAR> BANKALARi_Yukle(string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<BANKALAR> ls = new List<BANKALAR>();

            ls = (from T in db.BANKALARs select T).ToList<BANKALAR>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<BANKALAR> BANKALARi_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<BANKALAR> ls = new List<BANKALAR>();

            ls = (from T in db.BANKALARs where T.ban_lastup_date >= Tarih1 && T.ban_lastup_date <= Tarih2 select T).ToList<BANKALAR>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static BANKALAR BANKALAR_EvrakDetayGetir(string kod, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            BANKALAR tt = db.BANKALARs.FirstOrDefault(t => t.ban_kod == kod);

            return tt;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static BANKALAR BANKALARi_Kaydet(BANKALAR ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.ban_RECid_RECno = RandomDondur();
            db.BANKALARs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.ban_RECid_RECno = ch.ban_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static BANKALAR BANKALARi_Guncelle(BANKALAR ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.ban_RECid_RECno = ch.ban_RECno;
            db.BANKALARs.Attach(ch, true);
            db.SubmitChanges();
            //ch.ban_RECid_RECno = ch.ban_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        #endregion

        #region BARKOD TANITIM KARTLARI

        
        public static List<BARKOD_TANIMLARI> BARKOD_TANIMLARIi_Yukle(string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<BARKOD_TANIMLARI> ls = new List<BARKOD_TANIMLARI>();

            ls = (from T in db.BARKOD_TANIMLARIs select T).ToList<BARKOD_TANIMLARI>();

            return ls;

            }
        }

        public static List<BARKOD_TANIMLARI> BARKOD_TANIMLARIi_Yukle(DateTime Tarih1, DateTime Tarih2,string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<BARKOD_TANIMLARI> ls = new List<BARKOD_TANIMLARI>();

            ls = (from T in db.BARKOD_TANIMLARIs where T.bar_lastup_date >= Tarih1 && T.bar_lastup_date <= Tarih2 select T).ToList<BARKOD_TANIMLARI>();

            return ls;

            }
        }


        public static BARKOD_TANIMLARI BARKOD_TANIMLARI_EvrakDetayGetir(string kod, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            BARKOD_TANIMLARI tt = db.BARKOD_TANIMLARIs.FirstOrDefault(t => t.bar_kodu == kod);

            return tt;

            }
        }

        public static BARKOD_TANIMLARI BARKOD_TANIMLARIi_Kaydet(BARKOD_TANIMLARI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.bar_RECid_RECno = RandomDondur();
            db.BARKOD_TANIMLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.bar_RECid_RECno = ch.bar_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        public static BARKOD_TANIMLARI BARKOD_TANIMLARIi_Guncelle(BARKOD_TANIMLARI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.bar_RECid_RECno = ch.bar_RECno;
            db.BARKOD_TANIMLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.bar_RECid_RECno = ch.bar_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        #endregion
        // yok
        #region CÝHAZ SORUNLARI KARTLARI
        

        public static List<CIHAZ_SORUNLARI> CIHAZ_SORUNLARIi_Yukle(string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<CIHAZ_SORUNLARI> ls = new List<CIHAZ_SORUNLARI>();

            ls = (from T in db.CIHAZ_SORUNLARIs select T).ToList<CIHAZ_SORUNLARI>();

            return ls;

            }
        }

        public static CIHAZ_SORUNLARI CIHAZ_SORUNLARI_EvrakDetayGetir(string kod, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            CIHAZ_SORUNLARI tt = db.CIHAZ_SORUNLARIs.FirstOrDefault(t => t.chs_kodu == kod);

            return tt;

            }
        }

        public static CIHAZ_SORUNLARI CIHAZ_SORUNLARIi_Kaydet(CIHAZ_SORUNLARI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.CIHAZ_SORUNLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.chs_RECid_RECno = ch.chs_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        public static CIHAZ_SORUNLARI CIHAZ_SORUNLARIi_Guncelle(CIHAZ_SORUNLARI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.CIHAZ_SORUNLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.chs_RECid_RECno = ch.chs_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        #endregion
        // yok
        #region DEMÝRBAÞ GRUPLARI TANITIM KARTLARI
        

        public static List<DEMIRBAS_GRUPLARI> DEMIRBAS_GRUPLARIi_Yukle(string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<DEMIRBAS_GRUPLARI> ls = new List<DEMIRBAS_GRUPLARI>();

            ls = (from T in db.DEMIRBAS_GRUPLARIs select T).ToList<DEMIRBAS_GRUPLARI>();

            return ls;

            }
        }

        public static List<DEMIRBAS_GRUPLARI> DEMIRBAS_GRUPLARIi_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<DEMIRBAS_GRUPLARI> ls = new List<DEMIRBAS_GRUPLARI>();

                ls = (from T in db.DEMIRBAS_GRUPLARIs where T.grp_lastup_date >= Tarih1 && T.grp_lastup_date <= Tarih2 select T).ToList<DEMIRBAS_GRUPLARI>();

                return ls;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static DEMIRBAS_GRUPLARI DEMIRBAS_GRUPLARI_EvrakDetayGetir(string kod, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            DEMIRBAS_GRUPLARI tt = db.DEMIRBAS_GRUPLARIs.FirstOrDefault(t => t.grp_kod == kod);

            return tt;

            }
        }

        public static DEMIRBAS_GRUPLARI DEMIRBAS_GRUPLARIi_Kaydet(DEMIRBAS_GRUPLARI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.DEMIRBAS_GRUPLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.grp_RECid_RECno = ch.grp_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        public static DEMIRBAS_GRUPLARI DEMIRBAS_GRUPLARIi_Guncelle(DEMIRBAS_GRUPLARI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.DEMIRBAS_GRUPLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.grp_RECid_RECno = ch.grp_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        #endregion
        // yok
        #region DEMÝRBAÞ MALÝ YIL TANIMLARI TANITIM KARTLARI
       
        
        public static List<DEMIRBAS_MALIYIL_TANIMLARI> DEMIRBAS_MALIYIL_TANIMLARIi_Yukle(string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<DEMIRBAS_MALIYIL_TANIMLARI> ls = new List<DEMIRBAS_MALIYIL_TANIMLARI>();

            ls = (from T in db.DEMIRBAS_MALIYIL_TANIMLARIs select T).ToList<DEMIRBAS_MALIYIL_TANIMLARI>();

            return ls;

            }
        }

        public static List<DEMIRBAS_MALIYIL_TANIMLARI> DEMIRBAS_MALIYIL_TANIMLARIi_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<DEMIRBAS_MALIYIL_TANIMLARI> ls = new List<DEMIRBAS_MALIYIL_TANIMLARI>();

                ls = (from T in db.DEMIRBAS_MALIYIL_TANIMLARIs where T.amy_lastup_date >= Tarih1 && T.amy_lastup_date <= Tarih2 select T).ToList<DEMIRBAS_MALIYIL_TANIMLARI>();

                return ls;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }


        public static DEMIRBAS_MALIYIL_TANIMLARI DEMIRBAS_MALIYIL_TANIMLARI_EvrakDetayGetir(string kod,Int32 maliyil, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            DEMIRBAS_MALIYIL_TANIMLARI tt = db.DEMIRBAS_MALIYIL_TANIMLARIs.FirstOrDefault(t => t.amy_kod == kod && t.amy_maliyil ==maliyil);

            return tt;

            }
        }

        public static DEMIRBAS_MALIYIL_TANIMLARI DEMIRBAS_MALIYIL_TANIMLARIi_Kaydet(DEMIRBAS_MALIYIL_TANIMLARI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.DEMIRBAS_MALIYIL_TANIMLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.amy_RECid_RECno = ch.amy_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        public static DEMIRBAS_MALIYIL_TANIMLARI DEMIRBAS_MALIYIL_TANIMLARIi_Guncelle(DEMIRBAS_MALIYIL_TANIMLARI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.DEMIRBAS_MALIYIL_TANIMLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.amy_RECid_RECno = ch.amy_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        #endregion
        // yok
        #region DEMÝRBAÞLAR TANITIM KARTLARI

        
        public static List<DEMIRBASLAR> DEMIRBASLARi_Yukle(string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<DEMIRBASLAR> ls = new List<DEMIRBASLAR>();

            ls = (from T in db.DEMIRBASLARs select T).ToList<DEMIRBASLAR>();

            return ls;

            }
        }

        public static List<DEMIRBASLAR> DEMIRBASLARi_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<DEMIRBASLAR> ls = new List<DEMIRBASLAR>();

                ls = (from T in db.DEMIRBASLARs where T.dem_lastup_date >= Tarih1 && T.dem_lastup_date <= Tarih2 select T).ToList<DEMIRBASLAR>();

                return ls;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static DEMIRBASLAR DEMIRBASLAR_EvrakDetayGetir(string kod, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            DEMIRBASLAR tt = db.DEMIRBASLARs.FirstOrDefault(t => t.dem_kod == kod);

            return tt;

            }
        }

        public static DEMIRBASLAR DEMIRBASLARi_Kaydet(DEMIRBASLAR ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.DEMIRBASLARs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.dem_RECid_RECno = ch.dem_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        public static DEMIRBASLAR DEMIRBASLARi_Guncelle(DEMIRBASLAR ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.DEMIRBASLARs.Attach(ch, true);
            db.SubmitChanges();
            //ch.dem_RECid_RECno = ch.dem_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        #endregion
        // yok
        #region FÝRMA MALÝYIL PARAMETRELERÝ TANITIM KARTLARI

        
        public static List<FIRMA_MALIYIL_BEYANNAME_PARAMETRELERI> FIRMA_MALIYIL_BEYANNAME_PARAMETRELERIi_Yukle(string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<FIRMA_MALIYIL_BEYANNAME_PARAMETRELERI> ls = new List<FIRMA_MALIYIL_BEYANNAME_PARAMETRELERI>();

            ls = (from T in db.FIRMA_MALIYIL_BEYANNAME_PARAMETRELERIs select T).ToList<FIRMA_MALIYIL_BEYANNAME_PARAMETRELERI>();

            return ls;

            }
        }

        public static FIRMA_MALIYIL_BEYANNAME_PARAMETRELERI FIRMA_MALIYIL_BEYANNAME_PARAMETRELERI_EvrakDetayGetir(Int32 sirano,Int32 maliyil, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            FIRMA_MALIYIL_BEYANNAME_PARAMETRELERI tt = db.FIRMA_MALIYIL_BEYANNAME_PARAMETRELERIs.FirstOrDefault(t => t.fmyb_sirano == sirano && t.fmyb_maliyil == maliyil);

            return tt;

            }
        }

        public static FIRMA_MALIYIL_BEYANNAME_PARAMETRELERI FIRMA_MALIYIL_BEYANNAME_PARAMETRELERIi_Kaydet(FIRMA_MALIYIL_BEYANNAME_PARAMETRELERI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.FIRMA_MALIYIL_BEYANNAME_PARAMETRELERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.fmyb_RECid_RECno = ch.fmyb_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        public static FIRMA_MALIYIL_BEYANNAME_PARAMETRELERI FIRMA_MALIYIL_BEYANNAME_PARAMETRELERIi_Guncelle(FIRMA_MALIYIL_BEYANNAME_PARAMETRELERI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.FIRMA_MALIYIL_BEYANNAME_PARAMETRELERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.fmyb_RECid_RECno = ch.fmyb_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        #endregion
        // yok
        #region FÝRMA MALÝYIL BÝLGÝLERÝ TANITIM KARTLARI
        

        public static List<FIRMA_MALIYIL_BILGILERI> FIRMA_MALIYIL_BILGILERIi_Yukle(string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<FIRMA_MALIYIL_BILGILERI> ls = new List<FIRMA_MALIYIL_BILGILERI>();

            ls = (from T in db.FIRMA_MALIYIL_BILGILERIs select T).ToList<FIRMA_MALIYIL_BILGILERI>();

            return ls;

            }
        }

        public static FIRMA_MALIYIL_BILGILERI FIRMA_MALIYIL_BILGILERI_EvrakDetayGetir(Int32 sirano,Int32 maliyil, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            FIRMA_MALIYIL_BILGILERI tt = db.FIRMA_MALIYIL_BILGILERIs.FirstOrDefault(t => t.fmy_sirano == sirano && t.fmy_maliyil == maliyil);

            return tt;

            }
        }

        public static FIRMA_MALIYIL_BILGILERI FIRMA_MALIYIL_BILGILERIi_Kaydet(FIRMA_MALIYIL_BILGILERI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.FIRMA_MALIYIL_BILGILERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.fmy_RECid_RECno = ch.fmy_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        public static FIRMA_MALIYIL_BILGILERI FIRMA_MALIYIL_BILGILERIi_Guncelle(FIRMA_MALIYIL_BILGILERI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.FIRMA_MALIYIL_BILGILERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.fmy_RECid_RECno = ch.fmy_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        #endregion
        // yok
        #region FÝRMA MALÝYIL ÜRETÝM PARAMETRELERI TANITIM KARTLARI

        
        public static List<FIRMA_MALIYIL_URETIM_PARAMETRELERI> FIRMA_MALIYIL_URETIM_PARAMETRELERIi_Yukle(string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<FIRMA_MALIYIL_URETIM_PARAMETRELERI> ls = new List<FIRMA_MALIYIL_URETIM_PARAMETRELERI>();

            ls = (from T in db.FIRMA_MALIYIL_URETIM_PARAMETRELERIs select T).ToList<FIRMA_MALIYIL_URETIM_PARAMETRELERI>();

            return ls;

            }
        }

        public static FIRMA_MALIYIL_URETIM_PARAMETRELERI FIRMA_MALIYIL_URETIM_PARAMETRELERI_EvrakDetayGetir(Int32 sirano, Int32 maliyil, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            FIRMA_MALIYIL_URETIM_PARAMETRELERI tt = db.FIRMA_MALIYIL_URETIM_PARAMETRELERIs.FirstOrDefault(t => t.fmu_sirano == sirano && t.fmu_maliyil ==maliyil);

            return tt;

            }
        }

        public static FIRMA_MALIYIL_URETIM_PARAMETRELERI FIRMA_MALIYIL_URETIM_PARAMETRELERIi_Kaydet(FIRMA_MALIYIL_URETIM_PARAMETRELERI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.FIRMA_MALIYIL_URETIM_PARAMETRELERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.fmu_RECid_RECno = ch.fmu_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        public static FIRMA_MALIYIL_URETIM_PARAMETRELERI FIRMA_MALIYIL_URETIM_PARAMETRELERIi_Guncelle(FIRMA_MALIYIL_URETIM_PARAMETRELERI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.FIRMA_MALIYIL_URETIM_PARAMETRELERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.fmu_RECid_RECno = ch.fmu_RECno;
            //db.SubmitChanges();
            return ch;
            }
        }

        #endregion
        // yok
        #region FÝRMA TEMSÝLCÝLERÝ TANITIM KARTLARI

        
        public static List<FIRMA_TEMSILCILERI> FIRMA_TEMSILCILERIi_Yukle(string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<FIRMA_TEMSILCILERI> ls = new List<FIRMA_TEMSILCILERI>();

            ls = (from T in db.FIRMA_TEMSILCILERIs select T).ToList<FIRMA_TEMSILCILERI>();

            return ls;

            }
        }

        public static FIRMA_TEMSILCILERI FIRMA_TEMSILCILERI_EvrakDetayGetir(Int32 firmano,string kod, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            FIRMA_TEMSILCILERI tt = db.FIRMA_TEMSILCILERIs.FirstOrDefault(t => t.tms_Bag_Firma_No == firmano && t.tms_Kodu == kod);

            return tt;

            }
        }

        public static FIRMA_TEMSILCILERI FIRMA_TEMSILCILERIi_Kaydet(FIRMA_TEMSILCILERI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.FIRMA_TEMSILCILERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.tms_RECid_RECno = ch.tms_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        public static FIRMA_TEMSILCILERI FIRMA_TEMSILCILERIi_Guncelle(FIRMA_TEMSILCILERI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.FIRMA_TEMSILCILERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.tms_RECid_RECno = ch.tms_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        #endregion
        // yok
        #region FÝRMA TANITIM KARTLARI

        
        public static List<FIRMALAR> FIRMALARi_Yukle(string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<FIRMALAR> ls = new List<FIRMALAR>();

            ls = (from T in db.FIRMALARs select T).ToList<FIRMALAR>();

            return ls;

            }
        }

        public static FIRMALAR FIRMALAR_EvrakDetayGetir(Int32 sirano, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            FIRMALAR tt = db.FIRMALARs.FirstOrDefault(t => t.fir_sirano == sirano);

            return tt;

            }
        }

        public static FIRMALAR FIRMALARi_Kaydet(FIRMALAR ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.FIRMALARs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.fir_RECid_RECno = ch.fir_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        public static FIRMALAR FIRMALARi_Guncelle(FIRMALAR ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.FIRMALARs.Attach(ch, true);
            db.SubmitChanges();
            //ch.fir_RECid_RECno = ch.fir_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        #endregion

        #region HÝZMET HESAPLARI TANITIM KARTLARI

        
        public static List<HIZMET_HESAPLARI> HIZMET_HESAPLARIi_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<HIZMET_HESAPLARI> ls = new List<HIZMET_HESAPLARI>();

            ls = (from T in db.HIZMET_HESAPLARIs select T).ToList<HIZMET_HESAPLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }


        public static List<HIZMET_HESAPLARI> HIZMET_HESAPLARIi_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<HIZMET_HESAPLARI> ls = new List<HIZMET_HESAPLARI>();

            ls = (from T in db.HIZMET_HESAPLARIs where T.hiz_lastup_date >= Tarih1 && T.hiz_lastup_date <= Tarih2 select T).ToList<HIZMET_HESAPLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static HIZMET_HESAPLARI HIZMET_HESAPLARI_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            HIZMET_HESAPLARI tt = db.HIZMET_HESAPLARIs.FirstOrDefault(t => t.hiz_kod == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static HIZMET_HESAPLARI HIZMET_HESAPLARIi_Kaydet(HIZMET_HESAPLARI ch, string conn)
        {

            try
            {

            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.hiz_RECid_RECno = RandomDondur();
            db.HIZMET_HESAPLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.hiz_RECid_RECno = ch.hiz_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static HIZMET_HESAPLARI HIZMET_HESAPLARIi_Guncelle(HIZMET_HESAPLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.hiz_RECid_RECno = ch.hiz_RECno;
            db.HIZMET_HESAPLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.hiz_RECid_RECno = ch.hiz_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        #endregion

        #region ÝTHALAT MUHASEBE GRUP TANITIM KARTLARI

        
        public static List<ITHALAT_MUHASEBE_GRUPLARI> ITHALAT_MUHASEBE_GRUPLARIi_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<ITHALAT_MUHASEBE_GRUPLARI> ls = new List<ITHALAT_MUHASEBE_GRUPLARI>();

            ls = (from T in db.ITHALAT_MUHASEBE_GRUPLARIs select T).ToList<ITHALAT_MUHASEBE_GRUPLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<ITHALAT_MUHASEBE_GRUPLARI> ITHALAT_MUHASEBE_GRUPLARIi_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<ITHALAT_MUHASEBE_GRUPLARI> ls = new List<ITHALAT_MUHASEBE_GRUPLARI>();

            ls = (from T in db.ITHALAT_MUHASEBE_GRUPLARIs where T.IthMuh_lastup_date >= Tarih1 && T.IthMuh_lastup_date <= Tarih2 select T).ToList<ITHALAT_MUHASEBE_GRUPLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }


        public static ITHALAT_MUHASEBE_GRUPLARI ITHALAT_MUHASEBE_GRUPLARI_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            ITHALAT_MUHASEBE_GRUPLARI tt = db.ITHALAT_MUHASEBE_GRUPLARIs.FirstOrDefault(t => t.IthMuh_kod == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static ITHALAT_MUHASEBE_GRUPLARI ITHALAT_MUHASEBE_GRUPLARIi_Kaydet(ITHALAT_MUHASEBE_GRUPLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.IthMuh_RECid_RECno = RandomDondur();
            db.ITHALAT_MUHASEBE_GRUPLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.IthMuh_RECid_RECno = ch.IthMuh_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static ITHALAT_MUHASEBE_GRUPLARI ITHALAT_MUHASEBE_GRUPLARIi_Guncelle(ITHALAT_MUHASEBE_GRUPLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.IthMuh_RECid_RECno = ch.IthMuh_RECno;
            db.ITHALAT_MUHASEBE_GRUPLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.IthMuh_RECid_RECno = ch.IthMuh_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        #endregion

        #region KASA TANITIM KARTLARI

        
        public static List<KASALAR> KASALARi_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<KASALAR> ls = new List<KASALAR>();

            ls = (from T in db.KASALARs select T).ToList<KASALAR>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<KASALAR> KASALARi_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<KASALAR> ls = new List<KASALAR>();

            ls = (from T in db.KASALARs where T.kas_lastup_date >= Tarih1 && T.kas_lastup_date <= Tarih2 select T).ToList<KASALAR>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static KASALAR KASALAR_EvrakDetayGetir(string kod, string conn)
        {

            try
            {

            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            KASALAR tt = db.KASALARs.FirstOrDefault(t => t.kas_kod == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static KASALAR KASALARi_Kaydet(KASALAR ch, string conn)
        {

            try
            {
            //ch.kas_RECid_RECno = RandomDondur();
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.KASALARs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.kas_RECid_RECno = ch.kas_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static KASALAR KASALARi_Guncelle(KASALAR ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.kas_RECid_RECno = ch.kas_RECno;
            db.KASALARs.Attach(ch, true);
            db.SubmitChanges();
            //ch.kas_RECid_RECno = ch.kas_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        #endregion

        #region MASRAF HESAPLARI TANITIM KARTLARI

        
        public static List<MASRAF_HESAPLARI> MASRAF_HESAPLARIi_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<MASRAF_HESAPLARI> ls = new List<MASRAF_HESAPLARI>();

            ls = (from T in db.MASRAF_HESAPLARIs select T).ToList<MASRAF_HESAPLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static List<MASRAF_HESAPLARI> MASRAF_HESAPLARIi_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<MASRAF_HESAPLARI> ls = new List<MASRAF_HESAPLARI>();

            ls = (from T in db.MASRAF_HESAPLARIs where T.his_lastup_date >= Tarih1 && T.his_lastup_date <= Tarih2 select T).ToList<MASRAF_HESAPLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static MASRAF_HESAPLARI MASRAF_HESAPLARI_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            MASRAF_HESAPLARI tt = db.MASRAF_HESAPLARIs.FirstOrDefault(t => t.his_kod == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static MASRAF_HESAPLARI MASRAF_HESAPLARIi_Kaydet(MASRAF_HESAPLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.his_RECid_RECno = RandomDondur();
            db.MASRAF_HESAPLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.his_RECid_RECno = ch.his_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static MASRAF_HESAPLARI MASRAF_HESAPLARIi_Guncelle(MASRAF_HESAPLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.his_RECid_RECno = ch.his_RECno;
            db.MASRAF_HESAPLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.his_RECid_RECno = ch.his_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        #endregion

        #region MUHASEBE FÝÞ GRUBU TANITIM KARTLARI


        public static List<MUHASEBE_FIS_GRUBU_TANIMLARI> MUHASEBE_FIS_GRUBU_TANIMLARIi_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<MUHASEBE_FIS_GRUBU_TANIMLARI> ls = new List<MUHASEBE_FIS_GRUBU_TANIMLARI>();

            ls = (from T in db.MUHASEBE_FIS_GRUBU_TANIMLARIs select T).ToList<MUHASEBE_FIS_GRUBU_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<MUHASEBE_FIS_GRUBU_TANIMLARI> MUHASEBE_FIS_GRUBU_TANIMLARIi_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<MUHASEBE_FIS_GRUBU_TANIMLARI> ls = new List<MUHASEBE_FIS_GRUBU_TANIMLARI>();

            ls = (from T in db.MUHASEBE_FIS_GRUBU_TANIMLARIs where T.mfg_lastup_date >= Tarih1 && T.mfg_lastup_date <= Tarih2 select T).ToList<MUHASEBE_FIS_GRUBU_TANIMLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static MUHASEBE_FIS_GRUBU_TANIMLARI MUHASEBE_FIS_GRUBU_TANIMLARI_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            MUHASEBE_FIS_GRUBU_TANIMLARI tt = db.MUHASEBE_FIS_GRUBU_TANIMLARIs.FirstOrDefault(t => t.mfg_kodu == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static MUHASEBE_FIS_GRUBU_TANIMLARI MUHASEBE_FIS_GRUBU_TANIMLARIi_Kaydet(MUHASEBE_FIS_GRUBU_TANIMLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.mfg_RECid_RECno = RandomDondur();
            db.MUHASEBE_FIS_GRUBU_TANIMLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.mfg_RECid_RECno = ch.mfg_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static MUHASEBE_FIS_GRUBU_TANIMLARI MUHASEBE_FIS_GRUBU_TANIMLARIi_Guncelle(MUHASEBE_FIS_GRUBU_TANIMLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.mfg_RECid_RECno = ch.mfg_RECno;
            db.MUHASEBE_FIS_GRUBU_TANIMLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.mfg_RECid_RECno = ch.mfg_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region MUHASEBE HESAP GRUPLARI TANITIM KARTLARI

        
        public static List<MUHASEBE_HESAP_GRUPLARI> MUHASEBE_HESAP_GRUPLARIi_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<MUHASEBE_HESAP_GRUPLARI> ls = new List<MUHASEBE_HESAP_GRUPLARI>();

            ls = (from T in db.MUHASEBE_HESAP_GRUPLARIs select T).ToList<MUHASEBE_HESAP_GRUPLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<MUHASEBE_HESAP_GRUPLARI> MUHASEBE_HESAP_GRUPLARIi_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<MUHASEBE_HESAP_GRUPLARI> ls = new List<MUHASEBE_HESAP_GRUPLARI>();

            ls = (from T in db.MUHASEBE_HESAP_GRUPLARIs where T.mhg_lastup_date >= Tarih1 && T.mhg_lastup_date <= Tarih2 select T).ToList<MUHASEBE_HESAP_GRUPLARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static MUHASEBE_HESAP_GRUPLARI MUHASEBE_HESAP_GRUPLARI_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            MUHASEBE_HESAP_GRUPLARI tt = db.MUHASEBE_HESAP_GRUPLARIs.FirstOrDefault(t => t.mhg_kodu == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static MUHASEBE_HESAP_GRUPLARI MUHASEBE_HESAP_GRUPLARIi_Kaydet(MUHASEBE_HESAP_GRUPLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.mhg_RECid_RECno = RandomDondur();
            db.MUHASEBE_HESAP_GRUPLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.mhg_RECid_RECno = ch.mhg_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static MUHASEBE_HESAP_GRUPLARI MUHASEBE_HESAP_GRUPLARIi_Guncelle(MUHASEBE_HESAP_GRUPLARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.mhg_RECid_RECno = ch.mhg_RECno;
            db.MUHASEBE_HESAP_GRUPLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.mhg_RECid_RECno = ch.mhg_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        #endregion

        #region PROJELER TANITIM KARTLARI

        
        public static List<PROJELER> PROJELERi_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<PROJELER> ls = new List<PROJELER>();

            ls = (from T in db.PROJELERs select T).ToList<PROJELER>();


            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<PROJELER> PROJELERi_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<PROJELER> ls = new List<PROJELER>();

            ls = (from T in db.PROJELERs where T.pro_lastup_date>= Tarih1 && T.pro_lastup_date <= Tarih2 select T).ToList<PROJELER>();

            return ls;


            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }


        public static PROJELER PROJELER_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            PROJELER tt = db.PROJELERs.FirstOrDefault(t => t.pro_kodu == kod);

            return tt;


            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static PROJELER PROJELERi_Kaydet(PROJELER ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.pro_RECid_RECno = RandomDondur();
            db.PROJELERs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.pro_RECid_RECno = ch.pro_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static PROJELER PROJELERi_Guncelle(PROJELER ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.pro_RECid_RECno = ch.pro_RECno;
            db.PROJELERs.Attach(ch, true);
            db.SubmitChanges();
            //ch.pro_RECid_RECno = ch.pro_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        #endregion

        #region SON KULLANICILAR TANITIM KARTLARI

        
        public static List<SON_KULLANICILAR> SON_KULLANICILARi_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<SON_KULLANICILAR> ls = new List<SON_KULLANICILAR>();

            ls = (from T in db.SON_KULLANICILARs select T).ToList<SON_KULLANICILAR>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }


        public static List<SON_KULLANICILAR> SON_KULLANICILARi_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<SON_KULLANICILAR> ls = new List<SON_KULLANICILAR>();

            ls = (from T in db.SON_KULLANICILARs where T.tuk_lastup_date >= Tarih1 && T.tuk_lastup_date <= Tarih2 select T).ToList<SON_KULLANICILAR>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }


        public static SON_KULLANICILAR SON_KULLANICILAR_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            SON_KULLANICILAR tt = db.SON_KULLANICILARs.FirstOrDefault(t => t.tuk_kodu == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static SON_KULLANICILAR SON_KULLANICILARi_Kaydet(SON_KULLANICILAR ch, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.tuk_RECid_RECno = RandomDondur();
            db.SON_KULLANICILARs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.tuk_RECid_RECno = ch.tuk_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static SON_KULLANICILAR SON_KULLANICILARi_Guncelle(SON_KULLANICILAR ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.tuk_RECid_RECno = ch.tuk_RECno;
            db.SON_KULLANICILARs.Attach(ch, true);
            db.SubmitChanges();
            //ch.tuk_RECid_RECno = ch.tuk_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region SORUMLULUK MERKEZLERÝ TANITIM KARTLARI

        
        public static List<SORUMLULUK_MERKEZLERI> SORUMLULUK_MERKEZLERIi_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<SORUMLULUK_MERKEZLERI> ls = new List<SORUMLULUK_MERKEZLERI>();

            ls = (from T in db.SORUMLULUK_MERKEZLERIs select T).ToList<SORUMLULUK_MERKEZLERI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static List<SORUMLULUK_MERKEZLERI> SORUMLULUK_MERKEZLERIi_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try{
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<SORUMLULUK_MERKEZLERI> ls = new List<SORUMLULUK_MERKEZLERI>();

            ls = (from T in db.SORUMLULUK_MERKEZLERIs where T.som_lastup_date >= Tarih1 && T.som_lastup_date <= Tarih2 select T).ToList<SORUMLULUK_MERKEZLERI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static SORUMLULUK_MERKEZLERI SORUMLULUK_MERKEZLERI_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            SORUMLULUK_MERKEZLERI tt = db.SORUMLULUK_MERKEZLERIs.FirstOrDefault(t => t.som_kod == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static SORUMLULUK_MERKEZLERI SORUMLULUK_MERKEZLERIi_Kaydet(SORUMLULUK_MERKEZLERI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.som_RECid_RECno = RandomDondur();
            db.SORUMLULUK_MERKEZLERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.som_RECid_RECno = ch.som_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static SORUMLULUK_MERKEZLERI SORUMLULUK_MERKEZLERIi_Guncelle(SORUMLULUK_MERKEZLERI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.som_RECid_RECno = ch.som_RECno;
            db.SORUMLULUK_MERKEZLERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.som_RECid_RECno = ch.som_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion


        // yok
        #region SUBELER TANITIM KARTLARI


        public static List<SUBELER> SUBELERi_Yukle(string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<SUBELER> ls = new List<SUBELER>();

            ls = (from T in db.SUBELERs select T).ToList<SUBELER>();

            return ls;

            }
        }

        public static SUBELER SUBELER_EvrakDetayGetir(Int32 firmano,Int32 subeno, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            SUBELER tt = db.SUBELERs.FirstOrDefault(t => t.Sube_no == subeno && t.Sube_bag_firma == firmano);

            return tt;

            }
        }

        public static SUBELER SUBELERi_Kaydet(SUBELER ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.SUBELERs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.Sube_RECid_RECno = ch.Sube_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        public static SUBELER SUBELERi_Guncelle(SUBELER ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.SUBELERs.Attach(ch, true);
            db.SubmitChanges();
            //ch.Sube_RECid_RECno = ch.Sube_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        #endregion

        // yok
        #region TESLÝM TÜRLERÝ TANITIM KARTLARI
        

        public static List<TESLIM_TURLERI> TESLIM_TURLERIi_Yukle(string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<TESLIM_TURLERI> ls = new List<TESLIM_TURLERI>();

            ls = (from T in db.TESLIM_TURLERIs select T).ToList<TESLIM_TURLERI>();

            return ls;

            }
        }

        public static TESLIM_TURLERI TESLIM_TURLERI_EvrakDetayGetir(string kod, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            TESLIM_TURLERI tt = db.TESLIM_TURLERIs.FirstOrDefault(t => t.tslt_kod == kod);

            return tt;

            }
        }

        public static TESLIM_TURLERI TESLIM_TURLERIi_Kaydet(TESLIM_TURLERI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.TESLIM_TURLERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.tslt_RECid_RECno = ch.tslt_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        public static TESLIM_TURLERI TESLIM_TURLERIi_Guncelle(TESLIM_TURLERI ch, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            db.TESLIM_TURLERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.tslt_RECid_RECno = ch.tslt_RECno;
            //db.SubmitChanges();
            return ch;

            }
        }

        #endregion

        #region URUN TANITIM KARTLARI


        public static List<URUNLER> URUNLERi_Yukle(string conn)
        {

            try{
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<URUNLER> ls = new List<URUNLER>();

            ls = (from T in db.URUNLERs select T).ToList<URUNLER>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static List<URUNLER> URUNLERi_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<URUNLER> ls = new List<URUNLER>();

            ls = (from T in db.URUNLERs where T.uru_lastup_date >= Tarih1 && T.uru_lastup_date <= Tarih2 select T).ToList<URUNLER>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static URUNLER URUNLER_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            URUNLER tt = db.URUNLERs.FirstOrDefault(t => t.uru_stok_kod == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static URUNLER URUNLERi_Kaydet(URUNLER ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.uru_RECid_RECno = RandomDondur();
            db.URUNLERs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.uru_RECid_RECno = ch.uru_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static URUNLER URUNLERi_Guncelle(URUNLER ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.uru_RECid_RECno = ch.uru_RECno;
            db.URUNLERs.Attach(ch, true);
            db.SubmitChanges();
            //ch.uru_RECid_RECno = ch.uru_RECno;
            //db.SubmitChanges();
            return ch;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region URETIM MALZEME PLANLAMA AKTARIMI

        public static List<URETIM_MALZEME_PLANLAMA> URETIMMALZPLANLAMA_Yukle(string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<URETIM_MALZEME_PLANLAMA> ls = new List<URETIM_MALZEME_PLANLAMA>();

                ls = (from T in db.URETIM_MALZEME_PLANLAMAs select T).ToList<URETIM_MALZEME_PLANLAMA>();

                return ls;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static List<URETIM_MALZEME_PLANLAMA> URETIMMALZPLANLAMA_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<URETIM_MALZEME_PLANLAMA> ls = new List<URETIM_MALZEME_PLANLAMA>();

                ls = (from T in db.URETIM_MALZEME_PLANLAMAs where T.upl_lastup_date >= Tarih1 && T.upl_lastup_date <= Tarih2 select T).ToList<URETIM_MALZEME_PLANLAMA>();

                return ls;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static URETIM_MALZEME_PLANLAMA URETIMMALZPLANLAMA_EvrakDetayGetir(string isemri, int satirno, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                URETIM_MALZEME_PLANLAMA tt = db.URETIM_MALZEME_PLANLAMAs.FirstOrDefault(t => t.upl_isemri == isemri && t.upl_satirno == satirno);

                return tt;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static URETIM_MALZEME_PLANLAMA URETIMMALZPLANLAMA_Kaydet(URETIM_MALZEME_PLANLAMA ch, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                //ch.uru_RECid_RECno = RandomDondur();
                db.URETIM_MALZEME_PLANLAMAs.InsertOnSubmit(ch);
                db.SubmitChanges();
                //ch.uru_RECid_RECno = ch.uru_RECno;
                //db.SubmitChanges();
                return ch;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static URETIM_MALZEME_PLANLAMA URETIMMALZPLANLAMA_Guncelle(URETIM_MALZEME_PLANLAMA ch, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                //ch.uru_RECid_RECno = ch.uru_RECno;
                db.URETIM_MALZEME_PLANLAMAs.Attach(ch, true);
                db.SubmitChanges();
                //ch.uru_RECid_RECno = ch.uru_RECno;
                //db.SubmitChanges();
                return ch;
            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region FINANSAL SOZLESMELER AKTARIMI

        public static List<FINANSAL_SOZLESMELER> FINANSAL_SOZLESMELER_Yukle(string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<FINANSAL_SOZLESMELER> ls = new List<FINANSAL_SOZLESMELER>();

                ls = (from T in db.FINANSAL_SOZLESMELERs select T).ToList<FINANSAL_SOZLESMELER>();

                return ls;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static List<FINANSAL_SOZLESMELER> FINANSAL_SOZLESMELER_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<FINANSAL_SOZLESMELER> ls = new List<FINANSAL_SOZLESMELER>();

                ls = (from T in db.FINANSAL_SOZLESMELERs where T.FS_lastup_date >= Tarih1 && T.FS_lastup_date <= Tarih2 select T).ToList<FINANSAL_SOZLESMELER>();

                return ls;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static FINANSAL_SOZLESMELER FINANSAL_SOZLESMELER_EvrakDetayGetir(string fskodu, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                FINANSAL_SOZLESMELER tt = db.FINANSAL_SOZLESMELERs.FirstOrDefault(t => t.FS_sozkodu == fskodu);

                return tt;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static FINANSAL_SOZLESMELER FINANSAL_SOZLESMELER_Kaydet(FINANSAL_SOZLESMELER ch, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                //ch.uru_RECid_RECno = RandomDondur();
                db.FINANSAL_SOZLESMELERs.InsertOnSubmit(ch);
                db.SubmitChanges();
                //ch.uru_RECid_RECno = ch.uru_RECno;
                //db.SubmitChanges();
                return ch;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static FINANSAL_SOZLESMELER FINANSAL_SOZLESMELER_Guncelle(FINANSAL_SOZLESMELER ch, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                //ch.uru_RECid_RECno = ch.uru_RECno;
                db.FINANSAL_SOZLESMELERs.Attach(ch, true);
                db.SubmitChanges();
                //ch.uru_RECid_RECno = ch.uru_RECno;
                //db.SubmitChanges();
                return ch;
            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region FINANSAL SOZLESME TAKSITLERI AKTARIMI


        public static List<FINANSAL_SOZLESME_TAKSITLERI> FINANSAL_SOZLESME_TAKSITLERI_Yukle(string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<FINANSAL_SOZLESME_TAKSITLERI> ls = new List<FINANSAL_SOZLESME_TAKSITLERI>();

                ls = (from T in db.FINANSAL_SOZLESME_TAKSITLERIs select T).ToList<FINANSAL_SOZLESME_TAKSITLERI>();

                return ls;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static List<FINANSAL_SOZLESME_TAKSITLERI> FINANSAL_SOZLESME_TAKSITLERI_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<FINANSAL_SOZLESME_TAKSITLERI> ls = new List<FINANSAL_SOZLESME_TAKSITLERI>();

                ls = (from T in db.FINANSAL_SOZLESME_TAKSITLERIs where T.FST_lastup_date >= Tarih1 && T.FST_lastup_date <= Tarih2 select T).ToList<FINANSAL_SOZLESME_TAKSITLERI>();

                return ls;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static FINANSAL_SOZLESME_TAKSITLERI FINANSAL_SOZLESME_TAKSITLERI_EvrakDetayGetir(string fstkod, short? taksitno, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                FINANSAL_SOZLESME_TAKSITLERI tt = db.FINANSAL_SOZLESME_TAKSITLERIs.FirstOrDefault(t => t.FST_sozkodu == fstkod && t.FST_taksitno == taksitno);

                return tt;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static FINANSAL_SOZLESME_TAKSITLERI FINANSAL_SOZLESME_TAKSITLERI_Kaydet(FINANSAL_SOZLESME_TAKSITLERI ch, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                //ch.uru_RECid_RECno = RandomDondur();
                db.FINANSAL_SOZLESME_TAKSITLERIs.InsertOnSubmit(ch);
                db.SubmitChanges();
                //ch.uru_RECid_RECno = ch.uru_RECno;
                //db.SubmitChanges();
                return ch;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static FINANSAL_SOZLESME_TAKSITLERI FINANSAL_SOZLESME_TAKSITLERI_Guncelle(FINANSAL_SOZLESME_TAKSITLERI ch, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                //ch.uru_RECid_RECno = ch.uru_RECno;
                db.FINANSAL_SOZLESME_TAKSITLERIs.Attach(ch, true);
                db.SubmitChanges();
                //ch.uru_RECid_RECno = ch.uru_RECno;
                //db.SubmitChanges();
                return ch;
            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region URUN REÇETELERÝ AKTARIMI

        public static List<URUN_RECETELERI> URUNRECETE_Yukle(string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<URUN_RECETELERI> ls = new List<URUN_RECETELERI>();

                ls = (from T in db.URUN_RECETELERIs select T).ToList<URUN_RECETELERI>();

                return ls;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static List<URUN_RECETELERI> URUNRECETE_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<URUN_RECETELERI> ls = new List<URUN_RECETELERI>();

                ls = (from T in db.URUN_RECETELERIs where T.rec_lastup_date >= Tarih1 && T.rec_lastup_date <= Tarih2 select T).ToList<URUN_RECETELERI>();

                return ls;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static URUN_RECETELERI URUNRECETE_EvrakDetayGetir(string kod,string tkod, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                URUN_RECETELERI tt = db.URUN_RECETELERIs.FirstOrDefault(t => t.rec_anakod == kod && t.rec_tuketim_kod==tkod);

                return tt;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static URUN_RECETELERI URUNRECETE_Kaydet(URUN_RECETELERI ch, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                //ch.uru_RECid_RECno = RandomDondur();
                db.URUN_RECETELERIs.InsertOnSubmit(ch);
                db.SubmitChanges();
                //ch.uru_RECid_RECno = ch.uru_RECno;
                //db.SubmitChanges();
                return ch;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static URUN_RECETELERI URUNRECETE_Guncelle(URUN_RECETELERI ch, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                //ch.uru_RECid_RECno = ch.uru_RECno;
                db.URUN_RECETELERIs.Attach(ch, true);
                db.SubmitChanges();
                //ch.uru_RECid_RECno = ch.uru_RECno;
                //db.SubmitChanges();
                return ch;
            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #region ISEMIRLERI TANITIM KARTLARI


        public static List<ISEMIRLERI> ISEMIRLERI_Yukle(string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<ISEMIRLERI> ls = new List<ISEMIRLERI>();

                ls = (from T in db.ISEMIRLERIs select T).ToList<ISEMIRLERI>();


                return ls;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<ISEMIRLERI> ISEMIRLERI_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<ISEMIRLERI> ls = new List<ISEMIRLERI>();

                ls = (from T in db.ISEMIRLERIs where T.is_lastup_date >= Tarih1 && T.is_lastup_date <= Tarih2 select T).ToList<ISEMIRLERI>();

                return ls;


            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }


        public static ISEMIRLERI ISEMIRLERI_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                ISEMIRLERI tt = db.ISEMIRLERIs.FirstOrDefault(t => t.is_Kod == kod);

                return tt;


            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static ISEMIRLERI ISEMIRLERI_Kaydet(ISEMIRLERI ch, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                //ch.pro_RECid_RECno = RandomDondur();
                db.ISEMIRLERIs.InsertOnSubmit(ch);
                db.SubmitChanges();
                //ch.pro_RECid_RECno = ch.pro_RECno;
                //db.SubmitChanges();
                return ch;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static ISEMIRLERI ISEMIRLERI_Guncelle(ISEMIRLERI ch, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                //ch.pro_RECid_RECno = ch.pro_RECno;
                db.ISEMIRLERIs.Attach(ch, true);
                db.SubmitChanges();
                //ch.pro_RECid_RECno = ch.pro_RECno;
                //db.SubmitChanges();
                return ch;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        #endregion


        #region MUHASEBE HESAP PLANI TANITIM KARTLARI


        public static List<MUHASEBE_HESAP_PLANI> MUHASEBE_HESAP_PLANIi_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<MUHASEBE_HESAP_PLANI> ls = new List<MUHASEBE_HESAP_PLANI>();

            ls = (from T in db.MUHASEBE_HESAP_PLANIs where T.muh_lastup_date >= Tarih1 && T.muh_lastup_date <= Tarih2 select T).ToList<MUHASEBE_HESAP_PLANI>();

            return ls;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<MUHASEBE_HESAP_PLANI> MUHASEBE_HESAP_PLANIi_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<MUHASEBE_HESAP_PLANI> ls = new List<MUHASEBE_HESAP_PLANI>();

            ls = (from T in db.MUHASEBE_HESAP_PLANIs select T).ToList<MUHASEBE_HESAP_PLANI>();

            return ls;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static MUHASEBE_HESAP_PLANI MUHASEBE_HESAP_PLANI_EvrakDetayGetir(string kod, string conn)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            MUHASEBE_HESAP_PLANI tt = db.MUHASEBE_HESAP_PLANIs.FirstOrDefault(t => t.muh_hesap_kod == kod);

            return tt;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static MUHASEBE_HESAP_PLANI MUHASEBE_HESAP_PLANIi_Kaydet(MUHASEBE_HESAP_PLANI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.muh_RECid_RECno = RandomDondur();
            db.MUHASEBE_HESAP_PLANIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.muh_RECid_RECno = ch.muh_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static MUHASEBE_HESAP_PLANI MUHASEBE_HESAP_PLANIi_Guncelle(MUHASEBE_HESAP_PLANI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.muh_RECid_RECno = ch.muh_RECno;
            db.MUHASEBE_HESAP_PLANIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.muh_RECid_RECno = ch.muh_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        #endregion

        #region ÝHRACAT DOSYALARI  TANITIM KARTLARI


        public static List<IHRACAT_DOSYALARI> IHRACAT_DOSYALARIi_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<IHRACAT_DOSYALARI> ls = new List<IHRACAT_DOSYALARI>();

            ls = (from T in db.IHRACAT_DOSYALARIs select T).ToList<IHRACAT_DOSYALARI>();

            return ls;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<IHRACAT_DOSYALARI> IHRACAT_DOSYALARIi_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<IHRACAT_DOSYALARI> ls = new List<IHRACAT_DOSYALARI>();

            ls = (from T in db.IHRACAT_DOSYALARIs where T.ihr_lastup_date >= Tarih1 && T.ihr_lastup_date <= Tarih2 select T).ToList<IHRACAT_DOSYALARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static IHRACAT_DOSYALARI IHRACAT_DOSYALARI_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            IHRACAT_DOSYALARI tt = db.IHRACAT_DOSYALARIs.FirstOrDefault(t => t.ihr_kodu == kod);

            return tt;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static IHRACAT_DOSYALARI IHRACAT_DOSYALARIi_Kaydet(IHRACAT_DOSYALARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.ihr_RECid_RECno = RandomDondur();
            db.IHRACAT_DOSYALARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.ihr_RECid_RECno = ch.ihr_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static IHRACAT_DOSYALARI IHRACAT_DOSYALARIi_Guncelle(IHRACAT_DOSYALARI ch, string conn)
        {

            try
            {

            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.ihr_RECid_RECno = ch.ihr_RECno;
            db.IHRACAT_DOSYALARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.ihr_RECid_RECno = ch.ihr_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        #endregion

        #region ÝTHALAT DOSYALARI TANITIM KARTLARI

        
        public static List<ITHALAT_DOSYALARI> ITHALAT_DOSYALARIi_Yukle(string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<ITHALAT_DOSYALARI> ls = new List<ITHALAT_DOSYALARI>();

            ls = (from T in db.ITHALAT_DOSYALARIs select T).ToList<ITHALAT_DOSYALARI>();

            return ls;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<ITHALAT_DOSYALARI> ITHALAT_DOSYALARIi_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<ITHALAT_DOSYALARI> ls = new List<ITHALAT_DOSYALARI>();

            ls = (from T in db.ITHALAT_DOSYALARIs where T.ith_lastup_date >= Tarih1 && T.ith_lastup_date <= Tarih2 select T).ToList<ITHALAT_DOSYALARI>();

            return ls;
            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static ITHALAT_DOSYALARI ITHALAT_DOSYALARI_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            ITHALAT_DOSYALARI tt = db.ITHALAT_DOSYALARIs.FirstOrDefault(t => t.ith_kodu == kod);

            return tt;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static ITHALAT_DOSYALARI ITHALAT_DOSYALARIi_Kaydet(ITHALAT_DOSYALARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.ith_RECid_RECno = RandomDondur();
            db.ITHALAT_DOSYALARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.ith_RECid_RECno = ch.ith_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static ITHALAT_DOSYALARI ITHALAT_DOSYALARIi_Guncelle(ITHALAT_DOSYALARI ch, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            //ch.ith_RECid_RECno = ch.ith_RECno;
            db.ITHALAT_DOSYALARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.ith_RECid_RECno = ch.ith_RECno;
            //db.SubmitChanges();
            return ch;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        static Int32 RandomDondur()
        {
            Random rnd = new Random();
            Int32 sayi;
            sayi = rnd.Next(100, 10000);
            sayi = sayi - (2 * sayi);

            return sayi;


        }


        #endregion



        #region KREDÝ KARTI SÖZLEÞMELERÝ


        public static List<KREDI_SOZLESMESI_TANIMLARI> KREDI_SOZLESMESI_TANIMLARI_Yukle(string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<KREDI_SOZLESMESI_TANIMLARI> ls = new List<KREDI_SOZLESMESI_TANIMLARI>();

                ls = (from T in db.KREDI_SOZLESMESI_TANIMLARIs select T).ToList<KREDI_SOZLESMESI_TANIMLARI>();

                return ls;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<KREDI_SOZLESMESI_TANIMLARI> KREDI_SOZLESMESI_TANIMLARI_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<KREDI_SOZLESMESI_TANIMLARI> ls = new List<KREDI_SOZLESMESI_TANIMLARI>();

                ls = (from T in db.KREDI_SOZLESMESI_TANIMLARIs where T.krsoz_lastup_date >= Tarih1 && T.krsoz_lastup_date <= Tarih2 select T).ToList<KREDI_SOZLESMESI_TANIMLARI>();

                return ls;
            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static KREDI_SOZLESMESI_TANIMLARI KREDI_SOZLESMESI_TANIMLARI_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                KREDI_SOZLESMESI_TANIMLARI tt = db.KREDI_SOZLESMESI_TANIMLARIs.FirstOrDefault(t => t.krsoz_kodu == kod);

                return tt;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static KREDI_SOZLESMESI_TANIMLARI KREDI_SOZLESMESI_TANIMLARI_Kaydet(KREDI_SOZLESMESI_TANIMLARI ch, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                //ch.krsoz_RECid_RECno = RandomDondur();
                db.KREDI_SOZLESMESI_TANIMLARIs.InsertOnSubmit(ch);
                db.SubmitChanges();
                //ch.krsoz_RECid_RECno = ch.krsoz_RECno;
                //db.SubmitChanges();
                return ch;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static KREDI_SOZLESMESI_TANIMLARI KREDI_SOZLESMESI_TANIMLARI_Guncelle(KREDI_SOZLESMESI_TANIMLARI ch, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                //ch.krsoz_RECid_RECno = ch.krsoz_RECno;
                db.KREDI_SOZLESMESI_TANIMLARIs.Attach(ch, true);
                db.SubmitChanges();
                //ch.krsoz_RECid_RECno = ch.krsoz_RECno;
                //db.SubmitChanges();
                return ch;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion



        #region KREDÝ TAKSÝT TANIMLARI


        public static List<KREDI_SOZLESMESI_TAKSIT_TANIMLARI> KREDI_TAKSIT_TANIMLARI_Yukle(string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<KREDI_SOZLESMESI_TAKSIT_TANIMLARI> ls = new List<KREDI_SOZLESMESI_TAKSIT_TANIMLARI>();

                ls = (from T in db.KREDI_SOZLESMESI_TAKSIT_TANIMLARIs select T).ToList<KREDI_SOZLESMESI_TAKSIT_TANIMLARI>();

                return ls;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static List<KREDI_SOZLESMESI_TAKSIT_TANIMLARI> KREDI_TAKSIT_TANIMLARI_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<KREDI_SOZLESMESI_TAKSIT_TANIMLARI> ls = new List<KREDI_SOZLESMESI_TAKSIT_TANIMLARI>();

                ls = (from T in db.KREDI_SOZLESMESI_TAKSIT_TANIMLARIs where T.krsoztaksit_lastup_date >= Tarih1 && T.krsoztaksit_lastup_date <= Tarih2 select T).ToList<KREDI_SOZLESMESI_TAKSIT_TANIMLARI>();

                return ls;
            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static KREDI_SOZLESMESI_TAKSIT_TANIMLARI KREDI_TAKSIT_TANIMLARI_EvrakDetayGetir(string kod,int tno ,string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                KREDI_SOZLESMESI_TAKSIT_TANIMLARI tt = db.KREDI_SOZLESMESI_TAKSIT_TANIMLARIs.FirstOrDefault(t => t.krsoztaksit_sozkodu == kod && t.krsoztaksit_taksitno==tno);

                return tt;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static KREDI_SOZLESMESI_TAKSIT_TANIMLARI KREDI_TAKSIT_TANIMLARI_Kaydet(KREDI_SOZLESMESI_TAKSIT_TANIMLARI ch, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                //ch.krsoztaksit_RECid_RECno = RandomDondur();
                db.KREDI_SOZLESMESI_TAKSIT_TANIMLARIs.InsertOnSubmit(ch);
                db.SubmitChanges();
                //ch.krsoztaksit_RECid_RECno = ch.krsoztaksit_RECno;
                //db.SubmitChanges();
                return ch;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static KREDI_SOZLESMESI_TAKSIT_TANIMLARI KREDI_TAKSIT_TANIMLARI_Guncelle(KREDI_SOZLESMESI_TAKSIT_TANIMLARI ch, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                //ch.krsoztaksit_RECid_RECno = ch.krsoztaksit_RECno;
                db.KREDI_SOZLESMESI_TAKSIT_TANIMLARIs.Attach(ch, true);
                db.SubmitChanges();
                //ch.krsoztaksit_RECid_RECno = ch.krsoztaksit_RECno;
                //db.SubmitChanges();
                return ch;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion


        #region DÖNEMLERE YAYILAN HÝZMETLER TANITIM KARTLARI


        public static List<DONEMLERE_YAYILAN_HIZMETLER> DONEMLERE_YAYILAN_HIZMETLER_Yukle(string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<DONEMLERE_YAYILAN_HIZMETLER> ls = new List<DONEMLERE_YAYILAN_HIZMETLER>();

                ls = (from T in db.DONEMLERE_YAYILAN_HIZMETLERs select T).ToList<DONEMLERE_YAYILAN_HIZMETLER>();

                return ls;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static List<DONEMLERE_YAYILAN_HIZMETLER> DONEMLERE_YAYILAN_HIZMETLER_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<DONEMLERE_YAYILAN_HIZMETLER> ls = new List<DONEMLERE_YAYILAN_HIZMETLER>();

                ls = (from T in db.DONEMLERE_YAYILAN_HIZMETLERs where T.dyh_lastup_date >= Tarih1 && T.dyh_lastup_date <= Tarih2 select T).ToList<DONEMLERE_YAYILAN_HIZMETLER>();

                return ls;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static DONEMLERE_YAYILAN_HIZMETLER DONEMLERE_YAYILAN_HIZMETLER_EvrakDetayGetir(string kod, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                DONEMLERE_YAYILAN_HIZMETLER tt = db.DONEMLERE_YAYILAN_HIZMETLERs.FirstOrDefault(t => t.dyh_kodu == kod);

                return tt;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static DONEMLERE_YAYILAN_HIZMETLER DONEMLERE_YAYILAN_HIZMETLER_Kaydet(DONEMLERE_YAYILAN_HIZMETLER ch, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                //ch.dyh_RECid_RECno = RandomDondur();
                db.DONEMLERE_YAYILAN_HIZMETLERs.InsertOnSubmit(ch);
                db.SubmitChanges();
                //ch.dyh_RECid_RECno = ch.dyh_RECno;
                //db.SubmitChanges();
                return ch;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static DONEMLERE_YAYILAN_HIZMETLER DONEMLERE_YAYILAN_HIZMETLER_Guncelle(DONEMLERE_YAYILAN_HIZMETLER ch, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                //ch.dyh_RECid_RECno = ch.dyh_RECno;
                db.DONEMLERE_YAYILAN_HIZMETLERs.Attach(ch, true);
                db.SubmitChanges();
                //ch.dyh_RECid_RECno = ch.dyh_RECno;
                //db.SubmitChanges();
                return ch;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        #endregion

        #endregion


    }

}
