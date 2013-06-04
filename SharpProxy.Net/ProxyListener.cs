using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SharpProxy
{
    //http://msdn.microsoft.com/en-us/library/system.net.httplistener.aspx
    class ProxyListener : IProxyListener
    {
        private readonly TcpListener _listener;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="address"></param>
        public ProxyListener(Int32 port, string address)
        {
            try
            {
                var localAddress = IPAddress.Parse(address);
                _listener = new TcpListener(localAddress, port);
            }
            catch (Exception e)
            {
                throw new NotSupportedException("TcpListener not supported on this platform.", e);
            }

            ProxyRequest.SetFiddlerCert();
        }

        private bool IsListening { get; set; }

        bool IProxyListener.IsListening { get { return IsListening; } }
 
        void IProxyListener.Start()
        {
            _listener.Start();
            IsListening = true;
            Task.Run((Action) WaitForRequest);
            Console.WriteLine("Proxy listening...");
        }

        void IProxyListener.Stop()
        {
            _listener.Stop();
            IsListening = false;
            Console.WriteLine("Proxy not listening...");
        }

        private async void WaitForRequest()
        {
            TcpClient client = null;
            try
            {
                client = await Task.Factory.FromAsync<TcpClient>(_listener.BeginAcceptTcpClient, _listener.EndAcceptTcpClient, null);
            }
            catch (Exception)
            {
                if (Debugger.IsAttached)
                    Debugger.Break();
            }
            finally
            {
                if (IsListening)
                    WaitForRequest();
            }
            if (client == null)
                return;
            HandleRequest(client);
        }

        async private void HandleRequest(TcpClient client)
        {
            if (client == null)
                throw new ArgumentNullException("client");

            try
            {
                var proxyRequest = new ProxyRequest(client.GetStream());
                await proxyRequest.Process();
            }
            catch (Exception)
            {
            }
            finally
            {
                client.Close();
            }
        }
    }
}
