using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SolutionAlias.TitleBar
{
    /// <summary>
    /// Moves Visual Studio's main window when the user drags the alias label, same functionality as dragging the solution-name label.
    /// </summary>
    /// <remarks>
    /// The alias label is not inside an INonClientArea element, so Visual Studio's WM_NCHITTEST handler reports
    /// it as client area and Windows doesn't drag the window from it. Sending WM_NCLBUTTONDOWN with HTCAPTION
    /// hands the drag to Windows, including dragging a maximised window down to restore it.
    /// Window.DragMove isn't used because it does nothing while the window is maximised.
    /// </remarks>
    internal static class MainWindowDrag
    {
        /// <summary>Windows message for the left mouse button pressed in the window's frame.</summary>
        private const int WM_NCLBUTTONDOWN = 0x00A1;
        /// <summary>Hit-test value for the title bar.</summary>
        private const int HTCAPTION = 2;

        /// <summary>
        /// Starts Windows' own title-bar drag of Visual Studio's main window from the current cursor position.
        /// </summary>
        /// <remarks>
        /// Only called once per drag, after that the Window handles the move.
        /// </remarks>
        public static void StartMovingWindowWithMouse()
        {
            Window? mainWindow = Application.Current?.MainWindow;
            if (mainWindow == null)
            {
                return;
            }

            IntPtr handle = new WindowInteropHelper(mainWindow).Handle;
            if (handle == IntPtr.Zero || !GetCursorPos(out POINT cursor))
            {
                return;
            }

            SendMessage(handle, WM_NCLBUTTONDOWN, new IntPtr(HTCAPTION), MakeLParam(cursor.X, cursor.Y));
        }

        /// <summary>Packs screen coordinates into a message lParam: x in the low 16 bits, y in the high 16 bits.</summary>
        private static IntPtr MakeLParam(int x, int y) => new IntPtr((y << 16) | (x & 0xFFFF));

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        /// <summary>Reads the cursor position in screen pixels. Returns false if Windows can't give it.</summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT point);

        /// <summary>Sends a message to the window and waits until the window has handled it.</summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}
