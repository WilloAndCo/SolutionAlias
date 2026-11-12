using System.Linq;
using System.Windows.Media;
using SolutionAlias.Label;
using SolutionAlias.Storage;
using Xunit;

namespace SolutionAlias.UnitTests
{
    /// <summary>The colours from the solution path: <see cref="LabelColours"/>.</summary>
    public sealed class LabelColoursTests
    {
        [Theory]
        [InlineData(@"C:\Repo\App.sln")]
        [InlineData(@"E:\Dev\Release\App.sln")]
        public void CalculateTextColourFromPath_AnyPath_IsBlack(string solutionPath)
        {
            Assert.Equal(Colors.Black, LabelColours.CalculateTextColourFromPath(solutionPath));
        }

        [Fact]
        public void CalculateBackgroundColourFromPath_SamePath_SameColour()
        {
            Assert.Equal(LabelColours.CalculateBackgroundColourFromPath(@"C:\Repo\App.sln"), LabelColours.CalculateBackgroundColourFromPath(@"C:\Repo\App.sln"));
        }

        [Fact]
        public void CalculateBackgroundColourFromPath_IgnoresCase()
        {
            Assert.Equal(LabelColours.CalculateBackgroundColourFromPath(@"C:\Repo\App.sln"), LabelColours.CalculateBackgroundColourFromPath(@"c:\REPO\app.SLN"));
        }

        [Fact]
        public void CalculateBackgroundColourFromPath_NormalisesRelativeSegments()
        {
            // resolves \.. before choosing colours, so the paths are treated as equal
            Assert.Equal(LabelColours.CalculateBackgroundColourFromPath(@"C:\Repo\App.sln"), LabelColours.CalculateBackgroundColourFromPath(@"C:\Repo\Sub\..\App.sln"));
        }

        [Fact]
        public void CalculateBackgroundColourFromPath_MainAndReleaseCopies_GetDifferentColours()
        {
            // Not true of every two copies, since there are only 40 colours to choose from, but true of these two
            Assert.NotEqual(LabelColours.CalculateBackgroundColourFromPath(@"E:\Dev\Main\App.sln"), LabelColours.CalculateBackgroundColourFromPath(@"E:\Dev\Release\App.sln"));
        }

        [Fact]
        public void CalculateBackgroundColourFromPath_InvalidPath_DoesNotThrow()
        {
            LabelColours.CalculateBackgroundColourFromPath("C:\\Repo\\<bad>|.sln");
        }

        [Fact]
        public void CalculateBackgroundColourFromPath_ManyPaths_UseEveryAutomaticBackgroundColour()
        {
            // The hash spreads paths over the whole list, not just a few of its colours
            int numColoursUsed = Enumerable.Range(0, 2000)
                .Select(i => LabelColours.CalculateBackgroundColourFromPath($@"C:\Repo{i}\App.sln"))
                .Distinct()
                .Count();

            Assert.Equal(LabelColours.AutomaticBackgroundColours.Count, numColoursUsed);
        }

        [Fact]
        public void AutomaticBackgroundColours_WithBlackText_MeetsRequiredContrast()
        {
            foreach (Color background in LabelColours.AutomaticBackgroundColours)
            {
                double ratio = ContrastCalculator.CalculateContrastRatio(Colors.Black, background);
                Assert.True(ratio >= ContrastCalculator.MinimumForNormalText, $"Background {HexColourText.FormatAsHex(background)} with black text: {ratio:0.00}:1");
            }
        }
    }
}
