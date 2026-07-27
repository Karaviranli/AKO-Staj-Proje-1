using System.Text.RegularExpressions;
using DataFlow.Business.Common;
using DataFlow.Business.Dtos.Rules;

namespace DataFlow.Business.Concrete.Rules;

/// <summary>
/// Bir satırın, kullanıcı tarafından arayüzden kurgulanan koşul ağacını sağlayıp
/// sağlamadığını değerlendirir. AND/OR ve iç içe gruplar desteklenir.
/// </summary>
public static class ConditionEvaluator
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(200);

    /// <summary>Koşul yoksa satır otomatik olarak eşleşmiş sayılır (kural tüm satırlara uygulanır).</summary>
    public static bool Matches(Dictionary<string, object?> row, ConditionGroupDto? group)
    {
        if (group is null) return true;
        if (group.Conditions.Count == 0 && group.Groups.Count == 0) return true;

        var isOr = string.Equals(group.Logic, "OR", StringComparison.OrdinalIgnoreCase);
        var results = new List<bool>(group.Conditions.Count + group.Groups.Count);

        foreach (var condition in group.Conditions)
            results.Add(Evaluate(row, condition));

        foreach (var nested in group.Groups)
            results.Add(Matches(row, nested));

        return isOr ? results.Any(r => r) : results.All(r => r);
    }

    private static bool Evaluate(Dictionary<string, object?> row, ConditionDto condition)
    {
        // Kolon adı büyük/küçük harf duyarsız aranır — bozuk başlıklara tolerans.
        var key = row.Keys.FirstOrDefault(k =>
            string.Equals(k, condition.Column, StringComparison.OrdinalIgnoreCase));

        var cell = key is null ? null : ValueHelper.Normalize(row[key]);
        var target = condition.Value;

        switch (condition.Operator)
        {
            case ConditionOperators.IsNull:
                return ValueHelper.IsNullish(cell);
            case ConditionOperators.IsNotNull:
                return !ValueHelper.IsNullish(cell);
            case ConditionOperators.IsEmpty:
                return string.IsNullOrWhiteSpace(ValueHelper.AsString(cell));
            case ConditionOperators.IsNotEmpty:
                return !string.IsNullOrWhiteSpace(ValueHelper.AsString(cell));
            case ConditionOperators.IsNumeric:
                return ValueHelper.TryAsNumber(cell, out _);
            case ConditionOperators.IsNotNumeric:
                return !ValueHelper.TryAsNumber(cell, out _);
        }

        // Buradan sonraki operatörler bir karşılaştırma değeri gerektirir.
        if (target is null) return false;

        // Metin operatörlerinde baştaki/sondaki boşluk yok sayılır; kullanıcı
        // " Ankara" ile "Ankara" arasındaki farkı düşünmek zorunda kalmamalı.
        var cellText = ValueHelper.AsString(cell)?.Trim() ?? string.Empty;
        var comparison = condition.CaseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        return condition.Operator switch
        {
            ConditionOperators.Equals =>
                ValueHelper.Compare(cell, target, condition.CaseSensitive) == 0,

            ConditionOperators.NotEquals =>
                ValueHelper.Compare(cell, target, condition.CaseSensitive) != 0,

            // Sayısal/tarihsel karşılaştırmalarda boş hücre asla eşleşmez.
            ConditionOperators.GreaterThan =>
                !ValueHelper.IsNullish(cell) && ValueHelper.Compare(cell, target, condition.CaseSensitive) > 0,

            ConditionOperators.GreaterOrEqual =>
                !ValueHelper.IsNullish(cell) && ValueHelper.Compare(cell, target, condition.CaseSensitive) >= 0,

            ConditionOperators.LessThan =>
                !ValueHelper.IsNullish(cell) && ValueHelper.Compare(cell, target, condition.CaseSensitive) < 0,

            ConditionOperators.LessOrEqual =>
                !ValueHelper.IsNullish(cell) && ValueHelper.Compare(cell, target, condition.CaseSensitive) <= 0,

            ConditionOperators.Between =>
                !ValueHelper.IsNullish(cell)
                && ValueHelper.Compare(cell, target, condition.CaseSensitive) >= 0
                && ValueHelper.Compare(cell, condition.Value2, condition.CaseSensitive) <= 0,

            ConditionOperators.Contains => cellText.Contains(target, comparison),
            ConditionOperators.NotContains => !cellText.Contains(target, comparison),
            ConditionOperators.StartsWith => cellText.StartsWith(target, comparison),
            ConditionOperators.EndsWith => cellText.EndsWith(target, comparison),

            ConditionOperators.In => SplitList(target).Any(x => x.Equals(cellText, comparison)),
            ConditionOperators.NotIn => !SplitList(target).Any(x => x.Equals(cellText, comparison)),

            ConditionOperators.Regex => SafeRegex(cellText, target, condition.CaseSensitive),

            _ => false
        };
    }

    private static IEnumerable<string> SplitList(string raw)
        => raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool SafeRegex(string input, string pattern, bool caseSensitive)
    {
        try
        {
            var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            return Regex.IsMatch(input, pattern, options, RegexTimeout);
        }
        catch (ArgumentException)      // geçersiz desen
        {
            return false;
        }
        catch (RegexMatchTimeoutException)  // katastrofik geri izleme koruması
        {
            return false;
        }
    }
}
