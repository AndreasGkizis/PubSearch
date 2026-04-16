using ResearchPublications.Application.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ResearchPublications.Infrastructure.Files;

public class PdfPigTextExtractor : IPdfTextExtractor
{
    public string Extract(Stream pdfStream)
    {
        try
        {
            using var doc = PdfDocument.Open(pdfStream);
            var sb = new System.Text.StringBuilder();

            foreach (var page in doc.GetPages())
            {
                foreach (var word in page.GetWords())
                    sb.Append(word.Text).Append(' ');

                sb.AppendLine();
            }

            return sb.ToString().Trim();
        }
        catch
        {
            return string.Empty;
        }
    }
}
