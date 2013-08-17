using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpProxy
{
    public static class HeadersExtensions
    {
        public static bool ContainsIgnoreCase(this IEnumerable<KeyValuePair<string, string>> headers, string searchKey,
                                              string searchValue)
        {
            return headers.Contains(new KeyValuePair<string, string>(searchKey, searchValue), IgnoreCaseKeyValuePairComparer.Instance);
        }

        public class IgnoreCaseKeyValuePairComparer : IEqualityComparer<KeyValuePair<string, string>>
        {
            public static readonly IgnoreCaseKeyValuePairComparer Instance = new IgnoreCaseKeyValuePairComparer();

            public bool Equals(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
            {
                return string.Equals(x.Key, y.Key, StringComparison.InvariantCultureIgnoreCase)
                       && string.Equals(x.Value, y.Value, StringComparison.InvariantCultureIgnoreCase);
            }

            public int GetHashCode(KeyValuePair<string, string> obj)
            {
                throw new NotImplementedException();
            }
        }
    }
}