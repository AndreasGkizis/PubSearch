using System.Text.Json;
using ResearchPublications.Infrastructure.Search;

namespace ResearchPublications.UnitTests.Infrastructure;

public sealed class PublicationDocumentTests
{
    [Fact]
    public void MetadataDocumentSerialization_OmitsUnownedOcrFields()
    {
        var document = new PublicationDocument
        {
            Id = "1",
            Title = "Metadata update",
            ContentHash = "hash"
        };

        var json = JsonSerializer.Serialize(document);

        Assert.DoesNotContain("ocr_text", json);
        Assert.DoesNotContain("ocr_status", json);
        Assert.DoesNotContain("ocr_source_hash", json);
    }
}
