namespace SmartMeeting.Api.Email;

public class EmailSettings
{
    public string Host { get; set; } = "mailhog";
    public int Port { get; set; } = 1025;
    public bool UseSsl { get; set; } = false;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string FromName { get; set; } = "SmartMeeting CHU";
    public string FromAddress { get; set; } = "no-reply@chu-demo.ma";
}
