using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;

namespace SharpProxy
{
    public class ProxyResponse
    {
        private Stream Stream { get; set; }

        public string Version;
        public string StatusCode;
        public string StatusDescription;
        public NameValueCollection Headers;

        public ProxyResponse(Stream stream, HttpWebResponse httpResponse) : this(
            stream,
            "HTTP/" + httpResponse.ProtocolVersion,
            ((int)httpResponse.StatusCode).ToString(CultureInfo.InvariantCulture),
            httpResponse.StatusDescription,
            httpResponse.Headers)
        {
        }

        public ProxyResponse(Stream stream, string version, string statusCode, string statusDescription, NameValueCollection headers)
        {
            Stream = stream;

            Version = version;
            StatusCode = statusCode;
            StatusDescription = statusDescription;
            Headers = headers;
        }

        public void Send(byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
                Send(ms);
        }

        public void Send(Stream data = null)
        {
            var line = string.Format("{0} {1} {2}", Version, StatusCode, StatusDescription);
            Stream.WriteLine(line);
            //Stream.Flush();

            foreach (var key in Headers.AllKeys)
                Stream.WriteLine(string.Format("{0}: {1}", key, Headers[key]));
            Stream.WriteLine();
            //Stream.Flush();

            if (data != null)
                data.CopyTo(Stream);
            //Stream.Flush();
        }
    }
}