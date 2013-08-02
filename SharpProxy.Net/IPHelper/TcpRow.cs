using System;
using System.Net;
using System.Net.NetworkInformation;

namespace IPHelper
{
    public class TcpRow
    {
        #region Private Fields

        private readonly IPEndPoint _localEndPoint;
        private readonly int _processId;
        private readonly IPEndPoint _remoteEndPoint;
        private readonly TcpState _state;

        #endregion

        #region Constructors

        public TcpRow(Win32Funcs.TcpRow tcpRow)
        {
            _state = tcpRow.state;
            _processId = tcpRow.owningPid;

            int localPort = (tcpRow.localPort1 << 8) + (tcpRow.localPort2) + (tcpRow.localPort3 << 24) +
                            (tcpRow.localPort4 << 16);
            long localAddress = tcpRow.localAddr;
            _localEndPoint = new IPEndPoint(localAddress, localPort);

            int remotePort = (tcpRow.remotePort1 << 8) + (tcpRow.remotePort2) + (tcpRow.remotePort3 << 24) +
                             (tcpRow.remotePort4 << 16);
            long remoteAddress = tcpRow.remoteAddr;
            _remoteEndPoint = new IPEndPoint(remoteAddress, remotePort);
        }

        #endregion

        #region Public Properties

        public IPEndPoint LocalEndPoint
        {
            get { return _localEndPoint; }
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return _remoteEndPoint; }
        }

        public TcpState State
        {
            get { return _state; }
        }

        public int ProcessId
        {
            get { return _processId; }
        }

        #endregion

        public override string ToString()
        {
            string returnData = "ProcessId : " + ProcessId +
                                " " + "Local Address : " + _localEndPoint +
                                " " + "Remote Address : " + _remoteEndPoint +
                                " " + "State : " + State;
            return returnData;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            try
            {
                var newObject = obj as TcpRow;
                if (newObject == null)
                    return false;
                if (newObject.GetHashCode() == GetHashCode())
                    return true;
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }

        public override int GetHashCode()
        {
            string dummy = ToString();
            return dummy.GetHashCode();
        }
    }
}