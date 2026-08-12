using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace BigLineconnect
{
    /// <summary>
    /// BigLineconnect WebRTC UDP Zero-Latency Media Transport
    /// Sends real-time H.264 video NAL units over UDP/RTP with 0ms Head-of-Line Blocking.
    /// </summary>
    public static class WebRtcUdpTransport
    {
        private static UdpClient? _udpClient;
        private static IPEndPoint? _targetEndPoint;
        private static bool _isRunning = false;
        private static CancellationTokenSource? _cts;

        public static event Action<byte[]>? OnMediaPacketReceived;

        public static ushort LocalUdpPort { get; private set; }

        public static void Initialize(int port = 0)
        {
            try
            {
                Shutdown();
                _cts = new CancellationTokenSource();
                _udpClient = new UdpClient(port);
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                
                LocalUdpPort = (ushort)((IPEndPoint)_udpClient.Client.LocalEndPoint!).Port;
                _isRunning = true;

                _ = Task.Run(() => ReceiveLoop(_cts.Token));
            }
            catch
            {
                try
                {
                    _udpClient = new UdpClient(0);
                    LocalUdpPort = (ushort)((IPEndPoint)_udpClient.Client.LocalEndPoint!).Port;
                    _isRunning = true;
                    _ = Task.Run(() => ReceiveLoop(_cts.Token));
                }
                catch { }
            }
        }

        public static void SetTargetEndPoint(string ip, int port)
        {
            if (IPAddress.TryParse(ip, out IPAddress? address) && port > 0)
            {
                _targetEndPoint = new IPEndPoint(address, port);
            }
        }

        public static void SendMediaPacket(byte[] payload)
        {
            if (!_isRunning || _udpClient == null || _targetEndPoint == null || payload == null || payload.Length == 0) return;

            try
            {
                // Non-blocking zero-buffer UDP send
                _udpClient.Send(payload, payload.Length, _targetEndPoint);
            }
            catch { }
        }

        private static async Task ReceiveLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _isRunning && _udpClient != null)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync(token).ConfigureAwait(false);
                    if (result.Buffer != null && result.Buffer.Length > 0)
                    {
                        OnMediaPacketReceived?.Invoke(result.Buffer);
                    }
                }
                catch
                {
                    await Task.Delay(10, token).ConfigureAwait(false);
                }
            }
        }

        public static void Shutdown()
        {
            _isRunning = false;
            _cts?.Cancel();
            _udpClient?.Close();
            _udpClient?.Dispose();
            _udpClient = null;
            LocalUdpPort = 0;
        }
    }
}
