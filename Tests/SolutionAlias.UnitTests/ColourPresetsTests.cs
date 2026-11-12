using System.Linq;
using System.Windows.Media;
using SolutionAlias.Dialog;
using SolutionAlias.Label;
using SolutionAlias.Storage;
using Xunit;

namespace SolutionAlias.UnitTests
{
    /// <summary>The dialog's colour swatches: <see cref="ColourPresets"/>.</summary>
    public sealed class ColourPresetsTests
    {
        [Fact]
        public void TextAndBackground_FormatAsHexThenParse_Unchanged()
        {
            foreach (NameColourPair preset in ColourPresets.Text.Concat(ColourPresets.Background))
            {
                Assert.Equal(preset.Colour, HexColourText.Parse(HexColourText.FormatAsHex(preset.Colour)));
            }
        }

        [Fact]
        public void Background_ColourNames_AreDistinct()
        {
            Assert.Equal(ColourPresets.Background.Count, ColourPresets.Background.Select(preset => preset.Name).Distinct().Count());
        }

        [Fact]
        public void Background_WithBlackText_MeetsRequiredContrast()
        {
            // The default text is black, so every background preset has to be light enough for it
            foreach (NameColourPair preset in ColourPresets.Background)
            {
                double ratio = ContrastCalculator.CalculateContrastRatio(Colors.Black, preset.Colour);
                Assert.True(ratio >= ContrastCalculator.MinimumForNormalText, $"{preset.Name} {HexColourText.FormatAsHex(preset.Colour)} with black text: {ratio:0.00}:1");
            }
        }

        [Fact]
        public void Text_ColourNames_AreDistinct()
        {
            Assert.Equal(ColourPresets.Text.Count, ColourPresets.Text.Select(preset => preset.Name).Distinct().Count());
        }

        [Fact]
        public void Text_PathStartingColour_IsBlack()
        {
            Assert.Equal("Black", ColourPresets.Text[0].Name);
            Assert.Equal(LabelColours.CalculateTextColourFromPath(@"C:\Repo\App.sln"), ColourPresets.Text[0].Colour);
        }
    }
}
