namespace CloudGuard.Api.Services.Terraform;

public interface ITerraformParserService
{
    IReadOnlyList<ParsedTerraformResource> ParseFile(string relativePath, string content, string? parentModule);
}

public interface ITerraformProjectParser
{
    IReadOnlyList<ParsedTerraformResource> ParseProject(IReadOnlyList<TerraformFileEntry> files);
}

public interface ITerraformArchiveExtractor
{
    Task<IReadOnlyList<TerraformFileEntry>> ExtractAsync(IFormFile file, CancellationToken cancellationToken = default);
}
