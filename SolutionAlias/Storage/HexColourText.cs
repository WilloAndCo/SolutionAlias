using System.Globalization;
using System.Windows.Media;

namespace SolutionAlias.Storage
{
    /// <summary>Conversion between a colour and its text in the alias file: "#RRGGBB" only.</summary>
    internal static class HexColourText
    {
        /// <summary>The length of "#RRGGBB".</summary>
        private const int HexTextLength = 7;

        public static Color? Parse(string? text)
        {
            if (text == null)
            {
                return null;
            }

            string trimmedText = text.Trim();
            if (trimmedText.Length != HexTextLength || trimmedText[0] != '#')
            {
                return null;
            }

            // parse the string as hex to int (without the leading #)
            if (int.TryParse(trimmedText.Substring(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int rgb) == false)
            {
                return null;
            }

            return Color.FromRgb((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
        }

        public static string FormatAsHex(Color colour)
        {
            return $"#{colour.R:X2}{colour.G:X2}{colour.B:X2}";
        }
    }
}
