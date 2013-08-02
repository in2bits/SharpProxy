using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;

namespace IPHelper
{
    public static class Functions
    {
        #region Public Methods

        public static UInt32 AddIPAddress(IPAddress ipAddress, IPAddress ipAddressMask, int adaptorIndex)
        {
            // Note :: This Function has not been tested yet !
            IntPtr ptrNTEContext;
            IntPtr ptrNTEInstance;

#pragma warning disable 612,618
            uint result = Win32Funcs.AddIPAddress((uint)ipAddress.Address, (uint)ipAddressMask.Address,
#pragma warning restore 612,618
 adaptorIndex, out ptrNTEContext, out ptrNTEInstance);

            return result;
        }

        public static TcpTable GetExtendedTcpTable(bool sorted, Win32Funcs.TcpTableType tabletype)
        {
            var tcpRows = new List<TcpRow>();

            IntPtr tcpTable = IntPtr.Zero;
            int tcpTableLength = 0;

            if (Win32Funcs.GetExtendedTcpTable(tcpTable, ref tcpTableLength, sorted, Win32Funcs.AfInet,
                                               tabletype, 0) != 0)
            {
                try
                {
                    tcpTable = Marshal.AllocHGlobal(tcpTableLength);
                    if (
                        Win32Funcs.GetExtendedTcpTable(tcpTable, ref tcpTableLength, true, Win32Funcs.AfInet,
                                                       tabletype, 0) == 0)
                    {
                        var table = (Win32Funcs.TcpTable)Marshal.PtrToStructure(tcpTable, typeof(Win32Funcs.TcpTable));

                        var rowPtr = (IntPtr)((long)tcpTable + Marshal.SizeOf(table.Length));
                        for (int i = 0; i < table.Length; ++i)
                        {
                            tcpRows.Add(
                                new TcpRow(
                                    (Win32Funcs.TcpRow)Marshal.PtrToStructure(rowPtr, typeof(Win32Funcs.TcpRow))));
                            rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(typeof(Win32Funcs.TcpRow)));
                        }
                    }
                }
                finally
                {
                    if (tcpTable != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(tcpTable);
                    }
                }
            }

            return new TcpTable(tcpRows);
        }


        public static UdpTable GetExtendedUdpTable(bool sorted, Win32Funcs.UdpTableType tabletype)
        {
            var udpRows = new List<UdpRow>();

            IntPtr udpTable = IntPtr.Zero;
            int udpTableLength = 0;

            if (
                Win32Funcs.GetExtendedUdpTable(udpTable, ref udpTableLength, sorted, Win32Funcs.AfInet,
                                               tabletype, 0) != 0)
            {
                try
                {
                    udpTable = Marshal.AllocHGlobal(udpTableLength);
                    if (
                        Win32Funcs.GetExtendedUdpTable(udpTable, ref udpTableLength, true, Win32Funcs.AfInet,
                                                       tabletype, 0) == 0)
                    {
                        var table = (Win32Funcs.UdpTable)Marshal.PtrToStructure(udpTable, typeof(Win32Funcs.UdpTable));

                        var rowPtr = (IntPtr)((long)udpTable + Marshal.SizeOf(table.Length));
                        for (int i = 0; i < table.Length; ++i)
                        {
                            udpRows.Add(
                                new UdpRow(
                                    (Win32Funcs.UdpRow)Marshal.PtrToStructure(rowPtr, typeof(Win32Funcs.UdpRow))));
                            rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(typeof(Win32Funcs.TcpRow)));
                        }
                    }
                }
                finally
                {
                    if (udpTable != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(udpTable);
                    }
                }
            }

            return new UdpTable(udpRows);
        }


        public static IPNetTable GetIpNetTable(bool sorted)
        {
            var ipNetRows = new List<IPNetRow>();

            IntPtr ipNetTable = IntPtr.Zero;
            int ipNetTableLength = 0;

            if (Win32Funcs.GetIpNetTable(ipNetTable, ref ipNetTableLength, sorted) != 0)
            {
                try
                {
                    ipNetTable = Marshal.AllocCoTaskMem(ipNetTableLength);

                    if (Win32Funcs.GetIpNetTable(ipNetTable, ref ipNetTableLength, sorted) == 0)
                    {
                        var table =
                            (Win32Funcs.IpNetTable)Marshal.PtrToStructure(ipNetTable, typeof(Win32Funcs.IpNetTable));

                        var rowPtr = (IntPtr)((long)ipNetTable + Marshal.SizeOf(table.Length));
                        for (int i = 0; i < table.Length; i++)
                        {
                            ipNetRows.Add(
                                new IPNetRow(
                                    (Win32Funcs.IpNetRow)Marshal.PtrToStructure(rowPtr, typeof(Win32Funcs.IpNetRow))));

                            rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(typeof(Win32Funcs.IpNetRow)));
                        }
                    }
                }
                finally
                {
                    if (ipNetTable != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(ipNetTable);
                    }
                }
            }
            return new IPNetTable(ipNetRows);
        }

        public static UInt32 CreateIpNetEntry(IPNetRow netRow)
        {
            throw new NotImplementedException("Not yet implemented!");
            //var data = netRow.GetIpNetRowStructure();
            //var pointer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Win32Funcs.IpNetRow)));
            //Console.WriteLine(Marshal.SizeOf(typeof (Win32Funcs.IpNetRow)).ToString());
            //try
            //{
            //    Marshal.StructureToPtr(data, pointer, false);

            //}
            //catch (Exception)
            //{
            //    //throw new Exception("Operation Failed, Error Code : " + returnValue);
            //}
            //Console.WriteLine(pointer.ToString());
            //var returnValue =  Win32Funcs.CreateIpNetEntry(pointer);
            //return returnValue;
        }

        public static uint GetAdapterIndex(string adaptorName)
        {
            throw new NotImplementedException("There are some Difficulties");
        }

        #endregion
    }
}