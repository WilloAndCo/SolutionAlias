using System.Collections.Generic;
using System.Windows.Media;

namespace SolutionAlias.Dialog
{
    internal sealed class NameColourPair
    {
        public NameColourPair(string name, string hex)
        {
            Name = name;
            Colour = (Color)ColorConverter.ConvertFromString(hex);
        }

        public string Name { get; }

        public Color Colour { get; }
    }

    /// <summary>Default colours the dialog provides.</summary>
    internal static class ColourPresets
    {
        public static readonly IReadOnlyList<NameColourPair> Text = new[]
        {
            new NameColourPair("Black", "#000000"),
            new NameColourPair("White", "#FFFFFF"),
            new NameColourPair("Light grey", "#E0E0E0"),
            new NameColourPair("Dark grey", "#333333"),
            new NameColourPair("Yellow", "#FFD54F"),
        };

        /// <summary>Setting pastel colour selection by default</summary>
        public static readonly IReadOnlyList<NameColourPair> Background = new[]
        {
            new NameColourPair("Rose", "#F4A6A6"),
            new NameColourPair("Peach", "#F8C291"),
            new NameColourPair("Butter", "#FFE08A"),
            new NameColourPair("Mint", "#B8E0A8"),
            new NameColourPair("Aqua", "#A8DADC"),
            new NameColourPair("Sky", "#A9C6F5"),
            new NameColourPair("Lavender", "#CDB4F0"),
            new NameColourPair("Pink", "#F5B3D3"),
            new NameColourPair("Cloud", "#D5D8DC"),
            new NameColourPair("Sand", "#E6D3B3"),
        };
    }
}
