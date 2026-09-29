using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace SmartMeeting.Api.Controllers;

/// <summary>Raccourcis pour lire l'utilisateur contenu dans le token JWT.</summary>
public static class CurrentUser
{
    public static int Id(this ControllerBase controller) =>
        int.Parse(controller.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static string Role(this ControllerBase controller) =>
        controller.User.FindFirstValue(ClaimTypes.Role) ?? "Participant";

    /// <summary>Admin ou secrétaire : profils autorisés à agir sur toutes les réunions.</summary>
    public static bool IsStaff(this ControllerBase controller) =>
        controller.User.IsInRole("Admin") || controller.User.IsInRole("Secretaire");
}
