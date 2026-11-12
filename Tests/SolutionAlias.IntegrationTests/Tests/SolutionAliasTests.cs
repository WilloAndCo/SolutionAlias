using System.IO;
using SolutionAlias.Label;
using SolutionAlias.Options;
using SolutionAlias.Storage;
using SolutionAlias.IntegrationTests.Run;
using SolutionAlias.IntegrationTests.VisualStudio;
using Xunit;
using Xunit.Abstractions;

namespace SolutionAlias.IntegrationTests.Tests
{
    /// <summary>Tests that interact with the extension using direct DTE calls and the VS Inspector extension</summary>
    [Trait("Category", "Integration")]
    public sealed class SolutionAliasTests
    {
        private const string ExtensionAssemblyName = "SolutionAlias";
        private readonly ITestOutputHelper _output;

        public SolutionAliasTests(ITestOutputHelper output)
        {
            _output = output;
            VisualStudioTestRun.CurrentTestOutput = output;
        }

        [Theory]
        [VisualStudioVersions]
        public void NoAlias_ShowsSolutionPathInPathColour(int visualStudioVersion)
        {
            var test = (VisualStudioSession vs) =>
            {
                AliasLabelUtil.WaitAndAssertLabelAppearanceMatches(vs, AliasLabelUtil.BuildExpectedAppearance(vs.SolutionFile, null), _output);
            };

            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), test);
        }

        [Theory]
        [VisualStudioVersions]
        public void AliasFile_ShowsAliasInItsColours(int visualStudioVersion)
        {
            var alias = new AliasFileContents { Alias = "MAIN", BackgroundColourRgb = "#C0392B" };
            var prepareCopy = (string copyDirectory) => AliasFileUtil.WriteAliasFile(copyDirectory, SampleSolutionCopy.SmallSolutionName, alias);
            var test = (VisualStudioSession vs) => AliasLabelUtil.WaitAndAssertLabelAppearanceMatches(vs, AliasLabelUtil.BuildExpectedAppearance(vs.SolutionFile, alias), _output);

            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName, prepareCopy), test);
        }

        [Theory]
        [VisualStudioVersions]
        public void ShowLabelOption_HidesAndShowsTheLabel(int visualStudioVersion)
        {
            var test = (VisualStudioSession vs) =>
            {
                AliasLabelUtil.WaitForLabelToAppear(vs);

                vs.VsInspector.SetAndSaveOption(ExtensionAssemblyName, nameof(GeneralOptions.ShowLabel), false);
                AliasLabelUtil.WaitAndAssertLabelAttached(vs, shouldBeAttached: false, failureMessage: "The label is still there with Show label off.");

                vs.VsInspector.SetAndSaveOption(ExtensionAssemblyName, nameof(GeneralOptions.ShowLabel), true);
                AliasLabelUtil.WaitAndAssertLabelAttached(vs, shouldBeAttached: true, failureMessage: "The label didn't come back with Show label on.");
            };

            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), test);
        }

        [Theory]
        [VisualStudioVersions]
        public void FoldersToShowOption_ShortensThePath(int visualStudioVersion)
        {
            var test = (VisualStudioSession vs) =>
            {
                string defaultPathDisplay = LabelAppearanceUtil.ShortenPathToLastFolders(vs.SolutionFile, GeneralOptions.DefaultNumFoldersToShow);
                _output.WriteLine($"At the default: {defaultPathDisplay}");
                AliasLabelUtil.WaitAndAssertEqual(vs.VsInspector.ReadAliasLabelText, defaultPathDisplay);

                string solutionFileName = SampleSolutionCopy.SmallSolutionName + ".sln";
                string oneFolderPathDisplay = Path.Combine("...", SampleSolutionCopy.SmallSolutionName, solutionFileName);

                foreach ((int folders, string expected) in new[] { (0, solutionFileName), (1, oneFolderPathDisplay), (100, vs.SolutionFile) })
                {
                    vs.VsInspector.SetAndSaveOption(ExtensionAssemblyName, nameof(GeneralOptions.NumFoldersToShow), folders);
                    AliasLabelUtil.WaitAndAssertEqual(vs.VsInspector.ReadAliasLabelText, expected);
                    Assert.StartsWith(vs.SolutionFile, vs.VsInspector.ReadAliasLabelToolTip());
                }
            };

            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), test);
        }

        [Theory]
        [VisualStudioVersions]
        public void AlwaysUseThemeColourOn_UsesVisualStudiosColours(int visualStudioVersion)
        {
            var test = (VisualStudioSession vs) =>
            {
                string pathColour = LabelColours.CalculateBackgroundColourFromPath(vs.SolutionFile).ToString();
                AliasLabelUtil.WaitAndAssertEqual(vs.VsInspector.ReadAliasLabelBackground, pathColour);

                vs.VsInspector.SetAndSaveOption(ExtensionAssemblyName, nameof(GeneralOptions.AlwaysUseThemeColour), true);

                string background = Wait.ReadUntilChangedOrTimeout(vs.VsInspector.ReadAliasLabelBackground, pathColour, Wait.WaitToHappenDuration);
                _output.WriteLine($"Background with Always use theme colour on: {background} (path colour {pathColour})");
                Assert.NotEqual(pathColour, background);
            };

            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), test);
        }

        [Theory]
        [VisualStudioVersions]
        public void OpacityOption_ChangesTheLabelOpacity(int visualStudioVersion)
        {
            var test = (VisualStudioSession vs) =>
            {
                Wait.PollUntilTrueOrThrow(vs.VsInspector.IsAliasLabelDrawn, Wait.WaitToHappenDuration, () => "the label to be drawn");
                Assert.Equal(GeneralOptions.DefaultOpacityPercent, vs.VsInspector.ReadAliasLabelOpacityPercent());

                vs.VsInspector.SetAndSaveOption(ExtensionAssemblyName, nameof(GeneralOptions.OpacityPercent), 40);
                AliasLabelUtil.WaitAndAssertEqual(vs.VsInspector.ReadAliasLabelOpacityPercent, 40);
                Assert.True(vs.VsInspector.IsAliasLabelDrawn(), "The label isn't drawn at 40% opacity.");

                // Out of range: treated as the nearest end of 10-100.
                vs.VsInspector.SetAndSaveOption(ExtensionAssemblyName, nameof(GeneralOptions.OpacityPercent), 0);
                AliasLabelUtil.WaitAndAssertEqual(vs.VsInspector.ReadAliasLabelOpacityPercent, 10);
                Assert.True(vs.VsInspector.IsAliasLabelDrawn(), "The label isn't drawn at the lowest opacity.");
            };

            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), test);
        }

        [Theory]
        [VisualStudioVersions]
        public void ClosingAndReopeningTheSolution_ReloadsTheAlias(int visualStudioVersion)
        {
            var test = (VisualStudioSession vs) =>
            {
                AliasLabelUtil.WaitForLabelToAppear(vs);

                vs.Dte.Solution.Close(SaveFirst: false);
                AliasLabelUtil.WaitAndAssertLabelAttached(vs, shouldBeAttached: false, failureMessage: "The label is still there with no solution open.");
                                
                var alias = new AliasFileContents { Alias = "RELEASE" };
                AliasFileUtil.WriteAliasFile(vs.SolutionDirectory, SampleSolutionCopy.SmallSolutionName, alias);
                vs.Dte.Solution.Open(vs.SolutionFile);

                AliasLabelUtil.WaitAndAssertLabelAppearanceMatches(vs, AliasLabelUtil.BuildExpectedAppearance(vs.SolutionFile, alias), _output);
            };

            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), test);
        }

        [Theory]
        [VisualStudioVersions]
        public void UnsavedSolution_HasNoLabel(int visualStudioVersion)
        {
            var test = (VisualStudioSession vs) =>
            {
                AliasLabelUtil.WaitForLabelToAppear(vs);

                vs.Dte.Solution.Close(SaveFirst: false);

                AliasLabelUtil.WaitAndAssertLabelAttached(vs, shouldBeAttached: false, failureMessage: "The label is still there with no solution open.");

                vs.Dte.Solution.Create(Path.Combine(vs.SolutionDirectory, "Unsaved"), "Unsaved");

                bool attached = Wait.ReadUntilChangedOrTimeout(vs.VsInspector.IsAliasLabelAttached, false, Wait.CheckNothingChangesDuration);
                _output.WriteLine($"New solution: '{vs.Dte.Solution.FullName}', label attached: {attached}");
                Assert.False(attached, "A solution that hasn't been saved shows a label.");

                // Saved afterwards so Visual Studio doesn't ask about it when the test closes it.
                // Will be deleted after as it's in the copy directory which gets cleaned up.
                string saveAs = Path.Combine(vs.SolutionDirectory, "Unsaved", "Unsaved.sln");
                Directory.CreateDirectory(Path.GetDirectoryName(saveAs));
                vs.Dte.Solution.SaveAs(saveAs);
            };

            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName), test);
        }

        [Theory]
        [VisualStudioVersions]
        public void OpeningAnotherSolution_FirstHadAnAlias_ShowsTheNewSolutionsPath(int visualStudioVersion)
        {
            var alias = new AliasFileContents { Alias = "MAIN", BackgroundColourRgb = "#C0392B" };

            // Nested is copied inside Small's copy, so it is deleted along with it.
            var prepareCopy = (string copyDirectory) =>
            {
                AliasFileUtil.WriteAliasFile(copyDirectory, SampleSolutionCopy.SmallSolutionName, alias);
                SampleSolutionCopy.CopySampleSolutionInto(SampleSolutionCopy.NestedSolutionName, Path.Combine(copyDirectory, SampleSolutionCopy.NestedSolutionName));
            };

            var test = (VisualStudioSession vs) =>
            {
                AliasLabelUtil.WaitAndAssertLabelAppearanceMatches(vs, AliasLabelUtil.BuildExpectedAppearance(vs.SolutionFile, alias), _output);

                // Nested has no alias file, so Small's alias and colours mustn't carry over.
                string nestedSolutionFile = Path.Combine(vs.SolutionDirectory, SampleSolutionCopy.NestedSolutionName, SampleSolutionCopy.NestedSolutionName + ".sln");
                vs.Dte.Solution.Open(nestedSolutionFile);

                AliasLabelUtil.WaitAndAssertLabelAppearanceMatches(vs, AliasLabelUtil.BuildExpectedAppearance(nestedSolutionFile, null), _output);
            };

            VisualStudioTestRun.RunTestInVisualStudio(new VisualStudioTestSetup(visualStudioVersion, SampleSolutionCopy.SmallSolutionName, prepareCopy), test);
        }
    }
}
