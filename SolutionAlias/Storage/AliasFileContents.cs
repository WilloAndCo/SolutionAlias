using System.Runtime.Serialization;

namespace SolutionAlias.Storage
{
    /// <summary>
    /// The contents of SolutionAlias.json, for example
    /// { "alias": "MAIN", "textColourRgb": "#FFFFFF", "backgroundColourRgb": "#C0392B" }.
    /// </summary>
    [DataContract]
    internal sealed class AliasFileContents
    {
        [DataMember(Name = "alias", Order = 1)]
        public string? Alias { get; set; }

        [DataMember(Name = "textColourRgb", Order = 2)]
        public string? TextColourRgb { get; set; }

        [DataMember(Name = "backgroundColourRgb", Order = 3)]
        public string? BackgroundColourRgb { get; set; }

        public bool HasAlias => !string.IsNullOrWhiteSpace(Alias);

        public static string? NormaliseAliasText(string? aliasText)
        {
            string trimmed = aliasText?.Trim() ?? string.Empty;
            return trimmed.Length > 0 ? trimmed : null;
        }
    }
}
