using System.IO;

namespace SolutionAlias.Storage
{
    internal sealed class SolutionAndAliasLocation
    {
        private SolutionAndAliasLocation(string solutionFilePath, AliasFileLocation aliasFileLocation)
        {
            SolutionFilePath = solutionFilePath;
            AliasFileLocation = aliasFileLocation;
        }

        public string SolutionFilePath { get; }
        public AliasFileLocation AliasFileLocation { get; }

        public static SolutionAndAliasLocation? CreateForSavedSolution(string? solutionDirectory, string? solutionFilePath)
        {
            // A solution that has never been saved (a new one, or the temporary one Visual Studio makes when a project file
            // is opened on its own) still has a path where it would be saved, but no file there yet.
            if (solutionFilePath == null || File.Exists(solutionFilePath) == false)
            {
                return null;
            }

            AliasFileLocation? aliasFileLocation = AliasFileLocation.CreateForSolution(solutionDirectory, solutionFilePath);
            return aliasFileLocation != null ? new SolutionAndAliasLocation(solutionFilePath, aliasFileLocation) : null;
        }
    }
}
