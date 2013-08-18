using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using IPHelper;

namespace SharpProxy
{
    public class ProxyRequest
    {
        public int ClientPid { get; protected set; }
        public Socket ClientSocket { get; set; }
        public NetworkStream ClientStream { get; set; }

        protected IPEndPoint RemoteEndPoint { get; set; }
        public Socket RemoteSocket { get; set; }
        public NetworkStream RemoteStream { get; set; }

        public HttpRequestPrologue Prologue { get; set; }

        public ProxyResponse Response { get; set; }

        protected ProxyRequest()
        {
        }

        public static ProxyRequest For(Socket clientSocket)
        {
            var clientIpEndpoint = clientSocket.RemoteEndPoint as IPEndPoint;
            var pid = 0;
            if (clientIpEndpoint != null)
                pid = ResolvePid(clientIpEndpoint);

            var request = new ProxyRequest
                {
                    ClientPid = pid,
                    ClientSocket = clientSocket,
                    ClientStream = new NetworkStream(clientSocket)
                };

            request.Prologue = HttpRequestPrologue.From(request.ClientStream);

            return request;
        }

        private static int ResolvePid(IPEndPoint ipEndPoint)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var tcpTable = Functions.GetExtendedTcpTable(false, Win32Funcs.TcpTableType.OwnerPidAll);
            var clientRow = tcpTable.FirstOrDefault(x => Equals(x.LocalEndPoint, ipEndPoint));
            var pid = clientRow.ProcessId;
            stopwatch.Stop();
            return pid;
        }

        async public Task Process()
        {
            Debug.Write("Request: ");
            if (Prologue.Method == "CONNECT")
            {
                Debug.Write("SSL: ");

                var proxySslResponse = new ProxySslResponse(Prologue.Version, HttpStatusCode.OK, "Connection Established");
                proxySslResponse.Prologue.WriteTo(ClientStream);

                var sslRequest = await ProxySslRequest.For(this);
                await sslRequest.Process();

                return;
            }

            await InitRemoteSocket();
            await InitRemoteStream();

            await WritePrologueToRemote();

            await CopyContentFromClientToServer();

            await GetResponse();

            await WriteResponseToClient();

            //End();

            if (RemoteSocket != null && Response.Prologue.Headers.ContainsIgnoreCase("Connection", "Keep-Alive"))
            {
                RemoteSocketProvider.Return(RemoteEndPoint, RemoteSocket);
            }
            else
            {
                End();
            }

            Debug.WriteLine("Done");
        }

        private async Task InitRemoteSocket()
        {
            RemoteEndPoint = await GetIPEndpoint();
            RemoteSocket = await RemoteSocketProvider.Get(RemoteEndPoint);
        }

        protected virtual Task InitRemoteStream()
        {
            RemoteStream = new NetworkStream(RemoteSocket);
            return Task.FromResult(0);
        }

        protected async virtual Task WritePrologueToRemote()
        {
            await Prologue.WriteTo(RemoteStream);
        }

        protected virtual async Task CopyContentFromClientToServer()
        {
            //Debug.WriteLine("Reading Request Content");
            long contentLength;
            if (!long.TryParse(Prologue.Headers.FirstOrDefault(x => x.Key == "Content-Length").Value, out contentLength))
                contentLength = -1;
            await ClientStream.CopyHttpMessageToAsync(ClientSocket, RemoteStream, contentLength);
        }

        protected async virtual Task GetResponse()
        {
            //Debug.WriteLine("Getting Response");
            Response = await ProxyResponse.From(RemoteSocket, RemoteStream);
        }

        protected async virtual Task WriteResponseToClient()
        {
            //Debug.WriteLine("Relaying Response");
            await Response.WriteTo(ClientStream);
        }

        protected virtual void End()
        {
            RemoteStream.Close();

            RemoteSocket.Shutdown(SocketShutdown.Both);
            RemoteSocket.Close();

            ClientStream.Close();

            ClientSocket.Shutdown(SocketShutdown.Both);
            ClientSocket.Close();
        }

        async protected virtual Task<IPEndPoint> GetIPEndpoint()
        {
            var uri = new Uri(Prologue.Destination, UriKind.Absolute);
            var host = uri.Host;
            var port = uri.Port;

            var ipEndPoint = await IPEndPointProvider.Get(host, port);
            return ipEndPoint;
        }
    }
}