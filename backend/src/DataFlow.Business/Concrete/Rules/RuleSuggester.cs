using DataFlow.Business.Common;
using DataFlow.Business.Dtos.Rules;

namespace DataFlow.Business.Concrete.Rules;

/// <summary>
/// Veri setini inceleyip "akıllı temizlik" kuralları önerir. Öneriler yalnızca
/// güvenli ve tersine çevrilebilir dönüşümlerdir (ham veri hiç değişmez, her
/// çalıştırma yeni kayıt üretir). Kullanıcı hepsini ya da bir kısmını uygular.
///
/// Öneri mantığı, DataQualityAnalyzer'ın bulgularıyla aynı sinyalleri kullanır:
/// tamamen boş kolon, baş/son boşluk, tip uyumsuzluğu, eksik değer, tekrar eden satır.
/// </summary>
public static class RuleSuggester
{
    public static List<RuleSuggestionDto> Suggest(DatasetModel dataset)
    {
        var suggestions = new List<RuleSuggestionDto>();
        if (dataset.RowCount == 0) return suggestions;

        foreach (var column in dataset.Columns)
        {
            var values = dataset.ColumnValues(column);
            var type = ValueHelper.InferType(values);
            var nulls = values.Count(ValueHelper.IsNullish);

            // 1) Tamamen boş kolon → sil.
            if (type == "empty")
            {
                suggestions.Add(new RuleSuggestionDto
                {
                    Category = "kolon",
                    Reason = $"'{column}' kolonu tamamen boş.",
                    Impact = dataset.RowCount,
                    Rule = new RuleDto
                    {
                        Name = $"Boş kolonu sil: {column}",
                        Action = new RuleActionDto { Type = ActionTypes.DropColumn, TargetColumn = column }
                    }
                });
                continue;
            }

            // 2) Baş/son boşluk taşıyan metin kolonları → kırp.
            var wsCount = values.Count(v =>
                v is string s && !ValueHelper.IsNullish(v) && s != s.Trim());

            if (type == "text" && wsCount > 0)
            {
                suggestions.Add(new RuleSuggestionDto
                {
                    Category = "temizlik",
                    Reason = $"'{column}' kolonunda {wsCount} hücrede baştaki/sondaki boşluk var.",
                    Impact = wsCount,
                    Rule = new RuleDto
                    {
                        Name = $"Boşlukları kırp: {column}",
                        Action = new RuleActionDto { Type = ActionTypes.Trim, TargetColumn = column }
                    }
                });
            }

            // 3) Sayısal kolonda karışık format / tip uyumsuzluğu → sayıya çevir.
            if (type == "number")
            {
                var mismatch = values.Count(v => !ValueHelper.IsNullish(v) && !ValueHelper.TryAsNumber(v, out _));

                // Zaten temiz tam sayı kolonlarına (MusteriNo, Yas) öneri yapmayalım;
                // yalnızca gerçek uyumsuzluk ya da karışık biçim (virgül/nokta/boşluk/para) varsa öner.
                var mixedFormat = values.Any(v =>
                    v is string s && !ValueHelper.IsNullish(v) &&
                    s.Any(c => c is ',' or '.' or ' ' or '%' || (!char.IsDigit(c) && c is not ('-' or '+'))));

                if (mismatch > 0 || mixedFormat)
                {
                    var reason = mismatch > 0
                        ? $"'{column}' sayısal ama {mismatch} hücre sayıya çevrilemiyor; kalanı da farklı biçimlerde (örn. 1.250,50)."
                        : $"'{column}' sayısal değerler karışık biçimde (örn. 1.250,50 / %10); tek biçime getirilecek.";

                    suggestions.Add(new RuleSuggestionDto
                    {
                        Category = "tip",
                        Reason = reason,
                        Impact = Math.Max(mismatch, 1),
                        Rule = new RuleDto
                        {
                            Name = $"Sayıya çevir: {column}",
                            Action = new RuleActionDto { Type = ActionTypes.CastNumber, TargetColumn = column }
                        }
                    });
                }
            }

            // 4) Tarih kolonu → tek biçime (yyyy-MM-dd) getir.
            if (type == "date")
            {
                var varied = values.Any(v => v is string);
                if (varied)
                {
                    suggestions.Add(new RuleSuggestionDto
                    {
                        Category = "tip",
                        Reason = $"'{column}' tarih kolonu farklı biçimlerde (2025-01-03 / 03.01.2025); tek biçime getirilecek.",
                        Impact = values.Count(v => !ValueHelper.IsNullish(v)),
                        Rule = new RuleDto
                        {
                            Name = $"Tarihi standartlaştır: {column}",
                            Action = new RuleActionDto
                            {
                                Type = ActionTypes.CastDate,
                                TargetColumn = column,
                                Value = "yyyy-MM-dd"
                            }
                        }
                    });
                }
            }

            // 5) Eksik değer taşıyan metin kolonları → "Belirsiz" ile doldur (öneri; kullanıcı değeri değiştirebilir).
            if (type == "text" && nulls > 0)
            {
                suggestions.Add(new RuleSuggestionDto
                {
                    Category = "eksik",
                    Reason = $"'{column}' kolonunda {nulls} boş hücre var; 'Belirsiz' ile doldurulabilir.",
                    Impact = nulls,
                    Rule = new RuleDto
                    {
                        Name = $"Boşsa 'Belirsiz' yaz: {column}",
                        Condition = new ConditionGroupDto
                        {
                            Logic = "AND",
                            Conditions = new List<ConditionDto>
                            {
                                new() { Column = column, Operator = ConditionOperators.IsNull }
                            }
                        },
                        Action = new RuleActionDto
                        {
                            Type = ActionTypes.FillNull,
                            TargetColumn = column,
                            Value = "Belirsiz"
                        }
                    }
                });
            }
        }

        // 6) Tekrar eden satırlar → tekilleştir (en sona; önceki temizlikler tekrarları ortaya çıkarabilir).
        var signatures = dataset.Rows.Select(r =>
            string.Join("", dataset.Columns.Select(c =>
                ValueHelper.AsString(r.TryGetValue(c, out var v) ? v : null)?.Trim() ?? "")));
        var duplicates = dataset.RowCount - signatures.Distinct().Count();

        if (duplicates > 0)
        {
            suggestions.Add(new RuleSuggestionDto
            {
                Category = "tekrar",
                Reason = $"{duplicates} adet birebir tekrar eden satır var.",
                Impact = duplicates,
                Rule = new RuleDto
                {
                    Name = "Tekrar eden satırları sil",
                    Action = new RuleActionDto { Type = ActionTypes.Deduplicate }
                }
            });
        }

        // Sıra numaralarını ata: önce temizlik/tip/eksik (kolon bazlı), en sonda tekilleştirme.
        for (var i = 0; i < suggestions.Count; i++)
            suggestions[i].Rule.Order = i + 1;

        return suggestions;
    }
}
