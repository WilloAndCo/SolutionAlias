using SolutionAlias.Dialog;
using SolutionAlias.Label;
using SolutionAlias.Options;
using SolutionAlias.Storage;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Media;
using SolutionAlias.IntegrationTests.Run;
using SolutionAlias.IntegrationTests.Screen;
using SolutionAlias.IntegrationTests.VisualStudio;
using Xunit;
using Xunit.Abstractions;

namespace SolutionAlias.IntegrationTests.Tests
{
    /// <summary>Tests that interact with the extension using the Mouse</summary>
    [Trait("Category", "UI")]
    public sealed class SolutionAliasUITests
    {
        private readonly ITestOutputHelper _output;

        public SolutionAliasUITests(ITestOutputHelper output)
        {
            _output = output;
            VisualStudioTestRun.CurrentTestOutput = output;
        }

        [Theory]
        [VisualStudioVersions]
        public void LeftClickingTheLabel_OpensTheDialog_AndOkSavesTheAlias(int visualStudioVersion)
        {
            var test = (VisualStudioSession vs) => AliasDialogUtil.RunAndCloseLeftoverDialogs(vs, () =>
            {
                MouseController.LeftClick(AliasLabelUtil.BringToFrontAndWaitForLabelCentreInPixels(vs));

                AutomationElement dialog = AliasDialogUtil.WaitForDialogToOpen(vs);
                ScreenElementsController.ReplaceTextBoxText(ScreenElementsController.GetDescendantByAutomationId(dialog, "AliasTextBox", Wait.WaitToHappenDuration), "UITEST");
                ScreenElementsController.PressButtonOrMenuItem(ScreenElementsController.GetDescendantByAutomationId(dialog, "OkButton", Wait.WaitToHappenDuration));
                AliasDialogUtil.WaitForDialogToClose(vs);

                AliasLabelUtil.WaitAndAssertEqual(vs.VsInspector.ReadAliasLabelText, "UITEST");
                Assert.True(File.Exists(AliasFileUtil.GetAliasFilePath(vs)), "OK didn't write an alias file.");
                Assert.Contains("\"alias\": \"UITEST\"", File.ReadAllText(AliasFileUtil.GetAliasFilePath(vs)));
            });

            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), test);
        }

        [Theory]
        [VisualStudioVersions]
        public void RightClickingTheLabel_OpensTheDialog_AndCancelChangesNothing(int visualStudioVersion)
        {
            var test = (VisualStudioSession vs) => AliasDialogUtil.RunAndCloseLeftoverDialogs(vs, () =>
            {
                string textBefore = Wait.ReadUntilMatchOrTimeout(vs.VsInspector.ReadAliasLabelText, vs.SolutionFile, Wait.WaitToHappenDuration);
                MouseController.RightClick(AliasLabelUtil.BringToFrontAndWaitForLabelCentreInPixels(vs));

                AutomationElement dialog = AliasDialogUtil.WaitForDialogToOpen(vs);
                ScreenElementsController.ReplaceTextBoxText(ScreenElementsController.GetDescendantByAutomationId(dialog, "AliasTextBox", Wait.WaitToHappenDuration), "NOT SAVED");
                ScreenElementsController.PressButtonOrMenuItem(ScreenElementsController.GetDescendantByNameAndType(dialog, "Cancel", ControlType.Button, Wait.WaitToHappenDuration));
                AliasDialogUtil.WaitForDialogToClose(vs);

                Assert.Equal(textBefore, Wait.ReadUntilChangedOrTimeout(vs.VsInspector.ReadAliasLabelText, textBefore, Wait.CheckNothingChangesDuration));
                Assert.False(File.Exists(AliasFileUtil.GetAliasFilePath(vs)), "Cancel wrote an alias file.");
            });

            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), test);
        }

        [Theory]
        [VisualStudioVersions]
        public void Clear_RemovesTheAliasAndItsFile(int visualStudioVersion)
        {
            var prepareCopy = (string copyDirectory) => AliasFileUtil.WriteAliasFile(copyDirectory, SampleSolutionCopy.SmallSolutionName, new AliasFileContents { Alias = "MAIN", BackgroundColourRgb = "#C0392B" });

            var test = (VisualStudioSession vs) => AliasDialogUtil.RunAndCloseLeftoverDialogs(vs, () =>
            {
                AliasLabelUtil.WaitAndAssertEqual(vs.VsInspector.ReadAliasLabelText, "MAIN");
                MouseController.LeftClick(AliasLabelUtil.BringToFrontAndWaitForLabelCentreInPixels(vs));

                AutomationElement dialog = AliasDialogUtil.WaitForDialogToOpen(vs);
                ScreenElementsController.PressButtonOrMenuItem(ScreenElementsController.GetDescendantByAutomationId(dialog, "ClearButton", Wait.WaitToHappenDuration));
                AliasDialogUtil.WaitForDialogToClose(vs);

                string pathLabel = LabelAppearanceUtil.ShortenPathToLastFolders(vs.SolutionFile, GeneralOptions.DefaultNumFoldersToShow);
                AliasLabelUtil.WaitAndAssertEqual(vs.VsInspector.ReadAliasLabelText, pathLabel);
                Assert.False(File.Exists(AliasFileUtil.GetAliasFilePath(vs)), "Clear left the alias file.");
            });

            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName, prepareCopy), test);
        }

        [Theory]
        [VisualStudioVersions]
        public void ChoosingABackgroundSwatch_AndOk_SavesAndShowsTheColour(int visualStudioVersion)
        {
            Color mint = ColourPresets.Background.Single(preset => preset.Name == "Mint").Colour;

            var test = (VisualStudioSession vs) => AliasDialogUtil.RunAndCloseLeftoverDialogs(vs, () =>
            {
                MouseController.LeftClick(AliasLabelUtil.BringToFrontAndWaitForLabelCentreInPixels(vs));

                AutomationElement dialog = AliasDialogUtil.WaitForDialogToOpen(vs);
                ScreenElementsController.SelectRadioButton(ScreenElementsController.GetDescendantByNameAndType(dialog, "Background colour: Mint", ControlType.RadioButton, Wait.WaitToHappenDuration));
                ScreenElementsController.PressButtonOrMenuItem(ScreenElementsController.GetDescendantByAutomationId(dialog, "OkButton", Wait.WaitToHappenDuration));
                AliasDialogUtil.WaitForDialogToClose(vs);

                AliasLabelUtil.WaitAndAssertEqual(vs.VsInspector.ReadAliasLabelBackground, mint.ToString());

                Assert.Contains($"\"backgroundColourRgb\": \"{HexColourText.FormatAsHex(mint)}\"", File.ReadAllText(AliasFileUtil.GetAliasFilePath(vs)));
            });

            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), test);
        }

        [Theory]
        [VisualStudioVersions]
        public void OptionsButton_OpensSolutionAliasOptions(int visualStudioVersion)
        {
            var test = (VisualStudioSession vs) => AliasDialogUtil.RunAndCloseLeftoverDialogs(vs, () =>
            {
                MouseController.LeftClick(AliasLabelUtil.BringToFrontAndWaitForLabelCentreInPixels(vs));
                AutomationElement dialog = AliasDialogUtil.WaitForDialogToOpen(vs);
                ScreenElementsController.PressButtonOrMenuItem(ScreenElementsController.GetDescendantByNameAndType(dialog, "Options...", ControlType.Button, Wait.WaitToHappenDuration));

                AutomationElement options = vs.ScreenElementsController.GetWindow(AliasDialogUtil.OptionsWindowName, Wait.WaitToHappenDuration);
                Assert.True(vs.ScreenElementsController.CannotFindWindow(AliasDialogUtil.DialogName), "The alias dialog is still open on top of Tools > Options.");

                // The page shows the extension's options by their display names.
                Wait.PollUntilTrueOrThrow(
                    () => ScreenElementsController.HasDescendantNamed(options, "Show label"),
                    Wait.WaitToHappenDuration,
                    () => $"Tools > Options to show the Solution Alias page ('Show label'). It shows: {ScreenElementsController.DescendantsToString(options)}");
                ScreenElementsController.CloseOptionsWindow(options);
                Wait.PollUntilTrueOrThrow(() => vs.ScreenElementsController.CannotFindWindow(AliasDialogUtil.OptionsWindowName), Wait.WaitToHappenDuration, () => "Tools > Options to close");
            });

            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), test);
        }

        [Theory]
        [VisualStudioVersions]
        public void DraggingTheLabel_MovesTheWindow_WithoutOpeningTheDialog(int visualStudioVersion)
        {


            var test = (VisualStudioSession vs) => AliasDialogUtil.RunAndCloseLeftoverDialogs(vs, () =>
            {
                // set window to "restore down" so it is not maximised and can be dragged
                vs.Dte.MainWindow.WindowState = EnvDTE.vsWindowState.vsWindowStateNormal;

                Wait.PollUntilTrueOrThrow(() => vs.VsInspector.ReadAliasLabelScreenBounds() != null, Wait.WaitToHappenDuration, () => "the label to be drawn in the restored window");

                Rect windowBoundsBefore = ScreenElementsController.GetWindowScreenBoundsInPixels(vs.MainWindowHandle);
                Point start = AliasLabelUtil.BringToFrontAndWaitForLabelCentreInPixels(vs);

                const int numDragSteps = 15;
                Vector stepOffset = new Vector(8, 5);

                // Implementation detail: the extension hands the drag to Windows from the cursor's position when the mouse passes the drag threshold, on the first step, so the window moves one step less than the mouse
                Vector expectedMovement = stepOffset * (numDragSteps - 1);               

                MouseController.DragWithLeftButton(start, stepOffset, numDragSteps);

                Rect windowBoundsAfter = Rect.Empty;
                Wait.PollUntilTrueOrThrow(
                    () =>
                    {
                        windowBoundsAfter = ScreenElementsController.GetWindowScreenBoundsInPixels(vs.MainWindowHandle);

                        double movedX = windowBoundsAfter.X - windowBoundsBefore.X;
                        double movedY = windowBoundsAfter.Y - windowBoundsBefore.Y;
                        
                        const double AllowedErrorInPixels = 1;
                        return Math.Abs(movedX - expectedMovement.X) <= AllowedErrorInPixels
                            && Math.Abs(movedY - expectedMovement.Y) <= AllowedErrorInPixels;
                    },
                    Wait.WaitToHappenDuration,
                    () => $"the window to move by {expectedMovement} (it was at {windowBoundsBefore.TopLeft}, now {windowBoundsAfter.TopLeft})");
                _output.WriteLine($"Window moved from {windowBoundsBefore.TopLeft} to {windowBoundsAfter.TopLeft}.");

                Assert.Null(Wait.ReadUntilChangedOrTimeout(() => vs.ScreenElementsController.FindWindow(AliasDialogUtil.DialogName), null, Wait.CheckNothingChangesDuration));
            });

            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), test);
        }

        [Theory]
        [VisualStudioVersions]
        public void SolutionContextMenu_SetSolutionAlias_OpensTheDialog(int visualStudioVersion)
        {
            var test = (VisualStudioSession vs) => AliasDialogUtil.RunAndCloseLeftoverDialogs(vs, () =>
            {
                // Solution Explorer may be closed or behind another tab; this shows it and brings it to the front.
                vs.Dte.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Activate();

                AutomationElement? solutionRow = null;
                Wait.PollUntilTrueOrThrow(() => (solutionRow = vs.SolutionExplorerRows.FindSolutionRow(SampleSolutionCopy.SmallSolutionName)) != null, Wait.WaitToHappenDuration, () => "the Solution row");

                Point row = ScreenElementsController.GetScreenCentreInPixels(solutionRow!);
                ScreenElementsController.BringWindowToFront(vs.MainWindowHandle);
                MouseController.ThrowIfPointIsNotOverProcessWindow(row, vs.ProcessId, descriptionOfPoint: "the Solution row");

                // Right click context menu
                MouseController.RightClick(row);

                // Find the Alias option an click it
                AutomationElement item = vs.ScreenElementsController.GetVisibleElement("Set Solution Alias...", ControlType.MenuItem, Wait.WaitToHappenDuration);
                MouseController.LeftClick(ScreenElementsController.GetScreenCentreInPixels(item));

                // Check the dialog opened and close it
                AutomationElement dialog = AliasDialogUtil.WaitForDialogToOpen(vs);
                ScreenElementsController.PressButtonOrMenuItem(ScreenElementsController.GetDescendantByNameAndType(dialog, "Cancel", ControlType.Button, Wait.WaitToHappenDuration));
                AliasDialogUtil.WaitForDialogToClose(vs);
            });

            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), test);
        }
    }
}
