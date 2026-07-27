namespace DataFlow.Business.Concrete.Services;

/// <summary>appsettings.json > Jwt bölümünden bağlanır.</summary>
public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = "DataFlow.API";
    public string Audience { get; set; } = "DataFlow.Client";

    /// <summary>Token geçerlilik süresi (dakika).</summary>
    public int ExpiryMinutes { get; set; } = 120;
}
