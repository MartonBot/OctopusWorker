﻿// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using JetBrains.Annotations;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Docker;

partial class Build
{

    readonly List<TestConfigurationOnLinuxDistribution> TestOnLinuxDistributions = new()
    {
        new TestConfigurationOnLinuxDistribution(NetCore, "linux-x64", "debian:buster", "deb"),
        new TestConfigurationOnLinuxDistribution(NetCore, "linux-x64", "debian:oldoldstable-slim", "deb"),
        new TestConfigurationOnLinuxDistribution(NetCore, "linux-x64", "debian:oldstable-slim", "deb"),
        new TestConfigurationOnLinuxDistribution(NetCore, "linux-x64", "debian:stable-slim", "deb"),
        new TestConfigurationOnLinuxDistribution(NetCore, "linux-x64", "linuxmintd/mint19.3-amd64", "deb"),
        new TestConfigurationOnLinuxDistribution(NetCore, "linux-x64", "ubuntu:latest", "deb"),
        new TestConfigurationOnLinuxDistribution(NetCore, "linux-x64", "ubuntu:rolling", "deb"),
        new TestConfigurationOnLinuxDistribution(NetCore, "linux-x64", "ubuntu:trusty", "deb"),
        new TestConfigurationOnLinuxDistribution(NetCore, "linux-x64", "ubuntu:xenial", "deb"),
        new TestConfigurationOnLinuxDistribution(NetCore, "linux-x64", "centos:latest", "rpm"),
        new TestConfigurationOnLinuxDistribution(NetCore, "linux-x64", "centos:7", "rpm"),
        new TestConfigurationOnLinuxDistribution(NetCore, "linux-x64", "fedora:latest", "rpm"),
        new TestConfigurationOnLinuxDistribution(NetCore, "linux-x64", "roboxes/rhel7", "rpm"),
        new TestConfigurationOnLinuxDistribution(NetCore, "linux-x64", "roboxes/rhel8", "rpm"),
    };

    [PublicAPI]
    Target TestWindows => _ => _
        .DependsOn(BuildWindows)
        .Executes(RunTests);

    [PublicAPI]
    Target TestLinux => _ => _
        .DependsOn(BuildLinux)
        .Executes(RunTests);

    [PublicAPI]
    Target TestOsx => _ => _
        .DependsOn(BuildOsx)
        .Executes(RunTests);

    [PublicAPI]
    Target TestLinuxPackages => _ => _
        .Description("Tests installing the .deb and .rpm packages onto all of the Linux target distributions.")
        .Executes(() =>
        {
            void RunLinuxPackageTestsFor(TestConfigurationOnLinuxDistribution testConfiguration)
            {
                Logging.InTest($"{testConfiguration.Framework}/{testConfiguration.RuntimeId}/{testConfiguration.DockerImage}/{testConfiguration.PackageType}", () =>
                {
                    string? archSuffix = null;
                    if (testConfiguration.PackageType == "deb" && testConfiguration.RuntimeId == "linux-x64")
                    {
                        archSuffix = "_amd64";
                    }
                    else if (testConfiguration.PackageType == "rpm" && testConfiguration.RuntimeId == "linux-x64")
                    {
                        archSuffix = ".x86_64";
                    }
                    if (string.IsNullOrEmpty(archSuffix)) throw new NotSupportedException();

                    var searchForTestFileDirectory = ArtifactsDirectory / testConfiguration.PackageType;
                    Logger.Info($"Searching for files in {searchForTestFileDirectory}");
                    var packageTypeFilePath = searchForTestFileDirectory.GlobFiles($"*{archSuffix}.{testConfiguration.PackageType}")
                        .Single();
                    var packageFile = Path.GetFileName(packageTypeFilePath);
                    Logger.Info($"Testing Linux package file {packageFile}");

                    var testScriptsBindMountPoint = RootDirectory / "linux-packages" / "test-scripts";

                    DockerTasks.DockerPull(settings => settings.SetName(testConfiguration.DockerImage));
                    DockerTasks.DockerRun(settings => settings
                        .EnableRm()
                        .EnableTty()
                        .SetImage(testConfiguration.DockerImage)
                        .SetEnv(
                            $"VERSION={OctoVersionInfo.FullSemVer}",
                            "INPUT_PATH=/input",
                            "OUTPUT_PATH=/output",
                            "SIGN_PRIVATE_KEY",
                            "SIGN_PASSPHRASE",
                            "REDHAT_SUBSCRIPTION_USERNAME",
                            "REDHAT_SUBSCRIPTION_PASSWORD",
                            $"BUILD_NUMBER={OctoVersionInfo.FullSemVer}")
                        .SetVolume(
                            $"{testScriptsBindMountPoint}:/test-scripts:ro",
                            $"{ArtifactsDirectory}:/artifacts:ro")
                        .SetArgs("bash", "/test-scripts/test-linux-package.sh", $"/artifacts/{testConfiguration.PackageType}/{packageFile}"));
                });
            }
            
            foreach (var testConfiguration in TestOnLinuxDistributions)
            {
                RunLinuxPackageTestsFor(testConfiguration);
            }
        });

    [PublicAPI]
    Target TestWindowsInstallerPermissions => _ => _
        .DependsOn(PackWindowsInstallers)
        .Executes(() =>
        {
            string GetTestName(AbsolutePath installerPath) => Path.GetFileName(installerPath).Replace(".msi", "");
            
            void TestInstallerPermissions(AbsolutePath installerPath)
            {
                var destination = TestDirectory / "install" / GetTestName(installerPath);
                FileSystemTasks.EnsureExistingDirectory(destination);

                InstallMsi(installerPath, destination);

                try
                {
                    var builtInUsersHaveWriteAccess = DoesSidHaveRightsToDirectory(destination, WellKnownSidType.BuiltinUsersSid, FileSystemRights.AppendData, FileSystemRights.CreateFiles);
                    if (builtInUsersHaveWriteAccess)
                    {
                        throw new Exception($"The installation destination {destination} has write permissions for the user BUILTIN\\Users. Expected write permissions to be removed by the installer.");
                    }
                }
                finally
                {
                    UninstallMsi(installerPath);
                }
                
                Logger.Info($"BUILTIN\\Users do not have write access to {destination}. Hooray!");
            }

            void InstallMsi(AbsolutePath installerPath, AbsolutePath destination)
            {
                var installLogName = Path.Combine(TestDirectory, $"{GetTestName(installerPath)}.install.log");

                Logger.Info($"Installing {installerPath} to {destination}");

                var arguments = $"/i {installerPath} /QN INSTALLLOCATION={destination} /L*V {installLogName}";
                Logger.Info($"Running msiexec {arguments}");
                var installationProcess = ProcessTasks.StartProcess("msiexec", arguments);
                installationProcess.WaitForExit();
                FileSystemTasks.CopyFileToDirectory(installLogName, ArtifactsDirectory);
                if (installationProcess.ExitCode != 0) {
                    throw new Exception($"The installation process exited with a non-zero exit code ({installationProcess.ExitCode}). Check the log {installLogName} for details.");
                }
            }
            
            void UninstallMsi(AbsolutePath installerPath)
            {
                Logger.Info($"Uninstalling {installerPath}");
                var uninstallLogName = Path.Combine(TestDirectory, $"{GetTestName(installerPath)}.uninstall.log");

                var arguments = $"/x {installerPath} /QN /L*V {uninstallLogName}";
                Logger.Info($"Running msiexec {arguments}");
                var uninstallProcess = ProcessTasks.StartProcess("msiexec", arguments);
                uninstallProcess.WaitForExit();
                FileSystemTasks.CopyFileToDirectory(uninstallLogName, ArtifactsDirectory);
            }
            
            bool DoesSidHaveRightsToDirectory(string directory, WellKnownSidType sid, params FileSystemRights[] rights)
            {
                var destinationInfo = new DirectoryInfo(directory);
                var acl = destinationInfo.GetAccessControl();
                var identifier = new SecurityIdentifier(sid, null);
                return acl
                    .GetAccessRules(true, true, typeof(SecurityIdentifier))
                    .Cast<FileSystemAccessRule>()
                    .Where(r => r.IdentityReference.Value == identifier.Value)
                    .Where(r => r.AccessControlType == AccessControlType.Allow)
                    .Any(r => rights.Any(right => r.FileSystemRights.HasFlag(right)));
            }

            FileSystemTasks.EnsureExistingDirectory(TestDirectory);
            FileSystemTasks.EnsureCleanDirectory(TestDirectory);

            var installers = (ArtifactsDirectory / "msi").GlobFiles("*x64.msi");

            if (!installers.Any())
            {
                throw new Exception($"Expected to find at least one installer in the directory {ArtifactsDirectory}");
            }
            
            foreach (var installer in installers)
            {
                TestInstallerPermissions(installer);
            }
        });
}