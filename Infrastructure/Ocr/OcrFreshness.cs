namespace ResearchPublications.Infrastructure.Ocr;

internal static class OcrFreshness
{
    internal static bool IsCurrent(
        string? status,
        string? sourceFileName,
        string? sourceHash,
        string? pipelineVersion,
        string expectedFileName,
        string expectedHash,
        string expectedPipelineVersion) =>
        status == OcrStatus.Complete
        && string.Equals(sourceFileName, expectedFileName, StringComparison.Ordinal)
        && string.Equals(sourceHash, expectedHash, StringComparison.Ordinal)
        && string.Equals(pipelineVersion, expectedPipelineVersion, StringComparison.Ordinal);

    internal static bool ShouldDeferRetry(
        string? status,
        string? sourceFileName,
        string? sourceHash,
        string? pipelineVersion,
        long? retryAfterTimestamp,
        string expectedFileName,
        string expectedHash,
        string expectedPipelineVersion,
        long nowTimestamp) =>
        status == OcrStatus.Failed
        && retryAfterTimestamp > nowTimestamp
        && string.Equals(sourceFileName, expectedFileName, StringComparison.Ordinal)
        && string.Equals(sourceHash, expectedHash, StringComparison.Ordinal)
        && string.Equals(pipelineVersion, expectedPipelineVersion, StringComparison.Ordinal);
}

internal static class OcrStatus
{
    internal const string Complete = "complete";
    internal const string Failed = "failed";
    internal const string Missing = "missing";
    internal const string Processing = "processing";
}
