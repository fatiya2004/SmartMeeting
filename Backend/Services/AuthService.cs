using Microsoft.EntityFrameworkCore;
using SmartMeeting.Api.Data;
using SmartMeeting.Api.DTOs;
using SmartMeeting.Api.Models;

namespace SmartMeeting.Api.Services;

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request);
    Task<UserDto?> GetByIdAsync(int userId);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;

    public AuthService(AppDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _context.Users.AnyAsync(u => u.Email == email))
        {
            return ServiceResult<AuthResponse>.Fail("Un compte existe déjà avec cet email.");
        }

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Service = request.Service.Trim(),
            Role = UserRole.Participant,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return ServiceResult<AuthResponse>.Ok(BuildResponse(user));
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return ServiceResult<AuthResponse>.Fail("Email ou mot de passe incorrect.");
        }

        if (!user.IsActive)
        {
            return ServiceResult<AuthResponse>.Fail("Ce compte est désactivé.");
        }

        return ServiceResult<AuthResponse>.Ok(BuildResponse(user));
    }

    public async Task<UserDto?> GetByIdAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user is null ? null : UserDto.From(user);
    }

    private AuthResponse BuildResponse(User user)
    {
        var (token, expiresAt) = _jwtService.GenerateToken(user);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = UserDto.From(user)
        };
    }
}
