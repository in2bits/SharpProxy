using System.Net;

namespace SharpProxy.Adapter.Net
{
    public static class SharpProxyExtensions
    {
        public static IWebProxy ToProxy(this IProxyEngine proxyEngine)
        {
            return new SharpWebProxy(proxyEngine);
        }
    }
}
