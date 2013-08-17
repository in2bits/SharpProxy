using System;
using System.IO;

namespace SharpProxy
{
    public class HttpsResponsePrologue : HttpResponsePrologue
    {
        protected override void ReadFirstLine(Stream stream)
        {
            throw new NotSupportedException();
        }
    }
}