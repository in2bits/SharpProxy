using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using SharpProxy;
using SharpProxy.Adapter.Net;

namespace ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback = TrustCertificate;
            _engine = ProxyEngine.New();
            Task.Run((Action)MakeRequest);
            while (!_done)
            {
                //Console.Write(".");
                Thread.Sleep(100);
            }
            Console.WriteLine();
            Console.WriteLine("Done!");
            Console.ReadKey();
        }

        private static bool TrustCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
        {
            return true;
        }

        private static IProxyEngine _engine;

        private static WebClient _a;
        private static WebClient _b;

        private static bool _aDone;
        private static bool _bDone;

        private static bool _done;

        private static void MakeRequest()
        {
            var uriA = new Uri("http://www.yahoo.com", UriKind.Absolute);
            var uriB = new Uri("https://www.google.com", UriKind.Absolute);

            _a = new WebClient {Proxy = _engine.ToProxy()};
            _a.OpenReadCompleted += OnOpenReadCompleted;

            _b = new WebClient {Proxy = _engine.ToProxy()};
            _b.OpenReadCompleted += OnOpenReadCompleted;

            _a.OpenReadAsync(uriA);
            _b.OpenReadAsync(uriB);
        }

        private static void OnOpenReadCompleted(object sender, OpenReadCompletedEventArgs openReadCompletedEventArgs)
        {
            if (openReadCompletedEventArgs.Error == null)
            {
                using (var reader = new StreamReader(openReadCompletedEventArgs.Result))
                    Console.WriteLine(reader.ReadToEnd());
            }
            else
            {
                Console.WriteLine("ERROR");
            }
            if (sender == _a)
                _aDone = true;
            if (sender == _b)
                _bDone = true;
            _done = (_aDone && _bDone);
        }
    }
}
