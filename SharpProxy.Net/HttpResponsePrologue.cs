using System;
using System.IO;
using System.Net;

namespace SharpProxy
{
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
            //Debug.WriteLine(line);
            stream.WriteLine(line);

            foreach (var pair in Headers)
            {
                if (pair.Key.ToLowerInvariant() == "proxy-connection")
                    continue;
                line = string.Format("{0}: {1}", pair.Key, pair.Value);
                stream.WriteLine(line);
            }
            //Debug.WriteLine(null);
            stream.WriteLine();
            //stream.Flush();
        }
    }
}