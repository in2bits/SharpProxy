using System.Collections;
using System.Collections.Generic;

namespace IPHelper
{
    public class IPNetTable : IEnumerable<IPNetRow>
    {
        #region Private Fields

        private readonly IEnumerable<IPNetRow> _ipNetRows;

        #endregion

        #region Constructors

        public IPNetTable(IEnumerable<IPNetRow> tcpRows)
        {
            _ipNetRows = tcpRows;
        }

        #endregion

        #region Public Properties

        public IEnumerable<IPNetRow> Rows
        {
            get { return _ipNetRows; }
        }

        #endregion

        #region IEnumerable<IPNetRow> Members

        public IEnumerator<IPNetRow> GetEnumerator()
        {
            return _ipNetRows.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _ipNetRows.GetEnumerator();
        }

        #endregion
    }
}