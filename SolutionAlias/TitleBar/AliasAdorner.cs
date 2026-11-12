using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using SolutionAlias.Label;

namespace SolutionAlias.TitleBar
{
    /// <summary>
    /// This AliasAdorner is attached to the Visual Studio Solution name label, and draws another label next to it.
    /// </summary>
    /// <remarks>
    /// An Adorner is a WPF element attached to another element to draw something over/around it in another layer which is the parent.
    /// </remarks>
    internal sealed class AliasAdorner : Adorner, IDisposable
    {
        private const double SolutionLabelOffset = 4; // In WPF units
        private const double HideBelowWidth = 24; // In WPF units
        private const int NumAliasChildren = 1; // Adorner has a single Border child

        private readonly TitleBarParts _titleBarParts;
        private readonly Border _aliasLabel;
        private readonly TextBlock _aliasText;
        private Point _pressPosition;

        public AliasAdorner(TitleBarParts parts, LabelAppearance appearance)
            : base(parts.SolutionNameLabel) // the adorner is attached to VS's label
        {
            _titleBarParts = parts;

            _aliasText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis, // trim text if it doesn't fit in the available space
            };

            // Adorners inherit from the adorner layer, not from the adorned element, so copy the solution text details
            BindOneWay(_aliasText, TextBlock.FontFamilyProperty, parts.SolutionNameText, TextElement.FontFamilyProperty);
            BindOneWay(_aliasText, TextBlock.FontSizeProperty, parts.SolutionNameText, TextElement.FontSizeProperty);
            BindOneWay(_aliasText, TextBlock.FontWeightProperty, parts.SolutionNameText, TextElement.FontWeightProperty);

            _aliasLabel = new Border
            {
                Child = _aliasText,
                Cursor = Cursors.Hand,
                SnapsToDevicePixels = true,
            };

            // Looks the same as the solution name label
            BindOneWay(_aliasLabel, Border.PaddingProperty, parts.SolutionNameLabel, Border.PaddingProperty);
            BindOneWay(_aliasLabel, Border.CornerRadiusProperty, parts.SolutionNameLabel, Border.CornerRadiusProperty);
            BindOneWay(_aliasLabel, Border.BorderThicknessProperty, parts.SolutionNameLabel, Border.BorderThicknessProperty);
            BindOneWay(_aliasLabel, Border.BorderBrushProperty, parts.SolutionNameLabel, Border.BorderBrushProperty);

            _aliasLabel.MouseLeftButtonDown += OnMouseLeftButtonDown;
            _aliasLabel.MouseMove += OnMouseMove;
            _aliasLabel.MouseLeftButtonUp += OnMouseLeftButtonUp;
            _aliasLabel.MouseRightButtonDown += OnMouseRightButtonDown;
            _aliasLabel.MouseRightButtonUp += OnMouseRightButtonUp;
            AddVisualChild(_aliasLabel);

            // The available space changes when the window is resized or Visual Studio's label changes width.
            parts.SolutionNameGrid.SizeChanged += OnSolutionNameGridSizeChanged;
            parts.SolutionNameLabel.SizeChanged += OnSolutionNameLabelSizeChanged;
            parts.SolutionNameLabel.IsVisibleChanged += OnSolutionNameLabelIsVisibleChanged;

            SetAppearance(appearance);
        }

        /// <summary>Raised when the label is clicked with either button, without being dragged.</summary>
        public event EventHandler? Clicked;

        public void SetAppearance(LabelAppearance appearance)
        {
            _aliasText.Text = appearance.Name;
            _aliasLabel.ToolTip = appearance.ToolTip;
            SetBackgroundOrFollowVisualStudio(appearance.BackgroundColour, appearance.IsFollowingVisualStudioColours);
            SetTextColourOrFollowVisualStudio(appearance.TextColour, appearance.IsFollowingVisualStudioColours);
            Opacity = appearance.Opacity;
            InvalidateMeasure();
        }

        public void Dispose()
        {
            _titleBarParts.SolutionNameGrid.SizeChanged -= OnSolutionNameGridSizeChanged;
            _titleBarParts.SolutionNameLabel.SizeChanged -= OnSolutionNameLabelSizeChanged;
            _titleBarParts.SolutionNameLabel.IsVisibleChanged -= OnSolutionNameLabelIsVisibleChanged;
            BindingOperations.ClearAllBindings(_aliasLabel);
            BindingOperations.ClearAllBindings(_aliasText);
        }

        protected override int VisualChildrenCount => NumAliasChildren;

        protected override Visual GetVisualChild(int index) =>
            index == 0 ? _aliasLabel : throw new ArgumentOutOfRangeException(nameof(index));

        protected override Size MeasureOverride(Size constraint)
        {
            // label size is calculated - given no limitation, the label measures its size and stores it in DesiredSize internally. Used in ArrangeOverride
            _aliasLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            // This Adorner doesn't use a size: it is set on top of the solution label and the child label is arranged to a position relative to that
            return new Size();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {            
            double availableWidth = CalculateAvailableWidth();
            double desiredWidth = _aliasLabel.DesiredSize.Width;
            double drawWidth = Math.Min(desiredWidth, availableWidth);            
            
            bool hasDesiredWidth = drawWidth >= desiredWidth;
            bool showLabel = _titleBarParts.SolutionNameLabel.IsVisible && drawWidth > 0 && (hasDesiredWidth || drawWidth >= HideBelowWidth);

            Size solutionNameLabelSize = _titleBarParts.SolutionNameLabel.RenderSize;
            _aliasLabel.Arrange(new Rect(solutionNameLabelSize.Width + SolutionLabelOffset, 0, showLabel ? drawWidth : 0, solutionNameLabelSize.Height));

            // Changing Visibility would cause an update loop, so use opacity and hit testing instead
            _aliasLabel.Opacity = showLabel ? 1 : 0;
            _aliasLabel.IsHitTestVisible = showLabel;        
            
            return finalSize;
        }

        /// <summary>Space between end of the solution name to the end of the available area.</summary>
        private double CalculateAvailableWidth()
        {
            FrameworkElement solutionNameGrid = _titleBarParts.SolutionNameGrid;
            Border solutionNameLabel = _titleBarParts.SolutionNameLabel;
            if (!solutionNameGrid.IsVisible || !solutionNameLabel.IsVisible)
            {
                return 0;
            }

            try
            {
                Point start = solutionNameLabel.TranslatePoint(new Point(solutionNameLabel.RenderSize.Width + SolutionLabelOffset, 0), solutionNameGrid);
                return Math.Max(0, solutionNameGrid.ActualWidth - start.X);
            }
            catch (InvalidOperationException)
            {
                return 0;
            }
        }

        /// <summary>
        /// The appearance's colour, or with "Always use theme colour", Visual Studio's label copied live (theme colours, and
        /// the inactive-window colours).
        /// </summary>
        private void SetBackgroundOrFollowVisualStudio(Color backgroundColour, bool isFollowingVisualStudioColours)
        {
            _aliasLabel.ClearValue(Border.BackgroundProperty);
            if (!isFollowingVisualStudioColours)
            {
                _aliasLabel.Background = LabelColours.CreateFrozenBrush(backgroundColour);
            }
            else
            {
                BindOneWay(_aliasLabel, Border.BackgroundProperty, _titleBarParts.SolutionNameLabel, Border.BackgroundProperty);
            }
        }

        private void SetTextColourOrFollowVisualStudio(Color textColour, bool isFollowingVisualStudioColours)
        {
            _aliasText.ClearValue(TextBlock.ForegroundProperty);
            if (!isFollowingVisualStudioColours)
            {
                _aliasText.Foreground = LabelColours.CreateFrozenBrush(textColour);
            }
            else
            {
                BindOneWay(_aliasText, TextBlock.ForegroundProperty, _titleBarParts.SolutionNameText, TextElement.ForegroundProperty);
            }
        }

        private void OnSolutionNameGridSizeChanged(object sender, SizeChangedEventArgs e) => InvalidateArrange();

        private void OnSolutionNameLabelSizeChanged(object sender, SizeChangedEventArgs e) => InvalidateArrange();

        private void OnSolutionNameLabelIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) => InvalidateArrange();

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // starting press position
            _pressPosition = e.GetPosition(_aliasLabel);

            // send every mouse event to the alias label until the capture is released
            _aliasLabel.CaptureMouse();

            e.Handled = true;
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_aliasLabel.IsMouseCaptured == false)
            {
                // mouse release unrelated to this label
                return;
            }

            _aliasLabel.ReleaseMouseCapture();
            e.Handled = true;
            Clicked?.Invoke(this, EventArgs.Empty);
        }

        private void OnMouseRightButtonDown(object sender, MouseButtonEventArgs e) => e.Handled = true;

        private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Clicked?.Invoke(this, EventArgs.Empty);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (_aliasLabel.IsMouseCaptured == false)
            {
                // mouse release unrelated to this label
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                // not moving while pressed
                return;
            }

            Vector moveDistance = e.GetPosition(_aliasLabel) - _pressPosition;
            if (Math.Abs(moveDistance.X) < SystemParameters.MinimumHorizontalDragDistance
                && Math.Abs(moveDistance.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                // not moved far enough to be considered a drag
                return;
            }

            // A drag rather than a click, move the window
            _aliasLabel.ReleaseMouseCapture();
            MainWindowDrag.StartMovingWindowWithMouse();
        }

        /// <summary>Links one property to another and keeps it up to date.</summary>
        private static void BindOneWay(DependencyObject target, DependencyProperty targetProperty, DependencyObject source, DependencyProperty sourceProperty)
        {
            BindingOperations.SetBinding(target, targetProperty, new Binding
            {
                Source = source,
                Path = new PropertyPath(sourceProperty),
                Mode = BindingMode.OneWay,
            });
        }
    }
}
