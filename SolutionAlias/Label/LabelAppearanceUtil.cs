using System;
using System.IO;
using System.Linq;
using System.Windows.Media;
using SolutionAlias.Storage;

namespace SolutionAlias.Label
{
    internal static class LabelAppearanceUtil
    {
        /// <summary>Builds what the title-bar label and the dialog's preview show.</summary>
        public static LabelAppearance BuildAppearance(string solutionFilePath, AliasFileContents? aliasFileContents, int numFoldersToShow, bool alwaysUseThemeColour, int opacityPercent, Color themeTextColour, Color themeBackgroundColour)
        {
            double opacity = LabelOpacity.ClampPercentAndConvertToOpacity(opacityPercent);

            string? aliasText = (aliasFileContents?.HasAlias == true) ? aliasFileContents.Alias : null;
            string name = aliasText ?? ShortenPathToLastFolders(solutionFilePath, numFoldersToShow);

            string action = aliasText != null ? "Click to change the alias" : "Click to set an alias";
            if (alwaysUseThemeColour == false)
            {
                action += aliasText != null ? " or its colours" : " or colours";
            }

            string toolTip = $"{solutionFilePath}{Environment.NewLine}{action}, or drag to move the window.";
                        
            if (alwaysUseThemeColour)
            {
                return new LabelAppearance(name, textColour: themeTextColour, backgroundColour: themeBackgroundColour, isFollowingVisualStudioColours: true, toolTip: toolTip, opacity: opacity);
            }
            else
            {
                // Each colour from the file, or from the solution path when the file has none (or can't be read)
                Color textColour = HexColourText.Parse(aliasFileContents?.TextColourRgb) ?? LabelColours.CalculateTextColourFromPath(solutionFilePath);
                Color backgroundColour = HexColourText.Parse(aliasFileContents?.BackgroundColourRgb) ?? LabelColours.CalculateBackgroundColourFromPath(solutionFilePath);
                return new LabelAppearance(name, textColour: textColour, backgroundColour: backgroundColour, isFollowingVisualStudioColours: false, toolTip: toolTip, opacity: opacity);
            }

        }

        public static string ShortenPathToLastFolders(string solutionFilePath, int numFoldersToShow)
        {
            try
            {
                string name = Path.GetFileName(solutionFilePath);
                if (numFoldersToShow <= 0)
                {
                    return name;
                }

                // The folders between the root (E:\ or \\server\share) and the file.
                string root = Path.GetPathRoot(solutionFilePath) ?? string.Empty;
                string directory = Path.GetDirectoryName(solutionFilePath) ?? string.Empty;
                string[] parts = directory.Substring(Math.Min(root.Length, directory.Length))
                    .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length <= numFoldersToShow)
                {
                    return solutionFilePath;
                }

                return "..." + Path.DirectorySeparatorChar
                    + string.Join(Path.DirectorySeparatorChar.ToString(), parts.Skip(parts.Length - numFoldersToShow))
                    + Path.DirectorySeparatorChar + name;
            }
            catch (ArgumentException)
            {
                // Unable to shorten, show the full path
                return solutionFilePath;
            }
        }
    }
}
