using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SolutionAlias.TitleBar
{
    /// <summary>The title-bar elements around the solution name.</summary>
    /// <remarks>
    /// Layout in the supported VS versions:
    /// <code>
    /// VS 2022, classic title bar (SolutionInfoControl template in Microsoft.VisualStudio.Shell.UI.Internal):
    ///   Grid (columns Auto, *)
    ///   ├ Border "PART_AvailableSpaceMeasuringControl"
    ///   └ Border "TextBorder"                 the solution-name label; background MainWindowSolutionName*BackgroundBrushKey
    ///     └ StatusBarTextControl "PART_SolutionNameTextBlock"
    /// VS 2026, and VS 2022's refreshed UI (template in Microsoft.VisualStudio.Shell.Styles / Shell.UI.Internal 18.x):
    ///   Grid
    ///   ├ Border "PART_AvailableSpaceMeasuringControl"
    ///   └ Border "ControlBorder"              the solution-name label; rounded, with a border
    ///     └ TabItemTextControl "PART_SolutionNameTextBlock"
    /// </code>
    /// </remarks>
    internal sealed class TitleBarParts
    {
        public const string SolutionNamePartName = "PART_SolutionNameTextBlock";
        private const string TitleBarTypeName = "MainWindowTitleBar";

        private TitleBarParts(FrameworkElement solutionNameText, Border solutionNameLabel, FrameworkElement solutionNameGrid)
        {
            SolutionNameText = solutionNameText;
            SolutionNameLabel = solutionNameLabel;
            SolutionNameGrid = solutionNameGrid;
        }

        /// <summary>PART_SolutionNameTextBlock: the solution name text.</summary>
        public FrameworkElement SolutionNameText { get; }

        /// <summary>Visual Studio's bordered solution-name label around the text.</summary>
        public Border SolutionNameLabel { get; }

        /// <summary>The Grid that holds the solution-name label.</summary>
        public FrameworkElement SolutionNameGrid { get; }

        /// <summary>False once Visual Studio has removed these elements from the window or rebuilt the solution-name label.</summary>
        public bool IsStillConnected =>
            PresentationSource.FromVisual(SolutionNameText) != null
            && ReferenceEquals(VisualTreeHelper.GetParent(SolutionNameText), SolutionNameLabel)
            && ReferenceEquals(VisualTreeHelper.GetParent(SolutionNameLabel), SolutionNameGrid);

        public static TitleBarParts? FindInMainWindow(Window? mainWindow)
        {
            if (mainWindow == null)
            {
                return null;
            }

            DependencyObject? searchFrom = FindFirstDescendant(mainWindow, IsTitleBar, maxDepth: 12) ?? mainWindow;
            if (searchFrom == null)
            {
                return null;
            }

            FrameworkElement? findSolutionNameText = FindFirstDescendant(searchFrom, IsSolutionName, maxDepth: 40) as FrameworkElement;

            if (findSolutionNameText == null
                || (VisualTreeHelper.GetParent(findSolutionNameText) is Border solutionNameLabel) == false
                || (VisualTreeHelper.GetParent(solutionNameLabel) is FrameworkElement solutionNameGrid) == false)
            {
                return null;
            }

            return new TitleBarParts(findSolutionNameText, solutionNameLabel, solutionNameGrid);
        }

        private static bool IsTitleBar(DependencyObject element) =>
            element.GetType().Name == TitleBarTypeName;

        private static bool IsSolutionName(DependencyObject element) =>
            element is FrameworkElement frameworkElement && frameworkElement.Name == SolutionNamePartName;

        /// <remarks>Uses VisualTreeHelper because the text is not in the XAML of the main window, so window.FindName can't find it.</remarks>
        private static DependencyObject? FindFirstDescendant(DependencyObject root, Func<DependencyObject, bool> isMatch, int maxDepth)
        {
            // Stop at the depth limit. Must be Visual or Visual3D to use VisualTreeHelper
            if (maxDepth == 0 || !(root is Visual || root is Visual3D))
            {
                return null;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (isMatch(child))
                {
                    return child;
                }

                DependencyObject? descendant = FindFirstDescendant(child, isMatch, maxDepth - 1);
                if (descendant != null)
                {
                    return descendant;
                }
            }

            return null;
        }
    }
}
