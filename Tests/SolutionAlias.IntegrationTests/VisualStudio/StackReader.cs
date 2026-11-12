using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;

namespace SolutionAlias.IntegrationTests.VisualStudio
{
    /// <summary>
    /// Reads Visual Studio's main thread stack using the command-line debugger (cdb.exe)
    /// Useful for diagnosing if and why the main thread is stuck    
    /// </summary>
    /// <remarks>
    /// Starts a new debugging cdb process, pauses the visual studio process, cdb reads the stack, then detaches and leaves it running.
    /// </remarks>
    internal static class StackReader
    {
        private static readonly TimeSpan DebuggerTimeout = TimeSpan.FromSeconds(60);

        public static string GetMainStackWithDebugger(int vsProcessId)
        {
            // lookup which command line debugger executable to use
            string? cdbExeFilepath = FindCdbExecutable();
            if (cdbExeFilepath == null)
            {
                return "(not read: cdb.exe from the Windows SDK's Debugging Tools for Windows isn't installed, or the SDK's folder isn't in the registry)";
            }

            // create temporary folders for cdb's commands, cdb takes paths to run
            string tempWorkingFolder = Path.Combine(Path.GetTempPath(), "SolutionAlias.IntegrationTests", "StackReader");

            // .net only needs symbols for files and line numbers, methods exist without it, so empty symbol directory is fine
            string emptySymbolDirectory = Path.Combine(tempWorkingFolder, "no-symbols");
            Directory.CreateDirectory(emptySymbolDirectory);

            // commands file needed to run commands, issues them one at a time top to bottom
            string commandsFilepath = Path.Combine(tempWorkingFolder, $"commands-{vsProcessId}.txt");

            // sos turns dot net stacks into readable text, it's an extension to cdb, included by default with .net framework
            const string LoadExtensionToReadDotNetStackCommand = ".loadby sos clr";

            // ~0 = thread 0, e = execute, !clrstack = print the .net stack of the current thread
            const string ReadMainThreadStackCommand = "~0e !clrstack";

            // qd = quit and detach, leaves the process running
            const string DetachAndExitCommand = "qd";

            // add commands in order
            File.WriteAllLines(commandsFilepath, new[] { LoadExtensionToReadDotNetStackCommand, ReadMainThreadStackCommand, DetachAndExitCommand });

            try
            {
                // -p is process id, -y is symbol path, -cf is commands file
                string processArguments = $" -p {vsProcessId} -y \"{emptySymbolDirectory}\" -cf \"{commandsFilepath}\"";

                ProcessResult result = ProcessRunner.RunProcessAndCaptureOutput(cdbExeFilepath, processArguments, DebuggerTimeout);
                return FindStackInDebuggerOutput(result.Output);
            }
            catch (Exception ex) when (ex is TimeoutException || ex is Win32Exception || ex is InvalidOperationException || ex is IOException)
            {
                return $"(not read: {ex.Message})";
            }
            finally
            {
                DeleteFolderIgnoringErrors(tempWorkingFolder);
            }
        }

        /// <summary> Extract the stack from the full debugger output, using known start and end strings. </summary>
        private static string FindStackInDebuggerOutput(string debuggerOutput)
        {
            const string StacksStartWithStr = "OS Thread Id:";
            const string StacksEndBeforeStr = "> qd";

            int startIdx = debuggerOutput.IndexOf(StacksStartWithStr, StringComparison.Ordinal);
            if (startIdx < 0)
            {
                return debuggerOutput;
            }

            int endIdx = debuggerOutput.IndexOf(StacksEndBeforeStr, startIdx, StringComparison.Ordinal);
            if (endIdx < 0)
            {
                return debuggerOutput.Substring(startIdx);
            }

            int lineBeforeIdx = debuggerOutput.LastIndexOf('\n', endIdx);
            return debuggerOutput.Substring(startIdx, lineBeforeIdx - startIdx).Trim();
        }

        private static string? FindCdbExecutable()
        {
            // Visual Studio's own scripts find the Windows SDK using this registry method
            string? windowsKitsFolder = ReadWindowsKitsFolderFromRegistry();
            if (windowsKitsFolder == null)
            {
                return null;
            }

            // select the correct version of cdb.exe for the OS
            string architecture = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "arm64" : "x64";
            string cdb = Path.Combine(windowsKitsFolder, "Debuggers", architecture, "cdb.exe");
            return File.Exists(cdb) ? cdb : null;
        }

        /// <summary>The Windows 10/11 SDK's install folder recorded in the registry</summary>
        private static string? ReadWindowsKitsFolderFromRegistry()
        {
            // Inside windows dev kits root should be KitsRoot10 (called that since Windows 10) with data to the Windows SDK folder
            using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)
                .OpenSubKey(@"SOFTWARE\Microsoft\Windows Kits\Installed Roots"))
            {
                return key?.GetValue("KitsRoot10") as string;
            }
        }

        private static void DeleteFolderIgnoringErrors(string folder)
        {
            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
            }
        }
    }
}
