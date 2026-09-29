namespace SmartMeeting.Api.Email;

public interface IEmailService
{
    Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
