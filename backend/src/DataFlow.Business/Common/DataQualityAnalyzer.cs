namespace DataFlow.Business.Common;

/// <summary>
/// Yüklenen ham veri setinin "ne kadar bozuk" olduğunu ölçer.
/// Kullanıcı kural yazmadan önce hangi kolonun sorunlu olduğunu görür.
/// </summary>
public static class DataQualityAnalyzer
{
    public static QualityReport Analyze(DatasetModel dataset)
    {
        var report = new QualityReport
        {
            RowCount = dataset.RowCount,
            ColumnCount = dataset.ColumnCount
        };

        if (dataset.RowCount == 0) return report;

        foreach (var column in dataset.Columns)
        {
            var values = dataset.ColumnValues(column);
            var inferred = ValueHelper.InferType(values);

            var nullCount = values.Count(ValueHelper.IsNullish);
            var distinct = values
                .Select(v => ValueHelper.AsString(v) ?? "\0null")
                .Distinct()
                .Count();

            // Kolonun baskın tipine uymayan hücreler = tip tutarsızlığı
            var mismatched = 0;
            if (inferred is "number" or "date")
            {
                foreach (var value in values)
                {
                    if (ValueHelper.IsNullish(value)) continue;
                    var ok = inferred == "number"
                        ? ValueHelper.TryAsNumber(value, out _)
                        : ValueHelper.TryAsDate(value, out _);
                    if (!ok) mismatched++;
                }
            }

            report.Columns.Add(new ColumnProfile
            {
                Name = column,
                InferredType = inferred,
                NullCount = nullCount,
                NullRatio = Math.Round((double)nullCount / dataset.RowCount, 4),
                DistinctCount = distinct,
                TypeMismatchCount = mismatched,
                SampleValues = values
                    .Where(v => !ValueHelper.IsNullish(v))
                    .Select(v => ValueHelper.AsString(v)!)
                    .Distinct()
                    .Take(5)
                    .ToList()
            });
        }

        // Tam tekrar eden satırlar
        var signatures = dataset.Rows.Select(r =>
            string.Join("", dataset.Columns.Select(c =>
                ValueHelper.AsString(r.TryGetValue(c, out var v) ? v : null) ?? "")));
        report.DuplicateRowCount = dataset.RowCount - signatures.Distinct().Count();

        report.TotalNullCells = report.Columns.Sum(c => c.NullCount);
        report.TotalTypeMismatches = report.Columns.Sum(c => c.TypeMismatchCount);

        var totalCells = (long)dataset.RowCount * Math.Max(dataset.ColumnCount, 1);
        var problematic = report.TotalNullCells + report.TotalTypeMismatches
                          + (long)report.DuplicateRowCount * dataset.ColumnCount;

        report.QualityScore = totalCells == 0
            ? 100
            : (int)Math.Round(100 * (1 - Math.Min(1.0, (double)problematic / totalCells)));

        report.Warnings = BuildWarnings(report);
        return report;
    }

    private static List<string> BuildWarnings(QualityReport report)
    {
        var warnings = new List<string>();

        foreach (var column in report.Columns)
        {
            if (column.NullRatio >= 0.5)
                warnings.Add($"'{column.Name}' kolonunun %{column.NullRatio * 100:0} kadarı boş.");

            if (column.TypeMismatchCount > 0)
                warnings.Add($"'{column.Name}' kolonunda {column.TypeMismatchCount} hücre " +
                             $"beklenen '{column.InferredType}' tipine uymuyor.");

            if (column.InferredType == "empty")
                warnings.Add($"'{column.Name}' kolonu tamamen boş — silinebilir.");

            if (column.DistinctCount == 1 && report.RowCount > 1)
                warnings.Add($"'{column.Name}' kolonundaki tüm satırlar aynı değere sahip.");
        }

        if (report.DuplicateRowCount > 0)
            warnings.Add($"{report.DuplicateRowCount} adet birebir tekrar eden satır var.");

        return warnings;
    }
}

public class QualityReport
{
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public int DuplicateRowCount { get; set; }
    public int TotalNullCells { get; set; }
    public int TotalTypeMismatches { get; set; }

    /// <summary>0-100 arası veri sağlık skoru.</summary>
    public int QualityScore { get; set; } = 100;

    public List<ColumnProfile> Columns { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class ColumnProfile
{
    public string Name { get; set; } = string.Empty;
    public string InferredType { get; set; } = "text";
    public int NullCount { get; set; }
    public double NullRatio { get; set; }
    public int DistinctCount { get; set; }
    public int TypeMismatchCount { get; set; }
    public List<string> SampleValues { get; set; } = new();
}
