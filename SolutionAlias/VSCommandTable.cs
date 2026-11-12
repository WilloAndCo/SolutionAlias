using System;

namespace SolutionAlias
{
    /// <summary>GUIDs used in the project's C# code.</summary>
    /// <remarks>Only need adding here if they're referenced from code.</remarks>
    internal static class PackageGuids
    {
        // GUIDs from VSCommandTable.vsct. Must be manually kept in sync.

        public const string PackageString = "92ecd625-e983-4f00-931a-d837d43401dc";
        public const string CommandSetString = "9471764c-1d13-48ea-84a1-d2a40c18f1c6";

        // PackageGuids that are not needed by the VSCommandTable

        public static readonly Guid OutputWindowSection = new Guid("98fa3dac-c264-49f9-bfa5-b6119681cfae");
    }

    /// <summary>IDSymbol are a name for a number inside a specific GUID.</summary>
    /// <remarks>Only need adding here if they're referenced from code.</remarks>
    internal static class PackageIds
    {
        // GUID IDs from VSCommandTable.vsct. Must be manually kept in sync.

        public const int SetSolutionAliasCommand = 0x0100;
    }
}
