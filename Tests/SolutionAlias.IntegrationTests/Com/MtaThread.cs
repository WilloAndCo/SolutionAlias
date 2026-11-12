using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace SolutionAlias.IntegrationTests.Com
{
    /// <summary>Runs code on a new MTA thread, with no message filter, for UI Automation calls.</summary>
    internal static class MtaThread
    {
        public static void RunOnMtaThread(Action work)
        {
            // nullable object as T to allow reusing the functionality in RunOnMtaThread<T>
            RunOnMtaThread<object?>(() =>
            {
                work();
                return null;
            });
        }

        public static T RunOnMtaThread<T>(Func<T> work)
        {
            T result = default!;
            ExceptionDispatchInfo? failure = null;
            var thread = new Thread(() =>
            {
                StaThread.MakeCurrentThreadPerMonitorDpiAware();
                try
                {
                    result = work();
                }
                catch (Exception ex)
                {
                    failure = ExceptionDispatchInfo.Capture(ex);
                }
            })
            {
                IsBackground = true,
                Name = "UI Automation",
            };
            thread.SetApartmentState(ApartmentState.MTA);
            thread.Start();
            thread.Join();
            failure?.Throw();
            return result;
        }
    }
}
