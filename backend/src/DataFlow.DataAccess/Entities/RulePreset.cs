namespace DataFlow.DataAccess.Entities;

/// <summary>
/// Yeniden kullanılabilir kural şablonu. Kullanıcı bir kez kurguladığı kural setini
/// kaydedip başka veri setlerine tek tıkla uygulayabilir.
/// </summary>
public class RulePreset
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Hangi iş alanına ait: satis | calisan | genel</summary>
    public string Category { get; set; } = "genel";

    public string RulesJson { get; set; } = "[]";

    public bool IsSystemPreset { get; set; }
    public int? UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
