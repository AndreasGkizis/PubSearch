using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using ResearchPublications.Application.Interfaces;
using ResearchPublications.Domain.Interfaces;
using ResearchPublications.Infrastructure.Search;
using ResearchPublications.Infrastructure.Settings;
using Typesense;

namespace ResearchPublications.Infrastructure.Ocr;

internal sealed class TypesenseOcrIndexingService(
    ITypesenseClient typesense,
    IFileService fileService,
    IOcrPdfProcessor processor,
    OcrSettings settings,
    TimeProvider timeProvider,
    ILogger<TypesenseOcrIndexingService> logger) : IOcrIndexingService
{
    private const string CollectionName = "publications";

    public async Task ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PublicationDocument> documents;
        try
        {
            documents = await typesense.ExportDocuments<PublicationDocument>(
                CollectionName,
                new ExportParameters
                {
                    IncludeFields = "id,pdf_file_name,ocr_text,ocr_source_file_name,ocr_source_hash," +
                        "ocr_pipeline_version,ocr_status,ocr_error,ocr_retry_after_timestamp"
                },
                cancellationToken);
        }
        catch (TypesenseApiNotFoundException)
        {
            return;
        }

        foreach (var document in documents.OrderBy(item => int.Parse(item.Id)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ProcessDocumentAsync(document, cancellationToken);
        }
    }

    private async Task ProcessDocumentAsync(
        PublicationDocument document,
        CancellationToken cancellationToken)
    {
        var fileName = document.PdfFileName;
        if (string.IsNullOrWhiteSpace(fileName) || !fileService.Exists(fileName))
        {
            if (document.OcrStatus != OcrStatus.Missing
                || !string.IsNullOrEmpty(document.OcrText)
                || !string.IsNullOrEmpty(document.OcrSourceFileName))
            {
                await UpdateAsync(document.Id, new OcrDocumentUpdate
                {
                    OcrText = string.Empty,
                    OcrSourceFileName = string.Empty,
                    OcrSourceHash = string.Empty,
                    OcrPipelineVersion = settings.PipelineVersion,
                    OcrStatus = OcrStatus.Missing,
                    OcrError = string.Empty,
                    OcrRetryAfterTimestamp = 0
                });
            }
            return;
        }

        byte[] pdfBytes;
        await using (var pdf = await fileService.GetPdfAsync(fileName)
            ?? throw new FileNotFoundException("PDF disappeared before OCR processing.", fileName))
        {
            using var buffer = new MemoryStream();
            await pdf.CopyToAsync(buffer, cancellationToken);
            pdfBytes = buffer.ToArray();
        }

        var hash = OcrHash.Calculate(pdfBytes);
        var version = settings.PipelineVersion;
        var now = timeProvider.GetUtcNow().ToUnixTimeSeconds();

        if (OcrFreshness.IsCurrent(
            document.OcrStatus,
            document.OcrSourceFileName,
            document.OcrSourceHash,
            document.OcrPipelineVersion,
            fileName,
            hash,
            version))
        {
            return;
        }

        if (OcrFreshness.ShouldDeferRetry(
            document.OcrStatus,
            document.OcrSourceFileName,
            document.OcrSourceHash,
            document.OcrPipelineVersion,
            document.OcrRetryAfterTimestamp,
            fileName,
            hash,
            version,
            now))
        {
            return;
        }

        await UpdateAsync(document.Id, new OcrDocumentUpdate
        {
            OcrText = string.Empty,
            OcrSourceFileName = fileName,
            OcrSourceHash = hash,
            OcrPipelineVersion = version,
            OcrStatus = OcrStatus.Processing,
            OcrError = string.Empty,
            OcrRetryAfterTimestamp = 0
        });

        try
        {
            await using var pdf = new MemoryStream(pdfBytes, writable: false);
            var markdown = await processor.ProcessAsync(pdf, cancellationToken);
            await UpdateAsync(document.Id, new OcrDocumentUpdate
            {
                OcrText = markdown,
                OcrSourceFileName = fileName,
                OcrSourceHash = hash,
                OcrPipelineVersion = version,
                OcrStatus = OcrStatus.Complete,
                OcrError = string.Empty,
                OcrRetryAfterTimestamp = 0
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var error = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            await UpdateAsync(document.Id, new OcrDocumentUpdate
            {
                OcrText = string.Empty,
                OcrSourceFileName = fileName,
                OcrSourceHash = hash,
                OcrPipelineVersion = version,
                OcrStatus = OcrStatus.Failed,
                OcrError = error,
                OcrRetryAfterTimestamp = now + Math.Max(1, settings.RetrySeconds)
            });
            logger.LogWarning(ex, "OCR failed for publication {PublicationId}; metadata remains searchable.", document.Id);
        }
    }

    private Task<OcrDocumentUpdate> UpdateAsync(string id, OcrDocumentUpdate update) =>
        typesense.UpdateDocument(CollectionName, id, update);

    private sealed class OcrDocumentUpdate
    {
        [JsonPropertyName("ocr_text")]
        public string OcrText { get; init; } = string.Empty;

        [JsonPropertyName("ocr_source_file_name")]
        public string OcrSourceFileName { get; init; } = string.Empty;

        [JsonPropertyName("ocr_source_hash")]
        public string OcrSourceHash { get; init; } = string.Empty;

        [JsonPropertyName("ocr_pipeline_version")]
        public string OcrPipelineVersion { get; init; } = string.Empty;

        [JsonPropertyName("ocr_status")]
        public string OcrStatus { get; init; } = string.Empty;

        [JsonPropertyName("ocr_error")]
        public string OcrError { get; init; } = string.Empty;

        [JsonPropertyName("ocr_retry_after_timestamp")]
        public long OcrRetryAfterTimestamp { get; init; }
    }
}
