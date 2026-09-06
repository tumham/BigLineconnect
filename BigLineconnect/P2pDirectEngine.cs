using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BigLineconnect
{
    /// <summary>
    /// BigLineconnect P2P Direct Engine:
    /// STUN-assisted UDP Hole Punching & Zero-Cloud Latency Direct WAN/LAN Media & Input Transport.
    /// </summary>
    public static class P2pDirectEngine
    {
        private static UdpClient? _udpClient;
        private static IPEndPoint? _remoteEndpoint;
        private static volatile bool _isP2pConnected = false;
        private static CancellationTokenSource? _cts;

        public static bool IsP2pConnected => _isP2pConnected;
        public static IPEndPoint? RemoteEndpoint => _remoteEndpoint;

        public static ushort LocalUdpPort { get; private set; } = 0;
        public static string? PublicIp { get; private set; }
        public static int PublicPort { get; private set; } = 0;

        public static event Action<byte[]>? OnP2pPacketReceived;
        public static event Action<byte[]>? OnFrameReceived;
        public static event Action? OnP2pConnected;
        public static event Action? OnP2pDisconnected;
        public static event Action<string, int>? OnStunResolved;

        private static TaskCompletionSource<IPEndPoint?> _stunTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private static DateTime _lastUdpTrafficTime = DateTime.UtcNow;

        private static readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingLanProbes = new();

        // Frame reassembly buffer for UDP video frames
        private static readonly ConcurrentDictionary<ushort, FrameReassembly> _reassemblyBuffers = new();
        private static ushort _lastFrameId = 0;
        private static int _nextOutFrameId = 0;

        private class FrameReassembly
        {
            public ushort TotalChunks;
            public byte[][] Chunks;
            public int ReceivedCount;
            public DateTime CreatedAt;

            public FrameReassembly(ushort totalChunks)
            {
                TotalChunks = totalChunks;
                Chunks = new byte[totalChunks][];
                ReceivedCount = 0;
                CreatedAt = DateTime.UtcNow;
            }
        }

        public static void Initialize(int preferredPort = 0)
        {
            try
            {
                Shutdown();
                _cts = new CancellationTokenSource();
                _stunTcs = new TaskCompletionSource<IPEndPoint?>(TaskCreationOptions.RunContinuationsAsynchronously);
                _lastUdpTrafficTime = DateTime.UtcNow;
                
                try
                {
                    _udpClient = new UdpClient(preferredPort);
                }
                catch
                {
                    _udpClient = new UdpClient(0);
                }

                _udpClient.EnableBroadcast = true;
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // Prevent Windows from closing UDP socket or throwing 10054 on ICMP Port Unreachable
                try
                {
                    const int SIO_UDP_CONNRESET = -1744830452; // 0x9800000C
                    byte[] inValue = new byte[] { 0 };
                    byte[] outValue = new byte[] { 0 };
                    _udpClient.Client.IOControl(SIO_UDP_CONNRESET, inValue, outValue);
                }
                catch { }
                
                // Allow OS buffer to hold UDP bursts without dropping packets
                try
                {
                    _udpClient.Client.ReceiveBufferSize = 1024 * 1024 * 2; // 2MB
                    _udpClient.Client.SendBufferSize = 1024 * 1024 * 2;    // 2MB
                }
                catch { }

                LocalUdpPort = (ushort)((IPEndPoint)_udpClient.Client.LocalEndPoint!).Port;

                // 1. Single UDP consumer loop (handles STUN replies, hole-punch pings/pongs, and video/input packets)
                _ = Task.Run(() => ListenUdpLoop(_cts.Token));

                // 2. Discover external public IP & NAT mapped port via RFC 5389 STUN in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (_udpClient != null)
                        {
                            await StunClient.SendBindingRequestsAsync(_udpClient).ConfigureAwait(false);
                        }
                    }
                    catch { }
                });

                // 3. Watchdog: monitor UDP traffic, keep NAT alive, and auto-repunch if link drops
                _ = Task.Run(async () =>
                {
                    CancellationToken token = _cts.Token;
                    while (!token.IsCancellationRequested)
                    {
                        try { await Task.Delay(1500, token).ConfigureAwait(false); } catch { break; }
                        double silenceSec = (DateTime.UtcNow - _lastUdpTrafficTime).TotalSeconds;
                        if (_isP2pConnected && silenceSec > 2.0)
                        {
                            // Trigger background re-punch to refresh router NAT mappings before dropping!
                            TriggerAutoRePunch();
                        }
                        if (_isP2pConnected && silenceSec > 4.0)
                        {
                            _isP2pConnected = false;
                            OnP2pDisconnected?.Invoke();
                            TriggerAutoRePunch();
                        }
                    }
                });
            }
            catch { }
        }

        public static async Task<IPEndPoint?> EnsureStunResolvedAsync(int timeoutMs = 1500)
        {
            if (!string.IsNullOrEmpty(PublicIp) && PublicPort > 0 && IPAddress.TryParse(PublicIp, out var parsedIp))
            {
                return new IPEndPoint(parsedIp, PublicPort);
            }

            if (_udpClient != null)
            {
                try { _ = StunClient.SendBindingRequestsAsync(_udpClient); } catch { }
            }

            var currentTcs = _stunTcs;
            if (currentTcs != null)
            {
                var completed = await Task.WhenAny(currentTcs.Task, Task.Delay(timeoutMs)).ConfigureAwait(false);
                if (completed == currentTcs.Task)
                {
                    return await currentTcs.Task.ConfigureAwait(false);
                }
            }
            return null;
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

        private static string? _lastRemotePublicIp;
        private static int _lastRemotePublicPort;
        private static string? _lastRemoteLanIp;
        private static int _lastRemoteLanPort;
        private static DateTime _lastRePunchTime = DateTime.MinValue;

        public static void TriggerAutoRePunch()
        {
            if ((DateTime.UtcNow - _lastRePunchTime).TotalSeconds < 3.0) return;
            _lastRePunchTime = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(_lastRemotePublicIp) && _lastRemotePublicPort > 0)
            {
                StartHolePunch(_lastRemotePublicIp, _lastRemotePublicPort, _lastRemoteLanIp, _lastRemoteLanPort, isRePunch: true);
            }
        }

        /// <summary>
        /// Starts UDP Hole Punching to the target peer's public (with CGNAT sequential deltas) and LAN endpoints.
        /// </summary>
        public static void StartHolePunch(string remotePublicIp, int remotePublicPort, string? remoteLanIp = null, int remoteLanPort = 0, bool isRePunch = false)
        {
            if (_udpClient == null) return;
            CancellationToken token = _cts?.Token ?? CancellationToken.None;

            _lastRemotePublicIp = remotePublicIp;
            _lastRemotePublicPort = remotePublicPort;
            _lastRemoteLanIp = remoteLanIp;
            _lastRemoteLanPort = remoteLanPort;

            if (!isRePunch)
            {
                // Reset connection state for new initial hole punch session
                _isP2pConnected = false;
                _remoteEndpoint = null;
                _reassemblyBuffers.Clear();
            }
            _lastUdpTrafficTime = DateTime.UtcNow;

            var targetEndpoints = new List<IPEndPoint>();

            if (IPAddress.TryParse(remotePublicIp, out var pIp) && remotePublicPort > 0)
            {
                // Primary public mapped endpoint
                targetEndpoints.Add(new IPEndPoint(pIp, remotePublicPort));

                // Sequential port deltas for CGNAT / Symmetric NAT (+1, -1, +2, -2, +3, -3, +4, -4)
                int[] deltas = new[] { 1, -1, 2, -2, 3, -3, 4, -4 };
                foreach (int d in deltas)
                {
                    int shifted = remotePublicPort + d;
                    if (shifted >= 1024 && shifted <= 65535)
                    {
                        targetEndpoints.Add(new IPEndPoint(pIp, shifted));
                    }
                }
            }

            if (!string.IsNullOrEmpty(remoteLanIp) && IPAddress.TryParse(remoteLanIp, out var lIp))
            {
                targetEndpoints.Add(new IPEndPoint(lIp, remoteLanPort > 0 ? remoteLanPort : 18888));
            }

            if (targetEndpoints.Count == 0) return;

            _ = Task.Run(async () =>
            {
                byte[] pingPacket = Encoding.UTF8.GetBytes("P2P_PING");
                // Rapid punch bursts: send burst packets at 35ms intervals
                int rounds = isRePunch ? 15 : 35;
                for (int i = 0; i < rounds && !token.IsCancellationRequested; i++)
                {
                    if (_isP2pConnected && !isRePunch) break;

                    foreach (var ep in targetEndpoints)
                    {
                        try
                        {
                            if (_udpClient != null)
                                await _udpClient.SendAsync(pingPacket, pingPacket.Length, ep).ConfigureAwait(false);
                        }
                        catch { }
                    }

                    await Task.Delay(35, token).ConfigureAwait(false);
                }

                if (isRePunch) return; // Keep existing keep-alive loop alive

                // Continuous Keep-Alive & Auto-Heal Loop:
                // Rapid 400ms heartbeat keeps domestic router NAT port mapping permanently OPEN
                while (!token.IsCancellationRequested && _udpClient != null)
                {
                    if (_remoteEndpoint != null)
                    {
                        try
                        {
                            await _udpClient.SendAsync(pingPacket, pingPacket.Length, _remoteEndpoint).ConfigureAwait(false);
                        }
                        catch { }
                    }
                    else
                    {
                        // Periodically probe target endpoints if hole hasn't opened yet
                        foreach (var ep in targetEndpoints)
                        {
                            try { if (_udpClient != null) await _udpClient.SendAsync(pingPacket, pingPacket.Length, ep).ConfigureAwait(false); } catch { }
                        }
                    }
                    await Task.Delay(400, token).ConfigureAwait(false);
                }
            }, token);
        }

        public static bool SendP2pPacket(byte[] data)
        {
            if (_isP2pConnected && _udpClient != null && _remoteEndpoint != null)
            {
                try
                {
                    _udpClient.Send(data, data.Length, _remoteEndpoint);
                    return true;
                }
                catch
                {
                    _isP2pConnected = false;
                }
            }
            return false;
        }

        /// <summary>
        /// Sends video frames over UDP in MTU-safe chunks (~1200 bytes) with 0ms HoL blocking.
        /// </summary>
        public static void SendFrameChunks(byte[] frameData)
        {
            if (!_isP2pConnected || _udpClient == null || _remoteEndpoint == null || frameData == null || frameData.Length == 0) return;

            try
            {
                const int ChunkSize = 1200;
                ushort frameId = unchecked((ushort)Interlocked.Increment(ref _nextOutFrameId));
                ushort totalChunks = (ushort)((frameData.Length + ChunkSize - 1) / ChunkSize);

                for (ushort i = 0; i < totalChunks; i++)
                {
                    int offset = i * ChunkSize;
                    int len = Math.Min(ChunkSize, frameData.Length - offset);
                    byte[] packet = new byte[8 + len];
                    packet[0] = 0x50; // 'P'
                    packet[1] = (byte)(frameId >> 8);
                    packet[2] = (byte)(frameId & 0xFF);
                    packet[3] = (byte)(totalChunks >> 8);
                    packet[4] = (byte)(totalChunks & 0xFF);
                    packet[5] = (byte)(i >> 8);
                    packet[6] = (byte)(i & 0xFF);
                    packet[7] = 0;
                    Buffer.BlockCopy(frameData, offset, packet, 8, len);

                    _udpClient.Send(packet, packet.Length, _remoteEndpoint);

                    // Egress pacing: Yield 1ms every 8 chunks (~10 KB) to prevent socket buffer and router NAT overflow
                    if (totalChunks > 8 && (i % 8 == 7))
                    {
                        Thread.Sleep(1);
                    }
                }
            }
            catch { }
        }

        private static async Task ListenUdpLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _udpClient != null)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync(token).ConfigureAwait(false);
                    byte[] data = result.Buffer;
                    IPEndPoint senderEp = result.RemoteEndPoint;

                    // 1. RFC 5389 STUN Binding Success Response Parsing:
                    // 0x01, 0x01 = Binding Response; Magic Cookie at 4..7 = 0x2112A442
                    if (data.Length >= 20 && data[0] == 0x01 && data[1] == 0x01 && data[4] == 0x21 && data[5] == 0x12 && data[6] == 0xA4 && data[7] == 0x42)
                    {
                        var stunEp = StunClient.ParseResponse(data);
                        if (stunEp != null)
                        {
                            PublicIp = stunEp.Address.ToString();
                            PublicPort = stunEp.Port;
                            _stunTcs.TrySetResult(stunEp);
                            OnStunResolved?.Invoke(PublicIp, PublicPort);
                        }
                        continue;
                    }

                    if (data.Length >= 8 && Encoding.UTF8.GetString(data, 0, 8) == "P2P_PING")
                    {
                        _remoteEndpoint = senderEp;
                        _lastUdpTrafficTime = DateTime.UtcNow;
                        if (!_isP2pConnected)
                        {
                            _isP2pConnected = true;
                            OnP2pConnected?.Invoke();
                        }
                        byte[] pong = Encoding.UTF8.GetBytes("P2P_PONG");
                        await _udpClient.SendAsync(pong, pong.Length, senderEp).ConfigureAwait(false);
                    }
                    else if (data.Length >= 8 && Encoding.UTF8.GetString(data, 0, 8) == "P2P_PONG")
                    {
                        _remoteEndpoint = senderEp;
                        _lastUdpTrafficTime = DateTime.UtcNow;
                        if (!_isP2pConnected)
                        {
                            _isP2pConnected = true;
                            OnP2pConnected?.Invoke();
                        }
                    }
                    // Handle 0x50 Video Frame Chunk
                    else if (data.Length >= 8 && data[0] == 0x50)
                    {
                        _lastUdpTrafficTime = DateTime.UtcNow;
                        if (!_isP2pConnected)
                        {
                            _remoteEndpoint = senderEp;
                            _isP2pConnected = true;
                            OnP2pConnected?.Invoke();
                        }
                        ushort frameId = (ushort)((data[1] << 8) | data[2]);
                        ushort totalChunks = (ushort)((data[3] << 8) | data[4]);
                        ushort chunkIndex = (ushort)((data[5] << 8) | data[6]);

                        if (chunkIndex < totalChunks)
                        {
                            var reassembly = _reassemblyBuffers.GetOrAdd(frameId, id => new FrameReassembly(totalChunks));
                            
                            int chunkLen = data.Length - 8;
                            byte[] chunkData = new byte[chunkLen];
                            Buffer.BlockCopy(data, 8, chunkData, 0, chunkLen);

                            lock (reassembly)
                            {
                                if (reassembly.Chunks[chunkIndex] == null)
                                {
                                    reassembly.Chunks[chunkIndex] = chunkData;
                                    reassembly.ReceivedCount++;

                                    if (reassembly.ReceivedCount == reassembly.TotalChunks)
                                    {
                                        _reassemblyBuffers.TryRemove(frameId, out _);

                                        // Assemble entire frame
                                        int totalLen = 0;
                                        for (int c = 0; c < reassembly.TotalChunks; c++)
                                            totalLen += reassembly.Chunks[c].Length;

                                        byte[] fullFrame = new byte[totalLen];
                                        int dstOffset = 0;
                                        for (int c = 0; c < reassembly.TotalChunks; c++)
                                        {
                                            Buffer.BlockCopy(reassembly.Chunks[c], 0, fullFrame, dstOffset, reassembly.Chunks[c].Length);
                                            dstOffset += reassembly.Chunks[c].Length;
                                        }

                                        OnFrameReceived?.Invoke(fullFrame);
                                    }
                                }
                            }

                            // Cleanup stale frames older than 250ms
                            if (_reassemblyBuffers.Count > 6)
                            {
                                DateTime cutOff = DateTime.UtcNow.AddMilliseconds(-250);
                                foreach (var kvp in _reassemblyBuffers)
                                {
                                    if (kvp.Value.CreatedAt < cutOff)
                                        _reassemblyBuffers.TryRemove(kvp.Key, out _);
                                }
                            }
                        }
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
                                await _udpClient.SendAsync(probeAck, probeAck.Length, senderEp).ConfigureAwait(false);
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
                            if (_remoteEndpoint == null || senderEp.Address.Equals(_remoteEndpoint.Address))
                            {
                                _remoteEndpoint = senderEp;
                                _lastUdpTrafficTime = DateTime.UtcNow;
                                if (!_isP2pConnected)
                                {
                                    _isP2pConnected = true;
                                    OnP2pConnected?.Invoke();
                                }
                                OnP2pPacketReceived?.Invoke(data);
                            }
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
            _reassemblyBuffers.Clear();
        }
    }
}
