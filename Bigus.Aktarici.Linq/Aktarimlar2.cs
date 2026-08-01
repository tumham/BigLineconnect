using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace Bigus.Aktarici.Linq
{
    public class Aktarimlar2
    {


        public static List<STOK_HAREKETLERI> HareketleriYukle(string[] SeriNo, DateTime Tarih1, DateTime Tarih2, string conn, bool tipBelirle, int[] sthTip, int[] sthCins, int[] sthNormalIade, int[] sthEvrakTip, bool srm, string[] srm_merkezleri)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_HAREKETLERI> ls = new List<STOK_HAREKETLERI>();
            if (tipBelirle)
            {
                if (srm)
                {
                    ls = (from T in db.STOK_HAREKETLERIs where (T.sth_kilitli == false || T.sth_kilitli == null) && (SeriNo.Contains(T.sth_evrakno_seri)) && (T.sth_tarih >= Tarih1 && T.sth_tarih <= Tarih2) && !(srm_merkezleri.Contains(T.sth_stok_srm_merkezi)) && sthTip.Contains((int)T.sth_tip) && sthCins.Contains((int)T.sth_cins) && sthNormalIade.Contains((int)T.sth_normal_iade) && sthEvrakTip.Contains((int)T.sth_evraktip) select T).ToList<STOK_HAREKETLERI>();
                 
                }
                else
                {
                    ls = (from T in db.STOK_HAREKETLERIs where (T.sth_kilitli == false || T.sth_kilitli == null) && (SeriNo.Contains(T.sth_evrakno_seri)) && (T.sth_tarih >= Tarih1 && T.sth_tarih <= Tarih2) && sthTip.Contains((int)T.sth_tip) && sthCins.Contains((int)T.sth_cins) && sthNormalIade.Contains((int)T.sth_normal_iade) && sthEvrakTip.Contains((int)T.sth_evraktip) select T).ToList<STOK_HAREKETLERI>();
               
                } 
            }
            else
            {
                if (srm)
                {
                    ls = (from T in db.STOK_HAREKETLERIs where (T.sth_kilitli == false || T.sth_kilitli == null) && (SeriNo.Contains(T.sth_evrakno_seri)) && !(srm_merkezleri.Contains(T.sth_stok_srm_merkezi)) && (T.sth_tarih >= Tarih1 && T.sth_tarih <= Tarih2) select T).ToList<STOK_HAREKETLERI>();
           
                }
                else
                {ls = (from T in db.STOK_HAREKETLERIs where (T.sth_kilitli == false || T.sth_kilitli == null) && (SeriNo.Contains(T.sth_evrakno_seri))&&(T.sth_tarih >= Tarih1 && T.sth_tarih <= Tarih2) select T).ToList<STOK_HAREKETLERI>();
           
                }
            }
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

        public static List<STOK_HAREKETLERI> HareketleriYukle_anlik(string[] SeriNo, DateTime Tarih1, DateTime Tarih2, string conn, bool tipBelirle, int[] sthTip, int[] sthCins, int[] sthNormalIade, int[] sthEvrakTip, bool srm, string[] srm_merkezleri)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<STOK_HAREKETLERI> ls = new List<STOK_HAREKETLERI>();
            if (tipBelirle)
            {
                if (srm)
                {
                    ls = (from T in db.STOK_HAREKETLERIs where (T.sth_kilitli == false || T.sth_kilitli == null) && (SeriNo.Contains(T.sth_evrakno_seri)) && (T.sth_lastup_date.Value.Date >= Tarih1.Date && T.sth_lastup_date.Value.Date <= Tarih2.Date) && !(srm_merkezleri.Contains(T.sth_stok_srm_merkezi)) && sthTip.Contains((int)T.sth_tip) && sthCins.Contains((int)T.sth_cins) && sthNormalIade.Contains((int)T.sth_normal_iade) && sthEvrakTip.Contains((int)T.sth_evraktip) select T).ToList<STOK_HAREKETLERI>();                
            
                }
                else
                {
                    ls = (from T in db.STOK_HAREKETLERIs where (T.sth_kilitli == false || T.sth_kilitli == null) && (SeriNo.Contains(T.sth_evrakno_seri)) && (T.sth_lastup_date.Value.Date >= Tarih1.Date && T.sth_lastup_date.Value.Date <= Tarih2.Date) && sthTip.Contains((int)T.sth_tip) && sthCins.Contains((int)T.sth_cins) && sthNormalIade.Contains((int)T.sth_normal_iade) && sthEvrakTip.Contains((int)T.sth_evraktip) select T).ToList<STOK_HAREKETLERI>();                
            
                }
            }
            else
            {
                if (srm)
                {
                    ls = (from T in db.STOK_HAREKETLERIs where (T.sth_kilitli == false || T.sth_kilitli == null) && (SeriNo.Contains(T.sth_evrakno_seri)) && !(srm_merkezleri.Contains(T.sth_stok_srm_merkezi)) && (T.sth_lastup_date.Value.Date >= Tarih1.Date && T.sth_lastup_date.Value.Date <= Tarih2.Date) select T).ToList<STOK_HAREKETLERI>();
           
                }
                else
                {
                    ls = (from T in db.STOK_HAREKETLERIs where (T.sth_kilitli == false || T.sth_kilitli == null) && (SeriNo.Contains(T.sth_evrakno_seri)) && (T.sth_lastup_date.Value.Date >= Tarih1.Date && T.sth_lastup_date.Value.Date <= Tarih2.Date) select T).ToList<STOK_HAREKETLERI>();
           
                }
            }
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
        public static List<STOK_HAREKETLERI_EK> EKHareketleriYukle_anlik(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<STOK_HAREKETLERI_EK> ls = new List<STOK_HAREKETLERI_EK>();

                ls = (from T in db.STOK_HAREKETLERI_EKs where (T.sthek_lastup_date.Value.Date >= Tarih1.Date && T.sthek_lastup_date.Value.Date <= Tarih2.Date) select T).ToList<STOK_HAREKETLERI_EK>();
               
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


        public static List<STOK_HAREKETLERI> Stok_Hareketlerini_Yukle(Guid recno, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {


            //var tt = from T in db.STOK_HAREKETLERIs where T.sth_fat_recid_recno == recno select new { T.sth_evraktip, T.sth_tip, T.sth_evrakno_seri, T.sth_evrakno_sira, T.sth_satirno, T.sth_RECno };

            List<STOK_HAREKETLERI> ls = new List<STOK_HAREKETLERI>();

            ls = (from T in db.STOK_HAREKETLERIs where T.sth_fat_uid == recno select T).ToList<STOK_HAREKETLERI>();

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
        //public static List<STOK_HAREKETLERI_EK> StokEK_Hareketlerini_Yukle(Int32 recno,DateTime Tarih1, DateTime Tarih2,string conn)
        //{

        //    try
        //    {
        //        using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
        //        {


        //        //var tt = from T in db.STOK_HAREKETLERIs where T.sth_fat_recid_recno == recno select new { T.sth_evraktip, T.sth_tip, T.sth_evrakno_seri, T.sth_evrakno_sira, T.sth_satirno, T.sth_RECno };

        //        List<STOK_HAREKETLERI_EK> ls = new List<STOK_HAREKETLERI_EK>();

        //        ls = (from T in db.STOK_HAREKETLERI_EKs where (T.sthek_create_date >= Tarih1 && T.sthek_create_date <= Tarih2) && T.sthek_related_RECno == recno select T).ToList<STOK_HAREKETLERI_EK>();

        //        return ls;

        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
        //        System.Threading.Thread.CurrentThread.Abort();
        //        return null;

        //    }

        //}


        public static List<Evrak> Stok_Hareketleri_Yukle_2(string[] SeriNo, DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {


            var tt = from T in db.STOK_HAREKETLERIs where (T.sth_kilitli == false || T.sth_kilitli == null) && (SeriNo.Contains(T.sth_evrakno_seri)) && (T.sth_tarih >= Tarih1 && T.sth_tarih <= Tarih2) && T.sth_fat_uid == Guid.Empty select new { T.sth_evraktip, T.sth_tip, T.sth_evrakno_seri, T.sth_evrakno_sira, T.sth_satirno, T.sth_Guid };

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
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static List<Evrak> Cari_HareketleriYukle(string[] SeriNo, DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {


            var tx = from T in db.CARI_HESAP_HAREKETLERIs where (T.cha_kilitli == false || T.cha_kilitli == null) && (SeriNo.Contains(T.cha_evrakno_seri))  && (T.cha_tarihi >= Tarih1 && T.cha_tarihi <= Tarih2) && (T.cha_evrak_tip == 0 || T.cha_evrak_tip == 61 || T.cha_evrak_tip == 63) select new { T.cha_evrak_tip, T.cha_tip, T.cha_evrakno_seri, T.cha_evrakno_sira, T.cha_satir_no, T.cha_Guid };



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
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static List<CARI_HESAP_HAREKETLERI> Cari_HareketleriYukle_2(string[] SeriNo, DateTime Tarih1, DateTime Tarih2, string conn, bool tipBelirle, int[] chaTip, int[] chaCins, int[] chaNormalIade, int[] chaEvrakTip, bool srm, string[] srm_merkezleri)
        {
            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<CARI_HESAP_HAREKETLERI> ls = new List<CARI_HESAP_HAREKETLERI>();
            if (tipBelirle)
            {
                if (srm)
                {
                    ls = (from T in db.CARI_HESAP_HAREKETLERIs where (T.cha_kilitli == false || T.cha_kilitli == null) && (SeriNo.Contains(T.cha_evrakno_seri)) && (T.cha_tarihi >= Tarih1 && T.cha_tarihi <= Tarih2) && !(srm_merkezleri.Contains(T.cha_srmrkkodu)) && chaTip.Contains((int)T.cha_tip) && chaCins.Contains((int)T.cha_cinsi) && chaNormalIade.Contains((int)T.cha_normal_Iade) && chaEvrakTip.Contains((int)T.cha_evrak_tip) select T).ToList<CARI_HESAP_HAREKETLERI>();
         
                   
                }
                else
                {
                    ls = (from T in db.CARI_HESAP_HAREKETLERIs where (T.cha_kilitli == false || T.cha_kilitli == null) && (SeriNo.Contains(T.cha_evrakno_seri)) && (T.cha_tarihi >= Tarih1 && T.cha_tarihi <= Tarih2) && chaTip.Contains((int)T.cha_tip) && chaCins.Contains((int)T.cha_cinsi) && chaNormalIade.Contains((int)T.cha_normal_Iade) && chaEvrakTip.Contains((int)T.cha_evrak_tip) select T).ToList<CARI_HESAP_HAREKETLERI>();
         
                }
            }
            else
            {
                if (srm)
                {
                    ls = (from T in db.CARI_HESAP_HAREKETLERIs where (T.cha_kilitli == false || T.cha_kilitli == null) && (SeriNo.Contains(T.cha_evrakno_seri)) && !(srm_merkezleri.Contains(T.cha_srmrkkodu)) && (T.cha_tarihi >= Tarih1 && T.cha_tarihi <= Tarih2) select T).ToList<CARI_HESAP_HAREKETLERI>();
        
                  
                }
                else
                {
                    ls = (from T in db.CARI_HESAP_HAREKETLERIs where (T.cha_kilitli == false || T.cha_kilitli == null) && (SeriNo.Contains(T.cha_evrakno_seri)) && (T.cha_tarihi >= Tarih1 && T.cha_tarihi <= Tarih2) select T).ToList<CARI_HESAP_HAREKETLERI>();
        
                }
                
            }
            //var tt = from T in db.STOK_HAREKETLERIs where T.sth_evrakno_seri == SeriNo && (T.sth_tarih >= Tarih1 && T.sth_tarih <= Tarih2) select T; && (T.cha_evrak_tip != 0 && T.cha_evrak_tip != 61 && T.cha_evrak_tip != 63)

            //List<STOK_HAREKETLERI> ls = new List<STOK_HAREKETLERI>();
            //ls =tt.ToList<STOK_HAREKETLERI>();
            //return ls;
            
   

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


        public static List<CARI_HESAP_HAREKETLERI> Cari_HareketleriYukle_2_anlik(string[] SeriNo, DateTime Tarih1, DateTime Tarih2, string conn, bool tipBelirle, int[] chaTip, int[] chaCins, int[] chaNormalIade, int[] chaEvrakTip, bool srm, string[] srm_merkezleri)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<CARI_HESAP_HAREKETLERI> ls = new List<CARI_HESAP_HAREKETLERI>();

            if (tipBelirle)
            {
                if (srm)
                {
                    ls = (from T in db.CARI_HESAP_HAREKETLERIs where (T.cha_kilitli == false || T.cha_kilitli == null) && (SeriNo.Contains(T.cha_evrakno_seri)) && (T.cha_lastup_date.Value.Date >= Tarih1.Date && T.cha_lastup_date.Value.Date <= Tarih2.Date) && !(srm_merkezleri.Contains(T.cha_srmrkkodu)) && chaTip.Contains((int)T.cha_tip) && chaCins.Contains((int)T.cha_cinsi) && chaNormalIade.Contains((int)T.cha_normal_Iade) && chaEvrakTip.Contains((int)T.cha_evrak_tip) select T).ToList<CARI_HESAP_HAREKETLERI>();
                    
                }
                else
                {
                    ls = (from T in db.CARI_HESAP_HAREKETLERIs where (T.cha_kilitli == false || T.cha_kilitli == null) && (SeriNo.Contains(T.cha_evrakno_seri)) && (T.cha_lastup_date.Value.Date >= Tarih1.Date && T.cha_lastup_date.Value.Date <= Tarih2.Date) && chaTip.Contains((int)T.cha_tip) && chaCins.Contains((int)T.cha_cinsi) && chaNormalIade.Contains((int)T.cha_normal_Iade) && chaEvrakTip.Contains((int)T.cha_evrak_tip) select T).ToList<CARI_HESAP_HAREKETLERI>();
     
                }
                      
            }
            else
            {
                if (srm)
                {
                    ls = (from T in db.CARI_HESAP_HAREKETLERIs where (T.cha_kilitli == false || T.cha_kilitli == null) && (SeriNo.Contains(T.cha_evrakno_seri)) && !(srm_merkezleri.Contains(T.cha_srmrkkodu)) && (T.cha_lastup_date >= Tarih1 && T.cha_lastup_date <= Tarih2) select T).ToList<CARI_HESAP_HAREKETLERI>();
           
                    
                }
                else
                {
                    ls = (from T in db.CARI_HESAP_HAREKETLERIs where (T.cha_kilitli == false || T.cha_kilitli == null) && (SeriNo.Contains(T.cha_evrakno_seri)) && (T.cha_lastup_date.Value.Date >= Tarih1.Date && T.cha_lastup_date.Value.Date <= Tarih2.Date) select T).ToList<CARI_HESAP_HAREKETLERI>();
           
                }
                
            }
            //var tt = from T in db.STOK_HAREKETLERIs where T.sth_evrakno_seri == SeriNo && (T.sth_tarih >= Tarih1 && T.sth_tarih <= Tarih2) select T;&& (T.cha_evrak_tip != 0 && T.cha_evrak_tip != 61 && T.cha_evrak_tip != 63)

            //List<STOK_HAREKETLERI> ls = new List<STOK_HAREKETLERI>();
            //ls =tt.ToList<STOK_HAREKETLERI>();
            //return ls;



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


        public static List<SIPARISLER> SiparisleriYukle_anlik(string[] SeriNo, DateTime Tarih1, DateTime Tarih2, string conn, bool tipBelirle, int[] sipTip, int[] sipCins, bool srm, string[] srm_merkezleri)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<SIPARISLER> ls = new List<SIPARISLER>();

            if (tipBelirle)
            {
                if (srm)
                {
                    ls = (from T in db.SIPARISLERs where (SeriNo.Contains(T.sip_evrakno_seri)) && (T.sip_lastup_date.Value >= Tarih1.Date && T.sip_lastup_date.Value.Date <= Tarih2.Date) && !(srm_merkezleri.Contains(T.sip_stok_sormerk)) && sipTip.Contains((int)T.sip_tip) && sipCins.Contains((int)T.sip_cins) select T).ToList<SIPARISLER>();
          
                    
                }
                else
                {
                    ls = (from T in db.SIPARISLERs where (SeriNo.Contains(T.sip_evrakno_seri)) && (T.sip_lastup_date.Value.Date >= Tarih1.Date && T.sip_lastup_date.Value.Date <= Tarih2.Date) && sipTip.Contains((int)T.sip_tip) && sipCins.Contains((int)T.sip_cins) select T).ToList<SIPARISLER>();
          
                }
                
            }
            else
            {
                if (srm)
                {
                    ls = (from T in db.SIPARISLERs where (SeriNo.Contains(T.sip_evrakno_seri)) && !(srm_merkezleri.Contains(T.sip_stok_sormerk)) && (T.sip_lastup_date >= Tarih1 && T.sip_lastup_date <= Tarih2) select T).ToList<SIPARISLER>();
        
                }
                else
                {
                    ls = (from T in db.SIPARISLERs where (SeriNo.Contains(T.sip_evrakno_seri)) && (T.sip_lastup_date >= Tarih1 && T.sip_lastup_date <= Tarih2) select T).ToList<SIPARISLER>();
        
                }
                
            }

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

        public static List<SIPARISLER> SiparisleriYukle(string[] SeriNo, DateTime Tarih1, DateTime Tarih2, string conn, bool tipBelirle, int[] sipTip, int[] sipCins, bool srm, string[] srm_merkezleri)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<SIPARISLER> ls = new List<SIPARISLER>();
            if (tipBelirle)
            {
                if (srm)
                {
                    ls = (from T in db.SIPARISLERs where (SeriNo.Contains(T.sip_evrakno_seri)) && (T.sip_tarih >= Tarih1 && T.sip_tarih <= Tarih2) && !(srm_merkezleri.Contains(T.sip_stok_sormerk)) && sipTip.Contains((int)T.sip_tip) && sipCins.Contains((int)T.sip_cins) select T).ToList<SIPARISLER>();
            
                }
                else
                {
                    ls = (from T in db.SIPARISLERs where (SeriNo.Contains(T.sip_evrakno_seri)) && (T.sip_tarih >= Tarih1 && T.sip_tarih <= Tarih2) && sipTip.Contains((int)T.sip_tip) && sipCins.Contains((int)T.sip_cins) select T).ToList<SIPARISLER>();
            
                }
                
            }
            else
            {
                if (srm)
                {
                    ls = (from T in db.SIPARISLERs where (SeriNo.Contains(T.sip_evrakno_seri)) && !(srm_merkezleri.Contains(T.sip_stok_sormerk)) && (T.sip_tarih >= Tarih1 && T.sip_tarih <= Tarih2) select T).ToList<SIPARISLER>();
          
                }
                else
                {
                    ls = (from T in db.SIPARISLERs where (SeriNo.Contains(T.sip_evrakno_seri)) && (T.sip_tarih >= Tarih1 && T.sip_tarih <= Tarih2) select T).ToList<SIPARISLER>();
          
                }
                
            }

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




        public static List<BEDEN_HAREKETLERI> BedenHareketleriYukle(char BHarTipi, Guid RecNo, string conn)
        {


            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<BEDEN_HAREKETLERI> ls = new List<BEDEN_HAREKETLERI>();

            ls = (from T in db.BEDEN_HAREKETLERIs where T.BdnHar_Har_uid == RecNo select T).ToList<BEDEN_HAREKETLERI>();

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
        public static List<CIHAZ_HAREKETLERI> CihazHareketleriYukle(char HarTipi, Guid RecNo, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<CIHAZ_HAREKETLERI> ls = new List<CIHAZ_HAREKETLERI>();

            ls = (from T in db.CIHAZ_HAREKETLERIs where T.ChHar_master_uid == RecNo select T).ToList<CIHAZ_HAREKETLERI>();

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
        public static List<SATIS_SARTLARI> Satis_Sartlarini_Yukle(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<SATIS_SARTLARI> ls = new List<SATIS_SARTLARI>();
            ls = (from T in db.SATIS_SARTLARIs where (T.sat_evrak_tarih >= Tarih1 && T.sat_evrak_tarih <= Tarih2) select T).ToList<SATIS_SARTLARI>() ;

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

        public static List<SATIS_SARTLARI> Satis_Sartlarini_Yukle_anlik(DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            List<SATIS_SARTLARI> ls = new List<SATIS_SARTLARI>();
            ls = (from T in db.SATIS_SARTLARIs where (T.sat_lastup_date >= Tarih1 && T.sat_lastup_date <= Tarih2) select T).ToList<SATIS_SARTLARI>();

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

           
        public static bool Stok_Hareket_EvrakKontrol(Evrak ev, string conn)
        {

            try{
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            var tt = db.STOK_HAREKETLERIs.Count(T => (T.sth_evraktip == ev.EvrakTip && T.sth_tip == ev.Tip && T.sth_evrakno_seri == ev.SeriNo && T.sth_evrakno_sira == ev.SiraNo && T.sth_satirno == ev.SatirNo));

            Int32 sayi = Convert.ToInt32(tt);
            if (sayi > 0)
                return true;
            else
                return false;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return false; ;

            }

        }
        public static bool Cari_Hareket_EvrakKontrol(Evrak ev, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            var tt = db.CARI_HESAP_HAREKETLERIs.Count(T => (T.cha_evrak_tip == ev.EvrakTip && T.cha_tip == ev.Tip && T.cha_evrakno_seri == ev.SeriNo && T.cha_evrakno_sira == ev.SiraNo && T.cha_satir_no == ev.SatirNo));

            Int32 sayi = Convert.ToInt32(tt);
            if (sayi > 0)
                return true;
            else
                return false;

            }
                        }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return false;

            }

        }
        public static bool Siparis_EvrakKontrol(Evrak ev, string conn)
        {

            try
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
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return false;

            }

        }
        public static bool OdemeEmirleri_Kontrol(byte odemetip, string refno, string conn)
        {

            try
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
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return false;

            }
        }



        public static STOK_HAREKETLERI Stok_Hareket_EvrakDetayGetir(Guid RECno, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_HAREKETLERI tt = db.STOK_HAREKETLERIs.SingleOrDefault(t => t.sth_Guid == RECno);

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

        public static STOK_HAREKETLERI Stok_Hareket_EvrakDetayGetir_2(Evrak ev, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            STOK_HAREKETLERI tt = db.STOK_HAREKETLERIs.FirstOrDefault(T => (T.sth_evraktip == ev.EvrakTip && T.sth_tip == ev.Tip && T.sth_evrakno_seri == ev.SeriNo && T.sth_evrakno_sira == ev.SiraNo && T.sth_satirno == ev.SatirNo));
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


        public static CARI_HESAP_HAREKETLERI Cari_Hesap_Hareket_EvrakDetayGetir(Guid RECno, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            CARI_HESAP_HAREKETLERI tt = db.CARI_HESAP_HAREKETLERIs.SingleOrDefault(t => t.cha_Guid == RECno);

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
        public static STOK_HAREKETLERI_EK Stok_HareketEK_EvrakDetayGetir(Guid RECno, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                STOK_HAREKETLERI_EK tt = db.STOK_HAREKETLERI_EKs.SingleOrDefault(t => t.sthek_Guid == RECno);

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
        public static CARI_HESAP_HAREKETLERI_EK Cari_Hesap_HareketEK_EvrakDetayGetir(Guid RECno, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                CARI_HESAP_HAREKETLERI_EK tt = db.CARI_HESAP_HAREKETLERI_EKs.SingleOrDefault(t => t.chaek_related_uid == RECno);

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

        public static CARI_HESAP_HAREKETLERI Cari_Hesap_Hareket_EvrakDetayGetir_2(Evrak ev, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            CARI_HESAP_HAREKETLERI tt = db.CARI_HESAP_HAREKETLERIs.FirstOrDefault(T => T.cha_evrak_tip == ev.EvrakTip && T.cha_tip == ev.Tip && T.cha_evrakno_seri == ev.SeriNo && T.cha_evrakno_sira == ev.SiraNo && T.cha_satir_no == ev.SatirNo);

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
        public static STOK_HAREKETLERI_EK Stok_HareketEK_EvrakDetayGetir_2(Evrak ev, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                STOK_HAREKETLERI_EK tt = db.STOK_HAREKETLERI_EKs.FirstOrDefault(T => T.sthek_Guid == ev.RECno);

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

        public static CARI_HESAP_HAREKETLERI_EK Cari_Hesap_HareketEK_EvrakDetayGetir_2(Evrak ev, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                CARI_HESAP_HAREKETLERI_EK tt = db.CARI_HESAP_HAREKETLERI_EKs.FirstOrDefault(T => T.chaek_related_uid == ev.RECno);

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

        public static CARI_HESAP_HAREKETLERI_EK Cari_Hesap_HareketEK_EvrakDetayGetir_3(Guid chaRec, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                CARI_HESAP_HAREKETLERI_EK tt = db.CARI_HESAP_HAREKETLERI_EKs.FirstOrDefault(T => T.chaek_related_uid == chaRec);

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

        public static ODEME_EMIRLERI OdemeEmirleri_EvrakDetayGetir(byte odemetip, string refno, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            ODEME_EMIRLERI tt = db.ODEME_EMIRLERIs.FirstOrDefault(T => (T.sck_tip == odemetip && T.sck_refno == refno));

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


        public static SIPARISLER Siparis_EvrakDetayGetir(Guid RECno, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            SIPARISLER tt = db.SIPARISLERs.Single(t => t.sip_Guid  == RECno);

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

        public static SIPARISLER Siparis_EvrakDetayGetir_2(Evrak ev, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            SIPARISLER tt = db.SIPARISLERs.FirstOrDefault(T => (T.sip_tip == ev.EvrakTip && T.sip_cins == ev.Tip && T.sip_evrakno_seri == ev.SeriNo && T.sip_evrakno_sira == ev.SiraNo && T.sip_satirno == ev.SatirNo));

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

        public static SIPARISLER Siparisler_EvrakDetayGetir(Guid  RECno, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            
            SIPARISLER tt = db.SIPARISLERs.SingleOrDefault(t => t.sip_Guid == RECno);

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

   
        public static BEDEN_HAREKETLERI Beden_Hareketleri_EvrakDetayGetir(char tip,Guid recno,short bedenno,string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            BEDEN_HAREKETLERI tt = db.BEDEN_HAREKETLERIs.FirstOrDefault(T => (T.BdnHar_Har_uid==recno && T.BdnHar_BedenNo ==bedenno));

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

        public static CIHAZ_HAREKETLERI Cihaz_Hareketleri_EvrakDetayGetir(char tip, Guid recno, string serino, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            CIHAZ_HAREKETLERI tt = db.CIHAZ_HAREKETLERIs.FirstOrDefault(T => (T.ChHar_master_uid == recno && T.ChHar_SeriNo == serino));

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

        public static SATIS_SARTLARI Satis_Sartlari_EvrakDetayGetir(string serino,Int32 sirano,Int32 satirno, string conn)
        {

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            SATIS_SARTLARI tt = db.SATIS_SARTLARIs.FirstOrDefault(T => (T.sat_evrakno_seri == serino && T.sat_evrakno_sira == sirano && T.sat_satirno == satirno));

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

        public static CARI_HESAP_HAREKETLERI Cari_Hesap_Hareket_Kaydet(CARI_HESAP_HAREKETLERI ch, string conn)
        {
            #region Kullan�c�
            if (AktarimParametreleri.Parametre.akt_kullanici_no.Value != 0)
            {
                ch.cha_create_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
                ch.cha_lastup_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
            }
            #endregion

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            ch.cha_fis_sirano = 0;
            ch.cha_fis_tarih = Convert.ToDateTime("1899-12-30 00:00:00.000");
            ch.cha_special1 = "BGS";
            db.CARI_HESAP_HAREKETLERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.cha_RECid_RECno = ch.cha_RECno;
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

        public static STOK_HAREKETLERI_EK Stok_HareketEK_Kaydet(STOK_HAREKETLERI_EK shek, string conn)
        {
            #region Kullan�c�
            if (AktarimParametreleri.Parametre.akt_kullanici_no.Value != 0)
            {
                shek.sthek_create_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
                shek.sthek_lastup_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
            }
            #endregion


            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                shek.sthek_special1 = "BGS";
                db.STOK_HAREKETLERI_EKs.InsertOnSubmit(shek);
                db.SubmitChanges();
                //shek.sthek_RECid_RECno = shek.sthek_RECno;
                //db.SubmitChanges();
                return shek;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }
        public static CARI_HESAP_HAREKETLERI_EK Cari_Hesap_HareketEK_Kaydet(CARI_HESAP_HAREKETLERI_EK chek, string conn)
        {
            #region Kullan�c�
            if (AktarimParametreleri.Parametre.akt_kullanici_no.Value != 0)
            {
                chek.chaek_create_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
                chek.chaek_lastup_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
            }
            #endregion


            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                chek.chaek_special1 = "BGS";
                db.CARI_HESAP_HAREKETLERI_EKs.InsertOnSubmit(chek);
                db.SubmitChanges();
                //chek.chaek_RECid_RECno = chek.chaek_RECno;
                //db.SubmitChanges();
                return chek;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static CARI_HESAP_HAREKETLERI Cari_Hesap_Hareket_Guncelle(CARI_HESAP_HAREKETLERI ch, string conn)
        {
            #region Kullan�c�
            if (AktarimParametreleri.Parametre.akt_kullanici_no.Value != 0)
            {
                ch.cha_create_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
                ch.cha_lastup_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
            }
            #endregion

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            ch.cha_special1 = "BGS";
            ch.cha_fis_sirano = 0;
            ch.cha_fis_tarih = Convert.ToDateTime("1899-12-30 00:00:00.000");
            db.CARI_HESAP_HAREKETLERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.cha_RECid_RECno = ch.cha_RECno;
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
        public static STOK_HAREKETLERI_EK Stok_HareketEK_Guncelle(STOK_HAREKETLERI_EK shek, string conn)
        {
            #region Kullan�c�
            if (AktarimParametreleri.Parametre.akt_kullanici_no.Value != 0)
            {
                shek.sthek_create_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
                shek.sthek_lastup_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
            }
            #endregion


            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                shek.sthek_special1 = "BGS";
                db.STOK_HAREKETLERI_EKs.Attach(shek, true);
                db.SubmitChanges();
                //shek.sthek_RECid_RECno = shek.sthek_RECno;
                //db.SubmitChanges();
                return shek;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        public static CARI_HESAP_HAREKETLERI_EK Cari_Hesap_HareketEK_Guncelle(CARI_HESAP_HAREKETLERI_EK chek, string conn)
        {
            #region Kullan�c�
            if (AktarimParametreleri.Parametre.akt_kullanici_no.Value != 0)
            {
                chek.chaek_create_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
                chek.chaek_lastup_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
            }
            #endregion


            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                chek.chaek_special1 = "BGS";
                db.CARI_HESAP_HAREKETLERI_EKs.Attach(chek, true);
                db.SubmitChanges();
                //chek.chaek_RECid_RECno = chek.chaek_RECno;
                //db.SubmitChanges();
                return chek;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }

        }

        
        public static SIPARISLER Siparis_Kaydet(SIPARISLER ch, string conn)
        {
            #region Kullan�c�
            if (AktarimParametreleri.Parametre.akt_kullanici_no.Value != 0)
            {
                ch.sip_create_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
                ch.sip_lastup_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
            }
            #endregion


            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            ch.sip_special1 = "BGS";
            //ch.sip_RECid_RECno = RandomDondur();
            db.SIPARISLERs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.sip_RECid_RECno = ch.sip_RECno;
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
        public static SIPARISLER Siparis_Guncelle(SIPARISLER ch, string conn)
        {
            #region Kullan�c�
            if (AktarimParametreleri.Parametre.akt_kullanici_no.Value != 0)
            {
                ch.sip_create_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
                ch.sip_lastup_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
            }
            #endregion


            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            ch.sip_special1 = "BGS";
            //ch.sip_RECid_RECno = ch.sip_RECno;
            db.SIPARISLERs.Attach(ch, true);
            db.SubmitChanges();
            //ch.sip_RECid_RECno = ch.sip_RECno;
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

        public static STOK_HAREKETLERI Stok_Hareketleri_Kaydet(STOK_HAREKETLERI ch, string conn)
        {
            #region Kullan�c�
            if (AktarimParametreleri.Parametre.akt_kullanici_no.Value != 0)
            {
                ch.sth_create_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
                ch.sth_lastup_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
            }
            #endregion

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            ch.sth_special1 = "BGS";
            ch.sth_fis_sirano = 0;
            ch.sth_fis_tarihi = Convert.ToDateTime("1899-12-30 00:00:00.000");
            //ch.sth_RECid_RECno = RandomDondur();
            db.STOK_HAREKETLERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.sth_RECid_RECno = ch.sth_RECno;
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
        public static STOK_HAREKETLERI Stok_Hareketleri_Guncelle(STOK_HAREKETLERI ch, string conn)
        {
            #region Kullan�c�
            if (AktarimParametreleri.Parametre.akt_kullanici_no.Value != 0)
            {
                ch.sth_create_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
                ch.sth_lastup_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
            }
            #endregion

            try
            {

            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            ch.sth_special1 = "BGS";
            ch.sth_fis_sirano = 0;
            ch.sth_fis_tarihi = Convert.ToDateTime("1899-12-30 00:00:00.000");
            //ch.sth_RECid_RECno = ch.sth_RECno;
            db.STOK_HAREKETLERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.sth_RECid_RECno = ch.sth_RECno;
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
        
        public static ODEME_EMIRLERI OdemeEmirleri_Kaydet(ODEME_EMIRLERI ch, string conn)
        {
            #region Kullan�c�
            if (AktarimParametreleri.Parametre.akt_kullanici_no.Value != 0)
            {
                ch.sck_create_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
                ch.sck_lastup_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
            }
            #endregion

            try
            {
            //ch.sck_RECid_RECno = RandomDondur();
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
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }
        public static ODEME_EMIRLERI OdemeEmirleri_Guncelle(ODEME_EMIRLERI ch, string conn)
        {
            #region Kullan�c�
            if (AktarimParametreleri.Parametre.akt_kullanici_no.Value != 0)
            {
                ch.sck_create_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
                ch.sck_lastup_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
            }
            #endregion

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            ch.sck_special1 = "BGS";
            //ch.sck_RECid_RECno = ch.sck_RECno;
            db.ODEME_EMIRLERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.sck_RECid_RECno = ch.sck_RECno;
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

        public static BEDEN_HAREKETLERI BedenHareketleri_Kaydet(BEDEN_HAREKETLERI ch, string conn)
        {
            #region Kullan�c�
            if (AktarimParametreleri.Parametre.akt_kullanici_no.Value != 0)
            {
                ch.BdnHar_create_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
                ch.BdnHar_lastup_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
            }
            #endregion

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            ch.BdnHar_special1 = "BGS";
            //ch.BdnHar_RECid_RECno = RandomDondur();
            db.BEDEN_HAREKETLERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.BdnHar_RECid_RECno = ch.BdnHar_RECno;
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
        public static BEDEN_HAREKETLERI BedenHareketleri_Guncelle(BEDEN_HAREKETLERI ch, string conn)
        {
            #region Kullan�c�
            if (AktarimParametreleri.Parametre.akt_kullanici_no.Value != 0)
            {
                ch.BdnHar_create_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
                ch.BdnHar_lastup_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
            }
            #endregion

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            ch.BdnHar_special1 = "BGS";
            //ch.BdnHar_RECid_RECno = RandomDondur();
            db.BEDEN_HAREKETLERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.BdnHar_RECid_RECno = ch.BdnHar_RECno;
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

        public static CIHAZ_HAREKETLERI Cihaz_Hareketleri_Kaydet(CIHAZ_HAREKETLERI ch, string conn)
        {
            #region Kullan�c�
            if (AktarimParametreleri.Parametre.akt_kullanici_no.Value != 0)
            {
                ch.ChHar_create_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
                ch.ChHar_lastup_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
            }
            #endregion

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            ch.ChHar_special1 = "BGS";
            //ch.ChHar_RECid_RECno = RandomDondur();

            db.CIHAZ_HAREKETLERIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.ChHar_RECid_RECno = ch.ChHar_RECno;
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
        public static CIHAZ_HAREKETLERI Cihaz_Hareketleri_Guncelle(CIHAZ_HAREKETLERI ch, string conn)
        {
            #region Kullan�c�
            if (AktarimParametreleri.Parametre.akt_kullanici_no.Value != 0)
            {
                ch.ChHar_create_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
                ch.ChHar_lastup_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
            }
            #endregion

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            ch.ChHar_special1 = "BGS";
            //ch.ChHar_RECid_RECno = ch.ChHar_RECno;
            db.CIHAZ_HAREKETLERIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.ChHar_RECid_RECno = ch.ChHar_RECno;
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

        public static SATIS_SARTLARI Satis_Sartlari_Kaydet(SATIS_SARTLARI ch, string conn)
        {
            #region Kullan�c�
            if (AktarimParametreleri.Parametre.akt_kullanici_no.Value != 0)
            {
                ch.sat_create_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
                ch.sat_lastup_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
            }
            #endregion

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            ch.sat_special1 = "BGS";
            //ch.sat_RECid_RECno = RandomDondur();
            db.SATIS_SARTLARIs.InsertOnSubmit(ch);
            db.SubmitChanges();
            //ch.sat_RECid_RECno = ch.sat_RECno;
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
        public static SATIS_SARTLARI Satis_Sartlari_Guncelle(SATIS_SARTLARI ch, string conn)
        {
            #region Kullan�c�
            if (AktarimParametreleri.Parametre.akt_kullanici_no.Value != 0)
            {
                ch.sat_create_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
                ch.sat_lastup_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
            }
            #endregion

            try
            {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {
            ch.sat_special1 = "BGS";
            //ch.sat_RECid_RECno = ch.sat_RECno;
            db.SATIS_SARTLARIs.Attach(ch, true);
            db.SubmitChanges();
            //ch.sat_RECid_RECno = ch.sat_RECno;
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

        public static AKTARIM_PARAMETRELERI AktarimParametreleriniGetir(string conn)
        {
            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                AKTARIM_PARAMETRELERI model = db.AKTARIM_PARAMETRELERIs.FirstOrDefault();
                return model;
            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return null;

            }
        }

        public static void RecNolariDuzelt(string conn)
        {
            return;
            SqlConnection cnn = new SqlConnection(conn);
            SqlCommand cmd = new SqlCommand();
            cmd.CommandText = "UPDATE STOK_HAREKETLERI SET sth_RECid_RECno = sth_RECno WHERE sth_RECid_RECno <>sth_RECno; UPDATE CARI_HESAP_HAREKETLERI SET cha_RECid_RECno = cha_RECno WHERE cha_RECid_RECno <> cha_RECno; UPDATE SIPARISLER SET sip_RECid_RECno = sip_RECno WHERE sip_RECid_RECno <> sip_RECno; UPDATE CIHAZ_HAREKETLERI SET ChHar_RECid_RECno = ChHar_RECno WHERE ChHar_RECid_RECno <> ChHar_RECno ; UPDATE BEDEN_HAREKETLERI SET BdnHar_RECid_RECno=BdnHar_RECno WHERE  BdnHar_RECid_RECno<>BdnHar_RECno; UPDATE SATIS_SARTLARI SET sat_RECid_RECno=sat_RECno WHERE sat_RECid_RECno<>sat_RECno  ";
            cmd.Connection = cnn;
            try
            {
                int sayi;
                cnn.Open();
                cmd.ExecuteNonQuery();
                cnn.Close();
            }
            catch (Exception ex)
            {
                if (cnn.State == System.Data.ConnectionState.Open)
                    cnn.Close();
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
              
            }
            finally
            {
               
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


        #region S�L�NEN KAYITLARLA �LG�L� ��LEMLER
        #region Hareket Y�kle
        public static List<STOK_HAREKETLERI> HareketleriYukle_Del(string[] SeriNo, Int32[] KulNo, DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<STOK_HAREKETLERI> ls = new List<STOK_HAREKETLERI>();
                ls = (from T in db.STOK_HAREKETLERIs 
                      where (SeriNo.Contains(T.sth_evrakno_seri)) && 
                      !(KulNo.Contains(T.sth_create_user.Value)) &&
                      (T.sth_tarih >= Tarih1 && T.sth_tarih <= Tarih2) select T).ToList<STOK_HAREKETLERI>();
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
        
        public static List<STOK_HAREKETLERI> HareketleriYukle_anlik_Del(string[] SeriNo, Int32[] KulNo, DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<STOK_HAREKETLERI> ls = new List<STOK_HAREKETLERI>();
                ls = (from T in db.STOK_HAREKETLERIs 
                      where SeriNo.Contains(T.sth_evrakno_seri) &&
                      !(KulNo.Contains(T.sth_create_user.Value)) &&
                      (T.sth_lastup_date >= Tarih1 && T.sth_lastup_date <= Tarih2) select T).ToList<STOK_HAREKETLERI>();
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

        public static List<CARI_HESAP_HAREKETLERI> Cari_HareketleriYukle_2_Del(string[] SeriNo,Int32[] KulNo, DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<CARI_HESAP_HAREKETLERI> ls = new List<CARI_HESAP_HAREKETLERI>();

                ls = (from T in db.CARI_HESAP_HAREKETLERIs 
                      where SeriNo.Contains(T.cha_evrakno_seri) &&
                      !(KulNo.Contains(T.cha_create_user.Value)) &&
                      (T.cha_tarihi >= Tarih1 && T.cha_tarihi <= Tarih2) select T).ToList<CARI_HESAP_HAREKETLERI>();

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

        public static List<CARI_HESAP_HAREKETLERI> Cari_HareketleriYukle_2_anlik_Del(string[] SeriNo, Int32[] KulNo, DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<CARI_HESAP_HAREKETLERI> ls = new List<CARI_HESAP_HAREKETLERI>();

                ls = (from T in db.CARI_HESAP_HAREKETLERIs 
                      where SeriNo.Contains(T.cha_evrakno_seri) &&
                      !(KulNo.Contains(T.cha_create_user.Value)) &&
                      (T.cha_lastup_date >= Tarih1 && T.cha_lastup_date <= Tarih2) select T).ToList<CARI_HESAP_HAREKETLERI>();

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

        public static List<SIPARISLER> SiparisleriYukle_anlik_Del(string[] SeriNo, Int32[] KulNo, DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<SIPARISLER> ls = new List<SIPARISLER>();

                ls = (from T in db.SIPARISLERs 
                      where SeriNo.Contains(T.sip_evrakno_seri) &&
                      !(KulNo.Contains(T.sip_create_user.Value)) &&
                      (T.sip_lastup_date >= Tarih1 && T.sip_lastup_date <= Tarih2) select T).ToList<SIPARISLER>();

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

        public static List<SIPARISLER> SiparisleriYukle_Del(string[] SeriNo, Int32[] KulNo, DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<SIPARISLER> ls = new List<SIPARISLER>();

                ls = (from T in db.SIPARISLERs 
                      where SeriNo.Contains(T.sip_evrakno_seri) &&
                      !(KulNo.Contains(T.sip_create_user.Value)) &&
                      (T.sip_tarih >= Tarih1 && T.sip_tarih <= Tarih2) select T).ToList<SIPARISLER>();

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

        public static List<SATIS_SARTLARI> Satis_Sartlarini_Yukle_Del(Int32[] KulNo, DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<SATIS_SARTLARI> ls = new List<SATIS_SARTLARI>();
                ls = (from T in db.SATIS_SARTLARIs 
                      where
                      !(KulNo.Contains(T.sat_create_user.Value)) &&
                      (T.sat_evrak_tarih >= Tarih1 && T.sat_evrak_tarih <= Tarih2) select T).ToList<SATIS_SARTLARI>();

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

        public static List<SATIS_SARTLARI> Satis_Sartlarini_Yukle_anlik_Del(Int32[] KulNo, DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                List<SATIS_SARTLARI> ls = new List<SATIS_SARTLARI>();
                ls = (from T in db.SATIS_SARTLARIs 
                      where
                      !(KulNo.Contains(T.sat_create_user.Value)) &&
                      (T.sat_lastup_date >= Tarih1 && T.sat_lastup_date <= Tarih2) select T).ToList<SATIS_SARTLARI>();

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

        #endregion


        public static Int32 Stok_Hareket_EvrakCount(string seri,Int32 sira,byte tip, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                Int32 tt = db.STOK_HAREKETLERIs.Count(T => (T.sth_evraktip ==tip && T.sth_evrakno_seri == seri && T.sth_evrakno_sira == sira));
                return tt;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return 0;

            }
        }

        public static bool Stok_Hareket_EvrakSil(string seri, Int32 sira, byte tip, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            try
            {
                db.CommandTimeout = 0;

                db.Connection.Open();
                db.Transaction = db.Connection.BeginTransaction();

                #region Delete
                //db.ExecuteCommand("Delete from dbo.BEDEN_HAREKETLERI where BdnHar_Tipi='P' and BdnHar_DRECid_RECno in (Select sip_RECno from SIPARISLER where sip_RECno in (Select sth_sip_recid_recno from STOK_HAREKETLERI where sth_evraktip = {0} and sth_evrakno_seri = {1} and sth_evrakno_sira = {2}))", tip, seri, sira);

                db.ExecuteCommand("Delete from SIPARISLER where sip_Guid in (Select sth_sip_uid from STOK_HAREKETLERI where sth_special1='BGS' and sth_evraktip = {0} and sth_evrakno_seri = {1} and sth_evrakno_sira = {2})", tip, seri, sira);

                //db.ExecuteCommand("Delete from dbo.BEDEN_HAREKETLERI where BdnHar_Tipi='S' and BdnHar_DRECid_RECno in (Select sth_RECno from STOK_HAREKETLERI where sth_evraktip = {0} and sth_evrakno_seri = {1} and sth_evrakno_sira = {2})", tip, seri, sira);

                //db.ExecuteCommand("Delete from dbo.CIHAZ_HAREKETLERI where ChHar_master_tablo='S' and ChHar_master_recno in (Select sth_RECno from STOK_HAREKETLERI where sth_evraktip = {0} and sth_evrakno_seri = {1} and sth_evrakno_sira = {2})", tip, seri, sira);

                db.ExecuteCommand("Delete from CARI_HESAP_HAREKETLERI where cha_Guid in (Select sth_fat_uid from STOK_HAREKETLERI where sth_special1='BGS' and sth_evraktip = {0} and sth_evrakno_seri = {1} and sth_evrakno_sira = {2})", tip, seri, sira);

                db.ExecuteCommand("Delete from STOK_HAREKETLERI where sth_special1='BGS' and sth_evraktip = {0} and sth_evrakno_seri = {1} and sth_evrakno_sira = {2}", tip, seri, sira);

                #endregion


                db.Transaction.Commit();

                return true;

            }
            catch (Exception ex)
            {
                db.Transaction.Rollback();
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return false;

            }
            }
        }

        public static Int32 Cari_Hesap_Hareket_EvrakCount(string seri, Int32 sira, byte tip, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                Int32 tt = db.CARI_HESAP_HAREKETLERIs.Count(T => T.cha_evrak_tip == tip && T.cha_evrakno_seri == seri && T.cha_evrakno_sira == sira);

                return tt;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return 0;

            }

        }

        public static bool Cari_Hesap_Hareket_EvrakSil(string seri, Int32 sira, byte tip, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            try
            {
                db.CommandTimeout = 0;
                db.Connection.Open();
                db.Transaction = db.Connection.BeginTransaction();

                #region Delete

                List<CARI_HESAP_HAREKETLERI> ls = new List<CARI_HESAP_HAREKETLERI>();
                ls = (from t in db.CARI_HESAP_HAREKETLERIs
                      where t.cha_evrak_tip == tip &&
                      t.cha_evrakno_seri == seri &&
                      t.cha_evrakno_sira == sira
                      select t).ToList<CARI_HESAP_HAREKETLERI>();

                foreach (CARI_HESAP_HAREKETLERI item in ls)
                {
                    //sck_tip, sck_refno
                    db.ExecuteCommand("Delete from ODEME_EMIRLERI where sck_special1='BGS' and sck_tip = {0} and sck_refno = {1}", CariHarTipToOdemeTip(item.cha_cinsi.Value),item.cha_trefno);
                }


                db.ExecuteCommand("Delete from CARI_HESAP_HAREKETLERI where cha_special1='BGS' and cha_evrak_tip = {0} and cha_evrakno_seri = {1} and cha_evrakno_sira = {2}", tip, seri, sira);


                #endregion


                db.Transaction.Commit();

                return true;

            }
            catch (Exception ex)
            {
                db.Transaction.Rollback();
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return false;

            }
            }
        }

        public static byte CariHarTipToOdemeTip(byte tip)
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

        public static Int32 Siparis_EvrakCount(string seri, Int32 sira, byte tip,byte cins, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                 Int32 tt = db.SIPARISLERs.Count(T => (T.sip_tip == tip && T.sip_cins == cins && T.sip_evrakno_seri == seri && T.sip_evrakno_sira == sira));

                return tt;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return 0;

            }
        }

        public static bool Siparis_EvrakSil(string seri, Int32 sira, byte tip, byte cins, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            try
            {
                db.CommandTimeout = 0;

                db.Connection.Open();
                db.Transaction = db.Connection.BeginTransaction();

                #region Delete
                //db.ExecuteCommand("Delete from dbo.BEDEN_HAREKETLERI where BdnHar_Tipi='P' and BdnHar_DRECid_RECno in (Select sip_RECno from SIPARISLER where sip_tip={0} and sip_cins={1} and sip_evrakno_seri={2} and sip_evrakno_sira={3})", tip, cins,seri, sira);

                db.ExecuteCommand("Delete from SIPARISLER where sip_special1='BGS' and sip_tip={0} and sip_cins={1} and sip_evrakno_seri={2} and sip_evrakno_sira={3}", tip, cins, seri, sira);

                #endregion


                db.Transaction.Commit();

                return true;

            }
            catch (Exception ex)
            {
                db.Transaction.Rollback();
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return false;

            }
            }
        }

        public static Int32 Satis_Sartlari_EvrakCount(string serino, Int32 sirano, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                Int32 tt = db.SATIS_SARTLARIs.Count(T => (T.sat_evrakno_seri == serino && T.sat_evrakno_sira == sirano));

                return tt;

            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return 0;

            }
        }

        public static bool Satis_Sartlari_EvrakSil(string seri, Int32 sira, string conn)
        {
            using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
            {

            try
            {
                db.CommandTimeout = 0;

                db.Connection.Open();
                db.Transaction = db.Connection.BeginTransaction();

                #region Delete
                db.ExecuteCommand("Delete from dbo.SATIS_SARTLARI where sat_special1='BGS' and sat_evrakno_seri={0} and sat_evrakno_sira={1}",  seri, sira);


                #endregion


                db.Transaction.Commit();

                return true;

            }
            catch (Exception ex)
            {
                db.Transaction.Rollback();
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return false;

            }
            }
        }

        #endregion

        #region MUHASEBE_FISLERI AKTARIMI
        public static void Muhasebe_Fisleri_Sil(string[] SeriNo, DateTime Tarih1, DateTime Tarih2, string conn)
        {

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                List<MUHASEBE_FISLERI> ls = new List<MUHASEBE_FISLERI>();
                ls = (from T in db.MUHASEBE_FISLERIs 
                      where SeriNo.Contains(T.fis_tic_evrak_seri) && 
                      (T.fis_tarih >= Tarih1 && T.fis_tarih <= Tarih2) &&
                      (T.fis_proje_kodu!="MUH")
                      select T).ToList<MUHASEBE_FISLERI>();
                
                db.MUHASEBE_FISLERIs.DeleteAllOnSubmit(ls);
                db.SubmitChanges();
            }
            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
            }

        }

        public static Guid FisTicariRecNo_Getir(MUHASEBE_FISLERI mf, string conn_k,string conn_h)
        {
            Guid rn = Guid.Empty;
            try
            {
                BigusAktarimDataContext db_k = new BigusAktarimDataContext(conn_k);
                BigusAktarimDataContext db_h = new BigusAktarimDataContext(conn_h);

                

                if (mf.fis_ticari_tip == 0)
                {
                    rn= Guid.Empty;// Yok
                }
                else if (mf.fis_ticari_tip == 1) // Stok Hareketleri
                { }
                else if (mf.fis_ticari_tip == 2) // Cari Hesap Hareketleri
                {

                    CARI_HESAP_HAREKETLERI ch = db_k.CARI_HESAP_HAREKETLERIs.FirstOrDefault(t => t.cha_Guid == mf.fis_ticari_uid.Value);

                    if (ch != null)
                    {
                        CARI_HESAP_HAREKETLERI ch_h = (from t in db_h.CARI_HESAP_HAREKETLERIs
                                                      where t.cha_tip == ch.cha_tip.Value &&
                                                      t.cha_cinsi == ch.cha_cinsi.Value &&
                                                      t.cha_normal_Iade == ch.cha_normal_Iade.Value &&
                                                      t.cha_evrak_tip == ch.cha_evrak_tip.Value &&
                                                      t.cha_evrakno_seri == ch.cha_evrakno_seri &&
                                                      t.cha_evrakno_sira == ch.cha_evrakno_sira.Value &&
                                                      t.cha_satir_no == ch.cha_satir_no.Value
                                                      select t).FirstOrDefault();

                        if (ch_h != null)
                        {
                            rn = ch_h.cha_Guid;
                        }
                    }

                }
                else if (mf.fis_ticari_tip == 3) // Sipari�
                { }
                // 4:Personel Tahakkuk 
                // 5:Akaryak�t hareket 
                // 6:Demirba� Hareket 
                // 7:Smm Hareket

                return rn;

            }
            catch (Exception ex)
            {
                Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler.HandleUnhandledException(ex);
                System.Threading.Thread.CurrentThread.Abort();
                return rn;

            }

        }

        public static List<MUHASEBE_FISLERI> Muhasebe_Fisleri_Getir(string[] SeriNo, DateTime Tarih1, DateTime Tarih2, string conn)
        {
            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                List<MUHASEBE_FISLERI> ls = new List<MUHASEBE_FISLERI>();
                ls = (from T in db.MUHASEBE_FISLERIs 
                      where SeriNo.Contains(T.fis_tic_evrak_seri) &&  
                      (T.fis_tarih >= Tarih1 && T.fis_tarih <= Tarih2) 
                      select T).ToList<MUHASEBE_FISLERI>();

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

        public static List<MUHASEBE_FISLERI> Muhasebe_Fisleri_Getir_Anlik(string[] SeriNo, DateTime Tarih1, DateTime Tarih2, string conn)
        {
            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                List<MUHASEBE_FISLERI> ls = new List<MUHASEBE_FISLERI>();
                ls = (from T in db.MUHASEBE_FISLERIs
                      where (T.fis_lastup_date >= Tarih1 && T.fis_lastup_date <= Tarih2)
                      select T).ToList<MUHASEBE_FISLERI>();

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


        public static MUHASEBE_FISLERI Muhasebe_Fisleri_Kaydet(MUHASEBE_FISLERI ch, string conn)
        {
            #region Kullan�c�
            if (AktarimParametreleri.Parametre.akt_kullanici_no.Value != 0)
            {
                ch.fis_create_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
                ch.fis_lastup_user = Convert.ToInt16(AktarimParametreleri.Parametre.akt_kullanici_no.Value);
            }
            #endregion

            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {

                //ch.fis_RECid_RECno = RandomDondur();
                db.MUHASEBE_FISLERIs.InsertOnSubmit(ch);
                db.SubmitChanges();
                //ch.fis_RECid_RECno = ch.fis_RECno;
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
            public static void Stok_Hareketleri_Kilit_Guncelle(Guid guid, string conn)
        {
            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                    db.ExecuteCommand("UPDATE STOK_HAREKETLERI SET sth_kilitli = 1 WHERE sth_Guid = {0}", guid);
                }
            }
            catch (Exception ex) { System.IO.File.AppendAllText(@"C:\Aktarici_Error.txt", ex.ToString() + "\r\n"); }
        }

        public static void Cari_Hesap_Hareket_Kilit_Guncelle(Guid guid, string conn)
        {
            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                    db.ExecuteCommand("UPDATE CARI_HESAP_HAREKETLERI SET cha_kilitli = 1 WHERE cha_Guid = {0}", guid);
                }
            }
            catch (Exception ex) { System.IO.File.AppendAllText(@"C:\Aktarici_Error.txt", ex.ToString() + "\r\n"); }
        }
}
}
