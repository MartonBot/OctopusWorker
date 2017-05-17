﻿using System;
using System.Windows;
using Autofac;
using Octopus.Manager.Core.Shared.CommonTabs;
using Octopus.Manager.Core.Shared.ProxyWizard;
using Octopus.Manager.Core.Shared.ProxyWizard.Views;
using Octopus.Manager.Core.Shared.Shell;
using Octopus.Manager.Tentacle.TentacleConfiguration.SetupWizard.Views;
using Octopus.Shared.Util;

namespace Octopus.Manager.Tentacle.TentacleConfiguration.SetupWizard
{
    public class TentacleSetupWizardLauncher
    {
        readonly IComponentContext container;
        readonly InstanceSelectionModel instanceSelection;

        public TentacleSetupWizardLauncher(IComponentContext container, InstanceSelectionModel instanceSelection)
        {
            this.container = container;
            this.instanceSelection = instanceSelection;
        }

        public bool? ShowDialog(Window owner, string selectedInstance)
        {
            var wizardModel = new TentacleSetupWizardModel(selectedInstance, instanceSelection.ApplicationName, new PollingProxyWizardModel(selectedInstance, instanceSelection.ApplicationName));
            var wizard = new TabbedWizard();
            wizard.AddTab(new WelcomeTab(wizardModel));
            wizard.AddTab(new StorageTab(wizardModel));
            wizard.AddTab(new CommunicationStyleTab(wizardModel));
            wizard.AddTab(new ProxyConfigurationTab(wizardModel.ProxyWizardModel));
            wizard.AddTab(new TentacleActiveTab(wizardModel));
            wizard.AddTab(new TentacleActiveDetailsTab(wizardModel));
            wizard.AddTab(new TentaclePassiveTab(wizardModel));
            wizard.AddTab(new InstallTab(wizardModel, container.Resolve<ICommandLineRunner>()) {ReadyMessage = "That's all the information we need. When you click the button below, your new Tentacle service will be configured and started.", SuccessMessage = "Installation complete!"});

            var shellModel = container.Resolve<ShellViewModel>();
            var shell = new ShellView("Tentacle Setup Wizard", shellModel);
            shell.Height = 610;
            shell.Width = 890;
            shell.SetViewContent(wizard);
            shell.Owner = owner;
            return shell.ShowDialog();
        }
    }
}