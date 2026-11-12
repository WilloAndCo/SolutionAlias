using System;
using System.Diagnostics;
using System.Threading;

namespace SolutionAlias.IntegrationTests.VisualStudio
{
    internal static class Wait
    {
        /// <summary>The extensions act when Visual Studio is idle, so checks poll for a while before failing.</summary>
        public static readonly TimeSpan WaitToHappenDuration = TimeSpan.FromSeconds(20);

        /// <summary>How long a "nothing should change" check watches for a change.</summary>
        public static readonly TimeSpan CheckNothingChangesDuration = TimeSpan.FromSeconds(3);

        private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

        public static void PollUntilTrueOrThrow(Func<bool> condition, TimeSpan timeout, Func<string> failedWaitingForDesc)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed <= timeout)
            {
                if (condition())
                {
                    return;
                }

                Thread.Sleep(PollInterval);
            }

            throw new TimeoutException($"Timed out after {timeout.TotalSeconds:0} s waiting for {failedWaitingForDesc()}.");
        }

        public static T ReadUntilChangedOrTimeout<T>(Func<T> read, T changeFrom, TimeSpan timeout)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < timeout)
            {
                T value = read();
                if (Equals(value, changeFrom) == false)
                {
                    return value;
                }

                Thread.Sleep(PollInterval);
            }

            return changeFrom;
        }

        public static T ReadUntilMatchOrTimeout<T>(Func<T> read, T changeTo, TimeSpan timeout)
        {
            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                T last = read();
                if (Equals(last, changeTo) || stopwatch.Elapsed > timeout)
                {
                    return last;
                }

                Thread.Sleep(PollInterval);
            }
        }
    }
}
