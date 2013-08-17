using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SharpProxy
{
    public static class RemoteSocketProvider
    {
        private static readonly Dictionary<IPEndPoint, IList<Socket>> _cache = new Dictionary<IPEndPoint, IList<Socket>>();

        async public static Task<Socket> Get(IPEndPoint ipEndPoint)
        {
            lock (_cache)
            {
                if (_cache.ContainsKey(ipEndPoint))
                {
                    var sockets = _cache[ipEndPoint];
                    foreach (var oldSocket in sockets)
                    {
                        if (!oldSocket.Connected)
                            sockets.Remove(oldSocket);
                    }
                    if (sockets.Count > 0)
                    {
                        Debug.WriteLine("Re-Using Socket! (" + ipEndPoint + ")");
                        var firstSocket = sockets[0];
                        sockets.RemoveAt(0);
                        return firstSocket;
                    }
                }
            }

            var socket = await Task.Run(async () =>
                {
                    var newSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    var args = new SocketAsyncEventArgs
                        {
                            RemoteEndPoint = ipEndPoint
                        };
                    var completionSource = new TaskCompletionSource<bool>();
                    args.Completed += (sender, eventArgs) => completionSource.SetResult(true);
                    Debug.Write("Connecting " + ipEndPoint + "... ");
                    newSocket.ConnectAsync(args);
                    await completionSource.Task;
                    Debug.Write(" Connected!\r\n");
                    return newSocket;
                });
            return socket;
        }

        public static void Return(IPEndPoint ipEndPoint, Socket socket)
        {
            if (!socket.Connected)
                return;

            lock (_cache)
            {
                if (!_cache.ContainsKey(ipEndPoint))
                    _cache[ipEndPoint] = new List<Socket>();

                _cache[ipEndPoint].Add(socket);
            }
        }
    }
}