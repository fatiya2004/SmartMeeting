using Microsoft.EntityFrameworkCore;
using SmartMeeting.Api.Models;

namespace SmartMeeting.Api.Data;

/// <summary>
/// Insère des données de démonstration (noms fictifs) au premier démarrage.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        if (!await context.Users.AnyAsync())
        {
            var users = new List<User>
            {
                NewUser("Nadia", "Karim", "secretaire@chu-demo.ma", UserRole.Secretaire, "Secrétariat médical"),
                NewUser("Samir", "Idrissi", "admin@chu-demo.ma", UserRole.Admin, "Direction informatique"),
                NewUser("Ahmed", "Benali", "ahmed.benali@chu-demo.ma", UserRole.Participant, "Cardiologie"),
                NewUser("Sara", "Amrani", "sara.amrani@chu-demo.ma", UserRole.Participant, "Cardiologie"),
                NewUser("Youssef", "Alaoui", "youssef.alaoui@chu-demo.ma", UserRole.Participant, "Chirurgie"),
                NewUser("Imane", "Tazi", "imane.tazi@chu-demo.ma", UserRole.Participant, "Pédiatrie")
            };

            context.Users.AddRange(users);
            await context.SaveChangesAsync();
        }

        if (!await context.Rooms.AnyAsync())
        {
            context.Rooms.AddRange(
                new Room { Name = "Salle A - Cardiologie", Capacity = 12, Location = "Bâtiment A, 1er étage", Equipment = "Vidéoprojecteur, tableau blanc" },
                new Room { Name = "Salle B - Staff", Capacity = 25, Location = "Bâtiment A, 2e étage", Equipment = "Visioconférence, micro" },
                new Room { Name = "Salle C - Formation", Capacity = 40, Location = "Bâtiment B, rez-de-chaussée", Equipment = "Vidéoprojecteur, sonorisation" },
                new Room { Name = "Salle D - Réunion rapide", Capacity = 6, Location = "Bâtiment B, 1er étage", Equipment = "Écran TV" }
            );
            await context.SaveChangesAsync();
        }

        if (!await context.Unavailabilities.AnyAsync())
        {
            var ahmed = await context.Users.FirstAsync(u => u.Email == "ahmed.benali@chu-demo.ma");
            var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

            context.Unavailabilities.Add(new Unavailability
            {
                UserId = ahmed.Id,
                Type = UnavailabilityType.Garde,
                Date = tomorrow,
                StartTime = new TimeOnly(14, 0),
                EndTime = new TimeOnly(18, 0),
                Reason = "Garde de cardiologie"
            });

            await context.SaveChangesAsync();
        }
    }

    private static User NewUser(string firstName, string lastName, string email, UserRole role, string service) => new()
    {
        FirstName = firstName,
        LastName = lastName,
        Email = email,
        Role = role,
        Service = service,
        // Mot de passe de démonstration : Password123!
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!")
    };
}
