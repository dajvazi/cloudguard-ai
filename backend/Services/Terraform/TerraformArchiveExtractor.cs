using System.IO.Compression;

namespace CloudGuard.Api.Services.Terraform;

public class TerraformArchiveExtractor : ITerraformArchiveExtractor
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;
    private const int MaxFilesInArchive = 200;

    public async Task<IReadOnlyList<TerraformFileEntry>> ExtractAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        ValidateFile(file);

        if (file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            return await ExtractZipAsync(file, cancellationToken);

        if (file.FileName.EndsWith(".tf", StringComparison.OrdinalIgnoreCase))
        {
            var content = await ReadTextAsync(file, cancellationToken);
            return [new TerraformFileEntry(file.FileName, content)];
        }

        throw new ArgumentException("Lejohen vetëm skedarë .tf ose .zip.");
    }

    private static async Task<IReadOnlyList<TerraformFileEntry>> ExtractZipAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        var entries = new List<TerraformFileEntry>();

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.FullName.EndsWith('/')
                || !entry.Name.EndsWith(".tf", StringComparison.OrdinalIgnoreCase)
                || entry.FullName.Contains("__MACOSX", StringComparison.OrdinalIgnoreCase)
                || entry.FullName.StartsWith(".", StringComparison.Ordinal))
            {
                continue;
            }

            if (entries.Count >= MaxFilesInArchive)
                throw new ArgumentException($"Arkiva nuk mund të ketë më shumë se {MaxFilesInArchive} skedarë .tf.");

            await using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream);
            var content = await reader.ReadToEndAsync(cancellationToken);

            entries.Add(new TerraformFileEntry(entry.FullName, content));
        }

        if (entries.Count == 0)
            throw new ArgumentException("Arkiva nuk përmban asnjë skedar .tf.");

        return entries;
    }

    private static void ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
            throw new ArgumentException("Skedari është bosh.");

        if (file.Length > MaxFileSizeBytes)
            throw new ArgumentException("Skedari nuk mund të jetë më i madh se 10 MB.");
    }

    private static async Task<string> ReadTextAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
