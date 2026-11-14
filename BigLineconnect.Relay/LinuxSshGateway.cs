using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace BigLineconnect.Relay
{
    public static class LinuxSshGateway
    {
        private const int MaxSftpDownloadBytes = 8 * 1024 * 1024; // 8 MB

        public static async Task HandleWebSocketAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("WebSocket connection required.");
                return;
            }

            using var ws = await context.WebSockets.AcceptWebSocketAsync();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            using var sendLock = new SemaphoreSlim(1, 1);

            async Task SendJsonAsync(object payload)
            {
                if (ws.State != WebSocketState.Open) return;
                byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
                await sendLock.WaitAsync(cts.Token).ConfigureAwait(false);
                try
                {
                    if (ws.State == WebSocketState.Open)
                    {
                        await ws.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, cts.Token).ConfigureAwait(false);
                    }
                }
                finally
                {
                    sendLock.Release();
                }
            }

            try
            {
                // İlk mesaj: bağlantı bilgileri {"type":"connect","host":"...","port":22,"username":"root","password":"...","privateKey":"","cols":100,"rows":30}
                string? initJson = await ReceiveTextMessageAsync(ws, cts.Token).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(initJson)) return;

                using var initDoc = JsonDocument.Parse(initJson);
                var root = initDoc.RootElement;
                string host = root.TryGetProperty("host", out var pHost) ? (pHost.GetString() ?? "").Trim() : "";
                int port = root.TryGetProperty("port", out var pPort) && pPort.TryGetInt32(out int prt) ? Math.Clamp(prt, 1, 65535) : 22;
                string username = root.TryGetProperty("username", out var pUser) ? (pUser.GetString() ?? "").Trim() : "root";
                string password = root.TryGetProperty("password", out var pPass) ? (pPass.GetString() ?? "") : "";
                string privateKey = root.TryGetProperty("privateKey", out var pKey) ? (pKey.GetString() ?? "") : "";
                uint cols = root.TryGetProperty("cols", out var pCols) && pCols.TryGetUInt32(out uint c) ? Math.Clamp(c, 20u, 400u) : 100u;
                uint rows = root.TryGetProperty("rows", out var pRows) && pRows.TryGetUInt32(out uint r) ? Math.Clamp(r, 8u, 150u) : 30u;

                if (string.IsNullOrEmpty(host))
                {
                    await SendJsonAsync(new { type = "error", message = "Lütfen geçerli bir Linux sunucu IP veya alan adı girin." });
                    return;
                }

                // Güvenli Dahili Etkileşimli Eğitim / Test Simülatörü (Sıfır İşletim Sistemi Komutu, %100 İzole Bellek İçi)
                if (host.Equals("demo.bconnect.local", StringComparison.OrdinalIgnoreCase) ||
                    host.Equals("demo", StringComparison.OrdinalIgnoreCase))
                {
                    await RunInteractiveDemoSandboxAsync(ws, SendJsonAsync, cts.Token).ConfigureAwait(false);
                    return;
                }

                // SSRF Koruması: Yerel ağ ve bulut iç IP adreslerini engelle (yalnızca CI test ortamında izin verilir)
                var (allowed, reason) = await ValidateTargetHostAsync(host).ConfigureAwait(false);
                if (!allowed)
                {
                    await SendJsonAsync(new { type = "error", message = reason });
                    return;
                }

                if (string.IsNullOrEmpty(username))
                {
                    await SendJsonAsync(new { type = "error", message = "SSH kullanıcı adı boş olamaz (ör. root veya ubuntu)." });
                    return;
                }

                await SendJsonAsync(new { type = "status", message = $"🔐 {host}:{port} sunucusuna güvenli SSH tüneli kuruluyor..." });

                var authMethods = new List<AuthenticationMethod>();
                if (!string.IsNullOrWhiteSpace(privateKey))
                {
                    using var keyStream = new MemoryStream(Encoding.UTF8.GetBytes(privateKey));
                    PrivateKeyFile pkf = string.IsNullOrEmpty(password)
                        ? new PrivateKeyFile(keyStream)
                        : new PrivateKeyFile(keyStream, password);
                    authMethods.Add(new PrivateKeyAuthenticationMethod(username, pkf));
                }
                if (!string.IsNullOrEmpty(password))
                {
                    authMethods.Add(new PasswordAuthenticationMethod(username, password));
                    var kbd = new KeyboardInteractiveAuthenticationMethod(username);
                    kbd.AuthenticationPrompt += (_, e) =>
                    {
                        foreach (var prompt in e.Prompts)
                        {
                            prompt.Response = password;
                        }
                    };
                    authMethods.Add(kbd);
                }

                if (authMethods.Count == 0)
                {
                    await SendJsonAsync(new { type = "error", message = "Lütfen SSH şifresi veya Private Key girin." });
                    return;
                }

                var connInfo = new Renci.SshNet.ConnectionInfo(host, port, username, authMethods.ToArray())
                {
                    Timeout = TimeSpan.FromSeconds(12)
                };

                using var sshClient = new SshClient(connInfo);
                sshClient.KeepAliveInterval = TimeSpan.FromSeconds(20);
                await sshClient.ConnectAsync(cts.Token).ConfigureAwait(false);

                var termModes = new Dictionary<TerminalModes, uint>
                {
                    { TerminalModes.ECHO, 1 },
                    { TerminalModes.ICANON, 1 },
                    { TerminalModes.ISIG, 1 }
                };

                using var shell = sshClient.CreateShellStream("xterm-256color", cols, rows, 800, 600, 32768, termModes);

                await SendJsonAsync(new
                {
                    type = "connected",
                    host = $"{username}@{host}:{port}",
                    banner = sshClient.ConnectionInfo.ServerVersion ?? "OpenSSH"
                });

                // 1) SSH PTY -> WebSocket Okuma Görevi
                var shellReadTask = Task.Run(async () =>
                {
                    var buf = new byte[8192];
                    try
                    {
                        while (!cts.Token.IsCancellationRequested && ws.State == WebSocketState.Open && sshClient.IsConnected)
                        {
                            int n = await shell.ReadAsync(buf, 0, buf.Length, cts.Token).ConfigureAwait(false);
                            if (n <= 0)
                            {
                                await Task.Delay(15, cts.Token).ConfigureAwait(false);
                                continue;
                            }
                            string b64 = Convert.ToBase64String(buf, 0, n);
                            await SendJsonAsync(new { type = "output", data = b64 }).ConfigureAwait(false);
                        }
                    }
                    catch { }
                    finally
                    {
                        cts.Cancel();
                    }
                }, cts.Token);

                // 2) Canlı Linux Sistem Sağlığı (CPU / RAM / Disk / Uptime) Görevi
                var statsTask = Task.Run(async () =>
                {
                    try
                    {
                        while (!cts.Token.IsCancellationRequested && ws.State == WebSocketState.Open && sshClient.IsConnected)
                        {
                            try
                            {
                                var stats = CollectRemoteLinuxStats(sshClient);
                                if (stats != null)
                                {
                                    await SendJsonAsync(stats).ConfigureAwait(false);
                                }
                            }
                            catch { }
                            await Task.Delay(4000, cts.Token).ConfigureAwait(false);
                        }
                    }
                    catch { }
                }, cts.Token);

                // 3) WebSocket -> SSH PTY & SFTP Kontrol Döngüsü
                while (!cts.Token.IsCancellationRequested && ws.State == WebSocketState.Open && sshClient.IsConnected)
                {
                    string? msg = await ReceiveTextMessageAsync(ws, cts.Token).ConfigureAwait(false);
                    if (msg == null) break;

                    using var doc = JsonDocument.Parse(msg);
                    var el = doc.RootElement;
                    string msgType = el.TryGetProperty("type", out var pt) ? (pt.GetString() ?? "") : "";

                    if (msgType == "input")
                    {
                        string b64 = el.TryGetProperty("data", out var pd) ? (pd.GetString() ?? "") : "";
                        if (!string.IsNullOrEmpty(b64))
                        {
                            byte[] raw = Convert.FromBase64String(b64);
                            await shell.WriteAsync(raw, 0, raw.Length, cts.Token).ConfigureAwait(false);
                            await shell.FlushAsync(cts.Token).ConfigureAwait(false);
                        }
                    }
                    else if (msgType == "resize")
                    {
                        uint nc = el.TryGetProperty("cols", out var pc) && pc.TryGetUInt32(out uint vc) ? Math.Clamp(vc, 20u, 400u) : cols;
                        uint nr = el.TryGetProperty("rows", out var pr) && pr.TryGetUInt32(out uint vr) ? Math.Clamp(vr, 8u, 150u) : rows;
                        cols = nc;
                        rows = nr;
                        try { shell.ChangeWindowSize(nc, nr, 0, 0); } catch { }
                    }
                    else if (msgType == "sftp_get")
                    {
                        string remotePath = el.TryGetProperty("path", out var pp) ? (pp.GetString() ?? "").Trim() : "";
                        if (!string.IsNullOrEmpty(remotePath))
                        {
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    using var sftp = new SftpClient(connInfo);
                                    await sftp.ConnectAsync(cts.Token).ConfigureAwait(false);
                                    if (!sftp.Exists(remotePath))
                                    {
                                        await SendJsonAsync(new { type = "notice", level = "error", message = $"Dosya bulunamadı: {remotePath}" });
                                        return;
                                    }
                                    var attrs = sftp.GetAttributes(remotePath);
                                    if (attrs.IsDirectory)
                                    {
                                        await SendJsonAsync(new { type = "notice", level = "error", message = "Klasör yerine bir dosya yolu belirtin (ör. /var/log/syslog)." });
                                        return;
                                    }
                                    if (attrs.Size > MaxSftpDownloadBytes)
                                    {
                                        await SendJsonAsync(new { type = "notice", level = "error", message = "Dosya 8 MB sınırından büyük; lütfen önce sıkıştırın." });
                                        return;
                                    }
                                    using var ms = new MemoryStream();
                                    sftp.DownloadFile(remotePath, ms);
                                    string fileName = Path.GetFileName(remotePath);
                                    if (string.IsNullOrEmpty(fileName)) fileName = "download.log";
                                    await SendJsonAsync(new
                                    {
                                        type = "file_download",
                                        fileName,
                                        size = ms.Length,
                                        data = Convert.ToBase64String(ms.ToArray())
                                    });
                                }
                                catch (Exception ex)
                                {
                                    await SendJsonAsync(new { type = "notice", level = "error", message = "İndirme hatası: " + ex.Message });
                                }
                            }, cts.Token);
                        }
                    }
                    else if (msgType == "ping")
                    {
                        await SendJsonAsync(new { type = "pong" }).ConfigureAwait(false);
                    }
                }

                cts.Cancel();
                try { await Task.WhenAll(shellReadTask, statsTask).ConfigureAwait(false); } catch { }
            }
            catch (SshAuthenticationException)
            {
                await SendJsonAsync(new { type = "error", message = "❌ SSH kullanıcı adı veya şifresi hatalı!" });
            }
            catch (Exception ex)
            {
                await SendJsonAsync(new { type = "error", message = "⚠️ Bağlantı hatası: " + ex.Message });
            }
            finally
            {
                if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
                {
                    try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Done", CancellationToken.None); } catch { }
                }
            }
        }

        private static object? CollectRemoteLinuxStats(SshClient ssh)
        {
            if (!ssh.IsConnected) return null;
            const string script = "LC_ALL=C; " +
                                  "H=$(hostname 2>/dev/null || echo linux); " +
                                  "CORES=$(nproc 2>/dev/null || echo 1); " +
                                  "LOAD=$(awk '{print $1}' /proc/loadavg 2>/dev/null || echo 0); " +
                                  "MEM=$(awk '/MemTotal:/{t=$2} /MemAvailable:/{a=$2} END{if(t>0) printf \"%d %d\", (t-a)/1024, t/1024; else print \"0 0\"}' /proc/meminfo 2>/dev/null); " +
                                  "DISK=$(df -P / 2>/dev/null | awk 'NR==2{gsub(/%/,\"\",$5); print $5}'); " +
                                  "UP=$(awk '{d=int($1/86400); h=int(($1%86400)/3600); m=int(($1%3600)/60); if(d>0) printf \"%dg %dsa\",d,h; else printf \"%dsa %ddk\",h,m}' /proc/uptime 2>/dev/null); " +
                                  "echo \"$H|$CORES|$LOAD|$MEM|$DISK|$UP\"";
            using var cmd = ssh.CreateCommand(script);
            cmd.CommandTimeout = TimeSpan.FromSeconds(4);
            string res = (cmd.Execute() ?? "").Trim();
            var parts = res.Split('|');
            if (parts.Length < 6) return null;

            string hostname = parts[0];
            int cores = int.TryParse(parts[1], out int c) && c > 0 ? c : 1;
            double load1 = double.TryParse(parts[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double ld) ? ld : 0;
            int cpuPct = (int)Math.Clamp(Math.Round((load1 / cores) * 100.0), 1, 100);

            var memParts = parts[3].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            int ramUsedMb = memParts.Length > 0 && int.TryParse(memParts[0], out int ru) ? ru : 0;
            int ramTotalMb = memParts.Length > 1 && int.TryParse(memParts[1], out int rt) ? rt : 0;
            int diskPct = int.TryParse(parts[4].Trim(), out int dp) ? Math.Clamp(dp, 0, 100) : 0;
            string uptime = parts[5].Trim();

            return new
            {
                type = "stats",
                hostname,
                cpuPct,
                ramUsedMb,
                ramTotalMb,
                diskPct,
                uptime
            };
        }

        private static async Task<(bool Allowed, string Reason)> ValidateTargetHostAsync(string host)
        {
            bool allowLocalCi = Environment.GetEnvironmentVariable("BCONNECT_ALLOW_LOCAL_SSH_TEST") == "1";
            if (allowLocalCi) return (true, "");

            IPAddress[] addrs;
            try
            {
                addrs = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
            }
            catch
            {
                return (false, $"'{host}' adresi çözümlenemedi. Lütfen IP veya alan adını kontrol edin.");
            }

            if (addrs.Length == 0)
            {
                return (false, $"'{host}' için geçerli bir IP adresi bulunamadı.");
            }

            foreach (var ip in addrs)
            {
                if (IsPrivateOrReservedIp(ip))
                {
                    return (false, "Güvenlik politikası gereği yerel/özel iç ağ IP adreslerine bulut üzerinden doğrudan SSH açılamaz; lütfen sunucunun dış (Public) IP adresini veya alan adını girin.");
                }
            }

            return (true, "");
        }

        private static bool IsPrivateOrReservedIp(IPAddress ip)
        {
            if (IPAddress.IsLoopback(ip)) return true;
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();

            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                byte[] b = ip.GetAddressBytes();
                if (b[0] == 0 || b[0] == 10 || b[0] == 127) return true;
                if (b[0] == 169 && b[1] == 254) return true;
                if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true;
                if (b[0] == 192 && b[1] == 168) return true;
                if (b[0] == 100 && b[1] >= 64 && b[1] <= 127) return true;
                if (b[0] >= 224) return true;
                return false;
            }

            if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast) return true;
                byte[] b = ip.GetAddressBytes();
                if ((b[0] & 0xFE) == 0xFC) return true; // fc00::/7 Unique Local
                if (ip.Equals(IPAddress.IPv6None) || ip.Equals(IPAddress.IPv6Loopback)) return true;
            }

            return false;
        }

        private static async Task RunInteractiveDemoSandboxAsync(WebSocket ws, Func<object, Task> sendJson, CancellationToken token)
        {
            await sendJson(new
            {
                type = "connected",
                host = "root@ubuntu-prod-01 (Demo Test Sunucusu)",
                banner = "Ubuntu 24.04.1 LTS • Kernel 6.8.0-48-generic"
            });

            var rand = new Random();
            using var demoCts = CancellationTokenSource.CreateLinkedTokenSource(token);

            async Task WriteTermAsync(string text)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                await sendJson(new { type = "output", data = Convert.ToBase64String(bytes) });
            }

            string prompt = "\u001b[1;32mroot@ubuntu-prod-01\u001b[0m:\u001b[1;34m~\u001b[0m# ";

            const string innerPlain = "  >> B-CONNECT CANLI LINUX TERMINAL TEST SUNUCUSU (UBUNTU 24.04)  ";
            string bar = new string('═', innerPlain.Length);

            await WriteTermAsync(
                $"\u001b[1;33m╔{bar}╗\u001b[0m\r\n" +
                "\u001b[1;33m║\u001b[0m  \u001b[1;32m>>\u001b[0m \u001b[1;37mB-CONNECT CANLI LINUX TERMINAL TEST SUNUCUSU (UBUNTU 24.04)\u001b[0m  \u001b[1;33m║\u001b[0m\r\n" +
                $"\u001b[1;33m╚{bar}╝\u001b[0m\r\n" +
                "Sistem Hazır! Alttaki hazır butonlara dokunabilir veya komut yazabilirsiniz.\r\n" +
                "Örnek komutlar: \u001b[36mhelp\u001b[0m, \u001b[36muname -a\u001b[0m, \u001b[36mdf -h\u001b[0m, \u001b[36mfree -m\u001b[0m, \u001b[36msystemctl status\u001b[0m, \u001b[36mdocker ps\u001b[0m, \u001b[36mtop\u001b[0m\r\n\r\n" +
                prompt);

            var statsLoop = Task.Run(async () =>
            {
                int tick = 0;
                while (!demoCts.Token.IsCancellationRequested && ws.State == WebSocketState.Open)
                {
                    await sendJson(new
                    {
                        type = "stats",
                        hostname = "ubuntu-prod-01",
                        cpuPct = 11 + (tick * 3) % 14,
                        ramUsedMb = 1840 + (tick * 17) % 120,
                        ramTotalMb = 8192,
                        diskPct = 34,
                        uptime = "18g 7sa"
                    });
                    tick++;
                    try { await Task.Delay(3000, demoCts.Token); } catch { break; }
                }
            }, demoCts.Token);

            var lineBuf = new StringBuilder();
            var history = new List<string> { "systemctl status", "docker ps", "df -h", "free -m" };
            int histIdx = history.Count;

            async Task ReplaceCurrentLineAsync(string newLine)
            {
                int oldLen = lineBuf.Length;
                lineBuf.Clear();
                lineBuf.Append(newLine);
                await WriteTermAsync("\r" + prompt + new string(' ', oldLen) + "\r" + prompt + newLine);
            }

            while (!demoCts.Token.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                string? msg = await ReceiveTextMessageAsync(ws, demoCts.Token);
                if (msg == null) break;

                using var doc = JsonDocument.Parse(msg);
                var el = doc.RootElement;
                string mType = el.TryGetProperty("type", out var pt) ? (pt.GetString() ?? "") : "";

                if (mType == "sftp_get")
                {
                    string reqPath = el.TryGetProperty("path", out var pp) ? (pp.GetString() ?? "/var/log/syslog") : "/var/log/syslog";
                    string sampleLog =
                        $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} ubuntu-prod-01 systemd[1]: Started nginx.service - High performance web server.\n" +
                        $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} ubuntu-prod-01 postgresql[1422]: database system is ready to accept connections\n" +
                        $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} ubuntu-prod-01 bconnect-ssh[2091]: Remote admin session verified OK.\n";
                    string fName = Path.GetFileName(reqPath);
                    if (string.IsNullOrEmpty(fName)) fName = "syslog.log";
                    byte[] rawLog = Encoding.UTF8.GetBytes(sampleLog);
                    await sendJson(new
                    {
                        type = "file_download",
                        fileName = fName,
                        size = rawLog.Length,
                        data = Convert.ToBase64String(rawLog)
                    });
                    continue;
                }

                if (mType != "input") continue;
                string b64 = el.TryGetProperty("data", out var pd) ? (pd.GetString() ?? "") : "";
                if (string.IsNullOrEmpty(b64)) continue;
                string input = Encoding.UTF8.GetString(Convert.FromBase64String(b64));

                if (input == "\u001b[A") // Up Arrow
                {
                    if (history.Count > 0)
                    {
                        histIdx = Math.Max(0, histIdx - 1);
                        await ReplaceCurrentLineAsync(history[histIdx]);
                    }
                    continue;
                }
                if (input == "\u001b[B") // Down Arrow
                {
                    if (histIdx < history.Count - 1)
                    {
                        histIdx++;
                        await ReplaceCurrentLineAsync(history[histIdx]);
                    }
                    else
                    {
                        histIdx = history.Count;
                        await ReplaceCurrentLineAsync("");
                    }
                    continue;
                }

                foreach (char ch in input)
                {
                    if (ch == '\u0003') // Ctrl+C
                    {
                        lineBuf.Clear();
                        await WriteTermAsync("^C\r\n" + prompt);
                    }
                    else if (ch == '\t') // Tab auto-complete
                    {
                        string cur = lineBuf.ToString();
                        string[] cmds = { "systemctl status", "docker ps", "df -h", "free -m", "uname -a", "ls -la", "cat /var/log/syslog", "clear", "help" };
                        var match = cmds.FirstOrDefault(x => x.StartsWith(cur, StringComparison.OrdinalIgnoreCase));
                        if (!string.IsNullOrEmpty(match) && match.Length > cur.Length)
                        {
                            string rest = match.Substring(cur.Length);
                            lineBuf.Append(rest);
                            await WriteTermAsync(rest);
                        }
                    }
                    else if (ch == '\r' || ch == '\n')
                    {
                        string cmd = lineBuf.ToString().Trim();
                        lineBuf.Clear();
                        await WriteTermAsync("\r\n");
                        if (!string.IsNullOrEmpty(cmd))
                        {
                            history.Add(cmd);
                            histIdx = history.Count;
                            string reply = RunSimulatedLinuxCommand(cmd, rand);
                            if (reply == "__CLEAR__")
                            {
                                await WriteTermAsync("\u001b[2J\u001b[H" + prompt);
                                continue;
                            }
                            if (reply == "__EXIT__")
                            {
                                await WriteTermAsync("Oturum kapatıldı.\r\n");
                                demoCts.Cancel();
                                break;
                            }
                            await WriteTermAsync(reply + "\r\n");
                        }
                        await WriteTermAsync(prompt);
                    }
                    else if (ch == '\u007f' || ch == '\b')
                    {
                        if (lineBuf.Length > 0)
                        {
                            lineBuf.Length--;
                            await WriteTermAsync("\b \b");
                        }
                    }
                    else if (ch >= ' ')
                    {
                        lineBuf.Append(ch);
                        await WriteTermAsync(ch.ToString());
                    }
                }
            }

            demoCts.Cancel();
            try { await statsLoop; } catch { }
        }

        private static string RunSimulatedLinuxCommand(string cmd, Random rand)
        {
            string c = cmd.Trim().ToLowerInvariant();
            if (c == "clear" || c == "cls") return "__CLEAR__";
            if (c == "exit" || c == "logout") return "__EXIT__";
            if (c == "help")
            {
                return "\u001b[1;33mKullanılabilir Test Komutları:\u001b[0m\r\n" +
                       "  \u001b[36muname -a\u001b[0m          Çekirdek ve işletim sistemi bilgisi\r\n" +
                       "  \u001b[36mdf -h\u001b[0m             Disk doluluk durumu\r\n" +
                       "  \u001b[36mfree -m\u001b[0m           RAM bellek kullanımı\r\n" +
                       "  \u001b[36msystemctl status\u001b[0m  Aktif sunucu servisleri (nginx, postgresql, docker)\r\n" +
                       "  \u001b[36mdocker ps\u001b[0m         Çalışan konteyner listesi\r\n" +
                       "  \u001b[36mtop\u001b[0m / \u001b[36mhtop\u001b[0m        Anlık işlemci ve süreç tablosu\r\n" +
                       "  \u001b[36mls -la\u001b[0m            Klasör içeriği\r\n" +
                       "  \u001b[36mcat /var/log/syslog\u001b[0m Son sistem günlükleri";
            }
            if (c.StartsWith("uname"))
                return "Linux ubuntu-prod-01 6.8.0-48-generic #48-Ubuntu SMP PREEMPT_DYNAMIC x86_64 GNU/Linux";
            if (c == "whoami")
                return "root";
            if (c == "uptime")
                return $" {DateTime.UtcNow:HH:mm:ss} up 18 days,  7:14,  1 user,  load average: 0.24, 0.19, 0.15";
            if (c.StartsWith("df"))
            {
                return "Filesystem      Size  Used Avail Use% Mounted on\r\n" +
                       "/dev/nvme0n1p1  160G   54G  100G  34% /\r\n" +
                       "tmpfs           4.0G  1.2M  4.0G   1% /run\r\n" +
                       "/dev/nvme0n1p2  500G  142G  358G  29% /mnt/backups";
            }
            if (c.StartsWith("free"))
            {
                return "               total        used        free      shared  buff/cache   available\r\n" +
                       "Mem:            8192        1864        3910          48        2418        6024\r\n" +
                       "Swap:           2048           0        2048";
            }
            if (c.StartsWith("ls"))
            {
                return "total 48\r\n" +
                       "drwx------  6 root root  4096 Nov 14 21:10 \u001b[1;34m.\u001b[0m\r\n" +
                       "drwxr-xr-x 20 root root  4096 Nov 01 09:00 \u001b[1;34m..\u001b[0m\r\n" +
                       "drwxr-xr-x  3 root root  4096 Nov 14 03:00 \u001b[1;34mbackups\u001b[0m\r\n" +
                       "-rwxr-xr-x  1 root root  2480 Nov 12 14:22 \u001b[1;32mdeploy.sh\u001b[0m\r\n" +
                       "-rw-r--r--  1 root root  1842 Nov 14 18:45 docker-compose.yml\r\n" +
                       "-rw-r--r--  1 root root 14920 Nov 14 21:15 nginx-access.log";
            }
            if (c.Contains("systemctl"))
            {
                return "\u001b[1;32m●\u001b[0m ubuntu-prod-01 - State: \u001b[1;32mrunning\u001b[0m (0 failed jobs)\r\n" +
                       "   \u001b[1;32m●\u001b[0m nginx.service          loaded active \u001b[1;32mrunning\u001b[0m   High performance HTTP/Reverse Proxy\r\n" +
                       "   \u001b[1;32m●\u001b[0m postgresql@16.service  loaded active \u001b[1;32mrunning\u001b[0m   PostgreSQL 16 Relational Database\r\n" +
                       "   \u001b[1;32m●\u001b[0m docker.service         loaded active \u001b[1;32mrunning\u001b[0m   Docker Application Container Engine\r\n" +
                       "   \u001b[1;32m●\u001b[0m ssh.service            loaded active \u001b[1;32mrunning\u001b[0m   OpenBSD Secure Shell server";
            }
            if (c.StartsWith("docker"))
            {
                return "CONTAINER ID   IMAGE                STATUS          PORTS                    NAMES\r\n" +
                       "a84f9120c3b1   postgres:16-alpine   Up 18 days      0.0.0.0:5432->5432/tcp   erp-db\r\n" +
                       "c19e0441d8a2   redis:7-alpine       Up 18 days      127.0.0.1:6379->6379/tcp cache-redis\r\n" +
                       "f72b8830a914   nginx:1.27-alpine    Up 6 days       0.0.0.0:80->80/tcp       edge-proxy";
            }
            if (c == "top" || c == "htop" || c.StartsWith("ps"))
            {
                int cpu = 8 + rand.Next(0, 12);
                return $"\u001b[7m  PID USER      PR  NI    VIRT    RES    SHR S  %CPU  %MEM     TIME+ COMMAND          \u001b[0m\r\n" +
                       $" 1422 postgres  20   0 2418920 612480 584120 S   {cpu}.2   7.4  42:18.09 postgres         \r\n" +
                       " 1108 www-data  20   0  184200  42180  18400 S   2.1   0.5  14:05.44 nginx            \r\n" +
                       "  942 root      20   0 1894000 128400  64200 S   1.4   1.6  29:51.12 dockerd          \r\n" +
                       " 2091 root      20   0   17420   8940   7120 S   0.3   0.1   0:01.18 sshd";
            }
            if (c.StartsWith("cat"))
            {
                return $"{DateTime.UtcNow:MMM dd HH:mm:ss} ubuntu-prod-01 systemd[1]: Daily apt upgrade and clean activities succeeded.\r\n" +
                       $"{DateTime.UtcNow:MMM dd HH:mm:ss} ubuntu-prod-01 postgresql[1422]: checkpoint complete: wrote 48 buffers (0.3%)\r\n" +
                       $"{DateTime.UtcNow:MMM dd HH:mm:ss} ubuntu-prod-01 sshd[2091]: Accepted password for root via B-Connect Console";
            }
            return $"[Demo Sunucu] '{cmd}' çalıştırıldı (Çıkış Kodu: 0). Gerçek bir Linux sunucuya bağlandığınızda tüm komutlar doğrudan sunucu üzerinde çalışır.";
        }

        private static async Task<string?> ReceiveTextMessageAsync(WebSocket ws, CancellationToken token)
        {
            var buf = new byte[8192];
            var first = await ws.ReceiveAsync(new ArraySegment<byte>(buf), token).ConfigureAwait(false);
            if (first.MessageType == WebSocketMessageType.Close) return null;
            if (first.EndOfMessage)
            {
                return Encoding.UTF8.GetString(buf, 0, first.Count);
            }

            using var ms = new MemoryStream();
            ms.Write(buf, 0, first.Count);
            WebSocketReceiveResult res;
            do
            {
                res = await ws.ReceiveAsync(new ArraySegment<byte>(buf), token).ConfigureAwait(false);
                if (res.MessageType == WebSocketMessageType.Close) return null;
                ms.Write(buf, 0, res.Count);
            }
            while (!res.EndOfMessage);

            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}
