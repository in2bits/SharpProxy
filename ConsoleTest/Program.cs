using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using SharpProxy;

namespace ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback = TrustCertificate;
            _proxy = ProxyEngine.New();
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

        private static IWebProxy _proxy;

        private static Uri _uriA;
        private static Uri _uriB;

        private static HttpWebRequest _a;
        private static HttpWebRequest _b;

        private static bool _aDone;
        private static bool _bDone;

        private static bool _done;

        private static void MakeRequest()
        {
            _uriA = new Uri("http://www.yahoo.com", UriKind.Absolute);
            //_uriA = new Uri("http://hsrd.yahoo.com/favicon.ico", UriKind.Absolute);
            //_uriA = new Uri("https://www.google.com", UriKind.Absolute);
            //_uriA = new Uri("https://l.yimg.com/zz/combo?&nn/lib/metro/g/uicontrib/dali/dali_transport_1.1.34.js&nn/lib/metro/g/uicontrib/dali/metro_dali_1.0.27.js&nn/lib/metro/g/uicontrib/dali/module_api_1.1.16.js&nn/lib/metro/g/uicontrib/dali/yui_service_0.1.17.js", UriKind.Absolute);

            WebRequest.DefaultWebProxy = _proxy;

            _a = WebRequest.CreateHttp(_uriA);
            //_a.UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:22.0) Gecko/20100101 Firefox/22.0";
            //_a.KeepAlive = true;
            _a.BeginGetResponse(OnGotResponse, _a);

            //_b = WebRequest.CreateHttp(_uriB);
            //_b.BeginGetResponse(OnGotResponse, _b);

        }

        private static void OnGotResponse(IAsyncResult ar)
        {
            var request = (HttpWebRequest) ar.AsyncState;
            Exception ex = null;
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse) request.EndGetResponse(ar);
            }
            catch (Exception e)
            {
                ex = e;
            }

            if (ex == null)
            {
                Debug.WriteLine("COMPLETING WebClient");
                string text;
                var responseStream = response.GetResponseStream();
                var reader = new StreamReader(responseStream);
                text = reader.ReadToEnd();
                Console.WriteLine((request == _a ? _uriA : _uriB) + " : " + text.Length);
            //    using (var reader = new StreamReader(openReadCompletedEventArgs.Result))
            //        Console.WriteLine(reader.ReadToEnd());
            }
            else
            {
                Console.WriteLine("ERROR: " + ex.Message);
            }
            if (request == _a)
                _aDone = true;
            if (request == _b)
                _bDone = true;
            _done = (_aDone && _bDone);
        }
    }
}
