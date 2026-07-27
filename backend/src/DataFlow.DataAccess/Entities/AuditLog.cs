namespace DataFlow.DataAccess.Entities;

/// <summary>
/// Kim, ne zaman, hangi işlemi yaptı. Kurumsal izlenebilirlik gereksinimi.
/// </summary>
public class AuditLog
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string Username { get; set; } = "anonim";

    /// <summary>LOGIN | UPLOAD | PROCESS | DELETE | EXPORT</summary>
    public string Action { get; set; } = string.Empty;

    public string? Detail { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
