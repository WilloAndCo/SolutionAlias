using System.Windows.Media;
using SolutionAlias.Label;
using SolutionAlias.Options;
using SolutionAlias.Storage;
using Xunit;

namespace SolutionAlias.UnitTests
{
    /// <summary>What the title-bar label and the dialog's preview show: <see cref="LabelAppearanceUtil"/>.</summary>
    public sealed class LabelAppearanceUtilTests
    {
        private const string SolutionFile = @"C:\Code\MyApp\MyApp.sln";
        private static readonly Color SolutionFilePathColour = LabelColours.CalculateBackgroundColourFromPath(SolutionFile);        
        private static readonly Color ThemeTextColour = Colors.White;
        private static readonly Color ThemeBackgroundColour = Colors.DimGray;

        [Fact]
        public void BuildAppearance_NoAlias_ShowsPath()
        {
            LabelAppearance appearance = BuildAppearance(SolutionFile, aliasFileContents: null, alwaysUseThemeColour: false, numFoldersToShow: GeneralOptions.DefaultNumFoldersToShow);

            Assert.Equal(SolutionFile, appearance.Name);
            Assert.StartsWith(SolutionFile, appearance.ToolTip);
            Assert.Contains("Click to set an alias or colours", appearance.ToolTip);
        }

        [Theory]
        [InlineData(@"C:\Code\MyApp\MyApp.sln", 0, "MyApp.sln")]
        [InlineData(@"C:\Code\MyApp\MyApp.sln", -1, "MyApp.sln")]
        [InlineData(@"C:\Code\MyApp\MyApp.sln", 1, @"...\MyApp\MyApp.sln")]
        [InlineData(@"C:\Code\MyApp\MyApp.sln", 2, @"C:\Code\MyApp\MyApp.sln")]
        [InlineData(@"C:\Code\MyApp\MyApp.sln", 6, @"C:\Code\MyApp\MyApp.sln")]
        [InlineData(@"C:\a\b\c\d\e\f\g\h\X.sln", 6, @"...\c\d\e\f\g\h\X.sln")]
        [InlineData(@"E:\X.sln", 1, @"E:\X.sln")]
        [InlineData(@"E:\X.sln", 0, "X.sln")]
        [InlineData(@"\\server\share\a\b\X.sln", 1, @"...\b\X.sln")]
        [InlineData(@"\\server\share\a\X.sln", 1, @"\\server\share\a\X.sln")]
        public void ShortenPathToLastFolders_KeepsTheLastFolders(string solutionFile, int numFoldersToShow, string expected)
        {
            Assert.Equal(expected, LabelAppearanceUtil.ShortenPathToLastFolders(solutionFile, numFoldersToShow));
        }

        [Fact]
        public void ShortenPathToLastFolders_InvalidPathCharacters_ReturnsFullPath()
        {
            // .NET Framework's Path methods throw on < and |. Shortened, this would be "...\b\<bad>|.sln".
            const string invalid = @"C:\a\b\<bad>|.sln";
            Assert.Equal(invalid, LabelAppearanceUtil.ShortenPathToLastFolders(invalid, 1));
        }

        [Fact]
        public void BuildAppearance_NoAlias_ShowsShortenedPathAndFullPathInToolTip()
        {            
            const string longFolderChain = @"C:\Folder1\Folder2\Folder3\Folder4\Folder5\Folder6\Folder7\MyApp.sln";
            Color pathColour = LabelColours.CalculateBackgroundColourFromPath(longFolderChain);

            LabelAppearance appearance = BuildAppearance(longFolderChain, aliasFileContents: null, alwaysUseThemeColour: false, numFoldersToShow: GeneralOptions.DefaultNumFoldersToShow);

            Assert.Equal(@"...\Folder3\Folder4\Folder5\Folder6\Folder7\MyApp.sln", appearance.Name);
            Assert.StartsWith(longFolderChain, appearance.ToolTip);
            Assert.Equal(pathColour, appearance.BackgroundColour);
        }

        [Fact]
        public void BuildAppearance_AliasWithNoFoldersToShow_ShowsTheAlias()
        {
            LabelAppearance appearance = BuildAppearance(SolutionFile, new AliasFileContents { Alias = "MAIN" }, alwaysUseThemeColour: false, numFoldersToShow: 0);

            Assert.Equal("MAIN", appearance.Name);
        }

        [Fact]
        public void BuildAppearance_Alias_ShowsAliasOnly()
        {
            LabelAppearance appearance = BuildAppearance(SolutionFile, new AliasFileContents { Alias = "MAIN" }, alwaysUseThemeColour: false, numFoldersToShow: GeneralOptions.DefaultNumFoldersToShow);

            Assert.Equal("MAIN", appearance.Name);
            Assert.StartsWith(SolutionFile, appearance.ToolTip);
            Assert.Contains("Click to change the alias or its colours", appearance.ToolTip);
        }

        [Fact]
        public void BuildAppearance_ColourOnly_ShowsPathInChosenColour()
        {
            var alias = new AliasFileContents { BackgroundColourRgb = "#C0392B" };

            LabelAppearance appearance = BuildAppearance(SolutionFile, alias, alwaysUseThemeColour: false, numFoldersToShow: GeneralOptions.DefaultNumFoldersToShow);

            Assert.Equal(SolutionFile, appearance.Name);
            Assert.Equal(Color.FromRgb(0xC0, 0x39, 0x2B), appearance.BackgroundColour);
            Assert.Equal(Colors.Black, appearance.TextColour);
        }

        [Fact]
        public void BuildAppearance_NoFile_ColoursComeFromThePath()
        {
            LabelAppearance appearance = BuildAppearance(SolutionFile, aliasFileContents: null, alwaysUseThemeColour: false, numFoldersToShow: GeneralOptions.DefaultNumFoldersToShow);

            Assert.Equal(SolutionFilePathColour, appearance.BackgroundColour);
            Assert.Equal(LabelColours.CalculateTextColourFromPath(SolutionFile), appearance.TextColour);
            Assert.False(appearance.IsFollowingVisualStudioColours);
        }

        [Fact]
        public void BuildAppearance_DarkChosenBackground_KeepsTheTextColourFromThePath()
        {           
            var alias = new AliasFileContents { BackgroundColourRgb = "#000080" };
            LabelAppearance appearance = BuildAppearance(SolutionFile, alias, alwaysUseThemeColour: false, numFoldersToShow: GeneralOptions.DefaultNumFoldersToShow);

            Assert.Equal(Colors.Navy, appearance.BackgroundColour);
            Assert.Equal(Colors.Black, appearance.TextColour);
        }

        [Fact]
        public void BuildAppearance_AlwaysUseThemeColour_IgnoresChosenColours()
        {
            var alias = new AliasFileContents { Alias = "MAIN", TextColourRgb = "#FFD54F", BackgroundColourRgb = "#C0392B" };
            LabelAppearance appearance = BuildAppearance(SolutionFile, alias, alwaysUseThemeColour: true, numFoldersToShow: GeneralOptions.DefaultNumFoldersToShow);

            Assert.Equal("MAIN", appearance.Name);
            Assert.Equal(ThemeBackgroundColour, appearance.BackgroundColour);
            Assert.Equal(ThemeTextColour, appearance.TextColour);
            Assert.True(appearance.IsFollowingVisualStudioColours);
        }

        [Fact]
        public void BuildAppearance_AlwaysUseThemeColour_ToolTipDoesNotOfferColours()
        {
            LabelAppearance noAlias = BuildAppearance(SolutionFile, aliasFileContents: null, alwaysUseThemeColour: true, numFoldersToShow: GeneralOptions.DefaultNumFoldersToShow);
            LabelAppearance withAlias = BuildAppearance(SolutionFile, new AliasFileContents { Alias = "MAIN" }, alwaysUseThemeColour: true, numFoldersToShow: GeneralOptions.DefaultNumFoldersToShow);

            Assert.Contains("Click to set an alias, or drag", noAlias.ToolTip);
            Assert.Contains("Click to change the alias, or drag", withAlias.ToolTip);
        }

        [Fact]
        public void BuildAppearance_ChosenTextOnly_KeepsPathBackground()
        {
            var alias = new AliasFileContents { Alias = "MAIN", TextColourRgb = "#FFD54F" };
            LabelAppearance appearance = BuildAppearance(SolutionFile, alias, alwaysUseThemeColour: false, numFoldersToShow: GeneralOptions.DefaultNumFoldersToShow);

            Assert.Equal(Color.FromRgb(0xFF, 0xD5, 0x4F), appearance.TextColour);
            Assert.Equal(SolutionFilePathColour, appearance.BackgroundColour);
        }

        [Fact]
        public void BuildAppearance_InvalidBackgroundColourInFile_ComesFromThePath()
        {
            var alias = new AliasFileContents { Alias = "MAIN", BackgroundColourRgb = "not a colour" };
            LabelAppearance appearance = BuildAppearance(SolutionFile, alias, alwaysUseThemeColour: false, numFoldersToShow: GeneralOptions.DefaultNumFoldersToShow);

            Assert.Equal(SolutionFilePathColour, appearance.BackgroundColour);
        }

        [Fact]
        public void BuildAppearance_InvalidTextColourInFile_ComesFromThePath()
        {            var alias = new AliasFileContents { Alias = "MAIN", TextColourRgb = "not a colour" };

            LabelAppearance appearance = BuildAppearance(SolutionFile, alias, alwaysUseThemeColour: false, numFoldersToShow: GeneralOptions.DefaultNumFoldersToShow);

            Assert.Equal(LabelColours.CalculateTextColourFromPath(SolutionFile), appearance.TextColour);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void BuildAppearance_OpacityPercent_SetsTheOpacity(bool alwaysUseThemeColour)
        {
            LabelAppearance appearance = BuildAppearance(SolutionFile, aliasFileContents: null, alwaysUseThemeColour, numFoldersToShow: GeneralOptions.DefaultNumFoldersToShow, opacityPercent: 40);
            Assert.Equal(0.4, appearance.Opacity, 3);
        }

        private static LabelAppearance BuildAppearance(string solutionFile, AliasFileContents? aliasFileContents, bool alwaysUseThemeColour, int numFoldersToShow, int opacityPercent = GeneralOptions.DefaultOpacityPercent) =>
            LabelAppearanceUtil.BuildAppearance(solutionFile, aliasFileContents, numFoldersToShow, alwaysUseThemeColour, opacityPercent: opacityPercent, themeTextColour: ThemeTextColour, themeBackgroundColour: ThemeBackgroundColour);
    }
}
