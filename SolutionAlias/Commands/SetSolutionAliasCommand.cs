using System;
using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Shell;

namespace SolutionAlias.Commands
{
    /// <summary>"Set Solution Alias..." on the Solution node's context menu. Clicking the alias label does the same.</summary>
    [Command(PackageGuids.CommandSetString, PackageIds.SetSolutionAliasCommand)]
    internal sealed class SetSolutionAliasCommand : BaseCommand<SetSolutionAliasCommand>
    {
        protected override void BeforeQueryStatus(EventArgs e)
        {
            // Query required to run on UI thread because it checks the solution info
            ThreadHelper.ThrowIfNotOnUIThread();

            // The alias file lives next to the solution file, so the solution must have been saved.
            Command.Enabled = ((SolutionAliasPackage)Package).Controller?.IsValidSolutionOpen() == true;
        }

        protected override void Execute(object sender, EventArgs e)
        {
            // Execute required to run on UI thread because it shows a dialog
            ThreadHelper.ThrowIfNotOnUIThread();

            ((SolutionAliasPackage)Package).Controller?.ShowAliasDialogAndSaveResult();
        }
    }
}
