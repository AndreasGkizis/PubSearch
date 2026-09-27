using System.Text;
using ResearchPublications.Infrastructure.Ocr;

namespace ResearchPublications.UnitTests.Infrastructure;

public sealed class OcrFreshnessTests
{
    [Fact]
    public void IsCurrent_RequiresCompletedMatchingSourceAndPipeline()
    {
        Assert.True(OcrFreshness.IsCurrent(
            OcrStatus.Complete, "paper.pdf", "hash", "v1", "paper.pdf", "hash", "v1"));

        Assert.False(OcrFreshness.IsCurrent(
            OcrStatus.Complete, "paper.pdf", "hash", "v1", "paper.pdf", "hash", "v2"));
    }

    [Fact]
    public void CalculateHash_IsStableAndChangesWithContent()
    {
        var first = OcrHash.Calculate(Encoding.UTF8.GetBytes("first"));
        var repeated = OcrHash.Calculate(Encoding.UTF8.GetBytes("first"));
        var second = OcrHash.Calculate(Encoding.UTF8.GetBytes("second"));

        Assert.Equal(first, repeated);
        Assert.NotEqual(first, second);
        Assert.Equal(64, first.Length);
    }

    [Fact]
    public void ShouldDeferRetry_OnlyDefersCurrentFailedSourceUntilTimestamp()
    {
        Assert.True(OcrFreshness.ShouldDeferRetry(
            OcrStatus.Failed, "paper.pdf", "hash", "v1", 200,
            "paper.pdf", "hash", "v1", 100));

        Assert.False(OcrFreshness.ShouldDeferRetry(
            OcrStatus.Failed, "paper.pdf", "hash", "v1", 200,
            "replacement.pdf", "new-hash", "v1", 100));

        Assert.False(OcrFreshness.ShouldDeferRetry(
            OcrStatus.Failed, "paper.pdf", "hash", "v1", 100,
            "paper.pdf", "hash", "v1", 100));
    }
}
