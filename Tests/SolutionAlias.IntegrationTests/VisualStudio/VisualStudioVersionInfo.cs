using System;
using System.IO;

namespace SolutionAlias.IntegrationTests.VisualStudio
{
    /// <summary>
    /// What vswhere.exe reports about one installed Visual Studio version (e.g. version 17 = Visual Studio 2022)
    /// </summary>
    internal sealed class VisualStudioVersionInfo
    {
        internal VisualStudioVersionInfo(string installationId, string installationPath, Version version)
        {
            InstallationId = installationId;
            InstallationPath = installationPath;
            Version = version;
        }

        /// <summary>
        /// The Visual Studio Installer's ID for this installation, which vswhere calls instanceId. It is also the part
        /// after the underscore in the installation's settings folder names (17.0_804f5cb5).
        /// </summary>
        public string InstallationId { get; }

        public string InstallationPath { get; }
        public Version Version { get; }
        public string DevenvPath => Path.Combine(InstallationPath, @"Common7\IDE\devenv.exe");
        public string MSBuildPath => Path.Combine(InstallationPath, @"MSBuild\Current\Bin\MSBuild.exe");
        public string VisualStudioRegistryEditPath => Path.Combine(InstallationPath, @"Common7\IDE\VsRegEdit.exe");
        public override string ToString() => $"Visual Studio {Version} ({InstallationId})";
    }
}
