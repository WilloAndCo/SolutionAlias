using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SolutionAlias.IntegrationTests.VisualStudio
{
    internal sealed class ProcessResult
    {
        public ProcessResult(int exitCode, string output)
        {
            ExitCode = exitCode;
            Output = output;
        }

        public int ExitCode { get; }
        public string Output { get; }
    }

    internal static class ProcessRunner
    {
        /// <summary>Runs a process to completion and returns its exit code and output; throws if it runs past the timeout.</summary>
        public static ProcessResult RunProcessAndCaptureOutput(string fileName, string arguments, TimeSpan timeout)
        {
            var startInfo = new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute = false, // .Net starts the process, which allows it to redirect output
                CreateNoWindow = true, // output is redirected so nothing to see
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                // collect all output in one string builder
                var allOutput = new StringBuilder();

                // add event handlers to capture output and error data. lock for thread safe append.
                process.OutputDataReceived += (_, e) => { if (e.Data != null) { lock (allOutput) { allOutput.AppendLine(e.Data); } } };
                process.ErrorDataReceived += (_, e) => { if (e.Data != null) { lock (allOutput) { allOutput.AppendLine(e.Data); } } };

                // start the process - throw on fail
                process.Start();

                // enable the callbacks
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // WaitForExit with parameter wait for process to exit
                bool processExited = process.WaitForExit((int)timeout.TotalMilliseconds);
                if (processExited == false)
                {
                    KillProcessIgnoringErrors(process);
                    throw new TimeoutException($"{Path.GetFileName(fileName)} {arguments} didn't finish within {timeout}.");
                }

                // WaitForExit without parameter waits for process to exit and the output to finish
                process.WaitForExit();

                // No more access to allOutput at this point
                return new ProcessResult(process.ExitCode, allOutput.ToString());
            }
        }

        private static void KillProcessIgnoringErrors(Process process)
        {
            try
            {
                process.Kill();
            }
            catch (InvalidOperationException)
            {
                // the process exited on its own between the timeout and the kill
            }
        }
    }
}
