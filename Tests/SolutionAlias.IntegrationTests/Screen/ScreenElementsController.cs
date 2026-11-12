using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using SolutionAlias.IntegrationTests.Com;
using SolutionAlias.IntegrationTests.VisualStudio;

namespace SolutionAlias.IntegrationTests.Screen
{
    /// <summary>
    /// Finds and interacts with Visual Studio's windows, buttons and menu items using Windows UI Automation
    /// </summary>
    /// <remarks>
    /// Every UI Automation call here runs on on it own MtaThread from ms guidelines - https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-threading.
    /// These methods are called from the test StaThread
    /// </remarks>
    internal sealed class ScreenElementsController
    {
        private readonly int _processId;

        public ScreenElementsController(int processId)
        {
            _processId = processId;
        }

        #region Finding windows

        public AutomationElement GetWindow(string name, TimeSpan retryTimeout)
        {
            AutomationElement? found = null;
            Wait.PollUntilTrueOrThrow(() => (found = FindWindow(name)) != null, retryTimeout,() => $"a window named '{name}'. Visual Studio's windows: {TopLevelWindowNamesToString()}");
            return found!;
        }

        /// <summary>A window of Visual Studio's named <paramref name="name"/>: a top-level window, or one inside the main window.</summary>
        public AutomationElement? FindWindow(string name) => ReadOnAutomationThread<AutomationElement>(() =>
        {
            var namedCondition = new PropertyCondition(AutomationElement.NameProperty, name);
            var isWindowCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);
            foreach (AutomationElement window in FindVisualStudioTopLevelWindows())
            {
                if (window.Current.Name == name)
                {
                    return window;
                }

                AutomationElement inner = window.FindFirst(TreeScope.Descendants, new AndCondition(namedCondition, isWindowCondition));
                if (inner != null)
                {
                    return inner;
                }
            }

            return null;
        });

        public bool CannotFindWindow(string name) => FindWindow(name) == null;

        #endregion // Finding windows

        #region Finding elements

        /// <summary>Get an element with this name that is on screen, anywhere in Visual Studio's windows and menus.</summary>
        public AutomationElement GetVisibleElement(string name, ControlType controlType, TimeSpan retryTimeout)
        {
            var nameControlCondition = new AndCondition(
                new PropertyCondition(AutomationElement.NameProperty, name),
                new PropertyCondition(AutomationElement.ControlTypeProperty, controlType));

            AutomationElement? found = null;
            string searchedElements = "none";

            Wait.PollUntilTrueOrThrow(
                () =>
                {
                    List<AutomationElement> matches = ReadOnAutomationThread(() => FindVisualStudioTopLevelWindows()
                        .SelectMany(window => window.FindAll(TreeScope.Descendants, nameControlCondition).Cast<AutomationElement>())
                        .ToList()) ?? new List<AutomationElement>();

                    searchedElements = string.Join("; ", matches.Select(ElementToString));
                    found = matches.FirstOrDefault(IsOnScreen);
                    return found != null;
                },
                retryTimeout,
                () => $"a {controlType.ProgrammaticName} named '{name}' on screen (seached: {searchedElements})");

            return found!;
        }

        public static AutomationElement GetDescendantByAutomationId(AutomationElement scope, string automationId, TimeSpan retryTimeout)
        {
            var condition = new PropertyCondition(AutomationElement.AutomationIdProperty, automationId);
            AutomationElement? found = null;
            Wait.PollUntilTrueOrThrow(() => (found = ReadOnAutomationThread(() => scope.FindFirst(TreeScope.Descendants, condition))) != null, retryTimeout,() => $"an element with automation ID '{automationId}'");
            return found!;
        }

        public static AutomationElement GetDescendantByNameAndType(AutomationElement scope, string name, ControlType controlType, TimeSpan retryTimeout)
        {
            var condition = new AndCondition(
                new PropertyCondition(AutomationElement.NameProperty, name),
                new PropertyCondition(AutomationElement.ControlTypeProperty, controlType));
            AutomationElement? found = null;
            Wait.PollUntilTrueOrThrow(() => (found = ReadOnAutomationThread(() => scope.FindFirst(TreeScope.Descendants, condition))) != null, retryTimeout,() => $"a {controlType.ProgrammaticName} named '{name}'");
            return found!;
        }

        /// <summary>True when an element with this name is anywhere under <paramref name="scope"/>.</summary>
        public static bool HasDescendantNamed(AutomationElement scope, string name) =>
            ReadOnAutomationThread(() => scope.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, name))) != null;

        #endregion // Finding elements

        #region Acting on elements

        /// <summary>Presses a button or menu item through its Invoke pattern, as a click or Enter would.</summary>
        public static void PressButtonOrMenuItem(AutomationElement element)
        {
            MtaThread.RunOnMtaThread(() => ((InvokePattern)element.GetCurrentPattern(InvokePattern.Pattern)).Invoke());
        }

        /// <summary>Replaces a text box's text, as typing would.</summary>
        public static void ReplaceTextBoxText(AutomationElement textBox, string text)
        {
            MtaThread.RunOnMtaThread(() => ((ValuePattern)textBox.GetCurrentPattern(ValuePattern.Pattern)).SetValue(text));
        }

        /// <summary>Checks a radio button through its SelectionItem pattern, as clicking it would.</summary>
        public static void SelectRadioButton(AutomationElement radioButton)
        {
            MtaThread.RunOnMtaThread(() => ((SelectionItemPattern)radioButton.GetCurrentPattern(SelectionItemPattern.Pattern)).Select());
        }

        #endregion // Acting on elements

        #region Acting on windows

        /// <summary>Closes a window through its Window pattern, as its close button would.</summary>
        public static void CloseWindowThroughWindowPattern(AutomationElement window)
        {
            MtaThread.RunOnMtaThread(() => ((WindowPattern)window.GetCurrentPattern(WindowPattern.Pattern)).Close());
        }

        /// <summary>
        /// Cancels a classic Windows dialog (window class "#32770"), as its Cancel button or Esc would, by sending it the
        /// standard Cancel command. Tools > Options Window pattern's Close doesn't close it, and its
        /// buttons have no Invoke pattern for UI Automation to use.
        /// </summary>
        public static void CloseOptionsWindow(AutomationElement dialog)
        {
            var handle = new IntPtr(MtaThread.RunOnMtaThread(() => dialog.Current.NativeWindowHandle));
            if (handle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"'{MtaThread.RunOnMtaThread(() => dialog.Current.Name)}' isn't a classic Windows dialog (it has no window handle).");
            }

            const int WM_COMMAND = 0x0111; // a button, menu item, or control was clicked
            const int IDCANCEL = 2; // the cancel button id
            PostMessage(handle, WM_COMMAND, new IntPtr(IDCANCEL), IntPtr.Zero);
        }

        public static void BringWindowToFront(IntPtr window)
        {
            SetForegroundWindow(window);

            TimeSpan WaitForRedraw = TimeSpan.FromMilliseconds(50);
            Thread.Sleep(WaitForRedraw);
        }

        #endregion // Acting on windows

        #region Screen positions

        public static Point GetScreenCentreInPixels(AutomationElement element)
        {
            Rect bounds = MtaThread.RunOnMtaThread(() => element.Current.BoundingRectangle);
            return new Point(bounds.X + (bounds.Width / 2), bounds.Y + (bounds.Height / 2));
        }

        public static Rect GetWindowScreenBoundsInPixels(IntPtr window) =>
            MtaThread.RunOnMtaThread(() => AutomationElement.FromHandle(window).Current.BoundingRectangle);

        #endregion // Screen positions

        #region Stringify

        public static string DescendantsToString(AutomationElement scope) =>
            ReadOnAutomationThread(() =>
            {
                AutomationElementCollection all = scope.FindAll(TreeScope.Descendants, System.Windows.Automation.Condition.TrueCondition);
                return string.Join(", ", all.Cast<AutomationElement>().Select(ElementToString));
            }) ?? "(no longer there)";

        public string TopLevelWindowNamesToString()
        {
            return ReadOnAutomationThread(() => string.Join(", ", FindVisualStudioTopLevelWindows().Select(window => $"'{window.Current.Name}'"))) ?? "(none)";
        }

        public string WindowAndDialogNamesToString()
        {
            try
            {
                return ReadOnAutomationThread(() =>
                {
                    var isWindowCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);
                    var names = new List<string>();
                    foreach (AutomationElement window in FindVisualStudioTopLevelWindows())
                    {
                        names.Add($"'{window.Current.Name}'");
                        foreach (AutomationElement inner in window.FindAll(TreeScope.Descendants, isWindowCondition))
                        {
                            names.Add($"  inside it: '{inner.Current.Name}'");
                        }
                    }

                    return names.Count == 0 ? "(no windows)" : string.Join("; ", names);
                }) ?? "(couldn't read windows: they closed while being read)";
            }
            catch (InvalidOperationException ex)
            {
                return $"(couldn't read windows: {ex.Message})";
            }
        }

        #endregion // Stringify

        private List<AutomationElement> FindVisualStudioTopLevelWindows()
        {
            // A Top Level window is a window with no parent. Multiple windows may belong to Visual Studio and have the same process id (main window, options, etc)
            return AutomationElement.RootElement
                  .FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.ProcessIdProperty, _processId))
                  .Cast<AutomationElement>()
                  .ToList();
        }

        private static bool IsOnScreen(AutomationElement element)
        {
            try
            {
                Rect bounds = element.Current.BoundingRectangle;
                return element.Current.IsOffscreen == false && bounds.IsEmpty == false && bounds.Width > 0 && bounds.Height > 0;
            }
            catch (ElementNotAvailableException)
            {
                return false;
            }
        }

        private static string ElementToString(AutomationElement element)
        {
            try
            {
                AutomationElement.AutomationElementInformation currentInfo = element.Current;
                return $"{currentInfo.ControlType.ProgrammaticName.Replace("ControlType.", string.Empty)} '{currentInfo.Name}' at {currentInfo.BoundingRectangle}{(currentInfo.IsOffscreen ? " (off screen)" : string.Empty)}";
            }
            catch (ElementNotAvailableException)
            {
                return "(gone)";
            }
        }

        /// <summary> Read returns null if not available (Elements that disappear while being read e.g. menu closed) </summary>
        private static T? ReadOnAutomationThread<T>(Func<T?> read) where T : class
        {
            return MtaThread.RunOnMtaThread<T?>(() =>
            {
                try
                {
                    return read();
                }
                catch (ElementNotAvailableException)
                {
                    return null;
                }
            });
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr window);
    }
}
