using System.Text.RegularExpressions;

namespace ResearchPublications.Infrastructure.Ocr;

internal static partial class OcrMarkdownConverter
{
    internal const string Version = "markdown-1";

    public static string ToMarkdown(string text)
    {
        text = MarkdownWrapper().Replace(text.Trim(), "${content}");
        // Match only layout metadata at the beginning of a line, preserving brackets in content.
        return RegionPrefix().Replace(text, match => (match.Index > 0 ? "\n" : string.Empty) + (match.Groups["kind"].Value switch
        {
            "title" => "# ",
            "subtitle" => "## ",
            "header" => "Header: ",
            "footer" => "Footer: ",
            _ => string.Empty
        })).Trim();
    }

    [GeneratedRegex(@"^[ \t]*(?<kind>title|subtitle|text|header|footer|table|image|figure|caption|list|equation|formula|page_number)\s*\[\s*\d+\s*,\s*\d+\s*,\s*\d+\s*,\s*\d+\s*\][ \t]*", RegexOptions.Multiline)]
    private static partial Regex RegionPrefix();

    [GeneratedRegex(@"\A(?<fence>`{3,}|~{3,})(?:markdown|md)?[ \t]*\r?\n(?<content>[\s\S]*?)\r?\n\k<fence>[ \t]*\z", RegexOptions.IgnoreCase)]
    private static partial Regex MarkdownWrapper();
}
