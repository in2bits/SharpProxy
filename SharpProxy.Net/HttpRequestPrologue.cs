using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpProxy
{
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

            await stream.WriteAsync(prologueBytes, 0, prologueBytes.Length);
            //stream.Flush();
        }
    }
}
