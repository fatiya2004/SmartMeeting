using SmartMeeting.Api.DTOs;
using SmartMeeting.Api.Services;
using Xunit;

namespace SmartMeeting.Tests;

public class AuthServiceTests
{
    private static readonly JwtSettings Settings = new()
    {
        Secret = "cle-de-test-suffisamment-longue-pour-hmac-sha256",
        Issuer = "Test",
        Audience = "Test",
        ExpiresInHours = 1
    };

    [Fact]
    public async Task Login_AvecBonMotDePasse_RetourneUnToken()
    {
        await using var context = TestDatabase.Create(nameof(Login_AvecBonMotDePasse_RetourneUnToken));
        var service = new AuthService(context, new JwtService(Settings));

        var result = await service.LoginAsync(new LoginRequest { Email = "ahmed@test.ma", Password = "Password123!" });

        Assert.True(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Data!.Token));
        Assert.Equal("ahmed@test.ma", result.Data.User.Email);
    }

    [Fact]
    public async Task Login_AvecMauvaisMotDePasse_Echoue()
    {
        await using var context = TestDatabase.Create(nameof(Login_AvecMauvaisMotDePasse_Echoue));
        var service = new AuthService(context, new JwtService(Settings));

        var result = await service.LoginAsync(new LoginRequest { Email = "ahmed@test.ma", Password = "mauvais" });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Register_AvecEmailExistant_Echoue()
    {
        await using var context = TestDatabase.Create(nameof(Register_AvecEmailExistant_Echoue));
        var service = new AuthService(context, new JwtService(Settings));

        var result = await service.RegisterAsync(new RegisterRequest
        {
            FirstName = "Ahmed",
            LastName = "Benali",
            Email = "ahmed@test.ma",
            Password = "Password123!"
        });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Register_NouvelUtilisateur_Reussit()
    {
        await using var context = TestDatabase.Create(nameof(Register_NouvelUtilisateur_Reussit));
        var service = new AuthService(context, new JwtService(Settings));

        var result = await service.RegisterAsync(new RegisterRequest
        {
            FirstName = "Imane",
            LastName = "Tazi",
            Email = "imane@test.ma",
            Password = "Password123!",
            Service = "Pédiatrie"
        });

        Assert.True(result.Success);
        Assert.Equal("imane@test.ma", result.Data!.User.Email);
    }
}
