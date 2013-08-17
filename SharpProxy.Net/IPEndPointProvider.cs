using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace SharpProxy
{
    public static class IPEndPointProvider
    {
        private static readonly Dictionary<string, IPAddress> _cache = new Dictionary<string, IPAddress>();
        private static readonly Dictionary<string, Task<IPAddress>> _retrieving = new Dictionary<string, Task<IPAddress>>(); 

        async public static Task<IPEndPoint> Get(string host, int port)
        {
            lock (_cache)
            {
                if (_cache.ContainsKey(host))
                    return new IPEndPoint(_cache[host], port);
            }

            Task<IPAddress> addressTask = null;
            lock (_retrieving)
            {
                if (_retrieving.ContainsKey(host))
                {
                    addressTask = _retrieving[host];
                }
                else
                {
                    addressTask = Dns.GetHostAddressesAsync(host).ContinueWith(task =>
                        {
                            IPAddress newIpAddress = null;
                            if (!task.IsFaulted && task.Result != null)
                                newIpAddress = task.Result.FirstOrDefault();

                            lock (_cache)
                            {
                                _cache[host] = newIpAddress;
                            }

                            return newIpAddress;
                        });
                    _retrieving[host] = addressTask;
                }
            }

            var ipAddress = await addressTask;
            if (ipAddress == null)
                return null;
            return new IPEndPoint(ipAddress, port);
        }

        private static void DnsResolutionError()
        {
            if (Debugger.IsAttached)
                Debugger.Break();
        }
    }
}