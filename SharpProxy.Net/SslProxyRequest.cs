using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using LogProxy.MakeCertWrapper;

namespace SharpProxy
{
    public class SslProxyRequest : ProxyRequest
    {
        private ProxyRequest WrapperRequest { get; set; }

        public SslStream SecureClientStream { get; set; }
        public SslStream SecureRemoteStream { get; set; }

        private const string MakeCertPath = @"C:\Program Files (x86)\Fiddler2\makecert.exe";
        private static CertificateProvider _certProvider;
        async public static Task<SslProxyRequest> For(ProxyRequest wrapperRequest)
        {
            if (FiddlerCert == null)
                throw new NotSupportedException("SSL Not supported - Fidder Cert not found");

            var sslRequest = new SslProxyRequest();
            sslRequest.WrapperRequest = wrapperRequest;
            sslRequest.ClientPid = wrapperRequest.ClientPid;
            sslRequest.ClientSocket = wrapperRequest.ClientSocket;
            sslRequest.ClientStream = wrapperRequest.ClientStream;

            var clientSsslStream = new SslStream(wrapperRequest.ClientStream, true, RemoteCertificateValidator, LocalCertificateSelector)
                //{
                //    ReadTimeout = 30000,
                //    WriteTimeout = 30000
                //}
                ;
            if (_certProvider == null)
            {
                _certProvider = new CertificateProvider(MakeCertPath);
                _certProvider.EnsureRootCertificate();
            }
            var hostName = sslRequest.GetHostName();
            var hostCert = _certProvider.GetCertificateForHost(hostName);
            await clientSsslStream.AuthenticateAsServerAsync(hostCert);
            sslRequest.SecureClientStream = clientSsslStream;

            var prologue = HttpRequestPrologue.From(clientSsslStream);
            sslRequest.Prologue = prologue;

            return sslRequest;
        }

        private static X509Certificate LocalCertificateSelector(object sender, string targethost, X509CertificateCollection localcertificates, X509Certificate remotecertificate, string[] acceptableissuers)
        {
            return FiddlerCert;
        }

        private static bool RemoteCertificateValidator(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
        {
            return true;
        }

        protected async override Task WritePrologueToRemote()
        {
            await Prologue.WriteTo(SecureRemoteStream);
            await SecureRemoteStream.FlushAsync();
        }

        async protected override Task CopyContentFromClientToServer()
        {
            Debug.WriteLine("Reading Request Content");
            long contentLength;
            if (!long.TryParse(Prologue.Headers.FirstOrDefault(x => x.Key == "Content-Length").Value, out contentLength))
                contentLength = -1;
            await SecureClientStream.CopyHttpMessageToAsync(ClientSocket, SecureRemoteStream, contentLength);
            await SecureRemoteStream.FlushAsync();
        }

        protected async override Task WriteResponseToClient()
        {
            Debug.WriteLine("Relaying Response");
            await Response.WriteTo(SecureClientStream);
            //await SecureClientStream.FlushAsync();
        }

        protected override void End()
        {
            SecureRemoteStream.Close();
            SecureClientStream.Close();
            base.End();
        }

        protected override async Task GetResponse()
        {
            Response = await ProxyResponse.From(RemoteSocket, SecureRemoteStream);
        }

        async protected override Task InitRemoteStream()
        {
            await base.InitRemoteStream();
            var secureRemoteStream = new SslStream(RemoteStream, true, RemoteCertificateValidator);
            var targetHost = WrapperRequest.Prologue.Headers.First(x => x.Key == "Host").Value;
            await secureRemoteStream.AuthenticateAsClientAsync(targetHost);
            SecureRemoteStream = secureRemoteStream;
        }

        private string GetHostName()
        {
            return WrapperRequest.Prologue.Headers.First(x => x.Key == "Host").Value;
        }

        protected override async Task<IPEndPoint> GetIPEndpoint()
        {
            var host = GetHostName(); //Prologue.Destination;
            var port = 443;
            var destinationParts = Prologue.Destination.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (destinationParts.Length > 1)
            {
                host = destinationParts[0];
                port = Convert.ToInt16(destinationParts[1]);
            }

            Debug.WriteLine("Resolve DNS");
            var ipAddresses = await Dns.GetHostAddressesAsync(host);

            if (!ipAddresses.Any())
            {
                DnsResolutionError();
                return null;
            }

            var ipAddress = ipAddresses.First();

            var ipEndpoint = new IPEndPoint(ipAddress, port);

            return ipEndpoint;
        }

        //async override public Task Process()
        //{
        //    var httpRequest = WebRequest.CreateHttp(uriBuilder.Uri);
        //    httpRequest.KeepAlive = false;
        //    var httpResponse = (HttpWebResponse)(await Task.Factory.FromAsync<WebResponse>(httpRequest.BeginGetResponse, httpRequest.EndGetResponse, null).ConfigureAwait(false));
        //    var proxyResponse = await ProxyResponse.From(httpResponse.GetResponseStream());

        //    var responseStream = httpResponse.GetResponseStream();
        //    await proxyResponse.WriteTo(responseStream);
        //}

        private const string FiddlerCertIssuer = "CN=DO_NOT_TRUST_FiddlerRoot, O=DO_NOT_TRUST, OU=Created by http://www.fiddler2.com";
        private static X509Certificate FiddlerCert;

        public static void SetFiddlerCert()
        {
            var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
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