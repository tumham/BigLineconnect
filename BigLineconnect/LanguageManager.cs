using System;
using System.Collections.Generic;
using System.Globalization;

namespace BigLineconnect
{
    public static class LanguageManager
    {
        private static string _currentLang = "en";

        static LanguageManager()
        {
            try
            {
                string sysLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLower();
                if (sysLang == "tr" || sysLang == "es" || sysLang == "ru" || sysLang == "it")
                {
                    _currentLang = sysLang;
                }
            }
            catch { }
        }

        // Programmatic override
        public static void SetLanguage(string langCode)
        {
            if (langCode == "tr" || langCode == "en" || langCode == "es" || langCode == "ru" || langCode == "it")
            {
                _currentLang = langCode;
            }
        }

        public static string CurrentLanguage => _currentLang;
        public static bool Turkish => _currentLang == "tr";

        public static string Get(string key, params object[] args)
        {
            string format = _currentLang switch
            {
                "tr" => GetTr(key),
                "es" => GetEs(key),
                "ru" => GetRu(key),
                "it" => GetIt(key),
                _ => GetEn(key)
            };

            if (args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(format, args);
                }
                catch
                {
                    return format;
                }
            }
            return format;
        }

        private static string GetTr(string key)
        {
            if (TrTranslations.TryGetValue(key, out var val)) return val;
            return key;
        }

        private static string GetEs(string key)
        {
            if (EsTranslations.TryGetValue(key, out var val)) return val;
            return key;
        }

        private static string GetRu(string key)
        {
            if (RuTranslations.TryGetValue(key, out var val)) return val;
            return key;
        }

        private static string GetIt(string key)
        {
            if (ItTranslations.TryGetValue(key, out var val)) return val;
            return key;
        }

        private static string GetEn(string key)
        {
            if (EnTranslations.TryGetValue(key, out var val)) return val;
            return key;
        }

        private static readonly Dictionary<string, string> TrTranslations = new Dictionary<string, string>
        {
            { "btn_chat", "Sohbet (Chat)" },
            { "btn_file_manager", "Dosya Yöneticisi" },
            { "btn_send", "Gönder" },
            { "lbl_path", "Yol:" },
            { "btn_up", "Yukarı" },
            { "btn_refresh", "Yenile" },
            { "btn_upload", "Benden Karşıya Kopyala" },
            { "btn_upload_folder", "Klasör Yükle" },
            { "btn_download", "Karşıdan Bana Kopyala" },
            { "col_name", "Adı" },
            { "col_size", "Boyut" },
            { "col_modified", "Değiştirilme Tarihi" },
            { "chat_me", "Ben" },
            { "chat_remote", "Uzak Kullanıcı" },
            { "msg_busy_waiting", "Uzak makine meşgul, bekleniyor..." },
            { "msg_remote_busy", "Uzak bilgisayar şu an meşgul." },
            { "msg_id_not_found", "Bağlantı ID'si bulunamadı." },
            { "msg_enter_password", "Lütfen uzaktaki bilgisayarın erişim şifresini girin:" },
            { "title_password_required", "Erişim Şifresi Gerekli" },
            { "msg_waiting_approval", "Uzak Bağlantı Onayı Bekleniyor..." },
            { "msg_connection_failed", "Bağlantı Başarısız" },
            { "title_viewer", "Masaüstü Bağlantısı - ID: {0}" },
            { "title_chat", "Sohbet - BigLineconnect" },
            { "title_file_manager", "Dosya Yöneticisi - BigLineconnect" },
            { "title_support_chat", "BigLineconnect - Destek Sohbeti" },
            { "msg_enter_folder_first", "Lütfen önce bir klasörün içine girin." },
            { "msg_file_uploaded", "Dosya başarıyla yüklendi." },
            { "title_upload_success", "Yükleme Başarılı" },
            { "msg_upload_error", "Dosya yüklenirken hata oluştu: {0}" },
            { "msg_download_failed", "İndirme başlatılamadı" },
            { "msg_download_completed", "Dosya indirme tamamlandı" },
            { "title_download_completed", "İndirme Tamamlandı" },
            { "msg_receive_error", "Dosya alımı tamamlanırken hata: {0}" },
            { "msg_drag_drop_saved", "Uzak bilgisayardan gelen '{0}' dosyası başarıyla Masaüstünüze kaydedildi." },
            { "title_file_received", "Dosya Alındı" },
            { "auth_success", "Doğrulama başarılı!" },
            { "auth_failed", "Hatalı erişim şifresi girildi!" },
            { "auth_rejected", "Bağlantı isteği kullanıcı tarafından reddedildi!" },
            { "service_started", "Hizmet başlatıldı." }
        };

        private static readonly Dictionary<string, string> EsTranslations = new Dictionary<string, string>
        {
            { "btn_chat", "Chat" },
            { "btn_file_manager", "Gestor de archivos" },
            { "btn_send", "Enviar" },
            { "lbl_path", "Ruta:" },
            { "btn_up", "Subir" },
            { "btn_refresh", "Actualizar" },
            { "btn_upload", "Copiar de mí a remoto" },
            { "btn_upload_folder", "Subir carpeta" },
            { "btn_download", "Copiar de remoto a mí" },
            { "col_name", "Nombre" },
            { "col_size", "Tamaño" },
            { "col_modified", "Fecha de modificación" },
            { "chat_me", "Yo" },
            { "chat_remote", "Usuario remoto" },
            { "msg_busy_waiting", "La máquina remota está ocupada, esperando..." },
            { "msg_remote_busy", "El ordenador remoto está ocupado en este momento." },
            { "msg_id_not_found", "ID de conexión no encontrado." },
            { "msg_enter_password", "Por favor, introduzca la contraseña de acceso para el ordenador remoto:" },
            { "title_password_required", "Contraseña de acceso requerida" },
            { "msg_waiting_approval", "Esperando la aprobación de la conexión remota..." },
            { "msg_connection_failed", "Conexión fallida" },
            { "title_viewer", "Conexión de escritorio - ID: {0}" },
            { "title_chat", "Chat - BigLineconnect" },
            { "title_file_manager", "Gestor de archivos - BigLineconnect" },
            { "title_support_chat", "BigLineconnect - Chat de soporte" },
            { "msg_enter_folder_first", "Por favor, navegue a un directorio primero." },
            { "msg_file_uploaded", "Archivo subido correctamente." },
            { "title_upload_success", "Subida exitosa" },
            { "msg_upload_error", "Error al subir el archivo: {0}" },
            { "msg_download_failed", "Error al iniciar la descarga" },
            { "msg_download_completed", "Descarga de archivo completada" },
            { "title_download_completed", "Descarga completada" },
            { "msg_receive_error", "Error al finalizar la recepción del archivo: {0}" },
            { "msg_drag_drop_saved", "El archivo '{0}' enviado desde el ordenador remoto se guardó correctamente en su Escritorio." },
            { "title_file_received", "Archivo recibido" },
            { "auth_success", "¡Autenticación exitosa!" },
            { "auth_failed", "¡Contraseña de acceso incorrecta!" },
            { "auth_rejected", "¡Solicitud de conexión rechazada por el usuario!" },
            { "service_started", "Servicio iniciado." }
        };

        private static readonly Dictionary<string, string> RuTranslations = new Dictionary<string, string>
        {
            { "btn_chat", "Чат" },
            { "btn_file_manager", "Файловый менеджер" },
            { "btn_send", "Отправить" },
            { "lbl_path", "Путь:" },
            { "btn_up", "Вверх" },
            { "btn_refresh", "Обновить" },
            { "btn_upload", "Копировать от меня на удаленный" },
            { "btn_upload_folder", "Загрузить папку" },
            { "btn_download", "Копировать с удаленного ко мне" },
            { "col_name", "Имя" },
            { "col_size", "Размер" },
            { "col_modified", "Дата изменения" },
            { "chat_me", "Я" },
            { "chat_remote", "Удаленный пользователь" },
            { "msg_busy_waiting", "Удаленная машина занята, ожидание..." },
            { "msg_remote_busy", "Удаленный компьютер сейчас занят." },
            { "msg_id_not_found", "ID подключения не найден." },
            { "msg_enter_password", "Пожалуйста, введите пароль доступа к удаленному компьютеру:" },
            { "title_password_required", "Требуется пароль доступа" },
            { "msg_waiting_approval", "Ожидание подтверждения удаленного подключения..." },
            { "msg_connection_failed", "Подключение не удалось" },
            { "title_viewer", "Подключение к рабочему столу - ID: {0}" },
            { "title_chat", "Чат - BigLineconnect" },
            { "title_file_manager", "Файловый менеджер - BigLineconnect" },
            { "title_support_chat", "BigLineconnect - Чат поддержки" },
            { "msg_enter_folder_first", "Пожалуйста, сначала перейдите в каталог." },
            { "msg_file_uploaded", "Файл успешно загружен." },
            { "title_upload_success", "Загрузка успешна" },
            { "msg_upload_error", "Ошибка загрузки файла: {0}" },
            { "msg_download_failed", "Не удалось начать загрузку" },
            { "msg_download_completed", "Загрузка файла завершена" },
            { "title_download_completed", "Загрузка завершена" },
            { "msg_receive_error", "Ошибка при завершении приема файла: {0}" },
            { "msg_drag_drop_saved", "Файл '{0}', отправленный с удаленного компьютера, успешно сохранен на вашем рабочем столе." },
            { "title_file_received", "Файл получен" },
            { "auth_success", "Авторизация успешна!" },
            { "auth_failed", "Введен неверный пароль доступа!" },
            { "auth_rejected", "Запрос на подключение отклонен пользователем!" },
            { "service_started", "Служба запущена." }
        };

        private static readonly Dictionary<string, string> ItTranslations = new Dictionary<string, string>
        {
            { "btn_chat", "Chat" },
            { "btn_file_manager", "Gestione file" },
            { "btn_send", "Invia" },
            { "lbl_path", "Percorso:" },
            { "btn_up", "Su" },
            { "btn_refresh", "Aggiorna" },
            { "btn_upload", "Copia da me a remoto" },
            { "btn_upload_folder", "Carica cartella" },
            { "btn_download", "Copia da remoto a me" },
            { "col_name", "Nome" },
            { "col_size", "Dimensione" },
            { "col_modified", "Data modifica" },
            { "chat_me", "Io" },
            { "chat_remote", "Utente remoto" },
            { "msg_busy_waiting", "La macchina remota è occupata, in attesa..." },
            { "msg_remote_busy", "Il computer remoto è occupato al momento." },
            { "msg_id_not_found", "ID connessione non trovato." },
            { "msg_enter_password", "Inserire la password di accesso per il computer remoto:" },
            { "title_password_required", "Password di accesso richiesta" },
            { "msg_waiting_approval", "In attesa di approvazione della connessione remota..." },
            { "msg_connection_failed", "Connessione non riuscita" },
            { "title_viewer", "Connessione desktop - ID: {0}" },
            { "title_chat", "Chat - BigLineconnect" },
            { "title_file_manager", "Gestione file - BigLineconnect" },
            { "title_support_chat", "BigLineconnect - Chat di supporto" },
            { "msg_enter_folder_first", "Navigare prima in una directory." },
            { "msg_file_uploaded", "File caricato con successo." },
            { "title_upload_success", "Caricamento riuscito" },
            { "msg_upload_error", "Errore durante il caricamento del file: {0}" },
            { "msg_download_failed", "Impossibile avviare il download" },
            { "msg_download_completed", "Download del file completato" },
            { "title_download_completed", "Download completato" },
            { "msg_receive_error", "Errore durante il completamento della ricezione del file: {0}" },
            { "msg_drag_drop_saved", "Il file '{0}' inviato dal computer remoto è stato salvato con successo sul Desktop." },
            { "title_file_received", "File ricevuto" },
            { "auth_success", "Autenticazione riuscita!" },
            { "auth_failed", "Password di accesso errata inserita!" },
            { "auth_rejected", "Richiesta di connessione rifiutata dall'utente!" },
            { "service_started", "Servizio avviato." }
        };

        private static readonly Dictionary<string, string> EnTranslations = new Dictionary<string, string>
        {
            { "btn_chat", "Chat" },
            { "btn_file_manager", "File Manager" },
            { "btn_send", "Send" },
            { "lbl_path", "Path:" },
            { "btn_up", "Up" },
            { "btn_refresh", "Refresh" },
            { "btn_upload", "Copy from Me to Remote" },
            { "btn_upload_folder", "Upload Folder" },
            { "btn_download", "Copy from Remote to Me" },
            { "col_name", "Name" },
            { "col_size", "Size" },
            { "col_modified", "Modified Date" },
            { "chat_me", "Me" },
            { "chat_remote", "Remote User" },
            { "msg_busy_waiting", "Remote machine is busy, waiting..." },
            { "msg_remote_busy", "Remote computer is busy right now." },
            { "msg_id_not_found", "Connection ID not found." },
            { "msg_enter_password", "Please enter the access password for the remote computer:" },
            { "title_password_required", "Access Password Required" },
            { "msg_waiting_approval", "Waiting for Remote Connection Approval..." },
            { "msg_connection_failed", "Connection Failed" },
            { "title_viewer", "Desktop Connection - ID: {0}" },
            { "title_chat", "Chat - BigLineconnect" },
            { "title_file_manager", "File Manager - BigLineconnect" },
            { "title_support_chat", "BigLineconnect - Support Chat" },
            { "msg_enter_folder_first", "Please navigate into a directory first." },
            { "msg_file_uploaded", "File uploaded successfully." },
            { "title_upload_success", "Upload Successful" },
            { "msg_upload_error", "Error uploading file: {0}" },
            { "msg_download_failed", "Failed to start download" },
            { "msg_download_completed", "File download completed" },
            { "title_download_completed", "Download Completed" },
            { "msg_receive_error", "Error finalizing file receive: {0}" },
            { "msg_drag_drop_saved", "The file '{0}' sent from the remote computer was successfully saved to your Desktop." },
            { "title_file_received", "File Received" },
            { "auth_success", "Authentication successful!" },
            { "auth_failed", "Incorrect access password entered!" },
            { "auth_rejected", "Connection request rejected by user!" },
            { "service_started", "Service started." }
        };
    }
}
