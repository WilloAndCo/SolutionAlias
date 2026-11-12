using System.Windows.Media;
using Xunit;

namespace SolutionAlias.UnitTests
{
    /// <summary>The test helper that checks colour contrast: <see cref="ContrastCalculator"/>.</summary>
    public sealed class ContrastCalculatorTests
    {
        [Fact]
        public void CalculateContrastRatio_GreyOnWhite_MeetsMinimumForNormalText()
        {
            // #767676 is the lightest grey that passes on white; WCAG checkers report it as 4.54:1
            double ratio = ContrastCalculator.CalculateContrastRatio(Color.FromRgb(0x76, 0x76, 0x76), Colors.White);

            Assert.Equal(4.54, ratio, precision: 2);
            Assert.True(ratio >= ContrastCalculator.MinimumForNormalText, $"#767676 on white: {ratio:0.00}:1");
        }

        [Fact]
        public void CalculateContrastRatio_LightGreyOnWhite_FailsMinimumForNormalText()
        {
            // One step lighter than #767676, so just too light; WCAG checkers report it as 4.48:1
            double ratio = ContrastCalculator.CalculateContrastRatio(Color.FromRgb(0x77, 0x77, 0x77), Colors.White);

            Assert.Equal(4.48, ratio, precision: 2);
            Assert.True(ratio < ContrastCalculator.MinimumForNormalText, $"#777777 on white: {ratio:0.00}:1");
        }
    }
}
