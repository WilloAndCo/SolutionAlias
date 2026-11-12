using SolutionAlias.IntegrationTests.Com;
using SolutionAlias.IntegrationTests.VisualStudio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;

namespace SolutionAlias.IntegrationTests.Run
{
    /// <summary>
    /// All tests use this to run their code inside Visual Studio, on a copy of an existing sample solution
    /// All failures are logged in the results folder, the test throws at the end with these failures
    /// </summary>
    internal static class VisualStudioTestRun
    {
        /// <summary>The running tests output for Test Explorer. Use to report warnings and errors during the test run.</summary>
        public static ITestOutputHelper? CurrentTestOutput { get; set; }

        /// <summary>Runs a test inside a running Visual Studio instance.</summary>
        public static void RunTestInVisualStudio(VisualStudioTestSetup setup, Action<VisualStudioSession> test, [CallerMemberName] string testName = "")
        {
            // create a place for the test results
            string resultsFolder = TestResultsFolder.CreateResultsFolder(testName, setup.VisualStudioVersion);
            string logFile = TestResultsFolder.GetActivityLogFile(resultsFolder);

            // prepare visual studio settings for the run
            VisualStudioVersionInfo versionInfo = VisualStudioVersionFinder.FindInstallationForMajorVersion(setup.VisualStudioVersion);
            IntegrationTestsSettingsFolder.PrepareSettingsForVersion(versionInfo);

            // create a copy of the solution to run the test on
            string solutionDirectory = SampleSolutionCopy.CreateTemporaryCopyOfSolution(setup.SampleSolutionToCopy);
            
            // Each step that finds a problem adds it here instead of throwing, so the one failure at the end reports
            // all of them.
            var failureMessages = new List<string>();
            try
            {
                // find the solution file for the copy
                string solutionFile = Directory.GetFiles(solutionDirectory, "*.sln").Single();

                // apply any changes to the copy before running
                setup.PrepareCopy?.Invoke(solutionDirectory);

                // run the test in Visual Studio
                StartVisualStudioAndRunTest(versionInfo, solutionDirectory, solutionFile, resultsFolder, logFile, test, failureMessages);
            }
            catch (Exception ex)
            {
                // Steps that throw instead of adding to the list: PrepareCopy, starting devenv, waiting for DTE.
                failureMessages.Add(ex.ToString());
            }

            try
            {
                SampleSolutionCopy.DeleteTemporaryCopyOfSolution(solutionDirectory);
            }
            catch (Exception deleteFailure)
            {
                failureMessages.Add(deleteFailure.Message);
            }

            if (failureMessages.Count > 0)
            {
                string failureMessage = string.Join(Environment.NewLine + Environment.NewLine, failureMessages);
                TestResultsFolder.LogFailure(resultsFolder, failureMessage);
                throw new InvalidOperationException(failureMessage);                
            }
        }

        private static void StartVisualStudioAndRunTest(VisualStudioVersionInfo versionInfo, string solutionDirectory, string solutionFile, string resultsFolder, string logFile, Action<VisualStudioSession> test, List<string> failureMessages)
        {
            // start the Visual Studio process with the solution
            // rootsuffix sets which Settings folder to use
            // log enables the ActivityLog and writes it to logFile
            Process devenv = Process.Start(new ProcessStartInfo(
                versionInfo.DevenvPath,
                $"\"{solutionFile}\" /rootsuffix {IntegrationTestsSettingsFolder.IntegrationTestsRootSuffix} /log \"{logFile}\"")
            {
                UseShellExecute = false, // So Process.Start always returns this process
            });

            try
            {
                StaThread.RunOnStaThreadWithMessageFilter(() =>
                {
                    // attach dte to Visual Studio
                    EnvDTE80.DTE2 dte = RunningObjectTable.WaitForDteRegistration(devenv, TimeSpan.FromMinutes(3));

                    // get the session info
                    var session = new VisualStudioSession(versionInfo, dte, devenv.Id, solutionDirectory, solutionFile);
                    try
                    {
                        // load and reset the extensions, then run the test
                        session.WaitForSolutionAndLoadExtension();
                        session.ResetExtensionOptions();
                        test(session);
                    }
                    catch (Exception ex)
                    {
                        // The Output pane is read now: it's gone once Visual Studio closes.
                        failureMessages.Add($"{ex}{Environment.NewLine}{session.ReadExtensionOutputPaneForFailureMessage()}");
                    }

                    try
                    {
                        VisualStudioCloser.CloseOrKillAfterTimeout(devenv, dte, versionInfo, resultsFolder);
                    }
                    catch (TimeoutException closeFailure)
                    {
                        failureMessages.Add(closeFailure.Message);
                    }
                });
            }
            finally
            {
                if (devenv.HasExited == false)
                {
                    devenv.Kill();
                }

                devenv.Dispose();
            }

            AddActivityLogErrors(failureMessages, logFile);
        }

        private static void AddActivityLogErrors(List<string> failureMessages, string logFile)
        {
            try
            {
                IReadOnlyList<string> errors = ActivityLog.AllLoggedExtensionErrors(logFile);
                if (errors.Count > 0)
                {
                    failureMessages.Add($"ActivityLog has errors that mention the extension ({logFile}):{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");                    
                }
            }
            catch (Exception ex) when (ex is TimeoutException || ex is InvalidOperationException)
            {
                // For example a log Visual Studio never finished writing because it was killed.
                failureMessages.Add($"Couldn't read the ActivityLog ({logFile}): {ex.Message}");
            }
        }
    }
}
