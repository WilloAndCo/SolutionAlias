using System;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;
using SolutionAlias.Logging;

namespace SolutionAlias.Storage
{
    /// <summary>Reads, saves and deletes the alias file, and logs what happened.</summary>
    internal sealed class AliasFileStore
    {
        private readonly OutputLog _log;

        public AliasFileStore(OutputLog log)
        {
            _log = log;
        }

        public AliasFileContents? ReadAliasFile(AliasFileLocation location)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                AliasFileContents? aliasFileContents = location.ReadFileContents();
                _log.WriteLineIfEnabled(aliasFileContents != null
                    ? $"Loaded from {location.Path}: alias {AliasNameForLog(aliasFileContents)}."
                    : $"No alias file at {location.Path}; the label shows the path in the colours from the path.");
                return aliasFileContents;
            }
            catch (Exception ex) when (AliasFileLocation.IsExpectedFileException(ex))
            {
                _log.WriteLineIfEnabled($"Could not read {location.Path}: {ex.Message}");
                return null;
            }
        }

        public void SaveAliasFile(AliasFileLocation location, AliasFileContents contentsToSave)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                location.WriteContents(contentsToSave);
                _log.WriteLineIfEnabled($"Saved to {location.Path}: alias {AliasNameForLog(contentsToSave)}, text colour {contentsToSave.TextColourRgb}, background {contentsToSave.BackgroundColourRgb}.");
            }
            catch (Exception ex) when (AliasFileLocation.IsExpectedFileException(ex))
            {
                ShowCouldNotUpdateAliasFileError(location, ex);
            }
        }

        public void DeleteAliasFile(AliasFileLocation location)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                bool didAliasFileExist = location.DeleteIfExists();
                _log.WriteLineIfEnabled(didAliasFileExist
                    ? $"Alias cleared; deleted {location.Path}."
                    : $"Alias cleared; there was no {location.Path} to delete.");
            }
            catch (Exception ex) when (AliasFileLocation.IsExpectedFileException(ex))
            {
                ShowCouldNotUpdateAliasFileError(location, ex);
            }
        }

        private static void ShowCouldNotUpdateAliasFileError(AliasFileLocation location, Exception ex)
        {
            VS.MessageBox.ShowError($"{Vsix.Name}: could not update {location.Path}", ex.Message);
        }

        private static string AliasNameForLog(AliasFileContents contents) =>
            contents.HasAlias ? $"'{contents.Alias}'" : "(none; shows the path)";
    }
}
