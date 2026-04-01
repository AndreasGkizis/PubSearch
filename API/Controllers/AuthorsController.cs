using Microsoft.AspNetCore.Mvc;
using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Services;

namespace ResearchPublications.API.Controllers;

[ApiController]
[Route("api/authors")]
public class AuthorsController(AuthorService authorService, CacheService cacheService) : ControllerBase
{
    // GET /api/authors/filter-options
    [HttpGet("filter-options")]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> GetFilterOptions()
    {
        var options = await cacheService.GetAuthorFilterOptionsAsync();
        return Ok(options);
    }

    // GET /api/authors/search?q=john&limit=20
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q = "",
        [FromQuery] int limit = 20)
    {
        if (limit < 1 || limit > 100) limit = 20;
        var items = await authorService.SearchAsync(q, limit);
        return Ok(items);
    }

    // GET /api/authors?page=1&pageSize=20&q=john
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? q = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var (items, total) = await authorService.GetAllAsync(page, pageSize, q);
        return Ok(new { items, total, page, pageSize });
    }

    // GET /api/authors/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var dto = await authorService.GetByIdAsync(id);
        return Ok(dto);
    }

    // POST /api/authors
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AuthorManagementDto dto)
    {
        var newId = await authorService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId });
    }

    // PUT /api/authors/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] AuthorManagementDto dto)
    {
        await authorService.UpdateAsync(id, dto);
        return NoContent();
    }

    // DELETE /api/authors/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await authorService.DeleteAsync(id);
        return NoContent();
    }
}
