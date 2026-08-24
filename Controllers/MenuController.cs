using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MnceShisanyama.Api.Data;
using MnceShisanyama.Api.DTOs;

namespace MnceShisanyama.Api.Controllers;

/// <summary>Public, read-only menu for the customer ordering page. Only shows available items.</summary>
[ApiController]
[Route("api/menu")]
public class MenuController : ControllerBase
{
    private readonly AppDbContext _db;

    public MenuController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<MenuCategoryDto>>> GetMenu()
    {
        var categories = await _db.MenuCategories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new MenuCategoryDto(
                c.Id,
                c.Name,
                c.DisplayOrder,
                c.Items
                    .Where(i => i.IsAvailable)
                    .Select(i => new MenuItemDto(
                        i.Id, i.CategoryId, i.Name, i.Description, i.Price,
                        i.ServingInfo, i.ImageUrl, i.IsAvailable))
                    .ToList()
            ))
            .ToListAsync();

        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MenuItemDto>> GetItem(int id)
    {
        var item = await _db.MenuItems.FindAsync(id);
        if (item is null || !item.IsAvailable) return NotFound();

        return Ok(new MenuItemDto(
            item.Id, item.CategoryId, item.Name, item.Description, item.Price,
            item.ServingInfo, item.ImageUrl, item.IsAvailable));
    }
}
