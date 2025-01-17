using System;
using System.Linq;
using Autofac;
using Octopus.Shared.Diagnostics;
using Octopus.Tentacle.Configuration.Proxy;

namespace Octopus.Tentacle.Configuration
{
    public class LogMaskingModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ProxyPasswordMaskValuesProvider>().As<IProxyPasswordMaskValuesProvider>();
            builder.Register(b =>
            {
                var proxyPassword = b.Resolve<ITentacleConfiguration>().ProxyConfiguration.CustomProxyPassword;
                var sensitiveValues = b.Resolve<IProxyPasswordMaskValuesProvider>().GetProxyPasswordMaskValues(proxyPassword).ToArray();

                return new SensitiveValueMasker(sensitiveValues);
            }).As<SensitiveValueMasker>();
        }
    }
}