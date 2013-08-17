using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SharpProxy
{
    //http://msdn.microsoft.com/en-us/library/system.net.httplistener.aspx
    class ProxyListener
    {
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
                _proxySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            catch (Exception e)
            {
                throw new NotSupportedException("TcpListener not supported on this platform.", e);
            }
        }

        public bool IsListening { get; set; }
 
        public void Start()
        {
            _proxySocket.Bind(new IPEndPoint(_localAddress, _port));
            _proxySocket.Listen(25);
            IsListening = true;
            Task.Run(async () => { await WaitForRequest();} );
            Console.WriteLine("Proxy listening on " + _localAddress + ":" + _port + " ...");
        }

        public void Stop()
        {
            _proxySocket.Shutdown(SocketShutdown.Both);
            IsListening = false;
            Console.WriteLine("Proxy not listening...");
        }

        private async Task WaitForRequest()
        {
            Socket clientSocket = null;
            try
            {
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
            HandleRequest(clientSocket);
        }

        async private void HandleRequest(Socket clientSocket)
        {
            if (clientSocket == null)
                throw new ArgumentNullException("clientSocket");

            try
            {
                var socketRequests = 0;
                while (clientSocket.Connected)
                {
                    socketRequests++;
                    var proxyRequest = ProxyRequest.For(clientSocket);
                    if (socketRequests > 1)
                        Debug.WriteLine("Client Connection Reused #" + socketRequests);
                    await proxyRequest.Process();
                    if (!proxyRequest.Response.Prologue.Headers.ContainsIgnoreCase("Connection", "Keep-Alive"))
                    {
                        Debug.WriteLine("Client Connection Closed");
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Socket Error: " + e.Message);
            }
            finally
            {
                clientSocket.Close();
            }
        }
    }
}
