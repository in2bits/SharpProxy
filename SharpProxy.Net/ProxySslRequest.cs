using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using LogProxy.MakeCertWrapper;

namespace SharpProxy
{
    public class ProxySslRequest : ProxyRequest
    {
        private ProxyRequest WrapperRequest { get; set; }

        public SslStream SecureClientStream { get; set; }
        public SslStream SecureRemoteStream { get; set; }

        private const string MakeCertPath = @"C:\Program Files (x86)\Fiddler2\makecert.exe";
        private static CertificateProvider _certProvider;
        async public static Task<ProxySslRequest> For(ProxyRequest wrapperRequest)
        {
            if (_certProvider == null)
            {
                _certProvider = new CertificateProvider(MakeCertPath);
                _certProvider.EnsureRootCertificate();
            }

            var sslRequest = new ProxySslRequest
                {
                    WrapperRequest = wrapperRequest,
                    ClientPid = wrapperRequest.ClientPid,
                    ClientSocket = wrapperRequest.ClientSocket,
                    ClientStream = wrapperRequest.ClientStream
                };

            var hostName = sslRequest.GetHostName();
            sslRequest._hostCert = _certProvider.GetCertificateForHost(hostName);

            var clientSsslStream = new SslStream(wrapperRequest.ClientStream, true, RemoteCertificateValidator, sslRequest.LocalCertificateSelector);
            await clientSsslStream.AuthenticateAsServerAsync(sslRequest._hostCert);
            sslRequest.SecureClientStream = clientSsslStream;

            var prologue = HttpRequestPrologue.From(clientSsslStream);
            sslRequest.Prologue = prologue;

            return sslRequest;
        }

        private X509Certificate _hostCert;

        private X509Certificate LocalCertificateSelector(object sender, string targethost, X509CertificateCollection localcertificates, X509Certificate remotecertificate, string[] acceptableissuers)
        {
            return _hostCert;
        }

        private static bool RemoteCertificateValidator(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
        {
            return true;
        }

        async protected override Task InitRemoteStream()
        {
            await base.InitRemoteStream();
            var secureRemoteStream = new SslStream(RemoteStream, true, RemoteCertificateValidator);
            var targetHost = WrapperRequest.Prologue.Headers.First(x => x.Key == "Host").Value;
            await secureRemoteStream.AuthenticateAsClientAsync(targetHost);
            SecureRemoteStream = secureRemoteStream;
        }

        protected async override Task WritePrologueToRemote()
        {
            //Debug.WriteLine("WritePrologueToRemote");
            await Prologue.WriteTo(SecureRemoteStream);
            //Debug.WriteLine("WritePrologueToRemote - DONE");
        }

        async protected override Task CopyContentFromClientToServer()
        {
            //Debug.WriteLine("CopyContentFromClientToServer - A");
            long contentLength;
            if (!long.TryParse(Prologue.Headers.FirstOrDefault(x => x.Key == "Content-Length").Value, out contentLength))
                contentLength = -1;
            //await SecureClientStream.CopyHttpMessageToAsync(ClientSocket, SecureRemoteStream, contentLength);
            using (var ms = new MemoryStream())
            {
                await SecureClientStream.CopyHttpMessageToAsync(ClientSocket, ms, contentLength);
                //Debug.WriteLine("CopyContentFromClientToServer - A - DONE");
                ms.Position = 0;
                //Debug.WriteLine("CopyContentFromClientToServer - B");
                await ms.CopyToAsync(SecureRemoteStream);
                //Debug.WriteLine("CopyContentFromClientToServer - B - DONE");
            }
        }

        protected override async Task GetResponse()
        {
            //Debug.WriteLine("GetResponse");
            Response = await ProxyResponse.From(RemoteSocket, SecureRemoteStream);
            //Debug.WriteLine("GetResponse - DONE");
        }

        protected async override Task WriteResponseToClient()
        {
            //Debug.WriteLine("Relaying Response");
            await Response.WriteTo(SecureClientStream);
            //Debug.WriteLine("Relayed Response");
        }

        protected override void End()
        {
            SecureRemoteStream.Close();
            SecureClientStream.Close();
            base.End();
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

            //Debug.WriteLine("Resolve DNS");
            var ipEndpoint = await IPEndPointProvider.Get(host, port);
            return ipEndpoint;
        }

        //private const string FiddlerCertIssuer = "CN=DO_NOT_TRUST_FiddlerRoot, O=DO_NOT_TRUST, OU=Created by http://www.fiddler2.com";
        //private static X509Certificate FiddlerCert;

        //public static void SetFiddlerCert()
        //{
        //    var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
        //    store.Open(OpenFlags.ReadOnly);
        //    foreach (X509Certificate2 cert in store.Certificates)
        //    {
        //        if (cert.Issuer == FiddlerCertIssuer)
        //        {
        //            FiddlerCert = cert;
        //            break;
        //        }
        //    }
        //}
    }
}