using System.Net;

namespace SharpProxy
{
    public class ProxySslResponse : ProxyResponse
    {
        public ProxySslResponse(string version, HttpStatusCode statusCode, string statusDescription)
        {
            Prologue = new HttpResponsePrologue
                {
                    Version = version,
                    StatusCode = statusCode,
                    StatusDescription = statusDescription
                };
        }
    }
}