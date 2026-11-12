using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace SolutionAlias.Label
{
    /// <summary>Label colouring presets and utilities.</summary>
    internal static class LabelColours
    {
        /// <summary>
        /// The colours <see cref="CalculateBackgroundColourFromPath"/> picks from, all light enough for black text.
        /// </summary>
        public static readonly IReadOnlyList<Color> AutomaticBackgroundColours = new[]
        {
            Color.FromRgb(0xE6, 0xA7, 0xB4),
            Color.FromRgb(0xE6, 0x8A, 0x9C),
            Color.FromRgb(0xE8, 0x69, 0x82),
            Color.FromRgb(0xEC, 0x83, 0x83),
            Color.FromRgb(0xE0, 0x9D, 0x90),
            Color.FromRgb(0xE8, 0x7E, 0x69),
            Color.FromRgb(0xE8, 0x8F, 0x69),
            Color.FromRgb(0xE6, 0xBA, 0xA7),
            Color.FromRgb(0xEA, 0xAC, 0x76),
            Color.FromRgb(0xE6, 0xCB, 0xA7),
            Color.FromRgb(0xE8, 0xBD, 0x69),
            Color.FromRgb(0xE3, 0xD5, 0x9C),
            Color.FromRgb(0xE8, 0xD7, 0x69),
            Color.FromRgb(0xE1, 0xEB, 0xA3),
            Color.FromRgb(0xB9, 0xE8, 0x69),
            Color.FromRgb(0x9E, 0xDC, 0x84),
            Color.FromRgb(0x69, 0xE8, 0x69),
            Color.FromRgb(0xA7, 0xE6, 0xB2),
            Color.FromRgb(0x69, 0xE8, 0x9C),
            Color.FromRgb(0x69, 0xE8, 0xC2),
            Color.FromRgb(0x9E, 0xF0, 0xE2),
            Color.FromRgb(0x69, 0xDF, 0xE8),
            Color.FromRgb(0xA7, 0xDA, 0xE6),
            Color.FromRgb(0x69, 0xCE, 0xE8),
            Color.FromRgb(0x69, 0xBD, 0xE8),
            Color.FromRgb(0xA7, 0xC7, 0xE6),
            Color.FromRgb(0x69, 0xA0, 0xE8),
            Color.FromRgb(0x84, 0x95, 0xDC),
            Color.FromRgb(0xA5, 0xAC, 0xE9),
            Color.FromRgb(0x69, 0x7E, 0xE8),
            Color.FromRgb(0x95, 0x83, 0xEC),
            Color.FromRgb(0x7A, 0x69, 0xE8),
            Color.FromRgb(0xB5, 0x90, 0xE0),
            Color.FromRgb(0xA8, 0x69, 0xE8),
            Color.FromRgb(0xBF, 0x78, 0xD9),
            Color.FromRgb(0xEA, 0x76, 0xEA),
            Color.FromRgb(0xE6, 0xA7, 0xDE),
            Color.FromRgb(0xDC, 0x84, 0xC8),
            Color.FromRgb(0xEE, 0x91, 0xBC),
            Color.FromRgb(0xE8, 0x69, 0xA4),
        };

        /// <summary>A frozen brush: it can't be changed after creation, so WPF can optimise it.</summary>
        public static Brush CreateFrozenBrush(Color colour)
        {
            var brush = new SolidColorBrush(colour);
            brush.Freeze();
            return brush;
        }

        /// <summary>The text colour for a solution file's path: always black, which fits every automatic colour.</summary>
        public static Color CalculateTextColourFromPath(string solutionPath) => Colors.Black;

        /// <summary>
        /// The background colour for a solution file's path: the Auto background.
        /// </summary>
        /// <remarks>
        /// FNV hash of the path picks a colour.
        /// </remarks>
        public static Color CalculateBackgroundColourFromPath(string solutionPath)
        {
            string key = GetFullPathIgnoringErrors(solutionPath).ToUpperInvariant();

            // using FNV hash parameter values
            uint hash = 2166136261;
            foreach (char c in key)
            {
                hash ^= c;
                hash *= 16777619;
            }

            return AutomaticBackgroundColours[(int)(hash % (uint)AutomaticBackgroundColours.Count)];
        }

        private static string GetFullPathIgnoringErrors(string path)
        {
            try
            {
                return System.IO.Path.GetFullPath(path);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is System.IO.PathTooLongException || ex is System.Security.SecurityException)
            {
                return path;
            }
        }
    }
}
