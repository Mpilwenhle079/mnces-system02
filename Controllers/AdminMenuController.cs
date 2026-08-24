using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MnceShisanyama.Api.Data;
using MnceShisanyama.Api.DTOs;
using MnceShisanyama.Api.Filters;
using MnceShisanyama.Api.Models;

namespace MnceShisanyama.Api.Controllers;

/// <summary>Full menu management (including unavailable items). Admin dashboard only.</summary>
[ApiController]
[Route("api/admin/menu")]
[StaffAuth(StaffRole.Admin)]
public class AdminMenuController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminMenuController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("categories")]
    public async Task<ActionResult<List<MenuCategoryDto>>> GetAllCategories()
    {
        var categories = await _db.MenuCategories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new MenuCategoryDto(
                c.Id, c.Name, c.DisplayOrder,
                c.Items.Select(i => new MenuItemDto(
                    i.Id, i.CategoryId, i.Name, i.Description, i.Price,
                    i.ServingInfo, i.ImageUrl, i.IsAvailable)).ToList()
            ))
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPost("categories")]
    public async Task<ActionResult<MenuCategoryDto>> CreateCategory([FromBody] CreateMenuCategoryRequest request)
    {
        var category = new MenuCategory { Name = request.Name, DisplayOrder = request.DisplayOrder };
        _db.MenuCategories.Add(category);
        await _db.SaveChangesAsync();

        return Ok(new MenuCategoryDto(category.Id, category.Name, category.DisplayOrder, new List<MenuItemDto>()));
    }

    [HttpPost("items")]
    public async Task<ActionResult<MenuItemDto>> CreateItem([FromBody] CreateMenuItemRequest request)
    {
        var categoryExists = await _db.MenuCategories.AnyAsync(c => c.Id == request.CategoryId);
        if (!categoryExists) return BadRequest(new { message = "Unknown category." });

        var item = new MenuItem
        {
            CategoryId = request.CategoryId,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            ServingInfo = request.ServingInfo,
            ImageUrl = request.ImageUrl,
            IsAvailable = request.IsAvailable,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync();

        return Ok(new MenuItemDto(item.Id, item.CategoryId, item.Name, item.Description, item.Price,
            item.ServingInfo, item.ImageUrl, item.IsAvailable));
    }

    [HttpPut("items/{id:int}")]
    public async Task<ActionResult<MenuItemDto>> UpdateItem(int id, [FromBody] UpdateMenuItemRequest request)
    {
        var item = await _db.MenuItems.FindAsync(id);
        if (item is null) return NotFound();

        item.Name = request.Name;
        item.Description = request.Description;
        item.Price = request.Price;
        item.ServingInfo = request.ServingInfo;
        item.ImageUrl = request.ImageUrl;
        item.IsAvailable = request.IsAvailable;
        item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new MenuItemDto(item.Id, item.CategoryId, item.Name, item.Description, item.Price,
            item.ServingInfo, item.ImageUrl, item.IsAvailable));
    }

    /// <summary>Quick toggle for "sold out today" without opening the full edit form.</summary>
    [HttpPatch("items/{id:int}/availability")]
    public async Task<IActionResult> ToggleAvailability(int id, [FromQuery] bool isAvailable)
    {
        var item = await _db.MenuItems.FindAsync(id);
        if (item is null) return NotFound();

        item.IsAvailable = isAvailable;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("items/{id:int}")]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var item = await _db.MenuItems.FindAsync(id);
        if (item is null) return NotFound();

        var usedInOrders = await _db.OrderItems.AnyAsync(oi => oi.MenuItemId == id);
        if (usedInOrders)
        {
            // Keep order history intact - just hide it from the menu instead of hard-deleting.
            item.IsAvailable = false;
            item.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Item has past orders, so it was hidden instead of deleted." });
        }

        _db.MenuItems.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
