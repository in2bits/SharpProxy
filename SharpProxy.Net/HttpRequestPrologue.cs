using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SharpProxy
{
    public abstract class HttpPrologue
    {
        public string Version { get; set; }

        public IList<KeyValuePair<string, string>> Headers { get; protected set; }

        protected HttpPrologue()
        {
            Headers = new List<KeyValuePair<string, string>>();
            Errors = new List<string>();
        }

        public IList<string> Errors { get; set; }

        protected abstract void ReadFirstLine(Stream stream);

        protected static IList<KeyValuePair<string, string>> ReadHeaders(Stream stream)
        {
            var headers = new List<KeyValuePair<string, string>>();
            var line = stream.ReadLine();
            while (line.Trim() != "")
            {
                var parts = line.Split(new[] { ':' }, 2);
                if (parts.Length != 2)
                    throw new Exception("Invalid HttpHeader");
                var name = parts[0].Trim();
                var value = parts[1].Trim();
                headers.Add(new KeyValuePair<string, string>(name, value));
                line = stream.ReadLine();
            }

            return headers;
        }
    }

    public class HttpRequestPrologue : HttpPrologue
    {
        public string Method { get; set; }
        public string Destination { get; set; }

        protected override void ReadFirstLine(Stream stream)
        {
            var line = stream.ReadLine();
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
                throw new Exception("Invalid HttpRequest");
            Method = parts[0];
            Destination = parts[1];
            Version = parts[2];
        }

        public static HttpRequestPrologue From(Stream stream)
        {
            var prologue = new HttpRequestPrologue();

            prologue.ReadFirstLine(stream);
            prologue.Headers = ReadHeaders(stream);

            return prologue;
        }

        async public Task WriteTo(Stream stream)
        {
            var prologue = Method + " " + Destination + " " + Version + "\r\n";
            foreach (var pair in Headers)
            {
                if (pair.Key.ToLowerInvariant() == "proxy-connection")
                    continue;
                prologue += pair.Key + ": " + pair.Value + "\r\n";
            }
            prologue += "\r\n";
            var prologueBytes = Encoding.UTF8.GetBytes(prologue);

            Debug.WriteLine("Sending prologue");
            await stream.WriteAsync(prologueBytes, 0, prologueBytes.Length);
            //stream.Flush();
            Debug.WriteLine("Sent prologue");
        }
    }

    public class HttpResponsePrologue : HttpPrologue
    {
        public HttpStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; }

        protected override void ReadFirstLine(Stream stream)
        {
            var line = stream.ReadLine();
            var parts = line.Split(new[] { ' ' }, 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                throw new Exception("Invalid HttpRequest");
            Version = parts[0];
            var statusCode = 0;
            if (!int.TryParse(parts[1], out statusCode))
                Errors.Add("Invalid StatusCode");
            StatusCode = (HttpStatusCode) statusCode;
            if (parts.Length == 3)
                StatusDescription = parts[2];
        }

        public static HttpResponsePrologue From(Stream stream)
        {
            var prologue = new HttpResponsePrologue();

            prologue.ReadFirstLine(stream);
            prologue.Headers = ReadHeaders(stream);

            return prologue;
        }

        public void WriteTo(Stream stream)
        {
            var line = string.Format("{0} {1} {2}", Version, (int)StatusCode, StatusDescription);
            stream.WriteLine(line);

            foreach (var pair in Headers)
            {
                if (pair.Key.ToLowerInvariant() == "proxy-connection")
                    continue;
                stream.WriteLine(string.Format("{0}: {1}", pair.Key, pair.Value));
            }
            stream.WriteLine();
            stream.Flush();
        }
    }

    public class HttpsResponsePrologue : HttpResponsePrologue
    {
        protected override void ReadFirstLine(Stream stream)
        {
            throw new NotSupportedException();
        }
    }
}
