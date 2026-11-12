using System;
using System.IO;

namespace SolutionAlias.IntegrationTests.VisualStudio
{
    /// <summary> Find Folders within the Solution </summary>
    internal static class SolutionPaths
    {
        private static readonly Lazy<string> RootPath = new Lazy<string>(FindSolutionRoot);

        public static string Root => RootPath.Value;

        public static string SampleSolutions => Path.Combine(Root, @"Tests\SampleSolutions");

        private static string FindSolutionRoot()
        {
            // walk up the directory from the output folder until we find the solution file
            for (string directory = AppDomain.CurrentDomain.BaseDirectory; directory != null; directory = Path.GetDirectoryName(directory))
            {
                if (File.Exists(Path.Combine(directory, "SolutionAlias.sln")))
                {
                    return directory;
                }
            }

            throw new InvalidOperationException($"SolutionAlias.sln not found above {AppDomain.CurrentDomain.BaseDirectory}.");
        }
    }
}
