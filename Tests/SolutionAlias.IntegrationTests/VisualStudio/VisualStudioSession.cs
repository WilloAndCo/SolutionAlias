using System;
using System.Runtime.InteropServices;
using SolutionAlias.IntegrationTests.Com;
using SolutionAlias.IntegrationTests.Run;
using SolutionAlias.IntegrationTests.Screen;
using SolutionAlias.Options;

namespace SolutionAlias.IntegrationTests.VisualStudio
{
    /// <summary>Properties for one Visual Studio launch.</summary>
    internal sealed class VisualStudioSession
    {
        internal VisualStudioSession(VisualStudioVersionInfo versionInfo, EnvDTE80.DTE2 dte, int processId, string solutionDirectory, string solutionFile)
        {
            VersionInfo = versionInfo;
            Dte = dte;
            ProcessId = processId;
            SolutionDirectory = solutionDirectory;
            SolutionFile = solutionFile;
            SolutionExplorerRows = new SolutionExplorerRows(processId);
            ScreenElementsController = new ScreenElementsController(processId);
            VsInspector = new VsInspectorClient(dte);
        }

        public VisualStudioVersionInfo VersionInfo { get; }

        public EnvDTE80.DTE2 Dte { get; }

        /// <summary>devenv.exe's process ID.</summary>
        public int ProcessId { get; }

        /// <summary>The copy of the sample solution's folder that this session opened.</summary>
        public string SolutionDirectory { get; }

        /// <summary>The copied .sln file.</summary>
        public string SolutionFile { get; }

        public SolutionExplorerRows SolutionExplorerRows { get; }

        /// <summary>Visual Studio's windows, buttons and menus, through Windows UI Automation.</summary>
        public ScreenElementsController ScreenElementsController { get; }

        public VsInspectorClient VsInspector { get; }

        public IntPtr MainWindowHandle => Dte.MainWindow.HWnd;

        /// <summary>Waits until the solution is open, then loads the extension if Visual Studio hasn't already.</summary>
        internal void WaitForSolutionAndLoadExtension()
        {
            // wait until the solution is loaded
            Wait.PollUntilTrueOrThrow(
                () => Dte.Solution.IsOpen && Dte.Solution.Projects.Count > 0,
                TimeSpan.FromMinutes(3),
                () => $"the solution to open (IsOpen = {Dte.Solution.IsOpen})");

            // loads both extensions (if not loaded already)
            // VsInspector get loaded if LoadPackageIfNotLoaded is the first dte call to it due to the [ProvideAutomationObject] attribute
            // Solution Alias gets loaded because of the LoadPackageIfNotLoaded call itself, loading the package guid if it doesn't exist yet
            Wait.PollUntilTrueOrThrow(() => VsInspector.LoadPackageIfNotLoaded(VsInspectorClient.SolutionAliasPackageGuid), TimeSpan.FromSeconds(20), () => "Solution Alias to load");
        }

        /// <summary>The text of the extension's Output window pane</summary>
        internal string ReadExtensionOutputPaneForFailureMessage()
        {
            const string ExtensionOutputPaneName = "Solution Alias";

            try
            {
                EnvDTE.OutputWindowPane pane = Dte.ToolWindows.OutputWindow.OutputWindowPanes.Item(ExtensionOutputPaneName);
                EnvDTE.TextDocument document = pane.TextDocument;

                // CreateEditPoint returns a point at the start, and GetText return the text between that point and the end.
                string paneText = document.StartPoint.CreateEditPoint().GetText(document.EndPoint);
                return $"--- Output pane '{ExtensionOutputPaneName}':{Environment.NewLine}{paneText}";
            }
            catch (Exception ex) when (ex is COMException || ex is ArgumentException || ex is InvalidCastException)
            {
                return $"(Couldn't read the '{ExtensionOutputPaneName}' Output pane: {ex.Message})";
            }
        }

        /// <summary>Options are not reset automatically across sessions, set them to know defaults.</summary>
        internal void ResetExtensionOptions()
        {
            SetOptionIfDifferent("SolutionAlias", "ShowLabel", true);
            SetOptionIfDifferent("SolutionAlias", "NumFoldersToShow", GeneralOptions.DefaultNumFoldersToShow);
            SetOptionIfDifferent("SolutionAlias", "AlwaysUseThemeColour", GeneralOptions.DefaultAlwaysUseThemeColour);
            SetOptionIfDifferent("SolutionAlias", "OpacityPercent", GeneralOptions.DefaultOpacityPercent);
            SetOptionIfDifferent("SolutionAlias", "LogToOutputWindow", true);
        }
        
        private void SetOptionIfDifferent(string extension, string property, object optionValue)
        {
            if (string.Equals(VsInspector.ReadOption(extension, property), optionValue.ToString(), StringComparison.OrdinalIgnoreCase) == false)
            {
                VsInspector.SetAndSaveOption(extension, property, optionValue);
            }
        }
    }
}
