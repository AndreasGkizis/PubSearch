namespace ResearchPublications.Infrastructure.Settings;

public sealed class OcrSettings
{
    public bool Enabled { get; set; } = true;
    public string OllamaBaseUrl { get; set; } = "http://localhost:11435";
    public string Model { get; set; } = "frob/unlimited-ocr:3b";
    public string Prompt { get; set; } = "document parsing.";
    public int Dpi { get; set; } = 200;
    public int IntervalSeconds { get; set; } = 5;
    public int RetrySeconds { get; set; } = 300;

    public string PipelineVersion => $"{Model}|pdf-to-image-5.4.0|dpi-{Dpi}|{Prompt}";
}
