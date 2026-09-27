using System.Text.Json.Serialization;

namespace ResearchPublications.Infrastructure.Search;

public class PublicationDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("abstract")]
    public string Abstract { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("authors")]
    public string[] Authors { get; set; } = [];

    [JsonPropertyName("keywords")]
    public string[] Keywords { get; set; } = [];

    [JsonPropertyName("languages")]
    public string[] Languages { get; set; } = [];

    [JsonPropertyName("publication_types")]
    public string[] PublicationTypes { get; set; } = [];

    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("doi")]
    public string Doi { get; set; } = string.Empty;

    [JsonPropertyName("pdf_file_name")]
    public string PdfFileName { get; set; } = string.Empty;

    [JsonPropertyName("last_modified_timestamp")]
    public long LastModifiedTimestamp { get; set; }

    [JsonPropertyName("content_hash")]
    public string ContentHash { get; set; } = string.Empty;

    [JsonPropertyName("ocr_text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OcrText { get; set; }

    [JsonPropertyName("ocr_source_file_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OcrSourceFileName { get; set; }

    [JsonPropertyName("ocr_source_hash")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OcrSourceHash { get; set; }

    [JsonPropertyName("ocr_pipeline_version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OcrPipelineVersion { get; set; }

    [JsonPropertyName("ocr_status")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OcrStatus { get; set; }

    [JsonPropertyName("ocr_error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OcrError { get; set; }

    [JsonPropertyName("ocr_retry_after_timestamp")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? OcrRetryAfterTimestamp { get; set; }
}
