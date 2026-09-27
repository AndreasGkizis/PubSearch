using System.Runtime.CompilerServices;
using ResearchPublications.Infrastructure.Ocr;
using ResearchPublications.Infrastructure.Settings;

namespace ResearchPublications.UnitTests.Infrastructure;

public sealed class OcrPdfProcessorTests
{
    [Fact]
    public async Task ProcessAsync_SubmitsAndJoinsPagesInOrder()
    {
        var renderer = new FakeRenderer(["page-1"u8.ToArray(), "page-2"u8.ToArray()]);
        var ollama = new FakeOllama();
        var processor = new OcrPdfProcessor(renderer, ollama, new OcrSettings { Dpi = 200 });

        var markdown = await processor.ProcessAsync(new MemoryStream("pdf"u8.ToArray()));

        Assert.Equal("markdown-page-1\n\nmarkdown-page-2", markdown);
        Assert.Equal(["page-1", "page-2"], ollama.Requests);
        Assert.Equal(200, renderer.ReceivedDpi);
    }

    private sealed class FakeRenderer(IReadOnlyList<byte[]> pages) : IOcrPageRenderer
    {
        public int ReceivedDpi { get; private set; }

        public async IAsyncEnumerable<byte[]> RenderPngPagesAsync(
            Stream pdfStream,
            int dpi,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            ReceivedDpi = dpi;
            foreach (var page in pages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return page;
                await Task.Yield();
            }
        }
    }

    private sealed class FakeOllama : IOllamaOcrClient
    {
        public List<string> Requests { get; } = [];

        public Task<string> ExtractMarkdownAsync(byte[] pngBytes, CancellationToken cancellationToken = default)
        {
            var page = System.Text.Encoding.UTF8.GetString(pngBytes);
            Requests.Add(page);
            return Task.FromResult($"markdown-{page}");
        }
    }
}
