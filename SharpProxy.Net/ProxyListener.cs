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
        //private readonly TcpListener _listener;

        //https://gist.github.com/leandrosilva/656054
        private readonly Socket _proxySocket;
        private readonly IPAddress _localAddress;
        private readonly Int32 _port;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"></param>
        /// <param name="ipAddress"></param>
        public ProxyListener(Int32 port, IPAddress ipAddress)
        {
            try
            {
                _localAddress = ipAddress;
                _port = port;
                //_listener = new TcpListener(localAddress, port);
                _proxySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
            //_listener.Start();
            _proxySocket.Bind(new IPEndPoint(_localAddress, _port));
            _proxySocket.Listen(25);
            IsListening = true;
            Task.Run((Action) WaitForRequest);
            Console.WriteLine("Proxy listening...");
        }

        void IProxyListener.Stop()
        {
            //_listener.Stop();
            _proxySocket.Shutdown(SocketShutdown.Both);
            IsListening = false;
            Console.WriteLine("Proxy not listening...");
        }

        private async void WaitForRequest()
        {
            //TcpClient client = null;
            Socket clientSocket = null;
            try
            {
                //client = await Task.Factory.FromAsync<TcpClient>(_listener.BeginAcceptTcpClient, _listener.EndAcceptTcpClient, null);
                clientSocket = await Task.Factory.FromAsync<Socket>(_proxySocket.BeginAccept, _proxySocket.EndAccept, null);
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
            if (clientSocket == null)
                return;
            //var clientSocket = await clientSocketTask;
            HandleRequest(clientSocket);
        }

        async private void HandleRequest(Socket clientSocket)
        {
            if (clientSocket == null)
                throw new ArgumentNullException("client");

            try
            {
                //clientSocket.B
                var proxyRequest = new ProxyRequest(clientSocket);
                await proxyRequest.Process();
            }
            catch (Exception)
            {
            }
            finally
            {
                clientSocket.Close();
            }
        }
    }
}
