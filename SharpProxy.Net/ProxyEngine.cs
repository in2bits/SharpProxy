using System;

namespace SharpProxy
{
    public class ProxyEngine : IProxyEngine, IDisposable
    {
        private IProxyListener _listener;

        private ProxyEngine(bool autoStart = true)
        {
            var port = 8088;
            var ipString = "192.168.1.100"; //the ip of the machine on which the proxy is running/listening
            _listener = new ProxyListener(port, ipString);
            var uriBuilder = new UriBuilder("http", ipString, port);
            Uri = uriBuilder.Uri;
            if (autoStart)
                _listener.Start();
        }

        public static IProxyEngine New(bool autoStart = true)
        {
            return new ProxyEngine(autoStart);
        }

        private Uri Uri { get; set; }

        Uri IProxyEngine.Uri { get { return Uri; } }

        bool IProxyEngine.IsBypassed(Uri host)
        {
            return false;
        }

        IProxyListener IProxyEngine.Listener { get { return _listener; } }
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
