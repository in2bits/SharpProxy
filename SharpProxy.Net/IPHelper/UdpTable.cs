using System.Collections;
using System.Collections.Generic;

namespace IPHelper
{
    public class UdpTable : IEnumerable<UdpRow>
    {
        #region Private Fields

        private readonly IEnumerable<UdpRow> _udpRows;

        #endregion

        #region Constructors

        public UdpTable(IEnumerable<UdpRow> udpRows)
        {
            _udpRows = udpRows;
        }

        #endregion

        #region Public Properties

        public IEnumerable<UdpRow> Rows
        {
            get { return _udpRows; }
        }

        #endregion

        #region IEnumerable<UdpRow> Members

        public IEnumerator<UdpRow> GetEnumerator()
        {
            return _udpRows.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _udpRows.GetEnumerator();
        }

        #endregion
    }
}