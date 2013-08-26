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
        private IResponseInspector _responseInspector;
        public HttpResponsePrologue Prologue { get; set; }

        public Socket RemoteSocket { get; set; }
        public Stream RemoteStream { get; set; }

        public Stream Content { get; set; }

        async public static Task<ProxyResponse> From(Socket socket, Stream stream, IRequestInspector requestInspector)
        {
            var response = new ProxyResponse();

            response.RemoteSocket = socket;
            response.RemoteStream = stream;

            requestInspector.OnResponseBegun(response);

            await response.ReadPrologue();

            await response.ReadContent();

            return response;
        }

        async private Task ReadPrologue()
        {
            //Debug.WriteLine("Reader Server Response Prologue");
            Prologue = HttpResponsePrologue.From(RemoteStream);
            //Debug.WriteLine("Reader Server Response Prologue - DONE");

            if (_responseInspector != null)
                _responseInspector.OnPrologueReceived();
        }

        async private Task ReadContent()
        {
            if (_responseInspector != null)
                _responseInspector.OnResponseBodyProgress();

            long contentLength;
            if (!long.TryParse(Prologue.Headers.FirstOrDefault(x => x.Key == "Content-Length").Value, out contentLength))
                contentLength = -1;

            //Debug.WriteLine("Reader Server Response Content");
            var content = new MemoryStream();
            await RemoteStream.CopyHttpMessageToAsync(RemoteSocket, content, contentLength);
            if (content.Length != 0)
            {
                content.Position = 0;
                Content = content;
            }
            //Debug.WriteLine("Reader Server Response Content - DONE");

            if (_responseInspector != null)
                _responseInspector.OnResponseBodyReceived();
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

        public void RegisterInspector(IResponseInspector responseInspector)
        {
            _responseInspector = responseInspector;
        }
    }
}