using System.Security.Cryptography;

namespace ResearchPublications.Infrastructure.Ocr;

internal static class OcrHash
{
    internal static string Calculate(byte[] content) =>
        Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
}
