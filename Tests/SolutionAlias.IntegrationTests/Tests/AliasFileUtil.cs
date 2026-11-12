using System.IO;
using System.Text;
using SolutionAlias.Storage;
using SolutionAlias.IntegrationTests.VisualStudio;

namespace SolutionAlias.IntegrationTests.Tests
{
    internal static class AliasFileUtil
    {
        /// <summary>Writes the alias file by hand. Must match the extension format.</summary>
        public static void WriteAliasFile(string solutionDirectory, string solutionName, AliasFileContents alias)
        {
            string path = GetAliasFilePath(solutionDirectory, solutionName: solutionName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(
                path,
                $"{{ \"alias\": {ToJsonStringOrNullLiteral(alias.Alias)}, \"textColourRgb\": {ToJsonStringOrNullLiteral(alias.TextColourRgb)}, \"backgroundColourRgb\": {ToJsonStringOrNullLiteral(alias.BackgroundColourRgb)} }}",
                new UTF8Encoding(false));
        }

        /// <summary>Where the extension keeps a solution's alias file. Must match the extension format.</summary>
        public static string GetAliasFilePath(string solutionDirectory, string solutionName) =>
            Path.Combine(solutionDirectory, ".vs", solutionName, "SolutionAlias.json");

        public static string GetAliasFilePath(VisualStudioSession vs) =>
            GetAliasFilePath(vs.SolutionDirectory, solutionName: Path.GetFileNameWithoutExtension(vs.SolutionFile));

        private static string ToJsonStringOrNullLiteral(string? value) => value == null ? "null" : $"\"{value}\"";
    }
}
