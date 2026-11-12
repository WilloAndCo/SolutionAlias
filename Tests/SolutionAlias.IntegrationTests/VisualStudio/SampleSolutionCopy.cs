using System;
using System.IO;
using System.Linq;

namespace SolutionAlias.IntegrationTests.VisualStudio
{
    /// <summary>
    /// Creates a copy of an existing solution in SolutionPaths.SampleSolutions in %TEMP%\SolutionAlias.IntegrationTests,
    /// Tests can use this copy to avoid modifying the original. Tests run one at a time, so one copy of each solution is enough.
    /// </summary>
    internal static class SampleSolutionCopy
    {
        /// <summary>The folder in Tests\SampleSolutions, and also its .sln file's name (Small\Small.sln).</summary>
        public const string SmallSolutionName = "Small";

        /// <summary>The folder in Tests\SampleSolutions, and also its .sln file's name (Nested\Nested.sln).</summary>
        public const string NestedSolutionName = "Nested";

        /// <summary>
        /// Copies the solution's folder, without bin, obj and .vs, and returns the copy's folder.
        /// Deletes an existing solution if one is found
        /// </summary>
        public static string CreateTemporaryCopyOfSolution(string sampleSolutionName)
        {
            string copyFolder = Path.Combine(Path.GetTempPath(), "SolutionAlias.IntegrationTests", sampleSolutionName);
            DeleteLeftoverCopy(copyFolder);
            CopySampleSolutionInto(sampleSolutionName, copyFolder);
            return copyFolder;
        }

        /// <summary>Copies the solution's folder into <paramref name="destinationFolder"/>, without bin, obj and .vs.</summary>
        public static void CopySampleSolutionInto(string sampleSolutionName, string destinationFolder)
        {
            string sampleSolutionDirectory = Path.Combine(SolutionPaths.SampleSolutions, sampleSolutionName);

            Directory.CreateDirectory(destinationFolder);
            foreach (string file in Directory.GetFiles(sampleSolutionDirectory, "*", SearchOption.AllDirectories))
            {
                string pathFromSampleDirectory = file.Substring(sampleSolutionDirectory.Length + 1);
                if (pathFromSampleDirectory.Split('\\').Any(part => part == "bin" || part == "obj" || part == ".vs"))
                {
                    // cannot be copied because they rely on existing paths
                    continue;
                }

                string destination = Path.Combine(destinationFolder, pathFromSampleDirectory);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(file, destination);
            }
        }

        /// <summary>Throws if the copy cannot be deleted</summary>
        public static void DeleteTemporaryCopyOfSolution(string copiedSolutionDirectory)
        {
            try
            {
                Directory.Delete(copiedSolutionDirectory, recursive: true);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new InvalidOperationException($"Couldn't delete the solution copy ({copiedSolutionDirectory}): {ex.Message}", ex);
            }
        }

        /// <summary>A copy is left when an earlier test couldn't delete its copy, or when a run was stopped before deleting it.</summary>
        private static void DeleteLeftoverCopy(string copyFolder)
        {
            if (Directory.Exists(copyFolder) == false)
            {
                return;
            }

            try
            {
                Directory.Delete(copyFolder, recursive: true);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new InvalidOperationException($"Couldn't delete the solution copy an earlier test left ({copyFolder}): {ex.Message}", ex);
            }
        }
    }
}
