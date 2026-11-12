using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using SolutionAlias.Dialog;
using Xunit;

namespace SolutionAlias.UnitTests
{
    /// <summary>Runs test code on a new STA thread, needed for WPF controls</summary>
    internal static class StaThread
    {
        public static void RunOnNewStaThread(Action test)
        {
            ExceptionDispatchInfo? failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    test();
                }
                catch (Exception ex)
                {
                    failure = ExceptionDispatchInfo.Capture(ex);
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            // Rethrown with its original stack trace, so a failed Assert reports the line it failed on.
            failure?.Throw();
        }
    }

    /// <summary>The alias dialog's rows of colour swatches</summary>
    public sealed class ColourSwatchRowTests
    {
        private const string AutoSwatchName = "Background colour: Auto";
        private const string MintSwatchName = "Background colour: Mint";

        // No path to create an automatic colour, so use this placeholder
        private static readonly Color PlacholderAutoBackgroundColour = Color.FromRgb(0x12, 0x34, 0x56);
        private static readonly Color NotAPresetColour = Color.FromRgb(0x65, 0x43, 0x21);
        private static readonly Color Mint = ColourPresets.Background.Single(preset => preset.Name == "Mint").Colour;

        [Fact]
        public void CreateBackgroundColourRow_InitialColourIsTheAutomaticBackground_ChecksAuto()
        {
            StaThread.RunOnNewStaThread(() =>
            {
                var panel = new WrapPanel();
                ColourSwatchRow row = CreateBackgroundColourRow(panel, automaticBackground: PlacholderAutoBackgroundColour, initialColour: PlacholderAutoBackgroundColour);

                Assert.Equal(new[] { AutoSwatchName }, ReadCheckedSwatchNames(panel));
                Assert.Equal(PlacholderAutoBackgroundColour, row.ChosenColour);
            });
        }

        [Fact]
        public void CreateBackgroundColourRow_AutomaticBackgroundIsAlsoAPreset_ChecksAuto()
        {
            StaThread.RunOnNewStaThread(() =>
            {
                var panel = new WrapPanel();
                CreateBackgroundColourRow(panel, automaticBackground: Mint, initialColour: Mint);

                // If the automatic colour is the same as a present, the automatic swatch is selected
                Assert.Equal(new[] { AutoSwatchName }, ReadCheckedSwatchNames(panel));
            });
        }

        [Fact]
        public void CreateBackgroundColourRow_InitialColourIsAPreset_ChecksThatPreset()
        {
            StaThread.RunOnNewStaThread(() =>
            {
                var panel = new WrapPanel();
                ColourSwatchRow row = CreateBackgroundColourRow(panel, automaticBackground: PlacholderAutoBackgroundColour, initialColour: Mint);

                Assert.Equal(new[] { MintSwatchName }, ReadCheckedSwatchNames(panel));
                Assert.Equal(Mint, row.ChosenColour);
            });
        }

        [Fact]
        public void CreateBackgroundColourRow_InitialColourMatchesNoSwatch_ShowsAndChecksTheCustomSwatch()
        {
            StaThread.RunOnNewStaThread(() =>
            {
                var panel = new WrapPanel();
                ColourSwatchRow row = CreateBackgroundColourRow(panel, automaticBackground: PlacholderAutoBackgroundColour, initialColour: NotAPresetColour);

                RadioButton customSwatch = panel.Children.OfType<RadioButton>().Single(swatch => AutomationProperties.GetName(swatch).Contains("custom colour"));
                Assert.Equal(new[] { AutomationProperties.GetName(customSwatch) }, ReadCheckedSwatchNames(panel));
                Assert.Equal(Visibility.Visible, customSwatch.Visibility);
                Assert.Equal(NotAPresetColour, row.ChosenColour);
            });
        }

        [Fact]
        public void ChosenColourChanged_AnotherSwatchChecked_RaisedOnceWithItsColour()
        {
            StaThread.RunOnNewStaThread(() =>
            {
                var panel = new WrapPanel();
                ColourSwatchRow row = CreateBackgroundColourRow(panel, automaticBackground: PlacholderAutoBackgroundColour, initialColour: PlacholderAutoBackgroundColour);
                int timesRaised = 0;
                row.ChosenColourChanged += (sender, e) => timesRaised++;

                FindSwatch(panel, MintSwatchName).IsChecked = true;

                Assert.Equal(1, timesRaised);
                Assert.Equal(Mint, row.ChosenColour);
            });
        }

        private static ColourSwatchRow CreateBackgroundColourRow(Panel panel, Color automaticBackground, Color initialColour) =>
            ColourSwatchRow.CreateBackgroundColourRow(panel, new ControlTemplate(typeof(RadioButton)), automaticBackground: automaticBackground, initialColour: initialColour);

        private static string[] ReadCheckedSwatchNames(Panel panel) =>
            panel.Children.OfType<RadioButton>().Where(swatch => swatch.IsChecked == true).Select(swatch => AutomationProperties.GetName(swatch)).ToArray();

        private static RadioButton FindSwatch(Panel panel, string swatchName) =>
            panel.Children.OfType<RadioButton>().Single(swatch => AutomationProperties.GetName(swatch) == swatchName);
    }
}
