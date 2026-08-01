using System;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Tugi.DepotManagement.UnhandledExceptionManagement
{
    /// <summary>
    /// Bu class exception almamýz durumunda çalýþacak olan methodlarýmýzý barýndýrmaktadýr.
    /// </summary>
    public class MyExceptionHandler
    {
        public MyExceptionHandler()
        {

        }

        public void OnUnhandledException( object sender , UnhandledExceptionEventArgs e )
        {
            HandleUnhandledException ( sender , e.ExceptionObject );
        }

        public void OnThreadException( object sender , ThreadExceptionEventArgs e )
        {
            HandleUnhandledException ( sender , e.Exception );
        }

        private void HandleUnhandledException( object sender , object exception )
        {
            Exception m_Exception;

            UnHandledExceptionManagement.frmException m_Form;

            
            string m_Message;

            try
            {
                // object tipindeki exception'ýmýzý "Exception" tipine çevirelim.
                m_Exception = ( Exception )exception;

                // Yeni bir "frmException" oluþturalým.
                m_Form = new UnHandledExceptionManagement.frmException();

                // Oluþturduðumuz m_Form'un özelliklerini atayalým.
                m_Message = "Type: \n\t" + m_Exception.GetType ().ToString () + "\n" +
                            "Source: \n\t" + m_Exception.Source + "\n" +
                            "Message: \n\t" + m_Exception.Message + "\n" +
                            "Trace: \n\t" + m_Exception.StackTrace;

                m_Form.ExceptionName = m_Exception.GetType ().ToString ();
                m_Form.ExceptionMessage = m_Message;
                //m_Form.ExceptionSource = exception;

                string dosya = "Log_" + DateTime.Now.Date.ToShortDateString();
                StreamWriter yaz = File.AppendText(Application.StartupPath + "\\ErrorLog\\" + dosya );

                StringBuilder str = new StringBuilder();
                str.AppendLine("Tarih       : " + DateTime.Now.ToString());
                str.AppendLine("Hata Adý    : " +  m_Exception.GetType ().ToString ());
                str.AppendLine("Hata Mesajý : " + m_Exception.Message);
               
                str.AppendLine("");

                yaz.WriteLine(str.ToString());
                yaz.Close();
             
                // Oluþturduðumuz m_Form'u gösterelim.
               m_Form.ShowDialog ();
              
                

                try
                {
                    // EventLog kaynaðýmýz yoksa EventLog kaynaðýmýzý oluþturalým.
                    if ( !EventLog.SourceExists ( "UnhandledExceptionManagement" ) )
                        EventLog.CreateEventSource ( "UnhandledExceptionManagement" , "UnhandledExceptionManagement" );

                    // Yeni bir EventLog oluþturalým.
                    EventLog m_EventLog = new EventLog ();
                    // EventLog'umuzun kaynaðýný belirtelim.
                    m_EventLog.Source = "UnhandledExceptionManagement";
                    // EventLog'umuzu dolduðu zaman en eski log bilgisini silecek þekilde ayarlayalým.
                    m_EventLog.ModifyOverflowPolicy ( OverflowAction.OverwriteAsNeeded , m_EventLog.MinimumRetentionDays );
                    // Hatayý EventLog'umuza yazalým.
                    EventLog.WriteEntry ( "UnhandledExceptionManagement" , m_Message );
                    // EventLog'u kapatalým.
                    m_EventLog.Close ();

                   
                }
                catch ( Exception )
                {
                    // EventLog'a yazýlmasý sýrasýnda bir hata aldýysak herhangi birþey yapmayacaðýz.
                }

                

            }
            catch ( Exception ex )
            {
                // Exception ekranýnýn gösterilmesi sýrasýnda da bir hata aldýysak "Tanýmlanamayan hata" mesajý ile uygulamamýzý sonlandýralým

                //try
                //{
                //    MessageBox.Show ( "Tanýmlanamayan hata" , "Hata" , MessageBoxButtons.OK , MessageBoxIcon.Stop );
                //}
                //finally
                //{
                //    Environment.Exit ( 1 );
                //}
            
            }
        }


        public static void HandleUnhandledException(Exception ex1)
        {
            Exception m_Exception;

            UnHandledExceptionManagement.frmException m_Form;


            string m_Message;

            try
            {
                // object tipindeki exception'ýmýzý "Exception" tipine çevirelim.
                m_Exception = ex1;

                // Yeni bir "frmException" oluþturalým.
                m_Form = new UnHandledExceptionManagement.frmException();

                // Oluþturduðumuz m_Form'un özelliklerini atayalým.
                m_Message = "Type: \n\t" + m_Exception.GetType().ToString() + "\n" +
                            "Source: \n\t" + m_Exception.Source + "\n" +
                            "Message: \n\t" + m_Exception.Message + "\n" +
                            "Trace: \n\t" + m_Exception.StackTrace;

                m_Form.ExceptionName = m_Exception.GetType().ToString();
                m_Form.ExceptionMessage = m_Message;
                //m_Form.ExceptionSource = exception;

                string dosya = "Log_" + DateTime.Now.Date.ToShortDateString();
                StreamWriter yaz = File.AppendText(Application.StartupPath + "\\ErrorLog\\" + dosya);

                StringBuilder str = new StringBuilder();
                str.AppendLine("Tarih       : " + DateTime.Now.ToString());
                str.AppendLine("Hata Adý    : " + m_Exception.GetType().ToString());
                str.AppendLine("Hata Mesajý : " + m_Exception.Message);

                str.AppendLine("");

                yaz.WriteLine(str.ToString());
                yaz.Close();

                // Oluþturduðumuz m_Form'u gösterelim.
                m_Form.ShowDialog();


                try
                {
                    // EventLog kaynaðýmýz yoksa EventLog kaynaðýmýzý oluþturalým.
                    if (!EventLog.SourceExists("UnhandledExceptionManagement"))
                        EventLog.CreateEventSource("UnhandledExceptionManagement", "UnhandledExceptionManagement");

                    // Yeni bir EventLog oluþturalým.
                    EventLog m_EventLog = new EventLog();
                    // EventLog'umuzun kaynaðýný belirtelim.
                    m_EventLog.Source = "UnhandledExceptionManagement";
                    // EventLog'umuzu dolduðu zaman en eski log bilgisini silecek þekilde ayarlayalým.
                    m_EventLog.ModifyOverflowPolicy(OverflowAction.OverwriteAsNeeded, m_EventLog.MinimumRetentionDays);
                    // Hatayý EventLog'umuza yazalým.
                    EventLog.WriteEntry("UnhandledExceptionManagement", m_Message);
                    // EventLog'u kapatalým.
                    m_EventLog.Close();
                }
                catch (Exception)
                {
                    // EventLog'a yazýlmasý sýrasýnda bir hata aldýysak herhangi birþey yapmayacaðýz.
                }
            }
            catch (Exception ex)
            {
                // Exception ekranýnýn gösterilmesi sýrasýnda da bir hata aldýysak "Tanýmlanamayan hata" mesajý ile uygulamamýzý sonlandýralým

                try
                {
                    string dosya = "Log_" + DateTime.Now.Date.ToShortDateString();
                    StreamWriter yaz = File.AppendText(Application.StartupPath + "\\ErrorLog\\" + dosya);

                    StringBuilder str = new StringBuilder();
                    str.AppendLine("Tarih       : " + DateTime.Now.ToString());
                    str.AppendLine("Hata Adý    : TANIMLANAMAYAN HATA...");

                    str.AppendLine("Hata Mesajý : " + ex.Message);

                    str.AppendLine("");

                    yaz.WriteLine(str.ToString());
                    yaz.Close();

                    //MessageBox.Show("Tanýmlanamayan hata", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
                finally
                {
                    // Environment.Exit(1);
                }
            }
        }

    }
}
