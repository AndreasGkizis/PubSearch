using Microsoft.AspNetCore.Mvc;
using ResearchPublications.Application.DTOs;
using ResearchPublications.Application.Services;

namespace ResearchPublications.API.Controllers;

[ApiController]
[Route("api/authors")]
public class AuthorsController(AuthorService authorService) : ControllerBase
{
    // GET /api/authors?page=1&pageSize=20
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var (items, total) = await authorService.GetAllAsync(page, pageSize);
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
