using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.PlatformUI;
using SolutionAlias.Label;
using SolutionAlias.Storage;
using SolutionAlias.TitleBar;

namespace SolutionAlias.Dialog
{
    /// <summary>What the user chose in the alias dialog.</summary>
    internal abstract class AliasDialogAction
    {
    }

    internal sealed class SaveAliasAction : AliasDialogAction
    {
        public SaveAliasAction(AliasFileContents contentsToSave)
        {
            ContentsToSave = contentsToSave;
        }

        public AliasFileContents ContentsToSave { get; }
    }

    internal sealed class ClearAliasAction : AliasDialogAction
    {
    }

    internal sealed class OpenOptionsAction : AliasDialogAction
    {
    }

    /// <summary>Edits the alias text and its text and background colours.</summary>
    internal partial class AliasDialog : DialogWindow
    {
        private readonly string _solutionFilePath;
        private readonly int _numFoldersToShow;
        private readonly SolutionLabelAppearance _solutionLabelAppearance;
        private readonly bool _isAlwaysUsingThemeColour;
        private readonly int _opacityPercent;
        private Color _chosenTextColour;
        private Color _chosenBackgroundColour;

        public AliasDialog(AliasFileContents? current, string solutionFilePath, int numFoldersToShow, SolutionLabelAppearance solutionLabelAppearance, bool alwaysUseThemeColour, int opacityPercent)
        {
            InitializeComponent();
            _solutionFilePath = solutionFilePath;
            _numFoldersToShow = numFoldersToShow;
            _solutionLabelAppearance = solutionLabelAppearance;
            _isAlwaysUsingThemeColour = alwaysUseThemeColour;
            _opacityPercent = opacityPercent;

            // Start from the colours the label has without the theme: the file's, or else the colours from the path
            LabelAppearance currentColours = LabelAppearanceUtil.BuildAppearance(
                solutionFilePath,
                current,
                numFoldersToShow,
                alwaysUseThemeColour: false,
                opacityPercent: opacityPercent,
                themeTextColour: solutionLabelAppearance.TextColour,
                themeBackgroundColour: solutionLabelAppearance.BackgroundColour);
            _chosenTextColour = currentColours.TextColour;
            _chosenBackgroundColour = currentColours.BackgroundColour;

            PreviewSolutionName.Text = Path.GetFileNameWithoutExtension(solutionFilePath);
            ApplyAppearanceToPreviewLabel(PreviewSolutionLabel, PreviewSolutionName, LabelColours.CreateFrozenBrush(solutionLabelAppearance.BackgroundColour), LabelColours.CreateFrozenBrush(solutionLabelAppearance.TextColour));

            AliasTextBox.Text = current?.Alias ?? string.Empty;

            // "Always use theme colour" ignores the chosen colours, so there is nothing to choose
            if (alwaysUseThemeColour)
            {
                ColourChoices.Visibility = Visibility.Collapsed;
            }
            else
            {
                CreateColourRowsCheckingTheChosenColours();
            }

            UpdateAliasPreview();
            Loaded += OnLoaded;
        }

        public AliasDialogAction? ChosenAction { get; private set; }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AliasTextBox.Focus();
            AliasTextBox.SelectAll();
        }

        private void OnAliasTextChanged(object sender, TextChangedEventArgs e) => UpdateAliasPreview();

        private void OnOkClick(object sender, RoutedEventArgs e) => ChooseActionAndCloseDialog(new SaveAliasAction(CreateContentsToSave()));

        /// <summary>
        /// The alias in the text box and the chosen colours, as the file stores them: what OK saves, and what the preview shows.
        /// </summary>
        private AliasFileContents CreateContentsToSave() => new AliasFileContents
        {
            Alias = AliasFileContents.NormaliseAliasText(AliasTextBox.Text),
            TextColourRgb = HexColourText.FormatAsHex(_chosenTextColour),
            BackgroundColourRgb = HexColourText.FormatAsHex(_chosenBackgroundColour),
        };

        /// <summary>Adds the text and background colour rows, each with the swatch in the chosen colour checked.</summary>
        private void CreateColourRowsCheckingTheChosenColours()
        {
            // From the XAML
            var swatchDisplayTemplate = (ControlTemplate)FindResource("SwatchTemplate");

            ColourSwatchRow textColourRow = ColourSwatchRow.CreateTextColourRow(TextColourSwatches, swatchDisplayTemplate, _chosenTextColour);
            ColourSwatchRow backgroundColourRow = ColourSwatchRow.CreateBackgroundColourRow(
                BackgroundColourSwatches,
                swatchDisplayTemplate,
                automaticBackground: LabelColours.CalculateBackgroundColourFromPath(_solutionFilePath),
                initialColour: _chosenBackgroundColour);

            textColourRow.ChosenColourChanged += (sender, e) => SetChosenTextColourAndUpdatePreview(textColourRow.ChosenColour);
            backgroundColourRow.ChosenColourChanged += (sender, e) => SetChosenBackgroundColourAndUpdatePreview(backgroundColourRow.ChosenColour);
        }

        private void SetChosenTextColourAndUpdatePreview(Color textColour)
        {
            _chosenTextColour = textColour;
            UpdateAliasPreview();
        }

        private void SetChosenBackgroundColourAndUpdatePreview(Color backgroundColour)
        {
            _chosenBackgroundColour = backgroundColour;
            UpdateAliasPreview();
        }

        /// <summary>The title-bar label as OK would leave it, built the same way as the real one.</summary>
        private LabelAppearance BuildPreviewAppearance() =>
            LabelAppearanceUtil.BuildAppearance(
                _solutionFilePath,
                CreateContentsToSave(),
                _numFoldersToShow,
                _isAlwaysUsingThemeColour,
                opacityPercent: _opacityPercent,
                themeTextColour: _solutionLabelAppearance.TextColour,
                themeBackgroundColour: _solutionLabelAppearance.BackgroundColour);

        private void OnClearClick(object sender, RoutedEventArgs e) => ChooseActionAndCloseDialog(new ClearAliasAction());
        private void OnOptionsClick(object sender, RoutedEventArgs e) => ChooseActionAndCloseDialog(new OpenOptionsAction());

        private void ChooseActionAndCloseDialog(AliasDialogAction action)
        {
            ChosenAction = action;
            DialogResult = true;
        }

        private void UpdateAliasPreview()
        {
            LabelAppearance preview = BuildPreviewAppearance();
            PreviewAliasText.Text = preview.Name;
            ApplyAppearanceToPreviewLabel(PreviewAliasLabel, PreviewAliasText, LabelColours.CreateFrozenBrush(preview.BackgroundColour), LabelColours.CreateFrozenBrush(preview.TextColour));
            PreviewAliasLabel.Opacity = preview.Opacity;
        }

        private void ApplyAppearanceToPreviewLabel(Border label, TextBlock text, Brush background, Brush foreground)
        {
            label.Background = background;
            label.BorderBrush = _solutionLabelAppearance.BorderBrush;
            label.BorderThickness = _solutionLabelAppearance.BorderThickness;
            label.CornerRadius = _solutionLabelAppearance.CornerRadius;
            label.Padding = _solutionLabelAppearance.Padding;
            text.Foreground = foreground;
            text.FontFamily = _solutionLabelAppearance.FontFamily;
            text.FontSize = _solutionLabelAppearance.FontSize;
            text.FontWeight = _solutionLabelAppearance.FontWeight;
        }
    }
}
