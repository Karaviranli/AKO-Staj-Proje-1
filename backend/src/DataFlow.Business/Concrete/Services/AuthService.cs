using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DataFlow.Business.Abstract;
using DataFlow.Business.Dtos.Auth;
using DataFlow.DataAccess.Context;
using DataFlow.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DataFlow.Business.Concrete.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtSettings _jwt;
    private readonly IAuditService _audit;

    public AuthService(AppDbContext db, JwtSettings jwt, IAuditService audit)
    {
        _db = db;
        _jwt = jwt;
        _audit = audit;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string? ip = null)
    {
        var input = request.Username.Trim();

        // Kullanıcı adı veya e-posta ile giriş.
        var user = await _db.Users.FirstOrDefaultAsync(u =>
            u.Username == input || u.Email == input);

        // Kullanıcı yoksa da hash doğrulaması yapılır: cevap süresinden
        // kullanıcının var olup olmadığı anlaşılamasın (timing attack önlemi).
        var hash = user?.PasswordHash ?? DummyHash;
        var valid = BCrypt.Net.BCrypt.Verify(request.Password, hash);

        if (user is null || !valid)
            throw new UnauthorizedAccessException("Kullanıcı adı veya şifre hatalı.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Hesabınız pasif durumda. Yöneticinize başvurun.");

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _audit.LogAsync(user.Id, user.Username, "LOGIN", null, ip);

        return BuildResponse(user);
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, string? ip = null)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Username == username))
            throw new InvalidOperationException("Bu kullanıcı adı zaten kullanılıyor.");

        if (await _db.Users.AnyAsync(u => u.Email == email))
            throw new InvalidOperationException("Bu e-posta adresi zaten kayıtlı.");

        var user = new User
        {
            Username = username,
            Email = email,
            FullName = request.FullName?.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "Analyst"
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(user.Id, user.Username, "REGISTER", null, ip);

        return BuildResponse(user);
    }

    public async Task<UserDto?> GetProfileAsync(int userId)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        return user is null ? null : ToDto(user);
    }

    private AuthResponseDto BuildResponse(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresIn = _jwt.ExpiryMinutes * 60,
            ExpiresAt = expiresAt,
            User = ToDto(user)
        };
    }

    private static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        Role = user.Role,
        FullName = user.FullName
    };

    /// <summary>Var olmayan kullanıcılarda da BCrypt maliyetini ödemek için sabit hash.</summary>
    private const string DummyHash = "$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy";
}
