using System;
using System.Runtime.InteropServices;
using System.Threading;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using SolutionAlias.Logging;
using SolutionAlias.Options;
using Task = System.Threading.Tasks.Task;

namespace SolutionAlias
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // Load in the background as soon as a solution exists, so a saved alias appears without a click.
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(GeneralOptionsPage), Vsix.Name, "General", 0, 0, true, SupportsProfiles = true)]
    [Guid(PackageGuids.PackageString)]
    public sealed class SolutionAliasPackage : ToolkitPackage
    {
        /// <summary>Set at the end of <see cref="InitializeAsync"/>; null before then and once disposed.</summary>
        internal AliasController? Controller { get; private set; }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.RegisterCommandsAsync();

            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            GeneralOptions options = await GeneralOptions.GetLiveInstanceAsync();
            var log = new OutputLog(Vsix.Name, PackageGuids.OutputWindowSection) { IsEnabled = options.LogToOutputWindow };
            Controller = await AliasController.CreateAndStartAsync(this, options, log);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && Controller != null)
            {
#pragma warning disable VSTHRD010 // Visual Studio closes packages on the UI thread; DisposeController asserts it.
                DisposeController();
#pragma warning restore VSTHRD010
            }

            base.Dispose(disposing);
        }

        private void DisposeController()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Controller?.Dispose();
            Controller = null;
        }
    }
}
