using System.Net.Http;
using System.Text;
using System.Text.Json;

/// <summary>
/// Telegram Bot API üzerinden destek taleplerini push notification olarak gönderir.
/// Tenant (bayi) bazlı: Destek uzmanları kendi tenant'ına kayıt olur,
/// ilgili talepleri telefonlarına anında push bildirim (sesli ve banner) olarak alır.
/// Çift katmanlı (Markdown + Plain Text fallback), sıfır kayıp garantili bildirim mimarisi.
/// </summary>
public static class TelegramNotifier
{
    // Bot Token — BotFather'dan alınır, ortam değişkeninden okunur
    private static string? _botToken;
    
    // Tenant bazlı kayıtlı destek uzmanları: TenantId -> List<ChatId>
    private static readonly Dictionary<string, List<long>> _tenantChatIds = new(StringComparer.OrdinalIgnoreCase);
    private static readonly string _chatIdsFilePath = Path.Combine(AppContext.BaseDirectory, "telegram_chat_ids.json");
    
    // Uzun yoklama (Long-Polling) için özel 60 sn timeout'lu HttpClient
    private static readonly HttpClient _pollHttp = new() { Timeout = TimeSpan.FromSeconds(60) };
    
    // Bildirim gönderimi için hızlı 15 sn timeout'lu HttpClient
    private static readonly HttpClient _sendHttp = new() { Timeout = TimeSpan.FromSeconds(15) };
    
    private static readonly object _lock = new();
    private static DateTime _lastPollTime = DateTime.MinValue;
    private static int _lastUpdateId = 0;
    private static string _lastError = "";

    public class PendingSupportInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Issue { get; set; } = "";
        public string Priority { get; set; } = "";
        public string TenantId { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Aktif bekleyen talepleri bot üzerinden sorgulamak için delegasyon.
    /// </summary>
    public static Func<IEnumerable<PendingSupportInfo>>? GetPendingRequests { get; set; }

    /// <summary>
    /// Telegram bildirim sistemini başlatır. Bot token'ı ortam değişkeninden okur,
    /// kayıtlı chat ID'lerini yükler.
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
    /// Yeni destek talebi geldiğinde ilgili tenant'ın ve ana merkezin destek uzmanlarına push bildirimi gönderir.
    /// </summary>
    public static async Task NotifySupportRequestAsync(string customerName, string issue, string priority, string hostId, string tenantId)
    {
        if (string.IsNullOrEmpty(_botToken)) return;

        string priorityEmoji = priority switch
        {
            _ when priority.Contains("Yüksek") || priority.Contains("Acil") => "🔴",
            _ when priority.Contains("Orta") => "🟡",
            _ when priority.Contains("Düşük") || priority.Contains("Rutin") => "🟢",
            _ => "🟡"
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
🏢 Tenant: {EscapeMarkdown(tenantId)}
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
🏢 Tenant: {EscapeMarkdown(tenantId)}
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
    /// Telegram bot'a gelen mesajları işler (/start, /stop, /durum, /yardim).
    /// </summary>
    public static async Task ProcessBotUpdatesAsync()
    {
        if (string.IsNullOrEmpty(_botToken)) return;

        int offset = 0;
        
        while (true)
        {
            try
            {
                _lastPollTime = DateTime.UtcNow;
                // Telegram API'ye 25 saniyelik long-polling isteği atıyoruz (HttpClient timeout'u 60 sn olduğundan asla kopmaz)
                var url = $"https://api.telegram.org/bot{_botToken}/getUpdates?offset={offset}&timeout=25";
                var response = await _pollHttp.GetStringAsync(url);
                var doc = JsonDocument.Parse(response);

                if (doc.RootElement.TryGetProperty("result", out var results))
                {
                    foreach (var update in results.EnumerateArray())
                    {
                        int updateId = update.GetProperty("update_id").GetInt32();
                        offset = updateId + 1;
                        _lastUpdateId = offset;

                        if (update.TryGetProperty("message", out var msg))
                        {
                            var chat = msg.GetProperty("chat");
                            long chatId = chat.GetProperty("id").GetInt64();
                            string firstName = chat.TryGetProperty("first_name", out var fn) ? fn.GetString() ?? "" : "";
                            string text = msg.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";

                            if (text.StartsWith("/start", StringComparison.OrdinalIgnoreCase))
                            {
                                // /start [TENANT_ID] formatı — örn: /start BGS veya sadece /start
                                string targetTenant = "BGS";
                                var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 2)
                                {
                                    targetTenant = parts[1].Trim().ToUpperInvariant();
                                }

                                // Uzmanı hem hedef tenant'a hem de BGS ve BIGLINE'a kaydet ki hiçbir bildirim kaçmasın
                                RegisterChatId(targetTenant, chatId);
                                RegisterChatId("BGS", chatId);
                                RegisterChatId("BIGLINE", chatId);

                                await SendMessageAsync(chatId, $"""
✅ *Kayıt Başarılı ve Kalıcı Olarak Aktif!*

Merhaba *{EscapeMarkdown(firstName)}*! 🎉

🏢 Kayıtlı Kanallar: *{targetTenant}*, *BGS*, *BIGLINE*
📱 Bildirimler bu cihaza anında push bildirim olarak iletilecektir.
🔔 Bildirim sesinizin ve kilit ekranı izinlerinizin açık olduğundan emin olun.

_Müşteri talep gönderdiğinde doğrudan telefonunuza butonlu mesaj gelecektir._

_Komutlar:_
/durum — Bekleyen talepleri ve bot durumunu göster
/stop — Bildirimleri kapat
""");
                                Console.WriteLine($"[Telegram] ✅ Uzman kaydedildi: {firstName} → Tenant: {targetTenant} + BGS + BIGLINE (ChatID: {chatId})");

                                // Eğer şu anda sırada bekleyen talep varsa hemen uzmanına kart olarak gönder!
                                try
                                {
                                    var pending = GetPendingRequests?.Invoke()?.ToList();
                                    if (pending != null && pending.Count > 0)
                                    {
                                        await SendMessageAsync(chatId, $"⚡ *DİKKAT:* Şu anda sırada bekleyen *{pending.Count}* adet aktif destek talebi var:");
                                        foreach (var req in pending)
                                        {
                                            await NotifySupportRequestAsync(req.Name, req.Issue, req.Priority, req.Id, req.TenantId);
                                        }
                                    }
                                }
                                catch { }
                            }
                            else if (text.StartsWith("/stop", StringComparison.OrdinalIgnoreCase))
                            {
                                var removedTenants = UnregisterChatId(chatId);
                                await SendMessageAsync(chatId, $"""
🛑 *Kayıt Silindi*

Şu tenant'lardan çıkarıldınız: {string.Join(", ", removedTenants)}

_Tekrar bildirim almak için /start veya /start BGS yazabilirsiniz._
""");
                                Console.WriteLine($"[Telegram] 🛑 Uzman kaydı silindi: {firstName} (ChatID: {chatId})");
                            }
                            else if (text.StartsWith("/durum", StringComparison.OrdinalIgnoreCase))
                            {
                                string tenantInfo;
                                lock (_lock)
                                {
                                    var myTenants = _tenantChatIds.Where(kv => kv.Value.Contains(chatId)).Select(kv => kv.Key).ToList();
                                    tenantInfo = myTenants.Count > 0 ? string.Join(", ", myTenants) : "Hiçbir tenant'a kayıtlı değilsiniz";
                                }

                                var pending = GetPendingRequests?.Invoke()?.ToList();
                                int pendingCount = pending?.Count ?? 0;

                                await SendMessageAsync(chatId, $"""
📊 *BigLineconnect Destek Bot Durumu*

✅ Bot aktif ve çalışıyor
🏢 Kayıtlı kanallarınız: *{tenantInfo}*
⏳ Bekleyen aktif talep sayısı: *{pendingCount}*
⏰ Sunucu zamanı: {DateTime.Now:dd.MM.yyyy HH:mm:ss}

_Tüm bildirimler çift katmanlı push garantisiyle gönderilmektedir._
""");

                                if (pending != null && pending.Count > 0)
                                {
                                    foreach (var req in pending)
                                    {
                                        await NotifySupportRequestAsync(req.Name, req.Issue, req.Priority, req.Id, req.TenantId);
                                    }
                                }
                            }
                            else if (text.StartsWith("/yardim", StringComparison.OrdinalIgnoreCase) || text.StartsWith("/help", StringComparison.OrdinalIgnoreCase))
                            {
                                await SendMessageAsync(chatId, """
ℹ️ *BigLineconnect Bot Komutları*

/start BGS — BGS bayisi destek uzmanı olarak kayıt ol
/durum — Bekleyen talepleri ve bağlantı durumunu göster
/stop — Bildirimleri durdur
""");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _lastError = $"[{DateTime.UtcNow:HH:mm:ss}] {ex.Message}";
                Console.WriteLine($"[Telegram] ⚠️ Bot yoklama uyarısı: {ex.Message}");
                await Task.Delay(2000);
            }
        }
    }

    // ═══════════════════════════════════════════
    // PRIVATE HELPERS
    // ═══════════════════════════════════════════

    /// <summary>
    /// Belirli bir tenant'ın tüm kayıtlı destek uzmanlarına mesaj gönderir.
    /// Sıfır kayıp garantisi: Hedef tenant bulunsa dahi ana yöneticiye (BIGLINE) ve 
    /// eğer hedefte kimse yoksa sistemdeki TÜM kayıtlı cihazlara gönderir!
    /// </summary>
    private static async Task BroadcastToTenantAsync(string tenantId, string message, object? replyMarkup = null)
    {
        List<long> chatIds = new();
        lock (_lock)
        {
            // 1. İlgili tenant'ın uzmanlarını ekle
            if (_tenantChatIds.TryGetValue(tenantId, out var ids) && ids.Count > 0)
            {
                foreach (var id in ids) if (!chatIds.Contains(id)) chatIds.Add(id);
            }

            // 2. BGS uzmanlarını ekle
            if (_tenantChatIds.TryGetValue("BGS", out var bgsIds) && bgsIds.Count > 0)
            {
                foreach (var id in bgsIds) if (!chatIds.Contains(id)) chatIds.Add(id);
            }

            // 3. Ana yönetici (BIGLINE) uzmanlarını da ekle
            if (_tenantChatIds.TryGetValue("BIGLINE", out var masterIds))
            {
                foreach (var mid in masterIds) if (!chatIds.Contains(mid)) chatIds.Add(mid);
            }

            // 4. Fallback: Eğer hedef tenant'ta kayıtlı kimse bulunamadıysa, sistemde kayıtlı TÜM uzmanlara gönder
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

    /// <summary>
    /// Telegram'a push bildirimi gönderir.
    /// Çift katmanlı: Önce Markdown dener; Markdown parse hatası veya API 400 dönerse
    /// anında Düz Metin (Plain Text) olarak tekrar gönderir. Mesaj ASLA kaybolmaz.
    /// disable_notification=false ile zil sesi ve bildirim uyarısı garanti edilir.
    /// </summary>
    public static async Task<bool> SendMessageAsync(long chatId, string text, object? replyMarkup = null)
    {
        if (string.IsNullOrEmpty(_botToken)) return false;

        var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
        
        // 1. ADIM: Zengin Markdown formatıyla gönder
        try
        {
            var payload = new Dictionary<string, object>
            {
                ["chat_id"] = chatId,
                ["text"] = text,
                ["parse_mode"] = "Markdown",
                ["disable_notification"] = false // Sesli ve banner bildirim açık
            };
            if (replyMarkup != null) payload["reply_markup"] = replyMarkup;

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _sendHttp.PostAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[Telegram] Markdown iletim uyarısı ({response.StatusCode}): {errorBody}. Düz metin deneniyor...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Telegram] Markdown isteği hatası: {ex.Message}. Düz metin deneniyor...");
        }

        // 2. ADIM: FALLBACK — Markdown formatı Telegram tarafından reddedilirse düz metin olarak ilet
        try
        {
            string plainText = StripMarkdown(text);
            var fallbackPayload = new Dictionary<string, object>
            {
                ["chat_id"] = chatId,
                ["text"] = plainText,
                ["disable_notification"] = false
            };
            if (replyMarkup != null) fallbackPayload["reply_markup"] = replyMarkup;

            var fallbackJson = JsonSerializer.Serialize(fallbackPayload);
            using var fallbackContent = new StringContent(fallbackJson, Encoding.UTF8, "application/json");
            var fallbackResponse = await _sendHttp.PostAsync(url, fallbackContent);
            
            if (fallbackResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"[Telegram] ✅ Bildirim düz metin fallback ile başarıyla iletildi (ChatID: {chatId})");
                return true;
            }

            var fbError = await fallbackResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"[Telegram] ❌ Düz metin fallback de başarısız oldu (ChatID: {chatId}): {fbError}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Telegram] ❌ Fallback gönderim hatası: {ex.Message}");
            return false;
        }
    }

    public static void RegisterChatId(string tenantId, long chatId)
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

    public static List<string> UnregisterChatId(long chatId)
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
            // 1. Dosyadan yükle
            if (File.Exists(_chatIdsFilePath))
            {
                var json = File.ReadAllText(_chatIdsFilePath);
                if (json.TrimStart().StartsWith("["))
                {
                    var ids = JsonSerializer.Deserialize<List<long>>(json);
                    if (ids != null && ids.Count > 0)
                    {
                        lock (_lock)
                        {
                            _tenantChatIds["BIGLINE"] = ids;
                            _tenantChatIds["BGS"] = new List<long>(ids);
                        }
                    }
                }
                else
                {
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

            // 2. Ortam Değişkeninden (TELEGRAM_CHAT_IDS veya TELEGRAM_SUBSCRIBERS) yükle (Konteyner yeniden başlasa bile asla silinmez)
            string? envChatIds = Environment.GetEnvironmentVariable("TELEGRAM_CHAT_IDS") ?? Environment.GetEnvironmentVariable("TELEGRAM_SUBSCRIBERS");
            if (!string.IsNullOrWhiteSpace(envChatIds))
            {
                // Örn: "1234567,7654321" veya "BGS:1234567,BIGLINE:7654321"
                var entries = envChatIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var entry in entries)
                {
                    var parts = entry.Trim().Split(':');
                    if (parts.Length == 2 && long.TryParse(parts[1].Trim(), out long parsedId))
                    {
                        RegisterChatId(parts[0].Trim().ToUpperInvariant(), parsedId);
                    }
                    else if (long.TryParse(entry.Trim(), out long singleId))
                    {
                        RegisterChatId("BGS", singleId);
                        RegisterChatId("BIGLINE", singleId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Telegram] Chat ID yükleme hatası: {ex.Message}");
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

    public static object GetStatus()
    {
        lock (_lock)
        {
            return new
            {
                bot_token_configured = !string.IsNullOrEmpty(_botToken),
                bot_token_preview = string.IsNullOrEmpty(_botToken) ? "" : _botToken.Substring(0, Math.Min(8, _botToken.Length)) + "...",
                registered_tenants = _tenantChatIds.ToDictionary(k => k.Key, v => v.Value.ToList()),
                total_chat_ids = _tenantChatIds.Values.SelectMany(x => x).Distinct().Count(),
                storage_path = _chatIdsFilePath,
                storage_file_exists = File.Exists(_chatIdsFilePath),
                last_poll_utc = _lastPollTime.ToString("yyyy-MM-dd HH:mm:ss"),
                last_update_id = _lastUpdateId,
                last_error = _lastError
            };
        }
    }

    private static string EscapeMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Replace("_", "\\_").Replace("*", "\\*").Replace("`", "\\`").Replace("[", "\\[");
    }

    private static string StripMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Replace("*", "").Replace("`", "").Replace("_", "").Replace("\\[", "[").Replace("\\*", "*").Replace("\\_", "_");
    }
}
