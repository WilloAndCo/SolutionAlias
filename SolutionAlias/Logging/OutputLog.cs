using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SolutionAlias.Logging
{
    /// <summary>
    /// Logs internal changes of the extension. Can be enabled in options. Writes to a new section of the output window.
    /// </summary>
    internal sealed class OutputLog
    {
        private readonly string _paneTitle;
        private readonly Guid _paneGuid;
        private IVsOutputWindowPane? _pane;

        public OutputLog(string paneTitle, Guid paneGuid)
        {
            _paneTitle = paneTitle;
            _paneGuid = paneGuid;
        }

        public bool IsEnabled { get; set; }

        public void WriteLineIfEnabled(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (!IsEnabled)
            {
                return;
            }

            IVsOutputWindowPane? pane = GetOrCreateOutputPane();
            pane?.OutputStringThreadSafe($"{DateTime.Now:HH:mm:ss.fff}  {message}{Environment.NewLine}");
        }

        private IVsOutputWindowPane? GetOrCreateOutputPane()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_pane != null)
            {
                return _pane;
            }

            IVsOutputWindow? outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (outputWindow == null)
            {
                return null;
            }

            Guid paneGuid = _paneGuid;
            outputWindow.CreatePane(ref paneGuid, _paneTitle, fInitVisible: 1, fClearWithSolution: 0);
            outputWindow.GetPane(ref paneGuid, out _pane);
            return _pane;
        }
    }
}
