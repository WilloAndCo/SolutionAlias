using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security;
using System.Text;
using System.Xml;

namespace SolutionAlias.Storage
{
    /// <summary>
    /// Where a solution's alias file goes: .vs\SolutionName\SolutionAlias.json, next to the solution file.
    /// </summary>
    internal sealed class AliasFileLocation
    {
        public const string FileName = "SolutionAlias.json";

        private static readonly DataContractJsonSerializer Serialiser = new DataContractJsonSerializer(typeof(AliasFileContents));

        private AliasFileLocation(string path)
        {
            Path = path;
        }

        public static AliasFileLocation? CreateForSolution(string? solutionDirectory, string? solutionFile)
        {
            if (string.IsNullOrEmpty(solutionDirectory) || string.IsNullOrEmpty(solutionFile))
            {
                return null;
            }

            string solutionName = System.IO.Path.GetFileNameWithoutExtension(solutionFile);
            if (string.IsNullOrEmpty(solutionName))
            {
                return null;
            }

            return new AliasFileLocation(System.IO.Path.Combine(solutionDirectory, ".vs", solutionName, FileName));
        }

        public string Path { get; }

        public bool FileExists => File.Exists(Path);

        public AliasFileContents? ReadFileContents()
        {
            if (File.Exists(Path) == false)
            {
                return null;
            }
                        
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(Path))))
            {
                AliasFileContents? contents = (AliasFileContents?)Serialiser.ReadObject(stream);
                if (contents != null)
                {
                    // The file may be hand-written: trim the alias, and treat one that is only whitespace as no alias
                    contents.Alias = AliasFileContents.NormaliseAliasText(contents.Alias);
                    return contents;
                }
            }

            return null;
        }

        public void WriteContents(AliasFileContents contents)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            using (FileStream stream = File.Create(Path))
            using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(stream, new UTF8Encoding(false), ownsStream: false, indent: true))
            {
                Serialiser.WriteObject(writer, contents);
                writer.Flush();
            }
        }

        public bool DeleteIfExists()
        {
            if (File.Exists(Path) == false)
            {
                return false;
            }

            File.Delete(Path);
            return true;
        }

        /// <summary>True for the exceptions that reading or writing the file can throw.</summary>
        public static bool IsExpectedFileException(Exception ex) =>
            ex is IOException || ex is UnauthorizedAccessException || ex is SecurityException || ex is SerializationException;
    }
}
