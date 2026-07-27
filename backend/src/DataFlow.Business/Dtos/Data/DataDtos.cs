using DataFlow.Business.Common;
using DataFlow.Business.Dtos.Rules;

namespace DataFlow.Business.Dtos.Data;

/// <summary>Yükleme sonrası dönen özet + önizleme.</summary>
public class UploadResultDto
{
    public int FileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public long SizeInBytes { get; set; }
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public DateTime UploadedAt { get; set; }

    public List<string> Columns { get; set; } = new();

    /// <summary>Ağ trafiğini şişirmemek için yalnızca ilk N satır.</summary>
    public List<Dictionary<string, object?>> Preview { get; set; } = new();

    public QualityReport? Quality { get; set; }
}

/// <summary>POST ile doğrudan JSON veri gönderimi (dosyasız yükleme).</summary>
public class PostDataRequestDto
{
    public string DatasetName { get; set; } = "API Verisi";

    /// <summary>satis | calisan | genel — hangi iş alanına ait olduğu.</summary>
    public string Category { get; set; } = "genel";

    public List<Dictionary<string, object?>> Rows { get; set; } = new();
}

/// <summary>Kural setini bir veri setine uygulama isteği.</summary>
public class ProcessRequestDto
{
    public int FileId { get; set; }

    public string? Name { get; set; }

    public List<RuleDto> Rules { get; set; } = new();

    /// <summary>true ise sonuç veritabanına yazılmaz, sadece önizleme döner.</summary>
    public bool DryRun { get; set; }
}

public class ProcessResultDto
{
    public int? ProcessedDatasetId { get; set; }
    public int FileId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool DryRun { get; set; }

    public int RowsBefore { get; set; }
    public int RowsAfter { get; set; }
    public int CellsModified { get; set; }
    public int DurationMs { get; set; }

    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public List<RuleExecutionLogDto> ExecutionLog { get; set; } = new();
    public QualityReport? QualityAfter { get; set; }
}

/// <summary>Liste ekranları için hafif özet modeli (satır verisi taşımaz).</summary>
public class FileSummaryDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public long SizeInBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public int QualityScore { get; set; }
    public int ProcessedCount { get; set; }
}

public class ProcessedSummaryDto
{
    public int Id { get; set; }
    public int UploadedFileId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int RowsBefore { get; set; }
    public int RowsAfter { get; set; }
    public int CellsModified { get; set; }
    public int RuleCount { get; set; }
    public DateTime ProcessedAt { get; set; }
}

/// <summary>Sayfalanmış veri okuma (büyük veri setleri için).</summary>
public class PagedDataDto
{
    public List<string> Columns { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalRows { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalRows / PageSize);
}
