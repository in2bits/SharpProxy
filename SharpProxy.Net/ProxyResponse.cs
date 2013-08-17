using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SharpProxy
{
    public class ProxyResponse
    {
        public HttpResponsePrologue Prologue { get; set; }
        
        public Stream Content { get; set; }

        async public static Task<ProxyResponse> From(Socket socket, Stream stream)
        {
            var response = new ProxyResponse();

            //Debug.WriteLine("Reader Server Response Prologue");
            response.Prologue = HttpResponsePrologue.From(stream);
            //Debug.WriteLine("Reader Server Response Prologue - DONE");

            long contentLength;
            if (!long.TryParse(response.Prologue.Headers.FirstOrDefault(x => x.Key == "Content-Length").Value, out contentLength))
                contentLength = -1;

            //Debug.WriteLine("Reader Server Response Content");
            var content = new MemoryStream();
            await stream.CopyHttpMessageToAsync(socket, content, contentLength);
            if (content.Length != 0)
            {
                content.Position = 0;
                response.Content = content;
            }
            //Debug.WriteLine("Reader Server Response Content - DONE");

            return response;
        }

        async public Task WriteTo(NetworkStream stream)
        {
            Prologue.WriteTo(stream);
            if (Content != null)
                await Content.CopyToAsync(stream, Content.Length);
        }

        async public Task WriteTo(SslStream stream)
        {
            Prologue.WriteTo(stream);
            await stream.FlushAsync();
            if (Content != null)
            {
                await Content.CopyToAsync(stream, Content.Length);
            }
        }
    }
}