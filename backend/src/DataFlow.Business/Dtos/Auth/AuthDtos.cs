using System.ComponentModel.DataAnnotations;

namespace DataFlow.Business.Dtos.Auth;

public class LoginRequestDto
{
    [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequestDto
{
    [Required, MinLength(3, ErrorMessage = "Kullanıcı adı en az 3 karakter olmalıdır.")]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    public string Password { get; set; } = string.Empty;

    public string? FullName { get; set; }
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? FullName { get; set; }
}
