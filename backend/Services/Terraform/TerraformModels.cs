namespace CloudGuard.Api.Services.Terraform;

public record TerraformFileEntry(string RelativePath, string Content);

public record ParsedTerraformResource(
    string Name,
    string ResourceType,
    string DisplayType,
    string Description,
    string SourceKind,
    string SourceFile,
    string? ModuleSource,
    string? ParentModule);

public record ParsedModuleDeclaration(
    string Name,
    string Source,
    string DeclaringFile);
