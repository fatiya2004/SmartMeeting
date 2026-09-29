using Microsoft.EntityFrameworkCore;
using SmartMeeting.Api.Data;
using SmartMeeting.Api.Models;

namespace SmartMeeting.Tests;

/// <summary>Crée une base en mémoire pré-remplie pour chaque test.</summary>
public static class TestDatabase
{
    public static AppDbContext Create(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

        var context = new AppDbContext(options);

        context.Users.AddRange(
            new User { Id = 1, FirstName = "Ahmed", LastName = "Benali", Email = "ahmed@test.ma", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"), Role = UserRole.Participant },
            new User { Id = 2, FirstName = "Sara", LastName = "Amrani", Email = "sara@test.ma", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"), Role = UserRole.Participant },
            new User { Id = 3, FirstName = "Nadia", LastName = "Karim", Email = "nadia@test.ma", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"), Role = UserRole.Secretaire }
        );

        context.Rooms.AddRange(
            new Room { Id = 1, Name = "Salle A", Capacity = 10, IsActive = true },
            new Room { Id = 2, Name = "Salle B", Capacity = 4, IsActive = true }
        );

        context.SaveChanges();
        return context;
    }
}
