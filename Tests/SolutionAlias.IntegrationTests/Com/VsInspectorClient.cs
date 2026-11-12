using System;
using System.Globalization;
using System.Reflection;
using System.Windows;

namespace SolutionAlias.IntegrationTests.Com
{
    /// <summary>
    /// Calls the VisualStudioInspector extension through DTE.GetObject.
    /// </summary>
    /// <remarks>
    /// Names and functionality must match VisualStudioInspector exactly.
    /// </remarks>
    internal sealed class VsInspectorClient
    {
        public const string SolutionAliasPackageGuid = SolutionAlias.PackageGuids.PackageString;

        private readonly EnvDTE80.DTE2 _dte;
        private object? _vsInspectorProxy;

        public VsInspectorClient(EnvDTE80.DTE2 dte)
        {
            // DTE of the devenv.exe that VisualStudioTestRun started
            _dte = dte;
        }

        public bool IsAliasLabelDrawn() => InvokeInspectorMethodByName<bool>(nameof(IsAliasLabelDrawn));

        public bool IsAliasLabelAttached() => InvokeInspectorMethodByName<bool>(nameof(IsAliasLabelAttached));

        public string ReadAliasLabelText() => InvokeInspectorMethodByName<string>(nameof(ReadAliasLabelText));

        public string ReadAliasLabelForeground() => InvokeInspectorMethodByName<string>(nameof(ReadAliasLabelForeground));

        public string ReadAliasLabelBackground() => InvokeInspectorMethodByName<string>(nameof(ReadAliasLabelBackground));

        public string ReadAliasLabelToolTip() => InvokeInspectorMethodByName<string>(nameof(ReadAliasLabelToolTip));

        public int ReadAliasLabelOpacityPercent() => InvokeInspectorMethodByName<int>(nameof(ReadAliasLabelOpacityPercent));

        public Rect? ReadAliasLabelScreenBounds()
        {
            string boundsStr = InvokeInspectorMethodByName<string>(nameof(ReadAliasLabelScreenBounds));
            if (string.IsNullOrEmpty(boundsStr))
            {
                return null;
            }

            double[] boundsParts = Array.ConvertAll(boundsStr.Split(','), part => double.Parse(part, CultureInfo.InvariantCulture));
            return new Rect(boundsParts[0], boundsParts[1], boundsParts[2], boundsParts[3]);
        }

        /// <summary>Loads the package with this GUID if Visual Studio hasn't already; true once it is loaded.</summary>
        /// <remarks>Must be called before interacting with Options.</remarks>
        public bool LoadPackageIfNotLoaded(string packageGuidString) => InvokeInspectorMethodByName<bool>(nameof(LoadPackageIfNotLoaded), packageGuidString);

        public void SetAndSaveOption(string extensionAssemblyName, string optionName, object optionValue) =>
            InvokeInspectorMethodByName<bool>(nameof(SetAndSaveOption), extensionAssemblyName, optionName, Convert.ToString(optionValue, CultureInfo.InvariantCulture));

        public string ReadOption(string extensionAssemblyName, string optionName) => InvokeInspectorMethodByName<string>(nameof(ReadOption), extensionAssemblyName, optionName);

        /// <summary>Calls the inspector's method of the same name.</summary>
        /// <remarks>This runs on the Visual Studio instance through the COM interface.</remarks>
        private T InvokeInspectorMethodByName<T>(string methodName, params object[] args)
        {
            if (_vsInspectorProxy == null)
            {
                _vsInspectorProxy = _dte.GetObject("SolutionAlias.VisualStudioInspector")
                    ?? throw new InvalidOperationException("The inspector extension isn't available (DTE.GetObject returned null).");
            }

            return (T)_vsInspectorProxy.GetType().InvokeMember(methodName, BindingFlags.InvokeMethod, null, _vsInspectorProxy, args);
        }
    }
}
