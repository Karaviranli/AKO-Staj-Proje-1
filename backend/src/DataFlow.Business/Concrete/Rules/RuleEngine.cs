using System.Diagnostics;
using DataFlow.Business.Abstract;
using DataFlow.Business.Common;
using DataFlow.Business.Dtos.Rules;

namespace DataFlow.Business.Concrete.Rules;

/// <summary>
/// Kuralları SIRAYLA çalıştıran veri işleme hattı (pipeline).
/// Bir kuralın çıktısı bir sonraki kuralın girdisidir; bu sayede
/// "önce boşlukları doldur, sonra segmentle" gibi bağımlı kurgular mümkün olur.
///
/// Her adım için ayrı bir yürütme raporu (RuleExecutionLogDto) üretilir —
/// bu rapor hem kullanıcıya şeffaflık sağlar hem de sunumun ana çıktısıdır.
/// </summary>
public class RuleEngine : IRuleEngine
{
    public RuleEngineResult Execute(DatasetModel dataset, IEnumerable<RuleDto> rules)
    {
        var totalWatch = Stopwatch.StartNew();

        var working = dataset.Clone();
        working.Normalize();

        var result = new RuleEngineResult
        {
            RowsBefore = working.RowCount
        };

        var ordered = rules.Where(r => r.Enabled).OrderBy(r => r.Order).ToList();

        foreach (var rule in ordered)
        {
            var stepWatch = Stopwatch.StartNew();
            var rowsBefore = working.RowCount;

            var log = new RuleExecutionLogDto
            {
                Order = rule.Order,
                RuleName = string.IsNullOrWhiteSpace(rule.Name) ? $"Kural #{rule.Order}" : rule.Name,
                RowsBefore = rowsBefore
            };

            try
            {
                if (ActionTypes.DatasetLevel.Contains(rule.Action.Type))
                    ApplyDatasetLevel(working, rule, log);
                else
                    ApplyRowLevel(working, rule, log);
            }
            catch (Exception ex)
            {
                // Tek bir bozuk kural tüm işlemi çökertmemeli; adım atlanır ve raporlanır.
                log.Skipped = true;
                log.Warning = $"Kural çalıştırılamadı: {ex.Message}";
            }

            stepWatch.Stop();
            log.RowsAfter = working.RowCount;
            log.DurationMs = (int)stepWatch.ElapsedMilliseconds;
            log.Summary = BuildSummary(rule, log);

            result.CellsModified += log.CellsModified;
            result.Logs.Add(log);
        }

        working.RebuildColumns();
        working.Normalize();

        totalWatch.Stop();
        result.Dataset = working;
        result.RowsAfter = working.RowCount;
        result.DurationMs = (int)totalWatch.ElapsedMilliseconds;
        return result;
    }

    private static void ApplyRowLevel(DatasetModel dataset, RuleDto rule, RuleExecutionLogDto log)
    {
        var matched = new List<Dictionary<string, object?>>();

        foreach (var row in dataset.Rows)
            if (ConditionEvaluator.Matches(row, rule.Condition))
                matched.Add(row);

        log.RowsMatched = matched.Count;

        switch (rule.Action.Type)
        {
            case ActionTypes.DeleteRow:
            {
                var doomed = new HashSet<Dictionary<string, object?>>(matched, ReferenceComparer.Instance);
                dataset.Rows = dataset.Rows.Where(r => !doomed.Contains(r)).ToList();
                return;
            }

            case ActionTypes.KeepRow:
            {
                var keep = new HashSet<Dictionary<string, object?>>(matched, ReferenceComparer.Instance);
                dataset.Rows = dataset.Rows.Where(keep.Contains).ToList();
                return;
            }

            default:
            {
                var changed = 0;
                foreach (var row in matched)
                    if (ActionExecutor.ApplyToRow(row, rule.Action, dataset))
                        changed++;

                log.CellsModified = changed;

                if (changed == 0 && matched.Count > 0)
                    log.Warning = "Koşula uyan satır bulundu ancak hiçbir hücre değişmedi " +
                                  "(değer zaten aynı ya da hedef kolon uygun tipte değil).";
                return;
            }
        }
    }

    private static void ApplyDatasetLevel(DatasetModel dataset, RuleDto rule, RuleExecutionLogDto log)
    {
        var action = rule.Action;
        var column = dataset.ResolveColumn(action.TargetColumn);

        switch (action.Type)
        {
            case ActionTypes.RenameColumn:
            {
                if (column is null || string.IsNullOrWhiteSpace(action.Value2))
                {
                    log.Skipped = true;
                    log.Warning = $"'{action.TargetColumn}' kolonu bulunamadı veya yeni ad boş.";
                    return;
                }

                var newName = action.Value2.Trim();
                foreach (var row in dataset.Rows)
                {
                    if (!row.Remove(column, out var value)) continue;
                    row[newName] = value;
                    log.CellsModified++;
                }

                var index = dataset.Columns.IndexOf(column);
                if (index >= 0) dataset.Columns[index] = newName;
                return;
            }

            case ActionTypes.DropColumn:
            {
                if (column is null)
                {
                    log.Skipped = true;
                    log.Warning = $"'{action.TargetColumn}' kolonu bulunamadı.";
                    return;
                }

                foreach (var row in dataset.Rows)
                    if (row.Remove(column)) log.CellsModified++;

                dataset.Columns.Remove(column);
                return;
            }

            case ActionTypes.CopyColumn:
            {
                if (column is null || string.IsNullOrWhiteSpace(action.Value2))
                {
                    log.Skipped = true;
                    log.Warning = "Kaynak kolon bulunamadı veya hedef kolon adı boş.";
                    return;
                }

                var target = action.Value2.Trim();
                foreach (var row in dataset.Rows)
                {
                    row[target] = row.TryGetValue(column, out var v) ? v : null;
                    log.CellsModified++;
                }

                if (dataset.ResolveColumn(target) is null) dataset.Columns.Add(target);
                return;
            }

            case ActionTypes.Deduplicate:
            {
                // Kolon belirtilmişse o kolona göre, belirtilmemişse tüm satıra göre tekilleştir.
                var keys = column is not null
                    ? new List<string> { column }
                    : dataset.Columns;

                var seen = new HashSet<string>();
                var unique = new List<Dictionary<string, object?>>();

                foreach (var row in dataset.Rows)
                {
                    var signature = string.Join("", keys.Select(k =>
                        ValueHelper.AsString(row.TryGetValue(k, out var v) ? v : null) ?? ""));

                    if (seen.Add(signature)) unique.Add(row);
                }

                log.RowsMatched = dataset.Rows.Count - unique.Count;
                dataset.Rows = unique;
                return;
            }
        }
    }

    private static string BuildSummary(RuleDto rule, RuleExecutionLogDto log)
    {
        if (log.Skipped) return log.Warning ?? "Kural atlandı.";

        var label = ActionTypes.Labels.TryGetValue(rule.Action.Type, out var l) ? l : rule.Action.Type;
        var removed = log.RowsBefore - log.RowsAfter;

        return rule.Action.Type switch
        {
            ActionTypes.DeleteRow =>
                $"{removed} satır silindi ({log.RowsBefore} → {log.RowsAfter}).",

            ActionTypes.KeepRow =>
                $"Koşulu sağlamayan {removed} satır elendi ({log.RowsBefore} → {log.RowsAfter}).",

            ActionTypes.Deduplicate =>
                $"{log.RowsMatched} tekrar eden satır silindi ({log.RowsBefore} → {log.RowsAfter}).",

            ActionTypes.RenameColumn =>
                $"'{rule.Action.TargetColumn}' → '{rule.Action.Value2}' olarak yeniden adlandırıldı.",

            ActionTypes.DropColumn =>
                $"'{rule.Action.TargetColumn}' kolonu kaldırıldı.",

            ActionTypes.CopyColumn =>
                $"'{rule.Action.TargetColumn}' kolonu '{rule.Action.Value2}' adıyla kopyalandı.",

            _ =>
                $"{log.RowsMatched} satır koşula uydu, {log.CellsModified} hücrede '{label}' uygulandı."
        };
    }

    /// <summary>Satır sözlüklerini içeriğe göre değil referansa göre karşılaştırır.</summary>
    private sealed class ReferenceComparer : IEqualityComparer<Dictionary<string, object?>>
    {
        public static readonly ReferenceComparer Instance = new();

        public bool Equals(Dictionary<string, object?>? x, Dictionary<string, object?>? y)
            => ReferenceEquals(x, y);

        public int GetHashCode(Dictionary<string, object?> obj)
            => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
