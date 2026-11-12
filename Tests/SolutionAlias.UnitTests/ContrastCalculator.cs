using System;
using System.Windows.Media;

namespace SolutionAlias.UnitTests
{
    /// <summary>
    /// The WCAG 2.2 contrast ratio between two colours, from 1 (the same) to 21 (black on white).
    /// </summary>
    /// <remarks>
    /// The extension draws text in black unless another colour is chosen, and these checks make sure every colour is suitable for that.
    /// </remarks>
    internal static class ContrastCalculator
    {
        public const double MinimumForNormalText = 4.5;

        public static double CalculateContrastRatio(Color a, Color b)
        {
            double luminanceA = CalculateRelativeLuminance(a);
            double luminanceB = CalculateRelativeLuminance(b);

            // 0.05 is WCAG 2.2 allowance for ambient light reflecting off the screen
            const double ambientLightAllowance = 0.05;
            return (Math.Max(luminanceA, luminanceB) + ambientLightAllowance) / (Math.Min(luminanceA, luminanceB) + ambientLightAllowance);
        }

        // Hardcoded values are channel weights from WCAG 2.2
        private static double CalculateRelativeLuminance(Color colour) =>
            (0.2126 * ConvertChannelToLinear(colour.R)) + (0.7152 * ConvertChannelToLinear(colour.G)) + (0.0722 * ConvertChannelToLinear(colour.B));

        // Hardcoded values are from WCAG 2.2
        private static double ConvertChannelToLinear(byte channel)
        {
            double value = channel / 255.0;
            return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
        }
    }
}
