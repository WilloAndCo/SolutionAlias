using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.Shell;
using SolutionAlias.Label;
using SolutionAlias.Logging;

namespace SolutionAlias.TitleBar
{
    internal sealed class AttachedLabel
    {
        public AttachedLabel(TitleBarParts titleBarParts, AdornerLayer adornerLayer, AliasAdorner aliasAdorner)
        {
            TitleBarParts = titleBarParts;
            AdornerLayer = adornerLayer;
            AliasAdorner = aliasAdorner;
        }

        public TitleBarParts TitleBarParts { get; }
        public AdornerLayer AdornerLayer { get; }
        public AliasAdorner AliasAdorner { get; }
    }

    /// <summary>Shows the label in the title bar, and reattaches if it becomes detached.</summary>
    internal sealed class TitleBarAliasPresenter : IDisposable
    {
        private static readonly TimeSpan ReattachTimerInterval = TimeSpan.FromMilliseconds(500);
        /// <summary>After a search for the solution name finds nothing, skip this many ticks before searching again.</summary>
        private const int FailedSearchesTickInterval = 10;

        private LabelAppearance? _labelAppearance;
        private AttachedLabel? _attachedLabel;
        /// <summary>Checks if the label has come off the title bar (VS rebuilt its solution-name label or adorner layer).</summary>
        private readonly DispatcherTimer _reattachTimer;
        private int _ticksUntilNextSearch;
        private bool _titleBarNotFound;
        private readonly OutputLog _log;

        public TitleBarAliasPresenter(OutputLog log)
        {
            _log = log;
            _reattachTimer = new DispatcherTimer(DispatcherPriority.Background) { Interval = ReattachTimerInterval };
            _reattachTimer.Tick += OnReattachTimerTick;
        }

        /// <summary>Raised when the label is clicked with either mouse button.</summary>
        public event EventHandler? LabelClicked;

        public void ShowLabelAndStartReattachTimer(LabelAppearance labelAppearance)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _labelAppearance = labelAppearance;
            _attachedLabel?.AliasAdorner.SetAppearance(labelAppearance);
            ReattachLabelIfDetached(forceUpdate: true);
            _reattachTimer.Start();
        }

        public void HideLabelAndStopReattachTimer()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _reattachTimer.Stop();
            _labelAppearance = null;
            DetachAndDisposeLabel();
        }

        /// <summary>Stops the reattach timer without touching the label, for when Visual Studio is closing.</summary>
        public void StopReattachTimer()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _reattachTimer.Stop();
        }

        public void Dispose()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            HideLabelAndStopReattachTimer();
            _reattachTimer.Tick -= OnReattachTimerTick;
        }

        private void OnReattachTimerTick(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            ReattachLabelIfDetached();
        }

        private void ReattachLabelIfDetached(bool forceUpdate = false)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_labelAppearance == null)
            {
                return;
            }

            if (IsLabelStillAttached())
            {
                return;
            }

            // The label came off the title bar, so remove ours before searching again.
            if (_attachedLabel != null)
            {
                _log.WriteLineIfEnabled("The label came off the title bar (Visual Studio rebuilt its solution-name label or adorner layer); attaching it again.");
                DetachAndDisposeLabel();
            }

            // Still waiting after a failed search.
            if (forceUpdate == false && _ticksUntilNextSearch > 0)
            {
                _ticksUntilNextSearch--;
                return;
            }

            TitleBarParts? titleBarParts = TitleBarParts.FindInMainWindow(Application.Current?.MainWindow);
            AdornerLayer? adornerLayer = titleBarParts != null ? AdornerLayer.GetAdornerLayer(titleBarParts.SolutionNameLabel) : null;
            if (titleBarParts == null || adornerLayer == null)
            {
                WaitBeforeNextSearch(titleBarParts);
                return;
            }

            _ticksUntilNextSearch = 0;
            _titleBarNotFound = false;
            AttachNewLabel(titleBarParts, adornerLayer, _labelAppearance);
        }

        private void WaitBeforeNextSearch(TitleBarParts? titleBarParts)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            
            _ticksUntilNextSearch = FailedSearchesTickInterval;
            if (_titleBarNotFound)
            {
                return;
            }

            _log.WriteLineIfEnabled(titleBarParts == null
                ? "Visual Studio's solution-name label wasn't found in the main window; will keep trying."
                : "The title bar has no adorner layer; will keep trying.");
            _titleBarNotFound = true;
        }

        private void AttachNewLabel(TitleBarParts titleBarParts, AdornerLayer adornerLayer, LabelAppearance labelAppearance)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _attachedLabel = new AttachedLabel(titleBarParts, adornerLayer, new AliasAdorner(titleBarParts, labelAppearance));
            _attachedLabel.AliasAdorner.Clicked += OnAdornerClicked;
            adornerLayer.Add(_attachedLabel.AliasAdorner);
            _log.WriteLineIfEnabled($"Label '{labelAppearance.Name}' attached next to the title bar's '{titleBarParts.SolutionNameLabel.Name}' ({titleBarParts.SolutionNameLabel.GetType().Name}).");
        }

        private bool IsLabelStillAttached()
        {
            return _attachedLabel != null
                && _attachedLabel.TitleBarParts.IsStillConnected
                && ReferenceEquals(AdornerLayer.GetAdornerLayer(_attachedLabel.TitleBarParts.SolutionNameLabel), _attachedLabel.AdornerLayer)
                && ReferenceEquals(VisualTreeHelper.GetParent(_attachedLabel.AliasAdorner), _attachedLabel.AdornerLayer);
        }

        private void DetachAndDisposeLabel()
        {
            if (_attachedLabel == null)
            {
                return;
            }

            _attachedLabel.AliasAdorner.Clicked -= OnAdornerClicked;
            _attachedLabel.AliasAdorner.Dispose();
            if (ReferenceEquals(VisualTreeHelper.GetParent(_attachedLabel.AliasAdorner), _attachedLabel.AdornerLayer))
            {
                _attachedLabel.AdornerLayer.Remove(_attachedLabel.AliasAdorner);
            }

            _attachedLabel = null;
        }

        private void OnAdornerClicked(object sender, EventArgs e) => LabelClicked?.Invoke(this, EventArgs.Empty);
    }
}
