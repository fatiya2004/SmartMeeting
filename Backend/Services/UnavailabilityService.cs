using Microsoft.EntityFrameworkCore;
using SmartMeeting.Api.Data;
using SmartMeeting.Api.DTOs;
using SmartMeeting.Api.Models;

namespace SmartMeeting.Api.Services;

public interface IUnavailabilityService
{
    Task<List<UnavailabilityDto>> GetForUserAsync(int userId);
    Task<List<UnavailabilityDto>> GetAllAsync(DateOnly? date);
    Task<ServiceResult<UnavailabilityDto>> CreateAsync(UnavailabilityRequest request, int userId);
    Task<ServiceResult<bool>> DeleteAsync(int id, int userId, bool isStaff);
}

public class UnavailabilityService : IUnavailabilityService
{
    private readonly AppDbContext _context;

    public UnavailabilityService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<UnavailabilityDto>> GetForUserAsync(int userId)
    {
        var items = await _context.Unavailabilities
            .Include(u => u.User)
            .Where(u => u.UserId == userId)
            .OrderByDescending(u => u.Date)
            .ToListAsync();

        return items.Select(UnavailabilityDto.From).ToList();
    }

    public async Task<List<UnavailabilityDto>> GetAllAsync(DateOnly? date)
    {
        var query = _context.Unavailabilities.Include(u => u.User).AsQueryable();

        if (date is not null)
        {
            query = query.Where(u => u.Date == date);
        }

        var items = await query.OrderBy(u => u.Date).ThenBy(u => u.StartTime).ToListAsync();
        return items.Select(UnavailabilityDto.From).ToList();
    }

    public async Task<ServiceResult<UnavailabilityDto>> CreateAsync(UnavailabilityRequest request, int userId)
    {
        if (request.EndTime <= request.StartTime)
        {
            return ServiceResult<UnavailabilityDto>.Fail("L'heure de fin doit être postérieure à l'heure de début.");
        }

        var unavailability = new Unavailability
        {
            UserId = userId,
            Type = request.Type,
            Date = request.Date,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Reason = request.Reason
        };

        _context.Unavailabilities.Add(unavailability);
        await _context.SaveChangesAsync();

        var created = await _context.Unavailabilities.Include(u => u.User).FirstAsync(u => u.Id == unavailability.Id);
        return ServiceResult<UnavailabilityDto>.Ok(UnavailabilityDto.From(created));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id, int userId, bool isStaff)
    {
        var item = await _context.Unavailabilities.FirstOrDefaultAsync(u => u.Id == id);

        if (item is null)
        {
            return ServiceResult<bool>.Fail("Indisponibilité introuvable.");
        }

        if (!isStaff && item.UserId != userId)
        {
            return ServiceResult<bool>.Fail("Vous ne pouvez supprimer que vos propres indisponibilités.");
        }

        _context.Unavailabilities.Remove(item);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Ok(true);
    }
}
