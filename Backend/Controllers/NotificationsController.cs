using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartMeeting.Api.Data;
using SmartMeeting.Api.DTOs;

namespace SmartMeeting.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotificationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetMine()
    {
        var userId = this.Id();

        var notifications = await _context.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .Select(n => new
            {
                n.Id,
                Type = n.Type.ToString(),
                n.Subject,
                n.Message,
                n.IsRead,
                n.IsSent,
                n.CreatedAt,
                n.MeetingId
            })
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpPost("{id:int}/read")]
    public async Task<ActionResult<MessageResponse>> MarkAsRead(int id)
    {
        var userId = this.Id();
        var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

        if (notification is null)
        {
            return NotFound(new MessageResponse("Notification introuvable."));
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok(new MessageResponse("Notification marquée comme lue."));
    }
}
