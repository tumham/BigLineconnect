using System.Net.Http;
using System.Text;
using System.Text.Json;

/// <summary>
/// Telegram Bot API üzerinden destek taleplerini push notification olarak gönderir.
/// Destek uzmanının cep telefonunda Telegram titrer + ses çalar.
/// </summary>
public static class TelegramNotifier
{
    // Bot Token — BotFather'dan alınır, ortam değişkeni veya config'den okunur
    private static string? _botToken;
    
    // Kayıtlı destek uzmanları (Chat ID listesi)
    private static readonly List<long> _registeredChatIds = new();
    private static readonly string _chatIdsFilePath = Path.Combine(AppContext.BaseDirectory, "telegram_chat_ids.json");
    
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly object _lock = new();

    /// <summary>
    /// Telegram bildirim sistemini başlatır. Bot token'ı ortam değişkeninden okur,
    /// kayıtlı chat ID'lerini dosyadan yükler.
    /// </summary>
    public static void Initialize()
    {
        _botToken = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
        
        if (string.IsNullOrEmpty(_botToken))
        {
            Console.WriteLine("[Telegram] ⚠️ TELEGRAM_BOT_TOKEN ortam değişkeni ayarlanmamış. Telegram bildirimleri devre dışı.");
            return;
        }

        LoadChatIds();
        Console.WriteLine($"[Telegram] ✅ Bot başlatıldı. Kayıtlı uzman sayısı: {_registeredChatIds.Count}");
    }

    /// <summary>
    /// Yeni destek talebi geldiğinde tüm kayıtlı destek uzmanlarına bildirim gönderir.
    /// </summary>
    public static async Task NotifySupportRequestAsync(string customerName, string issue, string priority, string hostId, string tenantId)
    {
        if (string.IsNullOrEmpty(_botToken) || _registeredChatIds.Count == 0)
            return;

        string priorityEmoji = priority switch
        {
            "Yüksek" => "🔴",
            "Orta" => "🟡",
            "Düşük" => "🟢",
            _ => "⚪"
        };

        string message = $"""
🔔 *YENİ DESTEK TALEBİ*

📋 Firma: *{EscapeMarkdown(customerName)}*
🎯 Konu: {EscapeMarkdown(issue)}
{priorityEmoji} Öncelik: *{EscapeMarkdown(priority)}*
💻 Bilgisayar ID: `{hostId}`
🏢 Tenant: {EscapeMarkdown(tenantId)}
⏰ Zaman: {DateTime.Now:dd.MM.yyyy HH:mm}

━━━━━━━━━━━━━━━━
_BigLineconnect'i açarak bağlanabilirsiniz_
""";

        List<long> chatIds;
        lock (_lock)
        {
            chatIds = new List<long>(_registeredChatIds);
        }

        foreach (var chatId in chatIds)
        {
            try
            {
                await SendMessageAsync(chatId, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Telegram] ❌ Bildirim gönderilemedi (ChatID: {chatId}): {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Destek uzmanı bir talebe bağlandığında ekibe bildirim gönderir.
    /// </summary>
    public static async Task NotifySupportConnectedAsync(string customerName, string issue, string hostId, string operatorInfo)
    {
        if (string.IsNullOrEmpty(_botToken) || _registeredChatIds.Count == 0)
            return;

        string message = $"""
🟢 *DESTEK BAĞLANTISI KURULDU*

📋 Firma: *{EscapeMarkdown(customerName)}*
🎯 Konu: {EscapeMarkdown(issue)}
💻 Bilgisayar ID: `{hostId}`
👨‍💻 Bağlanan: *{EscapeMarkdown(operatorInfo)}*
⏰ Zaman: {DateTime.Now:dd.MM.yyyy HH:mm}

_Destek uzmanı müşteriye bağlandı_
""";

        await BroadcastAsync(message);
    }

    /// <summary>
    /// Destek talebi çözüldüğünde ekibe bildirim gönderir.
    /// </summary>
    public static async Task NotifyTicketResolvedAsync(string customerName, string issue, string status, string notes, string hostId)
    {
        if (string.IsNullOrEmpty(_botToken) || _registeredChatIds.Count == 0)
            return;

        string statusEmoji = status switch
        {
            _ when status.Contains("Çözüldü") => "✅",
            _ when status.Contains("İptal") => "❌",
            _ => "📋"
        };

        string notesLine = !string.IsNullOrEmpty(notes) ? $"\n📝 Not: _{EscapeMarkdown(notes)}_" : "";

        string message = $"""
{statusEmoji} *TALEP KAPATILDI*

📋 Firma: *{EscapeMarkdown(customerName)}*
🎯 Konu: {EscapeMarkdown(issue)}
💻 Bilgisayar ID: `{hostId}`
📊 Durum: *{EscapeMarkdown(status)}*{notesLine}
⏰ Zaman: {DateTime.Now:dd.MM.yyyy HH:mm}
""";

        await BroadcastAsync(message);
    }

    /// <summary>
    /// Tüm kayıtlı destek uzmanlarına mesaj gönderir.
    /// </summary>
    private static async Task BroadcastAsync(string message)
    {
        List<long> chatIds;
        lock (_lock)
        {
            chatIds = new List<long>(_registeredChatIds);
        }

        foreach (var chatId in chatIds)
        {
            try
            {
                await SendMessageAsync(chatId, message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Telegram] ❌ Bildirim gönderilemedi (ChatID: {chatId}): {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Telegram bot'a gelen /start mesajlarını işler. 
    /// Yeni destek uzmanını kayıt eder.
    /// </summary>
    public static async Task ProcessBotUpdatesAsync()
    {
        if (string.IsNullOrEmpty(_botToken)) return;

        int offset = 0;
        
        while (true)
        {
            try
            {
                var url = $"https://api.telegram.org/bot{_botToken}/getUpdates?offset={offset}&timeout=30";
                var response = await _http.GetStringAsync(url);
                var doc = JsonDocument.Parse(response);

                if (doc.RootElement.TryGetProperty("result", out var results))
                {
                    foreach (var update in results.EnumerateArray())
                    {
                        int updateId = update.GetProperty("update_id").GetInt32();
                        offset = updateId + 1;

                        if (update.TryGetProperty("message", out var msg))
                        {
                            var chat = msg.GetProperty("chat");
                            long chatId = chat.GetProperty("id").GetInt64();
                            string firstName = chat.TryGetProperty("first_name", out var fn) ? fn.GetString() ?? "" : "";
                            string text = msg.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";

                            if (text.StartsWith("/start"))
                            {
                                RegisterChatId(chatId);
                                await SendMessageAsync(chatId, $"""
✅ *Kayıt Başarılı!*

Merhaba *{EscapeMarkdown(firstName)}*! 🎉

Artık yeni destek talepleri geldiğinde bu sohbete bildirim alacaksınız.

📱 Telefonunuzun bildirim sesi açık olduğundan emin olun.

_Kaydınızı silmek için /stop yazabilirsiniz._
""");
                                Console.WriteLine($"[Telegram] ✅ Yeni uzman kaydedildi: {firstName} (ChatID: {chatId})");
                            }
                            else if (text.StartsWith("/stop"))
                            {
                                UnregisterChatId(chatId);
                                await SendMessageAsync(chatId, $"""
🛑 *Kayıt Silindi*

Artık destek talebi bildirimleri almayacaksınız.

_Tekrar kayıt olmak için /start yazabilirsiniz._
""");
                                Console.WriteLine($"[Telegram] 🛑 Uzman kaydı silindi: {firstName} (ChatID: {chatId})");
                            }
                            else if (text.StartsWith("/durum"))
                            {
                                int count;
                                lock (_lock) { count = _registeredChatIds.Count; }
                                await SendMessageAsync(chatId, $"""
📊 *BigLineconnect Destek Bot Durumu*

✅ Bot aktif
👥 Kayıtlı uzman sayısı: *{count}*
⏰ Sunucu zamanı: {DateTime.Now:dd.MM.yyyy HH:mm:ss}
""");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Telegram] ⚠️ Bot güncelleme hatası: {ex.Message}");
                await Task.Delay(5000); // Hata durumunda 5 saniye bekle
            }
        }
    }

    // ═══════════════════════════════════════════
    // PRIVATE HELPERS
    // ═══════════════════════════════════════════

    private static async Task SendMessageAsync(long chatId, string text)
    {
        var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
        var payload = new
        {
            chat_id = chatId,
            text = text,
            parse_mode = "Markdown"
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(url, content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Telegram] API Hata: {response.StatusCode} - {errorBody}");
        }
    }

    private static void RegisterChatId(long chatId)
    {
        lock (_lock)
        {
            if (!_registeredChatIds.Contains(chatId))
            {
                _registeredChatIds.Add(chatId);
                SaveChatIds();
            }
        }
    }

    private static void UnregisterChatId(long chatId)
    {
        lock (_lock)
        {
            _registeredChatIds.Remove(chatId);
            SaveChatIds();
        }
    }

    private static void LoadChatIds()
    {
        try
        {
            if (File.Exists(_chatIdsFilePath))
            {
                var json = File.ReadAllText(_chatIdsFilePath);
                var ids = JsonSerializer.Deserialize<List<long>>(json);
                if (ids != null)
                {
                    lock (_lock)
                    {
                        _registeredChatIds.Clear();
                        _registeredChatIds.AddRange(ids);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Telegram] Chat ID dosyası okunamadı: {ex.Message}");
        }
    }

    private static void SaveChatIds()
    {
        try
        {
            var json = JsonSerializer.Serialize(_registeredChatIds, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_chatIdsFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Telegram] Chat ID dosyası yazılamadı: {ex.Message}");
        }
    }

    private static string EscapeMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        // Telegram Markdown V1 özel karakterleri
        return text.Replace("_", "\\_").Replace("*", "\\*").Replace("`", "\\`").Replace("[", "\\[");
    }
}
