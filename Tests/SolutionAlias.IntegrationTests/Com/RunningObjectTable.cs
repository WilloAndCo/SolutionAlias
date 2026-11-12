using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;

namespace SolutionAlias.IntegrationTests.Com
{
    /// <summary>
    /// Finds a running Visual Studio's DTE object. Each devenv.exe registers its DTE in Windows' Running Object Table.
    /// </summary>
    internal static class RunningObjectTable
    {
        /// <summary>Polls until <paramref name="devenv"/> has registered its DTE object; throws if it exits or <paramref name="timeout"/> passes first.</summary>
        public static EnvDTE80.DTE2 WaitForDteRegistration(Process devenv, TimeSpan timeout)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < timeout)
            {
                if (devenv.HasExited)
                {
                    throw new InvalidOperationException($"devenv.exe exited with {devenv.ExitCode} before its DTE object was available.");
                }

                EnvDTE80.DTE2? dte = FindDteForProcess(devenv.Id);
                if (dte != null)
                {
                    return dte;
                }

                Thread.Sleep(500);
            }

            throw new TimeoutException($"devenv.exe (process {devenv.Id}) didn't register its DTE object within {timeout}.");
        }

        /// <summary>The DTE object of the devenv.exe with this process ID; it isn't there until devenv has registered it.</summary>
        private static EnvDTE80.DTE2? FindDteForProcess(int processId)
        {
            // 0 is the required parameter for GetRunningObjectTable in the docs
            Marshal.ThrowExceptionForHR(GetRunningObjectTable(0, out IRunningObjectTable table));

            // A default needed as a parameter for GetDisplayName
            Marshal.ThrowExceptionForHR(CreateBindCtx(0, out IBindCtx defaultBindContext));

            // enumerator over every entry in the running object table, Visual Studio's DTE object is there once it has been registered by devenv
            // A moniker is a COM object that stores the name of another object and can bind to it
            table.EnumRunning(out IEnumMoniker monikers);

            // Visual Studio registers its DTE under a name with processID at the end, we use this to look it up
            string lookupSuffix = ":" + processId;

            // while loop processes 1 entry at a time, only needs array size 1
            var currentMoniker = new IMoniker[1];
            while (monikers.Next(1, currentMoniker, IntPtr.Zero) == 0)
            {
                // Turns Moniker into a string, e.g. !VisualStudio.DTE.17.0:12345
                currentMoniker[0].GetDisplayName(defaultBindContext, null, out string monikerName);

                if (monikerName.StartsWith("!VisualStudio.DTE.", StringComparison.Ordinal) && monikerName.EndsWith(lookupSuffix, StringComparison.Ordinal))
                {
                    table.GetObject(currentMoniker[0], out object dte);
                    // DTE2 is the newer version of the DTE interface; the same object implements both
                    return (EnvDTE80.DTE2)dte;
                }
            }

            return null;
        }

        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable table);

        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(int reserved, out IBindCtx bindContext);
    }
}
