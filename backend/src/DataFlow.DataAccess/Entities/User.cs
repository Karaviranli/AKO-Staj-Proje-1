namespace DataFlow.DataAccess.Entities;

/// <summary>
/// Sisteme giriş yapan kullanıcı. Şifreler asla düz metin tutulmaz (BCrypt hash).
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Admin | Analyst | Viewer</summary>
    public string Role { get; set; } = "Analyst";

    public string? FullName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public ICollection<UploadedFile> UploadedFiles { get; set; } = new List<UploadedFile>();
}
