using System.Globalization;
using System.Text.Json;

namespace DataFlow.Business.Common;

/// <summary>
/// Bozuk/karışık veri setleriyle çalışırken tip zorlama (type coercion) katmanı.
/// "1.234,56 TL", " 42 ", "true", "2024-03-01" gibi tutarsız girdileri
/// karşılaştırılabilir CLR tiplerine indirger.
/// </summary>
public static class ValueHelper
{
    private static readonly string[] DateFormats =
    {
        "yyyy-MM-dd", "dd.MM.yyyy", "dd/MM/yyyy", "MM/dd/yyyy", "yyyy/MM/dd",
        "yyyy-MM-ddTHH:mm:ss", "dd.MM.yyyy HH:mm", "yyyy-MM-dd HH:mm:ss"
    };

    private static readonly HashSet<string> NullTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "null", "nil", "n/a", "na", "-", "--", "yok", "bilinmiyor", "undefined", "nan", "#n/a"
    };

    /// <summary>JsonElement gibi sarmalanmış değerleri düz CLR tipine indirger.</summary>
    public static object? Normalize(object? value)
    {
        if (value is null) return null;

        if (value is JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => je.TryGetDouble(out var d) ? d : je.GetRawText(),
                JsonValueKind.String => je.GetString(),
                _ => je.GetRawText()
            };
        }

        if (value is string s)
        {
            // Yalnızca "eksik veri" ifadeleri null'a indirgenir. Metin OLDUĞU GİBİ
            // korunur — baştaki/sondaki boşluğu kırpmak bir dönüşümdür ve kullanıcının
            // açıkça "Baş/son boşlukları kırp" kuralını seçmesiyle yapılmalıdır.
            return NullTokens.Contains(s.Trim()) ? null : s;
        }

        return value;
    }

    /// <summary>Değer "yok" sayılmalı mı? (null, boş metin veya n/a benzeri token)</summary>
    public static bool IsNullish(object? value)
    {
        var v = Normalize(value);
        if (v is null) return true;
        return v is string s && NullTokens.Contains(s.Trim());
    }

    public static string? AsString(object? value)
    {
        var v = Normalize(value);
        return v switch
        {
            null => null,
            string s => s,
            double d => d.ToString("0.##########", CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            DateTime dt => dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            _ => Convert.ToString(v, CultureInfo.InvariantCulture)
        };
    }

    /// <summary>
    /// Sayıya çevirmeyi dener. "1.234,56 TL", "%12", "3 500" gibi kirli girdileri de tolere eder.
    /// </summary>
    public static bool TryAsNumber(object? value, out double result)
    {
        result = 0;
        var v = Normalize(value);

        switch (v)
        {
            case null: return false;
            case double d: result = d; return true;
            case int i: result = i; return true;
            case long l: result = l; return true;
            case decimal m: result = (double)m; return true;
            case bool b: result = b ? 1 : 0; return true;
        }

        if (v is not string raw) return false;

        var cleaned = new string(raw.Where(c => char.IsDigit(c) || c is '.' or ',' or '-' or '+').ToArray());
        if (cleaned.Length == 0) return false;

        // Hem "1.234,56" (TR) hem "1,234.56" (EN) formatını çöz.
        int lastDot = cleaned.LastIndexOf('.');
        int lastComma = cleaned.LastIndexOf(',');

        if (lastDot >= 0 && lastComma >= 0)
        {
            if (lastComma > lastDot)          // 1.234,56 -> ondalık virgül
                cleaned = cleaned.Replace(".", "").Replace(',', '.');
            else                              // 1,234.56 -> ondalık nokta
                cleaned = cleaned.Replace(",", "");
        }
        else if (lastComma >= 0)
        {
            // Tek virgül: binlik ayıracı mı ondalık mı? Sağında tam 3 hane varsa binliktir.
            var decimals = cleaned.Length - lastComma - 1;
            cleaned = decimals == 3 ? cleaned.Replace(",", "") : cleaned.Replace(',', '.');
        }
        else if (lastDot >= 0)
        {
            // Tek nokta için de aynı belirsizlik var: "22.000" Türkçe veride
            // yirmi iki bindir, 22.0 değil. Sağında tam 3 hane varsa binlik sayılır.
            var decimals = cleaned.Length - lastDot - 1;
            if (decimals == 3) cleaned = cleaned.Replace(".", "");
        }

        return double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
    }

    public static bool TryAsDate(object? value, out DateTime result)
    {
        result = default;
        var v = Normalize(value);
        if (v is DateTime dt) { result = dt; return true; }
        if (v is not string s) return false;

        if (DateTime.TryParseExact(s, DateFormats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out result))
            return true;

        return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
            || DateTime.TryParse(s, new CultureInfo("tr-TR"), DateTimeStyles.None, out result);
    }

    public static bool TryAsBool(object? value, out bool result)
    {
        result = false;
        var v = Normalize(value);
        if (v is bool b) { result = b; return true; }
        if (v is double d) { result = Math.Abs(d) > double.Epsilon; return true; }
        if (v is not string s) return false;

        switch (s.Trim().ToLowerInvariant())
        {
            case "true" or "1" or "evet" or "yes" or "e" or "y" or "var":
                result = true; return true;
            case "false" or "0" or "hayir" or "hayır" or "no" or "h" or "n" or "yok":
                result = false; return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// İki değeri karşılaştırır. Her ikisi de sayıya çevrilebiliyorsa sayısal,
    /// tarihe çevrilebiliyorsa kronolojik, aksi halde metinsel karşılaştırma yapar.
    /// </summary>
    public static int Compare(object? left, object? right, bool caseSensitive = false)
    {
        if (TryAsNumber(left, out var ln) && TryAsNumber(right, out var rn))
            return ln.CompareTo(rn);

        if (TryAsDate(left, out var ld) && TryAsDate(right, out var rd))
            return ld.CompareTo(rd);

        // Karşılaştırmada baştaki/sondaki boşluk anlamlı değildir; " Ankara" ile
        // "Ankara" aynı sayılır. Saklanan değer bundan etkilenmez.
        var ls = AsString(left)?.Trim() ?? string.Empty;
        var rs = AsString(right)?.Trim() ?? string.Empty;
        return string.Compare(ls, rs,
            caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Bir kolonun baskın veri tipini tahmin eder: number | date | boolean | text | empty</summary>
    public static string InferType(IEnumerable<object?> values)
    {
        int total = 0, numbers = 0, dates = 0, bools = 0, nulls = 0;

        foreach (var v in values)
        {
            total++;
            if (IsNullish(v)) { nulls++; continue; }
            if (TryAsNumber(v, out _)) numbers++;
            else if (TryAsDate(v, out _)) dates++;
            else if (TryAsBool(v, out _)) bools++;
        }

        var filled = total - nulls;
        if (filled == 0) return "empty";

        double threshold = filled * 0.8;
        if (numbers >= threshold) return "number";
        if (dates >= threshold) return "date";
        if (bools >= threshold) return "boolean";
        return "text";
    }
}
