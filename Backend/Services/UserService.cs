using Microsoft.EntityFrameworkCore;
using SmartMeeting.Api.Data;
using SmartMeeting.Api.DTOs;
using SmartMeeting.Api.Models;

namespace SmartMeeting.Api.Services;

public interface IUserService
{
    Task<List<UserDto>> GetAllAsync();
    Task<List<UserDto>> SearchByNameAsync(IEnumerable<string> names);
    Task<ServiceResult<UserDto>> UpdateRoleAsync(int id, UserRole role);
    Task<ServiceResult<UserDto>> SetActiveAsync(int id, bool isActive);
}

public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserDto>> GetAllAsync()
    {
        var users = await _context.Users.AsNoTracking()
            .OrderBy(u => u.LastName)
            .ToListAsync();

        return users.Select(UserDto.From).ToList();
    }

    /// <summary>
    /// Utilisé par l'IA : convertit « Ahmed », « Sara » en utilisateurs réels.
    /// </summary>
    public async Task<List<UserDto>> SearchByNameAsync(IEnumerable<string> names)
    {
        var result = new List<UserDto>();
        var users = await _context.Users.AsNoTracking().Where(u => u.IsActive).ToListAsync();

        foreach (var rawName in names)
        {
            var name = rawName.Trim().ToLowerInvariant();

            if (name.Length == 0) continue;

            var match = users.FirstOrDefault(u =>
                u.FirstName.ToLowerInvariant() == name ||
                u.LastName.ToLowerInvariant() == name ||
                u.FullName.ToLowerInvariant().Contains(name) ||
                u.Email.ToLowerInvariant().StartsWith(name));

            if (match is not null && result.All(r => r.Id != match.Id))
            {
                result.Add(UserDto.From(match));
            }
        }

        return result;
    }

    public async Task<ServiceResult<UserDto>> UpdateRoleAsync(int id, UserRole role)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
        {
            return ServiceResult<UserDto>.Fail("Utilisateur introuvable.");
        }

        user.Role = role;
        await _context.SaveChangesAsync();

        return ServiceResult<UserDto>.Ok(UserDto.From(user));
    }

    public async Task<ServiceResult<UserDto>> SetActiveAsync(int id, bool isActive)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

        if (user is null)
        {
            return ServiceResult<UserDto>.Fail("Utilisateur introuvable.");
        }

        user.IsActive = isActive;
        await _context.SaveChangesAsync();

        return ServiceResult<UserDto>.Ok(UserDto.From(user));
    }
}
