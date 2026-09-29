using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SmartMeeting.Api.Email;

public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(EmailSettings settings, ILogger<SmtpEmailService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var mime = new MimeMessage();
            mime.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            mime.To.Add(new MailboxAddress(message.ToName, message.ToAddress));
            mime.Subject = message.Subject;
            mime.Body = new BodyBuilder { HtmlBody = message.HtmlBody }.ToMessageBody();

            using var client = new SmtpClient();

            var socketOptions = _settings.UseSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.None;

            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.UserName))
            {
                await client.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);
            }

            await client.SendAsync(mime, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email envoyé à {Address} : {Subject}", message.ToAddress, message.Subject);
            return true;
        }
        catch (Exception ex)
        {
            // L'échec d'un email ne doit jamais faire échouer la requête métier.
            _logger.LogError(ex, "Échec de l'envoi de l'email à {Address}", message.ToAddress);
            return false;
        }
    }
}
