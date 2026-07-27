namespace DataFlow.DataAccess.Entities;

/// <summary>
/// Kural motorundan (pipeline) geçmiş, temizlenmiş veri seti.
/// Her çalıştırma yeni bir kayıt üretir; böylece işlem geçmişi (audit trail) korunur.
/// </summary>
public class ProcessedDataset
{
    public int Id { get; set; }

    public int UploadedFileId { get; set; }
    public UploadedFile? UploadedFile { get; set; }

    public int UserId { get; set; }

    /// <summary>Kullanıcının bu çalıştırmaya verdiği ad. Örn: "Satış - Yaş segmentasyonu v2"</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Uygulanan kural setinin tam JSON hali (tekrar üretilebilirlik / reproducibility için)</summary>
    public string AppliedRulesJson { get; set; } = "[]";

    /// <summary>Her kuralın kaç satırı etkilediğini gösteren adım adım yürütme raporu</summary>
    public string ExecutionLogJson { get; set; } = "[]";

    /// <summary>Temizlenmiş nihai veri (frontend tabloya bunu basar)</summary>
    public string CleanDataJson { get; set; } = "[]";

    public string ColumnsJson { get; set; } = "[]";

    public int RowsBefore { get; set; }
    public int RowsAfter { get; set; }
    public int CellsModified { get; set; }
    public int DurationMs { get; set; }

    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
