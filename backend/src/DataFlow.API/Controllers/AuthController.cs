using DataFlow.Business.Abstract;
using DataFlow.Business.Dtos.Auth;
using DataFlow.Business.Dtos.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataFlow.API.Controllers;

public class AuthController : BaseApiController
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Kullanıcı adı/e-posta ve şifre ile giriş yapar, JWT döner.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(LoginRequestDto request)
    {
        var result = await _auth.LoginAsync(request, ClientIp);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Giriş başarılı."));
    }

    /// <summary>Yeni kullanıcı oluşturur ve doğrudan oturum açar.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register(RegisterRequestDto request)
    {
        var result = await _auth.RegisterAsync(request, ClientIp);
        return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Kayıt tamamlandı."));
    }

    /// <summary>Token'ın geçerliliğini doğrular ve oturum sahibinin bilgilerini döner.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> Me()
    {
        var profile = await _auth.GetProfileAsync(CurrentUserId);
        if (profile is null) return Unauthorized(ApiResponse<UserDto>.Fail("Kullanıcı bulunamadı."));

        return Ok(ApiResponse<UserDto>.Ok(profile));
    }
}
