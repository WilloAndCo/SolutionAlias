using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace SolutionAlias.IntegrationTests.VisualStudio
{
    /// <summary>
    /// Create a settings folder for the Integration Test version of Visual Studio to run with
    /// It ensures the extensions are installed and ready to use for the tests.
    /// </summary>
    /// <remarks>
    /// Visual Studio can have multiple settings folders to run against chosen by command line argument /rootsuffix
    /// </remarks>
    internal static class IntegrationTestsSettingsFolder
    {
        /// <summary>
        /// Add this to the settings folder name to identify it
        /// e.g. for Visual Studio 2022 17.0_804f5cb5, the IntegrationTests settings folder is 17.0_804f5cb5IntegrationTests in AppData                
        /// </summary>
        public const string IntegrationTestsRootSuffix = "IntegrationTests";

        private static readonly XNamespace VsixManifestNamespace = "http://schemas.microsoft.com/developer/vsx-schema/2011";
        private static readonly HashSet<string> PreparedInstallationIds = new HashSet<string>();

        /// <summary> Install the developed extension and the extension inspectors needed to gather test information </summary>
        private static readonly string[] ExtensionProjectsToInstall =
        {
            @"SolutionAlias\SolutionAlias.csproj",
            @"Tests\SolutionAlias.VisualStudioInspector\SolutionAlias.VisualStudioInspector.csproj",
        };

        /// <summary>Resets the settings in this version's settings folder and deploys the extensions into it.</summary>
        public static void PrepareSettingsForVersion(VisualStudioVersionInfo versionInfo)
        {
            if (PreparedInstallationIds.Contains(versionInfo.InstallationId))
            {
                return;
            }

            ResetSettings(versionInfo);
            TurnOffStartWindow(versionInfo);
            RemoveOldExtensions(versionInfo);
            BuildAndDeployExtensions(versionInfo);
            EnableDeployedExtensions(versionInfo);
            RebuildExtensionCaches(versionInfo);
            PreparedInstallationIds.Add(versionInfo.InstallationId);
        }

        private static void ResetSettings(VisualStudioVersionInfo versionInfo)
        {
            // General.vssettings is the default settings file to set
            // resetsettings opens the visual studio window so we also need File.Exit to close it
            RunDevenvAndWaitForExit(versionInfo, $"/rootsuffix {IntegrationTestsRootSuffix} /resetsettings General.vssettings /command File.Exit");
        }

        /// <summary>Turns off the start window, so devenv opens straight into the environment.</summary>
        private static void TurnOffStartWindow(VisualStudioVersionInfo versionInfo)
        {
            // Set the Setting's private registry value to 10 = "Empty environment (open with no start window)"
            ProcessResult startupSetting = ProcessRunner.RunProcessAndCaptureOutput(
                versionInfo.VisualStudioRegistryEditPath,
                arguments: $"set \"{versionInfo.InstallationPath}\" {IntegrationTestsRootSuffix} HKCU General OnEnvironmentStartup dword 10",
                TimeSpan.FromMinutes(1));

            if (startupSetting.ExitCode != 0)
            {
                throw new InvalidOperationException($"Turning off the start window for {versionInfo} failed:{Environment.NewLine}{startupSetting.Output}");
            }
        }

        private static void RemoveOldExtensions(VisualStudioVersionInfo versionInfo)
        {
            string deployed = GetSettingsExtensionsFolder(versionInfo);
            if (Directory.Exists(deployed))
            {
                Directory.Delete(deployed, recursive: true);
            }
        }

        /// <summary>
        /// Builds the extensions and deploys them into this settings folder (cannot deploy without a build step)
        /// </summary>
        private static void BuildAndDeployExtensions(VisualStudioVersionInfo versionInfo)
        {
            foreach (string project in ExtensionProjectsToInstall)
            {
                string projectPath = Path.Combine(SolutionPaths.Root, project);

                // -v:m is minimal output, to show just warnings and errors
                ProcessResult build = ProcessRunner.RunProcessAndCaptureOutput(
                    versionInfo.MSBuildPath,
                    arguments: $"\"{projectPath}\" -t:Build -p:Configuration=Debug -p:DeployExtension=true " +
                    $"-p:VSSDKTargetPlatformRegRootSuffix={IntegrationTestsRootSuffix} -p:DeployTargetInstanceId={versionInfo.InstallationId} -v:m",
                    TimeSpan.FromMinutes(10));

                if (build.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Building and deploying {project} for {versionInfo} failed:{Environment.NewLine}{build.Output}");
                }
            }
        }

        /// <summary>Adds each extension to ExtensionManager\EnabledExtensions</summary>
        private static void EnableDeployedExtensions(VisualStudioVersionInfo versionInfo)
        {
            // one manifest file per extension in ExtensionProjectsToInstall
            foreach (string manifestFile in FindExtensionManifestFiles(versionInfo))
            {
                string enabledExtId = GetEnabledExtensionIdentifier(manifestFile);
                string extensionFolder = Path.GetDirectoryName(manifestFile);
                AddToEnabledExtensions(versionInfo, enabledExtId, extensionFolder);
            }
        }

        /// <summary>One extension.vsixmanifest file per deployed extension.</summary>
        private static string[] FindExtensionManifestFiles(VisualStudioVersionInfo versionInfo)
        {
            return Directory.GetFiles(GetSettingsExtensionsFolder(versionInfo), "extension.vsixmanifest", SearchOption.AllDirectories);
        }

        /// <summary>The manifest's Identity as "Id,Version", the name of the extension's EnabledExtensions entry.</summary>
        private static string GetEnabledExtensionIdentifier(string manifestFile)
        {
            XElement identity = XDocument.Load(manifestFile).Descendants(VsixManifestNamespace + "Identity").Single();
            return $"{(string)identity.Attribute("Id")},{(string)identity.Attribute("Version")}";
        }

        private static void AddToEnabledExtensions(VisualStudioVersionInfo versionInfo, string enabledExtenstionId, string extensionFolder)
        {
            // Visual studio maps the id to the extension folder to enable it.
            ProcessResult result = ProcessRunner.RunProcessAndCaptureOutput(
                versionInfo.VisualStudioRegistryEditPath,
                arguments: $"set \"{versionInfo.InstallationPath}\" {IntegrationTestsRootSuffix} HKCU ExtensionManager\\EnabledExtensions " +
                $"\"{enabledExtenstionId}\" string \"{extensionFolder}\\\\\"",
                TimeSpan.FromMinutes(1));

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"Adding {extensionFolder} to EnabledExtensions for {versionInfo} failed:{Environment.NewLine}{result.Output}");
            }
        }

        /// <summary>Makes Visual Studio pick up the newly deployed extensions and their MEF parts.</summary>
        private static void RebuildExtensionCaches(VisualStudioVersionInfo versionInfo)
        {
            // Marks the extensions as changed, so the next rebuild rebuilds the MEF cache in full instead of reusing it
            // Note MEF is used by Visual Studio to find extension classes marked with [Export] attributes - not used currently but keep for compatability if changed
            RunDevenvAndWaitForExit(versionInfo, $"/rootsuffix {IntegrationTestsRootSuffix} /clearcache");

            // Rebuilds Visual Studio's cached copy of the extensions' registration (.pkgdef), assembly locations, menus and MEF parts
            RunDevenvAndWaitForExit(versionInfo, $"/rootsuffix {IntegrationTestsRootSuffix} /updateconfiguration");
        }

        /// <summary>Where our publisher's extensions are deployed in this settings folder.</summary>
        private static string GetSettingsExtensionsFolder(VisualStudioVersionInfo versionInfo)
        {
            // extensions always use the publisher name as a parent folder
            return Path.Combine(GetIntegrationTestsSettingsFolder(versionInfo), "Extensions", "WilloAndCo");
        }

        /// <summary>
        /// The IntegrationTests settings folder's path e.g. %LOCALAPPDATA%\Microsoft\VisualStudio\17.0_804f5cb5IntegrationTests
        /// </summary>
        private static string GetIntegrationTestsSettingsFolder(VisualStudioVersionInfo versionInfo)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Microsoft\VisualStudio",
                $"{versionInfo.Version.Major}.0_{versionInfo.InstallationId}{IntegrationTestsRootSuffix}");
        }

        private static void RunDevenvAndWaitForExit(VisualStudioVersionInfo versionInfo, string arguments)
        {
            ProcessResult result = ProcessRunner.RunProcessAndCaptureOutput(versionInfo.DevenvPath, arguments: arguments,TimeSpan.FromMinutes(5));
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"devenv {arguments} for {versionInfo} exited with {result.ExitCode}:{Environment.NewLine}{result.Output}");
            }
        }
    }
}
