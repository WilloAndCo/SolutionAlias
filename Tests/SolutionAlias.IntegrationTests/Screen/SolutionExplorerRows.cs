using System;
using System.Windows.Automation;

namespace SolutionAlias.IntegrationTests.Screen
{
    /// <summary>
    /// Finds Solution Explorer's rows through Windows UI Automation
    /// </summary>
    internal sealed class SolutionExplorerRows
    {
        /// <summary>Visual Studio's own automation ID for Solution Explorer's tree.</summary>
        private const string SolutionExplorerTreeAutomationId = "SolutionExplorer";

        private readonly int _vsProcessId;

        public SolutionExplorerRows(int processId)
        {
            _vsProcessId = processId;
        }

        public AutomationElement? FindSolutionRow(string solutionName)
        {
            // find Solution Explorer's tree in the Visual Studio process
            var solutionExplorerTreeFilter = new AndCondition(
                new PropertyCondition(AutomationElement.ProcessIdProperty, _vsProcessId),
                new PropertyCondition(AutomationElement.AutomationIdProperty, SolutionExplorerTreeAutomationId));

            AutomationElement windowsDesktop = AutomationElement.RootElement;
            AutomationElement? solutionExplorerTree = windowsDesktop.FindFirst(TreeScope.Descendants, solutionExplorerTreeFilter);
            if (solutionExplorerTree == null)
            {
                // Solution Explorer is closed or hasn't been created yet
                return null;
            }
            
            // Solution is always displayed this way in the Solution Explorer at the top
            string findNamePrefix = $"Solution '{solutionName}'";

            var treeItemFilter = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.TreeItem);
            foreach (AutomationElement row in solutionExplorerTree.FindAll(TreeScope.Descendants, treeItemFilter))
            {
                if (row.Current.Name.StartsWith(findNamePrefix, StringComparison.Ordinal))
                {
                    return row;
                }
            }

            return null;
        }
    }
}
