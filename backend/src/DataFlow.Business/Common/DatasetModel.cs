namespace DataFlow.Business.Common;

/// <summary>
/// Bellek içi, şemasız veri seti. Kolon adları çalışma zamanında dosyadan öğrenilir,
/// bu yüzden katı bir sınıf yerine sözlük tabanlı satır modeli kullanılır.
/// </summary>
public class DatasetModel
{
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();

    public int RowCount => Rows.Count;
    public int ColumnCount => Columns.Count;

    public static DatasetModel FromRows(List<Dictionary<string, object?>> rows)
    {
        var model = new DatasetModel { Rows = rows };
        model.RebuildColumns();
        return model;
    }

    /// <summary>Satırlarda geçen tüm kolonları ilk görülme sırasına göre toplar.</summary>
    public void RebuildColumns()
    {
        var seen = new List<string>();
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in Rows)
            foreach (var key in row.Keys)
                if (set.Add(key)) seen.Add(key);

        Columns = seen;
    }

    /// <summary>Tüm satırlarda eksik kolonları null ile tamamlar (dikdörtgen veri garantisi).</summary>
    public void Normalize()
    {
        foreach (var row in Rows)
            foreach (var col in Columns)
                if (!row.ContainsKey(col)) row[col] = null;
    }

    /// <summary>Kolonun gerçek adını büyük/küçük harf duyarsız bulur; yoksa null döner.</summary>
    public string? ResolveColumn(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return Columns.FirstOrDefault(c => string.Equals(c, name, StringComparison.OrdinalIgnoreCase));
    }

    public DatasetModel Clone() => new()
    {
        Columns = new List<string>(Columns),
        Rows = Rows.Select(r => new Dictionary<string, object?>(r)).ToList()
    };

    public List<object?> ColumnValues(string column)
        => Rows.Select(r => r.TryGetValue(column, out var v) ? v : null).ToList();
}
