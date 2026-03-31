using Microsoft.AspNetCore.Mvc;
using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Services;

namespace ResearchPublications.API.Controllers;

[ApiController]
[Route("api/publication-types")]
public class PublicationTypesController(PublicationTypeService publicationTypeService, CacheService cacheService) : ControllerBase
{
    // GET /api/publication-types/filter-options
    [HttpGet("filter-options")]
    [ResponseCache(Duration = 300)]
    public async Task<IActionResult> GetFilterOptions()
    {
        var options = await cacheService.GetPublicationTypeFilterOptionsAsync();
        return Ok(options);
    }

    // GET /api/publication-types/search?q=jour&limit=20
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q = "",
        [FromQuery] int limit = 20)
    {
        if (limit < 1 || limit > 100) limit = 20;
        var items = await publicationTypeService.SearchAsync(q, limit);
        return Ok(items);
    }

    // GET /api/publication-types?page=1&pageSize=20
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var (items, total) = await publicationTypeService.GetAllAsync(page, pageSize);
        return Ok(new { items, total, page, pageSize });
    }

    // GET /api/publication-types/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var dto = await publicationTypeService.GetByIdAsync(id);
        return Ok(dto);
    }

    // POST /api/publication-types
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PublicationTypeManagementDto dto)
    {
        var newId = await publicationTypeService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = newId }, new { id = newId });
    }

    // PUT /api/publication-types/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] PublicationTypeManagementDto dto)
    {
        await publicationTypeService.UpdateAsync(id, dto);
        return NoContent();
    }

    // DELETE /api/publication-types/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await publicationTypeService.DeleteAsync(id);
        return NoContent();
    }
}
