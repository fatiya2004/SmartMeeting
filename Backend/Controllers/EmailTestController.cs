using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMeeting.Api.DTOs;
using SmartMeeting.Api.Email;

namespace SmartMeeting.Api.Controllers;

/// <summary>
/// Endpoint de test permettant de vérifier la configuration SMTP.
/// </summary>
[ApiController]
[Route("api/email-test")]
[Authorize(Roles = "Admin")]
public class EmailTestController : ControllerBase
{
    private readonly IEmailService _emailService;

    public EmailTestController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost]
    public async Task<ActionResult<MessageResponse>> Send([FromQuery] string to)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            return BadRequest(new MessageResponse("Paramètre 'to' requis."));
        }

        var message = new EmailMessage
        {
            ToAddress = to,
            ToName = "Test SmartMeeting",
            Subject = "[SmartMeeting] Email de test",
            HtmlBody = "<p>La configuration SMTP fonctionne correctement.</p>"
        };

        var sent = await _emailService.SendAsync(message);

        return sent
            ? Ok(new MessageResponse($"Email envoyé à {to}."))
            : StatusCode(500, new MessageResponse("Échec de l'envoi, vérifiez la configuration SMTP."));
    }
}
