using System;
using System.Collections.Generic;
using System.IO;

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
}