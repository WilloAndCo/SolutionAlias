using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;

namespace SolutionAlias.VisualStudioInspector
{
    /// <summary>
    /// What the integration tests call through DTE.GetObject("SolutionAlias.VisualStudioInspector"). Every method runs
    /// on Visual Studio's UI thread and returns plain strings, numbers or booleans due to COM requirements.
    /// </summary>
    [ComVisible(true)] // ComVisible is true by default, setting it here to be explicit that we'll use it
    [ClassInterface(ClassInterfaceType.AutoDispatch)] // Public methods can only be called through COM's IDispatch by name (DTE.GetObject)
    public sealed partial class VsInspector
    {
        public const string AutomationName = "SolutionAlias.VisualStudioInspector";

        private static T RunOnVisualStudioUIThread<T>(Func<T> read)
        {
            // Run blocks until the lambda task is finished
            return ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                // calls read() once the switch to the Main/UI thread is complete
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                return read();
            });
        }

        private static string FormatBrushColour(Brush? brush)
        {
            return (brush is SolidColorBrush solid) ? solid.Color.ToString() : string.Empty;
        }

        /// <summary>The first element under <paramref name="parent"/> with this x:Name, searching depth first.</summary>
        /// <remarks>Duplicates TitleBarParts.FindFirstDescendant, which this project can't reference.</remarks>
        private static FrameworkElement? FindDescendantByName(DependencyObject parent, string name)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is FrameworkElement element && element.Name == name)
                {
                    return element;
                }

                FrameworkElement? found = FindDescendantByName(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
