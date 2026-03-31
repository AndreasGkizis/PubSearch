using Microsoft.AspNetCore.Mvc;
using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Services;
using ResearchPublications.Domain.Interfaces;

namespace ResearchPublications.API.Controllers;

[ApiController]
[Route("api/publications")]
public class PublicationsController(PublicationService publicationService, IFileService fileService)
    : ControllerBase
{
    // GET /api/publications?page=1&pageSize=20
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int      page     = 1,
        [FromQuery] int      pageSize = 20,
        [FromQuery] int?     yearFrom = null,
        [FromQuery] int?     yearTo   = null,
        [FromQuery] string[]? authors  = null,
        [FromQuery] string[]? keywords = null)
    {
        if (page < 1)    page     = 1;
        if (pageSize < 1 || pageSize > 20) pageSize = 20;

        var hasFilters = yearFrom.HasValue || yearTo.HasValue
            || (authors?.Length > 0) || (keywords?.Length > 0);
        var filters = hasFilters
            ? new SearchFilters(yearFrom, yearTo, authors, keywords)
            : null;

        var (items, total) = await publicationService.GetSummariesAsync(page, pageSize, filters);
        return Ok(new { items, total, page, pageSize });
    }

    // GET /api/publications/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var dto = await publicationService.GetDetailAsync(id);
        return Ok(dto);
    }

    // POST /api/publications
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PublicationDetailDto dto)
    {
        var newId = await publicationService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId });
    }

    // PUT /api/publications/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PublicationDetailDto dto)
    {
        await publicationService.UpdateAsync(id, dto);
        return NoContent();
    }

    // DELETE /api/publications/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await publicationService.DeleteAsync(id);
        return NoContent();
    }

    // GET /api/publications/{id}/download
    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var detail = await publicationService.GetDetailAsync(id);

        if (string.IsNullOrWhiteSpace(detail.PdfFileName))
            return NotFound(new { error = "No PDF is associated with this publication." });

        var stream = await fileService.GetPdfAsync(detail.PdfFileName);
        if (stream is null)
            return NotFound(new { error = "PDF file was not found on the server." });

        return File(stream, "application/pdf", detail.PdfFileName);
    }

    // POST /api/publications/upload
    // Upload a PDF file; returns { fileName } which can be used in create/update payloads.
    [HttpPost("upload")]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file was provided." });

        if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Only PDF files are accepted." });

        await using var stream = file.OpenReadStream();
        var savedName = await fileService.SavePdfAsync(stream, file.FileName);
        return Ok(new { fileName = savedName });
    }
}
