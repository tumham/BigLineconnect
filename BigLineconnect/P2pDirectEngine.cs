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
                
                // Allow OS buffer to hold UDP bursts without dropping packets
                try
                {
                    _udpClient.Client.ReceiveBufferSize = 1024 * 1024 * 2; // 2MB
                    _udpClient.Client.SendBufferSize = 1024 * 1024 * 2;    // 2MB
                }
                catch { }

                LocalUdpPort = (ushort)((IPEndPoint)_udpClient.Client.LocalEndPoint!).Port;

                _ = Task.Run(() => ListenUdpLoop(_cts.Token));

                // Discover external public IP & NAT mapped port via RFC 5389 STUN in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (_udpClient != null)
                        {
                            var stunEp = await StunClient.QueryExternalEndpointAsync(_udpClient, 2000).ConfigureAwait(false);
                            if (stunEp != null)
                            {
                                PublicIp = stunEp.Address.ToString();
                                PublicPort = stunEp.Port;
                            }
                        }
                    }
                    catch { }
                });
            }
            catch { }
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

        /// <summary>
        /// Starts UDP Hole Punching to the target peer's public and optional LAN endpoints.
        /// </summary>
        public static void StartHolePunch(string remotePublicIp, int remotePublicPort, string? remoteLanIp = null, int remoteLanPort = 0)
        {
            if (_udpClient == null) return;
            CancellationToken token = _cts?.Token ?? CancellationToken.None;

            IPEndPoint? publicEp = null;
            if (IPAddress.TryParse(remotePublicIp, out var pIp) && remotePublicPort > 0)
            {
                publicEp = new IPEndPoint(pIp, remotePublicPort);
            }

            IPEndPoint? lanEp = null;
            if (!string.IsNullOrEmpty(remoteLanIp) && IPAddress.TryParse(remoteLanIp, out var lIp))
            {
                lanEp = new IPEndPoint(lIp, remoteLanPort > 0 ? remoteLanPort : 18888);
            }

            if (publicEp == null && lanEp == null) return;

            _ = Task.Run(async () =>
            {
                byte[] pingPacket = Encoding.UTF8.GetBytes("P2P_PING");
                // Rapid punch bursts: send 10 packets at 50ms intervals
                for (int i = 0; i < 30 && !token.IsCancellationRequested; i++)
                {
                    if (_isP2pConnected) break;

                    try
                    {
                        if (publicEp != null && _udpClient != null)
                            await _udpClient.SendAsync(pingPacket, pingPacket.Length, publicEp).ConfigureAwait(false);
                        if (lanEp != null && _udpClient != null)
                            await _udpClient.SendAsync(pingPacket, pingPacket.Length, lanEp).ConfigureAwait(false);
                    }
                    catch { }

                    await Task.Delay(60, token).ConfigureAwait(false);
                }

                // Keep-alive loop once connected
                while (!token.IsCancellationRequested && _udpClient != null && _isP2pConnected && _remoteEndpoint != null)
                {
                    try
                    {
                        await _udpClient.SendAsync(pingPacket, pingPacket.Length, _remoteEndpoint).ConfigureAwait(false);
                    }
                    catch { }
                    await Task.Delay(3000, token).ConfigureAwait(false);
                }
            }, token);
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

                    if (data.Length >= 8 && Encoding.UTF8.GetString(data, 0, 8) == "P2P_PING")
                    {
                        _remoteEndpoint = senderEp;
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
                        if (!_isP2pConnected)
                        {
                            _isP2pConnected = true;
                            OnP2pConnected?.Invoke();
                        }
                    }
                    // Handle 0x50 Video Frame Chunk
                    else if (data.Length >= 8 && data[0] == 0x50)
                    {
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

                            // Cleanup stale frames older than 500ms
                            if (_reassemblyBuffers.Count > 10)
                            {
                                DateTime cutOff = DateTime.UtcNow.AddMilliseconds(-500);
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
                            if (_isP2pConnected && _remoteEndpoint != null && senderEp.Address.Equals(_remoteEndpoint.Address))
                            {
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
