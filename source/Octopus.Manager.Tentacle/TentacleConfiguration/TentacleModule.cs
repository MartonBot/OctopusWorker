﻿using System;
using Autofac;
using Octopus.Manager.Core.Shared.DeleteWizard;
using Octopus.Manager.Core.Shared.ProxyWizard;
using Octopus.Manager.Core.Shared.Shell;
using Octopus.Manager.Tentacle.TentacleConfiguration.SetupWizard;
using Octopus.Manager.Tentacle.TentacleConfiguration.TentacleManager;
using Octopus.Shared.Configuration;

namespace Octopus.Manager.Tentacle.TentacleConfiguration
{
    public class TentacleModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.RegisterType<ShellViewModel>().OnActivating(e => e.Instance.ShowEAPVersion = false); 
            builder.Register(CreateShell).As<ShellView>();
            builder.RegisterType<TentacleSetupWizardLauncher>();
            builder.RegisterType<ProxyWizardLauncher>();
            builder.RegisterType<DeleteWizardLauncher>();
            builder.RegisterType<InstanceSelectionModel>().AsSelf().SingleInstance().WithParameter("applicationName", ApplicationName.Tentacle);
        }

        static ShellView CreateShell(IComponentContext container)
        {
            var newInstanceLauncher = container.Resolve<TentacleSetupWizardLauncher>();

            var shellModel = container.Resolve<ShellViewModel>();
            var shell = new ShellView("Tentacle Manager", shellModel);
            shell.EnableInstanceSelection();
            shell.Height = 520;
            shell.SetViewContent(new TentacleManagerView(new TentacleManagerModel(), container.Resolve<InstanceSelectionModel>(), container.Resolve<IApplicationInstanceStore>(), newInstanceLauncher, container.Resolve<ProxyWizardLauncher>(), container.Resolve<DeleteWizardLauncher>()));
            return shell;
        }
    }
}