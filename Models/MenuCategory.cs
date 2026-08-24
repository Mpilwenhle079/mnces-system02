using System.ComponentModel.DataAnnotations;

namespace MnceShisanyama.Api.Models;

/// <summary>
/// A menu grouping, e.g. "Plates" or "Platters".
/// </summary>
public class MenuCategory
{
    public int Id { get; set; }

    [Required, MaxLength(60)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Controls left-to-right / top-to-bottom order on the customer menu.</summary>
    public int DisplayOrder { get; set; }

    public ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();
}
