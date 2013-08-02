using System;
using System.Net;

namespace IPHelper
{
    public class UdpRow
    {
        #region Private Fields

        private readonly IPEndPoint _localEndPoint;
        private readonly int _processId;

        #endregion

        public UdpRow(Win32Funcs.UdpRow udpRow)
        {
            try
            {
                _processId = udpRow.owningPid;

                int localPort = (udpRow.localPort1 << 8) + (udpRow.localPort2) + (udpRow.localPort3 << 24) +
                                (udpRow.localPort4 << 16);
                long localAddress = udpRow.localAddr;
                _localEndPoint = new IPEndPoint(localAddress, localPort);
            }
            catch
            {
            }
        }

        public IPEndPoint LocalEndPoint
        {
            get { return _localEndPoint; }
        }

        public int ProcessId
        {
            get { return _processId; }
        }


        public override string ToString()
        {
            string returnData = "ProcessId : " + ProcessId +
                                " " + "Local Address : " + _localEndPoint;
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