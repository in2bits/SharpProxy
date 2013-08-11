using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;

namespace LogProxy.MakeCertWrapper
{
    public class MakeCertParameters
    {
        private static readonly Dictionary<KeyUsage, string> KeyUsages = new Dictionary<KeyUsage, string> 
        { 
            { KeyUsage.ServerAuthentication, "1.3.6.1.5.5.7.3.1" },
            { KeyUsage.ClientAuthentication, "1.3.6.1.5.5.7.3.2" }
        };

        private const string DateFormat = "mm/dd/yyyy";

        public bool IsPrivateKeyExportable { get; set; }

        public bool IsSelfSigned { get; set; }

        public CertificateName Name { get; set; }

        public CertificateHashAlgorithm? HashAlgorithm { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public CertificateType? CertificateType { get; set; }

        public KeyUsage? Usage { get; set; }

        public StoreName? OutputStoreName { get; set; }

        public StoreLocation? OutputStoreLocation { get; set; }

        public string IssuerCertificateCommonName { get; set; }

        public StoreName? IssuerCertificateStoreName { get; set; }

        public StoreLocation? IssuerCertificateStoreLocation { get; set; }

        public KeyType? KeyType { get; set; }

        public override string ToString()
        {
            if (this.Name == null)
            {
                throw new InvalidOperationException("Certificate name should not be empty");
            }

            var parameters = new List<string>();
            AddBoolParameter(parameters, "-pe", this.IsPrivateKeyExportable);
            AddBoolParameter(parameters, "-r", this.IsSelfSigned);
            AddStringParameter(parameters, "-n", this.Name.ToString());
            AddStringParameter(parameters, "-a", this.TranslateHashAlgorithm());

            AddDateParameter(parameters, "-b", this.StartDate);
            AddDateParameter(parameters, "-e", this.EndDate);

            AddStringParameter(parameters, "-cy", this.TranslateCertificateType());
            AddStringParameter(parameters, "-eku", this.TranslateKeyUsage());

            AddStringParameter(parameters, "-ir", TranslateStoreLocation(this.IssuerCertificateStoreLocation));
            AddStringParameter(parameters, "-is", TranslateStoreName(this.IssuerCertificateStoreName));
            AddStringParameter(parameters, "-in", this.IssuerCertificateCommonName);

            AddStringParameter(parameters, "-sr", TranslateStoreLocation(this.OutputStoreLocation));
            AddStringParameter(parameters, "-ss", TranslateStoreName(this.OutputStoreName));

            AddStringParameter(parameters, "-sky", this.TranslateKeyType());

            return string.Join(" ", parameters);
        }

        private string TranslateHashAlgorithm()
        {
            if (this.HashAlgorithm.HasValue)
            {
                return this.HashAlgorithm.Value == CertificateHashAlgorithm.MD5 ? "md5" : "sha1";
            }

            return null;
        }

        private string TranslateKeyType()
        {
            if (this.KeyType.HasValue)
            {
                return this.KeyType.Value.ToString().ToLowerInvariant();
            }

            return null;
        }

        public string TranslateStoreLocation(StoreLocation? location)
        {
            if (location.HasValue)
            {
                return location.Value.ToString().ToLowerInvariant();
            }

            return null;
        }

        public string TranslateStoreName(StoreName? name)
        {
            if (name.HasValue)
            {
                return name.Value.ToString().ToLowerInvariant();
            }

            return null;
        }

        private string TranslateCertificateType()
        {
            if (this.CertificateType.HasValue)
            {
                return this.CertificateType.Value == MakeCertWrapper.CertificateType.CertificationAuthority ? "authority" : "end";
            }

            return null;
        }

        private string TranslateKeyUsage()
        {
            if (this.HashAlgorithm.HasValue)
            {
                return KeyUsages[this.Usage.Value];
            }

            return null;
        }

        private static void AddStringParameter(IList<string> parameters, string parameterName, string parameterValue)
        {
            if (!string.IsNullOrEmpty(parameterValue))
            {
                parameters.Add(parameterName + " " + EscapeParameterValue(parameterValue));
            }
        }

        private static void AddBoolParameter(IList<string> parameters, string parameterName, bool flag)
        {
            if (flag)
            {
                parameters.Add(parameterName);
            }
        }

        private static void AddDateParameter(IList<string> parameters, string parameterName, DateTime? date)
        {
            if (date.HasValue)
            {
                parameters.Add(parameterName + " " + date.Value.ToString(DateFormat, CultureInfo.InvariantCulture));
            }
        }

        private static string EscapeParameterValue(string value)
        {
            if (value.Contains(" "))
            {
                return "\"" + value + "\"";
            }
            else
            {
                return value;
            }
        }
    }
}
