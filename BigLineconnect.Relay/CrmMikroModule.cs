using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

public static class CrmMikroModule
{
    public class CrmSecurityConfig
    {
        public bool TwoFactorEnabled { get; set; } = false;
        public string TotpSecret { get; set; } = "";
        public string PendingSecret { get; set; } = "";
        public string EnabledAt { get; set; } = "";
    }
    public class CrmCategory
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Color { get; set; } = "#f59e0b";
    }

    public class CrmChannel
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Note { get; set; } = "";
        public bool Active { get; set; } = true;
    }

    public class CrmCompany
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "Kurumsal";
        // "Bakım Anlaşmalı" | "Ücretli" | "Ücretsiz"
        public string ServiceType { get; set; } = "Bakım Anlaşmalı";
        // ISO yyyy-MM-dd (e.g. "2027-12-31")
        public string ContractEndDate { get; set; } = "";
        public decimal DefaultFee { get; set; } = 0;
        public string HostIds { get; set; } = "";

        // Fatura & Mikro Eşleştirme Bilgileri
        public string MikroCariKod { get; set; } = "";
        public string InvoiceTitle { get; set; } = "";
        public string TaxOffice { get; set; } = "";
        public string TaxNumber { get; set; } = "";
        public string InvoiceAddress { get; set; } = "";
        public string ContactPerson { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public string DefaultServiceCode { get; set; } = "HZM-DESTEK-01";
        public int VatRate { get; set; } = 20;
        public string Notes { get; set; } = "";
        public string CreatedAt { get; set; } = "";
        public string UpdatedAt { get; set; } = "";
    }

    public class CrmSupportRecord
    {
        public string Id { get; set; } = "";
        public string CompanyId { get; set; } = "";
        public string CompanyName { get; set; } = "";
        public string Category { get; set; } = "";
        public string ServiceType { get; set; } = "";
        public string HostId { get; set; } = "";
        public string OperatorName { get; set; } = "";
        public string Issue { get; set; } = "";
        public string SolutionNotes { get; set; } = "";
        public int DurationMinutes { get; set; } = 15;
        public string Status { get; set; } = "Çözüldü"; // "Açık" | "Çözüldü" | "İptal"

        // Ücretlendirme & Mikro Hizmet Satış Faturası
        public bool IsBillable { get; set; } = false;
        public decimal NetAmount { get; set; } = 0;
        public int VatRate { get; set; } = 20;
        public decimal VatAmount { get; set; } = 0;
        public decimal TotalAmount { get; set; } = 0;
        public string MikroCariKod { get; set; } = "";
        public string MikroServiceCode { get; set; } = "HZM-DESTEK-01";
        public string MikroInvoiceStatus { get; set; } = "Gerekmiyor"; // Gerekmiyor | Bekliyor | Simüle Edildi (Test) | Mikro'ya Aktarıldı | Hata
        public string MikroInvoiceNo { get; set; } = "";
        public string MikroPayloadPreview { get; set; } = "";
        public string MikroError { get; set; } = "";

        public string CreatedAt { get; set; } = "";
        public string ClosedAt { get; set; } = "";
    }

    public class MikroConfig
    {
        // "Simulation" | "Api" | "Sql"
        public string Mode { get; set; } = "Simulation";
        public string ApiBaseUrl { get; set; } = "http://127.0.0.1:8094/Api/APIMethods/FaturaKaydetV2";
        public string ApiKey { get; set; } = "";
        public string FirmaKodu { get; set; } = "BIGUS";
        public int CalismaYili { get; set; } = 2026;
        public int SubeNo { get; set; } = 0;
        public int KullaniciNo { get; set; } = 1;
        public string EvrakSeri { get; set; } = "HZM";
        public int LastSimulatedSeq { get; set; } = 1000;
        public string DefaultHizmetKodu { get; set; } = "HZM-DESTEK-01";
        public int DefaultKdvOrani { get; set; } = 20;
        public string SqlConnectionString { get; set; } = "";
    }

    private static readonly object _lock = new();
    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly string SqlConnStr = Environment.GetEnvironmentVariable("BCONNECT_SQL_CONN")
        ?? "Server=213.142.159.18;Database=BigusDb;User Id=sa;Password=Bm1453;Encrypt=False;Connection Timeout=8;";
    private static volatile bool _sqlTableEnsured = false;
    private static volatile bool _sqlHealthy = false;
    private static readonly Dictionary<string, string> _memCache = new(StringComparer.OrdinalIgnoreCase);

    private static string NormalizeTenant(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "BIGLINE";
        var sb = new StringBuilder();
        foreach (char ch in raw.Trim().ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_') sb.Append(ch);
        }
        string t = sb.ToString();
        return string.IsNullOrEmpty(t) ? "BIGLINE" : (t.Length > 32 ? t.Substring(0, 32) : t);
    }

    private static string ResolveTenant(HttpContext ctx)
    {
        string? q = ctx.Request.Query["tenantId"].ToString();
        if (string.IsNullOrWhiteSpace(q)) q = ctx.Request.Headers["X-Tenant-Id"].ToString();
        return NormalizeTenant(q);
    }

    private static string TenantDataDir(string tenant)
    {
        string t = NormalizeTenant(tenant);
        string dir = Path.Combine(AppContext.BaseDirectory, "crm_data", t);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        return dir;
    }

    private static string StoreKeyFor(string tenant, string kind) => $"{NormalizeTenant(tenant)}:{kind}";
    private static string CategoriesPath(string t) => Path.Combine(TenantDataDir(t), "crm_categories.json");
    private static string CompaniesPath(string t) => Path.Combine(TenantDataDir(t), "crm_companies.json");
    private static string RecordsPath(string t) => Path.Combine(TenantDataDir(t), "crm_records.json");
    private static string MikroConfigPath(string t) => Path.Combine(TenantDataDir(t), "crm_mikro_config.json");
    private static string SecurityPath(string t) => Path.Combine(TenantDataDir(t), "crm_security.json");
    private static string ChannelsPath(string t) => Path.Combine(TenantDataDir(t), "crm_channels.json");

    public static List<CrmChannel> GetDealerChannels(string tenantId)
    {
        string t = NormalizeTenant(tenantId);
        return LoadJson(StoreKeyFor(t, "channels"), ChannelsPath(t), () => new List<CrmChannel>());
    }

    private static CrmSecurityConfig LoadSecurity(string tenant) =>
        LoadJson(StoreKeyFor(tenant, "security"), SecurityPath(tenant), () => new CrmSecurityConfig());

    // Pure RFC 6238 TOTP (Google Authenticator / Microsoft Authenticator uyumlu — 0 TL, sıfır dış kütüphane)
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    private static string GenerateBase32Secret(int byteLen = 20)
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(byteLen);
        var sb = new StringBuilder();
        int bits = 0, value = 0;
        foreach (byte b in bytes)
        {
            value = (value << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                sb.Append(Base32Alphabet[(value >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }
        if (bits > 0) sb.Append(Base32Alphabet[(value << (5 - bits)) & 31]);
        return sb.ToString();
    }

    private static byte[] DecodeBase32(string base32)
    {
        string clean = (base32 ?? "").Trim().Replace(" ", "").Replace("-", "").ToUpperInvariant();
        var output = new List<byte>();
        int bits = 0, value = 0;
        foreach (char c in clean)
        {
            int idx = Base32Alphabet.IndexOf(c);
            if (idx < 0) continue;
            value = (value << 5) | idx;
            bits += 5;
            if (bits >= 8)
            {
                output.Add((byte)((value >> (bits - 8)) & 0xFF));
                bits -= 8;
            }
        }
        return output.ToArray();
    }

    private static string ComputeTotpAtStep(string base32Secret, long timeStep)
    {
        byte[] key = DecodeBase32(base32Secret);
        if (key.Length == 0) return "";
        byte[] counter = new byte[8];
        for (int i = 7; i >= 0; i--) { counter[i] = (byte)(timeStep & 0xFF); timeStep >>= 8; }
        using var hmac = new HMACSHA1(key);
        byte[] hash = hmac.ComputeHash(counter);
        int offset = hash[hash.Length - 1] & 0x0F;
        int binary = ((hash[offset] & 0x7F) << 24)
                   | ((hash[offset + 1] & 0xFF) << 16)
                   | ((hash[offset + 2] & 0xFF) << 8)
                   | (hash[offset + 3] & 0xFF);
        int otp = binary % 1000000;
        return otp.ToString("D6");
    }

    private static bool VerifyTotpCode(string base32Secret, string? code)
    {
        if (string.IsNullOrWhiteSpace(base32Secret)) return false;
        string cleanCode = new string((code ?? "").Where(char.IsDigit).ToArray());
        if (cleanCode.Length != 6) return false;
        long currentStep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30L;
        // ±1 adım (30 sn) saat farkı toleransı
        for (long delta = -1; delta <= 1; delta++)
        {
            if (ComputeTotpAtStep(base32Secret, currentStep + delta) == cleanCode)
                return true;
        }
        return false;
    }

    private static List<string> ListKnownTenants()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "BIGLINE" };
        try
        {
            string root = Path.Combine(AppContext.BaseDirectory, "crm_data");
            if (Directory.Exists(root))
            {
                foreach (var d in Directory.GetDirectories(root))
                    set.Add(NormalizeTenant(Path.GetFileName(d)));
            }
        }
        catch { }
        try
        {
            using var conn = new System.Data.SqlClient.SqlConnection(SqlConnStr);
            conn.Open();
            EnsureCrmSqlTable(conn);
            using var cmd = new System.Data.SqlClient.SqlCommand("SELECT StoreKey FROM BConnect_CrmStore", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                string k = r.IsDBNull(0) ? "" : r.GetString(0);
                int idx = k.IndexOf(':');
                if (idx > 0) set.Add(NormalizeTenant(k.Substring(0, idx)));
            }
        }
        catch { }
        return set.OrderBy(x => x == "BIGLINE" ? "" : x).ToList();
    }

    private static string NowTr() => DateTime.UtcNow.AddHours(3).ToString("dd.MM.yyyy HH:mm:ss");

    private static void EnsureCrmSqlTable(System.Data.SqlClient.SqlConnection conn)
    {
        if (_sqlTableEnsured) return;
        string ddl = @"
        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BConnect_CrmStore')
        BEGIN
            CREATE TABLE BConnect_CrmStore (
                StoreKey NVARCHAR(64) NOT NULL PRIMARY KEY,
                JsonData NVARCHAR(MAX) NOT NULL,
                UpdatedAt NVARCHAR(64) NOT NULL
            );
        END";
        using var cmd = new System.Data.SqlClient.SqlCommand(ddl, conn);
        cmd.ExecuteNonQuery();
        _sqlTableEnsured = true;
    }

    private static string? TryLoadFromSql(string storeKey)
    {
        try
        {
            using var conn = new System.Data.SqlClient.SqlConnection(SqlConnStr);
            conn.Open();
            EnsureCrmSqlTable(conn);
            _sqlHealthy = true;
            using var cmd = new System.Data.SqlClient.SqlCommand("SELECT JsonData FROM BConnect_CrmStore WHERE StoreKey = @k", conn);
            cmd.Parameters.AddWithValue("@k", storeKey);
            var obj = cmd.ExecuteScalar();
            if (obj != null && obj != DBNull.Value)
                return obj.ToString();
        }
        catch { }
        return null;
    }

    private static void SaveToSqlAsync(string storeKey, string json)
    {
        string now = NowTr();
        _ = Task.Run(() =>
        {
            try
            {
                using var conn = new System.Data.SqlClient.SqlConnection(SqlConnStr);
                conn.Open();
                EnsureCrmSqlTable(conn);
                _sqlHealthy = true;
                string upsert = @"
                IF EXISTS (SELECT 1 FROM BConnect_CrmStore WHERE StoreKey = @k)
                    UPDATE BConnect_CrmStore SET JsonData = @j, UpdatedAt = @u WHERE StoreKey = @k;
                ELSE
                    INSERT INTO BConnect_CrmStore (StoreKey, JsonData, UpdatedAt) VALUES (@k, @j, @u);";
                using var cmd = new System.Data.SqlClient.SqlCommand(upsert, conn);
                cmd.Parameters.AddWithValue("@k", storeKey);
                cmd.Parameters.AddWithValue("@j", json);
                cmd.Parameters.AddWithValue("@u", now);
                cmd.ExecuteNonQuery();
            }
            catch { }
        });
    }

    private static T LoadJson<T>(string storeKey, string path, Func<T> defaultFactory)
    {
        lock (_lock)
        {
            try
            {
                // 1) Hızlı bellek önbelleği
                if (_memCache.TryGetValue(storeKey, out var cachedJson) && !string.IsNullOrWhiteSpace(cachedJson))
                {
                    var cv = JsonSerializer.Deserialize<T>(cachedJson, _jsonOpts);
                    if (cv != null) return cv;
                }

                // 2) Soğuk başlangıçta (Railway yeniden başlatma dahil) önce kalıcı SQL Server (BigusDb) kontrolü
                string? sqlJson = TryLoadFromSql(storeKey);
                if (!string.IsNullOrWhiteSpace(sqlJson))
                {
                    var sv = JsonSerializer.Deserialize<T>(sqlJson, _jsonOpts);
                    if (sv != null)
                    {
                        _memCache[storeKey] = sqlJson;
                        try { File.WriteAllText(path, sqlJson, Encoding.UTF8); } catch { }
                        return sv;
                    }
                }

                // 3) Yerel JSON yedek
                if (File.Exists(path))
                {
                    string txt = File.ReadAllText(path, Encoding.UTF8);
                    if (!string.IsNullOrWhiteSpace(txt))
                    {
                        var val = JsonSerializer.Deserialize<T>(txt, _jsonOpts);
                        if (val != null)
                        {
                            _memCache[storeKey] = txt;
                            SaveToSqlAsync(storeKey, txt);
                            return val;
                        }
                    }
                }
            }
            catch { }
            var def = defaultFactory();
            SaveJson(storeKey, path, def);
            return def;
        }
    }

    private static void SaveJson<T>(string storeKey, string path, T data)
    {
        lock (_lock)
        {
            try
            {
                string json = JsonSerializer.Serialize(data, _jsonOpts);
                _memCache[storeKey] = json;
                File.WriteAllText(path, json, Encoding.UTF8);
                SaveToSqlAsync(storeKey, json);
            }
            catch { }
        }
    }

    private static List<CrmCategory> LoadCategories(string tenant) => LoadJson(StoreKeyFor(tenant, "categories"), CategoriesPath(tenant), () => new List<CrmCategory>
    {
        new() { Id = "cat-vip", Name = "VIP", Color = "#f59e0b" },
        new() { Id = "cat-kurumsal", Name = "Kurumsal", Color = "#3b82f6" },
        new() { Id = "cat-kobi", Name = "KOBİ", Color = "#10b981" },
        new() { Id = "cat-bayi", Name = "Bayi", Color = "#a855f7" },
        new() { Id = "cat-muhasebe", Name = "Muhasebe / Mali Müşavir", Color = "#06b6d4" },
        new() { Id = "cat-uretim", Name = "Üretim / Sanayi", Color = "#ef4444" }
    });

    private static List<CrmCompany> LoadCompanies(string tenant) => LoadJson(StoreKeyFor(tenant, "companies"), CompaniesPath(tenant), () => new List<CrmCompany>());
    private static List<CrmSupportRecord> LoadRecords(string tenant) => LoadJson(StoreKeyFor(tenant, "records"), RecordsPath(tenant), () => new List<CrmSupportRecord>());
    private static MikroConfig LoadMikroConfig(string tenant) => LoadJson(StoreKeyFor(tenant, "mikro_config"), MikroConfigPath(tenant), () => new MikroConfig { FirmaKodu = NormalizeTenant(tenant) });

    private static (string ContractBadge, string ContractColor, int? DaysRemaining, bool IsExpired) EvaluateContract(CrmCompany c)
    {
        if (string.Equals(c.ServiceType, "Ücretli", StringComparison.OrdinalIgnoreCase))
            return ("Ücretli Destek", "#f97316", null, false);

        if (string.Equals(c.ServiceType, "Ücretsiz", StringComparison.OrdinalIgnoreCase))
            return ("Ücretsiz / Garanti", "#38bdf8", null, false);

        // Bakım Anlaşmalı
        if (DateTime.TryParseExact(c.ContractEndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
        {
            var today = DateTime.UtcNow.AddHours(3).Date;
            int days = (endDate.Date - today).Days;
            if (days < 0)
                return ($"Süresi Doldu ({endDate:dd.MM.yyyy})", "#ef4444", days, true);
            if (days <= 30)
                return ($"Bitiyor ({days} gün — {endDate:dd.MM.yyyy})", "#f59e0b", days, false);
            return ($"Bakım Anlaşmalı ({endDate:dd.MM.yyyy})", "#10b981", days, false);
        }

        return ("Bakım Anlaşmalı (Tarihsiz)", "#10b981", null, false);
    }

    private static string BuildMikroServiceInvoicePayload(CrmSupportRecord rec, CrmCompany? comp, MikroConfig cfg, int seqNo)
    {
        string tarih = DateTime.UtcNow.AddHours(3).ToString("dd.MM.yyyy");
        string cariKod = !string.IsNullOrWhiteSpace(rec.MikroCariKod) ? rec.MikroCariKod : (comp?.MikroCariKod ?? "");
        string hizmetKod = !string.IsNullOrWhiteSpace(rec.MikroServiceCode) ? rec.MikroServiceCode : cfg.DefaultHizmetKodu;
        string aciklama = $"B-Connect Uzak Destek ({rec.DurationMinutes} dk) - {rec.Issue}";
        if (aciklama.Length > 120) aciklama = aciklama.Substring(0, 120);

        var mikroObj = new
        {
            Mikro = new
            {
                FirmaKodu = cfg.FirmaKodu,
                CalismaYili = cfg.CalismaYili,
                ApiKey = string.IsNullOrEmpty(cfg.ApiKey) ? "(API_KEY_TANIMSIZ)" : "***GIZLI***",
                KullaniciNo = cfg.KullaniciNo,
                SubeNo = cfg.SubeNo,
                EvrakTipi = "HizmetSatisFaturasi",
                Evrak = new
                {
                    cha_evrakno_seri = cfg.EvrakSeri,
                    cha_evrakno_sira = seqNo,
                    cha_tarihi = tarih,
                    cha_kod = cariKod,
                    cha_unvan = comp?.InvoiceTitle ?? rec.CompanyName,
                    cha_vergi_dairesi = comp?.TaxOffice ?? "",
                    cha_vergi_no = comp?.TaxNumber ?? "",
                    cha_aciklama = aciklama,
                    cha_kdv_orani = rec.VatRate,
                    cha_meblag_net = rec.NetAmount,
                    cha_vergi_tutar = rec.VatAmount,
                    cha_toplam_tutar = rec.TotalAmount,
                    satirlar = new[]
                    {
                        new
                        {
                            hizmet_kodu = hizmetKod,
                            miktar = 1,
                            birim_fiyat = rec.NetAmount,
                            kdv_orani = rec.VatRate,
                            aciklama = aciklama
                        }
                    }
                }
            }
        };

        return JsonSerializer.Serialize(mikroObj, _jsonOpts);
    }

    private static async Task<(bool Ok, string Status, string InvoiceNo, string Payload, string Error)> DispatchToMikroAsync(string tenant, CrmSupportRecord rec, CrmCompany? comp)
    {
        var cfg = LoadMikroConfig(tenant);
        string cariKod = !string.IsNullOrWhiteSpace(rec.MikroCariKod) ? rec.MikroCariKod : (comp?.MikroCariKod ?? "");
        if (string.IsNullOrWhiteSpace(cariKod))
        {
            return (false, "Hata", "", "", "Müşteri kartında 'Mikro Cari Kodu' tanımlı değil! Lütfen firma kartından Mikro Cari Kodu girin.");
        }
        if (rec.NetAmount <= 0)
        {
            return (false, "Hata", "", "", "Fatura tutarı 0 TL'den büyük olmalıdır.");
        }

        int seqNo;
        lock (_lock)
        {
            cfg.LastSimulatedSeq++;
            seqNo = cfg.LastSimulatedSeq;
            SaveJson(StoreKeyFor(tenant, "mikro_config"), MikroConfigPath(tenant), cfg);
        }

        string invoiceNo = $"{cfg.EvrakSeri}-{cfg.CalismaYili}-{seqNo:D5}";
        string previewPayload = BuildMikroServiceInvoicePayload(rec, comp, cfg, seqNo);

        if (string.Equals(cfg.Mode, "Simulation", StringComparison.OrdinalIgnoreCase))
        {
            return (true, "Simüle Edildi (Test)", invoiceNo, previewPayload, "");
        }

        if (string.Equals(cfg.Mode, "Api", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(cfg.ApiBaseUrl))
                return (false, "Hata", "", previewPayload, "Mikro API URL tanımlı değil.");

            try
            {
                string realPayload = previewPayload.Replace("***GIZLI***", cfg.ApiKey ?? "");
                using var req = new HttpRequestMessage(HttpMethod.Post, cfg.ApiBaseUrl);
                req.Content = new StringContent(realPayload, Encoding.UTF8, "application/json");
                if (!string.IsNullOrWhiteSpace(cfg.ApiKey))
                    req.Headers.TryAddWithoutValidation("X-API-Key", cfg.ApiKey);

                using var res = await _http.SendAsync(req);
                string body = await res.Content.ReadAsStringAsync();
                if (res.IsSuccessStatusCode)
                    return (true, "Mikro'ya Aktarıldı", invoiceNo, previewPayload, "");

                return (false, "Hata", "", previewPayload, $"Mikro API HTTP {(int)res.StatusCode}: {(body.Length > 200 ? body.Substring(0, 200) : body)}");
            }
            catch (Exception ex)
            {
                return (false, "Hata", "", previewPayload, $"Mikro API bağlantı hatası: {ex.Message}");
            }
        }

        if (string.Equals(cfg.Mode, "Sql", StringComparison.OrdinalIgnoreCase))
        {
            // Güvenlik gereği SQL modunda şimdilik sadece güvenli önizleme ve bağlantı doğrulaması yapılır
            if (string.IsNullOrWhiteSpace(cfg.SqlConnectionString))
                return (false, "Hata", "", previewPayload, "SQL bağlantı cümlesi tanımlı değil.");

            return (true, "Simüle Edildi (Test)", invoiceNo, previewPayload + "\n\n-- SQL Modu: Canlı Mikro tablo yazımı onay sonrası aktif edilecektir.", "");
        }

        return (false, "Hata", "", previewPayload, "Bilinmeyen Mikro çalışma modu.");
    }

    public static HashSet<string> GetHostIdsForTenant(string? tenantId)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string t = NormalizeTenant(tenantId);
        foreach (var c in LoadCompanies(t))
        {
            foreach (var raw in (c.HostIds ?? "").Split(new[] { ',', ';', ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string digits = new string(raw.Where(char.IsDigit).ToArray());
                if (!string.IsNullOrEmpty(digits)) set.Add(digits);
            }
        }
        return set;
    }

    public static int GetRecordCountForTenant(string? tenantId)
    {
        return LoadRecords(NormalizeTenant(tenantId)).Count;
    }

    public static void MapEndpoints(WebApplication app)
    {
        // Serve /crm page cleanly
        app.MapGet("/crm", async context =>
        {
            string crmFile = Path.Combine(AppContext.BaseDirectory, "wwwroot", "crm.html");
            if (!File.Exists(crmFile))
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("crm.html not found");
                return;
            }
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync(crmFile);
        });

        // List known tenants (for Super Admin selector)
        app.MapGet("/api/crm/tenants", () => Results.Ok(new { tenants = ListKnownTenants() }));

        // --- İSTEĞE BAĞLI 2FA (GOOGLE AUTHENTICATOR / TOTP) ---
        app.MapGet("/api/crm/2fa/status", (HttpContext ctx) =>
        {
            string tenant = ResolveTenant(ctx);
            var sec = LoadSecurity(tenant);
            return Results.Ok(new { tenantId = tenant, enabled = sec.TwoFactorEnabled, enabledAt = sec.EnabledAt });
        });

        app.MapPost("/api/crm/2fa/setup", (HttpContext ctx) =>
        {
            string tenant = ResolveTenant(ctx);
            var sec = LoadSecurity(tenant);
            string pending = GenerateBase32Secret(20);
            sec.PendingSecret = pending;
            SaveJson(StoreKeyFor(tenant, "security"), SecurityPath(tenant), sec);
            string label = Uri.EscapeDataString($"B-Connect CRM ({tenant})");
            string otpAuthUri = $"otpauth://totp/{label}?secret={pending}&issuer=B-Connect&digits=6&period=30";
            return Results.Ok(new { success = true, secret = pending, otpAuthUri, enabled = sec.TwoFactorEnabled });
        });

        app.MapPost("/api/crm/2fa/enable", async (HttpContext ctx) =>
        {
            string tenant = ResolveTenant(ctx);
            using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
            string code = doc.RootElement.TryGetProperty("code", out var p) ? p.GetString() ?? "" : "";
            var sec = LoadSecurity(tenant);
            if (string.IsNullOrWhiteSpace(sec.PendingSecret))
                return Results.BadRequest(new { success = false, error = "Önce QR kod oluşturun." });
            if (!VerifyTotpCode(sec.PendingSecret, code))
                return Results.Ok(new { success = false, error = "6 haneli doğrulama kodu eşleşmedi. Telefon saatinizi kontrol edip tekrar deneyin." });

            sec.TotpSecret = sec.PendingSecret;
            sec.PendingSecret = "";
            sec.TwoFactorEnabled = true;
            sec.EnabledAt = NowTr();
            SaveJson(StoreKeyFor(tenant, "security"), SecurityPath(tenant), sec);
            return Results.Ok(new { success = true, enabled = true, enabledAt = sec.EnabledAt });
        });

        app.MapPost("/api/crm/2fa/verify", async (HttpContext ctx) =>
        {
            string tenant = ResolveTenant(ctx);
            using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
            string code = doc.RootElement.TryGetProperty("code", out var p) ? p.GetString() ?? "" : "";
            var sec = LoadSecurity(tenant);
            if (!sec.TwoFactorEnabled || string.IsNullOrWhiteSpace(sec.TotpSecret))
                return Results.Ok(new { success = true });
            bool ok = VerifyTotpCode(sec.TotpSecret, code);
            return Results.Ok(new { success = ok, error = ok ? "" : "6 haneli doğrulama kodu hatalı!" });
        });

        app.MapPost("/api/crm/2fa/disable", async (HttpContext ctx) =>
        {
            string tenant = ResolveTenant(ctx);
            using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
            string code = doc.RootElement.TryGetProperty("code", out var p) ? p.GetString() ?? "" : "";
            var sec = LoadSecurity(tenant);
            if (sec.TwoFactorEnabled && !VerifyTotpCode(sec.TotpSecret, code))
                return Results.Ok(new { success = false, error = "Kapatmak için mevcut 6 haneli doğrulama kodunu girin." });
            sec.TwoFactorEnabled = false;
            sec.TotpSecret = "";
            sec.PendingSecret = "";
            sec.EnabledAt = "";
            SaveJson(StoreKeyFor(tenant, "security"), SecurityPath(tenant), sec);
            return Results.Ok(new { success = true, enabled = false });
        });

        // Overview: categories + enriched companies + records + mikro config + KPIs (strictly scoped to tenantId)
        app.MapGet("/api/crm/overview", (HttpContext ctx) =>
        {
            string tenant = ResolveTenant(ctx);
            var categories = LoadCategories(tenant);
            var companies = LoadCompanies(tenant);
            var records = LoadRecords(tenant).OrderByDescending(r => r.CreatedAt).ToList();
            var mikro = LoadMikroConfig(tenant);

            var enrichedCompanies = companies.Select(c =>
            {
                var eval = EvaluateContract(c);
                int supportCount = records.Count(r => r.CompanyId == c.Id);
                decimal totalBilled = records.Where(r => r.CompanyId == c.Id && r.IsBillable).Sum(r => r.TotalAmount);
                return new
                {
                    c.Id,
                    c.Name,
                    c.Category,
                    c.ServiceType,
                    c.ContractEndDate,
                    c.DefaultFee,
                    c.HostIds,
                    c.MikroCariKod,
                    c.InvoiceTitle,
                    c.TaxOffice,
                    c.TaxNumber,
                    c.InvoiceAddress,
                    c.ContactPerson,
                    c.Phone,
                    c.Email,
                    c.DefaultServiceCode,
                    c.VatRate,
                    c.Notes,
                    c.CreatedAt,
                    c.UpdatedAt,
                    ContractBadge = eval.ContractBadge,
                    ContractColor = eval.ContractColor,
                    DaysRemaining = eval.DaysRemaining,
                    IsExpired = eval.IsExpired,
                    SupportCount = supportCount,
                    TotalBilled = totalBilled
                };
            }).OrderBy(x => x.Name).ToList();

            var kpis = new
            {
                totalCompanies = companies.Count,
                activeContracts = enrichedCompanies.Count(x => x.ServiceType == "Bakım Anlaşmalı" && !x.IsExpired),
                expiringSoon = enrichedCompanies.Count(x => x.ServiceType == "Bakım Anlaşmalı" && x.DaysRemaining.HasValue && x.DaysRemaining.Value >= 0 && x.DaysRemaining.Value <= 30),
                expiredContracts = enrichedCompanies.Count(x => x.IsExpired),
                paidCompanies = companies.Count(x => x.ServiceType == "Ücretli"),
                freeCompanies = companies.Count(x => x.ServiceType == "Ücretsiz"),
                totalRecords = records.Count,
                billableRecords = records.Count(x => x.IsBillable),
                totalRevenue = records.Where(x => x.IsBillable).Sum(x => x.TotalAmount),
                mikroSyncedCount = records.Count(x => x.MikroInvoiceStatus == "Mikro'ya Aktarıldı" || x.MikroInvoiceStatus == "Simüle Edildi (Test)")
            };

            return Results.Ok(new
            {
                tenantId = tenant,
                categories,
                companies = enrichedCompanies,
                records,
                mikroConfig = mikro,
                kpis,
                sqlPersistent = _sqlHealthy
            });
        });

        // Smart Host ID Lookup (strictly scoped to tenantId)
        app.MapGet("/api/crm/lookup", (HttpContext ctx, string? hostId) =>
        {
            string tenant = ResolveTenant(ctx);
            string clean = new string((hostId ?? "").Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(clean)) return Results.Ok(new { found = false });

            var companies = LoadCompanies(tenant);
            var match = companies.FirstOrDefault(c =>
                (c.HostIds ?? "").Split(new[] { ',', ';', ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(h => new string(h.Where(char.IsDigit).ToArray()) == clean));

            if (match == null) return Results.Ok(new { found = false });

            var eval = EvaluateContract(match);
            var recent = LoadRecords(tenant).Where(r => r.CompanyId == match.Id).OrderByDescending(r => r.CreatedAt).Take(3).ToList();
            return Results.Ok(new
            {
                found = true,
                tenantId = tenant,
                company = match,
                contractBadge = eval.ContractBadge,
                contractColor = eval.ContractColor,
                daysRemaining = eval.DaysRemaining,
                isExpired = eval.IsExpired,
                recentRecords = recent
            });
        });

        // Save Category
        app.MapPost("/api/crm/categories/save", async (HttpContext ctx) =>
        {
            string tenant = ResolveTenant(ctx);
            var body = await JsonSerializer.DeserializeAsync<CrmCategory>(ctx.Request.Body, _jsonOpts);
            if (body == null || string.IsNullOrWhiteSpace(body.Name))
                return Results.BadRequest(new { error = "Kategori adı boş olamaz." });

            var list = LoadCategories(tenant);
            if (string.IsNullOrWhiteSpace(body.Id))
                body.Id = "cat-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            var existing = list.FirstOrDefault(x => x.Id == body.Id || x.Name.Equals(body.Name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                existing.Name = body.Name.Trim();
                existing.Color = string.IsNullOrWhiteSpace(body.Color) ? "#f59e0b" : body.Color;
            }
            else
            {
                body.Name = body.Name.Trim();
                list.Add(body);
            }
            SaveJson(StoreKeyFor(tenant, "categories"), CategoriesPath(tenant), list);
            return Results.Ok(new { success = true, categories = list });
        });

        // Delete Category
        app.MapPost("/api/crm/categories/delete", async (HttpContext ctx) =>
        {
            string tenant = ResolveTenant(ctx);
            using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
            string id = doc.RootElement.TryGetProperty("id", out var p) ? p.GetString() ?? "" : "";
            var list = LoadCategories(tenant);
            list.RemoveAll(x => x.Id == id);
            SaveJson(StoreKeyFor(tenant, "categories"), CategoriesPath(tenant), list);
            return Results.Ok(new { success = true, categories = list });
        });

        // Save Company
        app.MapPost("/api/crm/companies/save", async (HttpContext ctx) =>
        {
            string tenant = ResolveTenant(ctx);
            var body = await JsonSerializer.DeserializeAsync<CrmCompany>(ctx.Request.Body, _jsonOpts);
            if (body == null || string.IsNullOrWhiteSpace(body.Name))
                return Results.BadRequest(new { error = "Firma adı zorunludur." });

            var list = LoadCompanies(tenant);
            string now = NowTr();
            if (string.IsNullOrWhiteSpace(body.Id))
            {
                body.Id = "cmp-" + Guid.NewGuid().ToString("N").Substring(0, 10);
                body.CreatedAt = now;
                body.UpdatedAt = now;
                list.Add(body);
            }
            else
            {
                var idx = list.FindIndex(x => x.Id == body.Id);
                if (idx >= 0)
                {
                    body.CreatedAt = string.IsNullOrEmpty(list[idx].CreatedAt) ? now : list[idx].CreatedAt;
                    body.UpdatedAt = now;
                    list[idx] = body;
                }
                else
                {
                    body.CreatedAt = now;
                    body.UpdatedAt = now;
                    list.Add(body);
                }
            }
            SaveJson(StoreKeyFor(tenant, "companies"), CompaniesPath(tenant), list);
            return Results.Ok(new { success = true, company = body });
        });

        // Delete Company
        app.MapPost("/api/crm/companies/delete", async (HttpContext ctx) =>
        {
            string tenant = ResolveTenant(ctx);
            using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
            string id = doc.RootElement.TryGetProperty("id", out var p) ? p.GetString() ?? "" : "";
            var list = LoadCompanies(tenant);
            list.RemoveAll(x => x.Id == id);
            SaveJson(StoreKeyFor(tenant, "companies"), CompaniesPath(tenant), list);
            return Results.Ok(new { success = true });
        });

        // Save Support Record & optionally create Mikro Service Invoice
        app.MapPost("/api/crm/records/save", async (HttpContext ctx) =>
        {
            string tenant = ResolveTenant(ctx);
            using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
            var root = doc.RootElement;
            bool sendToMikro = root.TryGetProperty("sendToMikro", out var sm) && sm.GetBoolean();

            var rec = JsonSerializer.Deserialize<CrmSupportRecord>(root.GetRawText(), _jsonOpts);
            if (rec == null || string.IsNullOrWhiteSpace(rec.Issue))
                return Results.BadRequest(new { error = "Destek açıklaması zorunludur." });

            var companies = LoadCompanies(tenant);
            var comp = companies.FirstOrDefault(c => c.Id == rec.CompanyId);
            if (comp != null)
            {
                rec.CompanyName = comp.Name;
                rec.Category = comp.Category;
                rec.ServiceType = comp.ServiceType;
                if (string.IsNullOrWhiteSpace(rec.MikroCariKod)) rec.MikroCariKod = comp.MikroCariKod;
                if (string.IsNullOrWhiteSpace(rec.MikroServiceCode)) rec.MikroServiceCode = comp.DefaultServiceCode;
            }

            if (rec.VatRate < 0 || rec.VatRate > 100) rec.VatRate = 20;
            rec.VatAmount = Math.Round(rec.NetAmount * rec.VatRate / 100m, 2);
            rec.TotalAmount = Math.Round(rec.NetAmount + rec.VatAmount, 2);

            string now = NowTr();
            if (string.IsNullOrWhiteSpace(rec.Id))
            {
                rec.Id = "sup-" + Guid.NewGuid().ToString("N").Substring(0, 10);
                rec.CreatedAt = now;
            }
            if (rec.Status == "Çözüldü" && string.IsNullOrWhiteSpace(rec.ClosedAt))
                rec.ClosedAt = now;

            if (!rec.IsBillable || rec.NetAmount <= 0)
            {
                rec.MikroInvoiceStatus = "Gerekmiyor";
                rec.MikroError = "";
            }
            else if (sendToMikro)
            {
                var res = await DispatchToMikroAsync(tenant, rec, comp);
                rec.MikroInvoiceStatus = res.Status;
                rec.MikroInvoiceNo = res.InvoiceNo;
                rec.MikroPayloadPreview = res.Payload;
                rec.MikroError = res.Error;
            }
            else if (string.IsNullOrWhiteSpace(rec.MikroInvoiceStatus) || rec.MikroInvoiceStatus == "Gerekmiyor")
            {
                rec.MikroInvoiceStatus = "Bekliyor";
            }

            var records = LoadRecords(tenant);
            int idx = records.FindIndex(x => x.Id == rec.Id);
            if (idx >= 0) records[idx] = rec;
            else records.Insert(0, rec);
            SaveJson(StoreKeyFor(tenant, "records"), RecordsPath(tenant), records);

            return Results.Ok(new { success = true, record = rec });
        });

        // Re-send / Trigger Mikro Invoice on existing record
        app.MapPost("/api/crm/records/send-mikro", async (HttpContext ctx) =>
        {
            string tenant = ResolveTenant(ctx);
            using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
            string id = doc.RootElement.TryGetProperty("id", out var p) ? p.GetString() ?? "" : "";
            var records = LoadRecords(tenant);
            var rec = records.FirstOrDefault(x => x.Id == id);
            if (rec == null) return Results.NotFound(new { error = "Kayıt bulunamadı." });

            var comp = LoadCompanies(tenant).FirstOrDefault(c => c.Id == rec.CompanyId);
            var res = await DispatchToMikroAsync(tenant, rec, comp);
            rec.MikroInvoiceStatus = res.Status;
            rec.MikroInvoiceNo = res.InvoiceNo;
            rec.MikroPayloadPreview = res.Payload;
            rec.MikroError = res.Error;
            SaveJson(StoreKeyFor(tenant, "records"), RecordsPath(tenant), records);

            return Results.Ok(new { success = res.Ok, record = rec, error = res.Error });
        });

        // Delete Support Record
        app.MapPost("/api/crm/records/delete", async (HttpContext ctx) =>
        {
            string tenant = ResolveTenant(ctx);
            using var doc = await JsonDocument.ParseAsync(ctx.Request.Body);
            string id = doc.RootElement.TryGetProperty("id", out var p) ? p.GetString() ?? "" : "";
            var records = LoadRecords(tenant);
            records.RemoveAll(x => x.Id == id);
            SaveJson(StoreKeyFor(tenant, "records"), RecordsPath(tenant), records);
            return Results.Ok(new { success = true });
        });

        // Save Mikro Config
        app.MapPost("/api/crm/mikro/config", async (HttpContext ctx) =>
        {
            string tenant = ResolveTenant(ctx);
            var cfg = await JsonSerializer.DeserializeAsync<MikroConfig>(ctx.Request.Body, _jsonOpts);
            if (cfg == null) return Results.BadRequest(new { error = "Geçersiz ayar." });
            var old = LoadMikroConfig(tenant);
            if (cfg.LastSimulatedSeq < old.LastSimulatedSeq) cfg.LastSimulatedSeq = old.LastSimulatedSeq;
            SaveJson(StoreKeyFor(tenant, "mikro_config"), MikroConfigPath(tenant), cfg);
            return Results.Ok(new { success = true, mikroConfig = cfg });
        });

        // Test Mikro Connection
        app.MapPost("/api/crm/mikro/test", async (HttpContext ctx) =>
        {
            string tenant = ResolveTenant(ctx);
            var cfg = await JsonSerializer.DeserializeAsync<MikroConfig>(ctx.Request.Body, _jsonOpts) ?? LoadMikroConfig(tenant);
            if (string.Equals(cfg.Mode, "Simulation", StringComparison.OrdinalIgnoreCase))
            {
                return Results.Ok(new { success = true, message = "🧪 Simülasyon (Test) Modu aktif! Gerçek Mikro veritabanına dokunmadan tüm Hizmet Satış Faturası akışını güvenle test edebilirsiniz." });
            }
            if (string.Equals(cfg.Mode, "Api", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(cfg.ApiBaseUrl))
                    return Results.Ok(new { success = false, message = "Mikro API URL adresi boş!" });
                try
                {
                    using var res = await _http.GetAsync(cfg.ApiBaseUrl);
                    return Results.Ok(new { success = true, message = $"Mikro API sunucusuna ulaşıldı (HTTP {(int)res.StatusCode})." });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { success = false, message = $"Mikro API erişilemedi: {ex.Message}" });
                }
            }
            if (string.Equals(cfg.Mode, "Sql", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(cfg.SqlConnectionString))
                    return Results.Ok(new { success = false, message = "SQL bağlantı cümlesi boş!" });
                try
                {
                    using var conn = new System.Data.SqlClient.SqlConnection(cfg.SqlConnectionString);
                    await conn.OpenAsync();
                    return Results.Ok(new { success = true, message = $"SQL Server bağlantısı başarılı! Veritabanı: {conn.Database}" });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { success = false, message = $"SQL bağlantı hatası: {ex.Message}" });
                }
            }
            return Results.Ok(new { success = false, message = "Geçersiz mod." });
        });

        // Seed 3 realistic Turkish demo companies for instant testing (scoped to tenantId)
        app.MapPost("/api/crm/seed-demo", (HttpContext ctx) =>
        {
            string tenant = ResolveTenant(ctx);
            var companies = LoadCompanies(tenant);
            string now = NowTr();
            var demos = new List<CrmCompany>
            {
                new()
                {
                    Id = "cmp-demo-bakim",
                    Name = "Anadolu Makina Sanayi A.Ş.",
                    Category = "Üretim / Sanayi",
                    ServiceType = "Bakım Anlaşmalı",
                    ContractEndDate = "2027-12-31",
                    DefaultFee = 0,
                    HostIds = "112233445, 554433221",
                    MikroCariKod = "120.01.001",
                    InvoiceTitle = "ANADOLU MAKİNA SANAYİ VE TİCARET A.Ş.",
                    TaxOffice = "Büyük Mükellefler",
                    TaxNumber = "1234567890",
                    InvoiceAddress = "İkitelli OSB Mah. 12. Cad. No:45 Başakşehir / İstanbul",
                    ContactPerson = "Ahmet Yılmaz",
                    Phone = "0532 111 22 33",
                    Email = "bilgi@anadolumakina.com.tr",
                    DefaultServiceCode = "HZM-BAKIM-01",
                    VatRate = 20,
                    Notes = "Yıllık tam kapsamlı Mikro ERP + Sunucu bakım anlaşmalı VIP müşteri.",
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new()
                {
                    Id = "cmp-demo-ucretli",
                    Name = "Yıldız Tekstil & Konfeksiyon Ltd.",
                    Category = "KOBİ",
                    ServiceType = "Ücretli",
                    ContractEndDate = "",
                    DefaultFee = 1500,
                    HostIds = "998877665",
                    MikroCariKod = "120.01.042",
                    InvoiceTitle = "YILDIZ TEKSTİL KONFEKSİYON SAN. TİC. LTD. ŞTİ.",
                    TaxOffice = "Merter V.D.",
                    TaxNumber = "9876543210",
                    InvoiceAddress = "Mehmet Nesih Özmen Mah. Merter / İstanbul",
                    ContactPerson = "Fatma Kaya",
                    Phone = "0533 444 55 66",
                    Email = "muhasebe@yildiztekstil.com.tr",
                    DefaultServiceCode = "HZM-DESTEK-01",
                    VatRate = 20,
                    Notes = "Bakım anlaşması yok — her uzaktan destek sonrası Hizmet Satış Faturası kesilir.",
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new()
                {
                    Id = "cmp-demo-ucretsiz",
                    Name = "Örnek Mali Müşavirlik (Demo)",
                    Category = "Muhasebe / Mali Müşavir",
                    ServiceType = "Ücretsiz",
                    ContractEndDate = "",
                    DefaultFee = 0,
                    HostIds = "776655443",
                    MikroCariKod = "120.01.099",
                    InvoiceTitle = "ÖRNEK MALİ MÜŞAVİRLİK",
                    TaxOffice = "Kadıköy V.D.",
                    TaxNumber = "5556667778",
                    InvoiceAddress = "Caferağa Mah. Kadıköy / İstanbul",
                    ContactPerson = "Mehmet Demir",
                    Phone = "0542 777 88 99",
                    Email = "info@ornekmali.com",
                    DefaultServiceCode = "HZM-DESTEK-01",
                    VatRate = 20,
                    Notes = "İlk 30 gün ücretsiz kurulum & eğitim kapsamında.",
                    CreatedAt = now,
                    UpdatedAt = now
                }
            };

            foreach (var d in demos)
            {
                if (!companies.Any(x => x.Id == d.Id))
                    companies.Add(d);
            }
            SaveJson(StoreKeyFor(tenant, "companies"), CompaniesPath(tenant), companies);
            return Results.Ok(new { success = true, tenantId = tenant, count = companies.Count });
        });

        // Dinamik Bayi Destek Kanalları / Birimler (Personel veya Departman — sıfır sabit kalıp!)
        app.MapGet("/api/crm/channels", (HttpContext ctx) =>
        {
            string tenant = ResolveTenant(ctx);
            var list = GetDealerChannels(tenant);
            return Results.Ok(new { tenantId = tenant, channels = list });
        });

        app.MapPost("/api/crm/channels", async (HttpContext ctx) =>
        {
            string tenant = ResolveTenant(ctx);
            using var reader = new StreamReader(ctx.Request.Body);
            string body = await reader.ReadToEndAsync();
            var dto = JsonSerializer.Deserialize<CrmChannel>(body, _jsonOpts);
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                return Results.BadRequest(new { error = "Kanal / Birim adı boş olamaz." });

            string cleanName = dto.Name.Trim();
            if (cleanName.Length > 60) cleanName = cleanName.Substring(0, 60);
            string cleanNote = (dto.Note ?? "").Trim();
            if (cleanNote.Length > 80) cleanNote = cleanNote.Substring(0, 80);

            var list = GetDealerChannels(tenant);
            if (!list.Any(c => c.Name.Equals(cleanName, StringComparison.OrdinalIgnoreCase)))
            {
                list.Add(new CrmChannel
                {
                    Id = "ch-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    Name = cleanName,
                    Note = cleanNote,
                    Active = true
                });
                SaveJson(StoreKeyFor(tenant, "channels"), ChannelsPath(tenant), list);
            }
            return Results.Ok(new { success = true, tenantId = tenant, channels = list });
        });

        app.MapDelete("/api/crm/channels/{id}", (HttpContext ctx, string id) =>
        {
            string tenant = ResolveTenant(ctx);
            var list = GetDealerChannels(tenant);
            int removed = list.RemoveAll(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase) || c.Name.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (removed > 0)
            {
                SaveJson(StoreKeyFor(tenant, "channels"), ChannelsPath(tenant), list);
            }
            return Results.Ok(new { success = true, removed, channels = list });
        });
    }
}
