using System;
using System.Collections.Generic;
using HarHar;

namespace SharpProxy
{
    public class HarRequestInspector : IRequestInspector
    {
        private readonly ProxyRequest _request;
        private readonly Log _log;
        private readonly Entry _entry;

        private DateTime _start;
        public HarRequestInspector(ProxyRequest request, Log log)
        {
            _request = request;
            _log = log;
            _entry = new Entry();
            _log.Entries.Add(_entry);
            _entry.StartedDateTime = DateTime.Now;
            _entry.Request = new RequestInfo();
            _start = DateTime.Now;
        }

        void IRequestInspector.OnClientPidResolved()
        {
            _entry.Connection = _request.ClientPid.ToString();
        }

        void IRequestInspector.OnPrologueReceived()
        {
            var prologue = _request.Prologue;

            _entry.Request.Method = prologue.Method;
            _entry.Request.Url = prologue.Destination;
            _entry.Request.HttpVersion = prologue.Version;
            var uri = new Uri(prologue.Destination, UriKind.RelativeOrAbsolute);
            _entry.Request.QueryString = new List<NameValuePairInfo>()
                {
                    new NameValuePairInfo(){Name="NotImplemented", Value="Request.QueryString"}
                };

            foreach (var pair in prologue.Headers)
            {
                if (pair.Key.ToLowerInvariant() == "cookie")
                {
                    //http://www.nczonline.net/blog/2009/05/05/http-cookies-explained/
                    var text = pair.Value;
                    var parts = text.Split(new char[] {':'}, 2, StringSplitOptions.None);
                    if (parts.Length == 1)
                        continue;
                    text = parts[1];
                    var cookieStrings = text.Split(new string[] {"; "}, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var cookieString in cookieStrings)
                    {
                        var cookieParts = cookieString.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                        parts = cookieParts[0].Split(new char[] {'='}, 2, StringSplitOptions.None);
                        if (parts.Length < 2)
                            break;
                        var cookie = new CookieInfo()
                            {
                                Name = parts[0],
                                Value = parts[1]
                            };
                        _entry.Request.Cookies.Add(cookie);
                    }
                    //TODO: Parse Cookies
                }
                _entry.Request.Headers.Add(new NameValuePairInfo(){Name=pair.Key,Value=pair.Value});
            }
        }

        void IRequestInspector.OnTransferredToSecureRequest(ProxySslRequest secureRequest)
        {
            //var secureEntry = new Entry();
            //var secureRequestInspector = new HarRequestInspector(secureRequest, new Entry())
            var secureRequestInspector = new HarRequestInspector(secureRequest, _log);
            secureRequest.RegisterInspector(secureRequestInspector);
        }

        void IRequestInspector.OnRemoteIpResolved()
        {
            _entry.ServerIpAddress = _request.RemoteSocket.RemoteEndPoint.ToString();
            _entry.Timings.Dns = (int)DateTime.Now.Subtract(_start).TotalMilliseconds;
        }

        void IRequestInspector.OnRemoteSocketConnected()
        {
            _entry.Timings.Connect = (int) DateTime.Now.Subtract(_start).TotalMilliseconds;
        }

        void IRequestInspector.OnRequestBodyProgress()
        {
            //throw new NotImplementedException();
        }

        private DateTime _requestBodySent;
        void IRequestInspector.OnRequestBodySent()
        {
            _requestBodySent = DateTime.Now;
            _entry.Timings.Send = (int) DateTime.Now.Subtract(_start).TotalMilliseconds;
            _entry.Request.PostData = new PostData()
                {
                    Text = "NotImplemented: Request.PostData"
                };
            //_entry.Request.BodySize = //NotImplemented: Request.BodySize
        }

        void IRequestInspector.OnResponseBegun(ProxyResponse response)
        {
            var responseInspector = new HarResponseInspector(response, _entry);
            response.RegisterInspector(responseInspector);
        }
    }
}