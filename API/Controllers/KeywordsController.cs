using Microsoft.AspNetCore.Mvc;
using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Services;

namespace ResearchPublications.API.Controllers;

[ApiController]
[Route("api/keywords")]
public class KeywordsController(KeywordService keywordService, CacheService cacheService) : ControllerBase
{
    // GET /api/keywords/filter-options
    [HttpGet("filter-options")]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> GetFilterOptions()
    {
        var options = await cacheService.GetKeywordFilterOptionsAsync();
        return Ok(options);
    }

    // GET /api/keywords/search?q=machine&limit=20
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q = "",
        [FromQuery] int limit = 20)
    {
        if (limit < 1 || limit > 100) limit = 20;
        var items = await keywordService.SearchAsync(q, limit);
        return Ok(items);
    }

    // GET /api/keywords?page=1&pageSize=20
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var (items, total) = await keywordService.GetAllAsync(page, pageSize);
        return Ok(new { items, total, page, pageSize });
    }

    // GET /api/keywords/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var dto = await keywordService.GetByIdAsync(id);
        return Ok(dto);
    }

    // POST /api/keywords
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] KeywordManagementDto dto)
    {
        var newId = await keywordService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId });
    }

    // PUT /api/keywords/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] KeywordManagementDto dto)
    {
        await keywordService.UpdateAsync(id, dto);
        return NoContent();
    }

    // DELETE /api/keywords/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await keywordService.DeleteAsync(id);
        return NoContent();
    }
}
