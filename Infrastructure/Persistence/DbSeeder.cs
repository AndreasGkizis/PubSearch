using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Domain.Interfaces;

namespace ResearchPublications.Infrastructure.Persistence;

public class DbSeeder(AppDbCntx context, IFileService fileService, ILogger<DbSeeder> logger)
{
    private const int Seed = 12345;
    private const string UiFixtureKeyword = "UI Test Fixture";
    private const string UiFixtureTitlePrefix = "UI Fixture ";

    private static readonly DateTime FixedTimestamp =
        new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly string[] KeywordPool =
    [
        "Mosaic Conservation",
        "Tesserae",
        "Grouting Techniques",
        "Roman Mosaics",
        "Byzantine Mosaics",
        "Opus Tessellatum",
        "Stone Restoration",
        "Lime Mortar",
        "Polychromy",
        "Archaeological Preservation",
        "In-Situ Conservation",
        "Detachment Methods",
        "Preventive Conservation",
        "Cultural Heritage",
        "Iconographic Analysis",
        "Adhesive Consolidation",
        "Vitreous Enamel",
        "Substrate Stabilization",
        "Digital Documentation",
        "Photogrammetry",
        "Mosaic Lifting",
        "Environmental Degradation",
        "Salt Crystallization",
        "Opus Vermiculatum",
        "Microclimate Monitoring",
        "Fresco Restoration",
        "Heritage Site Management",
        "Ceramic Analysis",
        "Pigment Identification",
        "Mortar Characterization",
        "Non-Destructive Testing",
        "X-Ray Fluorescence",
        "Raman Spectroscopy",
        "Petrographic Analysis",
        "Scanning Electron Microscopy",
        "3D Laser Scanning",
        "GIS Mapping",
        "Remote Sensing",
        "Stratigraphic Analysis",
        "Wall Painting Conservation",
        "Underwater Archaeology",
        "Ethnoarchaeology",
        "Museum Conservation",
        "Artifact Preservation",
        "Stone Weathering",
        "Biological Colonization",
        "Biocide Treatment",
        "Freeze-Thaw Resistance",
        "Thermal Cycling",
        "Moisture Migration",
        "Rising Damp",
        "Capillary Action",
        "Hydraulic Lime",
        "Pozzolanic Materials",
        "Portland Cement Repair",
        "Injection Grouting",
        "Consolidation Resins",
        "Acrylic Polymers",
        "Silicone Treatments",
        "Nanoparticle Consolidants",
        "Bio-Based Consolidants",
        "Sacrificial Layers",
        "Protective Sheltering",
        "Reburial Techniques",
        "Climate Change Adaptation",
        "Visitor Impact Assessment",
        "Site Interpretation",
        "Community Engagement",
        "Intangible Heritage",
        "Conservation Ethics",
        "Minimal Intervention",
        "Reversibility Principle",
        "Authenticity Assessment",
        "Condition Mapping",
        "Decay Pattern Analysis",
        "Color Measurement",
        "Spectrophotometry",
        "Infrared Thermography",
        "Ground Penetrating Radar",
        "Ultrasonic Testing",
        "Mechanical Testing",
        "Porosity Measurement",
        "Water Absorption Testing",
        "Adhesion Strength",
        "Compressive Strength",
        "Accelerated Aging",
        "Natural Aging Studies",
        "Case Study Documentation",
        "Conservation Database",
        "Open Access Publishing",
        "Interdisciplinary Methods",
        "Training Programs",
        "Capacity Building",
        "Risk Assessment",
        "Disaster Preparedness",
        "Post-Earthquake Conservation",
        "Flood Damage Mitigation",
        "Fire Damage Assessment",
        "Vandalism Prevention",
        "Looting Prevention",
        "Legal Frameworks",
        "International Conventions"
    ];

    private static readonly string[] LanguagePool =
    [
        "English",
        "French",
        "German",
        "Arabic",
        "Spanish",
        "Italian"
    ];

    private static readonly string[] PublicationTypePool =
    [
        "Journal",
        "Book",
        "Conference Paper",
        "Thesis",
        "Report"
    ];

    private static readonly string[] TitlePrefixes =
    [
        "Advances in",
        "A Comparative Study of",
        "Techniques for",
        "Characterization of",
        "New Approaches to",
        "Evaluating",
        "Long-Term Monitoring of",
        "The Role of",
        "Sustainable Methods for",
        "Digital Tools for"
    ];

    private static readonly string[] TitleSubjects =
    [
        "Tesserae Consolidation in Ancient Floor Mosaics",
        "Grouting Materials for In-Situ Mosaic Conservation",
        "Polychrome Stone Deterioration in Roman Mosaics",
        "Byzantine Mosaic Preservation Using Lime-Based Mortars",
        "Detachment and Relocation of Endangered Floor Mosaics",
        "Salt Crystallization Damage in Archaeological Mosaics",
        "Photogrammetric Recording of Opus Tessellatum Surfaces",
        "Preventive Conservation Strategies for Exposed Mosaics",
        "Adhesive Consolidation Techniques for Loose Tesserae",
        "Microclimate Effects on Mosaic Substrate Stability",
        "Environmental Degradation Patterns in Coastal Mosaics",
        "Iconographic Analysis of Late Antique Mosaic Panels"
    ];

    private static readonly string[] IntroSentences =
    [
        "This study examines the long-term effectiveness of conservation treatments applied to ancient mosaic pavements across the Mediterranean basin.",
        "The preservation of archaeological mosaics represents one of the most challenging tasks in the field of cultural heritage conservation today.",
        "Ancient mosaics are among the most fragile and complex composite artifacts surviving from antiquity, requiring interdisciplinary conservation approaches.",
        "Despite decades of research, the deterioration mechanisms affecting mosaic tesserae and their binding matrices remain only partially understood.",
        "The growing number of exposed mosaic sites worldwide demands scalable, evidence-based conservation strategies that balance accessibility with preservation.",
        "Recent advances in materials science and digital imaging have opened new avenues for non-invasive analysis and documentation of mosaic surfaces.",
        "This paper addresses a critical gap in the literature concerning the compatibility of modern repair materials with original Roman-era substrates.",
        "In-situ conservation of floor mosaics presents unique challenges related to environmental exposure, visitor traffic, and limited intervention windows."
    ];

    private static readonly string[] MethodSentences =
    [
        "Laboratory and field analyses were conducted on tesserae samples collected from multiple archaeological sites in Italy, Tunisia, and Jordan.",
        "Digital photogrammetry was employed to create high-resolution 3D models of the mosaic surfaces before, during, and after each conservation intervention.",
        "Accelerated ageing tests were performed under controlled temperature and humidity conditions to evaluate the durability of selected consolidation products.",
        "Salt crystallization cycles were simulated in laboratory conditions to assess damage mechanisms in porous stone substrates and lime-based bedding layers.",
        "Spectroscopic analysis, including X-ray fluorescence and Raman spectroscopy, revealed the original pigment composition of the polychrome tesserae.",
        "Fieldwork was carried out at three UNESCO World Heritage sites between 2018 and 2023, encompassing both emergency stabilization and planned conservation campaigns.",
        "Comparative analysis of six commercially available adhesive systems was performed using standardized peel-strength and reversibility testing protocols.",
        "Microclimate data loggers were installed at each site to continuously record temperature, relative humidity, and solar radiation over a twenty-four-month period.",
        "Thin-section petrography and scanning electron microscopy were used to characterize the mineralogical composition and porosity of the original mortars.",
        "A systematic survey of existing conservation records was conducted to establish a baseline assessment of prior treatments and their observable outcomes."
    ];

    private static readonly string[] ResultSentences =
    [
        "Results indicate that lime-based grouts significantly outperform synthetic polymer alternatives in long-term compatibility with original calcareous materials.",
        "The research highlights the critical importance of continuous microclimate monitoring for effective preventive conservation planning at exposed mosaic sites.",
        "The findings demonstrate that reversible consolidation approaches maintain structural integrity while preserving future retreatability of the treated surfaces.",
        "Quantitative image analysis revealed that tesserae loss rates were reduced by sixty-two percent in areas treated with the proposed consolidation protocol.",
        "Cross-disciplinary collaboration between conservators, archaeologists, materials scientists, and environmental engineers proved essential for successful project outcomes.",
        "The study identifies three previously undocumented deterioration patterns specific to coastal mosaics subject to marine aerosol deposition and thermal cycling.",
        "Data from the ageing tests confirm that hydraulic lime mortars exhibit superior resistance to freeze-thaw cycles compared to Portland cement-based repairs.",
        "Photogrammetric monitoring detected sub-millimeter surface displacement in several mosaic panels, providing early warning of potential structural failure.",
        "The classification system proposed here categorizes deterioration into twelve distinct types based on morphology, spatial distribution, and probable causative agents.",
        "Statistical analysis of the microclimate datasets revealed strong correlations between diurnal temperature fluctuations and the progression of salt-related damage."
    ];

    private static readonly string[] ConclusionSentences =
    [
        "The findings contribute to the development of evidence-based guidelines for in-situ mosaic conservation that can be adapted to diverse environmental contexts.",
        "A novel detachment protocol is proposed that minimizes mechanical stress on fragile tesserae while enabling safe relocation to controlled storage environments.",
        "Sustainable conservation practices were prioritized throughout the project to reduce the environmental footprint of large-scale restoration campaigns.",
        "These results underscore the need for long-term monitoring programs that extend well beyond the immediate post-treatment period to capture delayed deterioration effects.",
        "Further research is recommended to investigate the performance of bio-based consolidants as environmentally friendly alternatives to conventional synthetic resins.",
        "The methodological framework developed in this study is directly transferable to other mosaic typologies, including wall mosaics and fountain decorations.",
        "This work demonstrates that integrating digital documentation tools into routine conservation workflows improves both the accuracy and reproducibility of condition assessments.",
        "The authors advocate for the establishment of an international mosaic conservation database to facilitate knowledge sharing and comparative analysis across sites and regions."
    ];

    private static readonly UiSearchFixture[] UiSearchFixtures =
    [
        new("UI Fixture 01 - Abstract Only",
            "The abstract contains the unique abstractonlyamber marker. The body and PDF deliberately do not.",
            null, null, 2021, "English", "Journal"),
        new("UI Fixture 02 - Body Only",
            "A control abstract about conservation documentation without the distinctive body marker.",
            "The body contains the unique bodyonlycobalt marker for full-text search.",
            null, 2022, "English", "Journal"),
        new("UI Fixture 03 - OCR Only",
            "A control abstract about a scanned conservation record.",
            null,
            "This scanned UI test page contains the distinctive ocronlyjade marker and no matching catalog metadata.",
            2020, "English", "Journal"),
        new("UI Fixture 04 - Abstract and OCR",
            "The abstract contains dualfieldquartz, which also appears on the scanned page.",
            null,
            "Scanned page marker dualfieldquartz. This term is repeated in the abstract for snippet comparison.",
            2023, "English", "Journal"),
        new("UI Fixture 05 - Abstract Ranking Match",
            "The abstract contains rankingsapphire so abstract and OCR result ranking can be compared.",
            null, null, 2024, "English", "Journal"),
        new("UI Fixture 06 - OCR Ranking Match",
            "A control abstract for the lower-priority OCR ranking comparison.",
            null,
            "Scanned page marker rankingsapphire. This fixture should rank below the abstract match.",
            2024, "English", "Journal"),
        new("UI Fixture 07 - Title Only titleonlycoral",
            "A control abstract without the title-only search marker.",
            null, null, 2019, "English", "Journal"),
        new("UI Fixture 08 - Keyword Only",
            "A control abstract without the keyword-only search marker.",
            null, null, 2021, "English", "Journal", ExtraKeyword: "keywordonlycedar"),
        new("UI Fixture 09 - Author Only",
            "A control abstract for checking author-field matches.",
            null, null, 2022, "English", "Journal", AuthorOnly: true),
        new("UI Fixture 10 - Filter Match",
            "The abstract contains filteronlycopper for testing year, language, and type filters.",
            null, null, 2020, "French", "Report")
    ];

    public async Task SeedAsync()
    {
        if (await context.Publications.AnyAsync())
        {
            logger.LogInformation("Base seed skipped — data already exists.");
        }
        else
        {
            Randomizer.Seed = new Random(Seed);

            var authors = GenerateAuthors(50);
            var keywords = GenerateKeywords(100);
            var languages = GenerateLanguages(LanguagePool.Length);
            var publicationTypes = GeneratePublicationTypes(PublicationTypePool.Length);
            var publications = GeneratePublications(150, authors, keywords, languages, publicationTypes);

            await GeneratePdfFilesAsync(publications);

            context.Authors.AddRange(authors);
            context.Keywords.AddRange(keywords);
            context.Languages.AddRange(languages);
            context.PublicationTypes.AddRange(publicationTypes);
            context.Publications.AddRange(publications);

            await context.SaveChangesAsync();
            logger.LogInformation(
                "Seeded {Authors} authors, {Keywords} keywords, {Languages} languages, {PublicationTypes} publication types, {Publications} publications.",
                authors.Count, keywords.Count, languages.Count, publicationTypes.Count, publications.Count);
        }

        await SeedUiSearchFixturesAsync();
    }

    private async Task SeedUiSearchFixturesAsync()
    {
        var existingTitles = await context.Publications
            .Where(publication => publication.Title.StartsWith(UiFixtureTitlePrefix))
            .Select(publication => publication.Title)
            .ToHashSetAsync();
        var missing = UiSearchFixtures.Where(fixture => !existingTitles.Contains(fixture.Title)).ToList();
        if (missing.Count == 0)
            return;

        var defaultAuthor = await GetOrCreateAuthorAsync("UI Fixture", "Author");
        var authorOnly = await GetOrCreateAuthorAsync("Fixture", "AuthorOnlyLapis");
        var fixtureKeyword = await GetOrCreateKeywordAsync(UiFixtureKeyword);
        foreach (var fixture in missing)
        {
            var language = await GetOrCreateLanguageAsync(fixture.Language);
            var publicationType = await GetOrCreatePublicationTypeAsync(fixture.PublicationType);
            var keywords = new List<Keyword> { fixtureKeyword };
            if (fixture.ExtraKeyword is not null)
                keywords.Add(await GetOrCreateKeywordAsync(fixture.ExtraKeyword));

            var publication = new Publication
            {
                Title = fixture.Title,
                Abstract = fixture.Abstract,
                Body = fixture.Body,
                Year = fixture.Year,
                Authors = [fixture.AuthorOnly ? authorOnly : defaultAuthor],
                Keywords = keywords,
                Languages = [language],
                PublicationTypes = [publicationType],
                CreatedAt = FixedTimestamp,
                LastModified = FixedTimestamp
            };

            if (fixture.PdfBody is not null)
            {
                var seedPdf = SeedPdfGenerator.Generate(fixture.PdfBody);
                using var stream = new MemoryStream(seedPdf.PdfBytes, writable: false);
                var safeFileName = SanitizeFileName(fixture.FileName);
                publication.PdfFileName = await fileService.SavePdfAsync(stream, safeFileName);
            }

            context.Publications.Add(publication);
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} focused UI search fixtures.", missing.Count);
    }

    private async Task<Author> GetOrCreateAuthorAsync(string firstName, string lastName)
    {
        var local = context.Authors.Local.FirstOrDefault(author =>
            author.FirstName == firstName && author.LastName == lastName);
        if (local is not null)
            return local;

        var existing = await context.Authors.FirstOrDefaultAsync(author =>
            author.FirstName == firstName && author.LastName == lastName);
        if (existing is not null)
            return existing;

        var author = new Author
        {
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = FixedTimestamp,
            LastModified = FixedTimestamp
        };
        context.Authors.Add(author);
        return author;
    }

    private async Task<Keyword> GetOrCreateKeywordAsync(string value)
    {
        var local = context.Keywords.Local.FirstOrDefault(keyword => keyword.Value == value);
        if (local is not null)
            return local;

        var existing = await context.Keywords.FirstOrDefaultAsync(keyword => keyword.Value == value);
        if (existing is not null)
            return existing;

        var keyword = new Keyword { Value = value, CreatedAt = FixedTimestamp, LastModified = FixedTimestamp };
        context.Keywords.Add(keyword);
        return keyword;
    }

    private async Task<Language> GetOrCreateLanguageAsync(string value)
    {
        var local = context.Languages.Local.FirstOrDefault(language => language.Value == value);
        if (local is not null)
            return local;

        var existing = await context.Languages.FirstOrDefaultAsync(language => language.Value == value);
        if (existing is not null)
            return existing;

        var language = new Language { Value = value, CreatedAt = FixedTimestamp, LastModified = FixedTimestamp };
        context.Languages.Add(language);
        return language;
    }

    private async Task<PublicationType> GetOrCreatePublicationTypeAsync(string value)
    {
        var local = context.PublicationTypes.Local.FirstOrDefault(type => type.Value == value);
        if (local is not null)
            return local;

        var existing = await context.PublicationTypes.FirstOrDefaultAsync(type => type.Value == value);
        if (existing is not null)
            return existing;

        var type = new PublicationType { Value = value, CreatedAt = FixedTimestamp, LastModified = FixedTimestamp };
        context.PublicationTypes.Add(type);
        return type;
    }

    private List<Author> GenerateAuthors(int count)
    {
        var faker = new Faker<Author>()
            .RuleFor(a => a.FirstName, f => f.Name.FirstName())
            .RuleFor(a => a.MiddleName, f => f.Random.Bool(0.3f) ? f.Name.FirstName() : null)
            .RuleFor(a => a.LastName, f => f.Name.LastName())
            .RuleFor(a => a.Email, (f, a) => f.Internet.Email(a.FirstName, a.LastName))
            .RuleFor(a => a.CreatedAt, _ => FixedTimestamp)
            .RuleFor(a => a.LastModified, _ => FixedTimestamp);

        return faker.Generate(count);
    }

    private List<Keyword> GenerateKeywords(int count)
    {
        var faker = new Faker();
        var selected = faker.PickRandom(KeywordPool, count).ToList();

        return selected.Select(value => new Keyword
        {
            Value = value,
            CreatedAt = FixedTimestamp,
            LastModified = FixedTimestamp
        }).ToList();
    }

    private List<Language> GenerateLanguages(int count)
    {
        var faker = new Faker();
        var selected = faker.PickRandom(LanguagePool, count).ToList();

        return selected.Select(value => new Language
        {
            Value = value,
            CreatedAt = FixedTimestamp,
            LastModified = FixedTimestamp
        }).ToList();
    }

    private List<PublicationType> GeneratePublicationTypes(int count)
    {
        var faker = new Faker();
        var selected = faker.PickRandom(PublicationTypePool, count).ToList();

        return selected.Select(value => new PublicationType
        {
            Value = value,
            CreatedAt = FixedTimestamp,
            LastModified = FixedTimestamp
        }).ToList();
    }

    private List<Publication> GeneratePublications(
        int count, List<Author> authors, List<Keyword> keywords,
        List<Language> languages, List<PublicationType> publicationTypes)
    {
        var faker = new Faker<Publication>()
            .RuleFor(p => p.Title, f =>
                $"{f.PickRandom(TitlePrefixes)} {f.PickRandom(TitleSubjects)}")
            .RuleFor(p => p.Abstract, f => GenerateAbstract(f))
            .RuleFor(p => p.Body, _ => null)
            .RuleFor(p => p.Year, f => f.Random.Int(2015, 2025))
            .RuleFor(p => p.DOI, f =>
                $"10.{f.Random.Int(1000, 9999)}/{f.Random.AlphaNumeric(8)}")
            .RuleFor(p => p.Authors, f =>
                f.PickRandom(authors, f.Random.Int(1, 3)).ToList())
            .RuleFor(p => p.Keywords, f =>
                f.PickRandom(keywords, f.Random.Int(1, 4)).ToList())
            .RuleFor(p => p.Languages, f =>
                f.PickRandom(languages, f.Random.Int(1, 2)).ToList())
            .RuleFor(p => p.PublicationTypes, f =>
                new List<PublicationType> { f.PickRandom(publicationTypes) })
            .RuleFor(p => p.CreatedAt, _ => FixedTimestamp)
            .RuleFor(p => p.LastModified, _ => FixedTimestamp);

        return faker.Generate(count);
    }

    private static string GenerateAbstract(Faker f)
    {
        var sections = new List<string>();

        // Pick 1-2 intro, 2-3 method, 2-3 result, 1-2 conclusion sentences
        sections.AddRange(f.PickRandom(IntroSentences, f.Random.Int(1, 2)));
        sections.AddRange(f.PickRandom(MethodSentences, f.Random.Int(2, 3)));
        sections.AddRange(f.PickRandom(ResultSentences, f.Random.Int(2, 3)));
        sections.AddRange(f.PickRandom(ConclusionSentences, f.Random.Int(1, 2)));

        var abstractText = string.Join(" ", sections);

        // If still under 150 words, pad with extra method + result sentences
        while (WordCount(abstractText) < 150)
        {
            sections.Add(f.PickRandom(MethodSentences));
            sections.Add(f.PickRandom(ResultSentences));
            abstractText = string.Join(" ", sections);
        }

        return abstractText;
    }

    private async Task GeneratePdfFilesAsync(List<Publication> publications)
    {
        for (var index = 0; index < publications.Count; index++)
        {
            var pub = publications[index];
            var safeName = SanitizeFileName(pub.Title) + ".pdf";
            var seedPdf = SeedPdfGenerator.Generate(index + 1);
            using var stream = new MemoryStream(seedPdf.PdfBytes, writable: false);
            pub.PdfFileName = await fileService.SavePdfAsync(stream, safeName);
        }

        logger.LogInformation("Generated {Count} PDF files.", publications.Count);
    }

    private static string SanitizeFileName(string title)
    {
        var safe = title.ToLowerInvariant()
            .Replace(' ', '-');

        foreach (var c in Path.GetInvalidFileNameChars())
            safe = safe.Replace(c, '_');

        return safe.Length > 80 ? safe[..80] : safe;
    }

    private static int WordCount(string text) =>
        text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

    private sealed record UiSearchFixture(
        string Title,
        string Abstract,
        string? Body,
        string? PdfBody,
        int Year,
        string Language,
        string PublicationType,
        string? ExtraKeyword = null,
        bool AuthorOnly = false)
    {
        public string FileName => Title.Replace("UI Fixture ", "ui-test-fixture-") + ".pdf";
    }
}
