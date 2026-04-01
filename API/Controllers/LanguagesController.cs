using Microsoft.AspNetCore.Mvc;
using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Services;

namespace ResearchPublications.API.Controllers;

[ApiController]
[Route("api/languages")]
public class LanguagesController(LanguageService languageService, CacheService cacheService) : ControllerBase
{
    // GET /api/languages/filter-options
    [HttpGet("filter-options")]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> GetFilterOptions()
    {
        var options = await cacheService.GetLanguageFilterOptionsAsync();
        return Ok(options);
    }

    // GET /api/languages/search?q=eng&limit=20
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q = "",
        [FromQuery] int limit = 20)
    {
        if (limit < 1 || limit > 100) limit = 20;
        var items = await languageService.SearchAsync(q, limit);
        return Ok(items);
    }

    // GET /api/languages?page=1&pageSize=20&q=eng
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? q = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var (items, total) = await languageService.GetAllAsync(page, pageSize, q);
        return Ok(new { items, total, page, pageSize });
    }

    // GET /api/languages/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var dto = await languageService.GetByIdAsync(id);
        return Ok(dto);
    }

    // POST /api/languages
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] LanguageManagementDto dto)
    {
        var newId = await languageService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId });
    }

    // PUT /api/languages/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] LanguageManagementDto dto)
    {
        await languageService.UpdateAsync(id, dto);
        return NoContent();
    }

    // DELETE /api/languages/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await languageService.DeleteAsync(id);
        return NoContent();
    }
}
