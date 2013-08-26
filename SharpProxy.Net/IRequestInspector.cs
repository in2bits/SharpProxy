using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpProxy
{
    public interface IRequestInspector
    {
        void OnClientPidResolved();
        void OnPrologueReceived();
        void OnTransferredToSecureRequest(ProxySslRequest secureRequest);
        void OnRemoteIpResolved();
        void OnRemoteSocketConnected();
        void OnRequestBodyProgress();
        void OnRequestBodySent();
        void OnResponseBegun(ProxyResponse response);
    }
}
