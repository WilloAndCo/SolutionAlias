using System;
using System.Windows.Automation;
using SolutionAlias.IntegrationTests.Screen;
using SolutionAlias.IntegrationTests.VisualStudio;

namespace SolutionAlias.IntegrationTests.Tests
{
    internal static class AliasDialogUtil
    {
        public const string DialogName = "Solution Alias";
        public const string OptionsWindowName = "Options";

        public static AutomationElement WaitForDialogToOpen(VisualStudioSession vs)
        {
            return vs.ScreenElementsController.GetWindow(DialogName, Wait.WaitToHappenDuration);
        }

        public static void WaitForDialogToClose(VisualStudioSession vs)
        {
            Wait.PollUntilTrueOrThrow(() => vs.ScreenElementsController.CannotFindWindow(DialogName), Wait.WaitToHappenDuration, () => "the dialog to close");
        }

        /// <summary>Runs a test, and closes anything left open by the test. Otherwise Visual Studio may reject the calls to close it.</summary>
        public static void RunAndCloseLeftoverDialogs(VisualStudioSession vs, Action test)
        {
            try
            {
                test();
            }
            finally
            {
                try
                {
                    AutomationElement? dialog = vs.ScreenElementsController.FindWindow(DialogName);
                    if (dialog != null)
                    {
                        ScreenElementsController.CloseWindowThroughWindowPattern(dialog);
                    }

                    AutomationElement? options = vs.ScreenElementsController.FindWindow(OptionsWindowName);
                    if (options != null)
                    {
                        ScreenElementsController.CloseOptionsWindow(options);
                    }
                }
                catch (Exception ex) when (ex is ElementNotAvailableException || ex is InvalidOperationException)
                {
                }
            }
        }
    }
}
