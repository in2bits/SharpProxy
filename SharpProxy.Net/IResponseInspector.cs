using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpProxy
{
    public interface IResponseInspector
    {
        void OnPrologueReceived();
        void OnResponseBodyProgress();
        void OnResponseBodyReceived();
    }
}
