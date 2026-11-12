using System.Windows.Media;
using SolutionAlias.Storage;
using Xunit;

namespace SolutionAlias.UnitTests
{
    /// <summary>Colours as text in the alias file, "#RRGGBB": <see cref="HexColourText"/>.</summary>
    public sealed class HexColourTextTests
    {
        private static readonly Color Red = Color.FromRgb(0xC0, 0x39, 0x2B);

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("#GG0000")]
        [InlineData("not a colour")]
        [InlineData("#12")]
        public void Parse_EmptyOrInvalid_ReturnsNull(string? text)
        {
            Assert.Null(HexColourText.Parse(text));
        }

        [Theory]
        [InlineData("#C0392B")]
        [InlineData("#c0392b")]
        [InlineData("  #C0392B  ")]
        public void Parse_Hex_ReturnsColour(string text)
        {
            Assert.Equal(Red, HexColourText.Parse(text));
        }

        [Theory]
        [InlineData("Red")]
        [InlineData("Transparent")]
        [InlineData("#80C0392B")]
        [InlineData("#FFC0392B")]
        [InlineData("#C03")]
        [InlineData("C0392B")]
        [InlineData("#C0 392B")]
        [InlineData("#+C0392")]
        public void Parse_NotSixDigitHex_ReturnsNull(string text)
        {
            Assert.Null(HexColourText.Parse(text));
        }

        [Fact]
        public void FormatAsHex_Opaque_IsSixDigits()
        {
            Assert.Equal("#C0392B", HexColourText.FormatAsHex(Red));
        }

        [Fact]
        public void FormatAsHex_NotOpaque_LeavesOutAlpha()
        {
            Assert.Equal("#C0392B", HexColourText.FormatAsHex(Color.FromArgb(0x80, 0xC0, 0x39, 0x2B)));
        }
    }
}
