using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataFlow.Business.Common;

/// <summary>
/// Veritabanına yazılan ve API'den dönen JSON için tek merkezden ayar.
/// Türkçe karakterlerin ç gibi kaçış dizilerine dönüşmemesi için
/// Encoder gevşetilmiştir (çıktı yalnızca JSON gövdesinde kullanılır).
/// </summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = false
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);

    /// <summary>
    /// Veritabanından okunan satırları JsonElement yerine düz CLR tipleriyle döndürür.
    /// Aksi halde kural motoru JsonElement ile karşılaştırma yapmak zorunda kalırdı.
    /// </summary>
    public static List<Dictionary<string, object?>> DeserializeRows(string json)
    {
        var raw = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(json, Options)
                  ?? new List<Dictionary<string, object?>>();

        foreach (var row in raw)
            foreach (var key in row.Keys.ToList())
                row[key] = ValueHelper.Normalize(row[key]);

        return raw;
    }
}
