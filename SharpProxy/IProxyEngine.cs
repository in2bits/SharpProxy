using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpProxy
{
    public interface IProxyEngine
    {
        Uri Uri { get; }
        IProxyListener Listener { get; }
        bool IsBypassed(Uri host);
    }
}
