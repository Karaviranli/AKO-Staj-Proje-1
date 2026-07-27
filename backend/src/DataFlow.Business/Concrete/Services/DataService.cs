using System.Globalization;
using System.Text;
using CsvHelper;
using DataFlow.Business.Abstract;
using DataFlow.Business.Common;
using DataFlow.Business.Concrete.Rules;
using DataFlow.Business.Dtos.Data;
using DataFlow.Business.Dtos.Rules;
using DataFlow.DataAccess.Context;
using DataFlow.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataFlow.Business.Concrete.Services;

public class DataService : IDataService
{
    private const int PreviewRowCount = 100;

    private readonly AppDbContext _db;
    private readonly IFileParserFactory _parserFactory;
    private readonly IRuleEngine _ruleEngine;
    private readonly IAuditService _audit;

    public DataService(
        AppDbContext db,
        IFileParserFactory parserFactory,
        IRuleEngine ruleEngine,
        IAuditService audit)
    {
        _db = db;
        _parserFactory = parserFactory;
        _ruleEngine = ruleEngine;
        _audit = audit;
    }

    // ---------------------------------------------------------------- YÜKLEME

    public async Task<UploadResultDto> UploadFileAsync(Stream stream, string fileName, long size, int userId)
    {
        var parser = _parserFactory.GetParser(fileName);

        DatasetModel dataset;
        try
        {
            dataset = parser.Parse(stream, fileName);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Dosya okunamadı: {ex.Message}");
        }

        if (dataset.RowCount == 0)
            throw new InvalidDataException("Dosyada okunabilir satır bulunamadı.");

        dataset.Normalize();

        var sourceType = Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
        return await PersistAsync(dataset, fileName, sourceType, size, userId);
    }

    public async Task<UploadResultDto> UploadJsonAsync(PostDataRequestDto request, int userId)
    {
        if (request.Rows.Count == 0)
            throw new InvalidDataException("Gönderilen veri boş.");

        // Gelen değerler JsonElement olarak gelir; kural motoru için CLR tipine indirgenir.
        foreach (var row in request.Rows)
            foreach (var key in row.Keys.ToList())
                row[key] = ValueHelper.Normalize(row[key]);

        var dataset = DatasetModel.FromRows(request.Rows);
        dataset.Normalize();

        var size = Encoding.UTF8.GetByteCount(JsonDefaults.Serialize(request.Rows));
        var name = string.IsNullOrWhiteSpace(request.DatasetName) ? "API Verisi" : request.DatasetName.Trim();

        return await PersistAsync(dataset, name, "api", size, userId);
    }

    private async Task<UploadResultDto> PersistAsync(
        DatasetModel dataset, string fileName, string sourceType, long size, int userId)
    {
        var quality = DataQualityAnalyzer.Analyze(dataset);

        var entity = new UploadedFile
        {
            FileName = fileName,
            SourceType = sourceType,
            SizeInBytes = size,
            RowCount = dataset.RowCount,
            ColumnCount = dataset.ColumnCount,
            RawDataJson = JsonDefaults.Serialize(dataset.Rows),
            ColumnsJson = JsonDefaults.Serialize(dataset.Columns),
            QualityReportJson = JsonDefaults.Serialize(quality),
            UserId = userId
        };

        _db.UploadedFiles.Add(entity);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "-", "UPLOAD", $"{fileName} ({dataset.RowCount} satır)");

        return new UploadResultDto
        {
            FileId = entity.Id,
            FileName = entity.FileName,
            SourceType = entity.SourceType,
            SizeInBytes = entity.SizeInBytes,
            RowCount = entity.RowCount,
            ColumnCount = entity.ColumnCount,
            UploadedAt = entity.UploadedAt,
            Columns = dataset.Columns,
            Preview = dataset.Rows.Take(PreviewRowCount).ToList(),
            Quality = quality
        };
    }

    // ---------------------------------------------------------------- OKUMA

    public async Task<List<FileSummaryDto>> GetFilesAsync(int userId)
    {
        // RawDataJson çok büyük olabilir; listede satır verisi taşınmaz.
        var files = await _db.UploadedFiles
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.UploadedAt)
            .Select(f => new
            {
                f.Id, f.FileName, f.SourceType, f.RowCount, f.ColumnCount,
                f.SizeInBytes, f.UploadedAt, f.QualityReportJson,
                ProcessedCount = f.ProcessedDatasets.Count
            })
            .ToListAsync();

        return files.Select(f => new FileSummaryDto
        {
            Id = f.Id,
            FileName = f.FileName,
            SourceType = f.SourceType,
            RowCount = f.RowCount,
            ColumnCount = f.ColumnCount,
            SizeInBytes = f.SizeInBytes,
            UploadedAt = f.UploadedAt,
            ProcessedCount = f.ProcessedCount,
            QualityScore = f.QualityReportJson is null
                ? 100
                : JsonDefaults.Deserialize<QualityReport>(f.QualityReportJson)?.QualityScore ?? 100
        }).ToList();
    }

    public async Task<PagedDataDto?> GetFileDataAsync(int fileId, int userId, int page, int pageSize)
    {
        var file = await _db.UploadedFiles.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);

        if (file is null) return null;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var rows = JsonDefaults.DeserializeRows(file.RawDataJson);
        var columns = JsonDefaults.Deserialize<List<string>>(file.ColumnsJson) ?? new List<string>();

        return new PagedDataDto
        {
            Columns = columns,
            Rows = rows.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalRows = rows.Count
        };
    }

    public async Task<bool> DeleteFileAsync(int fileId, int userId)
    {
        var file = await _db.UploadedFiles
            .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);

        if (file is null) return false;

        _db.UploadedFiles.Remove(file);
        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "-", "DELETE", $"Dosya #{fileId} silindi");
        return true;
    }

    public async Task<List<RuleSuggestionDto>?> SuggestRulesAsync(int fileId, int userId)
    {
        var file = await _db.UploadedFiles.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);

        if (file is null) return null;

        var dataset = new DatasetModel
        {
            Columns = JsonDefaults.Deserialize<List<string>>(file.ColumnsJson) ?? new List<string>(),
            Rows = JsonDefaults.DeserializeRows(file.RawDataJson)
        };

        return RuleSuggester.Suggest(dataset);
    }

    // ---------------------------------------------------------------- İŞLEME

    public async Task<ProcessResultDto?> ProcessAsync(ProcessRequestDto request, int userId)
    {
        var file = await _db.UploadedFiles.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == request.FileId && f.UserId == userId);

        if (file is null) return null;

        var dataset = new DatasetModel
        {
            Columns = JsonDefaults.Deserialize<List<string>>(file.ColumnsJson) ?? new List<string>(),
            Rows = JsonDefaults.DeserializeRows(file.RawDataJson)
        };

        var result = _ruleEngine.Execute(dataset, request.Rules);
        var quality = DataQualityAnalyzer.Analyze(result.Dataset);

        var name = string.IsNullOrWhiteSpace(request.Name)
            ? $"{file.FileName} — {DateTime.Now:dd.MM.yyyy HH:mm}"
            : request.Name.Trim();

        int? savedId = null;

        if (!request.DryRun)
        {
            var processed = new ProcessedDataset
            {
                UploadedFileId = file.Id,
                UserId = userId,
                Name = name,
                AppliedRulesJson = JsonDefaults.Serialize(request.Rules),
                ExecutionLogJson = JsonDefaults.Serialize(result.Logs),
                CleanDataJson = JsonDefaults.Serialize(result.Dataset.Rows),
                ColumnsJson = JsonDefaults.Serialize(result.Dataset.Columns),
                RowsBefore = result.RowsBefore,
                RowsAfter = result.RowsAfter,
                CellsModified = result.CellsModified,
                DurationMs = result.DurationMs
            };

            _db.ProcessedDatasets.Add(processed);
            await _db.SaveChangesAsync();
            savedId = processed.Id;

            await _audit.LogAsync(userId, "-", "PROCESS",
                $"Dosya #{file.Id}, {request.Rules.Count} kural, {result.RowsBefore}→{result.RowsAfter} satır");
        }

        return new ProcessResultDto
        {
            ProcessedDatasetId = savedId,
            FileId = file.Id,
            Name = name,
            DryRun = request.DryRun,
            RowsBefore = result.RowsBefore,
            RowsAfter = result.RowsAfter,
            CellsModified = result.CellsModified,
            DurationMs = result.DurationMs,
            Columns = result.Dataset.Columns,
            Rows = result.Dataset.Rows.Take(PreviewRowCount).ToList(),
            ExecutionLog = result.Logs,
            QualityAfter = quality
        };
    }

    public async Task<List<ProcessedSummaryDto>> GetProcessedAsync(int userId)
    {
        var items = await _db.ProcessedDatasets
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.ProcessedAt)
            .Select(p => new
            {
                p.Id, p.UploadedFileId, p.Name, p.RowsBefore, p.RowsAfter,
                p.CellsModified, p.ProcessedAt, p.AppliedRulesJson,
                FileName = p.UploadedFile!.FileName
            })
            .ToListAsync();

        return items.Select(p => new ProcessedSummaryDto
        {
            Id = p.Id,
            UploadedFileId = p.UploadedFileId,
            FileName = p.FileName,
            Name = p.Name,
            RowsBefore = p.RowsBefore,
            RowsAfter = p.RowsAfter,
            CellsModified = p.CellsModified,
            ProcessedAt = p.ProcessedAt,
            RuleCount = JsonDefaults.Deserialize<List<RuleDto>>(p.AppliedRulesJson)?.Count ?? 0
        }).ToList();
    }

    public async Task<ProcessResultDto?> GetProcessedDetailAsync(int id, int userId)
    {
        var p = await _db.ProcessedDatasets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (p is null) return null;

        return new ProcessResultDto
        {
            ProcessedDatasetId = p.Id,
            FileId = p.UploadedFileId,
            Name = p.Name,
            RowsBefore = p.RowsBefore,
            RowsAfter = p.RowsAfter,
            CellsModified = p.CellsModified,
            DurationMs = p.DurationMs,
            Columns = JsonDefaults.Deserialize<List<string>>(p.ColumnsJson) ?? new List<string>(),
            Rows = JsonDefaults.DeserializeRows(p.CleanDataJson).Take(PreviewRowCount).ToList(),
            ExecutionLog = JsonDefaults.Deserialize<List<RuleExecutionLogDto>>(p.ExecutionLogJson)
                           ?? new List<RuleExecutionLogDto>()
        };
    }

    // ---------------------------------------------------------------- DIŞA AKTARIM

    public async Task<byte[]?> ExportCsvAsync(int processedId, int userId)
    {
        var p = await _db.ProcessedDatasets.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == processedId && x.UserId == userId);

        if (p is null) return null;

        var columns = JsonDefaults.Deserialize<List<string>>(p.ColumnsJson) ?? new List<string>();
        var rows = JsonDefaults.DeserializeRows(p.CleanDataJson);

        using var buffer = new MemoryStream();
        // Excel'in Türkçe karakterleri doğru açması için UTF-8 BOM şart.
        using (var writer = new StreamWriter(buffer, new UTF8Encoding(true), leaveOpen: true))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            foreach (var column in columns) csv.WriteField(column);
            await csv.NextRecordAsync();

            foreach (var row in rows)
            {
                foreach (var column in columns)
                    csv.WriteField(ValueHelper.AsString(row.TryGetValue(column, out var v) ? v : null));
                await csv.NextRecordAsync();
            }
        }

        await _audit.LogAsync(userId, "-", "EXPORT", $"İşlenmiş veri #{processedId} CSV olarak indirildi");
        return buffer.ToArray();
    }
}
