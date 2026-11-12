using System;
using System.Windows;
using System.Windows.Media;
using SolutionAlias.Label;
using SolutionAlias.Options;
using SolutionAlias.Storage;
using SolutionAlias.IntegrationTests.Screen;
using SolutionAlias.IntegrationTests.VisualStudio;
using Xunit;
using Xunit.Abstractions;

namespace SolutionAlias.IntegrationTests.Tests
{
    internal static class AliasLabelUtil
    {
        public static LabelAppearance BuildExpectedAppearance(string solutionFile, AliasFileContents? alias) =>
            LabelAppearanceUtil.BuildAppearance(
                solutionFile,
                alias,
                numFoldersToShow: GeneralOptions.DefaultNumFoldersToShow,
                alwaysUseThemeColour: false,
                opacityPercent: GeneralOptions.DefaultOpacityPercent,
                themeTextColour: Colors.White, // never used with alwaysUseThemeColour: false
                themeBackgroundColour: Colors.DimGray); // never used with alwaysUseThemeColour: false

        public static void WaitForLabelToAppear(VisualStudioSession vs)
        {
            Wait.PollUntilTrueOrThrow(vs.VsInspector.IsAliasLabelAttached, Wait.WaitToHappenDuration, () => "the label to appear");
        }

        public static void WaitAndAssertEqual<T>(Func<T> read, T expected)
        {
            Assert.Equal(expected, Wait.ReadUntilMatchOrTimeout(read, expected, Wait.WaitToHappenDuration));
        }

        public static void WaitAndAssertLabelAttached(VisualStudioSession vs, bool shouldBeAttached, string failureMessage)
        {
            bool isAttached = Wait.ReadUntilMatchOrTimeout(vs.VsInspector.IsAliasLabelAttached, shouldBeAttached, Wait.WaitToHappenDuration);
            Assert.True(isAttached == shouldBeAttached, failureMessage);
        }

        public static void WaitAndAssertLabelAppearanceMatches(VisualStudioSession vs, LabelAppearance expected, ITestOutputHelper output)
        {
            string text = Wait.ReadUntilMatchOrTimeout(vs.VsInspector.ReadAliasLabelText, expected.Name, Wait.WaitToHappenDuration);
            output.WriteLine($"Label: '{text}', text {vs.VsInspector.ReadAliasLabelForeground()}, background {vs.VsInspector.ReadAliasLabelBackground()}, shown {vs.VsInspector.IsAliasLabelDrawn()}.");

            Assert.Equal(expected.Name, text);
            Assert.Equal(expected.TextColour.ToString(), vs.VsInspector.ReadAliasLabelForeground());
            Assert.Equal(expected.BackgroundColour.ToString(), vs.VsInspector.ReadAliasLabelBackground());
            Assert.Equal(expected.ToolTip, vs.VsInspector.ReadAliasLabelToolTip());
        }

        /// <summary>The middle of the label, once Visual Studio is in front and the label has stayed in the same place.</summary>
        public static Point BringToFrontAndWaitForLabelCentreInPixels(VisualStudioSession vs)
        {
            ScreenElementsController.BringWindowToFront(vs.MainWindowHandle);
            Rect? previous = null;
            Rect? bounds = null;
            Wait.PollUntilTrueOrThrow(
                () =>
                {
                    previous = bounds;
                    bounds = vs.VsInspector.ReadAliasLabelScreenBounds();
                    return bounds != null && bounds == previous;
                },
                Wait.WaitToHappenDuration,
                () => $"the label to be drawn and stay in place (last at {bounds?.ToString() ?? "nowhere"})");

            Rect drawnBounds = bounds!.Value;
            var centre = new Point(drawnBounds.X + drawnBounds.Width / 2, drawnBounds.Y + drawnBounds.Height / 2);
            MouseController.ThrowIfPointIsNotOverProcessWindow(centre, vs.ProcessId, descriptionOfPoint : "the alias label");
            return centre;
        }
    }
}
