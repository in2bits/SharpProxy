using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarHar;
using ServiceStack.Text;

namespace SharpProxy
{
    public class HarProxyInspector : IProxyInspector
    {
        private Log _log;

        public HarProxyInspector()
        {
            _log = new Log();

            _log.Entries = new List<Entry>(); //TODO: Remove this by updating HarHar
            _log.Browser = new Browser() {Name = "SharpProxy", Version = "0.1", Comment = "Hello, World!"};
            _log.Comment = "SharpProxy Log";
        }

        void IProxyInspector.OnRequestReceived(ProxyRequest request)
        {
            var requestInspector = new HarRequestInspector(request, _log);
            request.RegisterInspector(requestInspector);
        }

        public void SaveTo(Stream har)
        {
            using (var writer = new StreamWriter(har))
                writer.Write(_log.ToJson());
        }
    }
}
