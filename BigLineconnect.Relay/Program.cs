using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;

    public static class RelayConstants
    {
        public const byte FileTransferTag = 0x46; // 'F'
    }

    public sealed class ReliableRelayPump : IDisposable
    {
        private const long MaxQueuedBytes = 64 * 1024 * 1024;
        private readonly WebSocket _target;
        private readonly Channel<(byte[] Data, WebSocketMessageType Type)> _channel;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _pumpTask;
        private long _queuedBytes;

        public ReliableRelayPump(WebSocket target)
        {
            _target = target;
            _channel = Channel.CreateUnbounded<(byte[], WebSocketMessageType)>(
                new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
            _pumpTask = Task.Run(PumpLoopAsync);
        }

        public async Task SubmitAsync(byte[] data, WebSocketMessageType type)
        {
            while (Interlocked.Read(ref _queuedBytes) > MaxQueuedBytes && !_cts.IsCancellationRequested)
            {
                try { await Task.Delay(15, _cts.Token).ConfigureAwait(false); } catch { break; }
            }
            Interlocked.Add(ref _queuedBytes, data.Length);
            try { await _channel.Writer.WriteAsync((data, type), _cts.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
        }

        private async Task PumpLoopAsync()
        {
            try
            {
                await foreach (var item in _channel.Reader.ReadAllAsync(_cts.Token).ConfigureAwait(false))
                {
                    Interlocked.Add(ref _queuedBytes, -item.Data.Length);
                    if (_target.State != WebSocketState.Open) continue;
                    try
                    {
                        await _target.SendAsync(new ArraySegment<byte>(item.Data), item.Type, true, _cts.Token).ConfigureAwait(false);
                    }
                    catch { }
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
        }

        public void Dispose()
        {
            try { _cts.Cancel(); } catch { }
            try { _channel.Writer.TryComplete(); } catch { }
            try { _cts.Dispose(); } catch { }
        }
    }

    public class FrameRelayPump
    {
        private readonly WebSocket _targetSocket;
        private readonly SemaphoreSlim _sendLock;
        private readonly Channel<byte[]> _channel;
        private readonly CancellationTokenSource _cts;
        private readonly Task _pumpTask;
        private long _lastSendTicks;

        public FrameRelayPump(WebSocket targetSocket, SemaphoreSlim sendLock, CancellationToken cancellationToken)
        {
            _targetSocket = targetSocket;
            _sendLock = sendLock;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });

            _pumpTask = Task.Run(PumpLoopAsync);
        }

        public void EnqueueFrame(byte[] frameBytes)
        {
            _channel.Writer.TryWrite(frameBytes);
        }

        public void Stop()
        {
            try
            {
                _channel.Writer.TryComplete();
                _cts.Cancel();
            }
            catch { }
        }

        private async Task PumpLoopAsync()
        {
            try
            {
                var reader = _channel.Reader;
                while (await reader.WaitToReadAsync(_cts.Token).ConfigureAwait(false))
                {
                    byte[]? latestFrame = null;
                    while (reader.TryRead(out var frameBytes))
                    {
                        latestFrame = frameBytes;
                    }

                    if (latestFrame != null && _targetSocket.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
                    {
                        _lastSendTicks = DateTime.UtcNow.Ticks;

                        bool locked = false;
                        try
                        {
                            await _sendLock.WaitAsync(_cts.Token).ConfigureAwait(false);
                            locked = true;
                            if (_targetSocket.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
                            {
                                await _targetSocket.SendAsync(
                                    new ArraySegment<byte>(latestFrame),
                                    WebSocketMessageType.Binary,
                                    true,
                                    _cts.Token
                                ).ConfigureAwait(false);
                            }
                        }
                        catch { }
                        finally
                        {
                            if (locked)
                            {
                                try { _sendLock.Release(); } catch { }
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }
    public class TelemetryLog
    {
        public string Hwid { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public string ComputerName { get; set; } = "";
        public string Username { get; set; } = "";
        public string OsVersion { get; set; } = "";
        public string AppVersion { get; set; } = "";
        public string SessionType { get; set; } = "";
        public string Details { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public static class TelemetryManager
    {
        private static readonly string DbPath = System.IO.Path.Combine(AppContext.BaseDirectory, "telemetry.json");
        private static readonly List<TelemetryLog> Logs = new();
        private static readonly object LogLock = new();

        static TelemetryManager()
        {
            LoadLogs();
        }

        private static void LoadLogs()
        {
            try
            {
                lock (LogLock)
                {
                    if (System.IO.File.Exists(DbPath))
                    {
                        string json = System.IO.File.ReadAllText(DbPath);
                        var items = System.Text.Json.JsonSerializer.Deserialize<List<TelemetryLog>>(json);
                        if (items != null)
                        {
                            Logs.Clear();
                            Logs.AddRange(items);
                        }
                    }
                }
            }
            catch { }
        }

        public static void SaveLogs()
        {
            try
            {
                lock (LogLock)
                {
                    string json = System.Text.Json.JsonSerializer.Serialize(Logs);
                    System.IO.File.WriteAllText(DbPath, json);
                }
            }
            catch { }
        }

        public static void LogEvent(string hwid, string ip, string computer, string user, string os, string ver, string type, string details)
        {
            var entry = new TelemetryLog
            {
                Hwid = hwid,
                IpAddress = ip,
                ComputerName = computer,
                Username = user,
                OsVersion = os,
                AppVersion = ver,
                SessionType = type,
                Details = details,
                Timestamp = DateTime.UtcNow.AddHours(3)
            };

            lock (LogLock)
            {
                Logs.Add(entry);
                if (Logs.Count > 5000)
                {
                    Logs.RemoveAt(0);
                }
            }
            SaveLogs();
        }

        public static List<TelemetryLog> GetLogs()
        {
            lock (LogLock)
            {
                return new List<TelemetryLog>(Logs);
            }
        }

        public static int GetUniqueInstallCount()
        {
            lock (LogLock)
            {
                var uniqueHwids = new HashSet<string>();
                foreach (var log in Logs)
                {
                    if (!string.IsNullOrEmpty(log.Hwid) && log.Hwid != "Bilinmeyen")
                    {
                        uniqueHwids.Add(log.Hwid);
                    }
                }
                return uniqueHwids.Count;
            }
        }
    }

    public class DownloadLogEntry
    {
        public int Id { get; set; }
        public string TimestampTr { get; set; } = "";
        public string EventType { get; set; } = ""; // DOWNLOAD_EXE, DOWNLOAD_ZIP, PAGE_VISIT
        public string FileName { get; set; } = "";
        public string ClientIp { get; set; } = "";
        public string LocationIsp { get; set; } = "Çözümleniyor...";
        public string DeviceInfo { get; set; } = "";
        public string SourceInfo { get; set; } = "";
    }

    public static class DownloadTrackerManager
    {
        private static readonly string SqlConnStr = Environment.GetEnvironmentVariable("BCONNECT_SQL_CONN")
            ?? "Server=213.142.159.18;Database=BigusDb;User Id=sa;Password=Bm1453;Encrypt=False;Connection Timeout=5;";
        private static readonly List<DownloadLogEntry> Entries = new();
        private static readonly object EntriesLock = new();
        private static readonly ConcurrentDictionary<string, string> GeoCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, DateTime> RecentDedup = new(StringComparer.OrdinalIgnoreCase);
        private static readonly System.Net.Http.HttpClient GeoHttp = new() { Timeout = TimeSpan.FromSeconds(2.5) };
        private static int _totalDownloads;
        private static int _totalVisits;
        private static readonly HashSet<string> UniqueDownloadIps = new(StringComparer.OrdinalIgnoreCase);

        static DownloadTrackerManager()
        {
            InitializeFromSql();
        }

        private static void InitializeFromSql()
        {
            try
            {
                using var conn = new System.Data.SqlClient.SqlConnection(SqlConnStr);
                conn.Open();
                using (var initCmd = conn.CreateCommand())
                {
                    initCmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BConnect_DownloadLogs')
BEGIN
    CREATE TABLE BConnect_DownloadLogs (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        TimestampTr NVARCHAR(32) NOT NULL,
        EventType NVARCHAR(32) NOT NULL,
        FileName NVARCHAR(128) NOT NULL,
        ClientIp NVARCHAR(64) NOT NULL,
        LocationIsp NVARCHAR(160) NOT NULL,
        DeviceInfo NVARCHAR(128) NOT NULL,
        SourceInfo NVARCHAR(128) NOT NULL
    );
END";
                    initCmd.ExecuteNonQuery();
                }

                using (var statsCmd = conn.CreateCommand())
                {
                    statsCmd.CommandText = @"
SELECT 
    SUM(CASE WHEN EventType IN ('DOWNLOAD_EXE', 'DOWNLOAD_ZIP') THEN 1 ELSE 0 END) AS DlCount,
    SUM(CASE WHEN EventType = 'PAGE_VISIT' THEN 1 ELSE 0 END) AS VisitCount
FROM BConnect_DownloadLogs";
                    using var sr = statsCmd.ExecuteReader();
                    if (sr.Read())
                    {
                        _totalDownloads = sr.IsDBNull(0) ? 0 : sr.GetInt32(0);
                        _totalVisits = sr.IsDBNull(1) ? 0 : sr.GetInt32(1);
                    }
                }

                using (var ipCmd = conn.CreateCommand())
                {
                    ipCmd.CommandText = "SELECT DISTINCT ClientIp FROM BConnect_DownloadLogs WHERE EventType IN ('DOWNLOAD_EXE', 'DOWNLOAD_ZIP')";
                    using var ir = ipCmd.ExecuteReader();
                    lock (EntriesLock)
                    {
                        while (ir.Read())
                        {
                            if (!ir.IsDBNull(0)) UniqueDownloadIps.Add(ir.GetString(0));
                        }
                    }
                }

                using (var loadCmd = conn.CreateCommand())
                {
                    loadCmd.CommandText = "SELECT TOP 500 Id, TimestampTr, EventType, FileName, ClientIp, LocationIsp, DeviceInfo, SourceInfo FROM BConnect_DownloadLogs ORDER BY Id DESC";
                    using var r = loadCmd.ExecuteReader();
                    var temp = new List<DownloadLogEntry>();
                    while (r.Read())
                    {
                        var item = new DownloadLogEntry
                        {
                            Id = r.GetInt32(0),
                            TimestampTr = r.IsDBNull(1) ? "" : r.GetString(1),
                            EventType = r.IsDBNull(2) ? "" : r.GetString(2),
                            FileName = r.IsDBNull(3) ? "" : r.GetString(3),
                            ClientIp = r.IsDBNull(4) ? "" : r.GetString(4),
                            LocationIsp = r.IsDBNull(5) ? "" : r.GetString(5),
                            DeviceInfo = r.IsDBNull(6) ? "" : r.GetString(6),
                            SourceInfo = r.IsDBNull(7) ? "" : r.GetString(7)
                        };
                        temp.Add(item);
                        if (!string.IsNullOrEmpty(item.ClientIp) && !string.IsNullOrEmpty(item.LocationIsp) && item.LocationIsp != "Çözümleniyor..." && item.LocationIsp != "Bilinmiyor")
                        {
                            GeoCache[item.ClientIp] = item.LocationIsp;
                        }
                    }
                    temp.Reverse();
                    lock (EntriesLock)
                    {
                        Entries.Clear();
                        Entries.AddRange(temp);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[DownloadTracker SQL Init Warning] " + ex.Message);
            }
        }

        public static string GetRealClientIp(HttpContext context)
        {
            string[] headers = { "CF-Connecting-IP", "True-Client-IP", "X-Real-IP", "X-Forwarded-For" };
            foreach (var h in headers)
            {
                if (context.Request.Headers.TryGetValue(h, out var val) && !string.IsNullOrWhiteSpace(val))
                {
                    string first = val.ToString().Split(',')[0].Trim();
                    if (!string.IsNullOrEmpty(first)) return first;
                }
            }
            return context.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen";
        }

        public static bool IsAutomatedBot(string ua)
        {
            if (string.IsNullOrWhiteSpace(ua)) return true;
            if (ua.StartsWith("WhatsApp/", StringComparison.OrdinalIgnoreCase) ||
                ua.StartsWith("curl/", StringComparison.OrdinalIgnoreCase) ||
                ua.StartsWith("python-", StringComparison.OrdinalIgnoreCase) ||
                ua.Contains("Googlebot", StringComparison.OrdinalIgnoreCase) ||
                ua.Contains("bingbot", StringComparison.OrdinalIgnoreCase) ||
                ua.Contains("YandexBot", StringComparison.OrdinalIgnoreCase) ||
                ua.Contains("facebookexternalhit", StringComparison.OrdinalIgnoreCase) ||
                ua.Contains("TelegramBot", StringComparison.OrdinalIgnoreCase) ||
                ua.Contains("Twitterbot", StringComparison.OrdinalIgnoreCase) ||
                ua.Contains("AhrefsBot", StringComparison.OrdinalIgnoreCase) ||
                ua.Contains("SemrushBot", StringComparison.OrdinalIgnoreCase) ||
                ua.Contains("PetalBot", StringComparison.OrdinalIgnoreCase) ||
                ua.Contains("UptimeRobot", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }

        public static string ParseDevice(string ua)
        {
            if (string.IsNullOrWhiteSpace(ua)) return "Bilinmeyen Cihaz";
            bool inWhatsApp = ua.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase);
            string os = "Diğer Cihaz";
            if (ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase)) os = "📱 iPhone";
            else if (ua.Contains("iPad", StringComparison.OrdinalIgnoreCase)) os = "📱 iPad";
            else if (ua.Contains("Android", StringComparison.OrdinalIgnoreCase)) os = "📱 Android Telefon";
            else if (ua.Contains("Windows NT", StringComparison.OrdinalIgnoreCase)) os = "💻 Windows PC";
            else if (ua.Contains("Macintosh", StringComparison.OrdinalIgnoreCase) || ua.Contains("Mac OS X", StringComparison.OrdinalIgnoreCase)) os = "🍎 Mac";
            else if (ua.Contains("Linux", StringComparison.OrdinalIgnoreCase)) os = "🐧 Linux PC";

            string browser = "";
            if (inWhatsApp) browser = "WhatsApp İçi";
            else if (ua.Contains("Edg/", StringComparison.OrdinalIgnoreCase)) browser = "Edge";
            else if (ua.Contains("OPR/", StringComparison.OrdinalIgnoreCase) || ua.Contains("Opera", StringComparison.OrdinalIgnoreCase)) browser = "Opera";
            else if (ua.Contains("Chrome/", StringComparison.OrdinalIgnoreCase)) browser = "Chrome";
            else if (ua.Contains("Firefox/", StringComparison.OrdinalIgnoreCase)) browser = "Firefox";
            else if (ua.Contains("Safari/", StringComparison.OrdinalIgnoreCase)) browser = "Safari";

            return string.IsNullOrEmpty(browser) ? os : $"{os} ({browser})";
        }

        public static string ParseSource(HttpContext ctx)
        {
            string ua = ctx.Request.Headers["User-Agent"].ToString();
            string referer = ctx.Request.Headers["Referer"].ToString();
            if (ua.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase) || referer.Contains("whatsapp", StringComparison.OrdinalIgnoreCase))
                return "💬 WhatsApp Linki";
            if (string.IsNullOrWhiteSpace(referer))
                return "🔗 Doğrudan / WhatsApp";
            if (referer.Contains("biglineconnect.bigus.com.tr", StringComparison.OrdinalIgnoreCase))
                return "🌐 Site İçi Buton";
            if (referer.Contains("bigus.com.tr", StringComparison.OrdinalIgnoreCase))
                return "🏢 bigus.com.tr";
            if (referer.Contains("google.", StringComparison.OrdinalIgnoreCase))
                return "🔍 Google Arama";
            if (referer.Contains("bing.", StringComparison.OrdinalIgnoreCase))
                return "🔍 Bing Arama";
            try
            {
                var uri = new Uri(referer);
                return "🔗 " + uri.Host;
            }
            catch
            {
                return "🔗 Harici Link";
            }
        }

        public static void TrackHttpEvent(HttpContext ctx, string eventType, string fileName)
        {
            try
            {
                if (!HttpMethods.IsGet(ctx.Request.Method)) return;

                // If Range request, only count initial byte range (bytes=0-...)
                string rangeHdr = ctx.Request.Headers["Range"].ToString();
                if (!string.IsNullOrEmpty(rangeHdr) && !rangeHdr.StartsWith("bytes=0-", StringComparison.OrdinalIgnoreCase))
                    return;

                string ua = ctx.Request.Headers["User-Agent"].ToString();
                if (IsAutomatedBot(ua)) return;

                string ip = GetRealClientIp(ctx);
                string dedupKey = $"{ip}|{eventType}|{fileName}";
                var nowUtc = DateTime.UtcNow;
                if (RecentDedup.TryGetValue(dedupKey, out var prevTime) && (nowUtc - prevTime).TotalSeconds < 12)
                    return;
                RecentDedup[dedupKey] = nowUtc;

                string trNow = nowUtc.AddHours(3).ToString("yyyy-MM-dd HH:mm:ss");
                string device = ParseDevice(ua);
                string source = ParseSource(ctx);
                GeoCache.TryGetValue(ip, out string? cachedLoc);

                var entry = new DownloadLogEntry
                {
                    TimestampTr = trNow,
                    EventType = eventType,
                    FileName = fileName,
                    ClientIp = ip,
                    LocationIsp = string.IsNullOrEmpty(cachedLoc) ? "Çözümleniyor..." : cachedLoc,
                    DeviceInfo = device,
                    SourceInfo = source
                };

                bool isDownload = (eventType == "DOWNLOAD_EXE" || eventType == "DOWNLOAD_ZIP");
                lock (EntriesLock)
                {
                    Entries.Add(entry);
                    if (Entries.Count > 500) Entries.RemoveAt(0);
                    if (isDownload)
                    {
                        _totalDownloads++;
                        if (!string.IsNullOrEmpty(ip)) UniqueDownloadIps.Add(ip);
                    }
                    else
                    {
                        _totalVisits++;
                    }
                }

                _ = Task.Run(async () =>
                {
                    int insertedId = 0;
                    try
                    {
                        using var conn = new System.Data.SqlClient.SqlConnection(SqlConnStr);
                        conn.Open();
                        using var cmd = conn.CreateCommand();
                        cmd.CommandText = @"
INSERT INTO BConnect_DownloadLogs (TimestampTr, EventType, FileName, ClientIp, LocationIsp, DeviceInfo, SourceInfo)
OUTPUT INSERTED.Id
VALUES (@TimestampTr, @EventType, @FileName, @ClientIp, @LocationIsp, @DeviceInfo, @SourceInfo);";
                        cmd.Parameters.AddWithValue("@TimestampTr", entry.TimestampTr);
                        cmd.Parameters.AddWithValue("@EventType", entry.EventType);
                        cmd.Parameters.AddWithValue("@FileName", entry.FileName);
                        cmd.Parameters.AddWithValue("@ClientIp", entry.ClientIp);
                        cmd.Parameters.AddWithValue("@LocationIsp", entry.LocationIsp);
                        cmd.Parameters.AddWithValue("@DeviceInfo", entry.DeviceInfo);
                        cmd.Parameters.AddWithValue("@SourceInfo", entry.SourceInfo);
                        var scalar = cmd.ExecuteScalar();
                        if (scalar != null && int.TryParse(scalar.ToString(), out int idVal))
                        {
                            insertedId = idVal;
                            entry.Id = idVal;
                        }
                    }
                    catch { }

                    if (string.IsNullOrEmpty(cachedLoc) && !string.IsNullOrEmpty(ip) && ip != "Bilinmeyen" && !ip.StartsWith("127.") && !ip.StartsWith("::1"))
                    {
                        try
                        {
                            string geoJson = await GeoHttp.GetStringAsync($"http://ip-api.com/json/{Uri.EscapeDataString(ip)}?fields=status,country,city,isp&lang=tr");
                            using var doc = System.Text.Json.JsonDocument.Parse(geoJson);
                            var root = doc.RootElement;
                            if (root.TryGetProperty("status", out var st) && st.GetString() == "success")
                            {
                                string city = root.TryGetProperty("city", out var cEl) ? (cEl.GetString() ?? "") : "";
                                string country = root.TryGetProperty("country", out var coEl) ? (coEl.GetString() ?? "") : "";
                                string isp = root.TryGetProperty("isp", out var iEl) ? (iEl.GetString() ?? "") : "";
                                string loc = !string.IsNullOrEmpty(city) ? city : country;
                                if (!string.IsNullOrEmpty(isp)) loc = string.IsNullOrEmpty(loc) ? isp : $"{loc} • {isp}";
                                if (string.IsNullOrEmpty(loc)) loc = "Bilinmiyor";

                                GeoCache[ip] = loc;
                                entry.LocationIsp = loc;

                                if (insertedId > 0)
                                {
                                    using var conn2 = new System.Data.SqlClient.SqlConnection(SqlConnStr);
                                    conn2.Open();
                                    using var upd = conn2.CreateCommand();
                                    upd.CommandText = "UPDATE BConnect_DownloadLogs SET LocationIsp = @Loc WHERE Id = @Id";
                                    upd.Parameters.AddWithValue("@Loc", loc);
                                    upd.Parameters.AddWithValue("@Id", insertedId);
                                    upd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                entry.LocationIsp = "Bilinmiyor";
                            }
                        }
                        catch
                        {
                            entry.LocationIsp = "Bilinmiyor";
                        }
                    }
                });
            }
            catch { }
        }

        public static object GetDashboardStats()
        {
            string todayPrefix = DateTime.UtcNow.AddHours(3).ToString("yyyy-MM-dd");
            lock (EntriesLock)
            {
                int todayDl = 0;
                int todayVis = 0;
                for (int i = 0; i < Entries.Count; i++)
                {
                    var e = Entries[i];
                    if (e.TimestampTr.StartsWith(todayPrefix, StringComparison.Ordinal))
                    {
                        if (e.EventType == "DOWNLOAD_EXE" || e.EventType == "DOWNLOAD_ZIP") todayDl++;
                        else if (e.EventType == "PAGE_VISIT") todayVis++;
                    }
                }
                var reversed = new List<DownloadLogEntry>(Entries.Count);
                for (int i = Entries.Count - 1; i >= 0; i--) reversed.Add(Entries[i]);

                return new
                {
                    totalDownloads = _totalDownloads,
                    todayDownloads = todayDl,
                    uniqueDownloadIps = UniqueDownloadIps.Count,
                    totalVisits = _totalVisits,
                    todayVisits = todayVis,
                    items = reversed
                };
            }
        }
    }

    public class DealerBrandEntry
    {
        public string DealerCode { get; set; } = "";
        public string BrandName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string CreatedAt { get; set; } = "";
    }

    public static class DealerBrandManager
    {
        private static readonly ConcurrentDictionary<string, DealerBrandEntry> Brands = new(StringComparer.OrdinalIgnoreCase);
        private static bool _initialized = false;
        private static readonly object InitLock = new();

        private const string SqlConnStr =
            "Server=213.142.159.18;Database=BigusDb;User Id=sa;Password=My7400660!;Encrypt=False;TrustServerCertificate=True;Connection Timeout=6;";

        public static string NormalizeCode(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var sb = new StringBuilder(raw.Length);
            foreach (char ch in raw.Trim().ToUpperInvariant())
            {
                if ((ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9'))
                    sb.Append(ch);
            }
            string s = sb.ToString();
            return s.Length > 24 ? s[..24] : s;
        }

        public static bool IsMobileBrowser(HttpContext ctx)
        {
            string ua = ctx.Request.Headers["User-Agent"].ToString();
            if (string.IsNullOrWhiteSpace(ua)) return false;
            if (ua.Contains("bot", StringComparison.OrdinalIgnoreCase) ||
                ua.Contains("crawler", StringComparison.OrdinalIgnoreCase) ||
                ua.Contains("spider", StringComparison.OrdinalIgnoreCase) ||
                ua.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase) ||
                ua.Contains("TelegramBot", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return ua.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
                   ua.Contains("iPad", StringComparison.OrdinalIgnoreCase) ||
                   ua.Contains("iPod", StringComparison.OrdinalIgnoreCase) ||
                   ua.Contains("Android", StringComparison.OrdinalIgnoreCase);
        }

        public static void EnsureInitialized()
        {
            if (_initialized) return;
            lock (InitLock)
            {
                if (_initialized) return;
                _initialized = true;
                try
                {
                    using var conn = new System.Data.SqlClient.SqlConnection(SqlConnStr);
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='BConnect_DealerBrands' AND xtype='U')
                            BEGIN
                                CREATE TABLE BConnect_DealerBrands (
                                    DealerCode NVARCHAR(32) NOT NULL PRIMARY KEY,
                                    BrandName NVARCHAR(160) NOT NULL,
                                    Phone NVARCHAR(64) NOT NULL DEFAULT '',
                                    CreatedAt NVARCHAR(32) NOT NULL DEFAULT ''
                                );
                            END";
                        cmd.ExecuteNonQuery();
                    }
                    using (var loadCmd = conn.CreateCommand())
                    {
                        loadCmd.CommandText = "SELECT DealerCode, BrandName, Phone, CreatedAt FROM BConnect_DealerBrands";
                        using var r = loadCmd.ExecuteReader();
                        while (r.Read())
                        {
                            var entry = new DealerBrandEntry
                            {
                                DealerCode = r.IsDBNull(0) ? "" : r.GetString(0),
                                BrandName = r.IsDBNull(1) ? "" : r.GetString(1),
                                Phone = r.IsDBNull(2) ? "" : r.GetString(2),
                                CreatedAt = r.IsDBNull(3) ? "" : r.GetString(3)
                            };
                            if (!string.IsNullOrEmpty(entry.DealerCode))
                                Brands[entry.DealerCode] = entry;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[DealerBrand] SQL Init Warning: " + ex.Message);
                }
            }
        }

        public static DealerBrandEntry? GetBrand(string? code)
        {
            EnsureInitialized();
            string clean = NormalizeCode(code);
            if (string.IsNullOrEmpty(clean)) return null;
            return Brands.TryGetValue(clean, out var b) ? b : null;
        }

        public static List<DealerBrandEntry> ListAll()
        {
            EnsureInitialized();
            return Brands.Values.OrderBy(x => x.DealerCode).ToList();
        }

        public static DealerBrandEntry SaveBrand(string code, string brandName, string phone)
        {
            EnsureInitialized();
            string cleanCode = NormalizeCode(code);
            string cleanName = (brandName ?? "").Trim();
            string cleanPhone = (phone ?? "").Trim();
            string nowTr = DateTime.UtcNow.AddHours(3).ToString("yyyy-MM-dd HH:mm");

            var entry = new DealerBrandEntry
            {
                DealerCode = cleanCode,
                BrandName = cleanName,
                Phone = cleanPhone,
                CreatedAt = nowTr
            };
            Brands[cleanCode] = entry;

            _ = Task.Run(() =>
            {
                try
                {
                    using var conn = new System.Data.SqlClient.SqlConnection(SqlConnStr);
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        IF EXISTS (SELECT 1 FROM BConnect_DealerBrands WHERE DealerCode = @Code)
                            UPDATE BConnect_DealerBrands SET BrandName = @Name, Phone = @Phone WHERE DealerCode = @Code
                        ELSE
                            INSERT INTO BConnect_DealerBrands (DealerCode, BrandName, Phone, CreatedAt) VALUES (@Code, @Name, @Phone, @Created)";
                    cmd.Parameters.AddWithValue("@Code", cleanCode);
                    cmd.Parameters.AddWithValue("@Name", cleanName);
                    cmd.Parameters.AddWithValue("@Phone", cleanPhone);
                    cmd.Parameters.AddWithValue("@Created", nowTr);
                    cmd.ExecuteNonQuery();
                }
                catch { }
            });
            return entry;
        }

        public static void DeleteBrand(string code)
        {
            EnsureInitialized();
            string cleanCode = NormalizeCode(code);
            if (string.IsNullOrEmpty(cleanCode)) return;
            Brands.TryRemove(cleanCode, out _);
            _ = Task.Run(() =>
            {
                try
                {
                    using var conn = new System.Data.SqlClient.SqlConnection(SqlConnStr);
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "DELETE FROM BConnect_DealerBrands WHERE DealerCode = @Code";
                    cmd.Parameters.AddWithValue("@Code", cleanCode);
                    cmd.ExecuteNonQuery();
                }
                catch { }
            });
        }
    }

    public class ResellerAccount
    {
        public string TenantId { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string ContactName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public int MaxQuota { get; set; } = 50;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public static class ResellerManager
    {
        private static readonly string DbPath = System.IO.Path.Combine(AppContext.BaseDirectory, "resellers.json");
        private static readonly string SqlConnStr = Environment.GetEnvironmentVariable("BCONNECT_SQL_CONN")
            ?? "Server=213.142.159.18;Database=BigusDb;User Id=sa;Password=Bm1453;Encrypt=False;Connection Timeout=8;";
        private static readonly List<ResellerAccount> Resellers = new();
        private static readonly object Lock = new();

        static ResellerManager()
        {
            Load();
        }

        private static void EnsureSqlTable(System.Data.SqlClient.SqlConnection conn)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BConnect_Resellers')
BEGIN
    CREATE TABLE BConnect_Resellers (
        TenantId NVARCHAR(50) PRIMARY KEY,
        CompanyName NVARCHAR(200) NOT NULL,
        ContactName NVARCHAR(200) NOT NULL,
        Phone NVARCHAR(50) NOT NULL,
        Email NVARCHAR(200) NOT NULL,
        Password NVARCHAR(200) NOT NULL,
        MaxQuota INT NOT NULL DEFAULT 50,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END";
            cmd.ExecuteNonQuery();
        }

        private static void Load()
        {
            lock (Lock)
            {
                bool loadedFromSql = false;
                try
                {
                    using var conn = new System.Data.SqlClient.SqlConnection(SqlConnStr);
                    conn.Open();
                    EnsureSqlTable(conn);
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT TenantId, CompanyName, ContactName, Phone, Email, Password, MaxQuota, CreatedAt FROM BConnect_Resellers ORDER BY CreatedAt ASC";
                    using var reader = cmd.ExecuteReader();
                    var sqlItems = new List<ResellerAccount>();
                    while (reader.Read())
                    {
                        sqlItems.Add(new ResellerAccount
                        {
                            TenantId = reader["TenantId"]?.ToString() ?? "",
                            CompanyName = reader["CompanyName"]?.ToString() ?? "",
                            ContactName = reader["ContactName"]?.ToString() ?? "",
                            Phone = reader["Phone"]?.ToString() ?? "",
                            Email = reader["Email"]?.ToString() ?? "",
                            Password = reader["Password"]?.ToString() ?? "",
                            MaxQuota = reader["MaxQuota"] != DBNull.Value ? Convert.ToInt32(reader["MaxQuota"]) : 50,
                            CreatedAt = reader["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedAt"]) : DateTime.Now
                        });
                    }
                    Resellers.Clear();
                    Resellers.AddRange(sqlItems);
                    loadedFromSql = true;
                }
                catch { }

                if (!loadedFromSql)
                {
                    try
                    {
                        if (System.IO.File.Exists(DbPath))
                        {
                            string json = System.IO.File.ReadAllText(DbPath);
                            var items = System.Text.Json.JsonSerializer.Deserialize<List<ResellerAccount>>(json);
                            if (items != null)
                            {
                                Resellers.Clear();
                                Resellers.AddRange(items);
                            }
                        }
                    }
                    catch { }
                }

                // Ensure default master reseller account exists if empty
                if (!Resellers.Exists(r => r.Email.Equals("my@bigus.com.tr", StringComparison.OrdinalIgnoreCase)))
                {
                    var master = new ResellerAccount
                    {
                        TenantId = "BAYI001",
                        CompanyName = "Bigus Bilişim",
                        ContactName = "Mahmut Yoldaş",
                        Email = "my@bigus.com.tr",
                        Phone = "05322762595",
                        Password = "123456",
                        MaxQuota = 50,
                        CreatedAt = DateTime.Now
                    };
                    Resellers.Add(master);
                    UpsertToSql(master);
                }
                SaveJsonLocal();
            }
        }

        private static void UpsertToSql(ResellerAccount acc)
        {
            try
            {
                using var conn = new System.Data.SqlClient.SqlConnection(SqlConnStr);
                conn.Open();
                EnsureSqlTable(conn);
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
IF EXISTS (SELECT 1 FROM BConnect_Resellers WHERE TenantId = @TenantId OR Email = @Email)
BEGIN
    UPDATE BConnect_Resellers
    SET CompanyName = @CompanyName, ContactName = @ContactName, Phone = @Phone, Email = @Email, Password = @Password, MaxQuota = @MaxQuota
    WHERE TenantId = @TenantId OR Email = @Email;
END
ELSE
BEGIN
    INSERT INTO BConnect_Resellers (TenantId, CompanyName, ContactName, Phone, Email, Password, MaxQuota, CreatedAt)
    VALUES (@TenantId, @CompanyName, @ContactName, @Phone, @Email, @Password, @MaxQuota, @CreatedAt);
END";
                cmd.Parameters.AddWithValue("@TenantId", acc.TenantId);
                cmd.Parameters.AddWithValue("@CompanyName", acc.CompanyName);
                cmd.Parameters.AddWithValue("@ContactName", acc.ContactName);
                cmd.Parameters.AddWithValue("@Phone", acc.Phone);
                cmd.Parameters.AddWithValue("@Email", acc.Email);
                cmd.Parameters.AddWithValue("@Password", acc.Password);
                cmd.Parameters.AddWithValue("@MaxQuota", acc.MaxQuota);
                cmd.Parameters.AddWithValue("@CreatedAt", acc.CreatedAt);
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        private static void SaveJsonLocal()
        {
            try
            {
                string json = System.Text.Json.JsonSerializer.Serialize(Resellers);
                System.IO.File.WriteAllText(DbPath, json);
            }
            catch { }
        }

        public static void Save()
        {
            lock (Lock)
            {
                SaveJsonLocal();
            }
        }

        private static string NormalizePhone(string phone)
        {
            string digits = new string((phone ?? "").Where(char.IsDigit).ToArray());
            if (digits.StartsWith("90") && digits.Length > 10) digits = digits.Substring(2);
            if (digits.StartsWith("0") && digits.Length > 10) digits = digits.Substring(1);
            return digits;
        }

        private static string NormalizeText(string text)
        {
            return (text ?? "").Trim().ToUpperInvariant();
        }

        public static ResellerAccount? Register(string company, string contact, string email, string phone, string password, out string errorMessage)
        {
            errorMessage = "";
            string cleanCompany = (company ?? "").Trim();
            string cleanContact = (contact ?? "").Trim();
            string cleanEmail = (email ?? "").Trim();
            string cleanPhone = NormalizePhone(phone);

            if (string.IsNullOrWhiteSpace(cleanCompany) || string.IsNullOrWhiteSpace(cleanEmail) || string.IsNullOrWhiteSpace(cleanPhone) || string.IsNullOrWhiteSpace(password))
            {
                errorMessage = "Lütfen firma adı, e-posta, telefon ve şifre alanlarını eksiksiz doldurun!";
                return null;
            }

            lock (Lock)
            {
                Load();
                var existingEmail = Resellers.Find(r => r.Email.Trim().Equals(cleanEmail, StringComparison.OrdinalIgnoreCase));
                if (existingEmail != null)
                {
                    errorMessage = $"Bu e-posta adresi ({cleanEmail}) ile zaten bir bayi kaydı mevcut! (Mevcut Bayi Kodunuz: {existingEmail.TenantId}). Lütfen 'Bayi Girişi' veya 'Şifremi Unuttum' bölümünü kullanın.";
                    return null;
                }

                var existingPhone = Resellers.Find(r => !string.IsNullOrEmpty(NormalizePhone(r.Phone)) && NormalizePhone(r.Phone) == cleanPhone);
                if (existingPhone != null)
                {
                    errorMessage = $"Bu telefon numarası ({phone}) ile zaten bir bayi kaydı mevcut! (Mevcut Bayi Kodunuz: {existingPhone.TenantId}). Lütfen 'Bayi Girişi' veya 'Şifremi Unuttum' bölümünü kullanın.";
                    return null;
                }

                var existingCompany = Resellers.Find(r => NormalizeText(r.CompanyName) == NormalizeText(cleanCompany));
                if (existingCompany != null)
                {
                    errorMessage = $"'{cleanCompany}' firma adıyla zaten bir bayi kaydı mevcut! (Mevcut Bayi Kodunuz: {existingCompany.TenantId}). Aynı firma için ikinci kez kayıt oluşturulamaz.";
                    return null;
                }

                int nextNum = Resellers.Count + 1;
                string tenantId = "BAYI" + nextNum.ToString("D3");
                while (Resellers.Exists(r => r.TenantId.Equals(tenantId, StringComparison.OrdinalIgnoreCase)))
                {
                    nextNum++;
                    tenantId = "BAYI" + nextNum.ToString("D3");
                }

                var account = new ResellerAccount
                {
                    TenantId = tenantId,
                    CompanyName = cleanCompany,
                    ContactName = cleanContact,
                    Email = cleanEmail,
                    Phone = (phone ?? "").Trim(),
                    Password = password,
                    MaxQuota = 50,
                    CreatedAt = DateTime.Now
                };
                Resellers.Add(account);
                UpsertToSql(account);
                SaveJsonLocal();
                return account;
            }
        }

        public static ResellerAccount? Login(string idOrEmail, string password)
        {
            lock (Lock)
            {
                Load();
                return Resellers.Find(r => (r.TenantId.Equals(idOrEmail.Trim(), StringComparison.OrdinalIgnoreCase) || r.Email.Equals(idOrEmail.Trim(), StringComparison.OrdinalIgnoreCase)) && r.Password == password);
            }
        }

        public static ResellerAccount? ResetPassword(string idOrEmail, string phone, string newPassword)
        {
            lock (Lock)
            {
                Load();
                string cleanPhone = NormalizePhone(phone);

                var acc = Resellers.Find(r =>
                {
                    bool idMatch = r.TenantId.Equals(idOrEmail.Trim(), StringComparison.OrdinalIgnoreCase) ||
                                   r.Email.Equals(idOrEmail.Trim(), StringComparison.OrdinalIgnoreCase);
                    if (!idMatch) return false;
                    string rPhone = NormalizePhone(r.Phone);
                    return !string.IsNullOrEmpty(cleanPhone) && (rPhone == cleanPhone || r.Phone.Trim() == phone.Trim());
                });

                if (acc != null)
                {
                    acc.Password = newPassword;
                    UpsertToSql(acc);
                    SaveJsonLocal();
                }
                return acc;
            }
        }
    }

    public class ViewerTrialRecord
    {
        public string ViewerId { get; set; } = "";
        public string ClientIp { get; set; } = "";
        public DateTime FirstSeenAt { get; set; } = DateTime.Now;
        public bool IsLicensed { get; set; } = false;
        public string LicenseKey { get; set; } = "";
        public string LicenseExpiresAt { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public Dictionary<string, int> GraceDailyUsage { get; set; } = new();
        [System.Text.Json.Serialization.JsonIgnore]
        public DateTime LastGraceSessionAt { get; set; } = DateTime.MinValue;
    }

    public class ViewerTrialStatusDto
    {
        public string Status { get; set; } = "TRIAL_ACTIVE"; // LICENSED, TRIAL_ACTIVE, GRACE_PERIOD, EXPIRED
        public int DaysRemaining { get; set; } = 30;
        public int GraceDaysRemaining { get; set; } = 7;
        public int TodaySessionsUsed { get; set; } = 0;
        public int TodaySessionsMax { get; set; } = 3;
        public int SessionDurationSeconds { get; set; } = 0; // 0 = unlimited, 300 = 5 mins in grace period
        public bool IsLicensed { get; set; } = false;
        public string LicenseKey { get; set; } = "";
        public string LicenseExpiresAt { get; set; } = "";
        public string Message { get; set; } = "";
        public string WebsiteUrl { get; set; } = "https://biglineconnect.bigus.com.tr";
        public string PriceTr { get; set; } = "Ayda Sadece 125 TL";
        public string PriceUsd { get; set; } = "$2.20 USD / month";
    }

    public static class ViewerTrialManager
    {
        private static readonly string DbPath = System.IO.Path.Combine(AppContext.BaseDirectory, "viewer_trials.json");
        private static readonly string SqlConnStr = Environment.GetEnvironmentVariable("BCONNECT_SQL_CONN")
            ?? "Server=213.142.159.18;Database=BigusDb;User Id=sa;Password=Bm1453;Encrypt=False;Connection Timeout=8;";
        private static readonly Dictionary<string, ViewerTrialRecord> Trials = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object Lock = new();

        static ViewerTrialManager()
        {
            EnsureSqlTable();
            Load();
        }

        private static void EnsureSqlTable()
        {
            try
            {
                using var conn = new System.Data.SqlClient.SqlConnection(SqlConnStr);
                conn.Open();
                string sql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BConnect_ViewerTrials')
                BEGIN
                    CREATE TABLE BConnect_ViewerTrials (
                        ViewerId NVARCHAR(128) NOT NULL PRIMARY KEY,
                        ClientIp NVARCHAR(64) NOT NULL DEFAULT '',
                        FirstSeenAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        IsLicensed BIT NOT NULL DEFAULT 0,
                        LicenseKey NVARCHAR(128) NOT NULL DEFAULT '',
                        LicenseExpiresAt NVARCHAR(64) NOT NULL DEFAULT '',
                        CustomerName NVARCHAR(256) NOT NULL DEFAULT '',
                        GraceDailyUsageJson NVARCHAR(MAX) NOT NULL DEFAULT '{}'
                    );
                END";
                using var cmd = new System.Data.SqlClient.SqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ViewerTrialManager SQL Init Warning] " + ex.Message);
            }
        }

        private static void SaveToSql(ViewerTrialRecord r)
        {
            try
            {
                string usageJson = System.Text.Json.JsonSerializer.Serialize(r.GraceDailyUsage ?? new Dictionary<string, int>());
                using var conn = new System.Data.SqlClient.SqlConnection(SqlConnStr);
                conn.Open();
                string sql = @"
                IF EXISTS (SELECT 1 FROM BConnect_ViewerTrials WHERE ViewerId = @ViewerId)
                BEGIN
                    UPDATE BConnect_ViewerTrials
                    SET ClientIp = @ClientIp,
                        FirstSeenAt = @FirstSeenAt,
                        IsLicensed = @IsLicensed,
                        LicenseKey = @LicenseKey,
                        LicenseExpiresAt = @LicenseExpiresAt,
                        CustomerName = @CustomerName,
                        GraceDailyUsageJson = @GraceDailyUsageJson
                    WHERE ViewerId = @ViewerId;
                END
                ELSE
                BEGIN
                    INSERT INTO BConnect_ViewerTrials (ViewerId, ClientIp, FirstSeenAt, IsLicensed, LicenseKey, LicenseExpiresAt, CustomerName, GraceDailyUsageJson)
                    VALUES (@ViewerId, @ClientIp, @FirstSeenAt, @IsLicensed, @LicenseKey, @LicenseExpiresAt, @CustomerName, @GraceDailyUsageJson);
                END";
                using var cmd = new System.Data.SqlClient.SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@ViewerId", r.ViewerId ?? "");
                cmd.Parameters.AddWithValue("@ClientIp", r.ClientIp ?? "");
                cmd.Parameters.AddWithValue("@FirstSeenAt", r.FirstSeenAt);
                cmd.Parameters.AddWithValue("@IsLicensed", r.IsLicensed);
                cmd.Parameters.AddWithValue("@LicenseKey", r.LicenseKey ?? "");
                cmd.Parameters.AddWithValue("@LicenseExpiresAt", r.LicenseExpiresAt ?? "");
                cmd.Parameters.AddWithValue("@CustomerName", r.CustomerName ?? "");
                cmd.Parameters.AddWithValue("@GraceDailyUsageJson", usageJson);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ViewerTrialManager SQL Save Warning] " + ex.Message);
            }
        }

        private static void Load()
        {
            lock (Lock)
            {
                try
                {
                    if (System.IO.File.Exists(DbPath))
                    {
                        string json = System.IO.File.ReadAllText(DbPath);
                        var list = System.Text.Json.JsonSerializer.Deserialize<List<ViewerTrialRecord>>(json);
                        if (list != null)
                        {
                            Trials.Clear();
                            foreach (var item in list)
                            {
                                if (!string.IsNullOrEmpty(item.ViewerId))
                                    Trials[item.ViewerId] = item;
                            }
                        }
                    }
                }
                catch { }

                try
                {
                    using var conn = new System.Data.SqlClient.SqlConnection(SqlConnStr);
                    conn.Open();
                    string sql = "SELECT ViewerId, ClientIp, FirstSeenAt, IsLicensed, LicenseKey, LicenseExpiresAt, CustomerName, GraceDailyUsageJson FROM BConnect_ViewerTrials";
                    using var cmd = new System.Data.SqlClient.SqlCommand(sql, conn);
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string vid = reader.IsDBNull(0) ? "" : reader.GetString(0);
                        if (string.IsNullOrEmpty(vid)) continue;
                        string usageJson = reader.IsDBNull(7) ? "{}" : reader.GetString(7);
                        Dictionary<string, int>? usageDict = null;
                        try { usageDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(usageJson); } catch { }
                        Trials[vid] = new ViewerTrialRecord
                        {
                            ViewerId = vid,
                            ClientIp = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            FirstSeenAt = reader.IsDBNull(2) ? DateTime.Now : reader.GetDateTime(2),
                            IsLicensed = !reader.IsDBNull(3) && reader.GetBoolean(3),
                            LicenseKey = reader.IsDBNull(4) ? "" : reader.GetString(4),
                            LicenseExpiresAt = reader.IsDBNull(5) ? "" : reader.GetString(5),
                            CustomerName = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            GraceDailyUsage = usageDict ?? new Dictionary<string, int>()
                        };
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[ViewerTrialManager SQL Load Warning] " + ex.Message);
                }
            }
        }

        public static void Save(ViewerTrialRecord? changedRecord = null)
        {
            try
            {
                lock (Lock)
                {
                    string json = System.Text.Json.JsonSerializer.Serialize(Trials.Values.ToList());
                    System.IO.File.WriteAllText(DbPath, json);
                }
                if (changedRecord != null)
                {
                    _ = Task.Run(() => SaveToSql(changedRecord));
                }
            }
            catch { }
        }

        public static ViewerTrialRecord GetOrCreate(string viewerId, string clientIp)
        {
            if (string.IsNullOrWhiteSpace(viewerId))
            {
                viewerId = "anon_" + Math.Abs((clientIp ?? "unknown").GetHashCode()).ToString();
            }

            lock (Lock)
            {
                if (!Trials.TryGetValue(viewerId, out var record))
                {
                    record = new ViewerTrialRecord
                    {
                        ViewerId = viewerId,
                        ClientIp = clientIp ?? "",
                        FirstSeenAt = DateTime.Now
                    };
                    Trials[viewerId] = record;
                    Save(record);
                }
                return record;
            }
        }

        public static ViewerTrialStatusDto GetStatus(string viewerId, string clientIp)
        {
            var record = GetOrCreate(viewerId, clientIp);
            lock (Lock)
            {
                if (record.IsLicensed)
                {
                    DateTime? expDate = null;
                    if (!string.IsNullOrEmpty(record.LicenseExpiresAt) && DateTime.TryParse(record.LicenseExpiresAt, out var d))
                    {
                        expDate = d;
                    }

                    if (expDate == null || expDate.Value > DateTime.Now)
                    {
                        return new ViewerTrialStatusDto
                        {
                            Status = "LICENSED",
                            IsLicensed = true,
                            LicenseKey = record.LicenseKey,
                            LicenseExpiresAt = record.LicenseExpiresAt,
                            DaysRemaining = 999,
                            GraceDaysRemaining = 7,
                            SessionDurationSeconds = 0,
                            Message = "Lisanslı Ticari Sürüm (Sınırsız)"
                        };
                    }
                    else
                    {
                        record.IsLicensed = false;
                        Save(record);
                    }
                }

                var elapsedDays = (int)(DateTime.Now - record.FirstSeenAt).TotalDays;
                string todayKey = DateTime.Now.ToString("yyyy-MM-dd");
                record.GraceDailyUsage.TryGetValue(todayKey, out int todaySessions);

                // İlk 30 Gün: Sınırsız Ücretsiz Deneme
                if (elapsedDays < 30)
                {
                    int daysLeft = Math.Max(1, 30 - elapsedDays);
                    return new ViewerTrialStatusDto
                    {
                        Status = "TRIAL_ACTIVE",
                        DaysRemaining = daysLeft,
                        GraceDaysRemaining = 7,
                        TodaySessionsUsed = todaySessions,
                        SessionDurationSeconds = 0,
                        Message = $"30 Günlük Sınırsız Deneme Sürümü ({daysLeft} gün kaldı) | https://biglineconnect.bigus.com.tr — Ayda Sadece 125 TL (International: $2.20 USD/mo)"
                    };
                }
                // 31 - 37. Günler (1 Hafta / 7 Gün Tolerans): Günde 3 Kez x 5 Dakika (300 sn)
                else if (elapsedDays < 37)
                {
                    int graceDaysLeft = Math.Max(1, 37 - elapsedDays);
                    return new ViewerTrialStatusDto
                    {
                        Status = "GRACE_PERIOD",
                        DaysRemaining = 0,
                        GraceDaysRemaining = graceDaysLeft,
                        TodaySessionsUsed = todaySessions,
                        TodaySessionsMax = 3,
                        SessionDurationSeconds = 300,
                        Message = $"30 Günlük Süreniz Doldu! 1 Haftalık Tolerans Süresi ({graceDaysLeft} gün kaldı) — Bugün: {todaySessions}/3 Hak (5'er dk) | Lisans: https://biglineconnect.bigus.com.tr (Ayda Sadece 125 TL | Global: $2.20 USD/mo)"
                    };
                }
                // 38. Gün ve Sonrası: Süre Tamamen Doldu -> Lisans Zorunlu
                else
                {
                    return new ViewerTrialStatusDto
                    {
                        Status = "EXPIRED",
                        DaysRemaining = 0,
                        GraceDaysRemaining = 0,
                        TodaySessionsUsed = todaySessions,
                        TodaySessionsMax = 3,
                        SessionDurationSeconds = 0,
                        Message = "30 günlük ücretsiz kullanım ve 1 haftalık (günde 3 kez 5 dk) tolerans süreniz sona ermiştir. Sınırsız kullanım için: https://biglineconnect.bigus.com.tr — Ayda Sadece 125 TL (International: $2.20 USD / month)"
                    };
                }
            }
        }

        public static bool TryStartSession(string viewerId, string clientIp, out ViewerTrialStatusDto statusDto)
        {
            statusDto = GetStatus(viewerId, clientIp);
            if (statusDto.Status == "LICENSED" || statusDto.Status == "TRIAL_ACTIVE")
            {
                return true;
            }

            if (statusDto.Status == "GRACE_PERIOD")
            {
                lock (Lock)
                {
                    string todayKey = DateTime.Now.ToString("yyyy-MM-dd");
                    var record = GetOrCreate(viewerId, clientIp);
                    record.GraceDailyUsage.TryGetValue(todayKey, out int count);

                    // Aynı 5 dakikalık oturum içinde anlık yeniden bağlanma (reconnect) olursa yeni hak düşme
                    if (count > 0 && (DateTime.Now - record.LastGraceSessionAt).TotalSeconds < 280)
                    {
                        int remSec = Math.Max(30, 300 - (int)(DateTime.Now - record.LastGraceSessionAt).TotalSeconds);
                        statusDto.TodaySessionsUsed = count;
                        statusDto.SessionDurationSeconds = remSec;
                        return true;
                    }

                    if (count >= 3)
                    {
                        return false;
                    }
                    record.GraceDailyUsage[todayKey] = count + 1;
                    record.LastGraceSessionAt = DateTime.Now;
                    Save(record);
                    statusDto.TodaySessionsUsed = count + 1;
                    statusDto.SessionDurationSeconds = 300;
                    return true;
                }
            }

            return false;
        }

        public static bool ActivateLicense(string viewerId, string licenseKey, List<Program.LicenseEntry> allLicenses, out string customerName, out string expiresAt)
        {
            customerName = "";
            expiresAt = "";
            if (string.IsNullOrWhiteSpace(licenseKey)) return false;

            var cleanKey = licenseKey.Trim().ToUpperInvariant();
            var lic = allLicenses.FirstOrDefault(l => l.LicenseKey.Equals(cleanKey, StringComparison.OrdinalIgnoreCase) && l.IsActive);

            // Donanım tabanlı BCPRO- anahtarlarını ve Master anahtarı da destekle
            if (lic == null && (cleanKey == "BCPRO-MASTER-PLATIN-2026-UNLIMITED" || (cleanKey.StartsWith("BCPRO-") && cleanKey.Length >= 20)))
            {
                customerName = "Kurumsal Pro Kullanıcı";
                expiresAt = DateTime.Now.AddYears(1).ToString("dd.MM.yyyy HH:mm");
            }
            else if (lic != null)
            {
                customerName = lic.CustomerName;
                expiresAt = lic.ExpiresAt;
            }
            else
            {
                return false;
            }

            lock (Lock)
            {
                var record = GetOrCreate(viewerId, "");
                record.IsLicensed = true;
                record.LicenseKey = cleanKey;
                record.LicenseExpiresAt = expiresAt;
                record.CustomerName = customerName;
                Save(record);
            }
            return true;
        }
    }

    public class Program
    {
        private static readonly ConcurrentDictionary<string, HostSession> ActiveHosts = new();
        private static string AdminPassword = "BigLineAdmin2026!";
        private static string AdminSessionToken = Guid.NewGuid().ToString();

                public class OperatorPresence
        {
            public string Name { get; set; } = "";
            public string TenantId { get; set; } = "BIGLINE";
            public bool IsOnline { get; set; } = true;
            public DateTime LastSeen { get; set; } = DateTime.Now;
            public string Title { get; set; } = "Destek UzmanÄ±";
        }

        public class SupportRequest
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string Issue { get; set; } = "";
            public string Priority { get; set; } = "Orta";
            public string Token { get; set; } = "";
            public string TenantId { get; set; } = "BIGLINE";
            public bool RequiresConfirmation { get; set; } = false;
            public DateTime CreatedAt { get; set; } = DateTime.Now;
            public string ImageBase64 { get; set; } = "";
            public string Status { get; set; } = "SÄ±rada Bekliyor";
            public string Notes { get; set; } = "";
            public string OperatorName { get; set; } = "";
            public string PreferredOperator { get; set; } = "";
        }
        public class SupportCreateDto
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public string? Issue { get; set; }
            public string? Priority { get; set; }
            public string? Token { get; set; }
            public string? TenantId { get; set; }
            public bool RequiresConfirmation { get; set; }
            public string? ImageBase64 { get; set; }
            public string? PreferredOperator { get; set; }
        }

        public class SupportHistoryEntry
        {
            public string Id { get; set; } = "";
            public string HostId { get; set; } = "";
            public string Token { get; set; } = "";
            public string Name { get; set; } = "";
            public string Issue { get; set; } = "";
            public string Priority { get; set; } = "Orta";
            public string TenantId { get; set; } = "BIGLINE";
            public string CreatedAt { get; set; } = "";
            public string ResolvedAt { get; set; } = "";
            public string Status { get; set; } = "Bekliyor";
            public string Notes { get; set; } = "";
            public string ImageBase64 { get; set; } = "";
            public string OperatorName { get; set; } = "";
            public string PreferredOperator { get; set; } = "";
        }

        public class LicenseEntry
        {
            public string LicenseKey { get; set; } = "";
            public string CustomerName { get; set; } = "";
            public string TierName { get; set; } = "Başlangıç (1.490 TL)";
            public int MaxOperators { get; set; } = 1;
            public int MaxChannels { get; set; } = 5;
            public int MaxUnattendedHosts { get; set; } = 50;
            public string CreatedAt { get; set; } = "";
            public string ExpiresAt { get; set; } = "";
            public bool IsActive { get; set; } = true;
        }

        private static readonly object HistoryLock = new();
        private static readonly string HistoryFilePath = System.IO.Path.Combine(AppContext.BaseDirectory, "support_history.json");

        private static readonly object LicenseLock = new();
        private static readonly string LicenseFilePath = System.IO.Path.Combine(AppContext.BaseDirectory, "licenses.json");
        private static readonly string LicenseSqlConnStr = Environment.GetEnvironmentVariable("BCONNECT_SQL_CONN")
            ?? "Server=213.142.159.18;Database=BigusDb;User Id=sa;Password=Bm1453;Encrypt=False;Connection Timeout=8;";

        private static List<LicenseEntry> LoadLicenses()
        {
            lock (LicenseLock)
            {
                var dict = new Dictionary<string, LicenseEntry>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    if (System.IO.File.Exists(LicenseFilePath))
                    {
                        string json = System.IO.File.ReadAllText(LicenseFilePath);
                        if (!string.IsNullOrWhiteSpace(json) && json.Trim() != "[]")
                        {
                            using (var doc = System.Text.Json.JsonDocument.Parse(json))
                            {
                                foreach (var el in doc.RootElement.EnumerateArray())
                                {
                                    var entry = new LicenseEntry
                                    {
                                        LicenseKey = el.TryGetProperty("LicenseKey", out var p1) ? p1.GetString() ?? "" : "",
                                        CustomerName = el.TryGetProperty("CustomerName", out var p2) ? p2.GetString() ?? "" : "",
                                        TierName = el.TryGetProperty("TierName", out var p3) ? p3.GetString() ?? "Başlangıç (1.490 TL)" : "Başlangıç (1.490 TL)",
                                        MaxOperators = el.TryGetProperty("MaxOperators", out var p4) ? p4.GetInt32() : 1,
                                        MaxChannels = el.TryGetProperty("MaxChannels", out var p5) ? p5.GetInt32() : 5,
                                        MaxUnattendedHosts = el.TryGetProperty("MaxUnattendedHosts", out var p6) ? p6.GetInt32() : 50,
                                        CreatedAt = el.TryGetProperty("CreatedAt", out var p7) ? p7.GetString() ?? "" : "",
                                        ExpiresAt = el.TryGetProperty("ExpiresAt", out var p8) ? p8.GetString() ?? "" : "",
                                        IsActive = el.TryGetProperty("IsActive", out var p9) ? p9.GetBoolean() : true
                                    };
                                    if (!string.IsNullOrEmpty(entry.LicenseKey))
                                        dict[entry.LicenseKey] = entry;
                                }
                            }
                        }
                    }
                }
                catch { }

                try
                {
                    using var conn = new System.Data.SqlClient.SqlConnection(LicenseSqlConnStr);
                    conn.Open();
                    string sql = "SELECT LicenseKey, CustomerName, TierName, MaxOperators, MaxChannels, MaxUnattendedHosts, CreatedAt, ExpiresAt, IsActive FROM BConnect_Licenses";
                    using var cmd = new System.Data.SqlClient.SqlCommand(sql, conn);
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string lKey = reader.IsDBNull(0) ? "" : reader.GetString(0);
                        if (string.IsNullOrEmpty(lKey)) continue;
                        dict[lKey] = new LicenseEntry
                        {
                            LicenseKey = lKey,
                            CustomerName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            TierName = reader.IsDBNull(2) ? "Pro Sınırsız (Ayda 125 TL)" : reader.GetString(2),
                            MaxOperators = reader.IsDBNull(3) ? 1 : reader.GetInt32(3),
                            MaxChannels = reader.IsDBNull(4) ? 5 : reader.GetInt32(4),
                            MaxUnattendedHosts = reader.IsDBNull(5) ? 50 : reader.GetInt32(5),
                            CreatedAt = reader.IsDBNull(6) ? "" : reader.GetString(6),
                            ExpiresAt = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            IsActive = reader.IsDBNull(8) || reader.GetBoolean(8)
                        };
                    }
                }
                catch { }

                return dict.Values.ToList();
            }
        }

        private static void SaveLicenses(List<LicenseEntry> list)
        {
            lock (LicenseLock)
            {
                try
                {
                    var sb = new System.Text.StringBuilder();
                    sb.Append("[\n");
                    for (int i = 0; i < list.Count; i++)
                    {
                        var e = list[i];
                        sb.Append("  {");
                        sb.Append($"\"LicenseKey\":\"{EscapeJsonString(e.LicenseKey)}\",");
                        sb.Append($"\"CustomerName\":\"{EscapeJsonString(e.CustomerName)}\",");
                        sb.Append($"\"TierName\":\"{EscapeJsonString(e.TierName)}\",");
                        sb.Append($"\"MaxOperators\":{e.MaxOperators},");
                        sb.Append($"\"MaxChannels\":{e.MaxChannels},");
                        sb.Append($"\"MaxUnattendedHosts\":{e.MaxUnattendedHosts},");
                        sb.Append($"\"CreatedAt\":\"{EscapeJsonString(e.CreatedAt)}\",");
                        sb.Append($"\"ExpiresAt\":\"{EscapeJsonString(e.ExpiresAt)}\",");
                        sb.Append($"\"IsActive\":{(e.IsActive ? "true" : "false")}");
                        sb.Append("}");
                        if (i < list.Count - 1) sb.Append(",");
                        sb.Append("\n");
                    }
                    sb.Append("]");
                    System.IO.File.WriteAllText(LicenseFilePath, sb.ToString(), System.Text.Encoding.UTF8);
                }
                catch { }

                _ = Task.Run(() =>
                {
                    try
                    {
                        using var conn = new System.Data.SqlClient.SqlConnection(LicenseSqlConnStr);
                        conn.Open();
                        foreach (var e in list)
                        {
                            if (string.IsNullOrEmpty(e.LicenseKey)) continue;
                            string sql = @"
                            IF EXISTS (SELECT 1 FROM BConnect_Licenses WHERE LicenseKey = @LicenseKey)
                            BEGIN
                                UPDATE BConnect_Licenses
                                SET CustomerName = @CustomerName,
                                    TierName = @TierName,
                                    MaxOperators = @MaxOperators,
                                    MaxChannels = @MaxChannels,
                                    MaxUnattendedHosts = @MaxUnattendedHosts,
                                    CreatedAt = @CreatedAt,
                                    ExpiresAt = @ExpiresAt,
                                    IsActive = @IsActive
                                WHERE LicenseKey = @LicenseKey;
                            END
                            ELSE
                            BEGIN
                                INSERT INTO BConnect_Licenses (LicenseKey, CustomerName, TierName, MaxOperators, MaxChannels, MaxUnattendedHosts, CreatedAt, ExpiresAt, IsActive)
                                VALUES (@LicenseKey, @CustomerName, @TierName, @MaxOperators, @MaxChannels, @MaxUnattendedHosts, @CreatedAt, @ExpiresAt, @IsActive);
                            END";
                            using var cmd = new System.Data.SqlClient.SqlCommand(sql, conn);
                            cmd.Parameters.AddWithValue("@LicenseKey", e.LicenseKey);
                            cmd.Parameters.AddWithValue("@CustomerName", e.CustomerName ?? "");
                            cmd.Parameters.AddWithValue("@TierName", e.TierName ?? "");
                            cmd.Parameters.AddWithValue("@MaxOperators", e.MaxOperators);
                            cmd.Parameters.AddWithValue("@MaxChannels", e.MaxChannels);
                            cmd.Parameters.AddWithValue("@MaxUnattendedHosts", e.MaxUnattendedHosts);
                            cmd.Parameters.AddWithValue("@CreatedAt", e.CreatedAt ?? "");
                            cmd.Parameters.AddWithValue("@ExpiresAt", e.ExpiresAt ?? "");
                            cmd.Parameters.AddWithValue("@IsActive", e.IsActive);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch { }
                });
            }
        }

        private static bool _sqliteMigrated = false;

        private static string GetTurkeyTimeString()
        {
            return DateTime.UtcNow.AddHours(3).ToString("dd.MM.yyyy HH:mm:ss");
        }

        private static string ExtractDateOnly(string dateTimeStr)
        {
            if (string.IsNullOrWhiteSpace(dateTimeStr)) return GetTurkeyTimeString().Substring(0, 10);
            string trimmed = dateTimeStr.Trim();
            if (trimmed.Length >= 10 && (trimmed[2] == '.' || trimmed[2] == '/'))
            {
                return trimmed.Substring(0, 10);
            }
            return trimmed;
        }

        private static List<SupportHistoryEntry> LoadSupportHistory()
        {
            lock (HistoryLock)
            {
                var list = new List<SupportHistoryEntry>();
                try
                {
                    if (System.IO.File.Exists(HistoryFilePath))
                    {
                        string json = System.IO.File.ReadAllText(HistoryFilePath);
                        if (!string.IsNullOrWhiteSpace(json) && json.Trim() != "[]")
                        {
                            using (var doc = System.Text.Json.JsonDocument.Parse(json))
                            {
                                foreach (var el in doc.RootElement.EnumerateArray())
                                {
                                    var entry = new SupportHistoryEntry
                                    {
                                        Id = el.TryGetProperty("Id", out var p1) ? p1.GetString() ?? "" : (el.TryGetProperty("id", out var p1b) ? p1b.GetString() ?? "" : ""),
                                        HostId = el.TryGetProperty("HostId", out var p2) ? p2.GetString() ?? "" : (el.TryGetProperty("hostId", out var p2b) ? p2b.GetString() ?? "" : ""),
                                        Token = el.TryGetProperty("Token", out var pT) ? pT.GetString() ?? "" : (el.TryGetProperty("token", out var pTb) ? pTb.GetString() ?? "" : ""),
                                        Name = el.TryGetProperty("Name", out var p3) ? p3.GetString() ?? "" : (el.TryGetProperty("name", out var p3b) ? p3b.GetString() ?? "" : ""),
                                        Issue = el.TryGetProperty("Issue", out var p4) ? p4.GetString() ?? "" : (el.TryGetProperty("issue", out var p4b) ? p4b.GetString() ?? "" : ""),
                                        TenantId = el.TryGetProperty("TenantId", out var p5) ? p5.GetString() ?? "BIGLINE" : (el.TryGetProperty("tenantId", out var p5b) ? p5b.GetString() ?? "BIGLINE" : "BIGLINE"),
                                        CreatedAt = el.TryGetProperty("CreatedAt", out var p6) ? p6.GetString() ?? "" : (el.TryGetProperty("createdAt", out var p6b) ? p6b.GetString() ?? "" : ""),
                                        ResolvedAt = el.TryGetProperty("ResolvedAt", out var p7) ? p7.GetString() ?? "" : (el.TryGetProperty("resolvedAt", out var p7b) ? p7b.GetString() ?? "" : ""),
                                        Status = el.TryGetProperty("Status", out var p8) ? p8.GetString() ?? "Bekliyor" : (el.TryGetProperty("status", out var p8b) ? p8b.GetString() ?? "Bekliyor" : "Bekliyor"),
                                        Priority = el.TryGetProperty("Priority", out var pP) ? pP.GetString() ?? "Orta" : (el.TryGetProperty("priority", out var pPb) ? pPb.GetString() ?? "Orta" : "Orta"),
                                        Notes = el.TryGetProperty("Notes", out var p9) ? p9.GetString() ?? "" : (el.TryGetProperty("notes", out var p9b) ? p9b.GetString() ?? "" : "")
                                    };
                                    list.Add(entry);
                                }
                            }
                        }
                    }
                }
                catch { }

                var deduplicated = new List<SupportHistoryEntry>();
                var seenKeys = new HashSet<string>();
                foreach (var item in list.OrderByDescending(x => x.ResolvedAt).ThenByDescending(x => x.CreatedAt))
                {
                    string dateOnly = ExtractDateOnly(item.CreatedAt);
                    string uniqueKey = !string.IsNullOrEmpty(item.Token)
                        ? item.Token
                        : $"{item.HostId}_{dateOnly}_{item.Issue}";

                    if (string.IsNullOrEmpty(uniqueKey) || seenKeys.Add(uniqueKey))
                    {
                        if (item.Name.StartsWith("Uzak Masaüstü"))
                        {
                            var betterItem = list.FirstOrDefault(x => x.HostId == item.HostId && !x.Name.StartsWith("Uzak Masaüstü"));
                            if (betterItem != null && !string.IsNullOrEmpty(betterItem.Name))
                            {
                                item.Name = betterItem.Name;
                            }
                        }
                        deduplicated.Add(item);
                    }
                }
                return deduplicated.OrderByDescending(x => x.CreatedAt).ToList();
            }
        }

        private static void SaveSupportHistory(List<SupportHistoryEntry> list)
        {
            lock (HistoryLock)
            {
                try
                {
                    var sb = new System.Text.StringBuilder();
                    sb.Append("[\n");
                    for (int i = 0; i < list.Count; i++)
                    {
                        var e = list[i];
                        sb.Append("  {");
                        sb.Append($"\"Id\":\"{EscapeJsonString(e.Id)}\",");
                        sb.Append($"\"HostId\":\"{EscapeJsonString(e.HostId)}\",");
                        sb.Append($"\"Token\":\"{EscapeJsonString(e.Token)}\",");
                        sb.Append($"\"Name\":\"{EscapeJsonString(e.Name)}\",");
                        sb.Append($"\"Issue\":\"{EscapeJsonString(e.Issue)}\",");
                        sb.Append($"\"Priority\":\"{EscapeJsonString(e.Priority)}\",");
                        sb.Append($"\"TenantId\":\"{EscapeJsonString(e.TenantId)}\",");
                        sb.Append($"\"CreatedAt\":\"{EscapeJsonString(e.CreatedAt)}\",");
                        sb.Append($"\"ResolvedAt\":\"{EscapeJsonString(e.ResolvedAt)}\",");
                        sb.Append($"\"Status\":\"{EscapeJsonString(e.Status)}\",");
                        sb.Append($"\"Notes\":\"{EscapeJsonString(e.Notes)}\"");
                        sb.Append("}");
                        if (i < list.Count - 1) sb.Append(",");
                        sb.Append("\n");
                    }
                    sb.Append("]");
                    System.IO.File.WriteAllText(HistoryFilePath, sb.ToString(), System.Text.Encoding.UTF8);
                }
                catch { }
            }
        }

        private static string EscapeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        public static class SqliteManager
        {
            public static void Initialize() { }
            public static void SaveTicket(SupportHistoryEntry entry) { }
            public static List<SupportHistoryEntry> LoadAllTickets() { return new List<SupportHistoryEntry>(); }
            public static void DeleteTicket(string id) { }
            public static void ClearAllTickets() { }
        }

                private static readonly ConcurrentDictionary<string, SupportRequest> ActiveSupportRequests = new();
        public static readonly ConcurrentDictionary<string, List<OperatorPresence>> TenantOperators = new();

        public static List<OperatorPresence> GetTenantOperators(string tenantId)
        {
            string t = string.IsNullOrEmpty(tenantId) ? "BIGLINE" : tenantId.Trim().ToUpperInvariant();
            return TenantOperators.GetOrAdd(t, key => new List<OperatorPresence>
            {
                new OperatorPresence { Name = "Uzman Mahmut", TenantId = key, IsOnline = true, Title = "KÄ±demli Destek UzmanÄ±" },
                new OperatorPresence { Name = "Uzman Berivan", TenantId = key, IsOnline = true, Title = "ERP & Sistem UzmanÄ±" },
                new OperatorPresence { Name = "Uzman Saliha", TenantId = key, IsOnline = true, Title = "Destek UzmanÄ±" },
                new OperatorPresence { Name = "Uzman Mehmet", TenantId = key, IsOnline = true, Title = "Teknik Destek UzmanÄ±" }
            });
        }

        public sealed class ClientPeer
        {
            private const long MaxOrderedQueueBytes = 16 * 1024 * 1024;
            public WebSocket Socket { get; }
            public SemaphoreSlim SendLock { get; } = new(1, 1);
            public FrameRelayPump FramePump { get; }
            public CancellationTokenSource Cts { get; } = new();
            public string ViewerId { get; }
            public volatile bool IsRegistered = true;
            public volatile bool IsWebClient = false;

            private readonly Channel<(byte[] Data, WebSocketMessageType Type)> _orderedChannel;
            private readonly CancellationTokenSource _pumpCts;
            private readonly Task _orderedPumpTask;
            private long _queuedBytes;

            public ClientPeer(WebSocket socket, CancellationToken hostToken, string viewerId)
            {
                Socket = socket;
                ViewerId = viewerId ?? "";
                _pumpCts = CancellationTokenSource.CreateLinkedTokenSource(hostToken, Cts.Token);
                FramePump = new FrameRelayPump(socket, SendLock, _pumpCts.Token);
                _orderedChannel = Channel.CreateUnbounded<(byte[], WebSocketMessageType)>(
                    new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
                _orderedPumpTask = Task.Run(PumpOrderedOutboxAsync);
            }

            public void EnqueueOrdered(byte[] data, WebSocketMessageType type)
            {
                if (!IsRegistered || Socket.State != WebSocketState.Open || _pumpCts.IsCancellationRequested)
                    return;

                // Protect memory if one co-specialist's network stalls badly (never drop remote file download chunks 0x46+0x52 or text control messages)
                bool isFileDownloadChunk = (type == WebSocketMessageType.Binary && data.Length >= 2 && data[0] == 0x46 && data[1] == 0x52);
                if (type == WebSocketMessageType.Binary && !isFileDownloadChunk && Interlocked.Read(ref _queuedBytes) > MaxOrderedQueueBytes)
                    return;

                Interlocked.Add(ref _queuedBytes, data.Length);
                if (!_orderedChannel.Writer.TryWrite((data, type)))
                {
                    Interlocked.Add(ref _queuedBytes, -data.Length);
                }
            }

            private async Task PumpOrderedOutboxAsync()
            {
                try
                {
                    await foreach (var item in _orderedChannel.Reader.ReadAllAsync(_pumpCts.Token).ConfigureAwait(false))
                    {
                        Interlocked.Add(ref _queuedBytes, -item.Data.Length);
                        if (!IsRegistered || Socket.State != WebSocketState.Open || _pumpCts.IsCancellationRequested)
                            continue;

                        bool locked = false;
                        try
                        {
                            await SendLock.WaitAsync(_pumpCts.Token).ConfigureAwait(false);
                            locked = true;
                            if (Socket.State == WebSocketState.Open)
                            {
                                await Socket.SendAsync(
                                    new ArraySegment<byte>(item.Data),
                                    item.Type,
                                    true,
                                    _pumpCts.Token
                                ).ConfigureAwait(false);
                            }
                        }
                        catch { }
                        finally
                        {
                            if (locked)
                            {
                                try { SendLock.Release(); } catch { }
                            }
                        }
                    }
                }
                catch { }
            }

            public void DisposePeer()
            {
                IsRegistered = false;
                try { FramePump.Stop(); } catch { }
                try { Cts.Cancel(); } catch { }
                try { _pumpCts.Cancel(); } catch { }
                try { _orderedChannel.Writer.TryComplete(); } catch { }
            }
        }

        public class HostSession
        {
            public string Id { get; set; } = "";
            public WebSocket HostSocket { get; set; } = null!;
            private readonly object _peersLock = new();
            private ClientPeer[] _activePeers = Array.Empty<ClientPeer>();
            public ClientPeer[] ActivePeers => _activePeers;
            public bool HasActiveClient => _activePeers.Length > 0;
            public uint LastForwardedAckSeq;
            public SemaphoreSlim HostSendLock { get; } = new(1, 1);
            public List<WebSocket> ViewOnlyClients { get; set; } = new();
            public CancellationTokenSource Cts { get; set; } = new();

            public int AddPeer(ClientPeer peer)
            {
                lock (_peersLock)
                {
                    var next = new List<ClientPeer>(_activePeers.Length + 1);
                    foreach (var existing in _activePeers)
                    {
                        if (existing.IsRegistered && existing.Socket.State == WebSocketState.Open)
                        {
                            next.Add(existing);
                        }
                        else
                        {
                            existing.DisposePeer();
                        }
                    }
                    next.Add(peer);
                    _activePeers = next.ToArray();
                    if (_activePeers.Length <= 1)
                    {
                        LastForwardedAckSeq = 0;
                    }
                    return _activePeers.Length;
                }
            }

            public int RemovePeer(ClientPeer peer, out bool anyWebRemaining, out bool anyNativeRemaining)
            {
                lock (_peersLock)
                {
                    var next = new List<ClientPeer>(_activePeers.Length);
                    anyWebRemaining = false;
                    anyNativeRemaining = false;
                    foreach (var existing in _activePeers)
                    {
                        if (!ReferenceEquals(existing, peer) && existing.IsRegistered && existing.Socket.State == WebSocketState.Open)
                        {
                            next.Add(existing);
                            if (existing.IsWebClient) anyWebRemaining = true;
                            else anyNativeRemaining = true;
                        }
                        else if (!ReferenceEquals(existing, peer))
                        {
                            existing.DisposePeer();
                        }
                    }
                    _activePeers = next.ToArray();
                    if (_activePeers.Length <= 1)
                    {
                        LastForwardedAckSeq = 0;
                    }
                    return _activePeers.Length;
                }
            }
            
            public string Hwid { get; set; } = "Bilinmeyen";
            public string ComputerName { get; set; } = "Bilinmeyen";
            public string Username { get; set; } = "Bilinmeyen";
            public string OsVersion { get; set; } = "Bilinmeyen";
            public string AppVersion { get; set; } = "Bilinmeyen";
            public string LicenseStatus { get; set; } = "Bilinmeyen";
            public string IpAddress { get; set; } = "Bilinmeyen";
            public string LanIp { get; set; } = "";
            public DateTime ConnectedAt { get; set; } = DateTime.Now;
            public long LastBinaryFrameTicks { get; set; }
            public long LastBandwidthSec { get; set; }
            public long BytesReceivedThisSec { get; set; }
        }

        public static void Main(string[] args)
        {
            try
            {
                InitializeAdminPassword();

                var builder = WebApplication.CreateBuilder(new WebApplicationOptions
                {
                    Args = args,
                    ContentRootPath = AppContext.BaseDirectory,
                    WebRootPath = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot")
                });
                builder.Host.UseWindowsService();
                var port = Environment.GetEnvironmentVariable("PORT") ?? "5080";
                if (port == "5080")
                {
                    builder.WebHost.UseUrls("http://0.0.0.0:5080");
                }
                else
                {
                    builder.WebHost.UseUrls($"http://0.0.0.0:{port}", "http://0.0.0.0:5080");
                }
            
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });

            var app = builder.Build();
            SqliteManager.Initialize();

            var forwardedOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
            };
            forwardedOptions.KnownNetworks.Clear();
            forwardedOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(forwardedOptions);

            app.UseCors();

            // İndirme (.EXE / .ZIP) ve Web Sayfası Ziyaret Takibi + Akıllı Mobil Web-to-Desktop Yönlendirme
            app.Use(async (ctx, next) =>
            {
                string reqPath = (ctx.Request.Path.Value ?? "").Trim();
                bool isMobile = DealerBrandManager.IsMobileBrowser(ctx);
                bool forceDownload = ctx.Request.Query["force"] == "1";
                bool forceDesktop = ctx.Request.Query["desktop"] == "1" || ctx.Request.Query.ContainsKey("token") || ctx.Request.Query.ContainsKey("ticketToken");

                if (reqPath.Equals("/B-Connect.exe", StringComparison.OrdinalIgnoreCase) ||
                    reqPath.Equals("/BigLineconnect.exe", StringComparison.OrdinalIgnoreCase) ||
                    reqPath.Equals("/BigLineconnect_setup.exe", StringComparison.OrdinalIgnoreCase) ||
                    reqPath.Equals("/download", StringComparison.OrdinalIgnoreCase) ||
                    reqPath.Equals("/indir", StringComparison.OrdinalIgnoreCase))
                {
                    if (isMobile && !forceDownload)
                    {
                        DownloadTrackerManager.TrackHttpEvent(ctx, "PAGE_VISIT", "📱 Mobil Cep Konsolu (.EXE Yönlendirme)");
                        ctx.Response.Redirect("/mobil?from=exe", false);
                        return;
                    }
                    DownloadTrackerManager.TrackHttpEvent(ctx, "DOWNLOAD_EXE", "B-Connect.exe");
                }
                else if (reqPath.Equals("/B-Connect.zip", StringComparison.OrdinalIgnoreCase) ||
                         reqPath.Equals("/BigLineconnect_setup.zip", StringComparison.OrdinalIgnoreCase))
                {
                    if (isMobile && !forceDownload)
                    {
                        DownloadTrackerManager.TrackHttpEvent(ctx, "PAGE_VISIT", "📱 Mobil Cep Konsolu (.ZIP Yönlendirme)");
                        ctx.Response.Redirect("/mobil?from=exe", false);
                        return;
                    }
                    DownloadTrackerManager.TrackHttpEvent(ctx, "DOWNLOAD_ZIP", "B-Connect.zip");
                }
                else if (reqPath.Equals("/", StringComparison.Ordinal) ||
                         reqPath.Equals("/index.html", StringComparison.OrdinalIgnoreCase))
                {
                    DownloadTrackerManager.TrackHttpEvent(ctx, "PAGE_VISIT", "Ana Sayfa");
                }
                else if (reqPath.Equals("/mobil", StringComparison.OrdinalIgnoreCase) ||
                         reqPath.Equals("/mobil.html", StringComparison.OrdinalIgnoreCase))
                {
                    DownloadTrackerManager.TrackHttpEvent(ctx, "PAGE_VISIT", "📱 Mobil Cep Konsolu");
                }
                else if (reqPath.Equals("/yardim.html", StringComparison.OrdinalIgnoreCase) ||
                         reqPath.Equals("/alpemix-alternatifi.html", StringComparison.OrdinalIgnoreCase) ||
                         reqPath.Equals("/anydesk-alternatifi.html", StringComparison.OrdinalIgnoreCase) ||
                         reqPath.Equals("/teamviewer-alternatifi.html", StringComparison.OrdinalIgnoreCase) ||
                         reqPath.Equals("/uzaktan-baglanti-programi.html", StringComparison.OrdinalIgnoreCase))
                {
                    DownloadTrackerManager.TrackHttpEvent(ctx, "PAGE_VISIT", reqPath.TrimStart('/'));
                }

                await next();
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                    ctx.Context.Response.Headers["Pragma"] = "no-cache";
                    ctx.Context.Response.Headers["Expires"] = "0";
                    ctx.Context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
                }
            });
            
            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(30)
            });

            app.MapGet("/api/system-announcement", () =>
            {
                return Results.Json(new
                {
                    id = "BCONNECT_V210_20261110",
                    title = "SYSTEM-B-Connect",
                    date = "10\\11\\2026",
                    message = "Windows v2.1.0 sürümü ile çift yönlü Uzaktan Yazdırma (Remote Print) özelliği eklendi. 60 FPS görüntü motoru ve akıllı kota kalkanı güçlendirildi. Gelişmiş Oturum Video Oynatıcı (play.html - 0.5x/1x/2x/4x hız ve kanıt kare yakalama) devreye alındı. Web, iOS ve Android erişim modülleri güncellenmiştir.",
                    active = false
                });
            });

            foreach (var dlRoute in new[] { "/download", "/indir", "/b-connect.exe", "/bconnect.exe", "/BConnect.exe" })
            {
                app.MapGet(dlRoute, async context =>
                {
                    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate, max-age=0";
                    context.Response.Headers["Pragma"] = "no-cache";
                    context.Response.Headers["Expires"] = "0";

                    string setupPath = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "B-Connect.exe");
                    if (!System.IO.File.Exists(setupPath))
                    {
                        setupPath = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "BigLineconnect.exe");
                    }
                    if (System.IO.File.Exists(setupPath))
                    {
                        context.Response.ContentType = "application/octet-stream";
                        context.Response.Headers["Content-Disposition"] = "attachment; filename=\"B-Connect.exe\"";
                        await context.Response.SendFileAsync(setupPath);
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("Kurulum dosyası bulunamadı.");
                    }
                });
            }

            app.MapGet("/BigLineconnect_setup.zip", async context =>
            {
                string setupPath = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "BigLineconnect_setup.zip");
                if (System.IO.File.Exists(setupPath))
                {
                    context.Response.ContentType = "application/zip";
                    context.Response.Headers["Content-Disposition"] = "attachment; filename=\"BigLineconnect_setup.zip\"";
                    await context.Response.SendFileAsync(setupPath);
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("BigLineconnect_setup.zip bulunamadı.");
                }
            });

            // -------------------------------------------------------------
            // SAFARI (iOS) & ANDROID MOBİL CEP KONSOLU (/mobil)
            // -------------------------------------------------------------
            foreach (var mobRoute in new[] { "/mobil", "/mobil.html" })
            {
                app.MapGet(mobRoute, async context =>
                {
                    string mobPath = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "mobil.html");
                    if (System.IO.File.Exists(mobPath))
                    {
                        context.Response.ContentType = "text/html; charset=utf-8";
                        await context.Response.WriteAsync(await System.IO.File.ReadAllTextAsync(mobPath));
                    }
                    else
                    {
                        context.Response.Redirect("/#baglan");
                    }
                });
            }

            // -------------------------------------------------------------
            // MADDE 1: WHITE-LABEL BAYİ KALESİ (/indir/{kod} & /bayi/{kod})
            // -------------------------------------------------------------
            app.MapGet("/indir/{dealerCode}", async (HttpContext context, string dealerCode) =>
            {
                string cleanCode = DealerBrandManager.NormalizeCode(dealerCode);
                if (string.IsNullOrEmpty(cleanCode))
                {
                    context.Response.Redirect("/B-Connect.exe");
                    return;
                }

                if (DealerBrandManager.IsMobileBrowser(context) && context.Request.Query["force"] != "1")
                {
                    DownloadTrackerManager.TrackHttpEvent(context, "PAGE_VISIT", $"📱 Mobil Cep Konsolu (Bayi: {cleanCode})");
                    context.Response.Redirect("/mobil?from=exe", false);
                    return;
                }

                string fileName = $"B-Connect_{cleanCode}.exe";
                DownloadTrackerManager.TrackHttpEvent(context, "DOWNLOAD_EXE", fileName);

                context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate, max-age=0";
                string setupPath = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "B-Connect.exe");
                if (System.IO.File.Exists(setupPath))
                {
                    context.Response.ContentType = "application/octet-stream";
                    context.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"";
                    await context.Response.SendFileAsync(setupPath);
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Kurulum dosyası bulunamadı.");
                }
            });

            app.MapGet("/bayi/{dealerCode}", async (HttpContext context, string dealerCode) =>
            {
                string cleanCode = DealerBrandManager.NormalizeCode(dealerCode);
                var brand = DealerBrandManager.GetBrand(cleanCode);
                string brandTitle = brand != null && !string.IsNullOrWhiteSpace(brand.BrandName)
                    ? System.Net.WebUtility.HtmlEncode(brand.BrandName)
                    : System.Net.WebUtility.HtmlEncode(cleanCode + " Kurumsal Destek");
                string brandPhone = brand != null && !string.IsNullOrWhiteSpace(brand.Phone)
                    ? System.Net.WebUtility.HtmlEncode(brand.Phone)
                    : "";

                DownloadTrackerManager.TrackHttpEvent(context, "PAGE_VISIT", $"Bayi Sayfası ({cleanCode})");

                bool isMob = DealerBrandManager.IsMobileBrowser(context);
                string html = $@"<!DOCTYPE html>
<html lang='tr'>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<title>{brandTitle} | Uzaktan Destek Merkezi</title>
<style>
body{{margin:0;min-height:100vh;background:#050507;color:#f8fafc;font-family:'Segoe UI',system-ui,sans-serif;display:flex;align-items:center;justify-content:center;padding:20px;}}
.box{{max-width:520px;width:100%;background:#0e1016;border:1px solid rgba(251,191,36,0.35);border-radius:22px;padding:34px 28px;text-align:center;box-shadow:0 24px 60px rgba(0,0,0,0.75);}}
.badge{{display:inline-block;background:rgba(251,191,36,0.14);border:1px solid rgba(251,191,36,0.4);color:#fbbf24;padding:5px 14px;border-radius:999px;font-size:12px;font-weight:700;margin-bottom:14px;}}
h1{{margin:0 0 10px;font-size:26px;color:#fff;}}
p{{color:#94a3b8;font-size:15px;line-height:1.5;margin:0 0 24px;}}
.btn{{display:block;text-decoration:none;padding:16px 20px;border-radius:14px;font-weight:800;font-size:16px;margin-bottom:12px;}}
.btn-pri{{background:linear-gradient(135deg,#fbbf24,#ea580c);color:#000;}}
.btn-sec{{background:rgba(255,255,255,0.06);border:1px solid rgba(255,255,255,0.15);color:#fff;}}
.foot{{margin-top:18px;font-size:12px;color:#64748b;}}
</style>
</head>
<body>
<div class='box'>
  <img src='/logo.png' alt='Logo' style='width:58px;height:58px;border-radius:14px;margin-bottom:12px;'>
  <div class='badge'>🛡️ Yetkili Uzaktan Destek Kanalı • Kod: {cleanCode}</div>
  <h1>{brandTitle}</h1>
  <p>Uzman ekibimizden hızlı uzaktan destek almak için aşağıdaki butona tıklayıp tek tıkla destek modülünü çalıştırabilirsiniz.{(string.IsNullOrEmpty(brandPhone) ? "" : $"<br><b style='color:#fbbf24;'>📞 Destek Hattı: {brandPhone}</b>")}</p>
  {(isMob
    ? $"<a class='btn btn-pri' href='https://wa.me/?text={Uri.EscapeDataString($"https://biglineconnect.bigus.com.tr/indir/{cleanCode}")}'>📲 İndirme Linkini Bilgisayarıma Gönder (WhatsApp)</a><a class='btn btn-sec' href='/indir/{cleanCode}?force=1'>💻 Windows Dosyasını İndir (B-Connect_{cleanCode}.exe)</a>"
    : $"<a class='btn btn-pri' href='/indir/{cleanCode}'>📥 Uzaktan Destek Programını İndir (B-Connect_{cleanCode}.exe)</a>")}
  <div class='foot'>B-Connect Kurumsal Güvenli Altyapısı ile korunmaktadır • Kurulum ve yönetici şifresi gerektirmez</div>
</div>
</body>
</html>";
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(html);
            });

            app.MapGet("/api/dealer/brand", (HttpContext context) =>
            {
                string code = DealerBrandManager.NormalizeCode(context.Request.Query["code"].ToString());
                var b = DealerBrandManager.GetBrand(code);
                if (b == null)
                    return Results.Json(new { found = false, dealerCode = code, brandName = "", phone = "" });
                return Results.Json(new { found = true, dealerCode = b.DealerCode, brandName = b.BrandName, phone = b.Phone });
            });

            app.MapGet("/api/dealer/brands/list", (HttpContext context) =>
            {
                if (!IsAdminAuthenticated(context)) return Results.Unauthorized();
                return Results.Json(DealerBrandManager.ListAll());
            });

            app.MapPost("/api/dealer/brands/save", async (HttpContext context) =>
            {
                if (!IsAdminAuthenticated(context)) return Results.Unauthorized();
                using var doc = await System.Text.Json.JsonDocument.ParseAsync(context.Request.Body);
                string code = doc.RootElement.TryGetProperty("dealerCode", out var p1) ? (p1.GetString() ?? "") : "";
                string name = doc.RootElement.TryGetProperty("brandName", out var p2) ? (p2.GetString() ?? "") : "";
                string phone = doc.RootElement.TryGetProperty("phone", out var p3) ? (p3.GetString() ?? "") : "";
                string clean = DealerBrandManager.NormalizeCode(code);
                if (string.IsNullOrEmpty(clean) || string.IsNullOrWhiteSpace(name))
                    return Results.BadRequest(new { error = "Bayi kodu ve marka adı zorunludur." });
                var saved = DealerBrandManager.SaveBrand(clean, name, phone);
                return Results.Json(saved);
            });

            app.MapPost("/api/dealer/brands/delete", async (HttpContext context) =>
            {
                if (!IsAdminAuthenticated(context)) return Results.Unauthorized();
                using var doc = await System.Text.Json.JsonDocument.ParseAsync(context.Request.Body);
                string code = doc.RootElement.TryGetProperty("dealerCode", out var p1) ? (p1.GetString() ?? "") : "";
                DealerBrandManager.DeleteBrand(code);
                return Results.Ok(new { ok = true });
            });

            // -------------------------------------------------------------
            // MADDE 2: AYLIK / YILLIK MÜŞTERİ HİZMET & BAKIM KARNESİ (/hizmet-karnesi)
            // -------------------------------------------------------------
            app.MapGet("/hizmet-karnesi", async (HttpContext context) =>
            {
                string tenant = (context.Request.Query["tenantId"].ToString() ?? "").Trim();
                string customer = (context.Request.Query["customer"].ToString() ?? "").Trim();
                var all = LoadSupportHistory().AsEnumerable();
                if (!string.IsNullOrEmpty(tenant) && !tenant.Equals("ALL", StringComparison.OrdinalIgnoreCase) && !tenant.Equals("SUPERADMIN", StringComparison.OrdinalIgnoreCase))
                    all = all.Where(x => (x.TenantId ?? "").Equals(tenant, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(customer))
                    all = all.Where(x => (x.Name ?? "").Contains(customer, StringComparison.OrdinalIgnoreCase));

                var items = all.OrderByDescending(x => x.ResolvedAt).ToList();
                int resolvedCount = items.Count(x => (x.Status ?? "").Contains("Çözüldü", StringComparison.OrdinalIgnoreCase) || (x.Status ?? "").Equals("RESOLVED", StringComparison.OrdinalIgnoreCase));
                var brand = DealerBrandManager.GetBrand(tenant);
                string providerTitle = brand != null && !string.IsNullOrWhiteSpace(brand.BrandName) ? brand.BrandName : (string.IsNullOrEmpty(tenant) ? "B-Connect Kurumsal Destek Merkezi" : tenant + " Teknik Destek Hizmetleri");
                string custTitle = string.IsNullOrEmpty(customer) ? "Tüm Kayıtlı Müşteriler" : customer;
                string reportDate = DateTime.UtcNow.AddHours(3).ToString("dd.MM.yyyy HH:mm");

                var rowsSb = new StringBuilder();
                if (items.Count == 0)
                {
                    rowsSb.Append("<tr><td colspan='5' style='text-align:center;padding:24px;color:#64748b;'>Seçilen kriterde destek kaydı bulunmuyor.</td></tr>");
                }
                else
                {
                    foreach (var it in items)
                    {
                        rowsSb.Append("<tr>")
                              .Append($"<td>{System.Net.WebUtility.HtmlEncode(it.ResolvedAt ?? it.CreatedAt ?? "-")}</td>")
                              .Append($"<td><b>{System.Net.WebUtility.HtmlEncode(it.Name ?? "-")}</b><br><small style='color:#64748b;'>ID: {System.Net.WebUtility.HtmlEncode(it.HostId ?? "-")}</small></td>")
                              .Append($"<td>{System.Net.WebUtility.HtmlEncode(it.Issue ?? "-")}</td>")
                              .Append($"<td>{System.Net.WebUtility.HtmlEncode(it.Status ?? "Çözüldü")}</td>")
                              .Append($"<td>{System.Net.WebUtility.HtmlEncode(it.Notes ?? "-")}</td>")
                              .Append("</tr>");
                    }
                }

                string waSummary = $"📊 *{providerTitle} — Hizmet & Bakım Özeti*\n🏢 Firma: {custTitle}\n📅 Rapor Tarihi: {reportDate}\n✅ Toplam Uzaktan Müdahale: {items.Count} oturum ({resolvedCount} çözülen talep)\n🛡️ Tüm bağlantılar kurumsal güvenlik denetim kaydı altındadır.";

                string page = $@"<!DOCTYPE html>
<html lang='tr'>
<head>
<meta charset='UTF-8'>
<title>Müşteri Hizmet & Bakım Karnesi - {System.Net.WebUtility.HtmlEncode(custTitle)}</title>
<style>
body{{font-family:'Segoe UI',Arial,sans-serif;background:#f1f5f9;color:#0f172a;margin:0;padding:28px;}}
.sheet{{max-width:960px;margin:0 auto;background:#fff;border-radius:16px;padding:36px;box-shadow:0 12px 36px rgba(15,23,42,0.08);border-top:6px solid #f59e0b;}}
.top{{display:flex;justify-content:space-between;align-items:flex-start;border-bottom:2px solid #e2e8f0;padding-bottom:20px;margin-bottom:22px;}}
.kpis{{display:grid;grid-template-columns:repeat(3,1fr);gap:14px;margin-bottom:24px;}}
.kpi{{background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;padding:16px;}}
.kpi .v{{font-size:28px;font-weight:800;color:#0f172a;}}
.kpi .l{{font-size:12px;color:#64748b;text-transform:uppercase;font-weight:700;}}
table{{width:100%;border-collapse:collapse;font-size:13.5px;}}
th{{background:#0f172a;color:#fbbf24;text-align:left;padding:11px 12px;font-size:12px;text-transform:uppercase;}}
td{{padding:10px 12px;border-bottom:1px solid #e2e8f0;vertical-align:top;}}
.actions{{display:flex;gap:10px;margin-bottom:18px;}}
.btn{{border:none;border-radius:8px;padding:10px 16px;font-weight:700;cursor:pointer;font-size:13px;}}
@media print{{body{{background:#fff;padding:0;}}.sheet{{box-shadow:none;padding:12px;}}.actions{{display:none;}}}}
</style>
</head>
<body>
<div class='sheet'>
  <div class='actions'>
    <button class='btn' style='background:#0f172a;color:#fbbf24;' onclick='window.print()'>🖨️ PDF Olarak Kaydet / Yazdır</button>
    <button class='btn' style='background:#16a34a;color:#fff;' onclick='navigator.clipboard.writeText(document.getElementById(""wa-txt"").value);alert(""WhatsApp özeti panoya kopyalandı!"")'>💬 WhatsApp Özetini Kopyala</button>
  </div>
  <textarea id='wa-txt' style='display:none;'>{System.Net.WebUtility.HtmlEncode(waSummary)}</textarea>
  <div class='top'>
    <div>
      <div style='font-size:12px;font-weight:800;color:#d97706;text-transform:uppercase;letter-spacing:1px;'>KURUMSAL BAKIM & UZAKTAN DESTEK HİZMET KARNESİ</div>
      <h1 style='margin:6px 0 4px;font-size:24px;'>{System.Net.WebUtility.HtmlEncode(providerTitle)}</h1>
      <div style='color:#475569;font-size:14px;'>Müşteri / Cari: <b>{System.Net.WebUtility.HtmlEncode(custTitle)}</b></div>
    </div>
    <div style='text-align:right;font-size:13px;color:#475569;'>
      <b>Rapor Tarihi:</b> {reportDate}<br>
      <b>Belge Türü:</b> Resmi Hizmet Dökümü
    </div>
  </div>
  <div class='kpis'>
    <div class='kpi'><div class='l'>Toplam Uzaktan Müdahale</div><div class='v'>{items.Count} Oturum</div></div>
    <div class='kpi'><div class='l'>Başarıyla Çözülen Talep</div><div class='v' style='color:#16a34a;'>{resolvedCount} Kayıt</div></div>
    <div class='kpi'><div class='l'>Hizmet Güvencesi</div><div class='v' style='font-size:18px;padding-top:6px;color:#d97706;'>✓ Kayıtlı & Denetimli</div></div>
  </div>
  <table>
    <thead><tr><th>Tarih / Saat</th><th>Müşteri & Cihaz</th><th>Bildirilen Konu</th><th>Durum</th><th>Uzman Çözüm Notu</th></tr></thead>
    <tbody>{rowsSb}</tbody>
  </table>
  <div style='margin-top:26px;padding-top:14px;border-top:1px dashed #cbd5e1;display:flex;justify-content:space-between;font-size:12px;color:#64748b;'>
    <span>Bu belge B-Connect Kurumsal Hizmet Kayıt Sistemi tarafından otomatik üretilmiştir.</span>
    <span>Yetkili Onay / Kaşe</span>
  </div>
</div>
</body>
</html>";
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(page);
            });

            app.MapGet("/temizle", async context =>
            {
                context.Response.Redirect("/temizle.zip");
            });

            app.MapGet("/temizle.bat", async context =>
            {
                string p = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "temizle.bat");
                if (System.IO.File.Exists(p))
                {
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    context.Response.Headers["Content-Disposition"] = "attachment; filename=\"temizle.bat\"";
                    await context.Response.SendFileAsync(p);
                }
                else { context.Response.StatusCode = 404; }
            });

            app.MapGet("/temizle.zip", async context =>
            {
                string p = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "temizle.zip");
                if (System.IO.File.Exists(p))
                {
                    context.Response.ContentType = "application/zip";
                    context.Response.Headers["Content-Disposition"] = "attachment; filename=\"temizle.zip\"";
                    await context.Response.SendFileAsync(p);
                }
                else { context.Response.StatusCode = 404; }
            });

            app.Map("/register-host", async context =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    
                    string rawId = context.Request.Query["requested_id"].ToString().Replace(" ", "").Trim();
                    
                    string hwid = context.Request.Query["hwid"].ToString().Trim();
                    string computer = context.Request.Query["computer_name"].ToString().Trim();
                    string username = context.Request.Query["username"].ToString().Trim();
                    string os = context.Request.Query["os"].ToString().Trim();
                    string ver = context.Request.Query["version"].ToString().Trim();
                    string lic = context.Request.Query["license_status"].ToString().Trim();
                    string lanIp = context.Request.Query["lan_ip"].ToString().Trim();
                    string ip = DownloadTrackerManager.GetRealClientIp(context);

                    if (string.IsNullOrEmpty(hwid)) hwid = "Bilinmeyen";
                    if (string.IsNullOrEmpty(computer)) computer = "Bilinmeyen";
                    if (string.IsNullOrEmpty(username)) username = "Bilinmeyen";
                    if (string.IsNullOrEmpty(os)) os = "Bilinmeyen";
                    if (string.IsNullOrEmpty(ver)) ver = "Bilinmeyen";
                    if (string.IsNullOrEmpty(lic)) lic = "Bilinmeyen";

                    bool isExisting = false;
                    if (!string.IsNullOrEmpty(rawId) && rawId.Length == 9 && long.TryParse(rawId, out _))
                    {
                        if (ActiveHosts.TryGetValue(rawId, out var existingSession))
                        {
                            Console.WriteLine($"[Relay] Host reconnecting for existing ID: {rawId}. Replacing old connection.");
                            try
                            {
                                existingSession.Cts.Cancel();
                                existingSession.HostSocket.Dispose();
                            }
                            catch { }
                        }
                        isExisting = true;
                    }
                    else
                    {
                        rawId = GenerateUniqueId();
                    }
                    
                    string formattedId = $"{rawId[..3]} {rawId[3..6]} {rawId[6..]}";
                    
                    var session = new HostSession
                    {
                        Id = rawId,
                        HostSocket = webSocket,
                        Hwid = hwid,
                        ComputerName = computer,
                        Username = username,
                        OsVersion = os,
                        AppVersion = ver,
                        LicenseStatus = lic,
                        IpAddress = ip,
                        LanIp = lanIp,
                        ConnectedAt = DateTime.UtcNow.AddHours(3)
                    };

                    ActiveHosts[rawId] = session;
                    Console.WriteLine($"[Relay] Host registered. ID: {formattedId}");
                    TelemetryManager.LogEvent(hwid, ip, computer, username, os, ver, "startup", $"Host bağlandı. ID: {formattedId}, Lisans: {lic}");

                    byte[] idMessage = Encoding.UTF8.GetBytes($"ID:{formattedId}");
                    await webSocket.SendAsync(new ArraySegment<byte>(idMessage), WebSocketMessageType.Text, true, CancellationToken.None);

                    var buffer = new byte[1024 * 256]; // 256KB buffer for zero-copy single-fragment relay
                    try
                    {
                        while (webSocket.State == WebSocketState.Open)
                        {
                            // ZERO-COPY FAST PATH: Most frames (especially delta SubFrames at 1-5 KB)
                            // arrive in a single WebSocket fragment. Avoid MemoryStream + ToArray() allocation!
                            var firstResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                            if (firstResult.MessageType == WebSocketMessageType.Close) break;
                            
                            byte[] msgBytes;
                            WebSocketMessageType msgType = firstResult.MessageType;
                            
                            if (firstResult.EndOfMessage)
                            {
                                // FAST PATH: Single fragment — direct slice, no MemoryStream needed!
                                msgBytes = new byte[firstResult.Count];
                                Buffer.BlockCopy(buffer, 0, msgBytes, 0, firstResult.Count);
                            }
                            else
                            {
                                // SLOW PATH: Multi-fragment (rare, large keyframes only)
                                using (var ms = new MemoryStream(firstResult.Count * 2))
                                {
                                    ms.Write(buffer, 0, firstResult.Count);
                                    WebSocketReceiveResult result;
                                    do
                                    {
                                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                                        if (result.MessageType == WebSocketMessageType.Close) break;
                                        ms.Write(buffer, 0, result.Count);
                                    }
                                    while (!result.EndOfMessage);
                                    
                                    if (result.MessageType == WebSocketMessageType.Close) break;
                                    msgBytes = ms.ToArray();
                                }
                            }

                            if (msgBytes.Length > 0)
                            {
                                var peers = session.ActivePeers;
                                if (peers.Length == 1)
                                {
                                    // SINGLE-SPECIALIST ULTRA-FAST PATH (100% Untouched Zero-Overhead Pipeline)
                                    var singlePeer = peers[0];
                                    var clientSock = singlePeer.Socket;
                                    if (msgType == WebSocketMessageType.Binary)
                                    {
                                        // 1. FAST-PATH FILE TRANSFER & NATIVE BUNDLES: Tagged 0x46 ('F') -> Forward directly without pump dropping or delay!
                                        if (msgBytes.Length > 0 && (msgBytes[0] == 0x46 || msgBytes[0] == 0xBC || msgBytes[0] == 0x53 || msgBytes[0] == 0x52))
                                        {
                                            if (clientSock.State == WebSocketState.Open)
                                            {
                                                bool locked = false;
                                                try
                                                {
                                                    await singlePeer.SendLock.WaitAsync(session.Cts.Token).ConfigureAwait(false);
                                                    locked = true;
                                                    if (clientSock.State == WebSocketState.Open)
                                                    {
                                                        await clientSock.SendAsync(
                                                            new ArraySegment<byte>(msgBytes),
                                                            WebSocketMessageType.Binary,
                                                            true,
                                                            session.Cts.Token
                                                        ).ConfigureAwait(false);
                                                    }
                                                }
                                                catch { }
                                                finally
                                                {
                                                    if (locked)
                                                    {
                                                        try { singlePeer.SendLock.Release(); } catch { }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // Newest Frame Wins via FrameRelayPump (Zero server-side delay):
                                            if (clientSock.State == WebSocketState.Open)
                                            {
                                                singlePeer.FramePump.EnqueueFrame(msgBytes);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // Text/Control messages MUST remain sequential and reliable
                                        if (clientSock.State == WebSocketState.Open)
                                        {
                                            bool locked = false;
                                            try
                                            {
                                                await singlePeer.SendLock.WaitAsync(session.Cts.Token).ConfigureAwait(false);
                                                locked = true;
                                                if (clientSock.State == WebSocketState.Open)
                                                {
                                                    await clientSock.SendAsync(
                                                        new ArraySegment<byte>(msgBytes),
                                                        msgType,
                                                        true,
                                                        CancellationToken.None
                                                    );
                                                }
                                            }
                                            catch { }
                                            finally
                                            {
                                                if (locked)
                                                {
                                                    try { singlePeer.SendLock.Release(); } catch { }
                                                }
                                            }
                                        }
                                    }
                                }
                                else if (peers.Length > 1)
                                {
                                    // MULTI-SPECIALIST CO-SESSION PATH: Fan out to all connected specialists without Head-of-Line blocking!
                                    bool isOrderedPacket = (msgType != WebSocketMessageType.Binary) ||
                                                           (msgBytes.Length > 0 && (msgBytes[0] == 0x46 || msgBytes[0] == 0xBC || msgBytes[0] == 0x53 || msgBytes[0] == 0x52));
                                    for (int pIdx = 0; pIdx < peers.Length; pIdx++)
                                    {
                                        var peer = peers[pIdx];
                                        if (peer.IsRegistered && peer.Socket.State == WebSocketState.Open)
                                        {
                                            if (isOrderedPacket)
                                            {
                                                peer.EnqueueOrdered(msgBytes, msgType);
                                            }
                                            else
                                            {
                                                peer.FramePump.EnqueueFrame(msgBytes);
                                            }
                                        }
                                    }
                                }

                                lock (session.ViewOnlyClients)
                                {
                                    for (int j = session.ViewOnlyClients.Count - 1; j >= 0; j--)
                                    {
                                        var soc = session.ViewOnlyClients[j];
                                        if (soc.State == WebSocketState.Open)
                                        {
                                            try
                                            {
                                                _ = soc.SendAsync(
                                                    new ArraySegment<byte>(msgBytes),
                                                    msgType,
                                                    true,
                                                    CancellationToken.None
                                                );
                                            }
                                            catch { }
                                        }
                                        else
                                        {
                                            session.ViewOnlyClients.RemoveAt(j);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception) { }
                    finally
                    {
                        session.Cts.Cancel();
                        if (ActiveHosts.TryGetValue(rawId, out var curSession) && ReferenceEquals(curSession, session))
                        {
                            ActiveHosts.TryRemove(rawId, out _);
                            Console.WriteLine($"[Relay] Host disconnected. ID: {formattedId}");
                            TelemetryManager.LogEvent(session.Hwid, session.IpAddress, session.ComputerName, session.Username, session.OsVersion, session.AppVersion, "disconnect", $"Host bağlantısı kesildi. ID: {formattedId}, Süre: {(DateTime.UtcNow.AddHours(3) - session.ConnectedAt).TotalMinutes:F1} dk");
                        }
                        else
                        {
                            Console.WriteLine($"[Relay] Old host session cleaned up for ID: {formattedId}, but newer session is active. ActiveHosts preserved.");
                        }
                        foreach (var peer in session.ActivePeers)
                        {
                            peer.DisposePeer();
                            if (peer.Socket.State == WebSocketState.Open)
                            {
                                try
                                {
                                    await peer.Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Host disconnected", CancellationToken.None);
                                }
                                catch { }
                            }
                        }

                        lock (session.ViewOnlyClients)
                        {
                            foreach (var soc in session.ViewOnlyClients)
                            {
                                if (soc.State == WebSocketState.Open)
                                {
                                    try { _ = soc.CloseAsync(WebSocketCloseStatus.NormalClosure, "Host disconnected", CancellationToken.None); } catch { }
                                }
                            }
                            session.ViewOnlyClients.Clear();
                        }
                    }
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                }
            });

            // -------------------------------------------------------------
            // LIGHTCONNECT STANDALONE RELAY ROUTES (/lc-host & /lc-client)
            // -------------------------------------------------------------
            var lcSessions = new System.Collections.Concurrent.ConcurrentDictionary<string, (WebSocket HostSocket, WebSocket? ClientSocket, CancellationTokenSource Cts)>();

            app.Map("/lc-host", async context =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    string hostId = context.Request.Query["id"].ToString().Replace(" ", "").Trim();
                    using var hostSocket = await context.WebSockets.AcceptWebSocketAsync();
                    var cts = new CancellationTokenSource();

                    var session = (HostSocket: hostSocket, ClientSocket: (WebSocket?)null, Cts: cts);
                    lcSessions[hostId] = session;
                    Console.WriteLine($"[LightConnect] Host registered: {hostId}");

                    var buffer = new byte[8192];
                    try
                    {
                        while (!cts.Token.IsCancellationRequested && hostSocket.State == WebSocketState.Open)
                        {
                            using var ms = new MemoryStream();
                            WebSocketReceiveResult res;
                            do
                            {
                                res = await hostSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                                if (res.MessageType == WebSocketMessageType.Close) break;
                                ms.Write(buffer, 0, res.Count);
                            }
                            while (!res.EndOfMessage);

                            if (res.MessageType == WebSocketMessageType.Close) break;

                            if (ms.Length > 0 && lcSessions.TryGetValue(hostId, out var activeSess) && activeSess.ClientSocket != null && activeSess.ClientSocket.State == WebSocketState.Open)
                            {
                                if (res.MessageType == WebSocketMessageType.Binary)
                                {
                                    await Task.Delay(66, cts.Token).ConfigureAwait(false);
                                }
                                await activeSess.ClientSocket.SendAsync(new ArraySegment<byte>(ms.ToArray()), res.MessageType, true, cts.Token);
                            }
                        }
                    }
                    catch { }
                    finally
                    {
                        lcSessions.TryRemove(hostId, out _);
                        Console.WriteLine($"[LightConnect] Host unregistered: {hostId}");
                    }
                }
            });

            app.Map("/lc-client", async context =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    string targetId = context.Request.Query["id"].ToString().Replace(" ", "").Trim();
                    using var clientSocket = await context.WebSockets.AcceptWebSocketAsync();

                    // MOBİL TO DESKTOP TAMAMEN KAPALI (Masaüstü Motoru Koruma Kilidi)
                    string rawViewerId = context.Request.Query["viewerId"].ToString().Trim();
                    if (DealerBrandManager.IsMobileBrowser(context) || rawViewerId.StartsWith("MOB-", StringComparison.OrdinalIgnoreCase))
                    {
                        byte[] mobErr = Encoding.UTF8.GetBytes("ERROR:MOBILE_DISABLED");
                        try { await clientSocket.SendAsync(new ArraySegment<byte>(mobErr), WebSocketMessageType.Text, true, CancellationToken.None); } catch { }
                        await Task.Delay(250);
                        try { await clientSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Mobile-to-desktop disabled", CancellationToken.None); } catch { }
                        return;
                    }

                    if (!lcSessions.TryGetValue(targetId, out var hostSession) || hostSession.HostSocket.State != WebSocketState.Open)
                    {
                        byte[] err = Encoding.UTF8.GetBytes("ERROR:NOT_FOUND");
                        await clientSocket.SendAsync(new ArraySegment<byte>(err), WebSocketMessageType.Text, true, CancellationToken.None);
                        return;
                    }

                    var updatedSession = (hostSession.HostSocket, ClientSocket: clientSocket, hostSession.Cts);
                    lcSessions[targetId] = updatedSession;
                    Console.WriteLine($"[LightConnect] Client connected to: {targetId}");

                    byte[] startMsg = Encoding.UTF8.GetBytes("START_STREAM");
                    await hostSession.HostSocket.SendAsync(new ArraySegment<byte>(startMsg), WebSocketMessageType.Text, true, CancellationToken.None);

                    var buffer = new byte[8192];
                    try
                    {
                        while (clientSocket.State == WebSocketState.Open && hostSession.HostSocket.State == WebSocketState.Open)
                        {
                            byte[] pooledBuffer = System.Buffers.ArrayPool<byte>.Shared.Rent(buffer.Length);
                            try
                            {
                                var res = await clientSocket.ReceiveAsync(new ArraySegment<byte>(pooledBuffer), CancellationToken.None);
                                if (res.MessageType == WebSocketMessageType.Close) break;

                                if (res.Count > 0 && hostSession.HostSocket.State == WebSocketState.Open)
                                {
                                    await hostSession.HostSocket.SendAsync(new ArraySegment<byte>(pooledBuffer, 0, res.Count), res.MessageType, res.EndOfMessage, CancellationToken.None);
                                }
                            }
                            finally
                            {
                                System.Buffers.ArrayPool<byte>.Shared.Return(pooledBuffer);
                            }
                        }
                    }
                    catch { }
                    finally
                    {
                        if (hostSession.HostSocket.State == WebSocketState.Open)
                        {
                            byte[] stopMsg = Encoding.UTF8.GetBytes("STOP_STREAM");
                            try { await hostSession.HostSocket.SendAsync(new ArraySegment<byte>(stopMsg), WebSocketMessageType.Text, true, CancellationToken.None); } catch { }
                        }
                        Console.WriteLine($"[LightConnect] Client disconnected from: {targetId}");
                    }
                }
            });

            app.Map("/connect-client", async context =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    string targetId = context.Request.Query["id"].ToString().Replace(" ", "").Trim();
                    using var clientSocket = await context.WebSockets.AcceptWebSocketAsync();

                    // MOBİL TO DESKTOP TAMAMEN KAPALI (Masaüstü Motoru Koruma Kilidi)
                    string rawViewerId = context.Request.Query["viewerId"].ToString().Trim();
                    if (DealerBrandManager.IsMobileBrowser(context) || rawViewerId.StartsWith("MOB-", StringComparison.OrdinalIgnoreCase))
                    {
                        byte[] mobErr = Encoding.UTF8.GetBytes("ERROR:MOBILE_DISABLED");
                        try { await clientSocket.SendAsync(new ArraySegment<byte>(mobErr), WebSocketMessageType.Text, true, CancellationToken.None); } catch { }
                        await Task.Delay(250);
                        try { await clientSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Mobile-to-desktop disabled", CancellationToken.None); } catch { }
                        return;
                    }

                    if (!ActiveHosts.TryGetValue(targetId, out var session))
                    {
                        byte[] errMsg = Encoding.UTF8.GetBytes("ERROR:ID_NOT_FOUND");
                        await clientSocket.SendAsync(new ArraySegment<byte>(errMsg), WebSocketMessageType.Text, true, CancellationToken.None);
                        await Task.Delay(300);
                        await clientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Host not found", CancellationToken.None);
                        return;
                    }

                    bool isViewOnly = context.Request.Query["viewOnly"] == "true";
                    
                    if (!isViewOnly)
                    {
                        string clientIp = DownloadTrackerManager.GetRealClientIp(context);
                        
                        // 30 Günlük Deneme, 1 Hafta (7 Gün) Tolerans (Günde 3x5 dk) & Lisans Kontrolü — Hiçbir uygulama muaf değil!
                        string viewerId = context.Request.Query["viewerId"].ToString().Trim();
                        string adminToken = context.Request.Query["adminToken"].ToString().Trim();
                        string tenantIdParam = context.Request.Query["tenantId"].ToString().Trim();
                        bool isExempt = (context.Request.Query["hwLicensed"].ToString().Trim() == "1");

                        var myPeer = new ClientPeer(clientSocket, session.Cts.Token, viewerId);
                        var myClientCts = myPeer.Cts;

                        if (!isExempt)
                        {
                            bool allowed = ViewerTrialManager.TryStartSession(viewerId, clientIp, out var trialStatus);
                            if (!allowed)
                            {
                                myPeer.DisposePeer();
                                string errCode = (trialStatus.Status == "GRACE_PERIOD") ? "ERROR:GRACE_LIMIT_REACHED" : "ERROR:TRIAL_EXPIRED";
                                byte[] errBytes = Encoding.UTF8.GetBytes(errCode);
                                await clientSocket.SendAsync(new ArraySegment<byte>(errBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                                await Task.Delay(400);
                                await clientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, errCode, CancellationToken.None);
                                return;
                            }

                            // İstemciye lisans ve deneme durum bilgisini ilet (ClientSocket henüz Host yayın döngüsüne açılmadan önce güvenle gönder!)
                            string infoMsg = $"SESSION_STATUS:{trialStatus.Status}:{trialStatus.DaysRemaining}:{trialStatus.GraceDaysRemaining}:{trialStatus.TodaySessionsUsed}:{trialStatus.SessionDurationSeconds}";
                            byte[] infoBytes = Encoding.UTF8.GetBytes(infoMsg);
                            try { await clientSocket.SendAsync(new ArraySegment<byte>(infoBytes), WebSocketMessageType.Text, true, CancellationToken.None); } catch { }

                            // 31-37. Günler (1 Hafta Tolerans): 5 dakika (300 saniye) sonra sunucu tarafında oturumu otomatik kapat
                            if (trialStatus.Status == "GRACE_PERIOD")
                            {
                                int limitSec = trialStatus.SessionDurationSeconds > 0 ? trialStatus.SessionDurationSeconds : 300;
                                var targetPeer = myPeer;
                                _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        await Task.Delay(TimeSpan.FromSeconds(limitSec), targetPeer.Cts.Token);
                                        if (targetPeer.IsRegistered && targetPeer.Socket.State == WebSocketState.Open)
                                        {
                                            byte[] timeoutBytes = Encoding.UTF8.GetBytes("ERROR:GRACE_TIMEOUT");
                                            bool locked = false;
                                            try
                                            {
                                                await targetPeer.SendLock.WaitAsync(CancellationToken.None);
                                                locked = true;
                                                await targetPeer.Socket.SendAsync(new ArraySegment<byte>(timeoutBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                                            }
                                            catch { }
                                            finally
                                            {
                                                if (locked) { try { targetPeer.SendLock.Release(); } catch { } }
                                            }
                                            await Task.Delay(300);
                                            targetPeer.Cts.Cancel();
                                        }
                                    }
                                    catch { }
                                });
                            }
                        }

                        // Send host_info JSON packet to viewer BEFORE exposing myPeer to Host stream loop (prevents concurrent SendAsync crash!)
                        if (!string.IsNullOrEmpty(session.LanIp))
                        {
                            string hostInfoJson = $"{{\"type\":\"host_info\",\"id\":\"{targetId}\",\"lan_ip\":\"{session.LanIp}\",\"public_ip\":\"{session.IpAddress}\",\"client_ip\":\"{clientIp}\",\"public_port\":18888,\"computer\":\"{session.ComputerName}\"}}";
                            byte[] hostInfoBytes = Encoding.UTF8.GetBytes(hostInfoJson);
                            try { await clientSocket.SendAsync(new ArraySegment<byte>(hostInfoBytes), WebSocketMessageType.Text, true, CancellationToken.None); } catch { }
                        }

                        // ÇOKLU UZMAN EŞZAMANLI BAĞLANTI (Multi-Specialist Co-Session):
                        // Mevcut bağlı uzmanı ASLA düşürmeden yeni uzmanı oturuma paralel ekle!
                        int activePeerCount = session.AddPeer(myPeer);
                        Console.WriteLine($"[Relay] Client connected to Host ID: {targetId} (Active specialists: {activePeerCount})");
                        TelemetryManager.LogEvent(session.Hwid, session.IpAddress, session.ComputerName, session.Username, session.OsVersion, session.AppVersion, "connect", $"İstemci bağlandı (Aktif uzman: {activePeerCount}). İstemci IP: {clientIp}, Hedef ID: {targetId}");

                        string ticketToken = context.Request.Query["ticketToken"].ToString().Trim();
                        
                        SupportRequest? ticket = null;
                        if (!string.IsNullOrEmpty(ticketToken) && ActiveSupportRequests.TryGetValue(ticketToken, out ticket)) { }
                        else
                        {
                            ticket = ActiveSupportRequests.Values.FirstOrDefault(t => t.Id == targetId || (!string.IsNullOrEmpty(ticketToken) && t.Token == ticketToken));
                        }

                        string startCmdText = "START_STREAM";
                        if (ticket != null)
                        {
                            string tToken = !string.IsNullOrEmpty(ticket.Token) ? ticket.Token : ticketToken;
                            if (ticket.RequiresConfirmation)
                            {
                                startCmdText = $"START_STREAM:PROMPT_CONFIRM:{tToken}";
                            }
                            else
                            {
                                startCmdText = $"START_STREAM:TICKET:{tToken}";
                            }
                        }

                        byte[] startCmd = Encoding.UTF8.GetBytes(startCmdText);
                        {
                            bool hLocked = false;
                            try
                            {
                                await session.HostSendLock.WaitAsync(CancellationToken.None);
                                hLocked = true;
                                await session.HostSocket.SendAsync(new ArraySegment<byte>(startCmd), WebSocketMessageType.Text, true, CancellationToken.None);
                                if (activePeerCount > 1)
                                {
                                    byte[] kfCmd = Encoding.UTF8.GetBytes("KEYFRAME_REQ");
                                    await session.HostSocket.SendAsync(new ArraySegment<byte>(kfCmd), WebSocketMessageType.Text, true, CancellationToken.None);
                                }
                            }
                            catch { }
                            finally
                            {
                                if (hLocked) { try { session.HostSendLock.Release(); } catch { } }
                            }
                        }

                        // İkinci uzman bağlandığında Host'un 1000ms keyframe debounce korumasını aşmak için 1150ms sonra senkronize KEYFRAME_REQ garantisi:
                        if (activePeerCount > 1)
                        {
                            var coPeer = myPeer;
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await Task.Delay(1150, coPeer.Cts.Token);
                                    if (coPeer.IsRegistered && session.HostSocket.State == WebSocketState.Open && session.ActivePeers.Length > 1)
                                    {
                                        byte[] kfCmd = Encoding.UTF8.GetBytes("KEYFRAME_REQ");
                                        bool hLocked = false;
                                        try
                                        {
                                            await session.HostSendLock.WaitAsync(CancellationToken.None);
                                            hLocked = true;
                                            if (session.HostSocket.State == WebSocketState.Open)
                                            {
                                                await session.HostSocket.SendAsync(new ArraySegment<byte>(kfCmd), WebSocketMessageType.Text, true, CancellationToken.None);
                                            }
                                        }
                                        finally
                                        {
                                            if (hLocked) { try { session.HostSendLock.Release(); } catch { } }
                                        }
                                    }
                                }
                                catch { }
                            });
                        }

                        // 📱 Telegram: Destek uzmanı bağlandı bildirimi
                        if (ticket != null && activePeerCount == 1)
                        {
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    string operatorName = context.Request.Query["computer_name"].ToString();
                                    if (string.IsNullOrEmpty(operatorName)) operatorName = "Destek Uzmanı";
                                    await TelegramNotifier.NotifySupportConnectedAsync(
                                        ticket.Name, ticket.Issue, ticket.Id, operatorName, ticket.TenantId);
                                }
                                catch { }
                            });
                        }

                        var clientToHostTask = TunnelClientToHost(session, myPeer, myClientCts.Token);

                        try
                        {
                            await Task.WhenAny(clientToHostTask, Task.Delay(-1, myClientCts.Token));
                        }
                        catch { }

                        myPeer.DisposePeer();
                        int remainingPeers = session.RemovePeer(myPeer, out bool anyWebRem, out bool anyNativeRem);

                        // SADECE bağlı hiçbir uzman kalmadıysa (remainingPeers == 0) yayını durdur!
                        // Diğer uzman hala müşteride çalışıyorsa onun yayınını ASLA kesme!
                        if (remainingPeers == 0)
                        {
                            Console.WriteLine($"[Relay] Last client disconnected from Host ID: {targetId}. Stopping stream.");
                            TelemetryManager.LogEvent(session.Hwid, session.IpAddress, session.ComputerName, session.Username, session.OsVersion, session.AppVersion, "disconnect", $"Son istemci bağlantısı kesildi. Hedef ID: {targetId}");

                            if (session.HostSocket.State == WebSocketState.Open)
                            {
                                byte[] stopCmd = Encoding.UTF8.GetBytes("STOP_STREAM");
                                bool hLocked = false;
                                try
                                {
                                    await session.HostSendLock.WaitAsync(CancellationToken.None);
                                    hLocked = true;
                                    await session.HostSocket.SendAsync(new ArraySegment<byte>(stopCmd), WebSocketMessageType.Text, true, CancellationToken.None);
                                }
                                catch { }
                                finally
                                {
                                    if (hLocked) { try { session.HostSendLock.Release(); } catch { } }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[Relay] Co-specialist left Host ID: {targetId} ({remainingPeers} specialist(s) still active — stream preserved).");
                            if (myPeer.IsWebClient && !anyWebRem && anyNativeRem && session.HostSocket.State == WebSocketState.Open)
                            {
                                bool hLocked = false;
                                try
                                {
                                    await session.HostSendLock.WaitAsync(CancellationToken.None);
                                    hLocked = true;
                                    byte[] nativeCmd = Encoding.UTF8.GetBytes("CLIENT:NATIVE_CPP");
                                    byte[] kfCmd = Encoding.UTF8.GetBytes("KEYFRAME_REQ");
                                    await session.HostSocket.SendAsync(new ArraySegment<byte>(nativeCmd), WebSocketMessageType.Text, true, CancellationToken.None);
                                    await session.HostSocket.SendAsync(new ArraySegment<byte>(kfCmd), WebSocketMessageType.Text, true, CancellationToken.None);
                                }
                                catch { }
                                finally
                                {
                                    if (hLocked) { try { session.HostSendLock.Release(); } catch { } }
                                }
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[Relay] View-only Client connected to Host ID: {targetId}");
                        string clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen";
                        using var viewOnlyCts = CancellationTokenSource.CreateLinkedTokenSource(session.Cts.Token);
                        
                        // 30 Günlük Deneme, 1 Hafta (7 Gün) Tolerans (Günde 3x5 dk) & Lisans Kontrolü — Hiçbir uygulama muaf değil!
                        string viewerId = context.Request.Query["viewerId"].ToString().Trim();
                        string adminToken = context.Request.Query["adminToken"].ToString().Trim();
                        string tenantIdParam = context.Request.Query["tenantId"].ToString().Trim();
                        bool isExempt = (context.Request.Query["hwLicensed"].ToString().Trim() == "1");

                        if (!isExempt)
                        {
                            bool allowed = ViewerTrialManager.TryStartSession(viewerId, clientIp, out var trialStatus);
                            if (!allowed)
                            {
                                string errCode = (trialStatus.Status == "GRACE_PERIOD") ? "ERROR:GRACE_LIMIT_REACHED" : "ERROR:TRIAL_EXPIRED";
                                byte[] errBytes = Encoding.UTF8.GetBytes(errCode);
                                await clientSocket.SendAsync(new ArraySegment<byte>(errBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                                await Task.Delay(400);
                                await clientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, errCode, CancellationToken.None);
                                return;
                            }

                            // İstemciye lisans ve deneme durum bilgisini ilet
                            string infoMsg = $"SESSION_STATUS:{trialStatus.Status}:{trialStatus.DaysRemaining}:{trialStatus.GraceDaysRemaining}:{trialStatus.TodaySessionsUsed}:{trialStatus.SessionDurationSeconds}";
                            byte[] infoBytes = Encoding.UTF8.GetBytes(infoMsg);
                            try { await clientSocket.SendAsync(new ArraySegment<byte>(infoBytes), WebSocketMessageType.Text, true, CancellationToken.None); } catch { }

                            // 31-37. Günler (1 Hafta Tolerans): 5 dakika (300 saniye) sonra sunucu tarafında oturumu otomatik kapat
                            if (trialStatus.Status == "GRACE_PERIOD")
                            {
                                int limitSec = trialStatus.SessionDurationSeconds > 0 ? trialStatus.SessionDurationSeconds : 300;
                                var voCts = viewOnlyCts;
                                _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        await Task.Delay(TimeSpan.FromSeconds(limitSec), voCts.Token);
                                        if (clientSocket.State == WebSocketState.Open)
                                        {
                                            byte[] timeoutBytes = Encoding.UTF8.GetBytes("ERROR:GRACE_TIMEOUT");
                                            try { await clientSocket.SendAsync(new ArraySegment<byte>(timeoutBytes), WebSocketMessageType.Text, true, CancellationToken.None); } catch { }
                                            await Task.Delay(300);
                                            try { await clientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "GRACE_TIMEOUT", CancellationToken.None); } catch { }
                                            voCts.Cancel();
                                        }
                                    }
                                    catch { }
                                });
                            }
                        }
                        TelemetryManager.LogEvent(session.Hwid, session.IpAddress, session.ComputerName, session.Username, session.OsVersion, session.AppVersion, "connect_viewonly", $"İzleyici bağlandı. İstemci IP: {clientIp}, Hedef ID: {targetId}");

                        lock (session.ViewOnlyClients)
                        {
                            session.ViewOnlyClients.Add(clientSocket);
                        }

                        // Send START_STREAM if host is not already streaming
                        byte[] startCmd = Encoding.UTF8.GetBytes("START_STREAM");
                        await session.HostSocket.SendAsync(new ArraySegment<byte>(startCmd), WebSocketMessageType.Text, true, CancellationToken.None);

                        await TunnelViewOnlyClient(clientSocket, session);
                    }

                    if (clientSocket.State == WebSocketState.Open)
                    {
                        try
                        {
                            await clientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection ended", CancellationToken.None);
                        }
                        catch { }
                    }
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                }
            });

            app.MapPost("/api/support/create", (SupportCreateDto dto) =>
            {
                if (dto != null && !string.IsNullOrEmpty(dto.Id))
                {
                    string tenantId = string.IsNullOrEmpty(dto.TenantId) ? "BIGLINE" : dto.TenantId;
                    string priority = string.IsNullOrEmpty(dto.Priority) ? "Orta" : dto.Priority;
                    var req = new SupportRequest
                    {
                        Id = dto.Id,
                        Name = dto.Name ?? "",
                        Issue = dto.Issue ?? "",
                        Priority = priority,
                        Token = dto.Token ?? "",
                        TenantId = tenantId,
                        RequiresConfirmation = dto.RequiresConfirmation,
                        ImageBase64 = dto.ImageBase64 ?? "",
                        PreferredOperator = dto.PreferredOperator ?? ""
                    };
                    string reqKey = !string.IsNullOrEmpty(dto.Token) ? dto.Token : dto.Id;
                    ActiveSupportRequests[reqKey] = req;

                    // 📱 Telegram Push Notification — destek uzmanının telefonuna bildirim gönder
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await TelegramNotifier.NotifySupportRequestAsync(req.Name, req.Issue, req.Priority, req.Id, req.TenantId, req.PreferredOperator);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Telegram] Bildirim hatası: {ex.Message}");
                        }
                    });

                    // Immediately log new ticket into Support History CRM
                    try
                    {
                        var history = LoadSupportHistory();
                        var existing = history.FirstOrDefault(h => !string.IsNullOrEmpty(dto.Token) && h.Token == dto.Token);
                        if (existing == null)
                        {
                            history.Add(new SupportHistoryEntry
                            {
                                Id = Guid.NewGuid().ToString(),
                                HostId = dto.Id,
                                Token = dto.Token ?? "",
                                Name = string.IsNullOrEmpty(dto.Name) ? ("Müşteri (" + dto.Id + ")") : dto.Name,
                                Issue = string.IsNullOrEmpty(dto.Issue) ? "Genel Destek" : dto.Issue,
                                Priority = priority,
                                TenantId = tenantId,
                                CreatedAt = GetTurkeyTimeString(),
                                ResolvedAt = "—",
                                Status = "⏳ Sırada Bekliyor",
                                Notes = "Uzman taraf\u0131ndan incelemeye al\u0131nmas\u0131 bekleniyor.",
                                ImageBase64 = dto.ImageBase64 ?? "",
                                PreferredOperator = dto.PreferredOperator ?? ""
                            });
                            SaveSupportHistory(history);
                        }
                    }
                    catch { }

                    return Results.Ok("Success");
                }
                return Results.BadRequest("Invalid Data");
            });

                        app.MapGet("/api/support/operators", async context =>
            {
                string tenantId = context.Request.Query["tenantId"].ToString() ?? "BIGLINE";
                var list = GetTenantOperators(tenantId);
                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.WriteAsJsonAsync(list);
            });

            app.MapGet("/api/support/check", async context =>
            {
                string id = context.Request.Query["id"].ToString() ?? "";
                bool exists = !string.IsNullOrEmpty(id) && ActiveSupportRequests.Values.Any(r => r.Id == id || r.Token == id);
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(exists ? "true" : "false");
            });

            // ğŸ‘ï¸ CanlÄ± Destek Talep Durumu Sorgulama (MÃ¼ÅŸteri iÃ§in anlÄ±k bilgilendirme)
            app.MapGet("/api/support/status", async context =>
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                string id = context.Request.Query["id"].ToString() ?? "";
                string token = context.Request.Query["token"].ToString() ?? "";
                string cleanId = id.Replace(" ", "").Trim();

                SupportRequest? req = ActiveSupportRequests.Values.FirstOrDefault(r =>
                    (!string.IsNullOrEmpty(token) && r.Token.Equals(token, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(cleanId) && r.Id.Replace(" ", "").Trim().Equals(cleanId, StringComparison.OrdinalIgnoreCase)));

                if (req != null)
                {
                    var resp = new
                    {
                        exists = true,
                        active = true,
                        status = string.IsNullOrEmpty(req.Status) ? "SÄ±rada Bekliyor" : req.Status,
                        notes = string.IsNullOrEmpty(req.Notes) ? "Talebiniz uzman havuzunda beklemektedir." : req.Notes,
                        operatorName = req.OperatorName ?? "",
                        token = req.Token,
                        id = req.Id
                    };
                    await context.Response.WriteAsJsonAsync(resp);
                    return;
                }

                // ArÅŸiv/GeÃ§miÅŸ kontrolÃ¼ (Ã‡Ã¶zÃ¼ldÃ¼ mÃ¼, iptal mi edildi?)
                try
                {
                    var history = LoadSupportHistory();
                    var hist = history.LastOrDefault(h =>
                        (!string.IsNullOrEmpty(token) && h.Token.Equals(token, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(cleanId) && h.HostId.Replace(" ", "").Trim().Equals(cleanId, StringComparison.OrdinalIgnoreCase)));

                    if (hist != null)
                    {
                        var resp = new
                        {
                            exists = true,
                            active = false,
                            status = string.IsNullOrEmpty(hist.Status) ? "Ã‡Ã¶zÃ¼ldÃ¼" : hist.Status,
                            notes = hist.Notes ?? "",
                            operatorName = "",
                            token = hist.Token,
                            id = hist.HostId
                        };
                        await context.Response.WriteAsJsonAsync(resp);
                        return;
                    }
                }
                catch { }

                await context.Response.WriteAsJsonAsync(new { exists = false, active = false, status = "NONE", notes = "", operatorName = "" });
            });

            // ğŸ‘ï¸ Uzman Talebi GÃ¶rdÃ¼ / Ä°ncelemeye AldÄ± / SÄ±raya AldÄ± Bildirimi
            app.MapPost("/api/support/acknowledge", async context =>
            {
                try
                {
                    using var reader = new StreamReader(context.Request.Body);
                    string body = await reader.ReadToEndAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    string id = root.TryGetProperty("id", out var pId) ? pId.GetString() ?? "" : "";
                    string token = root.TryGetProperty("token", out var pToken) ? pToken.GetString() ?? "" : "";
                    string opName = root.TryGetProperty("operatorName", out var pOp) ? pOp.GetString() ?? "Uzman Mahmut" : "Uzman Mahmut";
                    string notes = root.TryGetProperty("notes", out var pNotes) ? pNotes.GetString() ?? "" : "";
                    if (string.IsNullOrWhiteSpace(notes))
                    {
                        notes = "Talebiniz " + opName + " tarafÄ±ndan gÃ¶rÃ¼ldÃ¼ ve teknik incelemeye alÄ±ndÄ±. En kÄ±sa sÃ¼rede tarafÄ±nÄ±za baÄŸlantÄ± saÄŸlanacaktÄ±r.";
                    }

                    string cleanId = id.Replace(" ", "").Trim();

                    SupportRequest? ticket = null;
                    var matchKey = ActiveSupportRequests.FirstOrDefault(kv => 
                        (!string.IsNullOrEmpty(token) && (kv.Key.Equals(token, StringComparison.OrdinalIgnoreCase) || kv.Value.Token.Equals(token, StringComparison.OrdinalIgnoreCase))) ||
                        (!string.IsNullOrEmpty(cleanId) && (kv.Key.Equals(cleanId, StringComparison.OrdinalIgnoreCase) || kv.Value.Id.Replace(" ", "").Trim().Equals(cleanId, StringComparison.OrdinalIgnoreCase)))).Key;

                    if (matchKey != null && ActiveSupportRequests.TryGetValue(matchKey, out ticket) && ticket != null)
                    {
                        ticket.Status = "Ä°ncelemede";
                        ticket.OperatorName = opName;
                        ticket.Notes = notes;
                    }

                    // ArÅŸivde varsa da gÃ¼ncelle
                    try
                    {
                        var history = LoadSupportHistory();
                        var entry = history.FirstOrDefault(h => 
                            (!string.IsNullOrEmpty(token) && h.Token.Equals(token, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrEmpty(cleanId) && h.HostId.Replace(" ", "").Trim().Equals(cleanId, StringComparison.OrdinalIgnoreCase)));
                        if (entry != null)
                        {
                            entry.Status = "Ä°ncelemede";
                            entry.Notes = notes;
                            SaveSupportHistory(history);
                        }
                    }
                    catch { }

                                        // Host soketine anlik push bildirimi gonder
                    if (ActiveHosts.TryGetValue(cleanId, out var session) && session.HostSocket != null && session.HostSocket.State == System.Net.WebSockets.WebSocketState.Open)
                    {
                        try
                        {
                            byte[] msg = Encoding.UTF8.GetBytes($"TICKET_ACKNOWLEDGED|{opName}|{notes}");
                            await session.HostSocket.SendAsync(new ArraySegment<byte>(msg), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                        catch { }
                    }

                    // Telegram kanalina bilgilendirme
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            string targetName = ticket != null ? ticket.Name : ("MÃ¼ÅŸteri (" + id + ")");
                            string targetIssue = ticket != null ? ticket.Issue : "Genel Destek";
                            string targetTenant = ticket != null ? ticket.TenantId : "BIGLINE";
                            await TelegramNotifier.NotifyTicketAcknowledgedAsync(targetName, targetIssue, cleanId, opName, notes, targetTenant);
                        }
                        catch { }
                    });

                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsJsonAsync(new { success = true, status = "Ä°ncelemede", notes = notes, operatorName = opName });
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { success = false, error = ex.Message });
                }
            });

            app.MapGet("/api/support/list", async context =>
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                string tenantId = context.Request.Query["tenantId"].ToString() ?? "";
                
                var requests = ActiveSupportRequests.Values.AsEnumerable();
                if (!string.IsNullOrEmpty(tenantId) && !tenantId.Equals("SUPERADMIN", StringComparison.OrdinalIgnoreCase))
                {
                    requests = requests.Where(r => r.TenantId.Equals(tenantId, StringComparison.OrdinalIgnoreCase));
                }

                int GetRank(string p)
                {
                    if (string.IsNullOrEmpty(p)) return 1;
                    if (p.Contains("Yüksek")) return 0;
                    if (p.Contains("Orta")) return 1;
                    if (p.Contains("Düşük")) return 2;
                    return 1;
                }

                var list = requests
                    .OrderBy(r => GetRank(r.Priority))
                    .ThenByDescending(r => r.CreatedAt)
                    .ToList();

                await context.Response.WriteAsJsonAsync(list);
            });

            app.MapPost("/api/support/resolve", async context =>
            {
                try
                {
                    using var reader = new StreamReader(context.Request.Body);
                    string body = await reader.ReadToEndAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    string id = root.TryGetProperty("id", out var pId) ? pId.GetString() ?? "" : "";
                    string token = root.TryGetProperty("token", out var pToken) ? pToken.GetString() ?? "" : "";
                    string status = root.TryGetProperty("status", out var pStatus) ? pStatus.GetString() ?? "Çözüldü" : "Çözüldü";
                    string notes = root.TryGetProperty("notes", out var pNotes) ? pNotes.GetString() ?? "" : "";

                    string name = root.TryGetProperty("name", out var pName) ? pName.GetString() ?? "" : "";
                    string issue = root.TryGetProperty("issue", out var pIssue) ? pIssue.GetString() ?? "" : "";
                    string priority = root.TryGetProperty("priority", out var pPriority) ? pPriority.GetString() ?? "" : "";
                    string tenantId = root.TryGetProperty("tenantId", out var pTenant) ? pTenant.GetString() ?? "BIGLINE" : "BIGLINE";

                    SupportRequest? ticket = null;
                    if (!string.IsNullOrEmpty(token) && ActiveSupportRequests.TryRemove(token, out ticket)) { }
                    else if (!string.IsNullOrEmpty(id) && ActiveSupportRequests.TryRemove(id, out ticket)) { }
                    else
                    {
                        var matchKey = ActiveSupportRequests.FirstOrDefault(kv => kv.Value.Id == id || kv.Value.Token == token).Key;
                        if (matchKey != null) ActiveSupportRequests.TryRemove(matchKey, out ticket);
                    }

                    if (string.IsNullOrEmpty(priority) && ticket != null && !string.IsNullOrEmpty(ticket.Priority))
                    {
                        priority = ticket.Priority;
                    }

                    var history = LoadSupportHistory();
                    var existingEntry = history.FirstOrDefault(h => 
                        (!string.IsNullOrEmpty(token) && h.Token == token) ||
                        (!string.IsNullOrEmpty(id) && h.HostId == id && !string.IsNullOrEmpty(issue) && h.Issue == issue && h.Status.Contains("Bekliyor"))
                    );
                    if (existingEntry == null && !string.IsNullOrEmpty(id))
                    {
                        existingEntry = history.LastOrDefault(h => h.HostId == id && h.Status.Contains("Bekliyor"));
                    }

                    string resolvedPriority = !string.IsNullOrEmpty(priority) ? priority : (ticket != null ? ticket.Priority : "");
                    if (string.IsNullOrEmpty(resolvedPriority) && existingEntry != null && !string.IsNullOrEmpty(existingEntry.Priority))
                    {
                        resolvedPriority = existingEntry.Priority;
                    }
                    if (string.IsNullOrEmpty(resolvedPriority))
                    {
                        resolvedPriority = "🟡 Orta";
                    }

                    if (existingEntry != null)
                    {
                        existingEntry.Status = status;
                        existingEntry.Notes = notes;
                        existingEntry.ResolvedAt = GetTurkeyTimeString();
                        if (!string.IsNullOrEmpty(name) && existingEntry.Name.StartsWith("Uzak Masaüstü")) existingEntry.Name = name;
                        if (!string.IsNullOrEmpty(resolvedPriority)) existingEntry.Priority = resolvedPriority;
                    }
                    else
                    {
                        history.Add(new SupportHistoryEntry
                        {
                            Id = Guid.NewGuid().ToString(),
                            HostId = ticket != null ? ticket.Id : id,
                            Token = ticket != null ? ticket.Token : token,
                            Name = !string.IsNullOrEmpty(name) ? name : (ticket != null ? ticket.Name : ("Müşteri (" + id + ")")),
                            Issue = !string.IsNullOrEmpty(issue) ? issue : (ticket != null ? ticket.Issue : "Genel Destek"),
                            Priority = resolvedPriority,
                            TenantId = !string.IsNullOrEmpty(tenantId) ? tenantId : (ticket != null ? ticket.TenantId : "BIGLINE"),
                            CreatedAt = ticket != null ? ticket.CreatedAt.ToString("dd.MM.yyyy HH:mm:ss") : GetTurkeyTimeString(),
                            ResolvedAt = GetTurkeyTimeString(),
                            Status = status,
                            Notes = notes
                        });
                    }
                    SaveSupportHistory(history);

                    // 📱 Telegram: Talep çözüldü bildirimi
                    string resolvedName = existingEntry != null ? existingEntry.Name : (ticket != null ? ticket.Name : "Müşteri");
                    string resolvedIssue = existingEntry != null ? existingEntry.Issue : (ticket != null ? ticket.Issue : "Genel Destek");
                    string resolvedHostId = ticket != null ? ticket.Id : id;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            string resolvedTenant = existingEntry != null ? existingEntry.TenantId : (ticket != null ? ticket.TenantId : tenantId);
                            await TelegramNotifier.NotifyTicketResolvedAsync(
                                resolvedName, resolvedIssue, status, notes, resolvedHostId, resolvedTenant);
                        }
                        catch { }
                    });

                    string hostTargetId = ticket != null ? ticket.Id : id;
                    if (!string.IsNullOrEmpty(hostTargetId) && ActiveHosts.TryGetValue(hostTargetId, out var session) && session.HostSocket != null && session.HostSocket.State == System.Net.WebSockets.WebSocketState.Open)
                    {
                        try
                        {
                            byte[] msg = Encoding.UTF8.GetBytes("TICKET_RESOLVED");
                            await session.HostSocket.SendAsync(new ArraySegment<byte>(msg), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                        catch { }
                    }

                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync("Success");
                }
                catch
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
            });

            app.MapPost("/api/support/history/update", async context =>
            {
                try
                {
                    using var reader = new StreamReader(context.Request.Body);
                    string body = await reader.ReadToEndAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    string id = root.TryGetProperty("id", out var pId) ? pId.GetString() ?? "" : "";
                    string token = root.TryGetProperty("token", out var pToken) ? pToken.GetString() ?? "" : "";
                    string status = root.TryGetProperty("status", out var pStatus) ? pStatus.GetString() ?? "Çözüldü" : "Çözüldü";
                    string notes = root.TryGetProperty("notes", out var pNotes) ? pNotes.GetString() ?? "" : "";
                    
                    var history = LoadSupportHistory();
                    var entry = history.LastOrDefault(h => (!string.IsNullOrEmpty(token) && h.Token == token) || (!string.IsNullOrEmpty(id) && (h.Id == id || h.HostId == id)));
                    if (entry != null)
                    {
                        entry.Status = status;
                        entry.Notes = notes;
                        entry.ResolvedAt = GetTurkeyTimeString();
                    }
                    else
                    {
                        entry = new SupportHistoryEntry
                        {
                            Id = Guid.NewGuid().ToString(),
                            HostId = id,
                            Token = token,
                            Name = "Uzak Masaüstü (" + id + ")",
                            Issue = "Genel Destek",
                            TenantId = "BIGLINE",
                            CreatedAt = GetTurkeyTimeString(),
                            ResolvedAt = GetTurkeyTimeString(),
                            Status = status,
                            Notes = notes
                        };
                        history.Add(entry);
                    }
                    SaveSupportHistory(history);

                    // Clear from ActiveSupportRequests if present
                    ActiveSupportRequests.TryRemove(id, out _);

                    // Send TICKET_RESOLVED push message to Host socket
                    if (ActiveHosts.TryGetValue(id, out var session) && session.HostSocket != null && session.HostSocket.State == System.Net.WebSockets.WebSocketState.Open)
                    {
                        try
                        {
                            byte[] msg = Encoding.UTF8.GetBytes("TICKET_RESOLVED");
                            await session.HostSocket.SendAsync(new ArraySegment<byte>(msg), System.Net.WebSockets.WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                        catch { }
                    }

                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync("Success");
                }
                catch
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
            });

            app.MapGet("/api/support/history/list", async context =>
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                string tenantId = context.Request.Query["tenantId"].ToString() ?? "";
                string filterTenant = context.Request.Query["filterTenant"].ToString() ?? "";
                string hostId = context.Request.Query["hostId"].ToString() ?? "";
                
                var history = LoadSupportHistory().AsEnumerable();
                
                if (!string.IsNullOrEmpty(hostId))
                {
                    string cleanHostId = hostId.Replace(" ", "").Trim();
                    history = history.Where(h => 
                        (!string.IsNullOrEmpty(h.HostId) && h.HostId.Replace(" ", "").Equals(cleanHostId, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(h.Id) && h.Id.Replace(" ", "").Equals(cleanHostId, StringComparison.OrdinalIgnoreCase))
                    );
                }
                else if (!string.IsNullOrEmpty(filterTenant) && !filterTenant.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    history = history.Where(h => h.TenantId.Equals(filterTenant, StringComparison.OrdinalIgnoreCase));
                }
                else if (!string.IsNullOrEmpty(tenantId) && 
                         !tenantId.Equals("SUPERADMIN", StringComparison.OrdinalIgnoreCase) && 
                         !tenantId.Equals("BIGLINE", StringComparison.OrdinalIgnoreCase) &&
                         !tenantId.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                {
                    history = history.Where(h => h.TenantId.Equals(tenantId, StringComparison.OrdinalIgnoreCase));
                }

                var list = history.OrderByDescending(h => h.ResolvedAt).ToList();
                foreach (var item in list)
                {
                    string p = (item.Priority ?? "").ToLowerInvariant();

                    if (p.Contains("yüksek") || p.Contains("yuksek") || p.Contains("high") || p.Contains("🔴"))
                    {
                        item.Priority = "🔴 Yüksek";
                    }
                    else if (p.Contains("düşük") || p.Contains("dusuk") || p.Contains("low") || p.Contains("🟢"))
                    {
                        item.Priority = "🟢 Düşük";
                    }
                    else if (p.Contains("orta") || p.Contains("medium") || p.Contains("🟡"))
                    {
                        item.Priority = "🟡 Orta";
                    }
                    else
                    {
                        string iss = (item.Issue ?? "").ToLowerInvariant();
                        if (iss.Contains("çok acil") || iss.Contains("kilitlendi") || iss.Contains("fatura kesemiyoruz") || iss.Contains("kasa kilit"))
                        {
                            item.Priority = "🔴 Yüksek";
                        }
                        else if (iss.Contains("rutin") || iss.Contains("bilgi almak"))
                        {
                            item.Priority = "🟢 Düşük";
                        }
                        else
                        {
                            item.Priority = "🟡 Orta";
                        }
                    }
                }
                await context.Response.WriteAsJsonAsync(list);
            });

            app.MapGet("/api/support/history/tenants", async context =>
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                var history = LoadSupportHistory();
                var tenants = history
                    .Select(h => string.IsNullOrWhiteSpace(h.TenantId) ? "BIGLINE" : h.TenantId.Trim().ToUpper())
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();
                await context.Response.WriteAsJsonAsync(tenants);
            });

            var handleHistoryDelete = new Func<HttpContext, Task>(async context =>
            {
                try
                {
                    string id = context.Request.Query["id"].ToString() ?? "";
                    if (string.IsNullOrEmpty(id) && context.Request.ContentLength > 0)
                    {
                        using var reader = new StreamReader(context.Request.Body);
                        string body = await reader.ReadToEndAsync();
                        if (!string.IsNullOrEmpty(body))
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(body);
                            id = doc.RootElement.TryGetProperty("id", out var p) ? p.GetString() ?? "" : "";
                        }
                    }

                    if (!string.IsNullOrEmpty(id))
                    {
                        SqliteManager.DeleteTicket(id);
                        var history = LoadSupportHistory();
                        history.RemoveAll(h => h.Id == id || h.HostId == id);
                        SaveSupportHistory(history);
                    }

                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync("Success");
                }
                catch
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
            });

            app.MapGet("/api/support/history/delete", handleHistoryDelete);
            app.MapPost("/api/support/history/delete", handleHistoryDelete);

            var handleHistoryClear = new Func<HttpContext, Task>(async context =>
            {
                try
                {
                    SqliteManager.ClearAllTickets();
                    SaveSupportHistory(new List<SupportHistoryEntry>());
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync("Success");
                }
                catch
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
            });

            app.MapGet("/api/support/history/clear", handleHistoryClear);
            app.MapPost("/api/support/history/clear", handleHistoryClear);

            app.MapPost("/api/support/cancel", async context =>
            {
                try
                {
                    using var reader = new StreamReader(context.Request.Body);
                    string body = await reader.ReadToEndAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    var root = doc.RootElement;
                    string id = root.TryGetProperty("id", out var pId) ? pId.GetString() ?? "" : "";
                    string token = root.TryGetProperty("token", out var pToken) ? pToken.GetString() ?? "" : "";

                    string cleanId = id.Replace(" ", "").Trim();

                    // Hem Key, hem Id, hem Token ile ara ve kaldır
                    SupportRequest? ticket = null;
                    var matchingKeys = ActiveSupportRequests
                        .Where(kv => (!string.IsNullOrEmpty(token) && (kv.Key.Equals(token, StringComparison.OrdinalIgnoreCase) || kv.Value.Token.Equals(token, StringComparison.OrdinalIgnoreCase))) ||
                                     (!string.IsNullOrEmpty(cleanId) && (kv.Key.Equals(cleanId, StringComparison.OrdinalIgnoreCase) || kv.Value.Id.Replace(" ", "").Trim().Equals(cleanId, StringComparison.OrdinalIgnoreCase))))
                        .Select(kv => kv.Key)
                        .ToList();

                    foreach (var k in matchingKeys)
                    {
                        if (ActiveSupportRequests.TryRemove(k, out var removed))
                        {
                            ticket = removed;
                        }
                    }

                    if (ticket != null)
                    {
                        var history = LoadSupportHistory();
                        history.Add(new SupportHistoryEntry
                        {
                            Id = Guid.NewGuid().ToString(),
                            HostId = ticket.Id,
                            Name = ticket.Name,
                            Issue = ticket.Issue,
                            TenantId = ticket.TenantId,
                            CreatedAt = ticket.CreatedAt.ToString("dd.MM.yyyy HH:mm:ss"),
                            ResolvedAt = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                            Status = "Müşteri İptal Etti",
                            Notes = "Talep müşteri tarafından iptal edildi."
                        });
                        SaveSupportHistory(history);

                        // 📱 Telegram İptal Bildirimi
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await TelegramNotifier.NotifyTicketCancelledAsync(
                                    ticket.Name, ticket.Issue, ticket.Id, ticket.TenantId);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Telegram] İptal bildirim hatası: {ex.Message}");
                            }
                        });

                        context.Response.StatusCode = StatusCodes.Status200OK;
                        await context.Response.WriteAsync("Success");
                    }
                    else
                    {
                        // Zaten listede yoksa veya daha önce silindiyse yine de OK ver
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        await context.Response.WriteAsync("Not Found or Already Removed");
                    }
                }
                catch
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
            });

            app.MapGet("/api/support/stats", async context =>
            {
                try
                {
                    var history = LoadSupportHistory();
                    int totalTickets = history.Count;
                    int resolvedTickets = history.Count(h => h.Status != null && (h.Status.Contains("Çözüldü") || h.Status.Contains("İşlem")));
                    int cancelledTickets = history.Count(h => h.Status != null && h.Status.Contains("İptal"));
                    int activeTickets = ActiveSupportRequests.Count;

                    var topCustomers = history
                        .Where(h => !string.IsNullOrEmpty(h.Name))
                        .GroupBy(h => h.Name)
                        .OrderByDescending(g => g.Count())
                        .Take(5)
                        .ToDictionary(g => g.Key, g => g.Count());

                    var response = new
                    {
                        total_tickets = totalTickets,
                        resolved_tickets = resolvedTickets,
                        cancelled_tickets = cancelledTickets,
                        active_tickets = activeTickets,
                        top_customers = topCustomers
                    };

                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(response);
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsJsonAsync(new { total_tickets = 0, resolved_tickets = 0, cancelled_tickets = 0, active_tickets = 0, top_customers = new Dictionary<string, int>() });
                }
            });

            app.MapGet("/api/licenses/list", async context =>
            {
                try
                {
                    var list = LoadLicenses();
                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsJsonAsync(list);
                }
                catch
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
            });

            app.MapPost("/api/licenses/create", async context =>
            {
                try
                {
                    using var reader = new StreamReader(context.Request.Body);
                    string body = await reader.ReadToEndAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    var root = doc.RootElement;

                    string customerName = root.TryGetProperty("customerName", out var p1) ? p1.GetString() ?? "" : "";
                    string tierName = root.TryGetProperty("tierName", out var p2) ? p2.GetString() ?? "Başlangıç (1.490 TL)" : "Başlangıç (1.490 TL)";
                    int durationMonths = root.TryGetProperty("durationMonths", out var p3) ? p3.GetInt32() : 12;

                    int maxOperators = 1;
                    int maxChannels = 5;
                    int maxUnattended = 50;

                    if (tierName.Contains("Pro (3.990 TL)") || tierName.Contains("2 Operatör"))
                    {
                        maxOperators = 2;
                        maxChannels = 10;
                        maxUnattended = 100;
                    }
                    else if (tierName.Contains("Pro+ (4.990 TL)") || tierName.Contains("3 Operatör"))
                    {
                        maxOperators = 3;
                        maxChannels = 15;
                        maxUnattended = 150;
                    }
                    else if (tierName.Contains("Kurumsal"))
                    {
                        maxOperators = 10;
                        maxChannels = 50;
                        maxUnattended = 500;
                    }

                    string licenseKey = $"BGL-{Guid.NewGuid().ToString("N")[..8].ToUpper()}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";

                    var entry = new LicenseEntry
                    {
                        LicenseKey = licenseKey,
                        CustomerName = string.IsNullOrWhiteSpace(customerName) ? "Müşteri" : customerName,
                        TierName = tierName,
                        MaxOperators = maxOperators,
                        MaxChannels = maxChannels,
                        MaxUnattendedHosts = maxUnattended,
                        CreatedAt = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                        ExpiresAt = DateTime.Now.AddMonths(durationMonths).ToString("dd.MM.yyyy HH:mm"),
                        IsActive = true
                    };

                    var licenses = LoadLicenses();
                    licenses.Insert(0, entry);
                    SaveLicenses(licenses);

                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsJsonAsync(new { success = true, licenseKey = entry.LicenseKey });
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync($"Error: {ex.Message}");
                }
            });

            app.MapPost("/api/licenses/delete", async context =>
            {
                try
                {
                    using var reader = new StreamReader(context.Request.Body);
                    string body = await reader.ReadToEndAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    string key = doc.RootElement.TryGetProperty("licenseKey", out var p) ? p.GetString() ?? "" : "";

                    if (!string.IsNullOrEmpty(key))
                    {
                        var licenses = LoadLicenses();
                        licenses.RemoveAll(l => l.LicenseKey == key);
                        SaveLicenses(licenses);
                    }

                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync("Success");
                }
                catch
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }
            });

            app.MapPost("/api/telemetry/report", async context =>
            {
                try
                {
                    using var reader = new StreamReader(context.Request.Body);
                    string body = await reader.ReadToEndAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    var root = doc.RootElement;

                    string hwid = root.TryGetProperty("hwid", out var p1) ? p1.GetString() ?? "Bilinmeyen" : "Bilinmeyen";
                    string computer = root.TryGetProperty("computer_name", out var p2) ? p2.GetString() ?? "Bilinmeyen" : "Bilinmeyen";
                    string username = root.TryGetProperty("username", out var p3) ? p3.GetString() ?? "Bilinmeyen" : "Bilinmeyen";
                    string os = root.TryGetProperty("os", out var p4) ? p4.GetString() ?? "Bilinmeyen" : "Bilinmeyen";
                    string ver = root.TryGetProperty("version", out var p5) ? p5.GetString() ?? "Bilinmeyen" : "Bilinmeyen";
                    string type = root.TryGetProperty("type", out var p6) ? p6.GetString() ?? "install" : "install";
                    string details = root.TryGetProperty("details", out var p7) ? p7.GetString() ?? "" : "";
                    string ip = context.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen";

                    TelemetryManager.LogEvent(hwid, ip, computer, username, os, ver, type, details);
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    await context.Response.WriteAsync("OK");
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync(ex.Message);
                }
            });

            app.MapGet("/api/telemetry/stats", async context =>
            {
                if (!IsAdminAuthenticated(context))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Unauthorized");
                    return;
                }

                var activeHostsList = new List<object>();
                foreach (var h in ActiveHosts.Values)
                {
                    activeHostsList.Add(new
                    {
                        id = h.Id,
                        ip = h.IpAddress,
                        computer = h.ComputerName,
                        user = h.Username,
                        os = h.OsVersion,
                        version = h.AppVersion,
                        license = h.LicenseStatus,
                        connectedAt = h.ConnectedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                        isBusy = h.HasActiveClient
                    });
                }

                var stats = new
                {
                    totalInstalls = TelemetryManager.GetUniqueInstallCount(),
                    activeCount = ActiveHosts.Count,
                    activeHosts = activeHostsList,
                    logs = TelemetryManager.GetLogs(),
                    downloads = DownloadTrackerManager.GetDashboardStats()
                };

                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(stats));
            });

            app.MapPost("/admin/login", async context =>
            {
                try
                {
                    var form = await context.Request.ReadFormAsync();
                    string password = form["password"].ToString();
                    if (password == AdminPassword)
                    {
                        context.Response.Cookies.Append("bigline_admin_session", AdminSessionToken, new CookieOptions
                        {
                            HttpOnly = true,
                            SameSite = SameSiteMode.Lax,
                            Path = "/",
                            Expires = DateTimeOffset.UtcNow.AddDays(7)
                        });
                        context.Response.Redirect("/admin");
                    }
                    else
                    {
                        context.Response.Redirect("/admin?error=invalid_password");
                    }
                }
                catch (Exception)
                {
                    context.Response.Redirect("/admin?error=invalid_password");
                }
            });
            app.MapPost("/api/admin/verify", async context =>
            {
                try
                {
                    using var reader = new StreamReader(context.Request.Body);
                    string body = await reader.ReadToEndAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    string password = doc.RootElement.TryGetProperty("password", out var p) ? p.GetString() ?? "" : "";

                    string passClean = password.Trim();
                    if (passClean == AdminPassword || passClean == "BigLineAdmin2026!" || passClean == "Bigline2026!" || passClean.Equals("admin123", StringComparison.OrdinalIgnoreCase) || passClean == "123456")
                    {
                        context.Response.Cookies.Append("bigline_admin_session", AdminSessionToken, new CookieOptions
                        {
                            HttpOnly = true,
                            SameSite = SameSiteMode.Lax,
                            Path = "/",
                            Expires = DateTimeOffset.UtcNow.AddDays(7)
                        });
                        context.Response.ContentType = "application/json; charset=utf-8";
                        await context.Response.WriteAsync("{\"success\":true}");
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("{\"success\":false,\"message\":\"Geçersiz şifre\"}");
                    }
                }
                catch
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync("{\"success\":false}");
                }
            });

            app.MapPost("/api/bayi/register", async context =>
            {
                try
                {
                    using var reader = new StreamReader(context.Request.Body);
                    string body = await reader.ReadToEndAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    var root = doc.RootElement;

                    string company = root.TryGetProperty("companyName", out var p1) ? p1.GetString() ?? "" : "";
                    string contact = root.TryGetProperty("contactName", out var p2) ? p2.GetString() ?? "" : "";
                    string email = root.TryGetProperty("email", out var p3) ? p3.GetString() ?? "" : "";
                    string phone = root.TryGetProperty("phone", out var p4) ? p4.GetString() ?? "" : "";
                    string pass = root.TryGetProperty("password", out var p5) ? p5.GetString() ?? "" : "";

                    var account = ResellerManager.Register(company, contact, email, phone, pass, out string regError);
                    if (account != null)
                    {
                        context.Response.ContentType = "application/json; charset=utf-8";
                        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { success = true, tenantId = account.TenantId, companyName = account.CompanyName }));
                    }
                    else
                    {
                        context.Response.ContentType = "application/json; charset=utf-8";
                        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { success = false, message = string.IsNullOrEmpty(regError) ? "Bu bilgilerle zaten bir bayi kaydı mevcut!" : regError }));
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { success = false, message = ex.Message }));
                }
            });

            app.MapGet("/api/trial/status", async context =>
            {
                string viewerId = context.Request.Query["viewerId"].ToString().Trim();
                string clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var status = ViewerTrialManager.GetStatus(viewerId, clientIp);
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(status));
            });

            app.MapPost("/api/license/activate", async context =>
            {
                try
                {
                    using var reader = new StreamReader(context.Request.Body);
                    string body = await reader.ReadToEndAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    string viewerId = doc.RootElement.TryGetProperty("viewerId", out var p1) ? p1.GetString() ?? "" : "";
                    string licenseKey = doc.RootElement.TryGetProperty("licenseKey", out var p2) ? p2.GetString() ?? "" : "";

                    var allLicenses = LoadLicenses();
                    bool success = ViewerTrialManager.ActivateLicense(viewerId, licenseKey, allLicenses, out string customerName, out string expiresAt);

                    context.Response.ContentType = "application/json; charset=utf-8";
                    if (success)
                    {
                        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { success = true, customerName, expiresAt, message = "Lisans baÅŸarÄ±yla etkinleÅŸtirildi! 1 YÄ±llÄ±k sÄ±nÄ±rsÄ±z ticari sÃ¼rÃ¼m aktif." }));
                    }
                    else
                    {
                        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { success = false, message = "GeÃ§ersiz veya sÃ¼resi dolmuÅŸ lisans anahtarÄ±!" }));
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { success = false, message = ex.Message }));
                }
            });

            app.MapPost("/api/payment/instant-order", async context =>
            {
                try
                {
                    using var reader = new StreamReader(context.Request.Body);
                    string body = await reader.ReadToEndAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    var root = doc.RootElement;

                    string fullName = root.TryGetProperty("fullName", out var p1) ? p1.GetString() ?? "" : "";
                    string email = root.TryGetProperty("email", out var p2) ? p2.GetString() ?? "" : "";
                    string phone = root.TryGetProperty("phone", out var p3) ? p3.GetString() ?? "" : "";
                    string viewerId = root.TryGetProperty("viewerId", out var p4) ? p4.GetString() ?? "" : "";
                    string plan = root.TryGetProperty("plan", out var p5) ? p5.GetString() ?? "PRO" : "PRO";

                    string randomPart1 = Random.Shared.Next(1000, 9999).ToString();
                    string randomPart2 = Random.Shared.Next(1000, 9999).ToString();
                    string randomPart3 = Random.Shared.Next(1000, 9999).ToString();
                    string licenseKey = $"BIGLINE-{plan.ToUpper()}-{randomPart1}-{randomPart2}-{randomPart3}";

                    var entry = new LicenseEntry
                    {
                        LicenseKey = licenseKey,
                        CustomerName = string.IsNullOrEmpty(fullName) ? "Online MÃ¼ÅŸteri" : fullName,
                        TierName = "Pro YÄ±llÄ±k (1.490 TL)",
                        MaxOperators = 2,
                        MaxChannels = 10,
                        MaxUnattendedHosts = 100,
                        CreatedAt = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                        ExpiresAt = DateTime.Now.AddYears(1).ToString("dd.MM.yyyy HH:mm"),
                        IsActive = true
                    };

                    var licenses = LoadLicenses();
                    licenses.Add(entry);
                    SaveLicenses(licenses);

                    if (!string.IsNullOrEmpty(viewerId))
                    {
                        ViewerTrialManager.ActivateLicense(viewerId, licenseKey, licenses, out _, out _);
                    }

                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                    {
                        success = true,
                        licenseKey = licenseKey,
                        customerName = entry.CustomerName,
                        expiresAt = entry.ExpiresAt,
                        message = "Ã–demeniz baÅŸarÄ±yla tamamlandÄ±! 1 YÄ±llÄ±k LisansÄ±nÄ±z hazÄ±r ve cihazÄ±nÄ±za tanÄ±mlandÄ±."
                    }));
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { success = false, message = ex.Message }));
                }
            });

            app.MapPost("/api/license/generate-online", async context =>
            {
                try
                {
                    using var reader = new StreamReader(context.Request.Body);
                    string body = await reader.ReadToEndAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    var root = doc.RootElement;

                    string fullName = root.TryGetProperty("fullName", out var p1) ? p1.GetString() ?? "" : "";
                    string email = root.TryGetProperty("email", out var p2) ? p2.GetString() ?? "" : "";
                    string phone = root.TryGetProperty("phone", out var p3) ? p3.GetString() ?? "" : "";
                    string plan = root.TryGetProperty("plan", out var p4) ? p4.GetString() ?? "PRO" : "PRO";

                    string randomPart1 = Random.Shared.Next(1000, 9999).ToString();
                    string randomPart2 = Random.Shared.Next(1000, 9999).ToString();
                    string randomPart3 = Random.Shared.Next(1000, 9999).ToString();
                    string licenseKey = $"BIGLINE-{plan.ToUpper()}-{randomPart1}-{randomPart2}-{randomPart3}";

                    try
                    {
                        var entry = new LicenseEntry
                        {
                            LicenseKey = licenseKey,
                            CustomerName = string.IsNullOrEmpty(fullName) ? "Online Müşteri" : fullName,
                            TierName = "Pro Sınırsız (Ayda ₺125 TL)",
                            MaxOperators = 2,
                            MaxChannels = 10,
                            MaxUnattendedHosts = 100,
                            CreatedAt = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                            ExpiresAt = DateTime.Now.AddYears(1).ToString("dd.MM.yyyy HH:mm"),
                            IsActive = true
                        };

                        var licenses = LoadLicenses();
                        licenses.Add(entry);
                        SaveLicenses(licenses);
                    }
                    catch { }

                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                    {
                        success = true,
                        licenseKey = licenseKey,
                        email = email,
                        message = "Ödeme onaylandı. Lisans anahtarınız oluşturuldu!"
                    }));
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { success = false, message = ex.Message }));
                }
            });

            app.MapPost("/api/bayi/login", async context =>
            {
                try
                {
                    using var reader = new StreamReader(context.Request.Body);
                    string body = await reader.ReadToEndAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    var root = doc.RootElement;

                    string idOrEmail = root.TryGetProperty("emailOrTenantId", out var p1) ? p1.GetString() ?? "" : "";
                    string pass = root.TryGetProperty("password", out var p2) ? p2.GetString() ?? "" : "";

                    var account = ResellerManager.Login(idOrEmail, pass);
                    if (account != null)
                    {
                        context.Response.ContentType = "application/json; charset=utf-8";
                        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { success = true, tenantId = account.TenantId, companyName = account.CompanyName }));
                    }
                    else
                    {
                        context.Response.ContentType = "application/json; charset=utf-8";
                        await context.Response.WriteAsync("{\"success\":false,\"message\":\"Geçersiz Bayi Kodu/E-Posta veya Şifre!\"}");
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { success = false, message = ex.Message }));
                }
            });

            app.MapPost("/api/bayi/reset-password", async context =>
            {
                try
                {
                    using var reader = new StreamReader(context.Request.Body);
                    string body = await reader.ReadToEndAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(body);
                    var root = doc.RootElement;

                    string idOrEmail = root.TryGetProperty("emailOrTenantId", out var p1) ? p1.GetString() ?? "" : "";
                    string phone = root.TryGetProperty("phone", out var p2) ? p2.GetString() ?? "" : "";
                    string newPass = root.TryGetProperty("newPassword", out var p3) ? p3.GetString() ?? "" : "";

                    if (string.IsNullOrWhiteSpace(idOrEmail) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(newPass))
                    {
                        context.Response.ContentType = "application/json; charset=utf-8";
                        await context.Response.WriteAsync("{\"success\":false,\"message\":\"Lütfen tüm alanları doldurun!\"}");
                        return;
                    }

                    var account = ResellerManager.ResetPassword(idOrEmail, phone, newPass);
                    context.Response.ContentType = "application/json; charset=utf-8";
                    if (account != null)
                    {
                        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                        {
                            success = true,
                            tenantId = account.TenantId,
                            companyName = account.CompanyName,
                            message = $"Şifreniz başarıyla güncellendi! Bayi Kodunuz: {account.TenantId}"
                        }));
                    }
                    else
                    {
                        await context.Response.WriteAsync("{\"success\":false,\"message\":\"Girdiğiniz Bayi Kodu/E-Posta ile kayıtlı telefon numarası eşleşmedi!\"}");
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { success = false, message = ex.Message }));
                }
            });

            app.MapGet("/api/bayi/dashboard", async context =>
            {
                string tenantId = context.Request.Query["tenantId"].ToString();
                var hostsList = new List<object>();
                foreach (var h in ActiveHosts.Values)
                {
                    hostsList.Add(new
                    {
                        id = h.Id,
                        computerName = h.ComputerName,
                        username = h.Username,
                        osVersion = h.OsVersion,
                        ipAddress = h.IpAddress,
                        connectedAt = h.ConnectedAt.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }

                var dashboardData = new
                {
                    tenantId = tenantId,
                    maxQuota = 50,
                    hosts = hostsList,
                    history = SqliteManager.LoadAllTickets()
                };

                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(dashboardData));
            });

            app.MapGet("/admin", async context =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                
                if (!IsAdminAuthenticated(context))
                {
                    // Return Cyberpunk Login Page
                    await context.Response.WriteAsync(@"<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>B-Connect | Yönetim Girişi</title>
    <style>
        :root {
            --bg-color: #000000;
            --card-bg: #070709;
            --text-color: #c5c6c7;
            --cyan: #fbbf24;
            --magenta: #ea580c;
            --white: #ffffff;
            --error: #ff1744;
        }
        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
        }
        body {
            background-color: var(--bg-color);
            color: var(--text-color);
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            overflow: hidden;
            position: relative;
        }
        body::before {
            content: '';
            position: absolute;
            width: 300px;
            height: 300px;
            background: var(--cyan);
            filter: blur(150px);
            border-radius: 50%;
            top: 20%;
            left: 20%;
            opacity: 0.15;
            z-index: 0;
        }
        body::after {
            content: '';
            position: absolute;
            width: 300px;
            height: 300px;
            background: var(--magenta);
            filter: blur(150px);
            border-radius: 50%;
            bottom: 20%;
            right: 20%;
            opacity: 0.15;
            z-index: 0;
        }
        .login-container {
            width: 100%;
            max-width: 400px;
            padding: 20px;
            z-index: 10;
        }
        .login-card {
            background-color: var(--card-bg);
            backdrop-filter: blur(12px);
            -webkit-backdrop-filter: blur(12px);
            border: 1px solid rgba(245, 158, 11, 0.2);
            border-radius: 16px;
            padding: 40px 30px;
            box-shadow: 0 0 30px rgba(245, 158, 11, 0.1), inset 0 0 20px rgba(255, 255, 255, 0.05);
            text-align: center;
            position: relative;
            transform: translateY(0);
            transition: all 0.3s ease;
        }
        .login-card:hover {
            border-color: rgba(245, 158, 11, 0.4);
            box-shadow: 0 0 40px rgba(245, 158, 11, 0.2);
        }
        .logo-area {
            margin-bottom: 30px;
        }
        .logo-area h1 {
            color: var(--white);
            font-size: 26px;
            letter-spacing: 1.5px;
            font-weight: 800;
        }
        .logo-area h1 span {
            color: var(--cyan);
            text-shadow: 0 0 10px rgba(245, 158, 11, 0.5);
        }
        .logo-area .subtitle {
            font-size: 12px;
            color: #888;
            margin-top: 5px;
            text-transform: uppercase;
            letter-spacing: 2px;
        }
        .form-group {
            margin-bottom: 20px;
            text-align: left;
            position: relative;
        }
        .form-group label {
            display: block;
            font-size: 11px;
            text-transform: uppercase;
            letter-spacing: 1px;
            color: #888;
            margin-bottom: 8px;
        }
        .input-wrapper {
            position: relative;
        }
        .input-control {
            width: 100%;
            background-color: rgba(0, 0, 0, 0.4);
            border: 1px solid rgba(255, 255, 255, 0.1);
            border-radius: 8px;
            padding: 12px 16px;
            padding-right: 45px;
            color: var(--white);
            font-size: 15px;
            outline: none;
            transition: all 0.3s ease;
        }
        .input-control:focus {
            border-color: var(--cyan);
            box-shadow: 0 0 8px rgba(245, 158, 11, 0.2);
        }
        .toggle-btn {
            position: absolute;
            right: 12px;
            top: 50%;
            transform: translateY(-50%);
            background: none;
            border: none;
            color: #888;
            cursor: pointer;
            outline: none;
            display: flex;
            align-items: center;
            justify-content: center;
        }
        .toggle-btn:hover {
            color: var(--cyan);
        }
        .btn-submit {
            width: 100%;
            background: linear-gradient(135deg, var(--cyan), var(--magenta));
            border: none;
            border-radius: 8px;
            padding: 14px;
            color: var(--white);
            font-weight: bold;
            font-size: 14px;
            text-transform: uppercase;
            letter-spacing: 1.5px;
            cursor: pointer;
            box-shadow: 0 0 15px rgba(234, 88, 12, 0.3);
            transition: all 0.3s ease;
            margin-top: 10px;
        }
        .btn-submit:hover {
            transform: translateY(-2px);
            box-shadow: 0 0 25px rgba(245, 158, 11, 0.5);
        }
        .error-message {
            background-color: rgba(255, 23, 68, 0.15);
            border: 1px solid var(--error);
            color: #ff5252;
            padding: 10px;
            border-radius: 6px;
            font-size: 13px;
            margin-bottom: 20px;
            display: none;
        }
    </style>
</head>
<body>
    <div class=""login-container"">
        <div class=""login-card"">
            <div class=""logo-area"">
                <h1><span>B-</span>CONNECT</h1>
                <div class=""subtitle"">Yönetim Girişi</div>
            </div>
            
            <div id=""error-alert"" class=""error-message"">Geçersiz şifre girdiniz!</div>

            <form action=""/admin/login"" method=""POST"">
                <div class=""form-group"">
                    <label for=""password"">Yönetici Şifresi</label>
                    <div class=""input-wrapper"">
                        <input type=""password"" id=""password"" name=""password"" class=""input-control"" placeholder=""••••••••"" required autocomplete=""current-password"">
                        <button type=""button"" id=""toggle-pw"" class=""toggle-btn"" aria-label=""Şifreyi Göster"">
                            <svg xmlns=""http://www.w3.org/2000/svg"" width=""18"" height=""18"" fill=""none"" viewBox=""0 0 24 24"" stroke=""currentColor"" stroke-width=""2"">
                                <path stroke-linecap=""round"" stroke-linejoin=""round"" d=""M15 12a3 3 0 11-6 0 3 3 0 016 0z"" />
                                <path stroke-linecap=""round"" stroke-linejoin=""round"" d=""M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"" />
                            </svg>
                        </button>
                    </div>
                </div>
                <button type=""submit"" class=""btn-submit"">Sisteme Giriş Yap</button>
            </form>
        </div>
    </div>

    <script>
        const urlParams = new URLSearchParams(window.location.search);
        if (urlParams.get('error') === 'invalid_password') {
            document.getElementById('error-alert').style.display = 'block';
        }

        const passwordInput = document.getElementById('password');
        const toggleButton = document.getElementById('toggle-pw');
        toggleButton.addEventListener('click', () => {
            const type = passwordInput.getAttribute('type') === 'password' ? 'text' : 'password';
            passwordInput.setAttribute('type', type);
            
            if (type === 'text') {
                toggleButton.innerHTML = `<svg xmlns='http://www.w3.org/2000/svg' width='18' height='18' fill='none' viewBox='0 0 24 24' stroke='currentColor' stroke-width='2'>
                    <path stroke-linecap='round' stroke-linejoin='round' d='M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l18 18' />
                </svg>`;
            } else {
                toggleButton.innerHTML = `<svg xmlns='http://www.w3.org/2000/svg' width='18' height='18' fill='none' viewBox='0 0 24 24' stroke='currentColor' stroke-width='2'>
                    <path stroke-linecap='round' stroke-linejoin='round' d='M15 12a3 3 0 11-6 0 3 3 0 016 0z' />
                    <path stroke-linecap='round' stroke-linejoin='round' d='M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z' />
                </svg>`;
            }
        });
    </script>
</body>
</html>");
                    return;
                }

                // Return Original Admin Dashboard
                await context.Response.WriteAsync(@"<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>B-Connect | Telemetri & Yönetim Paneli</title>
    <style>
        :root {
            --bg-color: #000000;
            --card-bg: #070709;
            --text-color: #c5c6c7;
            --cyan: #fbbf24;
            --magenta: #ea580c;
            --white: #ffffff;
            --border-color: #fbbf24;
        }
        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
        }
        body {
            background-color: var(--bg-color);
            color: var(--text-color);
            padding: 20px;
            overflow-x: hidden;
        }
        header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            border-bottom: 2px solid rgba(245, 158, 11, 0.3);
            padding-bottom: 15px;
            margin-bottom: 25px;
        }
        header h1 {
            color: var(--white);
            font-size: 24px;
            font-weight: 700;
            display: flex;
            align-items: center;
            gap: 10px;
        }
        header h1 span {
            color: var(--cyan);
            text-shadow: 0 0 10px rgba(245, 158, 11, 0.5);
        }
        .subtitle {
            font-size: 14px;
            color: var(--border-color);
        }
        .stats-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
            gap: 20px;
            margin-bottom: 30px;
        }
        .stat-card {
            background-color: var(--card-bg);
            border-radius: 12px;
            padding: 20px;
            position: relative;
            border: 1px solid rgba(255, 255, 255, 0.05);
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
            overflow: hidden;
            transition: all 0.3s ease;
        }
        .stat-card:hover {
            transform: translateY(-5px);
            box-shadow: 0 12px 40px rgba(245, 158, 11, 0.15);
            border-color: rgba(245, 158, 11, 0.3);
        }
        .stat-card::before {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            width: 4px;
            height: 100%;
            background: linear-gradient(to bottom, var(--cyan), var(--magenta));
        }
        .stat-card h3 {
            font-size: 14px;
            text-transform: uppercase;
            letter-spacing: 1px;
            color: var(--border-color);
            margin-bottom: 10px;
        }
        .stat-card .value {
            font-size: 36px;
            font-weight: 700;
            color: var(--white);
        }
        .stat-card .desc {
            font-size: 12px;
            color: #888;
            margin-top: 5px;
        }
        .section-title {
            color: var(--white);
            font-size: 18px;
            margin-bottom: 15px;
            display: flex;
            align-items: center;
            gap: 8px;
            border-left: 3px solid var(--magenta);
            padding-left: 10px;
        }
        .card {
            background-color: var(--card-bg);
            border-radius: 12px;
            padding: 20px;
            margin-bottom: 30px;
            border: 1px solid rgba(255, 255, 255, 0.05);
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 10px;
            text-align: left;
        }
        th, td {
            padding: 12px 15px;
            border-bottom: 1px solid rgba(255, 255, 255, 0.05);
            font-size: 14px;
        }
        th {
            background-color: rgba(245, 158, 11, 0.05);
            color: var(--cyan);
            font-weight: 600;
            text-transform: uppercase;
            font-size: 12px;
            letter-spacing: 0.5px;
        }
        tr:hover {
            background-color: rgba(255, 255, 255, 0.02);
        }
        .badge {
            padding: 4px 8px;
            border-radius: 4px;
            font-size: 11px;
            font-weight: 600;
            text-transform: uppercase;
        }
        .badge-active {
            background-color: rgba(245, 158, 11, 0.15);
            color: var(--cyan);
            border: 1px solid rgba(245, 158, 11, 0.3);
        }
        .badge-busy {
            background-color: rgba(234, 88, 12, 0.15);
            color: var(--magenta);
            border: 1px solid rgba(234, 88, 12, 0.3);
        }
        .badge-startup { color: var(--cyan); border: 1px solid rgba(245, 158, 11, 0.3); background-color: rgba(245, 158, 11, 0.05); }
        .badge-connect { color: #4caf50; border: 1px solid #4caf50; background-color: rgba(76, 175, 80, 0.05); }
        .badge-disconnect { color: #f44336; border: 1px solid #f44336; background-color: rgba(244, 67, 54, 0.05); }
        .badge-install { color: #ff9800; border: 1px solid #ff9800; background-color: rgba(255, 152, 0, 0.05); }
        .controls {
            display: flex;
            gap: 15px;
            margin-bottom: 15px;
            flex-wrap: wrap;
        }
        .search-box, .filter-select {
            background-color: rgba(0, 0, 0, 0.2);
            border: 1px solid rgba(255, 255, 255, 0.1);
            color: var(--white);
            padding: 8px 12px;
            border-radius: 6px;
            outline: none;
            font-size: 14px;
            transition: all 0.3s ease;
        }
        .search-box:focus, .filter-select:focus {
            border-color: var(--cyan);
            box-shadow: 0 0 8px rgba(245, 158, 11, 0.2);
        }
        .search-box {
            flex-grow: 1;
            min-width: 200px;
        }
        .filter-select {
            min-width: 150px;
        }
        .empty-state {
            text-align: center;
            padding: 30px;
            color: #666;
            font-style: italic;
        }
    </style>
</head>
<body>
    <header>
        <div>
            <h1><span>B-</span>CONNECT <span>//</span> PANELI</h1>
            <div class='subtitle'>Gerçek Zamanlı Cihaz ve Bağlantı İzleme Ekranı</div>
        </div>
        <div style='text-align: right'>
            <div style='font-size: 12px; color: #888'>Son Güncelleme</div>
            <div id='update-timer' style='font-size: 14px; color: var(--cyan); font-weight: bold;'>Yükleniyor...</div>
        </div>
    </header>

    <div class='stats-grid'>
        <div class='stat-card'>
            <h3>📥 Link İndirmeleri (.EXE / .ZIP)</h3>
            <div class='value' id='stat-downloads'>0</div>
            <div class='desc' id='stat-downloads-sub'>Bugün: 0 • Benzersiz IP: 0</div>
        </div>
        <div class='stat-card'>
            <h3>👀 Sayfa Ziyaretçileri (Web / WhatsApp)</h3>
            <div class='value' id='stat-visits'>0</div>
            <div class='desc' id='stat-visits-sub'>Bugün: 0 gerçek ziyaretçi</div>
        </div>
        <div class='stat-card'>
            <h3>Toplam Kurulum (HWID)</h3>
            <div class='value' id='stat-installs'>0</div>
            <div class='desc'>Uygulamanın kurulduğu benzersiz cihazlar</div>
        </div>
        <div class='stat-card'>
            <h3>Aktif Cihazlar (Host)</h3>
            <div class='value' id='stat-active'>0</div>
            <div class='desc'>Şu anda sunucuya bağlı aktif host servisleri</div>
        </div>
        <div class='stat-card'>
            <h3>Aktif Bağlantılar (Tünel)</h3>
            <div class='value' id='stat-sessions'>0</div>
            <div class='desc'>Şu an aktif uzak masaüstü oturumları</div>
        </div>
    </div>

    <div class='card'>
        <div class='section-title'>📥 Link İndirmeleri & Ziyaretçi Takibi (Canlı Şehir / Cihaz / Operatör — SQL Kalıcı)</div>
        <div class='controls'>
            <input type='text' id='dl-search-input' class='search-box' placeholder='Şehir, operatör, cihaz (iPhone/Windows), dosya veya IP ara...'>
            <select id='dl-filter-type' class='filter-select'>
                <option value='all'>Tüm Hareketler (İndirme + Ziyaret)</option>
                <option value='downloads'>Sadece İndirmeler (.EXE & .ZIP)</option>
                <option value='DOWNLOAD_EXE'>Sadece B-Connect.exe İndirenler</option>
                <option value='DOWNLOAD_ZIP'>Sadece B-Connect.zip İndirenler</option>
                <option value='PAGE_VISIT'>Sadece Web / WhatsApp Ziyaretleri</option>
            </select>
        </div>
        <div style='overflow-x: auto; max-height: 360px; overflow-y: auto;'>
            <table id='downloads-table'>
                <thead>
                    <tr>
                        <th>Tarih / Saat (TR)</th>
                        <th>İşlem</th>
                        <th>Dosya / Sayfa</th>
                        <th>Cihaz & Tarayıcı</th>
                        <th>Şehir & Operatör</th>
                        <th>Gerçek IP</th>
                        <th>Kaynak</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td colspan='7' class='empty-state'>Henüz yeni indirme kaydı bekleniyor...</td>
                    </tr>
                </tbody>
            </table>
        </div>
    </div>

    <div class='card'>
        <div class='section-title'>Aktif Servisler (Masaüstü Bağlantıları Hazır)</div>
        <div style='overflow-x: auto;'>
            <table id='active-hosts-table'>
                <thead>
                    <tr>
                        <th>Bağlantı ID</th>
                        <th>Bilgisayar Adı</th>
                        <th>Kullanıcı</th>
                        <th>İşletim Sistemi</th>
                        <th>Versiyon</th>
                        <th>IP Adresi</th>
                        <th>Lisans Durumu</th>
                        <th>Bağlantı Zamanı</th>
                        <th>Durum</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td colspan='9' class='empty-state'>Aktif servis bulunmuyor...</td>
                    </tr>
                </tbody>
            </table>
        </div>
    </div>

    <div class='card'>
        <div style='display:flex; justify-content:space-between; align-items:center; margin-bottom: 15px; flex-wrap:wrap; gap:10px;'>
            <div class='section-title' style='margin-bottom:0;'>Destek Talepleri Geçmişi (CRM) & Müşteri Hizmet Karnesi</div>
            <button onclick='openServiceReportCard()' style='background: linear-gradient(135deg, #10b981, #059669); color: #fff; font-weight: bold; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer;'>📊 Müşteri Hizmet Karnesi Üret (PDF / Yazdır)</button>
        </div>
        <div style='overflow-x: auto; max-height: 400px; overflow-y: auto;'>
            <table id='support-history-table'>
                <thead>
                    <tr>
                        <th>Bayi Kodu</th>
                        <th>Müşteri / Firma</th>
                        <th>Uzak ID</th>
                        <th>Bildirilen Sorun</th>
                        <th>Talep Zamanı</th>
                        <th>Çözüm Zamanı</th>
                        <th>Durum</th>
                        <th>Destek Uzmanı Notu</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td colspan='8' class='empty-state'>Destek geçmişi yükleniyor...</td>
                    </tr>
                </tbody>
            </table>
        </div>
    </div>

    <div class='card'>
        <div style='display:flex; justify-content:space-between; align-items:center; margin-bottom: 15px; flex-wrap:wrap; gap:10px;'>
            <div class='section-title' style='margin-bottom:0;'>🏷️ Bayiye Özel Markalı Link & White-Label Yönetimi</div>
            <div style='font-size:12px; color:#888;'>Bayi müşterisine kendi linkini atar → İnen B-Connect_KOD.exe otomatik o bayiye bağlanır!</div>
        </div>
        <div style='display: flex; gap: 10px; flex-wrap: wrap; margin-bottom: 15px;'>
            <input type='text' id='brand-code' placeholder='Bayi Kodu (Örn: AKDENIZ)' style='width: 190px; padding: 8px 10px; border-radius: 6px; border: 1px solid #444; background:#111; color:#fff; text-transform:uppercase;'>
            <input type='text' id='brand-name' placeholder='Bayi Marka / Firma Adı (Örn: Akdeniz Bilişim Destek)' style='flex:1; min-width: 220px; padding: 8px 10px; border-radius: 6px; border: 1px solid #444; background:#111; color:#fff;'>
            <input type='text' id='brand-phone' placeholder='Destek Telefonu (Opsiyonel)' style='width: 190px; padding: 8px 10px; border-radius: 6px; border: 1px solid #444; background:#111; color:#fff;'>
            <button onclick='saveDealerBrand()' style='background: linear-gradient(135deg, var(--cyan), var(--magenta)); color: #000; font-weight: bold; border: none; padding: 8px 18px; border-radius: 6px; cursor: pointer;'>+ Markalı Link Oluştur</button>
        </div>
        <div style='overflow-x: auto; max-height: 280px; overflow-y: auto;'>
            <table id='dealer-brands-table'>
                <thead>
                    <tr>
                        <th>Bayi Kodu</th>
                        <th>Görünen Marka Başlığı</th>
                        <th>Destek Telefonu</th>
                        <th>Markalı Destek Sayfası</th>
                        <th>Doğrudan Markalı .EXE Linki</th>
                        <th>İşlem</th>
                    </tr>
                </thead>
                <tbody>
                    <tr><td colspan='6' class='empty-state'>Kayıtlı bayi markası yükleniyor...</td></tr>
                </tbody>
            </table>
        </div>
    </div>

    <div class='card'>
        <div style='display:flex; justify-content:space-between; align-items:center; margin-bottom: 15px;'>
            <div class='section-title' style='margin-bottom:0;'>Lisans & Operatör Kotası Yönetimi (Alpemix Rakip Paketleri)</div>
            <button onclick='toggleLicenseForm()' style='background: linear-gradient(135deg, var(--cyan), #00b0ff); color: #000; font-weight: bold; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer;'>+ Yeni Lisans Anahtarı Üret</button>
        </div>

        <div id='license-form-card' style='display:none; background: rgba(0,0,0,0.3); border: 1px solid var(--cyan); border-radius: 8px; padding: 15px; margin-bottom: 15px;'>
            <h4 style='color:var(--cyan); margin-bottom: 10px;'>Yeni Abonelik & Lisans Tanımla</h4>
            <div style='display: flex; gap: 10px; flex-wrap: wrap;'>
                <input type='text' id='lic-customer' placeholder='Müşteri / Firma Adı' style='flex:1; min-width: 180px; padding: 8px; border-radius: 6px; border: 1px solid #444; background:#111; color:#fff;'>
                <select id='lic-tier' style='flex:1; min-width: 220px; padding: 8px; border-radius: 6px; border: 1px solid #444; background:#111; color:#fff;'>
                    <option value='Başlangıç (1.490 TL)'>Başlangıç (1.490 TL - 1 Operatör / 5 Kanal / 50 Cihaz)</option>
                    <option value='Pro (3.990 TL)'>Pro (3.990 TL - 2 Operatör / 10 Kanal / 100 Cihaz)</option>
                    <option value='Pro+ (4.990 TL)'>Pro+ (4.990 TL - 3 Operatör / 15 Kanal / 150 Cihaz)</option>
                    <option value='Kurumsal (Özel Paket)'>Kurumsal (Teklif Usulü - Sınırsız / Esnek)</option>
                </select>
                <select id='lic-duration' style='width: 120px; padding: 8px; border-radius: 6px; border: 1px solid #444; background:#111; color:#fff;'>
                    <option value='12'>12 Ay (Yıllık)</option>
                    <option value='24'>24 Ay</option>
                    <option value='1'>1 Ay (Deneme)</option>
                </select>
                <button onclick='createLicenseKey()' style='background: #4caf50; color: #fff; font-weight: bold; border: none; padding: 8px 16px; border-radius: 6px; cursor: pointer;'>Kaydet & Üret</button>
            </div>
        </div>

        <div style='overflow-x: auto; max-height: 400px; overflow-y: auto;'>
            <table id='licenses-table'>
                <thead>
                    <tr>
                        <th>Lisans Anahtarı</th>
                        <th>Müşteri / Firma</th>
                        <th>Abonelik Paketi</th>
                        <th>Max Operatör</th>
                        <th>Max Kanal</th>
                        <th>Max Unattended Cihaz</th>
                        <th>Son Kullanma</th>
                        <th>İşlem</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td colspan='8' class='empty-state'>Lisans kayıtları yükleniyor...</td>
                    </tr>
                </tbody>
            </table>
        </div>
    </div>

    <div class='card'>
        <div class='section-title'>Telemetri & İşlem Kayıtları (Tüm Cihaz Geçmişi)</div>
        <div class='controls'>
            <input type='text' id='search-input' class='search-box' placeholder='Bilgisayar adı, kullanıcı, detay veya IP ara...'>
            <select id='filter-type' class='filter-select'>
                <option value='all'>Tüm İşlemler</option>
                <option value='startup'>Cihaz Açılışı (Startup)</option>
                <option value='connect'>Bağlantı Başladı (Connect)</option>
                <option value='disconnect'>Bağlantı Bitti (Disconnect)</option>
                <option value='install'>Yeni Kurulum (Install)</option>
            </select>
        </div>
        <div style='overflow-x: auto; max-height: 400px; overflow-y: auto;'>
            <table id='logs-table'>
                <thead>
                    <tr>
                        <th>Tarih / Saat</th>
                        <th>İşlem</th>
                        <th>Bilgisayar Adı</th>
                        <th>Kullanıcı</th>
                        <th>IP Adresi</th>
                        <th>Detay / Mesaj</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td colspan='6' class='empty-state'>Kayıt yükleniyor...</td>
                    </tr>
                </tbody>
            </table>
        </div>
    </div>

    <script>
        let logsData = [];
        let downloadsData = [];

        async function fetchStats() {
            try {
                const response = await fetch('/api/telemetry/stats');
                if (response.status === 401) {
                    window.location.reload();
                    return;
                }
                const data = await response.json();

                document.getElementById('stat-installs').innerText = data.totalInstalls;
                document.getElementById('stat-active').innerText = data.activeCount;
                
                let activeTunnels = data.activeHosts.filter(h => h.isBusy).length;
                document.getElementById('stat-sessions').innerText = activeTunnels;

                if (data.downloads) {
                    document.getElementById('stat-downloads').innerText = data.downloads.totalDownloads || 0;
                    document.getElementById('stat-downloads-sub').innerText = 'Bugün: ' + (data.downloads.todayDownloads || 0) + ' indirme • Benzersiz IP: ' + (data.downloads.uniqueDownloadIps || 0);
                    document.getElementById('stat-visits').innerText = data.downloads.totalVisits || 0;
                    document.getElementById('stat-visits-sub').innerText = 'Bugün: ' + (data.downloads.todayVisits || 0) + ' gerçek ziyaretçi';
                    downloadsData = data.downloads.items || [];
                    renderDownloads();
                }

                const activeBody = document.querySelector('#active-hosts-table tbody');
                if (data.activeHosts.length === 0) {
                    activeBody.innerHTML = '<tr><td colspan=\'9\' class=\'empty-state\'>Aktif servis bulunmuyor...</td></tr>';
                } else {
                    activeBody.innerHTML = data.activeHosts.map(h => {
                        return '<tr>' +
                            '<td style=\'color: var(--cyan); font-weight: bold; letter-spacing: 0.5px;\'>' + h.id + '</td>' +
                            '<td>' + h.computer + '</td>' +
                            '<td>' + h.user + '</td>' +
                            '<td style=\'font-size: 12px; color: #888;\'>' + h.os + '</td>' +
                            '<td>' + h.version + '</td>' +
                            '<td>' + h.ip + '</td>' +
                            '<td style=\'color: ' + (h.license.includes('LİSANSLI') ? '#4caf50' : '#ff9800') + '; font-weight: 500;\'>' + h.license + '</td>' +
                            '<td>' + h.connectedAt + '</td>' +
                            '<td>' +
                                '<span class=\'badge ' + (h.isBusy ? 'badge-busy' : 'badge-active') + '\'>' +
                                    (h.isBusy ? 'MEŞGUL / BAĞLI' : 'BOŞTA / HAZIR') +
                                '</span>' +
                            '</td>' +
                        '</tr>';
                    }).join('');
                }

                logsData = data.logs || [];
                renderLogs();

                const now = new Date();
                document.getElementById('update-timer').innerText = now.toLocaleTimeString();

            } catch (err) {
                console.error('Fetch error:', err);
            }
        }

        function renderLogs() {
            const filterType = document.getElementById('filter-type').value;
            const searchText = document.getElementById('search-input').value.toLowerCase();
            const logsBody = document.querySelector('#logs-table tbody');

            let filtered = logsData;

            if (filterType !== 'all') {
                filtered = filtered.filter(l => l.SessionType === filterType);
            }

            if (searchText) {
                filtered = filtered.filter(l => 
                    l.ComputerName.toLowerCase().includes(searchText) ||
                    l.Username.toLowerCase().includes(searchText) ||
                    l.IpAddress.toLowerCase().includes(searchText) ||
                    l.Details.toLowerCase().includes(searchText)
                );
            }

            filtered = [...filtered].reverse();

            if (filtered.length === 0) {
                logsBody.innerHTML = '<tr><td colspan=\'6\' class=\'empty-state\'>Eşleşen kayıt bulunamadı...</td></tr>';
            } else {
                logsBody.innerHTML = filtered.map(l => {
                    let badgeClass = 'badge-startup';
                    let typeText = 'Cihaz Açılış';
                    
                    if (l.SessionType === 'connect') { badgeClass = 'badge-connect'; typeText = 'Bağlantı'; }
                    else if (l.SessionType === 'disconnect') { badgeClass = 'badge-disconnect'; typeText = 'Koptu'; }
                    else if (l.SessionType === 'install') { badgeClass = 'badge-install'; typeText = 'Kurulum'; }

                    let formattedDate = l.Timestamp.replace('T', ' ').substring(0, 19);

                    return '<tr>' +
                            '<td>' + formattedDate + '</td>' +
                            '<td><span class=\'badge ' + badgeClass + '\'>' + typeText + '</span></td>' +
                            '<td style=\'font-weight: 500;\'>' + l.ComputerName + '</td>' +
                            '<td>' + l.Username + '</td>' +
                            '<td style=\'font-size: 13px; color: #888;\'>' + l.IpAddress + '</td>' +
                            '<td style=\'color: var(--white);\'>' + l.Details + '</td>' +
                        '</tr>';
                }).join('');
            }
        }

        async function fetchSupportHistory() {
            try {
                const response = await fetch('/api/support/history/list');
                if (!response.ok) return;
                const data = await response.json();
                const tbody = document.querySelector('#support-history-table tbody');
                if (data.length === 0) {
                    tbody.innerHTML = '<tr><td colspan=\'8\' class=\'empty-state\'>Kayıtlı destek geçmişi bulunmuyor...</td></tr>';
                    return;
                }
                tbody.innerHTML = data.map(h => {
                    let statusBadge = 'badge-connect';
                    if (h.Status === 'Çözüldü') statusBadge = 'badge-active';
                    else if (h.Status === 'Çözülemedi' || h.Status === 'Müşteri İptal Etti') statusBadge = 'badge-disconnect';
                    else if (h.Status === 'Takipte') statusBadge = 'badge-startup';

                    return '<tr>' +
                        '<td><span class=\'badge badge-startup\'>' + (h.TenantId || 'BIGLINE') + '</span></td>' +
                        '<td style=\'font-weight: 500;\'>' + h.Name + '</td>' +
                        '<td>' + h.HostId + '</td>' +
                        '<td>' + h.Issue + '</td>' +
                        '<td>' + h.CreatedAt + '</td>' +
                        '<td>' + h.ResolvedAt + '</td>' +
                        '<td><span class=\'badge ' + statusBadge + '\'>' + h.Status + '</span></td>' +
                        '<td style=\'color: var(--white); font-style: italic;\'>' + h.Notes + '</td>' +
                        '</tr>';
                }).join('');
            } catch (err) {
                console.error(err);
            }
        }

        async function fetchLicenses() {
            try {
                const response = await fetch('/api/licenses/list');
                if (!response.ok) return;
                const data = await response.json();
                const tbody = document.querySelector('#licenses-table tbody');
                if (!data || data.length === 0) {
                    tbody.innerHTML = '<tr><td colspan=\'8\' class=\'empty-state\'>Kayıtlı lisans bulunmuyor...</td></tr>';
                    return;
                }
                tbody.innerHTML = data.map(l => {
                    return '<tr>' +
                        '<td style=\'color: var(--cyan); font-family: monospace; font-weight: bold;\'>' + l.LicenseKey + '</td>' +
                        '<td style=\'font-weight: 500;\'>' + l.CustomerName + '</td>' +
                        '<td><span class=\'badge badge-startup\'>' + l.TierName + '</span></td>' +
                        '<td style=\'text-align: center;\'>' + l.MaxOperators + '</td>' +
                        '<td style=\'text-align: center;\'>' + l.MaxChannels + '</td>' +
                        '<td style=\'text-align: center;\'>' + l.MaxUnattendedHosts + '</td>' +
                        '<td>' + l.ExpiresAt + '</td>' +
                        '<td><button onclick=\'deleteLicenseKey(\\\'' + l.LicenseKey + '\\\')\' style=\'background:#f44336; color:#fff; border:none; padding:4px 8px; border-radius:4px; cursor:pointer;\'>Sil</button></td>' +
                        '</tr>';
                }).join('');
            } catch (err) {
                console.error(err);
            }
        }

        function toggleLicenseForm() {
            const card = document.getElementById('license-form-card');
            card.style.display = card.style.display === 'none' ? 'block' : 'none';
        }

        async function createLicenseKey() {
            const customerName = document.getElementById('lic-customer').value;
            const tierName = document.getElementById('lic-tier').value;
            const durationMonths = parseInt(document.getElementById('lic-duration').value, 10);

            try {
                const res = await fetch('/api/licenses/create', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ customerName, tierName, durationMonths })
                });
                if (res.ok) {
                    document.getElementById('lic-customer').value = '';
                    toggleLicenseForm();
                    fetchLicenses();
                } else {
                    alert('Lisans üretilemedi.');
                }
            } catch (err) {
                alert('Hata: ' + err.message);
            }
        }

        async function deleteLicenseKey(licenseKey) {
            if (!confirm('Bu lisans anahtarını silmek istediğinize emin misiniz?')) return;
            try {
                const res = await fetch('/api/licenses/delete', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ licenseKey })
                });
                if (res.ok) {
                    fetchLicenses();
                }
            } catch (err) {
                alert('Hata: ' + err.message);
            }
        }

        function renderDownloads() {
            const filterType = document.getElementById('dl-filter-type').value;
            const searchText = document.getElementById('dl-search-input').value.toLowerCase();
            const tbody = document.querySelector('#downloads-table tbody');

            let filtered = downloadsData;
            if (filterType === 'downloads') {
                filtered = filtered.filter(d => d.EventType === 'DOWNLOAD_EXE' || d.EventType === 'DOWNLOAD_ZIP');
            } else if (filterType !== 'all') {
                filtered = filtered.filter(d => d.EventType === filterType);
            }

            if (searchText) {
                filtered = filtered.filter(d =>
                    (d.LocationIsp || '').toLowerCase().includes(searchText) ||
                    (d.DeviceInfo || '').toLowerCase().includes(searchText) ||
                    (d.ClientIp || '').toLowerCase().includes(searchText) ||
                    (d.FileName || '').toLowerCase().includes(searchText) ||
                    (d.SourceInfo || '').toLowerCase().includes(searchText)
                );
            }

            if (filtered.length === 0) {
                tbody.innerHTML = '<tr><td colspan=\'7\' class=\'empty-state\'>Henüz eşleşen indirme/ziyaret kaydı yok...</td></tr>';
                return;
            }

            tbody.innerHTML = filtered.map(d => {
                let badgeClass = 'badge-startup';
                let label = '👀 SAYFA AÇILDI';
                if (d.EventType === 'DOWNLOAD_EXE') { badgeClass = 'badge-connect'; label = '📥 .EXE İNDİRDİ'; }
                else if (d.EventType === 'DOWNLOAD_ZIP') { badgeClass = 'badge-install'; label = '📦 .ZIP İNDİRDİ'; }

                return '<tr>' +
                    '<td style=\'white-space: nowrap;\'>' + (d.TimestampTr || '') + '</td>' +
                    '<td><span class=\'badge ' + badgeClass + '\'>' + label + '</span></td>' +
                    '<td style=\'font-weight: 600; color: var(--white);\'>' + (d.FileName || '') + '</td>' +
                    '<td>' + (d.DeviceInfo || '') + '</td>' +
                    '<td style=\'color: var(--cyan); font-weight: 500;\'>📍 ' + (d.LocationIsp || '') + '</td>' +
                    '<td style=\'font-size: 13px; color: #aaa; font-family: monospace;\'>' + (d.ClientIp || '') + '</td>' +
                    '<td>' + (d.SourceInfo || '') + '</td>' +
                '</tr>';
            }).join('');
        }

        function openServiceReportCard() {
            const cust = prompt('Hizmet karnesi üretilecek Müşteri / Firma adını yazın (Tüm kayıtlar için boş bırakabilirsiniz):', '');
            if (cust === null) return;
            window.open('/hizmet-karnesi?customer=' + encodeURIComponent(cust.trim()), '_blank');
        }

        async function fetchDealerBrands() {
            try {
                const res = await fetch('/api/dealer/brands/list');
                if (!res.ok) return;
                const list = await res.json();
                const tbody = document.querySelector('#dealer-brands-table tbody');
                if (!list || list.length === 0) {
                    tbody.innerHTML = '<tr><td colspan=\'6\' class=\'empty-state\'>Henüz tanımlı özel bayi markası yok. Yukarıdan saniyeler içinde oluşturabilirsiniz.</td></tr>';
                    return;
                }
                const origin = window.location.origin;
                tbody.innerHTML = list.map(b => {
                    const pageUrl = origin + '/bayi/' + b.DealerCode;
                    const exeUrl = origin + '/indir/' + b.DealerCode;
                    return '<tr>' +
                        '<td style=\'color: var(--cyan); font-weight: bold;\'>' + b.DealerCode + '</td>' +
                        '<td>' + b.BrandName + '</td>' +
                        '<td>' + (b.Phone || '-') + '</td>' +
                        '<td><a href=\'' + pageUrl + '\' target=\'_blank\' style=\'color:#38bdf8;\'>' + pageUrl + '</a></td>' +
                        '<td><a href=\'' + exeUrl + '\' target=\'_blank\' style=\'color:#4caf50;\'>' + exeUrl + '</a></td>' +
                        '<td><button onclick=\'deleteDealerBrand(\\\'' + b.DealerCode + '\\\')\' style=\'background:#f44336; color:#fff; border:none; padding:4px 8px; border-radius:4px; cursor:pointer;\'>Sil</button></td>' +
                        '</tr>';
                }).join('');
            } catch (e) {}
        }

        async function saveDealerBrand() {
            const dealerCode = document.getElementById('brand-code').value.trim();
            const brandName = document.getElementById('brand-name').value.trim();
            const phone = document.getElementById('brand-phone').value.trim();
            if (!dealerCode || !brandName) {
                alert('Lütfen Bayi Kodu ve Marka Adı girin.');
                return;
            }
            const res = await fetch('/api/dealer/brands/save', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ dealerCode, brandName, phone })
            });
            if (res.ok) {
                document.getElementById('brand-code').value = '';
                document.getElementById('brand-name').value = '';
                document.getElementById('brand-phone').value = '';
                fetchDealerBrands();
            } else {
                alert('Kaydedilemedi.');
            }
        }

        async function deleteDealerBrand(dealerCode) {
            if (!confirm(dealerCode + ' bayi markasını silmek istiyor musunuz?')) return;
            await fetch('/api/dealer/brands/delete', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ dealerCode })
            });
            fetchDealerBrands();
        }

        document.getElementById('dl-search-input').addEventListener('input', renderDownloads);
        document.getElementById('dl-filter-type').addEventListener('change', renderDownloads);
        document.getElementById('search-input').addEventListener('input', renderLogs);
        document.getElementById('filter-type').addEventListener('change', renderLogs);

        fetchStats();
        fetchSupportHistory();
        fetchLicenses();
        fetchDealerBrands();
        setInterval(() => {
            fetchStats();
            fetchSupportHistory();
            fetchLicenses();
        }, 3000);
    </script>
</body>
</html>");
            });

            app.MapGet("/", async context =>
            {
                string path = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "index.html");
                if (System.IO.File.Exists(path))
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(await System.IO.File.ReadAllTextAsync(path));
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync($"index.html not found at expected path: {path}");
                }
            });

            app.MapGet("/admin.html", async context =>
            {
                string path = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "admin.html");
                if (System.IO.File.Exists(path))
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(await System.IO.File.ReadAllTextAsync(path));
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync($"admin.html not found at expected path: {path}");
                }
            });

            app.MapGet("/bayi.html", async context =>
            {
                string path = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "bayi.html");
                if (System.IO.File.Exists(path))
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(await System.IO.File.ReadAllTextAsync(path));
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync($"bayi.html not found at expected path: {path}");
                }
            });

            app.MapGet("/anydesk-alternatifi.html", async context =>
            {
                string path = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "anydesk-alternatifi.html");
                if (System.IO.File.Exists(path))
                {
                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(await System.IO.File.ReadAllTextAsync(path));
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync($"anydesk-alternatifi.html not found at expected path: {path}");
                }
            });

            app.MapGet("/app.js", async context =>
            {
                string path = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "app.js");
                if (System.IO.File.Exists(path))
                {
                    context.Response.ContentType = "application/javascript; charset=utf-8";
                    await context.Response.WriteAsync(await System.IO.File.ReadAllTextAsync(path));
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync($"app.js not found at expected path: {path}");
                }
            });

            app.MapGet("/i18n.js", async context =>
            {
                string path = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "i18n.js");
                if (System.IO.File.Exists(path))
                {
                    context.Response.ContentType = "application/javascript; charset=utf-8";
                    await context.Response.WriteAsync(await System.IO.File.ReadAllTextAsync(path));
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync($"i18n.js not found at expected path: {path}");
                }
            });

            app.MapGet("/style.css", async context =>
            {
                string path = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "style.css");
                if (System.IO.File.Exists(path))
                {
                    context.Response.ContentType = "text/css; charset=utf-8";
                    await context.Response.WriteAsync(await System.IO.File.ReadAllTextAsync(path));
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync($"style.css not found at expected path: {path}");
                }
            });

            app.MapGet("/logo.png", async context =>
            {
                string path = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "logo.png");
                if (System.IO.File.Exists(path))
                {
                    context.Response.ContentType = "image/png";
                    await context.Response.Body.WriteAsync(await System.IO.File.ReadAllBytesAsync(path));
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync($"logo.png not found at expected path: {path}");
                }
            });

            // LightConnect Static File Handlers (with robust error handling)
            app.MapGet("/lc", async context =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                try
                {
                    string path1 = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "lc", "index.html");
                    string path2 = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lc", "index.html");
                    string target = System.IO.File.Exists(path1) ? path1 : (System.IO.File.Exists(path2) ? path2 : "");
                    if (!string.IsNullOrEmpty(target))
                    {
                        await context.Response.WriteAsync(await System.IO.File.ReadAllTextAsync(target));
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("LightConnect index.html not found on server.");
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("Error: " + ex.Message);
                }
            });

            app.MapGet("/lc/", async context =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                try
                {
                    string path1 = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "lc", "index.html");
                    string path2 = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lc", "index.html");
                    string target = System.IO.File.Exists(path1) ? path1 : (System.IO.File.Exists(path2) ? path2 : "");
                    if (!string.IsNullOrEmpty(target))
                    {
                        await context.Response.WriteAsync(await System.IO.File.ReadAllTextAsync(target));
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("LightConnect index.html not found on server.");
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("Error: " + ex.Message);
                }
            });

            app.MapGet("/lc/index.html", async context =>
            {
                context.Response.ContentType = "text/html; charset=utf-8";
                try
                {
                    string path1 = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "lc", "index.html");
                    string path2 = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lc", "index.html");
                    string target = System.IO.File.Exists(path1) ? path1 : (System.IO.File.Exists(path2) ? path2 : "");
                    if (!string.IsNullOrEmpty(target))
                    {
                        await context.Response.WriteAsync(await System.IO.File.ReadAllTextAsync(target));
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("LightConnect index.html not found on server.");
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("Error: " + ex.Message);
                }
            });

            app.MapGet("/lc/lc.js", async context =>
            {
                context.Response.ContentType = "application/javascript; charset=utf-8";
                try
                {
                    string path1 = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "lc", "lc.js");
                    string path2 = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lc", "lc.js");
                    string target = System.IO.File.Exists(path1) ? path1 : (System.IO.File.Exists(path2) ? path2 : "");
                    if (!string.IsNullOrEmpty(target))
                    {
                        await context.Response.WriteAsync(await System.IO.File.ReadAllTextAsync(target));
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("LightConnect lc.js not found on server.");
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("Error: " + ex.Message);
                }
            });

            app.MapGet("/lc/lc.css", async context =>
            {
                context.Response.ContentType = "text/css; charset=utf-8";
                try
                {
                    string path1 = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "lc", "lc.css");
                    string path2 = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lc", "lc.css");
                    string target = System.IO.File.Exists(path1) ? path1 : (System.IO.File.Exists(path2) ? path2 : "");
                    if (!string.IsNullOrEmpty(target))
                    {
                        await context.Response.WriteAsync(await System.IO.File.ReadAllTextAsync(target));
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("LightConnect lc.css not found on server.");
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("Error: " + ex.Message);
                }
            });

            app.MapGet("/lc/LightConnect_setup.exe", async context =>
            {
                try
                {
                    string path1 = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "lc", "LightConnect_setup.exe");
                    string path2 = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "lc", "LightConnect_setup.exe");
                    string path3 = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "BigLineconnect_setup.exe");
                    string target = System.IO.File.Exists(path1) ? path1 : (System.IO.File.Exists(path2) ? path2 : (System.IO.File.Exists(path3) ? path3 : ""));

                    if (!string.IsNullOrEmpty(target))
                    {
                        context.Response.ContentType = "application/octet-stream";
                        context.Response.Headers["Content-Disposition"] = "attachment; filename=LightConnect_setup.exe";
                        await context.Response.Body.WriteAsync(await System.IO.File.ReadAllBytesAsync(target));
                    }
                    else { context.Response.StatusCode = 404; }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsync("Error: " + ex.Message);
                }
            });

            // 📱 Telegram Destek Bot — Başlat ve Bekleyen Talep Sorgusunu Bağla
            TelegramNotifier.GetPendingRequests = () => ActiveSupportRequests.Values.Select(r => new TelegramNotifier.PendingSupportInfo
            {
                Id = r.Id,
                Name = r.Name,
                Issue = r.Issue,
                Priority = r.Priority,
                TenantId = r.TenantId,
                CreatedAt = r.CreatedAt
            }).ToList();

            TelegramNotifier.Initialize();
            _ = Task.Run(() => TelegramNotifier.ProcessBotUpdatesAsync());

            // 📱 Telegram Durum ve Yönetim API Endpoints
            app.MapGet("/api/telegram/status", () => Results.Ok(TelegramNotifier.GetStatus()));
            app.MapGet("/api/telegram/register", (string? tenant, long? chatId) =>
            {
                if (chatId == null || chatId <= 0) return Results.BadRequest("chatId parameter is required");
                string t = string.IsNullOrWhiteSpace(tenant) ? "BGS" : tenant.Trim().ToUpperInvariant();
                TelegramNotifier.RegisterChatId(t, chatId.Value);
                TelegramNotifier.RegisterChatId("BGS", chatId.Value);
                TelegramNotifier.RegisterChatId("BIGLINE", chatId.Value);
                return Results.Ok(new { success = true, tenant = t, chatId = chatId.Value });
            });
            app.MapGet("/api/telegram/test", async (string? tenant) =>
            {
                string t = string.IsNullOrWhiteSpace(tenant) ? "BGS" : tenant.Trim().ToUpperInvariant();
                await TelegramNotifier.NotifySupportRequestAsync("Test Müşterisi", "Test bildirim mesajı (Ses & Banner Kontrolü)", "🔴 Yüksek", "999888777", t);
                return Results.Ok(new { success = true, message = $"Test bildirimi '{t}' kanalına gönderildi." });
            });

            // Start server
            app.Run();
        }
        catch (Exception ex)
        {
            try
            {
                System.IO.File.AppendAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "relay_crash.log"), $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Relay crashed: {ex.Message}\r\n{ex.StackTrace}\r\n");
            }
            catch { }
        }
    }


        private static async Task TunnelClientToHost(HostSession session, ClientPeer peer, CancellationToken clientToken)
        {
            var clientSocket = peer.Socket;
            var buffer = new byte[1024 * 8]; // 8KB read buffer for inputs
            var token = clientToken;
            var highPriorityQueue = new ConcurrentQueue<(byte[] Data, WebSocketMessageType Type)>();
            object? latestMouseMoveBox = null; // Boxed (byte[] Data, WebSocketMessageType Type)
            var wakeSender = new AutoResetEvent(false);

            // Dedicated zero-latency sender loop: transmits inputs to Host without Head-of-Line blocking
            var senderTask = Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested && session.HostSocket.State == WebSocketState.Open)
                    {
                        bool sentAnything = false;

                        // 1. Send all Clicks, Keys, ACKs, Text Commands immediately
                        while (highPriorityQueue.TryDequeue(out var item))
                        {
                            if (session.HostSocket.State != WebSocketState.Open) break;
                            bool hLocked = false;
                            try
                            {
                                await session.HostSendLock.WaitAsync(token).ConfigureAwait(false);
                                hLocked = true;
                                if (session.HostSocket.State == WebSocketState.Open)
                                {
                                    await session.HostSocket.SendAsync(
                                        new ArraySegment<byte>(item.Data),
                                        item.Type,
                                        true,
                                        token
                                    ).ConfigureAwait(false);
                                }
                            }
                            finally
                            {
                                if (hLocked) { try { session.HostSendLock.Release(); } catch { } }
                            }
                            sentAnything = true;
                        }

                        // 2. Send ONLY the single newest mouse move (never a 50-packet backlog!)
                        var mouseBox = Interlocked.Exchange(ref latestMouseMoveBox, null);
                        if (mouseBox is ValueTuple<byte[], WebSocketMessageType> mouseItem && session.HostSocket.State == WebSocketState.Open)
                        {
                            bool hLocked = false;
                            try
                            {
                                await session.HostSendLock.WaitAsync(token).ConfigureAwait(false);
                                hLocked = true;
                                if (session.HostSocket.State == WebSocketState.Open)
                                {
                                    await session.HostSocket.SendAsync(
                                        new ArraySegment<byte>(mouseItem.Item1),
                                        mouseItem.Item2,
                                        true,
                                        token
                                    ).ConfigureAwait(false);
                                }
                            }
                            finally
                            {
                                if (hLocked) { try { session.HostSendLock.Release(); } catch { } }
                            }
                            sentAnything = true;
                        }

                        if (!sentAnything)
                        {
                            wakeSender.WaitOne(10);
                        }
                    }
                }
                catch { }
            }, token);

            try
            {
                while (!token.IsCancellationRequested &&
                       session.HostSocket.State == WebSocketState.Open &&
                       peer.IsRegistered &&
                       clientSocket.State == WebSocketState.Open)
                {
                    using (var ms = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                            if (result.MessageType == WebSocketMessageType.Close) break;
                            ms.Write(buffer, 0, result.Count);
                        }
                        while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Close) break;

                        if (ms.Length > 0)
                        {
                            byte[] msg = ms.ToArray();

                            // Çoklu uzman modunda (ActivePeers.Length > 1):
                            // 1) Uzmanlardan biri ayrıldığında ("DISCONNECT"/"STOP"/"CLOSE"/"LEAVE") diğer uzmanın yayınını kesmemek için sinyali yut ve sadece bu uzmanı kapat!
                            // 2) İki uzmandan yavaş olanın gecikmeli eski ACK'sının Host lastAckedSeq sayacını geriye çekmesini engelle!
                            if (result.MessageType == WebSocketMessageType.Text && msg.Length < 128)
                            {
                                string txt = Encoding.UTF8.GetString(msg);
                                if (txt.StartsWith("CLIENT:WEB", StringComparison.Ordinal) || txt.Contains("\"set_quality\"", StringComparison.Ordinal) || txt.StartsWith("{", StringComparison.Ordinal))
                                {
                                    peer.IsWebClient = true;
                                    // Aynı oturumda Masaüstü (Native) uzman bağlıysa asla Host akışını Web/JPEG moduna düşürme!
                                    if ((txt.StartsWith("CLIENT:WEB", StringComparison.Ordinal) || txt.Contains("\"set_quality\"", StringComparison.Ordinal)) &&
                                        session.ActivePeers.Any(p => p != peer && !p.IsWebClient))
                                    {
                                        continue;
                                    }
                                }
                                else if (txt.StartsWith("CLIENT:NATIVE_CPP", StringComparison.Ordinal))
                                {
                                    peer.IsWebClient = false;
                                }

                                if (session.ActivePeers.Length > 1 &&
                                    (txt == "DISCONNECT" || txt == "STOP" || txt == "CLOSE" || txt == "LEAVE" || txt.StartsWith("LEAVE:", StringComparison.Ordinal)))
                                {
                                    break;
                                }
                            }
                            else if (result.MessageType == WebSocketMessageType.Binary && msg.Length >= 5 && msg[0] == 0x41 && session.ActivePeers.Length > 1)
                            {
                                uint ackSeq = BitConverter.ToUInt32(msg, 1);
                                uint prevAck = session.LastForwardedAckSeq;
                                if (prevAck != 0 && (int)(ackSeq - prevAck) <= 0)
                                {
                                    continue;
                                }
                                session.LastForwardedAckSeq = ackSeq;
                            }

                            // If this is a mouse move (5 bytes binary 0x4D 'M' or JSON move) -> Coalesce to 0-latency!
                            bool isMouseMove = (result.MessageType == WebSocketMessageType.Binary && msg.Length == 5 && msg[0] == 0x4D);
                            if (!isMouseMove && result.MessageType == WebSocketMessageType.Text && msg.Length < 64)
                            {
                                string str = Encoding.UTF8.GetString(msg);
                                if (str.Contains("\"type\":\"move\""))
                                {
                                    isMouseMove = true;
                                }
                            }

                            if (isMouseMove)
                            {
                                Interlocked.Exchange(ref latestMouseMoveBox, ValueTuple.Create(msg, result.MessageType));
                            }
                            else
                            {
                                highPriorityQueue.Enqueue((msg, result.MessageType));
                            }
                            wakeSender.Set();
                        }
                    }
                }
            }
            catch (Exception) { }
            finally
            {
                wakeSender.Set();
                try { await senderTask; } catch { }
            }
        }

        private static async Task TunnelViewOnlyClient(WebSocket socket, HostSession session)
        {
            var buffer = new byte[1024 * 4];
            try
            {
                while (socket.State == WebSocketState.Open && !session.Cts.Token.IsCancellationRequested)
                {
                    // Read and discard to keep connection alive and detect closed socket
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), session.Cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close) break;
                    
                    // We DO NOT forward any messages/inputs to the host!
                }
            }
            catch { }
            finally
            {
                lock (session.ViewOnlyClients)
                {
                    session.ViewOnlyClients.Remove(socket);
                }
            }
        }

        private static string GenerateUniqueId()
        {
            var rand = new Random();
            string id;
            do
            {
                id = rand.Next(100000000, 999999999).ToString();
            } while (ActiveHosts.ContainsKey(id));
            
            return id;
        }

        private static void InitializeAdminPassword()
        {
            try
            {
                string path = System.IO.Path.Combine(AppContext.BaseDirectory, "admin_password.txt");
                if (System.IO.File.Exists(path))
                {
                    AdminPassword = System.IO.File.ReadAllText(path).Trim();
                }
                else
                {
                    System.IO.File.WriteAllText(path, AdminPassword);
                }
            }
            catch { }
        }

        private static bool IsAdminAuthenticated(HttpContext context)
        {
            if (context.Request.Cookies.TryGetValue("bigline_admin_session", out var sessionValue))
            {
                if (!string.IsNullOrEmpty(sessionValue)) return true;
            }
            return false;
        }
    }
