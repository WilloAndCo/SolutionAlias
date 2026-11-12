using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;

namespace SolutionAlias.TitleBar
{
    /// <summary>How the Solution Name label is displayed in the title bar.</summary>
    internal sealed class SolutionLabelAppearance
    {
        public Color TextColour { get; private set; } = Colors.White;
        public Color BackgroundColour { get; private set; } = Colors.DimGray;
        public Brush BorderBrush { get; private set; } = Brushes.Transparent;
        public Thickness BorderThickness { get; private set; }
        public CornerRadius CornerRadius { get; private set; }
        public Thickness Padding { get; private set; } = new Thickness(8, 4, 8, 4);
        public FontFamily FontFamily { get; private set; } = SystemFonts.MessageFontFamily;
        public double FontSize { get; private set; } = SystemFonts.MessageFontSize;
        public FontWeight FontWeight { get; private set; } = FontWeights.SemiBold;

        public static SolutionLabelAppearance CreateFromTitleBarOrTheme()
        {
            var appearance = new SolutionLabelAppearance();

            // default is theme colours
            appearance.BackgroundColour = ReadSingleColourOrFallback(FindThemeBrush(EnvironmentColors.MainWindowSolutionNameActiveBackgroundBrushKey), appearance.BackgroundColour);
            appearance.TextColour = ReadSingleColourOrFallback(FindThemeBrush(EnvironmentColors.MainWindowSolutionNameActiveTextBrushKey), appearance.TextColour);

            // if the title bar is found, use that
            TitleBarParts? titleBarParts = TitleBarParts.FindInMainWindow(Application.Current?.MainWindow);
            if (titleBarParts != null)
            {
                Border solutionNameLabel = titleBarParts.SolutionNameLabel;
                appearance.BackgroundColour = ReadSingleColourOrFallback(solutionNameLabel.Background, appearance.BackgroundColour);
                appearance.BorderBrush = solutionNameLabel.BorderBrush;
                appearance.BorderThickness = solutionNameLabel.BorderThickness;
                appearance.CornerRadius = solutionNameLabel.CornerRadius;
                appearance.Padding = solutionNameLabel.Padding;

                FrameworkElement solutionNameText = titleBarParts.SolutionNameText;
                appearance.TextColour = ReadSingleColourOrFallback(solutionNameText.GetValue(TextElement.ForegroundProperty) as Brush, appearance.TextColour);
                appearance.FontFamily = (FontFamily)solutionNameText.GetValue(TextElement.FontFamilyProperty);
                appearance.FontSize = (double)solutionNameText.GetValue(TextElement.FontSizeProperty);
                appearance.FontWeight = (FontWeight)solutionNameText.GetValue(TextElement.FontWeightProperty);
            }

            return appearance;
        }

        private static Brush? FindThemeBrush(object resourceKey) =>
            Application.Current?.TryFindResource(resourceKey) as Brush;

        private static Color ReadSingleColourOrFallback(Brush? brush, Color fallbackColour) =>
            brush is SolidColorBrush singleColourBrush ? singleColourBrush.Color : fallbackColour;
    }
}
