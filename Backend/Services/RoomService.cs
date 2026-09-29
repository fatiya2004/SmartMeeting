using Microsoft.EntityFrameworkCore;
using SmartMeeting.Api.Data;
using SmartMeeting.Api.DTOs;
using SmartMeeting.Api.Models;

namespace SmartMeeting.Api.Services;

public interface IRoomService
{
    Task<List<RoomDto>> GetAllAsync(bool onlyActive);
    Task<RoomDto?> GetByIdAsync(int id);
    Task<ServiceResult<RoomDto>> CreateAsync(RoomRequest request);
    Task<ServiceResult<RoomDto>> UpdateAsync(int id, RoomRequest request);
    Task<ServiceResult<bool>> DeleteAsync(int id);
}

public class RoomService : IRoomService
{
    private readonly AppDbContext _context;

    public RoomService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<RoomDto>> GetAllAsync(bool onlyActive)
    {
        var query = _context.Rooms.AsNoTracking().AsQueryable();

        if (onlyActive)
        {
            query = query.Where(r => r.IsActive);
        }

        return await query
            .OrderBy(r => r.Name)
            .Select(r => RoomDto.From(r))
            .ToListAsync();
    }

    public async Task<RoomDto?> GetByIdAsync(int id)
    {
        var room = await _context.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
        return room is null ? null : RoomDto.From(room);
    }

    public async Task<ServiceResult<RoomDto>> CreateAsync(RoomRequest request)
    {
        var name = request.Name.Trim();

        if (await _context.Rooms.AnyAsync(r => r.Name == name))
        {
            return ServiceResult<RoomDto>.Fail("Une salle porte déjà ce nom.");
        }

        var room = new Room
        {
            Name = name,
            Capacity = request.Capacity,
            Location = request.Location.Trim(),
            Equipment = request.Equipment.Trim(),
            IsActive = request.IsActive
        };

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        return ServiceResult<RoomDto>.Ok(RoomDto.From(room));
    }

    public async Task<ServiceResult<RoomDto>> UpdateAsync(int id, RoomRequest request)
    {
        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id);

        if (room is null)
        {
            return ServiceResult<RoomDto>.Fail("Salle introuvable.");
        }

        var name = request.Name.Trim();

        if (await _context.Rooms.AnyAsync(r => r.Name == name && r.Id != id))
        {
            return ServiceResult<RoomDto>.Fail("Une autre salle porte déjà ce nom.");
        }

        room.Name = name;
        room.Capacity = request.Capacity;
        room.Location = request.Location.Trim();
        room.Equipment = request.Equipment.Trim();
        room.IsActive = request.IsActive;

        await _context.SaveChangesAsync();

        return ServiceResult<RoomDto>.Ok(RoomDto.From(room));
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id)
    {
        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id);

        if (room is null)
        {
            return ServiceResult<bool>.Fail("Salle introuvable.");
        }

        var hasMeetings = await _context.Meetings.AnyAsync(m => m.RoomId == id);

        if (hasMeetings)
        {
            // On ne supprime pas une salle déjà utilisée : on la désactive.
            room.IsActive = false;
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(false);
        }

        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();

        return ServiceResult<bool>.Ok(true);
    }
}
