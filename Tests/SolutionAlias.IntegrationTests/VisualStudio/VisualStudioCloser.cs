using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using SolutionAlias.IntegrationTests.Run;
using SolutionAlias.IntegrationTests.Screen;

namespace SolutionAlias.IntegrationTests.VisualStudio
{
    internal struct CloseHangResult
    {
        public CloseHangResult(string windowsWhenStuck, string stackWhenStuck)
        {
            WindowsWhenStuck = windowsWhenStuck;
            StackWhenStuck = stackWhenStuck;
        }

        public string WindowsWhenStuck { get; }
        public string StackWhenStuck { get; }
    }

    /// <summary>Closes Visual Studio at the end of each launch, and kills it if it doesn't close in time.</summary>
    internal static class VisualStudioCloser
    {
        // Known Visual Studio hangs on close that aren't caused by the extension
        private static readonly IReadOnlyDictionary<string, string> WarningHangs = new Dictionary<string, string>
        {
            // Visual Studio 2022's built-in GitHub Copilot: Copilot's background sometimes never finishes on shutdown
            ["GitHub Copilot shutdown"] = "Microsoft.VisualStudio.Copilot.Common.CopilotJoinableTaskFactoryWrapper.Dispose",
        };

        private static readonly TimeSpan CloseTimeLimit = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Closes Visual Studio. If it hasn't exited in a set time, it is killed.
        /// Known hangs are reported as warnings, all others are reported as test failures.
        /// </summary>
        public static void CloseOrKillAfterTimeout(Process devenv, EnvDTE80.DTE2 dte, VisualStudioVersionInfo versionInfo, string resultsFolder)
        {
            var screenElements = new ScreenElementsController(devenv.Id);
            CloseHangResult? hangResult = null;

            // Timer callback fires if the using block does not exit in time
            using (var hangTimer = new Timer(
                _ =>
                {
                    // runs on another thread, so can query the hanging thread
                    hangResult = new CloseHangResult(
                        windowsWhenStuck: screenElements.WindowAndDialogNamesToString(),
                        stackWhenStuck: StackReader.GetMainStackWithDebugger(devenv.Id));
                    KillIgnoringErrors(devenv);
                },
                null,
                CloseTimeLimit,
                Timeout.InfiniteTimeSpan))
            {
                try
                {
                    // Don't save any changes to the solution
                    dte.Solution.Close(SaveFirst: false);
                    dte.Quit();
                }
                catch (COMException)
                {
                    // Already closed
                }

                // Wait for exist, timer catches hangs
                devenv.WaitForExit();

                // Synced state across threads to check that the callback isn't mid-diagnosis when the process finially exists             
                using (var sharedHasCallbackFinished = new ManualResetEvent(false))
                {
                    hangTimer.Dispose(sharedHasCallbackFinished); // store reference inside the timer to change when callback is finished
                    sharedHasCallbackFinished.WaitOne(); // blocks until finished is true
                }
            }

            if (hangResult == null)
            {
                // no hang happened
                return;
            }

            string? knownHangName = CheckForKnownHangName(hangResult.Value.StackWhenStuck);
            if (knownHangName != null)
            {
                WriteWarningToOutputAndResults(
                    resultsFolder: resultsFolder,
                    $"Visual Studio {versionInfo.Version} didn't close within {CloseTimeLimit.TotalSeconds:0} s and was killed: it was stuck in a known Visual Studio hang "
                    + $"that isn't caused by the extension ({knownHangName}). The test's own checks passed or failed as reported.");
                return;
            }

            throw new TimeoutException(
                $"Visual Studio didn't close within {CloseTimeLimit.TotalSeconds:0} s and was killed. Windows it was showing: {hangResult.Value.WindowsWhenStuck}{Environment.NewLine}"
                + $"Main thread: {hangResult.Value.StackWhenStuck}");
        }

        private static string? CheckForKnownHangName(string hangCallStack)
        {
            foreach (KeyValuePair<string, string> warningHang in WarningHangs)
            {
                if (hangCallStack.Contains(warningHang.Value))
                {
                    return warningHang.Key;
                }
            }

            return null;
        }

        private static void WriteWarningToOutputAndResults(string resultsFolder, string message)
        {
            VisualStudioTestRun.CurrentTestOutput?.WriteLine("WARNING: " + message);
            TestResultsFolder.LogWarning(resultsFolder, message);
        }

        private static void KillIgnoringErrors(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (InvalidOperationException)
            {
                // already exited
            }
            catch (Win32Exception)
            {
                // couldn't be killed / already ending
            }
        }
    }
}
