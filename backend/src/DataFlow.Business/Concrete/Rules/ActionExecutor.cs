using System.Globalization;
using DataFlow.Business.Common;
using DataFlow.Business.Dtos.Rules;

namespace DataFlow.Business.Concrete.Rules;

/// <summary>
/// Koşulu sağlayan satırlara aksiyonu uygular. Satır seviyesi aksiyonlar
/// (silme/tutma) motor tarafından, hücre seviyesi aksiyonlar burada işlenir.
/// </summary>
public static class ActionExecutor
{
    /// <summary>
    /// Tek bir satıra hücre seviyesi aksiyonu uygular.
    /// </summary>
    /// <returns>Hücre gerçekten değiştiyse true.</returns>
    public static bool ApplyToRow(Dictionary<string, object?> row, RuleActionDto action, DatasetModel dataset)
    {
        var column = action.TargetColumn;
        if (string.IsNullOrWhiteSpace(column)) return false;

        // Kolon adını mevcut şemada büyük/küçük harf duyarsız çöz;
        // bulunamazsa aksiyon yeni bir kolon oluşturuyor demektir.
        var key = row.Keys.FirstOrDefault(k => string.Equals(k, column, StringComparison.OrdinalIgnoreCase))
                  ?? column;

        var stored = row.TryGetValue(key, out var raw) ? raw : null;
        var current = ValueHelper.Normalize(stored);
        object? next = current;

        switch (action.Type)
        {
            case ActionTypes.SetValue:
                next = ParseLiteral(action.Value);
                break;

            case ActionTypes.FillNull:
                if (!ValueHelper.IsNullish(current)) return false;
                next = ParseLiteral(action.Value);
                break;

            case ActionTypes.Trim:
                next = ValueHelper.AsString(current)?.Trim();
                break;

            case ActionTypes.ToUpper:
                next = ValueHelper.AsString(current)?.ToUpper(TurkishCulture);
                break;

            case ActionTypes.ToLower:
                next = ValueHelper.AsString(current)?.ToLower(TurkishCulture);
                break;

            case ActionTypes.ToTitleCase:
                var lowered = ValueHelper.AsString(current)?.ToLower(TurkishCulture);
                next = lowered is null ? null : TurkishCulture.TextInfo.ToTitleCase(lowered);
                break;

            case ActionTypes.Replace:
                var text = ValueHelper.AsString(current);
                next = text is null || action.Value is null
                    ? text
                    : text.Replace(action.Value, action.Value2 ?? string.Empty);
                break;

            case ActionTypes.RemoveSpaces:
                next = ValueHelper.AsString(current) is { } sp
                    ? new string(sp.Where(c => !char.IsWhiteSpace(c)).ToArray())
                    : null;
                break;

            case ActionTypes.OnlyDigits:
                next = ValueHelper.AsString(current) is { } dg
                    ? new string(dg.Where(char.IsDigit).ToArray())
                    : null;
                break;

            case ActionTypes.Multiply:
            case ActionTypes.Divide:
            case ActionTypes.Add:
            case ActionTypes.Subtract:
                next = Arithmetic(current, action);
                break;

            case ActionTypes.Round:
                if (!ValueHelper.TryAsNumber(current, out var toRound)) return false;
                var digits = int.TryParse(action.Value, out var d) ? Math.Clamp(d, 0, 15) : 0;
                next = Math.Round(toRound, digits, MidpointRounding.AwayFromZero);
                break;

            case ActionTypes.Abs:
                if (!ValueHelper.TryAsNumber(current, out var toAbs)) return false;
                next = Math.Abs(toAbs);
                break;

            case ActionTypes.CastNumber:
                next = ValueHelper.TryAsNumber(current, out var num) ? num : null;
                break;

            case ActionTypes.CastDate:
                next = ValueHelper.TryAsDate(current, out var dt)
                    ? dt.ToString(string.IsNullOrWhiteSpace(action.Value) ? "yyyy-MM-dd" : action.Value,
                                  CultureInfo.InvariantCulture)
                    : null;
                break;

            case ActionTypes.CastText:
                next = ValueHelper.AsString(current);
                break;

            case ActionTypes.FlagRow:
                key = string.IsNullOrWhiteSpace(action.TargetColumn) ? "_flag" : key;
                next = ParseLiteral(action.Value) ?? true;
                break;

            default:
                return false;
        }

        // Karşılaştırma biçimlendirilmiş metin üzerinden değil, değerin kendisi
        // üzerinden yapılır: AsString ondalığı 10 haneye kırptığı için
        // 17490,550000000003 → 17490,55 yuvarlaması "değişiklik yok" sanılıyordu.
        if (Equals(stored, next)) return false;

        row[key] = next;
        if (dataset.ResolveColumn(key) is null) dataset.Columns.Add(key);
        return true;
    }

    private static object? Arithmetic(object? current, RuleActionDto action)
    {
        if (!ValueHelper.TryAsNumber(current, out var value)) return current;
        if (!ValueHelper.TryAsNumber(action.Value, out var operand)) return current;

        return action.Type switch
        {
            ActionTypes.Multiply => value * operand,
            ActionTypes.Add => value + operand,
            ActionTypes.Subtract => value - operand,
            // Sıfıra bölme: değeri bozmak yerine olduğu gibi bırak.
            ActionTypes.Divide => Math.Abs(operand) < double.Epsilon ? current : value / operand,
            _ => current
        };
    }

    /// <summary>
    /// Kullanıcı arayüzden her değeri metin olarak gönderir. Sayı veya boolean
    /// gibi görünüyorsa uygun tipe çevrilir; aksi halde metin kalır.
    /// </summary>
    private static object? ParseLiteral(string? raw)
    {
        if (raw is null) return null;
        var trimmed = raw.Trim();
        if (trimmed.Length == 0) return string.Empty;

        if (string.Equals(trimmed, "null", StringComparison.OrdinalIgnoreCase)) return null;

        if (double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out var n))
            return n;

        if (bool.TryParse(trimmed, out var b)) return b;

        return trimmed;
    }

    private static readonly CultureInfo TurkishCulture = new("tr-TR");
}
