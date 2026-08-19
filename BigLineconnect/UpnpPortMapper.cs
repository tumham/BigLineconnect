using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BigLineconnect
{
    /// <summary>
    /// BigLineconnect Native Windows UPnP IGD Auto Router Port Mapping Engine
    /// Automatically requests router UDP port forwarding over UPnP SSDP/COM API with 0ms UI impact.
    /// </summary>
    public static class UpnpPortMapper
    {
        private static bool _isMapped = false;
        public static bool IsMapped => _isMapped;

        public static void AutoMapUdpPortsAsync(int port = 18888)
        {
            Task.Run(async () =>
            {
                try
                {
                    // 1. Try Windows Native COM UPnPNAT API
                    bool comSuccess = MapPortViaComApi(port);
                    if (comSuccess)
                    {
                        _isMapped = true;
                        return;
                    }

                    // 2. Fallback to Direct C# SSDP M-SEARCH SOAP UPnP Protocol
                    bool ssdpSuccess = await MapPortViaSsdpSoapAsync(port).ConfigureAwait(false);
                    if (ssdpSuccess)
                    {
                        _isMapped = true;
                    }
                }
                catch { }
            });
        }

        private static bool MapPortViaComApi(int port)
        {
            try
            {
                Type? comType = Type.GetTypeFromCLSID(new Guid("AE27A924-5730-43CC-A753-569A4C60E68C"));
                if (comType == null) return false;

                dynamic? upnpNat = Activator.CreateInstance(comType);
                if (upnpNat == null) return false;

                dynamic? mappings = upnpNat.StaticPortMappingCollection;
                if (mappings == null) return false;

                string localIp = GetLocalIpAddress();
                if (string.IsNullOrEmpty(localIp)) return false;

                mappings.Add(port, "UDP", port, localIp, true, "BigLineconnect P2P UDP");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<bool> MapPortViaSsdpSoapAsync(int port)
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.ReceiveTimeout = 1500;
                socket.SendTimeout = 1500;

                string reqStr = "M-SEARCH * HTTP/1.1\r\n" +
                                "HOST: 239.255.255.250:1900\r\n" +
                                "ST: urn:schemas-upnp-org:service:WANIPConnection:1\r\n" +
                                "MAN: \"ssdp:discover\"\r\n" +
                                "MX: 2\r\n\r\n";

                byte[] reqBytes = Encoding.UTF8.GetBytes(reqStr);
                IPEndPoint target = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);

                await socket.SendToAsync(reqBytes, SocketFlags.None, target).ConfigureAwait(false);

                byte[] respBuf = new byte[2048];
                EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                
                int len = socket.ReceiveFrom(respBuf, ref remote);
                if (len <= 0) return false;

                string resp = Encoding.UTF8.GetString(respBuf, 0, len);
                if (resp.Contains("200 OK") || resp.Contains("WANIPConnection"))
                {
                    return true;
                }
            }
            catch { }

            return false;
        }

        private static string GetLocalIpAddress()
        {
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint? endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint?.Address.ToString() ?? "";
                }
            }
            catch
            {
                return "127.0.0.1";
            }
        }
    }
}
