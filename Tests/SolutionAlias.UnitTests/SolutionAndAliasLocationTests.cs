using System;
using System.IO;
using SolutionAlias.Storage;
using Xunit;

namespace SolutionAlias.UnitTests
{
    /// <summary>A saved solution's .sln file and its alias file: <see cref="SolutionAndAliasLocation"/>.</summary>
    public sealed class SolutionAndAliasLocationTests : IDisposable
    {
        // An instance of the class is created per test, so guid gives each one a different temp directory. "N" excludes the guid hyphens.
        private readonly string _tempSolutionDirectory = Path.Combine(Path.GetTempPath(), "SolutionAlias.UnitTests", Guid.NewGuid().ToString("N"));

        public void Dispose()
        {
            if (Directory.Exists(_tempSolutionDirectory))
            {
                Directory.Delete(_tempSolutionDirectory, recursive: true);
            }
        }

        [Fact]
        public void CreateForSavedSolution_SolutionFileExists_ReturnsTheSolutionAndItsAliasFile()
        {
            string solutionFilePath = Path.Combine(_tempSolutionDirectory, "MyApp.sln");
            Directory.CreateDirectory(_tempSolutionDirectory);
            File.WriteAllText(solutionFilePath, string.Empty);

            SolutionAndAliasLocation? location = SolutionAndAliasLocation.CreateForSavedSolution(_tempSolutionDirectory, solutionFilePath);

            Assert.NotNull(location);
            Assert.Equal(solutionFilePath, location.SolutionFilePath);
            Assert.Equal(Path.Combine(_tempSolutionDirectory, ".vs", "MyApp", "SolutionAlias.json"), location.AliasFileLocation.Path);
        }

        [Fact]
        public void CreateForSavedSolution_SolutionNeverSaved_ReturnsNull()
        {
            // Visual Studio reports where an unsaved solution would be saved, but there is no file there.
            string solutionFilePath = Path.Combine(_tempSolutionDirectory, "MyApp.sln");

            Assert.Null(SolutionAndAliasLocation.CreateForSavedSolution(_tempSolutionDirectory, solutionFilePath));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void CreateForSavedSolution_NoSolutionFilePath_ReturnsNull(string? solutionFilePath)
        {
            Assert.Null(SolutionAndAliasLocation.CreateForSavedSolution(_tempSolutionDirectory, solutionFilePath));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void CreateForSavedSolution_SolutionFileExistsButNoDirectory_ReturnsNull(string? solutionDirectory)
        {
            string solutionFilePath = Path.Combine(_tempSolutionDirectory, "MyApp.sln");
            Directory.CreateDirectory(_tempSolutionDirectory);
            File.WriteAllText(solutionFilePath, string.Empty);

            Assert.Null(SolutionAndAliasLocation.CreateForSavedSolution(solutionDirectory, solutionFilePath));
        }
    }
}
