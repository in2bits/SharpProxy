using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace SharpProxy
{
    public class ProxyEngine : IWebProxy, IDisposable
    {
        private ProxyListener _listener;

        private ProxyEngine(IPAddress ipAddress, int port, bool autoStart = true)
        {
            _listener = new ProxyListener(port, ipAddress);
            var uriBuilder = new UriBuilder("http", ipAddress.ToString(), port);
            Uri = uriBuilder.Uri;
            if (autoStart)
                _listener.Start();
        }

        public static ProxyEngine New(bool autoStart = true)
        {
            var port = 8088;
            var ip = LocalIPAddress(); //the ip of the machine on which the proxy is running/listening
            return new ProxyEngine(ip, port, autoStart);
        }
        
        private static IPAddress LocalIPAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            return host
                .AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }

        private Uri Uri { get; set; }

        Uri IWebProxy.GetProxy(Uri uri)
        {
            return Uri;
        }

        bool IWebProxy.IsBypassed(Uri host)
        {
            return false;
        }

        ICredentials IWebProxy.Credentials { get; set; }

        public void Dispose()
        {
            if (_listener == null)
                return;
            if (_listener.IsListening)
                _listener.Stop();
            _listener = null;
        }
    }
}
