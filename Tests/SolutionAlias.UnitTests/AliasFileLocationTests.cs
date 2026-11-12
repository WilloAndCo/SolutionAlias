using System;
using System.IO;
using System.Text;
using SolutionAlias.Storage;
using Xunit;

namespace SolutionAlias.UnitTests
{
    /// <summary>The alias file, .vs\&lt;SolutionName&gt;\SolutionAlias.json: <see cref="AliasFileLocation"/>.</summary>
    public sealed class AliasFileLocationTests : IDisposable
    {
        // An instance of the class is created per test, so guid gives each one a different temp directory. "N" excludes the guid hyphens.
        private readonly string _tempSolutionDirectory = Path.Combine(Path.GetTempPath(), "SolutionAlias.UnitTests", Guid.NewGuid().ToString("N"));
        private readonly AliasFileLocation _aliasFileLocation;

        public AliasFileLocationTests()
        {
            _aliasFileLocation = AliasFileLocation.CreateForSolution(_tempSolutionDirectory, Path.Combine(_tempSolutionDirectory, "MyApp.sln"))
                ?? throw new InvalidOperationException("A saved solution's paths gave no alias file.");
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempSolutionDirectory))
            {
                Directory.Delete(_tempSolutionDirectory, recursive: true);
            }
        }

        [Fact]
        public void CreateForSolution_PathIsInDotVsSolutionNameFolder()
        {
            Assert.Equal(Path.Combine(_tempSolutionDirectory, ".vs", "MyApp", "SolutionAlias.json"), _aliasFileLocation.Path);
        }

        [Fact]
        public void CreateForSolution_NameWithDots_KeepsAllButTheExtension()
        {
            AliasFileLocation? location = AliasFileLocation.CreateForSolution(@"C:\Repo", @"C:\Repo\My.App.Main.sln");

            Assert.NotNull(location);
            Assert.Equal(@"C:\Repo\.vs\My.App.Main\SolutionAlias.json", location.Path);
        }

        [Theory]
        [InlineData(null, @"C:\Repo\A.sln")]
        [InlineData("", @"C:\Repo\A.sln")]
        [InlineData(@"C:\Repo", null)]
        [InlineData(@"C:\Repo", "")]
        [InlineData(@"C:\Repo", @"C:\Repo\.sln")]
        public void CreateForSolution_MissingDirectoryOrSolutionName_ReturnsNull(string? directory, string? solutionFile)
        {
            Assert.Null(AliasFileLocation.CreateForSolution(directory, solutionFile));
        }

        [Fact]
        public void ReadFileContents_NoFile_ReturnsNull()
        {
            Assert.False(File.Exists(_aliasFileLocation.Path));
            Assert.Null(_aliasFileLocation.ReadFileContents());
        }

        [Fact]
        public void WriteContentsThenReadFileContents_RoundTrips()
        {
            _aliasFileLocation.WriteContents(new AliasFileContents { Alias = "MAIN", TextColourRgb = "#FFFFFF", BackgroundColourRgb = "#C0392B" });

            AliasFileContents? read = _aliasFileLocation.ReadFileContents();
            Assert.True(File.Exists(_aliasFileLocation.Path));
            Assert.NotNull(read);
            Assert.Equal("MAIN", read.Alias);
            Assert.Equal("#FFFFFF", read.TextColourRgb);
            Assert.Equal("#C0392B", read.BackgroundColourRgb);
        }

        [Fact]
        public void WriteContents_UsesTheDocumentedNamesWithoutByteOrderMark()
        {
            _aliasFileLocation.WriteContents(new AliasFileContents { Alias = "MAIN", BackgroundColourRgb = "#C0392B" });

            byte[] bytes = File.ReadAllBytes(_aliasFileLocation.Path);
            string json = Encoding.UTF8.GetString(bytes);

            Assert.NotEqual(0xEF, bytes[0]); // no byte order mark
            Assert.Contains("\"alias\":", json);
            Assert.Contains("\"textColourRgb\":", json);
            Assert.Contains("\"backgroundColourRgb\":", json);
        }

        [Fact]
        public void WriteContents_FileExists_ReplacesTheContents()
        {
            // The longer file goes first, so any of its text left behind would break the second one
            _aliasFileLocation.WriteContents(new AliasFileContents { Alias = "A MUCH LONGER ALIAS", TextColourRgb = "#FFFFFF", BackgroundColourRgb = "#C0392B" });
            _aliasFileLocation.WriteContents(new AliasFileContents { Alias = "MAIN" });

            AliasFileContents? read = _aliasFileLocation.ReadFileContents();
            Assert.NotNull(read);
            Assert.Equal("MAIN", read.Alias);
            Assert.Null(read.TextColourRgb);
            Assert.Null(read.BackgroundColourRgb);
        }

        [Fact]
        public void ReadFileContents_HandWrittenFileWithByteOrderMark_Works()
        {
            WriteAliasFileText("{ \"alias\": \"RELEASE\" }", withByteOrderMark: true);

            AliasFileContents? read = _aliasFileLocation.ReadFileContents();
            Assert.NotNull(read);
            Assert.Equal("RELEASE", read.Alias);
        }

        [Fact]
        public void ReadFileContents_ColourOnly_KeepsColoursWithNoAlias()
        {
            WriteAliasFileText("{ \"alias\": null, \"textColourRgb\": null, \"backgroundColourRgb\": \"#C0392B\" }");

            AliasFileContents? read = _aliasFileLocation.ReadFileContents();
            Assert.NotNull(read);
            Assert.False(read.HasAlias);
            Assert.Null(read.Alias);
            Assert.Null(read.TextColourRgb);
            Assert.Equal("#C0392B", read.BackgroundColourRgb);
        }

        [Fact]
        public void ReadFileContents_MissingFields_AreNull()
        {
            WriteAliasFileText("{ \"backgroundColourRgb\": \"#27AE60\" }");

            AliasFileContents? read = _aliasFileLocation.ReadFileContents();
            Assert.NotNull(read);
            Assert.Null(read.Alias);
            Assert.Null(read.TextColourRgb);
            Assert.Equal("#27AE60", read.BackgroundColourRgb);
        }

        [Fact]
        public void ReadFileContents_WhitespaceAliasWithColour_HasNoAlias()
        {
            WriteAliasFileText("{ \"alias\": \"   \", \"textColourRgb\": \"#FFFFFF\" }");

            AliasFileContents? read = _aliasFileLocation.ReadFileContents();
            Assert.NotNull(read);
            Assert.Null(read.Alias);
            Assert.Equal("#FFFFFF", read.TextColourRgb);
        }

        [Fact]
        public void ReadFileContents_HandWrittenAliasWithSurroundingSpaces_ReturnsTrimmedAlias()
        {
            WriteAliasFileText("{ \"alias\": \"  MAIN  \" }");

            AliasFileContents? read = _aliasFileLocation.ReadFileContents();
            Assert.NotNull(read);
            Assert.Equal("MAIN", read.Alias);
        }

        [Theory]
        [InlineData("{ \"alias\": null, \"textColourRgb\": null, \"backgroundColourRgb\": null }")]
        [InlineData("{ \"alias\": \"  \" }")]
        [InlineData("{ }")]
        public void ReadFileContents_NothingSet_ReturnsAllNull(string json)
        {
            WriteAliasFileText(json);

            AliasFileContents? read = _aliasFileLocation.ReadFileContents();
            Assert.NotNull(read);
            Assert.Null(read.Alias);
            Assert.Null(read.TextColourRgb);
            Assert.Null(read.BackgroundColourRgb);
        }

        [Fact]
        public void ReadFileContents_FileHoldingOnlyNull_ReturnsNull()
        {
            WriteAliasFileText("null");

            Assert.Null(_aliasFileLocation.ReadFileContents());
        }

        [Theory]
        [InlineData("")]
        [InlineData("{ \"alias\": ")]
        [InlineData("not json")]
        public void ReadFileContents_BrokenFile_ThrowsAnExceptionTheStoreCatches(string json)
        {
            // AliasFileStore catches the exceptions AliasFileLocation.IsExpectedFileException accepts and treats the file as missing.
            WriteAliasFileText(json);

            Exception? ex = Record.Exception(() => _aliasFileLocation.ReadFileContents());

            Assert.NotNull(ex);
            Assert.True(AliasFileLocation.IsExpectedFileException(ex), $"Exception AliasFileStore doesn't catch: {ex}");
        }

        [Fact]
        public void ReadFileContents_NumberAsAlias_ReadsTheNumberAsText()
        {
            // Not an error: DataContractJsonSerializer reads a JSON number into a string field
            WriteAliasFileText("{ \"alias\": 5 }");

            AliasFileContents? read = _aliasFileLocation.ReadFileContents();
            Assert.NotNull(read);
            Assert.Equal("5", read.Alias);
        }

        [Fact]
        public void ReadFileContents_ArrayInsteadOfObject_ReturnsAllNull()
        {
            // Not an error: DataContractJsonSerializer finds none of the fields in it
            WriteAliasFileText("[ 1, 2 ]");

            AliasFileContents? read = _aliasFileLocation.ReadFileContents();
            Assert.NotNull(read);
            Assert.Null(read.Alias);
            Assert.Null(read.TextColourRgb);
            Assert.Null(read.BackgroundColourRgb);
        }

        [Fact]
        public void DeleteIfExists_FileExists_RemovesFileAndReturnsTrue()
        {
            _aliasFileLocation.WriteContents(new AliasFileContents { Alias = "MAIN" });

            bool didFileExist = _aliasFileLocation.DeleteIfExists();

            Assert.True(didFileExist);
            Assert.False(File.Exists(_aliasFileLocation.Path));
            Assert.Null(_aliasFileLocation.ReadFileContents());
        }

        [Fact]
        public void DeleteIfExists_NoFile_ReturnsFalse()
        {
            bool didFileExist = _aliasFileLocation.DeleteIfExists();

            Assert.False(didFileExist);
            Assert.False(File.Exists(_aliasFileLocation.Path));
        }

        private void WriteAliasFileText(string json, bool withByteOrderMark = false)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_aliasFileLocation.Path));
            File.WriteAllText(_aliasFileLocation.Path, json, new UTF8Encoding(withByteOrderMark));
        }
    }
}
