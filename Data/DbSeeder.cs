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

        if (!HasCurrentSchema(db))
        {
            Console.WriteLine("[DbSeeder] Older database schema detected. Rebuilding the database from scratch...");
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }

        if (!db.MenuCategories.Any())
        {
            var plates = new MenuCategory { Name = "Plates", DisplayOrder = 1 };
            var platters = new MenuCategory { Name = "Platters", DisplayOrder = 2 };

            plates.Items = new List<MenuItem>
            {
                new() { Name = "Wors Roll", Description = "Char-grilled wors in a fresh roll with our house relish.", Price = 45m, ImageUrl = WorsImage },
                new() { Name = "Pap & Wors", Description = "Creamy pap with smoky wors and a fire-kissed sauce.", Price = 55m, ImageUrl = WorsImage },
                new() { Name = "Pap & Beef", Description = "Tender flame-grilled beef served with comforting pap.", Price = 75m, ImageUrl = BeefImage },
                new() { Name = "Pap with Chicken, Wors & Salads", Description = "A generous plate of chicken, wors, pap, and fresh salads.", Price = 85m, ImageUrl = ChickenImage },
                new() { Name = "Pap with Beef, Wors & Salads", Description = "Flame-grilled beef and wors with pap and crisp salads.", Price = 90m, ImageUrl = MixedPlateImage },
                new() { Name = "5 Chicken Wings", Description = "Five juicy chicken wings grilled over real heat.", Price = 60m, ImageUrl = ChickenImage },
            };

            platters.Items = new List<MenuItem>
            {
                new()
                {
                    Name = "Pap with Beef, Chicken, Wors & Salads",
                    ServingInfo = "Serves 2",
                    Description = "A sharing platter of beef, chicken, wors, pap, and salads.", Price = 150m, ImageUrl = PlattersImage
                },
                new()
                {
                    Name = "Pap with Beef, Chicken, Wors & Salads",
                    ServingInfo = "Serves 3",
                    Description = "A generous sharing platter for three, built for the table.", Price = 220m, ImageUrl = PlattersImage
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
            foreach (var item in category.Items.Where(item => string.IsNullOrWhiteSpace(item.Description)))
                item.Description = MenuDescription(item.Name, item.ServingInfo);
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

    private static string MenuDescription(string name, string? servingInfo) =>
        name.Contains("Wings", StringComparison.OrdinalIgnoreCase)
            ? "Juicy chicken wings grilled over real heat."
            : name.Contains("Wors Roll", StringComparison.OrdinalIgnoreCase)
                ? "Char-grilled wors in a fresh roll with house relish."
                : name.Contains("Platter", StringComparison.OrdinalIgnoreCase) || servingInfo is not null
                    ? $"A sharing platter of beef, chicken, wors, pap, and salads{(servingInfo is null ? "." : $" ({servingInfo.ToLowerInvariant()}).") }"
                    : name.Contains("Chicken", StringComparison.OrdinalIgnoreCase)
                        ? "A generous plate of flame-grilled chicken, pap, and fresh salads."
                        : name.Contains("Beef", StringComparison.OrdinalIgnoreCase)
                            ? "Tender flame-grilled beef served with comforting pap."
                            : "Smoky flame-grilled wors served with pap and fresh sides.";

    private static bool HasCurrentSchema(AppDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var wasClosed = connection.State == System.Data.ConnectionState.Closed;
        if (wasClosed) connection.Open();

        try
        {
            return HasTable(connection, "Payments") &&
                HasTable(connection, "SupportCalls") &&
                HasColumn(connection, "Customers", "Email") &&
                HasColumn(connection, "Orders", "Subtotal") &&
                HasColumn(connection, "Orders", "DiscountAmount") &&
                HasColumn(connection, "Orders", "PickupCodeHash") &&
                HasColumn(connection, "Orders", "PickupCodeSentAt") &&
                HasColumn(connection, "Orders", "PickupVerifiedAt");
        }
        finally
        {
            if (wasClosed) connection.Close();
        }
    }

    private static bool HasTable(System.Data.Common.DbConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = $name LIMIT 1";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "$name";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);
        return command.ExecuteScalar() is not null;
    }

    private static bool HasColumn(System.Data.Common.DbConnection connection, string tableName, string columnName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info([{tableName}])";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
