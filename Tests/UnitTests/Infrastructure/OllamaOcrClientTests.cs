using System.Net;
using System.Text;
using System.Text.Json;
using ResearchPublications.Infrastructure.Ocr;
using ResearchPublications.Infrastructure.Settings;

namespace ResearchPublications.UnitTests.Infrastructure;

public sealed class OllamaOcrClientTests
{
    [Fact]
    public async Task ExtractMarkdownAsync_MapsChatRequestAndResponse()
    {
        string? requestJson = null;
        var handler = new StubHandler(async request =>
        {
            requestJson = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"message\":{\"role\":\"assistant\",\"content\":\"# Markdown\"}}",
                    Encoding.UTF8,
                    "application/json")
            };
        });
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://ollama/") };
        var client = new OllamaOcrClient(httpClient, new OcrSettings
        {
            Model = "frob/unlimited-ocr:3b",
            Prompt = "document parsing."
        });

        var result = await client.ExtractMarkdownAsync([1, 2, 3]);

        Assert.Equal("# Markdown", result);
        using var json = JsonDocument.Parse(requestJson!);
        Assert.Equal("frob/unlimited-ocr:3b", json.RootElement.GetProperty("model").GetString());
        Assert.False(json.RootElement.GetProperty("stream").GetBoolean());
        var message = json.RootElement.GetProperty("messages")[0];
        Assert.Equal("document parsing.", message.GetProperty("content").GetString());
        Assert.Equal(Convert.ToBase64String([1, 2, 3]), message.GetProperty("images")[0].GetString());
    }

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> callback)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => callback(request);
    }
}
