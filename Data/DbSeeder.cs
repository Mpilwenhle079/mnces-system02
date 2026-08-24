using Microsoft.EntityFrameworkCore;
using MnceShisanyama.Api.Models;
using MnceShisanyama.Api.Services;

namespace MnceShisanyama.Api.Data;

/// <summary>
/// Seeds the database with Mnce Tpain's real starting menu (from the shop's food
/// menu flyer) plus two demo staff PINs, so the system is immediately usable.
/// </summary>
public static class DbSeeder
{
    // Food-specific defaults for the Mnce Shisanyama menu.
    private const string WorsImage = "https://images.unsplash.com/photo-1559847844-5315695dadae?auto=format&fit=crop&w=1200&q=82";
    private const string BeefImage = "https://images.unsplash.com/photo-1544025162-d76694265947?auto=format&fit=crop&w=1200&q=82";
    private const string MixedPlateImage = "https://images.unsplash.com/photo-1555939594-58d7cb561ad1?auto=format&fit=crop&w=1200&q=82";
    private const string PlattersImage = "https://images.unsplash.com/photo-1547592180-85f173990554?auto=format&fit=crop&w=1200&q=82";
    private const string ChickenImage = "https://images.unsplash.com/photo-1604908176997-125f25cc6f3d?auto=format&fit=crop&w=1200&q=82";

    public static void Seed(AppDbContext db)
    {
        db.Database.EnsureCreated();

        if (!db.MenuCategories.Any())
        {
            var plates = new MenuCategory { Name = "Plates", DisplayOrder = 1 };
            var platters = new MenuCategory { Name = "Platters", DisplayOrder = 2 };

            plates.Items = new List<MenuItem>
            {
                new() { Name = "Wors Roll", Price = 45m, ImageUrl = WorsImage },
                new() { Name = "Pap & Wors", Price = 55m, ImageUrl = WorsImage },
                new() { Name = "Pap & Beef", Price = 75m, ImageUrl = BeefImage },
                new() { Name = "Pap with Chicken, Wors & Salads", Price = 85m, ImageUrl = ChickenImage },
                new() { Name = "Pap with Beef, Wors & Salads", Price = 90m, ImageUrl = MixedPlateImage },
                new() { Name = "5 Chicken Wings", Price = 60m, ImageUrl = ChickenImage },
            };

            platters.Items = new List<MenuItem>
            {
                new()
                {
                    Name = "Pap with Beef, Chicken, Wors & Salads",
                    ServingInfo = "Serves 2",
                    Price = 150m, ImageUrl = PlattersImage
                },
                new()
                {
                    Name = "Pap with Beef, Chicken, Wors & Salads",
                    ServingInfo = "Serves 3",
                    Price = 220m, ImageUrl = PlattersImage
                },
            };

            db.MenuCategories.AddRange(plates, platters);
            db.SaveChanges();
        }

        foreach (var category in db.MenuCategories.Include(c => c.Items))
        {
            var image = category.Name.Equals("Platters", StringComparison.OrdinalIgnoreCase) ? PlattersImage : MixedPlateImage;
            foreach (var item in category.Items.Where(item => string.IsNullOrWhiteSpace(item.ImageUrl)))
            {
                item.ImageUrl = item.Name.Contains("Wings", StringComparison.OrdinalIgnoreCase) ||
                    item.Name.Contains("Chicken", StringComparison.OrdinalIgnoreCase)
                    ? ChickenImage
                    : item.Name.Contains("Wors", StringComparison.OrdinalIgnoreCase)
                        ? WorsImage
                        : item.Name.Contains("Beef", StringComparison.OrdinalIgnoreCase)
                            ? BeefImage
                        : image;
            }
        }
        db.SaveChanges();

        if (!db.StaffUsers.Any())
        {
            db.StaffUsers.AddRange(
                new StaffUser { Name = "Kitchen Staff", PinHash = StaffAuthService.HashPin("1111"), Role = StaffRole.Kitchen },
                new StaffUser { Name = "Manager", PinHash = StaffAuthService.HashPin("2580"), Role = StaffRole.Admin }
            );
            db.SaveChanges();
        }
    }
}
