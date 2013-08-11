using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using IPHelper;

namespace SharpProxy
{
    public class ProxyRequest
    {
        public int ClientPid { get; protected set; }
        public Socket ClientSocket { get; set; }
        public NetworkStream ClientStream { get; set; }

        public Socket RemoteSocket { get; set; }
        public NetworkStream RemoteStream { get; set; }

        public HttpRequestPrologue Prologue { get; set; }

        public ProxyResponse Response { get; set; }

        protected ProxyRequest()
        {
        }

        public static ProxyRequest For(Socket clientSocket)
        {
            var ipEndpoint = clientSocket.RemoteEndPoint as IPEndPoint;
            var pid = 0;
            if (ipEndpoint != null)
                pid = ResolvePid(ipEndpoint);

            var request = new ProxyRequest();

            request.ClientPid = pid;
            request.ClientSocket = clientSocket;
            request.ClientStream = new NetworkStream(clientSocket);

            request.Prologue = HttpRequestPrologue.From(request.ClientStream);

            return request;
        }

        private static int ResolvePid(IPEndPoint ipEndPoint)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var port = ipEndPoint.Port;
            var tcpTable = Functions.GetExtendedTcpTable(false, Win32Funcs.TcpTableType.OwnerPidAll);
            var clientRow = tcpTable.FirstOrDefault(x => Equals(x.LocalEndPoint, ipEndPoint));
            var pid = clientRow.ProcessId;
            stopwatch.Stop();
            return pid;
        }

        async public Task Process()
        {
            Debug.WriteLine("Processing");
            if (Prologue.Method == "CONNECT")
            {
                var proxySslResponse = new ProxySslResponse(Prologue.Version, HttpStatusCode.OK, "Connection Established");
                proxySslResponse.Prologue.WriteTo(ClientStream);
                //await ClientStream.FlushAsync();

                var sslRequest = await SslProxyRequest.For(this);
                await sslRequest.Process();

                Debug.WriteLine("Processed SSL");
                return;
            }

            Debug.WriteLine("GetViaSocket " + Prologue.Destination);

            await InitRemoteSocket();
            await InitRemoteStream();

            await WritePrologueToRemote();

            await CopyContentFromClientToServer();

            await GetResponse();

            await WriteResponseToClient();

            End();

            Debug.WriteLine("Processed");
        }

        protected async virtual Task WritePrologueToRemote()
        {
            await Prologue.WriteTo(RemoteStream);
            await RemoteStream.FlushAsync();

        }

        protected async virtual Task GetResponse()
        {
            Debug.WriteLine("Getting Response");
            Response = await ProxyResponse.From(RemoteSocket, RemoteStream);
        }

        protected async virtual Task WriteResponseToClient()
        {
            Debug.WriteLine("Relaying Response");
            await Response.WriteTo(ClientStream);
            //await ClientStream.FlushAsync();
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

        private async Task InitRemoteSocket()
        {
            var ipEndpoint = await GetIPEndpoint();

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var args = new SocketAsyncEventArgs
                {
                    RemoteEndPoint = ipEndpoint
                };
            var completionSource = new TaskCompletionSource<bool>();
            args.Completed += (sender, eventArgs) => completionSource.SetResult(true);
            Debug.WriteLine("Connect socket");
            socket.ConnectAsync(args);
            await completionSource.Task;
            RemoteSocket = socket;
        }

        protected virtual async Task CopyContentFromClientToServer()
        {
            Debug.WriteLine("Reading Request Content");
            long contentLength;
            if (!long.TryParse(Prologue.Headers.FirstOrDefault(x => x.Key == "Content-Length").Value, out contentLength))
                contentLength = -1;
            await ClientStream.CopyHttpMessageToAsync(ClientSocket, RemoteStream, contentLength);
            await RemoteStream.FlushAsync();
        }

        protected virtual Task InitRemoteStream()
        {
            RemoteStream = new NetworkStream(RemoteSocket);
            return Task.FromResult(0);
        }

        async protected virtual Task<IPEndPoint> GetIPEndpoint()
        {
            var uri = new Uri(Prologue.Destination, UriKind.Absolute);
            //var scheme = uri.Scheme;
            var host = uri.Host;
            var port = uri.Port;
            //var path = uri.AbsolutePath;

            Debug.WriteLine("Resolve DNS");
            var ipAddresses = await Dns.GetHostAddressesAsync(host);

            if (!ipAddresses.Any())
            {
                DnsResolutionError();
                return null;
            }

            var ipAddress = ipAddresses.First();
            var ipEndpoint = new IPEndPoint(ipAddress, port);
            return ipEndpoint;
        }

        protected void DnsResolutionError()
        {
            if (Debugger.IsAttached)
                Debugger.Break();
        }
    }
}