using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace SolutionAlias.VisualStudioInspector
{
    /// <summary>
    /// VsInspectorPackage allows DTE access to VsInspector at runtime through the Automation Object, DTE.GetObject("SolutionAlias.VisualStudioInspector")
    /// </summary>
    /// <remarks>
    /// Only AsyncPackage required, rather than ToolkitPackage, because the extension doesn't use any of the util.
    /// Doesn't reference SolutionAlias, lookups are done by name using reflection
    /// ProvideAutomationObject attributes means this is loaded immidiately on the first dte call to it.
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutomationObject(VsInspector.AutomationName)]
    [Guid("30e32437-9cb9-4e36-b20a-07532f83ce4b")]
    public sealed class VsInspectorPackage : AsyncPackage
    {
        private VsInspector? _inspector;

        // ProvideAutomationObject attribute and GetAutomationObject are required for the DTE lookup
        protected override object GetAutomationObject(string name)
        {
            if (string.Equals(name, VsInspector.AutomationName, StringComparison.Ordinal))
            {
                // Create on first lookup
                return _inspector ?? (_inspector = new VsInspector());
            }
            else
            {
                return base.GetAutomationObject(name);
            }
        }
    }
}
