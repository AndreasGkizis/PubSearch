using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResearchPublications.Domain.Entities;

namespace ResearchPublications.Infrastructure.Persistence;

public class DbSeeder(AppDbCntx context, ILogger<DbSeeder> logger)
{
    private const int Seed = 12345;

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
        "Microclimate Monitoring"
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

    public async Task SeedAsync()
    {
        if (await context.Publications.AnyAsync())
        {
            logger.LogInformation("Seed skipped — data already exists.");
            return;
        }

        Randomizer.Seed = new Random(Seed);

        var authors = GenerateAuthors(1000);
        var keywords = GenerateKeywords(KeywordPool.Length);
        var publications = GeneratePublications(50000, authors, keywords);

        context.Authors.AddRange(authors);
        context.Keywords.AddRange(keywords);
        context.Publications.AddRange(publications);

        await context.SaveChangesAsync();
        logger.LogInformation(
            "Seeded {Authors} authors, {Keywords} keywords, {Publications} publications.",
            authors.Count, keywords.Count, publications.Count);
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

    private List<Publication> GeneratePublications(
        int count, List<Author> authors, List<Keyword> keywords)
    {
        var faker = new Faker<Publication>()
            .RuleFor(p => p.Title, f =>
                $"{f.PickRandom(TitlePrefixes)} {f.PickRandom(TitleSubjects)}")
            .RuleFor(p => p.Abstract, f => GenerateAbstract(f))
            .RuleFor(p => p.Year, f => f.Random.Int(2015, 2025))
            .RuleFor(p => p.DOI, f =>
                $"10.{f.Random.Int(1000, 9999)}/{f.Random.AlphaNumeric(8)}")
            .RuleFor(p => p.Authors, f =>
                f.PickRandom(authors, f.Random.Int(1, 3)).ToList())
            .RuleFor(p => p.Keywords, f =>
                f.PickRandom(keywords, f.Random.Int(1, 4)).ToList())
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

    private static int WordCount(string text) =>
        text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
}
