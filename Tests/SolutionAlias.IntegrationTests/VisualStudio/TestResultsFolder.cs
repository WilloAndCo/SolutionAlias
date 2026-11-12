using System;
using System.IO;

namespace SolutionAlias.IntegrationTests.VisualStudio
{
    /// <summary>
    /// A result folder for all test results, %TEMP%\SolutionAlias.IntegrationTests\Results
    /// Stored to review test results after they have run (each test has a separate results folder for each Visual Studio version)
    /// </summary>
    internal static class TestResultsFolder
    {
        private const string ActivityLogFileName = "ActivityLog.xml";
        private const string FailureFileName = "Failure.txt";
        private const string WarningFileName = "Warning.txt";

        /// <summary>Creates or clears a results folder for one test on one Visual Studio version</summary>
        public static string CreateResultsFolder(string testName, int visualStudioVersion)
        {
            string resultsFolder = Path.Combine(
                Path.GetTempPath(),
                "SolutionAlias.IntegrationTests",
                "Results",
                $"{testName}-{visualStudioVersion}");

            Directory.CreateDirectory(resultsFolder);

            // Deletes the files rather than the folder: creating a folder straight after deleting it can fail while
            // Windows is still finishing the delete.
            foreach (string file in Directory.GetFiles(resultsFolder))
            {
                File.Delete(file);
            }

            return resultsFolder;
        }

        /// <summary>Where Visual Studio writes its ActivityLog for the test.</summary>
        public static string GetActivityLogFile(string resultsFolder) =>
            Path.Combine(resultsFolder, ActivityLogFileName);

        /// <summary> Adds a warning to Warning.txt </summary>
        public static void LogWarning(string resultsFolder, string message)
        {
            try
            {
                File.AppendAllText(Path.Combine(resultsFolder, WarningFileName), message + Environment.NewLine);
            }
            catch (IOException)
            {
            }
        }

        /// <summary> Adds a failure to Failure.txt </summary>
        public static void LogFailure(string resultsFolder, string message)
        {
            try
            {
                File.AppendAllText(Path.Combine(resultsFolder, FailureFileName), message + Environment.NewLine);
            }
            catch (IOException)
            {
            }
        }
    }
}
