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

        public static ushort LocalUdpPort { get; private set; } = 0;

        public static void Initialize(int preferredPort = 0)
        {
            try
            {
                Shutdown();
                _cts = new CancellationTokenSource();
                _udpClient = new UdpClient(preferredPort);
                _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                
                LocalUdpPort = (ushort)((IPEndPoint)_udpClient.Client.LocalEndPoint!).Port;

                _ = Task.Run(() => ListenUdpLoop(_cts.Token));
            }
            catch
            {
                try
                {
                    _udpClient = new UdpClient(0);
                    LocalUdpPort = (ushort)((IPEndPoint)_udpClient.Client.LocalEndPoint!).Port;
                    _ = Task.Run(() => ListenUdpLoop(_cts.Token));
                }
                catch { }
            }
        }

        public static async Task PunchHoleAndConnectAsync(string remoteIp, int remotePort)
        {
            if (_udpClient == null || string.IsNullOrEmpty(remoteIp) || remotePort <= 0) return;

            try
            {
                _remoteEndpoint = new IPEndPoint(IPAddress.Parse(remoteIp), remotePort);
                byte[] pingPacket = Encoding.UTF8.GetBytes("P2P_PING");

                // Send 5 UDP ping packets to punch NAT hole
                for (int i = 0; i < 5; i++)
                {
                    await _udpClient.SendAsync(pingPacket, pingPacket.Length, _remoteEndpoint);
                    await Task.Delay(50);
                }
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
                        _remoteEndpoint = senderEp;
                        _isP2pConnected = true;
                        OnP2pPacketReceived?.Invoke(data);
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
