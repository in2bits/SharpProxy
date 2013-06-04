using System;
using System.Net;

namespace SharpProxy.Adapter.Net
{
    public class SharpWebProxy : IWebProxy
    {
        private readonly IProxyEngine _proxy;

        public SharpWebProxy(IProxyEngine proxyEngine)
        {
            _proxy = proxyEngine;
        }

        public Uri GetProxy(Uri destination)
        {
            return _proxy.Uri;
        }

        public bool IsBypassed(Uri host)
        {
            return _proxy.IsBypassed(host);
        }

        public ICredentials Credentials { get; set; }
    }
}