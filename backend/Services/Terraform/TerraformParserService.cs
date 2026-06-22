namespace CloudGuard.Api.Services.Terraform;

public class TerraformParserService : ITerraformParserService
{
    public IReadOnlyList<ParsedTerraformResource> ParseFile(
        string relativePath,
        string content,
        string? parentModule)
    {
        if (string.IsNullOrWhiteSpace(content))
            return [];

        var resources = new List<ParsedTerraformResource>();
        var normalizedPath = TerraformPathHelper.NormalizePath(relativePath);

        foreach (var block in TerraformBlockReader.FindBlocks(content))
        {
            var moduleSource = block.Kind == "module"
                ? TerraformBlockReader.GetStringAttribute(block.Body, "source")
                : null;

            var displayType = block.Kind switch
            {
                "module" => "Terraform Module",
                "data" => "Data Source",
                _ => TerraformResourceTypeMapper.ToDisplayType(block.Type),
            };

            var description = block.Kind switch
            {
                "module" => BuildModuleDescription(block.Name, moduleSource),
                "data" => $"data {block.Type}.{block.Name}",
                _ => $"resource {block.Type}.{block.Name}",
            };

            resources.Add(new ParsedTerraformResource(
                Name: block.Name,
                ResourceType: block.Kind == "module" ? "module" : block.Type,
                DisplayType: displayType,
                Description: description,
                SourceKind: block.Kind,
                SourceFile: normalizedPath,
                ModuleSource: moduleSource,
                ParentModule: parentModule));
        }

        return resources;
    }

    private static string BuildModuleDescription(string name, string? moduleSource)
    {
        return string.IsNullOrWhiteSpace(moduleSource)
            ? $"module {name}"
            : $"module {name} (source: {moduleSource})";
    }
}
