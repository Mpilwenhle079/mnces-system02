using System.ComponentModel.DataAnnotations;

namespace MnceShisanyama.Api.DTOs;

public record MenuItemDto(
    int Id,
    int CategoryId,
    string Name,
    string? Description,
    decimal Price,
    string? ServingInfo,
    string? ImageUrl,
    bool IsAvailable
);

public record MenuCategoryDto(
    int Id,
    string Name,
    int DisplayOrder,
    List<MenuItemDto> Items
);

public class CreateMenuItemRequest
{
    [Required]
    public int CategoryId { get; set; }

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(400)]
    public string? Description { get; set; }

    [Range(0, 100000)]
    public decimal Price { get; set; }

    [MaxLength(40)]
    public string? ServingInfo { get; set; }

    [MaxLength(300)]
    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; } = true;
}

public class UpdateMenuItemRequest
{
    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(400)]
    public string? Description { get; set; }

    [Range(0, 100000)]
    public decimal Price { get; set; }

    [MaxLength(40)]
    public string? ServingInfo { get; set; }

    [MaxLength(300)]
    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; }
}

public class CreateMenuCategoryRequest
{
    [Required, MaxLength(60)]
    public string Name { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
}
