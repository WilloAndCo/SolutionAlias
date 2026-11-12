using System;

namespace SolutionAlias.IntegrationTests.Run
{
    internal sealed class VisualStudioTestSetup
    {
        /// <param name="prepareCopy">Given the copy's folder; runs before Visual Studio opens it.</param>
        public VisualStudioTestSetup(int visualStudioVersion, string sampleSolutionToCopy, Action<string>? prepareCopy = null)
        {
            VisualStudioVersion = visualStudioVersion;
            SampleSolutionToCopy = sampleSolutionToCopy;
            PrepareCopy = prepareCopy;
        }

        public int VisualStudioVersion { get; }

        public string SampleSolutionToCopy { get; }

        public Action<string>? PrepareCopy { get; }
    }
}
