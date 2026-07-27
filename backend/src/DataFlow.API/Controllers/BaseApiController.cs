using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace DataFlow.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Token içindeki kullanıcı kimliği. İstemciden gelen bir userId'ye
    /// asla güvenilmez — yetki her zaman token'dan okunur.
    /// </summary>
    protected int CurrentUserId
    {
        get
        {
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? User.FindFirstValue("sub");

            return int.TryParse(raw, out var id)
                ? id
                : throw new UnauthorizedAccessException("Geçersiz oturum.");
        }
    }

    protected string CurrentUsername => User.FindFirstValue(ClaimTypes.Name) ?? "-";

    protected string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();
}
