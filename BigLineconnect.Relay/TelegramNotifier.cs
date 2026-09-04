using System.Net.Http;
using System.Text;
using System.Text.Json;

/// <summary>
/// Telegram Bot API üzerinden destek taleplerini push notification olarak gönderir.
/// Tenant (bayi) bazlı: Her destek uzmanı kendi tenant'ına kayıt olur,
/// sadece kendi müşterilerinin taleplerini alır.
/// </summary>
public static class TelegramNotifier
{
    // Bot Token — BotFather'dan alınır, ortam değişkeni veya config'den okunur
    private static string? _botToken;
    
    // Tenant bazlı kayıtlı destek uzmanları: TenantId -> List<ChatId>
    private static readonly Dictionary<string, List<long>> _tenantChatIds = new(StringComparer.OrdinalIgnoreCase);
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
        int totalUsers = 0;
        lock (_lock) { foreach (var kv in _tenantChatIds) totalUsers += kv.Value.Count; }
        Console.WriteLine($"[Telegram] ✅ Bot başlatıldı. Tenant sayısı: {_tenantChatIds.Count}, Toplam uzman: {totalUsers}");
    }

    /// <summary>
    /// Yeni destek talebi geldiğinde ilgili tenant'ın destek uzmanlarına bildirim gönderir.
    /// </summary>
    public static async Task NotifySupportRequestAsync(string customerName, string issue, string priority, string hostId, string tenantId)
    {
        if (string.IsNullOrEmpty(_botToken)) return;

        string priorityEmoji = priority switch
        {
            "Yüksek" => "🔴",
            "Orta" => "🟡",
            "Düşük" => "🟢",
            _ => "⚪"
        };

        string cleanHostId = System.Text.RegularExpressions.Regex.Replace(hostId ?? "", @"\D", "").Trim();
        string connectUrl = $"https://biglineconnect.bigus.com.tr/?id={cleanHostId}";

        string message = $"""
🔔 *YENİ DESTEK TALEBİ*

📋 Firma: *{EscapeMarkdown(customerName)}*
🎯 Konu: {EscapeMarkdown(issue)}
{priorityEmoji} Öncelik: *{EscapeMarkdown(priority)}*
💻 Bilgisayar ID: `{cleanHostId}`
🏢 Tenant: {EscapeMarkdown(tenantId)}
⏰ Zaman: {DateTime.Now:dd.MM.yyyy HH:mm}

━━━━━━━━━━━━━━━━
🔗 [Hemen Bağlan — BigLineconnect]({connectUrl})
🌐 {connectUrl}
""";

        var inlineKeyboard = new
        {
            inline_keyboard = new[]
            {
                new[]
                {
                    new { text = $"🚀 HEMEN BAĞLAN ({cleanHostId})", url = connectUrl }
                }
            }
        };

        await BroadcastToTenantAsync(tenantId, message, inlineKeyboard);
    }

    /// <summary>
    /// Destek uzmanı bir talebe bağlandığında ilgili tenant'ın ekibine bildirim gönderir.
    /// </summary>
    public static async Task NotifySupportConnectedAsync(string customerName, string issue, string hostId, string operatorInfo, string tenantId = "BIGLINE")
    {
        if (string.IsNullOrEmpty(_botToken)) return;

        string message = $"""
🟢 *DESTEK BAĞLANTISI KURULDU*

📋 Firma: *{EscapeMarkdown(customerName)}*
🎯 Konu: {EscapeMarkdown(issue)}
💻 Bilgisayar ID: `{hostId}`
👨‍💻 Bağlanan: *{EscapeMarkdown(operatorInfo)}*
⏰ Zaman: {DateTime.Now:dd.MM.yyyy HH:mm}

_Destek uzmanı müşteriye bağlandı_
""";

        await BroadcastToTenantAsync(tenantId, message);
    }

    /// <summary>
    /// Destek talebi çözüldüğünde ilgili tenant'ın ekibine bildirim gönderir.
    /// </summary>
    public static async Task NotifyTicketResolvedAsync(string customerName, string issue, string status, string notes, string hostId, string tenantId = "BIGLINE")
    {
        if (string.IsNullOrEmpty(_botToken)) return;

        string statusEmoji = status switch
        {
            _ when status.Contains("Çözüldü") => "✅",
            _ when status.Contains("Çözülmedi") || status.Contains("İptal") => "❌",
            _ when status.Contains("İncelenecek") => "🔍",
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

        await BroadcastToTenantAsync(tenantId, message);
    }

    /// <summary>
    /// Müşteri destek talebini iptal ettiğinde ilgili tenant'ın ekibine bildirim gönderir.
    /// </summary>
    public static async Task NotifyTicketCancelledAsync(string customerName, string issue, string hostId, string tenantId = "BIGLINE")
    {
        if (string.IsNullOrEmpty(_botToken)) return;

        string cleanHostId = (hostId ?? "").Replace(" ", "").Trim();

        string message = $"""
🚫 *DESTEK TALEBİ İPTAL EDİLDİ*

📋 Firma: *{EscapeMarkdown(customerName)}*
🎯 Konu: {EscapeMarkdown(issue)}
💻 Bilgisayar ID: `{cleanHostId}`
🏢 Tenant: {EscapeMarkdown(tenantId)}
⏰ Zaman: {DateTime.Now:dd.MM.yyyy HH:mm}

_Müşteri destek talebini kendi ekranından iptal etti._
""";

        await BroadcastToTenantAsync(tenantId, message);
    }

    /// <summary>
    /// Telegram bot'a gelen mesajları işler (/start, /stop, /durum).
    /// /start TENANT_ID ile tenant bazlı kayıt yapılır.
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
                                // /start TENANT_ID formatı — örn: /start SEMEDU
                                string tenantId = "BIGLINE"; // varsayılan
                                var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 2)
                                {
                                    tenantId = parts[1].Trim().ToUpperInvariant();
                                }

                                RegisterChatId(tenantId, chatId);
                                await SendMessageAsync(chatId, $"""
✅ *Kayıt Başarılı!*

Merhaba *{EscapeMarkdown(firstName)}*! 🎉

🏢 Tenant: *{tenantId}*
Artık *{tenantId}* müşterilerinden gelen destek talepleri bu sohbete bildirilecek.

📱 Telefonunuzun bildirim sesi açık olduğundan emin olun.

_Komutlar:_
/durum — Bot durumunu göster
/stop — Bildirimleri kapat
""");
                                Console.WriteLine($"[Telegram] ✅ Yeni uzman kaydedildi: {firstName} → Tenant: {tenantId} (ChatID: {chatId})");
                            }
                            else if (text.StartsWith("/stop"))
                            {
                                var removedTenants = UnregisterChatId(chatId);
                                await SendMessageAsync(chatId, $"""
🛑 *Kayıt Silindi*

Şu tenant'lardan çıkarıldınız: {string.Join(", ", removedTenants)}

_Tekrar kayıt olmak için /start TENANT\_ID yazabilirsiniz._
""");
                                Console.WriteLine($"[Telegram] 🛑 Uzman kaydı silindi: {firstName} (ChatID: {chatId})");
                            }
                            else if (text.StartsWith("/durum"))
                            {
                                string tenantInfo;
                                lock (_lock)
                                {
                                    var myTenants = _tenantChatIds.Where(kv => kv.Value.Contains(chatId)).Select(kv => kv.Key).ToList();
                                    tenantInfo = myTenants.Count > 0 ? string.Join(", ", myTenants) : "Hiçbir tenant'a kayıtlı değilsiniz";
                                }
                                await SendMessageAsync(chatId, $"""
📊 *BigLineconnect Destek Bot Durumu*

✅ Bot aktif
🏢 Kayıtlı tenant'larınız: *{tenantInfo}*
⏰ Sunucu zamanı: {DateTime.Now:dd.MM.yyyy HH:mm:ss}

_Yeni tenant eklemek için /start TENANT\_ID yazın_
""");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Telegram] ⚠️ Bot güncelleme hatası: {ex.Message}");
                await Task.Delay(5000);
            }
        }
    }

    // ═══════════════════════════════════════════
    // PRIVATE HELPERS
    // ═══════════════════════════════════════════

    /// <summary>
    /// Belirli bir tenant'ın tüm kayıtlı destek uzmanlarına mesaj gönderir.
    /// Eğer o tenant'a kayıtlı kimse yoksa veya ana yöneticiler varsa, bildirimi kaçırmamak için ana uzmanlara da iletir.
    /// </summary>
    private static async Task BroadcastToTenantAsync(string tenantId, string message, object? replyMarkup = null)
    {
        List<long> chatIds = new();
        lock (_lock)
        {
            // 1. İlgili tenant'ın uzmanlarını ekle
            if (_tenantChatIds.TryGetValue(tenantId, out var ids) && ids.Count > 0)
            {
                chatIds.AddRange(ids);
            }

            // 2. Ana yönetici (BIGLINE) uzmanlarını da ekle (Farklı bayi olsa bile merkezin haberi olsun)
            if (!tenantId.Equals("BIGLINE", StringComparison.OrdinalIgnoreCase) && 
                _tenantChatIds.TryGetValue("BIGLINE", out var masterIds))
            {
                foreach (var mid in masterIds)
                {
                    if (!chatIds.Contains(mid)) chatIds.Add(mid);
                }
            }

            // 3. Fallback: Eğer hedef tenant'ta kayıtlı kimse bulunamadıysa, sistemde kayıtlı TÜM uzmanlara gönder (Sıfır kayıp!)
            if (chatIds.Count == 0)
            {
                foreach (var kv in _tenantChatIds)
                {
                    foreach (var id in kv.Value)
                    {
                        if (!chatIds.Contains(id)) chatIds.Add(id);
                    }
                }
            }
        }

        if (chatIds.Count == 0)
        {
            Console.WriteLine($"[Telegram] ⚠️ Dikkat: Destek talebi için Telegram'da kayıtlı hiçbir uzman bulunamadı! (Tenant: {tenantId})");
            return;
        }

        foreach (var chatId in chatIds)
        {
            try
            {
                await SendMessageAsync(chatId, message, replyMarkup);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Telegram] ❌ Bildirim gönderilemedi (ChatID: {chatId}, Tenant: {tenantId}): {ex.Message}");
            }
        }
    }

    private static async Task SendMessageAsync(long chatId, string text, object? replyMarkup = null)
    {
        var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
        var payload = new Dictionary<string, object>
        {
            ["chat_id"] = chatId,
            ["text"] = text,
            ["parse_mode"] = "Markdown"
        };
        if (replyMarkup != null)
        {
            payload["reply_markup"] = replyMarkup;
        }

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(url, content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Telegram] API Hata: {response.StatusCode} - {errorBody}");
        }
    }

    private static void RegisterChatId(string tenantId, long chatId)
    {
        lock (_lock)
        {
            if (!_tenantChatIds.ContainsKey(tenantId))
                _tenantChatIds[tenantId] = new List<long>();
            
            if (!_tenantChatIds[tenantId].Contains(chatId))
                _tenantChatIds[tenantId].Add(chatId);
            
            SaveChatIds();
        }
    }

    private static List<string> UnregisterChatId(long chatId)
    {
        var removedFrom = new List<string>();
        lock (_lock)
        {
            foreach (var kv in _tenantChatIds)
            {
                if (kv.Value.Remove(chatId))
                    removedFrom.Add(kv.Key);
            }
            SaveChatIds();
        }
        return removedFrom;
    }

    private static void LoadChatIds()
    {
        try
        {
            if (File.Exists(_chatIdsFilePath))
            {
                var json = File.ReadAllText(_chatIdsFilePath);
                
                // Yeni format: {"SEMEDU": [123, 456], "BIGLINE": [789]}
                // Eski format: [123, 456] (flat liste — BIGLINE'a migrate et)
                if (json.TrimStart().StartsWith("["))
                {
                    // Eski flat format — BIGLINE'a migrate et
                    var ids = JsonSerializer.Deserialize<List<long>>(json);
                    if (ids != null && ids.Count > 0)
                    {
                        lock (_lock)
                        {
                            _tenantChatIds["BIGLINE"] = ids;
                        }
                        SaveChatIds(); // Yeni formatta kaydet
                        Console.WriteLine($"[Telegram] 📦 Eski format migrate edildi: {ids.Count} uzman → BIGLINE");
                    }
                }
                else
                {
                    // Yeni tenant bazlı format
                    var dict = JsonSerializer.Deserialize<Dictionary<string, List<long>>>(json);
                    if (dict != null)
                    {
                        lock (_lock)
                        {
                            _tenantChatIds.Clear();
                            foreach (var kv in dict)
                                _tenantChatIds[kv.Key] = kv.Value;
                        }
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
            var json = JsonSerializer.Serialize(_tenantChatIds, new JsonSerializerOptions { WriteIndented = true });
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
        return text.Replace("_", "\\_").Replace("*", "\\*").Replace("`", "\\`").Replace("[", "\\[");
    }
}
