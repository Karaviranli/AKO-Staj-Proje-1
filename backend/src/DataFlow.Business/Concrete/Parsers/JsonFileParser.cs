using System.Text.Json;
using DataFlow.Business.Abstract;
using DataFlow.Business.Common;

namespace DataFlow.Business.Concrete.Parsers;

/// <summary>
/// JSON okuyucu. Üç yaygın şekli destekler:
///   1) Doğrudan dizi:            [{...},{...}]
///   2) Sarmalanmış dizi:         {"data":[{...}]} / {"items":[...]} / {"records":[...]}
///   3) Tek nesne:                {...}
/// </summary>
public class JsonFileParser : IFileParser
{
    public string Extension => ".json";

    private static readonly string[] WrapperKeys = { "data", "items", "records", "rows", "result", "results" };

    public DatasetModel Parse(Stream stream, string fileName)
    {
        using var doc = JsonDocument.Parse(stream, new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });

        var root = doc.RootElement;
        var rows = new List<Dictionary<string, object?>>();

        switch (root.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in root.EnumerateArray())
                    AddRow(rows, item);
                break;

            case JsonValueKind.Object:
                var wrapped = WrapperKeys
                    .Select(key => root.TryGetProperty(key, out var v) ? (JsonElement?)v : null)
                    .FirstOrDefault(v => v is { ValueKind: JsonValueKind.Array });

                if (wrapped is { } array)
                    foreach (var item in array.EnumerateArray())
                        AddRow(rows, item);
                else
                    AddRow(rows, root);
                break;

            default:
                throw new InvalidDataException(
                    "JSON kökü bir nesne veya dizi olmalıdır.");
        }

        return DatasetModel.FromRows(rows);
    }

    private static void AddRow(List<Dictionary<string, object?>> rows, JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            // İlkel değerlerden oluşan dizi: [1,2,3] -> tek kolonlu tablo
            rows.Add(new Dictionary<string, object?> { ["Deger"] = ValueHelper.Normalize(element) });
            return;
        }

        var row = new Dictionary<string, object?>();
        Flatten(element, prefix: null, row);
        rows.Add(row);
    }

    /// <summary>
    /// İç içe nesneleri "adres.sehir" biçiminde düzleştirir; diziler JSON metni olarak saklanır.
    /// Tablo görünümü ancak düz (flat) veriyle mümkün olduğu için gereklidir.
    /// </summary>
    private static void Flatten(JsonElement element, string? prefix, Dictionary<string, object?> row)
    {
        foreach (var property in element.EnumerateObject())
        {
            var key = prefix is null ? property.Name : $"{prefix}.{property.Name}";

            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    Flatten(property.Value, key, row);
                    break;
                case JsonValueKind.Array:
                    row[key] = property.Value.GetRawText();
                    break;
                default:
                    row[key] = ValueHelper.Normalize(property.Value);
                    break;
            }
        }
    }
}
