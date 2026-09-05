using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace BigLineconnect
{
    /// <summary>
    /// RFC 5389 STUN Client for NAT Discovery and UDP Hole Punching.
    /// Queries public STUN servers (Google STUN) to resolve local client's external public IP & NAT mapped port.
    /// </summary>
    public static class StunClient
    {
        private static readonly string[] StunServers = new[]
        {
            "stun.l.google.com",
            "stun1.l.google.com",
            "stun2.l.google.com"
        };

        private const int StunPort = 19302;
        private const uint MagicCookie = 0x2112A442;

        public static async Task SendBindingRequestsAsync(UdpClient client)
        {
            try
            {
                // Build 20-byte RFC 5389 Binding Request
                byte[] request = new byte[20];
                request[0] = 0x00;
                request[1] = 0x01; // Binding Request
                request[2] = 0x00;
                request[3] = 0x00; // Length = 0
                // Magic Cookie 0x2112A442
                request[4] = 0x21;
                request[5] = 0x12;
                request[6] = 0xA4;
                request[7] = 0x42;
                // Transaction ID (12 bytes)
                var rand = new Random();
                byte[] txId = new byte[12];
                rand.NextBytes(txId);
                Buffer.BlockCopy(txId, 0, request, 8, 12);

                foreach (var host in StunServers)
                {
                    try
                    {
                        var addresses = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
                        foreach (var addr in addresses)
                        {
                            if (addr.AddressFamily == AddressFamily.InterNetwork)
                            {
                                var stunServerEp = new IPEndPoint(addr, StunPort);
                                await client.SendAsync(request, request.Length, stunServerEp).ConfigureAwait(false);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        public static IPEndPoint? ParseResponse(byte[] buf)
        {
            if (buf == null || buf.Length < 20) return null;

            // Verify Binding Response: 0x0101
            if (buf[0] != 0x01 || buf[1] != 0x01) return null;

            // Verify Magic Cookie: 0x2112A442
            if (buf[4] != 0x21 || buf[5] != 0x12 || buf[6] != 0xA4 || buf[7] != 0x42) return null;

            try
            {
                // Parse attributes starting at offset 20
                int offset = 20;
                int msgLength = (buf[2] << 8) | buf[3];
                int maxLen = Math.Min(buf.Length, 20 + msgLength);

                while (offset + 4 <= maxLen)
                {
                    ushort attrType = (ushort)((buf[offset] << 8) | buf[offset + 1]);
                    ushort attrLen = (ushort)((buf[offset + 2] << 8) | buf[offset + 3]);
                    offset += 4;

                    if (offset + attrLen > maxLen) break;

                    // 0x0020 = XOR-MAPPED-ADDRESS
                    if (attrType == 0x0020 && attrLen >= 8)
                    {
                        byte family = buf[offset + 1];
                        if (family == 0x01) // IPv4
                        {
                            int port = ((buf[offset + 2] << 8) | buf[offset + 3]) ^ 0x2112;
                            byte[] ipBytes = new byte[4];
                            ipBytes[0] = (byte)(buf[offset + 4] ^ 0x21);
                            ipBytes[1] = (byte)(buf[offset + 5] ^ 0x12);
                            ipBytes[2] = (byte)(buf[offset + 6] ^ 0xA4);
                            ipBytes[3] = (byte)(buf[offset + 7] ^ 0x42);
                            return new IPEndPoint(new IPAddress(ipBytes), port);
                        }
                    }
                    // 0x0001 = MAPPED-ADDRESS
                    else if (attrType == 0x0001 && attrLen >= 8)
                    {
                        byte family = buf[offset + 1];
                        if (family == 0x01) // IPv4
                        {
                            int port = (buf[offset + 2] << 8) | buf[offset + 3];
                            byte[] ipBytes = new byte[4];
                            Buffer.BlockCopy(buf, offset + 4, ipBytes, 0, 4);
                            return new IPEndPoint(new IPAddress(ipBytes), port);
                        }
                    }

                    // Align to 4-byte boundary
                    offset += (attrLen + 3) & ~3;
                }
            }
            catch { }

            return null;
        }

        public static async Task<IPEndPoint?> QueryExternalEndpointAsync(UdpClient udpClient, int timeoutMs = 1500)
        {
            foreach (var host in StunServers)
            {
                try
                {
                    var addresses = await Dns.GetHostAddressesAsync(host).ConfigureAwait(false);
                    if (addresses.Length == 0) continue;

                    var stunServerEp = new IPEndPoint(addresses[0], StunPort);
                    var endpoint = await QueryServerAsync(udpClient, stunServerEp, timeoutMs).ConfigureAwait(false);
                    if (endpoint != null) return endpoint;
                }
                catch { }
            }
            return null;
        }

        private static async Task<IPEndPoint?> QueryServerAsync(UdpClient client, IPEndPoint serverEp, int timeoutMs)
        {
            try
            {
                // Build 20-byte RFC 5389 Binding Request
                byte[] request = new byte[20];
                request[0] = 0x00;
                request[1] = 0x01; // Binding Request
                request[2] = 0x00;
                request[3] = 0x00; // Length = 0
                // Magic Cookie 0x2112A442
                request[4] = 0x21;
                request[5] = 0x12;
                request[6] = 0xA4;
                request[7] = 0x42;
                // Transaction ID (12 bytes)
                var rand = new Random();
                byte[] txId = new byte[12];
                rand.NextBytes(txId);
                Buffer.BlockCopy(txId, 0, request, 8, 12);

                await client.SendAsync(request, request.Length, serverEp).ConfigureAwait(false);

                using var cts = new CancellationTokenSource(timeoutMs);
                while (!cts.Token.IsCancellationRequested)
                {
                    var recvTask = client.ReceiveAsync(cts.Token).AsTask();
                    var completed = await Task.WhenAny(recvTask, Task.Delay(timeoutMs, cts.Token)).ConfigureAwait(false);
                    if (completed != recvTask) break;

                    var result = await recvTask.ConfigureAwait(false);
                    byte[] buf = result.Buffer;

                    var ep = ParseResponse(buf);
                    if (ep != null) return ep;
                }
            }
            catch { }
            return null;
        }
    }
}
