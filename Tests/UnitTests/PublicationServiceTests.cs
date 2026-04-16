using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using ResearchPublications.Application.Interfaces;
using ResearchPublications.Application.Services;
using ResearchPublications.Domain.Entities;
using ResearchPublications.Domain.Interfaces;
using Xunit;

namespace ResearchPublications.UnitTests;

public class PublicationServiceTests
{
    private readonly IPublicationRepository _repository = Substitute.For<IPublicationRepository>();
    private readonly IFileService _fileService = Substitute.For<IFileService>();
    private readonly ITypesenseIndexingService _indexing = Substitute.For<ITypesenseIndexingService>();
    private readonly PublicationService _sut;

    public PublicationServiceTests()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var authorRepo = Substitute.For<IAuthorRepository>();
        var keywordRepo = Substitute.For<IKeywordRepository>();
        var languageRepo = Substitute.For<ILanguageRepository>();
        var pubTypeRepo = Substitute.For<IPublicationTypeRepository>();

        authorRepo.GetFilterOptionsAsync().Returns([]);
        keywordRepo.GetFilterOptionsAsync().Returns([]);
        languageRepo.GetFilterOptionsAsync().Returns([]);
        pubTypeRepo.GetFilterOptionsAsync().Returns([]);

        var cacheService = new CacheService(cache, authorRepo, keywordRepo, languageRepo, pubTypeRepo);
        _sut = new PublicationService(_repository, cacheService, _indexing, _fileService);
    }

    // ── DeleteAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_PublicationHasPdf_DeletesPdf()
    {
        const string pdfFileName = "abc123_paper.pdf";
        _repository.GetByIdAsync(1).Returns(new Publication { Id = 1, Title = "T", PdfFileName = pdfFileName });

        await _sut.DeleteAsync(1);

        await _fileService.Received(1).DeletePdfAsync(pdfFileName);
    }

    [Fact]
    public async Task DeleteAsync_PublicationHasNoPdf_DoesNotCallDeletePdf()
    {
        _repository.GetByIdAsync(2).Returns(new Publication { Id = 2, Title = "T", PdfFileName = null });

        await _sut.DeleteAsync(2);

        await _fileService.DidNotReceive().DeletePdfAsync(Arg.Any<string>());
    }

    // ── UpdateAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_PdfReplaced_DeletesOldPdf()
    {
        const string oldPdf = "old_abc.pdf";
        const string newPdf = "new_xyz.pdf";
        _repository.GetByIdAsync(10).Returns(new Publication { Id = 10, Title = "T", PdfFileName = oldPdf });

        await _sut.UpdateAsync(10, new() { Title = "T", PdfFileName = newPdf });

        await _fileService.Received(1).DeletePdfAsync(oldPdf);
    }

    [Fact]
    public async Task UpdateAsync_PdfUnchanged_DoesNotDeletePdf()
    {
        const string samePdf = "same_file.pdf";
        _repository.GetByIdAsync(11).Returns(new Publication { Id = 11, Title = "T", PdfFileName = samePdf });

        await _sut.UpdateAsync(11, new() { Title = "T", PdfFileName = samePdf });

        await _fileService.DidNotReceive().DeletePdfAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task UpdateAsync_PdfCleared_DeletesOldPdf()
    {
        const string oldPdf = "existing.pdf";
        _repository.GetByIdAsync(12).Returns(new Publication { Id = 12, Title = "T", PdfFileName = oldPdf });

        await _sut.UpdateAsync(12, new() { Title = "T", PdfFileName = null });

        await _fileService.Received(1).DeletePdfAsync(oldPdf);
    }

    [Fact]
    public async Task UpdateAsync_NoPreviousPdf_DoesNotCallDeletePdf()
    {
        _repository.GetByIdAsync(13).Returns(new Publication { Id = 13, Title = "T", PdfFileName = null });

        await _sut.UpdateAsync(13, new() { Title = "T", PdfFileName = "new.pdf" });

        await _fileService.DidNotReceive().DeletePdfAsync(Arg.Any<string>());
    }
}
