using System;
using SolutionAlias.Options;

namespace SolutionAlias.Label
{
    internal static class LabelOpacity
    {
        /// <summary>A WPF opacity from 0.1 to 1; percentages outside 10-100 are taken as 10 or 100.</summary>
        public static double ClampPercentAndConvertToOpacity(int percent) =>
            Math.Max(GeneralOptions.MinimumOpacityPercent, Math.Min(GeneralOptions.MaximumOpacityPercent, percent)) / 100.0;
    }
}
