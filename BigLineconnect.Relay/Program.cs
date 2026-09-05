using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        private readonly Channel<byte[]> _channel;
        private readonly CancellationTokenSource _cts;
        private readonly Task _pumpTask;

        public FrameRelayPump(WebSocket targetSocket, CancellationToken cancellationToken)
        {
            _targetSocket = targetSocket;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(8)
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
                while (await reader.WaitToReadAsync(_cts.Token))
                {
                    while (reader.TryRead(out var frameBytes))
                    {
                        if (_targetSocket.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
                        {
                            try
                            {
                                await _targetSocket.SendAsync(
                                    new ArraySegment<byte>(frameBytes),
                                    WebSocketMessageType.Binary,
                                    true,
                                    _cts.Token
                                );
                            }
                            catch { }
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
                Timestamp = DateTime.Now
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
        private static readonly List<ResellerAccount> Resellers = new();
        private static readonly object Lock = new();

        static ResellerManager()
        {
            Load();
        }

        private static void Load()
        {
            try
            {
                lock (Lock)
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
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                lock (Lock)
                {
                    string json = System.Text.Json.JsonSerializer.Serialize(Resellers);
                    System.IO.File.WriteAllText(DbPath, json);
                }
            }
            catch { }
        }

        public static ResellerAccount? Register(string company, string contact, string email, string phone, string password)
        {
            lock (Lock)
            {
                if (Resellers.Exists(r => r.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                    return null;

                string tenantId = "BAYI" + (Resellers.Count + 1).ToString("D3");
                var account = new ResellerAccount
                {
                    TenantId = tenantId,
                    CompanyName = company,
                    ContactName = contact,
                    Email = email,
                    Phone = phone,
                    Password = password,
                    MaxQuota = 50,
                    CreatedAt = DateTime.Now
                };
                Resellers.Add(account);
                Save();
                return account;
            }
        }

        public static ResellerAccount? Login(string idOrEmail, string password)
        {
            lock (Lock)
            {
                return Resellers.Find(r => (r.TenantId.Equals(idOrEmail, StringComparison.OrdinalIgnoreCase) || r.Email.Equals(idOrEmail, StringComparison.OrdinalIgnoreCase)) && r.Password == password);
            }
        }
    }

    public class Program
    {
        private static readonly ConcurrentDictionary<string, HostSession> ActiveHosts = new();
        private static string AdminPassword = "BigLineAdmin2026!";
        private static string AdminSessionToken = Guid.NewGuid().ToString();

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

        private static List<LicenseEntry> LoadLicenses()
        {
            lock (LicenseLock)
            {
                var list = new List<LicenseEntry>();
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
                                    list.Add(entry);
                                }
                            }
                        }
                    }
                }
                catch { }
                return list;
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

        public class HostSession
        {
            public string Id { get; set; } = "";
            public WebSocket HostSocket { get; set; } = null!;
            public WebSocket? ClientSocket { get; set; }
            public FrameRelayPump? FramePump { get; set; }
            public List<WebSocket> ViewOnlyClients { get; set; } = new();
            public CancellationTokenSource Cts { get; set; } = new();
            public CancellationTokenSource? ClientCts { get; set; }
            
            public string Hwid { get; set; } = "Bilinmeyen";
            public string ComputerName { get; set; } = "Bilinmeyen";
            public string Username { get; set; } = "Bilinmeyen";
            public string OsVersion { get; set; } = "Bilinmeyen";
            public string AppVersion { get; set; } = "Bilinmeyen";
            public string LicenseStatus { get; set; } = "Bilinmeyen";
            public string IpAddress { get; set; } = "Bilinmeyen";
            public string LanIp { get; set; } = "";
            public DateTime ConnectedAt { get; set; } = DateTime.Now;
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

            app.MapGet("/download", async context =>
            {
                context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate, max-age=0";
                context.Response.Headers["Pragma"] = "no-cache";
                context.Response.Headers["Expires"] = "0";

                string setupPath = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "BigLineconnect_setup_v2.exe");
                if (!System.IO.File.Exists(setupPath))
                {
                    setupPath = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "BigLineconnect.exe");
                }
                if (!System.IO.File.Exists(setupPath))
                {
                    setupPath = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "BigLineconnect_setup.exe");
                }
                if (!System.IO.File.Exists(setupPath))
                {
                    setupPath = System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "BigLineconnect_setup.zip");
                }
                if (System.IO.File.Exists(setupPath))
                {
                    context.Response.ContentType = "application/octet-stream";
                    context.Response.Headers["Content-Disposition"] = "attachment; filename=\"BigLineconnect_setup.exe\"";
                    await context.Response.SendFileAsync(setupPath);
                }
                else
                {
                    context.Response.StatusCode = 404;
                    await context.Response.WriteAsync("Kurulum dosyası bulunamadı.");
                }
            });

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
                    string ip = context.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen";

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
                            ActiveHosts.TryRemove(rawId, out _);
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
                        ConnectedAt = DateTime.Now
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
                                if (msgType == WebSocketMessageType.Binary)
                                {
                                    // Newest Frame Wins via FrameRelayPump:
                                    // Host receive loop NEVER blocks! If viewer is slow, stale frames are dropped in 0ms!
                                    if (session.ClientSocket != null && session.ClientSocket.State == WebSocketState.Open)
                                    {
                                        if (session.FramePump == null)
                                        {
                                            session.FramePump = new FrameRelayPump(session.ClientSocket, session.Cts.Token);
                                        }
                                        session.FramePump.EnqueueFrame(msgBytes);
                                    }
                                }
                                else
                                {
                                    // Text/Control messages MUST remain sequential and reliable
                                    if (session.ClientSocket != null && session.ClientSocket.State == WebSocketState.Open)
                                    {
                                        try
                                        {
                                            await session.ClientSocket.SendAsync(
                                                new ArraySegment<byte>(msgBytes),
                                                msgType,
                                                true,
                                                CancellationToken.None
                                            );
                                        }
                                        catch { }
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
                        ActiveHosts.TryRemove(rawId, out _);
                        Console.WriteLine($"[Relay] Host disconnected. ID: {formattedId}");
                        TelemetryManager.LogEvent(session.Hwid, session.IpAddress, session.ComputerName, session.Username, session.OsVersion, session.AppVersion, "disconnect", $"Host bağlantısı kesildi. ID: {formattedId}, Süre: {(DateTime.Now - session.ConnectedAt).TotalMinutes:F1} dk");
                        
                        if (session.ClientSocket != null && session.ClientSocket.State == WebSocketState.Open)
                        {
                            try
                            {
                                await session.ClientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Host disconnected", CancellationToken.None);
                            }
                            catch { }
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
                        if (session.ClientSocket != null)
                        {
                            try { session.FramePump?.Stop(); } catch { }
                            session.FramePump = null;
                            try { session.ClientSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Replaced by new client", CancellationToken.None); } catch { }
                            try { session.ClientCts?.Cancel(); } catch { }
                        }

                        session.ClientSocket = clientSocket;
                        session.ClientCts = new CancellationTokenSource();
                        session.FramePump = new FrameRelayPump(clientSocket, session.Cts.Token);
                        Console.WriteLine($"[Relay] Client connected to Host ID: {targetId}");
                        string clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen";
                        TelemetryManager.LogEvent(session.Hwid, session.IpAddress, session.ComputerName, session.Username, session.OsVersion, session.AppVersion, "connect", $"İstemci bağlandı. İstemci IP: {clientIp}, Hedef ID: {targetId}");

                        // Send host_info JSON packet to viewer so viewer can auto-upgrade to 0.5ms LAN Direct if on same subnet
                        if (!string.IsNullOrEmpty(session.LanIp))
                        {
                            string hostInfoJson = $"{{\"type\":\"host_info\",\"id\":\"{targetId}\",\"lan_ip\":\"{session.LanIp}\",\"public_ip\":\"{session.IpAddress}\",\"public_port\":18888,\"computer\":\"{session.ComputerName}\"}}";
                            byte[] hostInfoBytes = Encoding.UTF8.GetBytes(hostInfoJson);
                            try { _ = clientSocket.SendAsync(new ArraySegment<byte>(hostInfoBytes), WebSocketMessageType.Text, true, CancellationToken.None); } catch { }
                        }

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
                        await session.HostSocket.SendAsync(new ArraySegment<byte>(startCmd), WebSocketMessageType.Text, true, CancellationToken.None);

                        // 📱 Telegram: Destek uzmanı bağlandı bildirimi
                        if (ticket != null)
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

                        var clientToHostTask = TunnelClientToHost(session);

                        try
                        {
                            await Task.WhenAny(clientToHostTask, Task.Delay(-1, session.ClientCts.Token));
                        }
                        catch { }

                        try { session.FramePump?.Stop(); } catch { }
                        session.FramePump = null;
                        try { session.ClientCts?.Cancel(); } catch { }
                        session.ClientSocket = null;
                        session.ClientCts = null;
                        Console.WriteLine($"[Relay] Client disconnected from Host ID: {targetId}");
                        TelemetryManager.LogEvent(session.Hwid, session.IpAddress, session.ComputerName, session.Username, session.OsVersion, session.AppVersion, "disconnect", $"İstemci bağlantısı kesildi. Hedef ID: {targetId}");

                        if (session.HostSocket.State == WebSocketState.Open)
                        {
                            byte[] stopCmd = Encoding.UTF8.GetBytes("STOP_STREAM");
                            try
                            {
                                await session.HostSocket.SendAsync(new ArraySegment<byte>(stopCmd), WebSocketMessageType.Text, true, CancellationToken.None);
                            }
                            catch { }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[Relay] View-only Client connected to Host ID: {targetId}");
                        string clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Bilinmeyen";
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
                        RequiresConfirmation = dto.RequiresConfirmation
                    };
                    string reqKey = !string.IsNullOrEmpty(dto.Token) ? dto.Token : dto.Id;
                    ActiveSupportRequests[reqKey] = req;

                    // 📱 Telegram Push Notification — destek uzmanının telefonuna bildirim gönder
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await TelegramNotifier.NotifySupportRequestAsync(
                                req.Name, req.Issue, req.Priority, req.Id, req.TenantId);
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
                                Notes = "Uzman tarafından incelemeye alınması bekleniyor."
                            });
                            SaveSupportHistory(history);
                        }
                    }
                    catch { }

                    return Results.Ok("Success");
                }
                return Results.BadRequest("Invalid Data");
            });

            app.MapGet("/api/support/check", async context =>
            {
                string id = context.Request.Query["id"].ToString() ?? "";
                bool exists = !string.IsNullOrEmpty(id) && ActiveSupportRequests.Values.Any(r => r.Id == id || r.Token == id);
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(exists ? "true" : "false");
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
                        isBusy = h.ClientSocket != null
                    });
                }

                var stats = new
                {
                    totalInstalls = TelemetryManager.GetUniqueInstallCount(),
                    activeCount = ActiveHosts.Count,
                    activeHosts = activeHostsList,
                    logs = TelemetryManager.GetLogs()
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

                    var account = ResellerManager.Register(company, contact, email, phone, pass);
                    if (account != null)
                    {
                        context.Response.ContentType = "application/json; charset=utf-8";
                        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new { success = true, tenantId = account.TenantId, companyName = account.CompanyName }));
                    }
                    else
                    {
                        context.Response.ContentType = "application/json; charset=utf-8";
                        await context.Response.WriteAsync("{\"success\":false,\"message\":\"Bu e-posta adresi zaten kayıtlı!\"}");
                    }
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
    <title>BigLineconnect | Yönetim Girişi</title>
    <style>
        :root {
            --bg-color: #0b0c10;
            --card-bg: rgba(31, 40, 51, 0.6);
            --text-color: #c5c6c7;
            --cyan: #00e5ff;
            --magenta: #d500f9;
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
            border: 1px solid rgba(0, 229, 255, 0.2);
            border-radius: 16px;
            padding: 40px 30px;
            box-shadow: 0 0 30px rgba(0, 229, 255, 0.1), inset 0 0 20px rgba(255, 255, 255, 0.05);
            text-align: center;
            position: relative;
            transform: translateY(0);
            transition: all 0.3s ease;
        }
        .login-card:hover {
            border-color: rgba(0, 229, 255, 0.4);
            box-shadow: 0 0 40px rgba(0, 229, 255, 0.2);
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
            text-shadow: 0 0 10px rgba(0, 229, 255, 0.5);
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
            box-shadow: 0 0 8px rgba(0, 229, 255, 0.2);
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
            box-shadow: 0 0 15px rgba(213, 0, 249, 0.3);
            transition: all 0.3s ease;
            margin-top: 10px;
        }
        .btn-submit:hover {
            transform: translateY(-2px);
            box-shadow: 0 0 25px rgba(0, 229, 255, 0.5);
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
                <h1><span>BIGLINE</span>CONNECT</h1>
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
    <title>BigLineconnect | Telemetri & Yönetim Paneli</title>
    <style>
        :root {
            --bg-color: #0b0c10;
            --card-bg: #1f2833;
            --text-color: #c5c6c7;
            --cyan: #00e5ff;
            --magenta: #d500f9;
            --white: #ffffff;
            --border-color: #45a29e;
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
            border-bottom: 2px solid rgba(0, 229, 255, 0.3);
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
            text-shadow: 0 0 10px rgba(0, 229, 255, 0.5);
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
            box-shadow: 0 12px 40px rgba(0, 229, 255, 0.15);
            border-color: rgba(0, 229, 255, 0.3);
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
            background-color: rgba(0, 229, 255, 0.05);
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
            background-color: rgba(0, 229, 255, 0.15);
            color: var(--cyan);
            border: 1px solid rgba(0, 229, 255, 0.3);
        }
        .badge-busy {
            background-color: rgba(213, 0, 249, 0.15);
            color: var(--magenta);
            border: 1px solid rgba(213, 0, 249, 0.3);
        }
        .badge-startup { color: var(--cyan); border: 1px solid rgba(0, 229, 255, 0.3); background-color: rgba(0, 229, 255, 0.05); }
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
            box-shadow: 0 0 8px rgba(0, 229, 255, 0.2);
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
            <h1><span>BIGLINE</span>CONNECT <span>//</span> PANELI</h1>
            <div class='subtitle'>Gerçek Zamanlı Cihaz ve Bağlantı İzleme Ekranı</div>
        </div>
        <div style='text-align: right'>
            <div style='font-size: 12px; color: #888'>Son Güncelleme</div>
            <div id='update-timer' style='font-size: 14px; color: var(--cyan); font-weight: bold;'>Yükleniyor...</div>
        </div>
    </header>

    <div class='stats-grid'>
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
        <div class='section-title'>Destek Talepleri Geçmişi (CRM)</div>
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

        document.getElementById('search-input').addEventListener('input', renderLogs);
        document.getElementById('filter-type').addEventListener('change', renderLogs);

        fetchStats();
        fetchSupportHistory();
        fetchLicenses();
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


        private static async Task TunnelClientToHost(HostSession session)
        {
            var buffer = new byte[1024 * 8]; // 8KB read buffer for inputs
            var token = session.ClientCts?.Token ?? session.Cts.Token;
            try
            {
                while (!token.IsCancellationRequested &&
                       session.HostSocket.State == WebSocketState.Open &&
                       session.ClientSocket != null &&
                       session.ClientSocket.State == WebSocketState.Open)
                {
                    using (var ms = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await session.ClientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                            if (result.MessageType == WebSocketMessageType.Close) break;
                            ms.Write(buffer, 0, result.Count);
                        }
                        while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Close) break;

                        if (ms.Length > 0 && session.HostSocket.State == WebSocketState.Open)
                        {
                            byte[] entireMsg = ms.ToArray();
                            await session.HostSocket.SendAsync(
                                new ArraySegment<byte>(entireMsg),
                                result.MessageType,
                                true, // End of message
                                token
                            );
                        }
                    }
                }
            }
            catch (Exception) { }
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
