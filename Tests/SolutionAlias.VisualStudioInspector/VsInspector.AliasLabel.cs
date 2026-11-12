using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SolutionAlias.VisualStudioInspector
{
    /// <summary>
    /// The extension's alias label read from the AliasAdorner over Visual Studio's solution-name text.
    /// </summary>
    public sealed partial class VsInspector
    {
        public bool IsAliasLabelDrawn() => RunOnVisualStudioUIThread(() =>
        {
            Border? label = FindAliasLabel();
            return label != null && label.IsVisible && label.Opacity > 0;
        });

        /// <summary>True when the alias label is attached to the title bar, even while it is hidden.</summary>
        public bool IsAliasLabelAttached() => RunOnVisualStudioUIThread(() => FindAliasLabel() != null);

        public string ReadAliasLabelText() => RunOnVisualStudioUIThread(() => (FindAliasLabel()?.Child as TextBlock)?.Text ?? string.Empty);

        /// <summary>The label's text colour as "#AARRGGBB", or empty when there is no label.</summary>
        public string ReadAliasLabelForeground() => RunOnVisualStudioUIThread(() => FormatBrushColour((FindAliasLabel()?.Child as TextBlock)?.Foreground));

        /// <summary>The label's background colour as "#AARRGGBB", or empty when there is no label.</summary>
        public string ReadAliasLabelBackground() => RunOnVisualStudioUIThread(() => FormatBrushColour(FindAliasLabel()?.Background));

        public string ReadAliasLabelToolTip() => RunOnVisualStudioUIThread(() => FindAliasLabel()?.ToolTip as string ?? string.Empty);

        /// <summary>
        /// The "Opacity (%)" the label is drawn at (0 to 100), or -1 when there is no label. Read from the AliasAdorner
        /// </summary>
        public int ReadAliasLabelOpacityPercent() => RunOnVisualStudioUIThread(() =>
        {
            Border? label = FindAliasLabel();
            return label != null && VisualTreeHelper.GetParent(label) is UIElement aliasAdorner ? (int)Math.Round(aliasAdorner.Opacity * 100) : -1;
        });

        /// <summary>Where the label is on screen as "left,top,width,height"; empty when it isn't drawn.</summary>
        public string ReadAliasLabelScreenBounds() => RunOnVisualStudioUIThread(() =>
        {
            Border? label = FindAliasLabel();
            if (label == null || label.IsVisible == false || label.Opacity <= 0 || PresentationSource.FromVisual(label) == null)
            {
                return string.Empty;
            }

            Point topLeft = label.PointToScreen(new Point(0, 0));
            Point bottomRight = label.PointToScreen(new Point(label.ActualWidth, label.ActualHeight));
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:0},{1:0},{2:0},{3:0}",
                topLeft.X,
                topLeft.Y,
                bottomRight.X - topLeft.X,
                bottomRight.Y - topLeft.Y);
        });

        /// <summary>The alias label's Border.</summary>
        /// <remarks>Lookup duplicated in TitleBarParts.</remarks>
        private static Border? FindAliasLabel()
        {
            Window? mainWindow = Application.Current?.MainWindow;
            
            FrameworkElement? solutionNameText = mainWindow != null ? FindDescendantByName(mainWindow, "PART_SolutionNameTextBlock") : null;
            if (solutionNameText == null)
            {
                return null;
            }

            if (VisualTreeHelper.GetParent(solutionNameText) is not UIElement solutionNameBorder)
            {
                return null;
            }

            Adorner[]? adornersAttached = AdornerLayer.GetAdornerLayer(solutionNameBorder)?.GetAdorners(solutionNameBorder);
            Adorner? lookupSolutionAliasAdorner = adornersAttached?.FirstOrDefault(adorner => adorner.GetType().FullName == "SolutionAlias.TitleBar.AliasAdorner");
            if (lookupSolutionAliasAdorner == null)
            {
                return null;
            }

            // Alias Adorner only has one child, that's the label border
            return VisualTreeHelper.GetChildrenCount(lookupSolutionAliasAdorner) > 0 ? VisualTreeHelper.GetChild(lookupSolutionAliasAdorner, 0) as Border : null;
        }
    }
}
