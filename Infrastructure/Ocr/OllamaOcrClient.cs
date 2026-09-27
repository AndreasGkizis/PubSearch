using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ResearchPublications.Infrastructure.Settings;

namespace ResearchPublications.Infrastructure.Ocr;

internal sealed class OllamaOcrClient(HttpClient httpClient, OcrSettings settings) : IOllamaOcrClient
{
    public async Task<string> ExtractMarkdownAsync(
        byte[] pngBytes,
        CancellationToken cancellationToken = default)
    {
        var request = new OllamaChatRequest(
            settings.Model,
            [new OllamaMessage("user", settings.Prompt, [Convert.ToBase64String(pngBytes)])],
            Stream: false);

        using var response = await httpClient.PostAsJsonAsync("api/chat", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Ollama returned an empty response.");

        if (string.IsNullOrWhiteSpace(result.Message?.Content))
            throw new InvalidOperationException("Ollama returned no OCR content.");

        return result.Message.Content.Trim();
    }

    private sealed record OllamaChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] OllamaMessage[] Messages,
        [property: JsonPropertyName("stream")] bool Stream);

    private sealed record OllamaMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content,
        [property: JsonPropertyName("images")]
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string[]? Images = null);

    private sealed record OllamaChatResponse(
        [property: JsonPropertyName("message")] OllamaMessage? Message);
}
