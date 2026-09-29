using SmartMeeting.Api.Models;

namespace SmartMeeting.Api.Email;

/// <summary>
/// Modèles HTML simples pour les emails du système.
/// </summary>
public static class EmailTemplates
{
    private const string Primary = "#0B5FA5";

    public static EmailMessage Invitation(User recipient, Meeting meeting, string roomName)
    {
        var body = Layout(
            "Invitation à une réunion",
            $"Bonjour {recipient.FullName},",
            "Vous êtes invité(e) à participer à la réunion suivante.",
            MeetingTable(meeting, roomName),
            "Merci de confirmer votre présence depuis la plateforme SmartMeeting.");

        return new EmailMessage
        {
            ToAddress = recipient.Email,
            ToName = recipient.FullName,
            Subject = $"[SmartMeeting] Invitation : {meeting.Title}",
            HtmlBody = body
        };
    }

    public static EmailMessage Approved(User recipient, Meeting meeting, string roomName)
    {
        var body = Layout(
            "Réunion confirmée",
            $"Bonjour {recipient.FullName},",
            "Votre demande de réunion a été validée par le secrétariat.",
            MeetingTable(meeting, roomName),
            "Cet email vaut confirmation de la réservation de la salle.");

        return new EmailMessage
        {
            ToAddress = recipient.Email,
            ToName = recipient.FullName,
            Subject = $"[SmartMeeting] Réunion confirmée : {meeting.Title}",
            HtmlBody = body
        };
    }

    public static EmailMessage Rejected(User recipient, Meeting meeting, string reason)
    {
        var body = Layout(
            "Demande refusée",
            $"Bonjour {recipient.FullName},",
            "Votre demande de réunion n'a pas pu être validée.",
            $"<p style=\"margin:0 0 8px\"><strong>Réunion :</strong> {Escape(meeting.Title)}</p>" +
            $"<p style=\"margin:0\"><strong>Motif :</strong> {Escape(reason)}</p>",
            "Vous pouvez soumettre une nouvelle demande avec un autre créneau.");

        return new EmailMessage
        {
            ToAddress = recipient.Email,
            ToName = recipient.FullName,
            Subject = $"[SmartMeeting] Demande refusée : {meeting.Title}",
            HtmlBody = body
        };
    }

    public static EmailMessage Modified(User recipient, Meeting meeting, string roomName)
    {
        var body = Layout(
            "Réunion modifiée",
            $"Bonjour {recipient.FullName},",
            "Les informations de la réunion suivante ont été modifiées.",
            MeetingTable(meeting, roomName),
            "Merci de vérifier votre agenda.");

        return new EmailMessage
        {
            ToAddress = recipient.Email,
            ToName = recipient.FullName,
            Subject = $"[SmartMeeting] Réunion modifiée : {meeting.Title}",
            HtmlBody = body
        };
    }

    public static EmailMessage Reminder(User recipient, Meeting meeting, string roomName)
    {
        var body = Layout(
            "Rappel de réunion",
            $"Bonjour {recipient.FullName},",
            "Votre réunion commence bientôt.",
            MeetingTable(meeting, roomName),
            "Ceci est un rappel automatique.");

        return new EmailMessage
        {
            ToAddress = recipient.Email,
            ToName = recipient.FullName,
            Subject = $"[SmartMeeting] Rappel : {meeting.Title}",
            HtmlBody = body
        };
    }

    private static string MeetingTable(Meeting meeting, string roomName)
    {
        var rows = new (string Label, string Value)[]
        {
            ("Titre", meeting.Title),
            ("Date", meeting.Date.ToString("dd/MM/yyyy")),
            ("Horaire", $"{meeting.StartTime:HH\\:mm} - {meeting.EndTime:HH\\:mm}"),
            ("Salle", roomName),
            ("Priorité", meeting.Priority.ToString().ToUpperInvariant())
        };

        var cells = string.Join(string.Empty, rows.Select(r =>
            $"<tr><td style=\"padding:8px 12px;border-bottom:1px solid #E6E6E6;color:#555\">{Escape(r.Label)}</td>" +
            $"<td style=\"padding:8px 12px;border-bottom:1px solid #E6E6E6;font-weight:600\">{Escape(r.Value)}</td></tr>"));

        return $"<table style=\"width:100%;border-collapse:collapse;margin:16px 0\">{cells}</table>";
    }

    private static string Layout(string title, string greeting, string intro, string content, string footer)
    {
        return $@"<!DOCTYPE html>
<html lang=""fr"">
<head><meta charset=""utf-8""></head>
<body style=""margin:0;padding:24px;background:#F4F6F9;font-family:Segoe UI,Arial,sans-serif;color:#222"">
  <div style=""max-width:560px;margin:0 auto;background:#FFFFFF;border-radius:8px;overflow:hidden;border:1px solid #E1E5EA"">
    <div style=""background:{Primary};color:#FFFFFF;padding:18px 24px"">
      <div style=""font-size:13px;opacity:.85"">CHU Mohammed VI d'Oujda</div>
      <div style=""font-size:18px;font-weight:700"">SmartMeeting — {Escape(title)}</div>
    </div>
    <div style=""padding:24px"">
      <p style=""margin:0 0 12px"">{Escape(greeting)}</p>
      <p style=""margin:0 0 4px"">{Escape(intro)}</p>
      {content}
      <p style=""margin:12px 0 0;color:#555;font-size:14px"">{Escape(footer)}</p>
    </div>
    <div style=""padding:14px 24px;background:#FAFBFC;color:#8A8F98;font-size:12px"">
      Message automatique — merci de ne pas répondre à cet email.
    </div>
  </div>
</body>
</html>";
    }

    private static string Escape(string value) =>
        value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
