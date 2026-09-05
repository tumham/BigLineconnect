using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BigLineconnect
{
    /// <summary>
    /// BigLineconnect P2P Direct Engine: UDP Hole Punching & Zero-Cloud Latency Direct Transport
    /// </summary>
    public static class P2pDirectEngine
    {
        private static UdpClient? _udpClient;
        private static IPEndPoint? _remoteEndpoint;
        private static bool _isP2pConnected = false;
        private static CancellationTokenSource? _cts;

        public static bool IsP2pConnected => _isP2pConnected;

        public static event Action<byte[]>? OnP2pPacketReceived;

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingLanProbes = new();

        public static ushort LocalUdpPort { get; private set; } = 0;

        public static void Initialize(int preferredPort = 0)
        {
            try
            {
                Shutdown();
                _cts = new CancellationTokenSource();
                _udpClient = new UdpClient(preferredPort);
                _udpClient.EnableBroadcast = true;
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                
                LocalUdpPort = (ushort)((IPEndPoint)_udpClient.Client.LocalEndPoint!).Port;

                _ = Task.Run(() => ListenUdpLoop(_cts.Token));
            }
            catch
            {
                try
                {
                    _udpClient = new UdpClient(0);
                    _udpClient.EnableBroadcast = true;
                    LocalUdpPort = (ushort)((IPEndPoint)_udpClient.Client.LocalEndPoint!).Port;
                    _ = Task.Run(() => ListenUdpLoop(_cts.Token));
                }
                catch { }
            }
        }

        public static async Task<string?> ProbeLocalLanForHostIdAsync(string hostId, int timeoutMs = 250)
        {
            if (string.IsNullOrEmpty(hostId)) return null;
            string cleanId = hostId.Trim().Replace(" ", "");

            try
            {
                var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                _pendingLanProbes[cleanId] = tcs;

                using var probeClient = new UdpClient();
                probeClient.EnableBroadcast = true;
                byte[] probePacket = Encoding.UTF8.GetBytes($"LAN_PROBE:{cleanId}");
                var broadcastEp = new IPEndPoint(IPAddress.Broadcast, 18888);
                await probeClient.SendAsync(probePacket, probePacket.Length, broadcastEp).ConfigureAwait(false);

                var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs)).ConfigureAwait(false);
                if (completed == tcs.Task)
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
            }
            catch { }
            finally
            {
                _pendingLanProbes.TryRemove(cleanId, out _);
            }
            return null;
        }

        public static async Task PunchHoleAndConnectAsync(string remoteIp, int remotePort)
        {
            if (_udpClient == null || string.IsNullOrEmpty(remoteIp) || remotePort <= 0) return;

            try
            {
                _remoteEndpoint = new IPEndPoint(IPAddress.Parse(remoteIp), remotePort);
                byte[] pingPacket = Encoding.UTF8.GetBytes("P2P_PING");
                CancellationToken token = _cts?.Token ?? CancellationToken.None;

                _ = Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested && _udpClient != null)
                    {
                        try
                        {
                            await _udpClient.SendAsync(pingPacket, pingPacket.Length, _remoteEndpoint).ConfigureAwait(false);
                        }
                        catch { }

                        int delay = _isP2pConnected ? 5000 : 1000;
                        await Task.Delay(delay, token).ConfigureAwait(false);
                    }
                }, token);
            }
            catch { }
        }

        public static void SendP2pPacket(byte[] data)
        {
            if (_isP2pConnected && _udpClient != null && _remoteEndpoint != null)
            {
                try
                {
                    _udpClient.Send(data, data.Length, _remoteEndpoint);
                }
                catch
                {
                    _isP2pConnected = false;
                }
            }
        }

        private static async Task ListenUdpLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _udpClient != null)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync(token);
                    byte[] data = result.Buffer;
                    IPEndPoint senderEp = result.RemoteEndPoint;

                    if (data.Length >= 8 && Encoding.UTF8.GetString(data, 0, 8) == "P2P_PING")
                    {
                        _remoteEndpoint = senderEp;
                        _isP2pConnected = true;
                        byte[] pong = Encoding.UTF8.GetBytes("P2P_PONG");
                        await _udpClient.SendAsync(pong, pong.Length, senderEp);
                    }
                    else if (data.Length >= 8 && Encoding.UTF8.GetString(data, 0, 8) == "P2P_PONG")
                    {
                        _remoteEndpoint = senderEp;
                        _isP2pConnected = true;
                    }
                    else
                    {
                        string textMsg = data.Length > 0 ? Encoding.UTF8.GetString(data) : "";
                        if (textMsg.StartsWith("LAN_PROBE:"))
                        {
                            string targetProbeId = textMsg.Substring("LAN_PROBE:".Length).Trim().Replace(" ", "");
                            string myId = Program.CurrentHostId != null ? Program.CurrentHostId.Trim().Replace(" ", "") : "";
                            if (!string.IsNullOrEmpty(myId) && myId != "---" && targetProbeId == myId)
                            {
                                string realLanIp = Program.GetLocalLanIPAddress();
                                byte[] probeAck = Encoding.UTF8.GetBytes($"LAN_PROBE_ACK:{targetProbeId}:{realLanIp}");
                                await _udpClient.SendAsync(probeAck, probeAck.Length, senderEp);
                            }
                        }
                        else if (textMsg.StartsWith("LAN_PROBE_ACK:"))
                        {
                            var parts = textMsg.Split(':');
                            if (parts.Length >= 3)
                            {
                                string ackId = parts[1].Trim();
                                string discoveredIp = parts[2].Trim();
                                if (_pendingLanProbes.TryGetValue(ackId, out var tcs))
                                {
                                    tcs.TrySetResult(discoveredIp);
                                }
                            }
                        }
                        else
                        {
                            _remoteEndpoint = senderEp;
                            _isP2pConnected = true;
                            OnP2pPacketReceived?.Invoke(data);
                        }
                    }
                }
                catch { }
            }
        }

        public static void Shutdown()
        {
            _isP2pConnected = false;
            try { _cts?.Cancel(); } catch { }
            try { _udpClient?.Close(); _udpClient?.Dispose(); } catch { }
            _udpClient = null;
            _remoteEndpoint = null;
        }
    }
}
