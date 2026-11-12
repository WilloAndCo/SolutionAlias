using System.Windows.Media;

namespace SolutionAlias.Label
{
    /// <summary>What the title-bar label and the dialog's preview show.</summary>
    internal sealed class LabelAppearance
    {
        public LabelAppearance(string name, Color textColour, Color backgroundColour, bool isFollowingVisualStudioColours, string toolTip, double opacity)
        {
            Name = name;
            TextColour = textColour;
            BackgroundColour = backgroundColour;
            IsFollowingVisualStudioColours = isFollowingVisualStudioColours;
            ToolTip = toolTip;
            Opacity = opacity;
        }

        public string Name { get; }
        public Color TextColour { get; }
        public Color BackgroundColour { get; }
        public bool IsFollowingVisualStudioColours { get; }
        public string ToolTip { get; }
        public double Opacity { get; }
    }
}
