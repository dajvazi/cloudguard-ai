using System.Text.RegularExpressions;

namespace CloudGuard.Api.Services.Terraform;

internal static partial class TerraformBlockReader
{
    [GeneratedRegex(
        @"^\s*(?<kind>resource|data)\s+""(?<type>[^""]+)""\s+""(?<name>[^""]+)""|^\s*module\s+""(?<modname>[^""]+)""",
        RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex BlockHeaderRegex();

    [GeneratedRegex(@"(?<key>\w+)\s*=\s*""(?<value>[^""]*)""", RegexOptions.Compiled)]
    private static partial Regex StringAttributeRegex();

    public static IEnumerable<TerraformBlockMatch> FindBlocks(string content)
    {
        foreach (Match match in BlockHeaderRegex().Matches(content))
        {
            var kind = match.Groups["kind"].Success
                ? match.Groups["kind"].Value
                : "module";

            var type = kind == "module"
                ? "module"
                : match.Groups["type"].Value;

            var name = kind == "module"
                ? match.Groups["modname"].Value
                : match.Groups["name"].Value;

            var body = ExtractBlockBody(content, match.Index + match.Length);
            yield return new TerraformBlockMatch(kind, type, name, body);
        }
    }

    public static string? GetStringAttribute(string blockBody, string key)
    {
        var match = StringAttributeRegex().Matches(blockBody)
            .Cast<Match>()
            .FirstOrDefault(m => m.Groups["key"].Value.Equals(key, StringComparison.OrdinalIgnoreCase));

        return match?.Groups["value"].Value;
    }

    private static string ExtractBlockBody(string content, int startIndex)
    {
        var braceIndex = content.IndexOf('{', startIndex);
        if (braceIndex < 0)
            return string.Empty;

        var depth = 0;
        for (var i = braceIndex; i < content.Length; i++)
        {
            if (content[i] == '{')
                depth++;
            else if (content[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return content[(braceIndex + 1)..i];
            }
        }

        return string.Empty;
    }

    internal record TerraformBlockMatch(string Kind, string Type, string Name, string Body);
}

internal static class TerraformPathHelper
{
    public static string NormalizePath(string path) =>
        path.Replace('\\', '/').Trim('/');

    public static string? ResolveLocalModulePath(string declaringFile, string moduleSource)
    {
        if (IsRemoteSource(moduleSource))
            return null;

        var declaringDirectory = NormalizePath(Path.GetDirectoryName(declaringFile) ?? string.Empty);
        var combined = string.IsNullOrEmpty(declaringDirectory)
            ? moduleSource
            : $"{declaringDirectory}/{moduleSource}";

        return NormalizeRelativePath(combined);
    }

    private static string NormalizeRelativePath(string path)
    {
        var segments = path
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        var stack = new List<string>();

        foreach (var segment in segments)
        {
            if (segment == ".")
                continue;

            if (segment == "..")
            {
                if (stack.Count > 0)
                    stack.RemoveAt(stack.Count - 1);
                continue;
            }

            stack.Add(segment);
        }

        return string.Join('/', stack);
    }

    public static bool IsRemoteSource(string source) =>
        source.StartsWith("git::", StringComparison.OrdinalIgnoreCase)
        || source.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || source.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || source.StartsWith("registry.terraform.io", StringComparison.OrdinalIgnoreCase);

    public static bool IsFileInModuleDirectory(string filePath, string moduleDirectory)
    {
        var normalizedFile = NormalizePath(filePath);
        var normalizedModule = NormalizePath(moduleDirectory);

        return normalizedFile.Equals(normalizedModule, StringComparison.OrdinalIgnoreCase)
            || normalizedFile.StartsWith($"{normalizedModule}/", StringComparison.OrdinalIgnoreCase);
    }
}
