namespace CloudGuard.Api.Services.Terraform;

public class TerraformProjectParser(ITerraformParserService fileParser) : ITerraformProjectParser
{
    public IReadOnlyList<ParsedTerraformResource> ParseProject(IReadOnlyList<TerraformFileEntry> files)
    {
        if (files.Count == 0)
            return [];

        var normalizedFiles = files
            .Select(f => new TerraformFileEntry(
                TerraformPathHelper.NormalizePath(f.RelativePath),
                f.Content))
            .OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var modulePathMap = BuildModulePathMap(normalizedFiles);
        var results = new List<ParsedTerraformResource>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in normalizedFiles)
        {
            var parentModule = ResolveParentModule(file.RelativePath, modulePathMap);
            var parsed = fileParser.ParseFile(file.RelativePath, file.Content, parentModule);

            foreach (var resource in parsed)
            {
                var key = $"{resource.SourceKind}:{resource.ResourceType}:{resource.Name}:{resource.SourceFile}:{resource.ParentModule}";

                if (seen.Add(key))
                    results.Add(resource);
            }
        }

        return results;
    }

    private static Dictionary<string, string> BuildModulePathMap(IReadOnlyList<TerraformFileEntry> files)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            foreach (var block in TerraformBlockReader.FindBlocks(file.Content))
            {
                if (!block.Kind.Equals("module", StringComparison.OrdinalIgnoreCase))
                    continue;

                var source = TerraformBlockReader.GetStringAttribute(block.Body, "source");
                if (string.IsNullOrWhiteSpace(source))
                    continue;

                var resolvedPath = TerraformPathHelper.ResolveLocalModulePath(file.RelativePath, source);
                if (resolvedPath is null)
                    continue;

                map.TryAdd(resolvedPath, block.Name);
            }
        }

        return map;
    }

    private static string? ResolveParentModule(
        string filePath,
        IReadOnlyDictionary<string, string> modulePathMap)
    {
        string? bestMatch = null;
        var bestLength = -1;

        foreach (var (modulePath, moduleName) in modulePathMap)
        {
            if (!TerraformPathHelper.IsFileInModuleDirectory(filePath, modulePath))
                continue;

            if (modulePath.Length > bestLength)
            {
                bestLength = modulePath.Length;
                bestMatch = moduleName;
            }
        }

        return bestMatch;
    }
}
