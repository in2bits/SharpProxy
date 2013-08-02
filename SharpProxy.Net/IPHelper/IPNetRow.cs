using System;
using System.Net;
using System.Net.NetworkInformation;

namespace IPHelper
{
    public class IPNetRow
    {
        #region Private Fields

        private readonly int _adaptorIndex;
        private readonly IPAddress _entryIPAddress;
        private readonly PhysicalAddress _entryMacAddress;
        private readonly Win32Funcs.IpNetRow _orginalData;
        private readonly Win32Funcs.ArpEntryType _typeOfArp;

        #endregion

        #region Constructors

        public IPNetRow(Win32Funcs.IpNetRow ipNetRow)
        {
            // Todo : I Know what i have to do here! :D ;)
            _orginalData = ipNetRow;
            _adaptorIndex = ipNetRow.adaptorIndex;
            var macaddress = new byte[8]
                                 {
                                     ipNetRow.adaptorPhysicalMacAddress0, ipNetRow.adaptorPhysicalMacAddress1,
                                     ipNetRow.adaptorPhysicalMacAddress2, ipNetRow.adaptorPhysicalMacAddress3,
                                     ipNetRow.adaptorPhysicalMacAddress4, ipNetRow.adaptorPhysicalMacAddress5,
                                     ipNetRow.adaptorPhysicalMacAddress6, ipNetRow.adaptorPhysicalMacAddress7
                                 };
            _entryMacAddress = new PhysicalAddress(macaddress);
            _entryIPAddress = new IPAddress(BitConverter.GetBytes(ipNetRow.adaptorAddr));
            _typeOfArp = (Win32Funcs.ArpEntryType)ipNetRow.typeOfARPEntry;
        }

        #endregion

        #region Public Properties

        public int AdaptorIndex
        {
            get { return _adaptorIndex; }
        }

        public PhysicalAddress EntryMacAddress
        {
            get { return _entryMacAddress; }
        }

        public IPAddress EntryIPAddress
        {
            get { return _entryIPAddress; }
        }

        public Win32Funcs.ArpEntryType TypeOfArpEntry
        {
            get { return _typeOfArp; }
        }

        public Win32Funcs.IpNetRow GetIpNetRowStructure()
        {
            return _orginalData;
        }

        #endregion
    }
}