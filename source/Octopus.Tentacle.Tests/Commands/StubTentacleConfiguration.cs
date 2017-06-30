﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Octopus.Shared.Configuration;
using Octopus.Shared.Security;
using Octopus.Tentacle.Configuration;

namespace Octopus.Tentacle.Tests.Commands
{
    class StubTentacleConfiguration : ITentacleConfiguration
    {
        IList<OctopusServerConfiguration> servers = new List<OctopusServerConfiguration>();

        public string TentacleSquid { get; private set; }

        public IEnumerable<OctopusServerConfiguration> TrustedOctopusServers
        {
            get { return servers; }
        }

        public IEnumerable<string> TrustedOctopusThumbprints
        {
            get { return servers.Select(s => s.Thumbprint); }
            set { servers = value.Select(v => new OctopusServerConfiguration(v)).ToList(); }
        }

        public int ServicesPortNumber { get; set; }
        public string ApplicationDirectory { get; set; }
        public string PackagesDirectory { get; private set; }
        public string LogsDirectory { get; private set; }
        public string JournalFilePath { get; private set; }
        public X509Certificate2 TentacleCertificate { get; set; }
        public string ListenIpAddress { get; set; }
        public bool NoListen { get; set; }
        public OctopusServerConfiguration LastReceivedHandshake { get; set; }
        public IProxyConfiguration ProxyConfiguration { get; set; }
        public IPollingProxyConfiguration PollingProxyConfiguration { get; set; }

        public bool AddOrUpdateTrustedOctopusServer(OctopusServerConfiguration machine)
        {
            if (machine == null) throw new ArgumentNullException("machine");
            var result = false;

            var all = TrustedOctopusServers.ToList();

            var existing = all.SingleOrDefault(m => AreEqual(m, machine));

            if (existing != null)
            {
                result = true;
                all.Remove(existing);
            }

            all.Add(machine);
            servers = all;

            return result;
        }

        public void ResetTrustedOctopusServers()
        {
            servers = new List<OctopusServerConfiguration>();
        }

        public void RemoveTrustedOctopusServersWithThumbprint(string toRemove)
        {
            servers = servers.Where(s => s.Thumbprint != toRemove).ToList();
        }

        public void UpdateTrustedServerThumbprint(string old, string @new)
        {
            foreach (var s in servers.Where(s => s.Thumbprint == old))
                s.Thumbprint = @new;
        }

        public X509Certificate2 GenerateNewCertificate()
        {
            var cert = new CertificateGenerator().GenerateNew("cn=foo");
            TentacleCertificate = cert;
            return cert;
        }

        public void ImportCertificate(X509Certificate2 certificate)
        {
            TentacleCertificate = certificate;
        }

        public void Save()
        {
        }

        static bool AreEqual(OctopusServerConfiguration left, OctopusServerConfiguration right)
        {
            var thumbprintsMatch = string.Compare(left.Thumbprint, right.Thumbprint, StringComparison.OrdinalIgnoreCase) == 0;
            var addressesMatch = Uri.Compare(left.Address, right.Address, UriComponents.AbsoluteUri, UriFormat.SafeUnescaped, StringComparison.OrdinalIgnoreCase) == 0;

            return thumbprintsMatch && addressesMatch;
        }
    }
}