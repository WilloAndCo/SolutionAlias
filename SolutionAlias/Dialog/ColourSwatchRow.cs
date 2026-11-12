using SolutionAlias.Label;
using SolutionAlias.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using WinForms = System.Windows.Forms;

namespace SolutionAlias.Dialog
{
    /// <summary>
    /// One row of colour swatches in the alias dialog: Auto first in the background row, then the presets, a custom colour,
    /// and "Custom...". The checked swatch's colour is the chosen colour.
    /// </summary>
    internal sealed class ColourSwatchRow
    {
        /// <summary>The colour picker's list of custom colours, shown in the picker. Lasts for the Visual Studio session; not saved.</summary>
        private static int[] s_pickerCustomColours = Array.Empty<int>();

        private readonly Panel _panel;
        private readonly ControlTemplate _template;
        private readonly string _description;
        private readonly RadioButton _customSwatchBtn;

        private ColourSwatchRow(Panel panel, ControlTemplate template, string description, IEnumerable<NameColourPair> presets)
        {
            _panel = panel;
            _template = template;
            _description = description;

            foreach (NameColourPair preset in presets)
            {
                _panel.Children.Add(CreateSwatch(preset.Name, preset.Colour, text: null));
            }

            // Space for a custom swatch, only visible once a colour matches no other swatch; its colour is set then
            _customSwatchBtn = CreateSwatch("Custom colour", Colors.Transparent, text: null);
            _customSwatchBtn.Visibility = Visibility.Collapsed;
            _panel.Children.Add(_customSwatchBtn);

            var customButton = new Button
            {
                Content = "Custom...",
                Height = 30,
                MinWidth = 72,
                Margin = new Thickness(0, 0, 4, 4),
                VerticalAlignment = VerticalAlignment.Top,
            };
            AutomationProperties.SetName(customButton, description + ": choose a custom colour");
            customButton.Click += OnCustomClick;
            _panel.Children.Add(customButton);
        }

        public event EventHandler? ChosenColourChanged;

        /// <summary>The checked swatch's colour.</summary>
        public Color ChosenColour { get; private set; }

        public static ColourSwatchRow CreateTextColourRow(Panel panel, ControlTemplate template, Color initialColour)
        {
            var row = new ColourSwatchRow(panel, template, "Text colour", ColourPresets.Text);
            row.CheckSwatchMatchingColourOrShowCustom(initialColour);
            return row;
        }

        public static ColourSwatchRow CreateBackgroundColourRow(Panel panel, ControlTemplate template, Color automaticBackground, Color initialColour)
        {
            var row = new ColourSwatchRow(panel, template, "Background colour", ColourPresets.Background);
            row._panel.Children.Insert(0, row.CreateSwatch("Auto", automaticBackground, text: "Auto"));
            row.CheckSwatchMatchingColourOrShowCustom(initialColour);
            return row;
        }

        private RadioButton CreateSwatch(string name, Color colour, string? text)
        {
            var swatch = new RadioButton
            {
                GroupName = _description,
                Template = _template,
                Background = LabelColours.CreateFrozenBrush(colour),
                Content = text == null ? null : CreateSwatchText(text),
                Tag = colour,
            };

            // colour name is not shown as a tooltip, but used here for screen readers to tell colours apart
            AutomationProperties.SetName(swatch, $"{_description}: {name}");
            swatch.Checked += OnSwatchChecked;
            return swatch;
        }

        private static TextBlock CreateSwatchText(string text) =>
            new TextBlock { Text = text, FontSize = 9, Foreground = LabelColours.CreateFrozenBrush(Colors.Black) };

        private void OnSwatchChecked(object sender, RoutedEventArgs e)
        {
            SetChosenColourAndNotifyIfDifferent((Color)((RadioButton)sender).Tag);
        }

        /// <summary>Checks the first swatch filled with <paramref name="colour"/>, or else shows it as the custom swatch.</summary>
        private void CheckSwatchMatchingColourOrShowCustom(Color colour)
        {
            // A swatch in this colour. Auto comes first, so the solution path's own colour checks Auto.
            RadioButton? foundMatch = _panel.Children.OfType<RadioButton>().FirstOrDefault(swatch => swatch != _customSwatchBtn && (Color)swatch.Tag == colour);
            if (foundMatch != null)
            {
                CheckSwatchAndUpdateChosenColour(foundMatch);
            }
            else
            {
                // Show custom
                SetCustomSwatchWithColour(colour);
                CheckSwatchAndUpdateChosenColour(_customSwatchBtn);
            }
        }

        private void SetCustomSwatchWithColour(Color colour)
        {
            _customSwatchBtn.Tag = colour;
            _customSwatchBtn.Background = LabelColours.CreateFrozenBrush(colour);
            AutomationProperties.SetName(_customSwatchBtn, $"{_description}: custom colour {HexColourText.FormatAsHex(colour)}");
            _customSwatchBtn.Visibility = Visibility.Visible;
        }

        private void CheckSwatchAndUpdateChosenColour(RadioButton swatch)
        {
            swatch.IsChecked = true;

            // Checking an already-checked swatch raises no Checked event, but the custom swatch's colour may have changed.
            SetChosenColourAndNotifyIfDifferent((Color)swatch.Tag);
        }

        private void SetChosenColourAndNotifyIfDifferent(Color colour)
        {
            if (ChosenColour == colour)
            {
                return;
            }

            ChosenColour = colour;
            ChosenColourChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnCustomClick(object sender, RoutedEventArgs e)
        {
            // using WinForms because WPF doesn't have a built-in colour picker
            using (var picker = new WinForms.ColorDialog { AnyColor = true, FullOpen = true, CustomColors = s_pickerCustomColours })
            {
                // Start at the chosen colour; for Auto, the colour from the path
                picker.Color = System.Drawing.Color.FromArgb(ChosenColour.R, ChosenColour.G, ChosenColour.B);

                // Show the picker and wait until the user closes it.
                WinForms.DialogResult result = picker.ShowDialog();

                if (result == WinForms.DialogResult.OK)
                {
                    // On user pressing ok, check the swatch in that colour (Auto, a preset, or the custom swatch) and add to custom colours
                    s_pickerCustomColours = picker.CustomColors;
                    CheckSwatchMatchingColourOrShowCustom(Color.FromRgb(picker.Color.R, picker.Color.G, picker.Color.B));
                }
            }
        }
    }
}
