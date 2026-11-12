using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using SolutionAlias.IntegrationTests.Com;

namespace SolutionAlias.IntegrationTests.Screen
{
    /// <summary>Win32 MOUSEEVENTF_ values.</summary>
    internal static class MouseEventFlags
    {
        public const uint Move = 0x0001;
        public const uint LeftDown = 0x0002;
        public const uint LeftUp = 0x0004;
        public const uint RightDown = 0x0008;
        public const uint RightUp = 0x0010;
        public const uint VirtualDesk = 0x4000; // virtual desktop space covers all monitors
        public const uint Absolute = 0x8000;
    }

    /// <summary>Win32 SM_… values.</summary>
    internal static class SystemMetricsIndex
    {
        public const int VirtualScreenLeft = 76;
        public const int VirtualScreenTop = 77;
        public const int VirtualScreenWidth = 78;
        public const int VirtualScreenHeight = 79;
    }

    /// <summary>
    /// Moves and clicks the real mouse through Windows' SendInput, as a person would.
    /// </summary>
    /// <remarks>
    /// Required because Windows UI Automation has no mouse input
    /// Positions are in physical screen pixels. 
    /// Adds SendInput events to Windows’ system input queue
    /// </remarks>
    internal static class MouseController
    {
        /// <summary>Time between input events, so Visual Studio sees separate moves and button changes.</summary>
        private static readonly TimeSpan TimeBetweenInputs = TimeSpan.FromMilliseconds(50);

        public static void LeftClick(Point point) => MoveAndClickButton(point, MouseEventFlags.LeftDown, MouseEventFlags.LeftUp);
        public static void RightClick(Point point) => MoveAndClickButton(point, MouseEventFlags.RightDown, MouseEventFlags.RightUp);

        /// <summary>Moves to "from", presses the left button, moves by stepOffsetInPixels numSteps times, and releases it.</summary>
        public static void DragWithLeftButton(Point from, Vector stepOffsetInPixels, int numSteps)
        {
            MoveCursorToAndPause(from);
            SendMouseInputAndPause(MouseEventFlags.LeftDown);

            for (int step = 1; step <= numSteps; step++)
            {
                MoveCursorToAndPause(from + (stepOffsetInPixels * step));
            }

            SendMouseInputAndPause(MouseEventFlags.LeftUp);
        }

        /// <summary>Throws unless the top window at the point belongs to the process passed in.</summary>
        public static void ThrowIfPointIsNotOverProcessWindow(Point point, int processId, string descriptionOfPoint)
        {
            IntPtr window = WindowFromPoint(new POINT { X = (int)point.X, Y = (int)point.Y });
            GetWindowThreadProcessId(window, out int owner);
            if (owner != processId)
            {
                throw new InvalidOperationException(
                    $"Another window is on top of {descriptionOfPoint} at {point} (it belongs to process {owner}, Visual Studio is {processId}). Leave the screen alone while the UI tests run.");
            }
        }

        private static void MoveAndClickButton(Point point, uint down, uint up)
        {
            MoveCursorToAndPause(point);
            SendMouseInputAndPause(down);
            SendMouseInputAndPause(up);
        }

        private static void MoveCursorToAndPause(Point point)
        {
            // Metrics returns values in pixels
            int left = GetSystemMetrics(SystemMetricsIndex.VirtualScreenLeft);
            int top = GetSystemMetrics(SystemMetricsIndex.VirtualScreenTop);
            int width = GetSystemMetrics(SystemMetricsIndex.VirtualScreenWidth);
            int height = GetSystemMetrics(SystemMetricsIndex.VirtualScreenHeight);

            // SendInput expects values between 0-65535, so convert the pixels to this
            int x = (int)Math.Round((point.X - left) * 65535.0 / (width - 1));
            int y = (int)Math.Round((point.Y - top) * 65535.0 / (height - 1));

            SendMouseInputAndPause(MouseEventFlags.Move | MouseEventFlags.Absolute | MouseEventFlags.VirtualDesk, x, y);
        }

        /// <remarks> Only uses x and y positions if the MouseEventFlags.Move flag is set </remarks>
        private static void SendMouseInputAndPause(uint flags, int x = 0, int y = 0)
        {
            const uint InputMouse = 0; // win32 constant for mouse
            var input = new INPUT
            {
                Type = InputMouse,
                Mouse = new MOUSEINPUT { X = x, Y = y, Flags = flags },
            };

            if (SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>()) != 1)
            {
                throw new InvalidOperationException($"SendInput failed (error {Marshal.GetLastWin32Error()}).");
            }

            Thread.Sleep(TimeBetweenInputs);
        }

        #region Win32 structs

        // Sequential StructLayout is the C# default, added the attribute here because win32 calls expect it to be true

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT // must be 40 bytes
        {
            public uint Type;
            public MOUSEINPUT Mouse; // must be 32 bytes exactly to match what win32 expects
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int X;
            public int Y;
            public uint MouseData; // extra data for wheel and side-button events
            public uint Flags;
            public uint Time; // a timestamp in milliseconds for the event
            public IntPtr ExtraInfo; // custom extra data read by the reciever
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        #endregion // Win32 structs

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint count, INPUT[] inputs, int size);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int index);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out int processId);
    }
}
