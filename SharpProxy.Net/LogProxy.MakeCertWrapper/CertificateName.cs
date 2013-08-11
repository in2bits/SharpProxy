using System;
using System.Collections.Generic;

namespace LogProxy.MakeCertWrapper
{
    public class CertificateName
    {
        public string CommonName { get; set; }

        public string Organization { get; set; }

        public string OrganizationUnit { get; set; }

        public override string ToString()
        {
            var nameParts = new List<string>();

            if (string.IsNullOrEmpty(this.CommonName))
            {
                throw new InvalidOperationException("Certificate common name should not be empty");
            }

            AddNamePart(nameParts, "CN", this.CommonName);
            AddNamePart(nameParts, "O", this.Organization);
            AddNamePart(nameParts, "OU", this.OrganizationUnit);

            return string.Join(", ", nameParts);
        }

        private static void AddNamePart(IList<string> nameParts, string namePartKey, string namePartValue)
        {
            if (!string.IsNullOrEmpty(namePartValue))
            {
                nameParts.Add(namePartKey + "=" + namePartValue);
            }
        }
    }
}
