namespace DataFlow.DataAccess.Entities;

/// <summary>
/// Sisteme yüklenen ham veri seti. Kolon yapısı dosyadan dosyaya değiştiği için
/// satırlar ilişkisel kolonlara değil, JSON metin olarak saklanır (schema-less yaklaşım).
/// </summary>
public class UploadedFile
{
    public int Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    /// <summary>csv | xlsx | json | api</summary>
    public string SourceType { get; set; } = "csv";

    public long SizeInBytes { get; set; }

    /// <summary>Ham satırların JSON hali: [{"Ad":"Ali","Yas":34}, ...]</summary>
    public string RawDataJson { get; set; } = "[]";

    /// <summary>Tespit edilen kolon adları: ["Ad","Yas","Maas"]</summary>
    public string ColumnsJson { get; set; } = "[]";

    /// <summary>Yükleme anında çıkarılan veri kalitesi raporu (null oranı, tip tutarsızlığı vb.)</summary>
    public string? QualityReportJson { get; set; }

    public int RowCount { get; set; }
    public int ColumnCount { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }
    public User? User { get; set; }

    public ICollection<ProcessedDataset> ProcessedDatasets { get; set; } = new List<ProcessedDataset>();
}
