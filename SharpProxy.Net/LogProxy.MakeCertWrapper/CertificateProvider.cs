using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
//using LogProxy.Lib;

namespace LogProxy.MakeCertWrapper
{
    public class CertificateProvider// : ICertificateProvider
    {
        private const StoreName DefaultIssuerCertificateStoreName = StoreName.Root;
        private const StoreLocation DefaultIssuerCertificateStoreLocation = StoreLocation.CurrentUser;
        private const string DefaultIssuerCertificateCommonName = "LogProxy-Root";

        private const StoreName DefaultOutputStoreName = StoreName.My;
        private const StoreLocation DefaultOutputStoreLocation = StoreLocation.CurrentUser;

        private static readonly object syncLock = new object();

        private string makeCertPath;

        public CertificateProvider(string makeCertPath)
        {
            this.makeCertPath = makeCertPath;
        }

        public void EnsureRootCertificate()
        {
            //this.Log(MessageLevel.Info, "Checking root certificate");

            const StoreName storeName = DefaultIssuerCertificateStoreName;
            const StoreLocation storeLocation = DefaultIssuerCertificateStoreLocation;
            CertificateName rootCertName = GetRootCertificateName();

            if (FindCertificateByName(storeName, storeLocation, rootCertName) == null) 
            {
                lock (syncLock)
                {
                    if (FindCertificateByName(storeName, storeLocation, rootCertName) == null)
                    {
                        //this.Log(MessageLevel.Warning, "Creating root certificate");
                        CreateRootCertificate();
                    }
                }
            }
        }

        public X509Certificate2 GetCertificateForHost(string host)
        {
            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentNullException("host");
            }

            host = host.ToLowerInvariant();

            const StoreName storeName = DefaultOutputStoreName;
            const StoreLocation storeLocation = DefaultOutputStoreLocation;
            CertificateName hostCertName = GetCertificateNameByHost(host);

            X509Certificate2 certificate;

            if ((certificate = FindCertificateByName(storeName, storeLocation, hostCertName)) == null)
            {
                lock (syncLock)
                {
                    if ((certificate = FindCertificateByName(storeName, storeLocation, hostCertName)) == null)
                    {
                        //this.Log(MessageLevel.Info, "Creating host certificate for " + host);

                        CreateCertificateForHost(hostCertName);

                        if ((certificate = FindCertificateByName(storeName, storeLocation, hostCertName)) == null)
                        {
                            Thread.Sleep(1000);
                            if ((certificate = FindCertificateByName(storeName, storeLocation, hostCertName)) == null)
                            {
                                throw new InvalidOperationException("Could not find certificate after it was created");
                            }
                        }
                    }
                }
            }

            return certificate;
        }

        private void CreateRootCertificate()
        {
            var parameters = new MakeCertParameters
            {
                Name = GetRootCertificateName(),
                IsPrivateKeyExportable = true,
                IsSelfSigned = true,
                OutputStoreName = DefaultIssuerCertificateStoreName,
                OutputStoreLocation = DefaultIssuerCertificateStoreLocation,
                Usage = KeyUsage.ServerAuthentication,
                CertificateType = CertificateType.CertificationAuthority,
                HashAlgorithm = CertificateHashAlgorithm.SHA1,
                KeyType = KeyType.Signature
            };

            int output = CommandLine.Run(this.makeCertPath, parameters.ToString());
        }

        private void CreateCertificateForHost(CertificateName hostCertName)
        {
            var parameters = new MakeCertParameters
            {
                Name = hostCertName,
                IsPrivateKeyExportable = true,
                IssuerCertificateCommonName = DefaultIssuerCertificateCommonName,
                IssuerCertificateStoreName = DefaultIssuerCertificateStoreName,
                IssuerCertificateStoreLocation = DefaultIssuerCertificateStoreLocation,
                OutputStoreName = DefaultOutputStoreName,
                OutputStoreLocation = DefaultOutputStoreLocation,
                Usage = KeyUsage.ServerAuthentication,
                CertificateType = CertificateType.EndCertificate,
                HashAlgorithm = CertificateHashAlgorithm.SHA1,
                KeyType = KeyType.Exchange
            };

            int output = CommandLine.Run(this.makeCertPath, parameters.ToString());
        }

        private CertificateName GetCertificateNameByHost(string host)
        { 
            return new CertificateName { CommonName = host.ToLowerInvariant() };
        }

        private CertificateName GetRootCertificateName()
        {
            return new CertificateName { CommonName = DefaultIssuerCertificateCommonName };
        }

        private static X509Certificate2 FindCertificateByName(StoreName storeName, StoreLocation storeLocation, CertificateName name)
        {
            X509Store x509Store = new X509Store(storeName, storeLocation);
            x509Store.Open(OpenFlags.OpenExistingOnly);
            X509Certificate2Collection certificate2Collection = x509Store.Certificates.Find(X509FindType.FindBySubjectDistinguishedName, name.ToString(), validOnly: false);
            x509Store.Close();
            return certificate2Collection.OfType<X509Certificate2>().FirstOrDefault();
        }
    }
}
