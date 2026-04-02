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
}
