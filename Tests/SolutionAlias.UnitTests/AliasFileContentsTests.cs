using SolutionAlias.Storage;
using Xunit;

namespace SolutionAlias.UnitTests
{
    /// <summary>The alias and colours stored in the alias file: <see cref="AliasFileContents"/>.</summary>
    public sealed class AliasFileContentsTests
    {
        [Theory]
        [InlineData("  MAIN  ", "MAIN")]
        [InlineData("\tMAIN ", "MAIN")]
        [InlineData("MAIN", "MAIN")]
        public void NormaliseAliasText_SurroundingWhitespace_ReturnsTrimmed(string aliasText, string expected)
        {
            Assert.Equal(expected, AliasFileContents.NormaliseAliasText(aliasText));
        }

        [Fact]
        public void NormaliseAliasText_SpaceInTheMiddle_IsKept()
        {
            Assert.Equal("My App", AliasFileContents.NormaliseAliasText(" My App "));
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t ")]
        public void NormaliseAliasText_OnlyWhitespace_ReturnsNull(string aliasText)
        {
            Assert.Null(AliasFileContents.NormaliseAliasText(aliasText));
        }

        [Fact]
        public void NormaliseAliasText_Null_ReturnsNull()
        {
            Assert.Null(AliasFileContents.NormaliseAliasText(null));
        }
    }
}
