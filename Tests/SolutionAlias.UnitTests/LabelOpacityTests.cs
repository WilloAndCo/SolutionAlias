using SolutionAlias.Label;
using Xunit;

namespace SolutionAlias.UnitTests
{
    /// <summary>The "Opacity (%)" option as the opacity the label is drawn at (<see cref="LabelOpacity"/>).</summary>
    public sealed class LabelOpacityTests
    {
        [Theory]
        [InlineData(100, 1.0)]
        [InlineData(50, 0.5)]
        [InlineData(10, 0.1)]
        public void ClampPercentAndConvertToOpacity_InRange_ConvertsPercentToFraction(int percent, double opacity)
        {
            Assert.Equal(opacity, LabelOpacity.ClampPercentAndConvertToOpacity(percent), 3);
        }

        [Theory]
        [InlineData(5, 0.1)]
        [InlineData(0, 0.1)]
        [InlineData(-20, 0.1)]
        [InlineData(150, 1.0)]
        public void ClampPercentAndConvertToOpacity_OutOfRange_ClampsTo10Or100(int percent, double opacity)
        {
            Assert.Equal(opacity, LabelOpacity.ClampPercentAndConvertToOpacity(percent), 3);
        }
    }
}
