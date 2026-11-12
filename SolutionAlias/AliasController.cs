using System;
using System.Windows.Media;
using Community.VisualStudio.Toolkit;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SolutionAlias.Dialog;
using SolutionAlias.Label;
using SolutionAlias.Logging;
using SolutionAlias.Options;
using SolutionAlias.Storage;
using SolutionAlias.TitleBar;

namespace SolutionAlias
{
    /// <summary> Controls the Alias functionality </summary>
    internal sealed class AliasController : IDisposable
    {
        private readonly AsyncPackage _package;
        private readonly IVsSolution _solution;
        private readonly TitleBarAliasPresenter _presenter;
        private readonly AliasFileStore _aliasFileStore;
        private readonly GeneralOptions _options;
        private readonly OutputLog _log;
        private bool _isAliasDialogOpen;

        private AliasController(AsyncPackage package, IVsSolution solution, GeneralOptions options, OutputLog log)
        {
            _package = package;
            _solution = solution;
            _options = options;
            _log = log;
            _presenter = new TitleBarAliasPresenter(log);
            _aliasFileStore = new AliasFileStore(log);
        }

        public static async System.Threading.Tasks.Task<AliasController> CreateAndStartAsync(AsyncPackage package, GeneralOptions options, OutputLog log)
        {
            await package.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            // service that manages the solution at startup, should never be null
            var solution = (IVsSolution)await package.GetServiceAsync(typeof(SVsSolution));
            Assumes.Present(solution);

            var controller = new AliasController(package, solution, options, log);
            controller.SubscribeToEvents();
            controller.UpdateTitleBarLabel();
            return controller;
        }

        public bool IsValidSolutionOpen()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // only saved solutions are valid to have an alias, because the alias is stored in the .vs folder next to the solution file
            return ReadSavedSolution() != null;
        }

        public void Dispose()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            UnsubscribeFromEvents();
            _presenter.Dispose();
        }

        public void ShowAliasDialogAndSaveResult()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_isAliasDialogOpen)
            {
                return;
            }

            SolutionAndAliasLocation? solution = ReadSavedSolution();
            if (solution == null)
            {
                VS.MessageBox.ShowWarning(Vsix.Name, "Save the solution first. The alias is stored in the .vs folder next to the solution file.");
                return;
            }

            // Start from what is on disk, in case the file was changed outside Visual Studio.
            AliasFileContents? aliasFileContents = _aliasFileStore.ReadAliasFile(solution.AliasFileLocation);

            AliasDialog dialog = CreateAliasDialog(solution, aliasFileContents);
            _isAliasDialogOpen = true;
            try
            {
                // Modal means the dialog takes focus and this waits until it closes
                dialog.ShowModal();
            }
            finally
            {
                _isAliasDialogOpen = false;
            }

            switch (dialog.ChosenAction)
            {
                case OpenOptionsAction _:
                    _package.ShowOptionPage(typeof(GeneralOptionsPage));
                    break;
                case ClearAliasAction _:
                    _aliasFileStore.DeleteAliasFile(solution.AliasFileLocation);
                    break;
                case SaveAliasAction save:
                    _aliasFileStore.SaveAliasFile(solution.AliasFileLocation, save.ContentsToSave);
                    break;
            }

            UpdateTitleBarLabel();
        }

        private void SubscribeToEvents()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            VS.Events.SolutionEvents.OnAfterOpenSolution += OnAfterOpenSolution;
            VS.Events.SolutionEvents.OnAfterCloseSolution += OnAfterCloseSolution;
            VS.Events.ShellEvents.ShutdownStarted += OnShutdownStarted;
            GeneralOptions.Saved += OnOptionsSaved;
            _presenter.LabelClicked += OnLabelClicked;
        }

        private void UnsubscribeFromEvents()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            VS.Events.SolutionEvents.OnAfterOpenSolution -= OnAfterOpenSolution;
            VS.Events.SolutionEvents.OnAfterCloseSolution -= OnAfterCloseSolution;
            VS.Events.ShellEvents.ShutdownStarted -= OnShutdownStarted;
            GeneralOptions.Saved -= OnOptionsSaved;
            _presenter.LabelClicked -= OnLabelClicked;
        }

        private void OnAfterOpenSolution(Solution? solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            UpdateTitleBarLabel();
        }

        private void OnAfterCloseSolution()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _presenter.HideLabelAndStopReattachTimer();
        }

        private void OnOptionsSaved(GeneralOptions options)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _log.IsEnabled = options.LogToOutputWindow;
            UpdateTitleBarLabel();
        }

        private void OnShutdownStarted()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            UnsubscribeFromEvents();
            _presenter.StopReattachTimer();
        }

        private void OnLabelClicked(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // StartOnIdle opens the dialog after the mouse event has finished.
            // Dialog uses ShowModal which would pause the handling of the mouse event until after it closed
            ThreadHelper.JoinableTaskFactory.StartOnIdle(ShowAliasDialogAndSaveResult, VsTaskRunContext.UIThreadNormalPriority).FireAndForget();
        }

        private AliasDialog CreateAliasDialog(SolutionAndAliasLocation solution, AliasFileContents? aliasFileContents)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            return new AliasDialog(
                aliasFileContents,
                solution.SolutionFilePath,
                numFoldersToShow: _options.NumFoldersToShow,
                SolutionLabelAppearance.CreateFromTitleBarOrTheme(),
                _options.AlwaysUseThemeColour,
                opacityPercent: _options.OpacityPercent);
        }

        private void UpdateTitleBarLabel()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_options.ShowLabel == false)
            {
                _presenter.HideLabelAndStopReattachTimer();
                return;
            }

            SolutionAndAliasLocation? solution = ReadSavedSolution();
            if (solution == null)
            {
                _presenter.HideLabelAndStopReattachTimer();
                return;
            }

            AliasFileContents? aliasFileContents = _aliasFileStore.ReadAliasFile(solution.AliasFileLocation);
            SolutionLabelAppearance visualStudioLabel = SolutionLabelAppearance.CreateFromTitleBarOrTheme();

            _presenter.ShowLabelAndStartReattachTimer(LabelAppearanceUtil.BuildAppearance(
                solution.SolutionFilePath,
                aliasFileContents,
                numFoldersToShow: _options.NumFoldersToShow,
                _options.AlwaysUseThemeColour,
                opacityPercent: _options.OpacityPercent,
                themeTextColour: visualStudioLabel.TextColour,
                themeBackgroundColour: visualStudioLabel.BackgroundColour));
        }

        private SolutionAndAliasLocation? ReadSavedSolution()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (ErrorHandler.Failed(_solution.GetSolutionInfo(out string directory, out string solutionFile, out _)))
            {
                return null;
            }

            return SolutionAndAliasLocation.CreateForSavedSolution(directory, solutionFile);
        }
    }
}
