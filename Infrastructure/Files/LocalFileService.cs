using Microsoft.Extensions.Configuration;
using ResearchPublications.Domain.Interfaces;
using ResearchPublications.Infrastructure.Constants;

namespace ResearchPublications.Infrastructure.Files;

public class LocalFileService : IFileService
{
    private readonly string _basePath;

    public LocalFileService(IConfiguration configuration)
    {
        var path = configuration[ConfigKeys.PdfStoragePath]
            ?? throw new InvalidOperationException($"{ConfigKeys.PdfStoragePath} is not configured.");

        // Resolve and normalise once at startup
        _basePath = Path.GetFullPath(path);
    }

    public bool Exists(string fileName) => File.Exists(Resolve(fileName));

    public Task<Stream?> GetPdfAsync(string fileName)
    {
        var fullPath = Resolve(fileName);

        if (!File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, useAsync: true);
        return Task.FromResult<Stream?>(stream);
    }

    public async Task<string> SavePdfAsync(Stream content, string originalFileName)
    {
        // Sanitise and ensure a unique name to prevent collisions
        var safeName    = Path.GetFileName(originalFileName);
        var uniqueName  = $"{Guid.NewGuid():N}_{safeName}";
        var destination = Path.GetFullPath(Path.Combine(_basePath, uniqueName));

        // Guard against path-traversal even for the destination
        if (!destination.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Resolved path is outside the configured storage directory.");

        Directory.CreateDirectory(_basePath); // ensure folder exists

        await using var fs = new FileStream(destination, FileMode.CreateNew, FileAccess.Write,
            FileShare.None, bufferSize: 81920, useAsync: true);
        await content.CopyToAsync(fs);

        return uniqueName;
    }

    public Task DeletePdfAsync(string fileName)
    {
        var fullPath = Resolve(fileName);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    // ── Private ────────────────────────────────────────────────────────────

    private string Resolve(string fileName)
    {
        // Security: prevent path traversal — reject anything that escapes the base
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name must not be empty.", nameof(fileName));

        // Strip any directory components the caller might have injected
        var safeFileName = Path.GetFileName(fileName);
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, safeFileName));

        if (!fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Resolved path is outside the configured storage directory.");

        return fullPath;
    }
}
