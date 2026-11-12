using System;
using System.Runtime.InteropServices;

namespace SolutionAlias.IntegrationTests.Com
{
    [ComImport]
    [Guid("00000016-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IOleMessageFilter
    {
        [PreserveSig]
        int HandleInComingCall(int dwCallType, IntPtr hTaskCaller, int dwTickCount, IntPtr lpInterfaceInfo);

        [PreserveSig]
        int RetryRejectedCall(IntPtr hTaskCallee, int dwTickCount, int dwRejectType);

        [PreserveSig]
        int MessagePending(IntPtr hTaskCallee, int dwTickCount, int dwPendingType);
    }

    /// <summary>
    /// Retries DTE calls that Visual Studio rejects because it is busy, instead of failing them.
    /// </summary>
    /// <remarks>
    /// Registered with the StaThread, any COM messages will be handled by this filter while this is registered.
    /// </remarks>
    internal sealed class RetryCallsMessageFilter : IOleMessageFilter
    {
        /// <summary>How long to keep retrying a call before failing.</summary>
        private const int RetryForMs = 60_000;
        
        // constants from IMessageFilter
        private const int ServerCallIsHandled = 0;
        private const int ServerCallRetryLater = 2;
        private const int RetryImmediately = 99;
        private const int CancelCall = -1;
        private const int WaitDefaultProcessing = 2;

        public static void RegisterOnCurrentThread()
        {
            // Sets this Message Filter for any COM message on this thread until revoke
            Marshal.ThrowExceptionForHR(CoRegisterMessageFilter(new RetryCallsMessageFilter(), out _));
        }

        public static void RevokeOnCurrentThread()
        {
            // Sets this this thread's COM message filter back to the default
            CoRegisterMessageFilter(null, out _);
        }

        int IOleMessageFilter.HandleInComingCall(int dwCallType, IntPtr hTaskCaller, int dwTickCount, IntPtr lpInterfaceInfo)
        {
            // Nothing should be calling into these tests
            return ServerCallIsHandled;
        }            

        int IOleMessageFilter.RetryRejectedCall(IntPtr hTaskCallee, int dwTickCount, int dwRejectType)
        {
            if (dwRejectType == ServerCallRetryLater)
            {
                bool continue_retry = dwTickCount < RetryForMs;
                if (continue_retry)
                {
                    return RetryImmediately;
                }
            }

            return CancelCall;
        }

        int IOleMessageFilter.MessagePending(IntPtr hTaskCallee, int dwTickCount, int dwPendingType)
        {
            // called if a window message arrives on the test thread while it waits for Visual Studio to answer a COM call
            return WaitDefaultProcessing;
        }

        [DllImport("ole32.dll")]
        private static extern int CoRegisterMessageFilter(IOleMessageFilter? newFilter, out IOleMessageFilter? oldFilter);
    }
}
