using System.ComponentModel;
using System.Runtime.InteropServices;
using Community.VisualStudio.Toolkit;

namespace SolutionAlias.Options
{
    /// <summary>Tools > Options > Solution Alias > General.</summary>
    [ComVisible(true)]
    public class GeneralOptionsPage : BaseOptionPage<GeneralOptions> { }

    /// <summary>Per-user settings for all solutions, saved in the Visual Studio settings store.</summary>
    public class GeneralOptions : BaseOptionModel<GeneralOptions>
    {
        // The options page sorts categories alphabetically, so the leading tab (not shown) puts "Title bar" before "Logging".
        private const string TitleBarCategory = "\tTitle bar";
        public const int DefaultNumFoldersToShow = 5;
        public const bool DefaultAlwaysUseThemeColour = false;
        public const int DefaultOpacityPercent = 100;
        public const int MinimumOpacityPercent = 10;
        public const int MaximumOpacityPercent = 100;

        [Category(TitleBarCategory)]
        [DisplayName("Show label")]
        [Description("Show the label next to the solution name in the title bar.")]
        [DefaultValue(true)]
        public bool ShowLabel { get; set; } = true;

        [Category(TitleBarCategory)]
        [DisplayName("Folders to show")]
        [Description("When a solution has no alias, how many folders of its path the label shows, counted up from the solution file.")]
        [DefaultValue(DefaultNumFoldersToShow)]
        public int NumFoldersToShow { get; set; } = DefaultNumFoldersToShow;

        [Category(TitleBarCategory)]
        [DisplayName("Always use theme colour")]
        [Description("Draw every label in the colours of Visual Studio's own solution-name label, ignoring the colours chosen for each solution, and hide the colour choices in the alias dialog.")]
        [DefaultValue(DefaultAlwaysUseThemeColour)]
        public bool AlwaysUseThemeColour { get; set; } = DefaultAlwaysUseThemeColour;

        [Category(TitleBarCategory)]
        [DisplayName("Opacity (%)")]
        [Description("Opacity of the label, from 10 to 100")]
        [DefaultValue(DefaultOpacityPercent)]
        public int OpacityPercent { get; set; } = DefaultOpacityPercent;

        [Category("Logging")]
        [DisplayName("Log to Output window")]
        [Description("Write alias logging to the 'Solution Alias' pane of the Output window for diagnostics.")]
        [DefaultValue(false)]
        public bool LogToOutputWindow { get; set; }
    }
}
