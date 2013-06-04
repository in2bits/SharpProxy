using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace SharpProxy
{
    public class ProxyRequest
    {
        private Stream Stream { get; set; }

        public string Method { get; set; }
        public string Destination { get; set; }
        public string Version { get; set; }
        public NameValueCollection Headers { get; private set; }

        public ProxyRequest(Stream stream)
        {
            Stream = stream;

            ReadPrologue();
        }

        private void ReadPrologue()
        {
            var line = Stream.ReadLine();
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
                throw new Exception("Invalid HttpRequest");
            var method = parts[0];
            var destination = parts[1];
            var version = parts[2];
            var headers = new NameValueCollection();
            line = Stream.ReadLine();
            while (line.Trim() != "")
            {
                parts = line.Split(new[] { ':' }, 2);
                if (parts.Length != 2)
                    throw new Exception("Invalid HttpHeader");
                var name = parts[0].Trim();
                var value = parts[1].Trim();
                headers.Add(name, value);
                line = Stream.ReadLine();
            }
            Method = method;
            Destination = destination;
            Version = version;
            Headers = headers;
        }

        async public Task Process()
        {
            if (Method == "CONNECT")
            {
                //TODO FiddlerCore
                if (FiddlerCert == null)
                    throw new NotSupportedException("SSL Not supported - Fidder Cert not found");
                await ProcessSsl();
            }
            else
            {
                await ProcessHttp();
            }
        }

        async private Task ProcessHttp()
        {
            var upperMethod = Method.ToUpperInvariant();

            switch (upperMethod)
            {
                case "GET":
                    await Get();
                    break;
                default:
                    throw new NotSupportedException("Method " + Method);
            }
        }

        async private Task Get()
        {
            var uri = new Uri(Destination, UriKind.Absolute);

            var httpRequest = WebRequest.CreateHttp(uri);
            httpRequest.Proxy = null;
            CopyHeaders(Headers, httpRequest);

            var httpResponse = (HttpWebResponse)(await Task.Factory.FromAsync<WebResponse>(httpRequest.BeginGetResponse, httpRequest.EndGetResponse, null).ConfigureAwait(false));

            var proxyResponse = new ProxyResponse(Stream, httpResponse);

            var responseStream = httpResponse.GetResponseStream();
            proxyResponse.Send(responseStream);
        }

        private void CopyHeaders(NameValueCollection nvc, HttpWebRequest httpRequest)
        {
            foreach (var key in nvc.AllKeys)
            {
                switch (key)
                {
                    case "Host": 
                        httpRequest.Host = nvc[key];
                        break;
                    case "Proxy-Connection":
                        break;
                    default:
                        throw new Exception("Unmapped Header " + key);
                }
            }
        }

        async private Task ProcessSsl()
        {
            var headers = new NameValueCollection();
            var responsePrologue = new ProxyResponse(Stream, Version, "200", "Connection Established", headers);
            responsePrologue.Send();

            var sslStream = new SslStream(Stream, true)
                {
                    ReadTimeout = 5000, 
                    WriteTimeout = 5000
                };
            sslStream.AuthenticateAsServer(FiddlerCert, false, SslProtocols.Default, false);

            var proxyRequest = new ProxyRequest(sslStream);
            var host = Destination;
            var port = 443;
            var destinationParts = Destination.Split(new[] {':'}, StringSplitOptions.RemoveEmptyEntries);
            if (destinationParts.Length > 1)
            {
                host = destinationParts[0];
                port = Convert.ToInt16(destinationParts[1]);
            }
            var uriBuilder = new UriBuilder("https", host, port, proxyRequest.Destination);
            var httpRequest = WebRequest.CreateHttp(uriBuilder.Uri);
            httpRequest.KeepAlive = false;
            var httpResponse = (HttpWebResponse)(await Task.Factory.FromAsync<WebResponse>(httpRequest.BeginGetResponse, httpRequest.EndGetResponse, null).ConfigureAwait(false));
            var proxyResponse = new ProxyResponse(sslStream, httpResponse);

            var responseStream = httpResponse.GetResponseStream();
            proxyResponse.Send(responseStream);
        }

        private const string FiddlerCertIssuer = "CN=DO_NOT_TRUST_FiddlerRoot, O=DO_NOT_TRUST, OU=Created by http://www.fiddler2.com";
        private static X509Certificate FiddlerCert;

        public static void SetFiddlerCert()
        {
            var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);
            foreach (X509Certificate2 cert in store.Certificates)
            {
                if (cert.Issuer == FiddlerCertIssuer)
                {
                    FiddlerCert = cert;
                    break;
                }
            }
        }
    }
}