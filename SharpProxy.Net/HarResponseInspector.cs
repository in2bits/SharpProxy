using System;
using HarHar;

namespace SharpProxy
{
    public class HarResponseInspector : IResponseInspector
    {
        private readonly ProxyResponse _response;
        private readonly Entry _entry;

        public HarResponseInspector(ProxyResponse response, Entry entry)
        {
            _response = response;
            _entry = entry;

            var responseInfo = new ResponseInfo();
            _entry.Response = responseInfo;
        }

        void IResponseInspector.OnPrologueReceived()
        {
            var prologue = _response.Prologue;

            _entry.Response.HttpVersion = prologue.Version;
            _entry.Response.Status = (int)prologue.StatusCode;
            _entry.Response.StatusText = prologue.StatusDescription;

            foreach (var pair in prologue.Headers)
            {
                if (pair.Key.ToLowerInvariant() == "cookie")
                {
                    //http://www.nczonline.net/blog/2009/05/05/http-cookies-explained/
                    var text = pair.Value;
                    var parts = text.Split(new char[] { ':' }, 2, StringSplitOptions.None);
                    if (parts.Length == 1)
                        continue;
                    text = parts[1];
                    var cookieStrings = text.Split(new string[] { "; " }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var cookieString in cookieStrings)
                    {
                        var cookieParts = cookieString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        parts = cookieParts[0].Split(new char[] { '=' }, 2, StringSplitOptions.None);
                        if (parts.Length < 2)
                            break;
                        var cookie = new CookieInfo()
                        {
                            Name = parts[0],
                            Value = parts[1]
                        };
                        _entry.Response.Cookies.Add(cookie);
                    }
                    //TODO: Parse Cookies
                }
                _entry.Response.Headers.Add(new NameValuePairInfo() { Name = pair.Key, Value = pair.Value });
            }
        }

        private DateTime _responseStarted = DateTime.MaxValue;
        void IResponseInspector.OnResponseBodyProgress()
        {
            if (_responseStarted == DateTime.MaxValue)
                _responseStarted = DateTime.Now;
        }

        void IResponseInspector.OnResponseBodyReceived()
        {
            _entry.Timings.Receive = (int) DateTime.Now.Subtract(_responseStarted).TotalMilliseconds;
            _entry.Time = _entry.Timings.GetTotal();
        }
    }
}