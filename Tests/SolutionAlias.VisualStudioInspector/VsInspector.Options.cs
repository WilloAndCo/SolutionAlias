using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SolutionAlias.VisualStudioInspector
{
    /// <summary>
    /// Interacting with Solution Alias's options (Tools > Options > Solution Alias > General).
    /// </summary>
    public sealed partial class VsInspector
    {
        /// <summary>Returns true when the package is loaded.</summary>
        /// <remarks>Required as a prerequisite for accessing the extension options.</remarks>
        public bool LoadPackageIfNotLoaded(string packageGuidString) => RunOnVisualStudioUIThread(() =>
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (ServiceProvider.GlobalProvider.GetService(typeof(SVsShell)) is not IVsShell shell)
            {
                return false;
            }

            var packageGuid = new Guid(packageGuidString);
            if (ErrorHandler.Succeeded(shell.IsPackageLoaded(ref packageGuid, out IVsPackage package)) && package != null)
            {
                return true;
            }

            return ErrorHandler.Succeeded(shell.LoadPackage(ref packageGuid, out package)) && package != null;
        });

        /// <summary>
        /// Sets an option from its text and saves it, e.g. SetAndSaveOption("SolutionAlias", "ShowLabel", "False") turns
        /// off the title bar label.
        /// </summary>
        public bool SetAndSaveOption(string extensionAssemblyName, string optionName, string optionValueAsText) => RunOnVisualStudioUIThread(() =>
        {
            object liveOptions = FindLiveOptionsObject(extensionAssemblyName);
            PropertyInfo optionProperty = FindOptionProperty(liveOptions, optionName);
            optionProperty.SetValue(liveOptions, ConvertOptionValueFromText(optionValueAsText, optionProperty.PropertyType));
            SaveOptions(liveOptions);
            return true;
        });

        /// <summary>An option's current value as text, e.g. ReadOption("SolutionAlias", "OpacityPercent") gets the current value of opacity.</summary>
        public string ReadOption(string extensionAssemblyName, string optionName) => RunOnVisualStudioUIThread(() =>
        {
            object liveOptions = FindLiveOptionsObject(extensionAssemblyName);
            PropertyInfo optionProperty = FindOptionProperty(liveOptions, optionName);
            return Convert.ToString(optionProperty.GetValue(liveOptions), CultureInfo.InvariantCulture);
        });

        /// <summary>The options object for the extension assembly, e.g. FindLiveOptionsObject("SolutionAlias").</summary>
        private static object FindLiveOptionsObject(string extensionAssemblyName)
        {
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            Assembly? extensionAssembly = loadedAssemblies.FirstOrDefault(assembly => assembly.GetName().Name == extensionAssemblyName);
            if (extensionAssembly == null)
            {
                throw new InvalidOperationException($"{extensionAssemblyName} isn't loaded; load its package first.");
            }

            string optionsTypeName = $"{extensionAssemblyName}.Options.GeneralOptions";
            Type optionsType = extensionAssembly.GetType(optionsTypeName, throwOnError: true);

            // FlattenHierarchy finds static properties in the base classes - Instance is in BaseOptionModel of the community toolkit
            PropertyInfo instanceProperty = optionsType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            return instanceProperty.GetValue(null);
        }

        /// <summary>The property for an option name, not its value, e.g. FindOptionProperty(liveOptions, "NumFoldersToShow").</summary>
        private static PropertyInfo FindOptionProperty(object liveOptions, string optionName)
        {
            Type optionsType = liveOptions.GetType();
            PropertyInfo? optionProperty = optionsType.GetProperty(optionName);
            if (optionProperty == null)
            {
                throw new ArgumentException($"No option '{optionName}' in {optionsType.FullName}.");
            }

            return optionProperty;
        }

        /// <summary>An option's value from its text, e.g. ConvertOptionValueFromText("40", typeof(int)).</summary>
        private static object ConvertOptionValueFromText(string optionValueAsText, Type optionType)
        {
            if (optionType.IsEnum)
            {
                // Convert.ChangeType can't turn text into an enum
                return Enum.Parse(optionType, optionValueAsText);
            }
            else
            {
                return Convert.ChangeType(optionValueAsText, optionType, CultureInfo.InvariantCulture);
            }
        }

        private static void SaveOptions(object liveOptions)
        {
            Type optionsType = liveOptions.GetType();

            // Save is in BaseOptionModel of the community toolkit
            MethodInfo? saveMethod = optionsType.GetMethod("Save", Type.EmptyTypes);
            if (saveMethod == null)
            {
                throw new InvalidOperationException($"{optionsType.FullName} has no Save().");
            }

            saveMethod.Invoke(liveOptions, null);
        }
    }
}
