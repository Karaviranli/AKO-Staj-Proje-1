using DataFlow.Business.Abstract;
using DataFlow.Business.Dtos.Common;
using DataFlow.Business.Dtos.Data;
using DataFlow.Business.Dtos.Rules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DataFlow.API.Controllers;

[Authorize]
public class DataController : BaseApiController
{
    private readonly IDataService _data;
    private readonly IConfiguration _config;

    public DataController(IDataService data, IConfiguration config)
    {
        _data = data;
        _config = config;
    }

    // ---------------------------------------------------------------- YÜKLEME

    /// <summary>CSV, XLSX veya JSON dosyası yükler; ham veriyi ve kalite raporunu döner.</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<UploadResultDto>>> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<UploadResultDto>.Fail("Dosya seçilmedi."));

        var allowed = _config.GetSection("Upload:AllowedExtensions").Get<string[]>()
                      ?? new[] { ".csv", ".xlsx", ".xls", ".json" };

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(extension))
            return BadRequest(ApiResponse<UploadResultDto>.Fail(
                $"'{extension}' desteklenmiyor. İzin verilenler: {string.Join(", ", allowed)}"));

        var maxMb = _config.GetValue("Upload:MaxFileSizeMb", 25);
        if (file.Length > (long)maxMb * 1024 * 1024)
            return BadRequest(ApiResponse<UploadResultDto>.Fail($"Dosya {maxMb} MB sınırını aşıyor."));

        await using var stream = file.OpenReadStream();
        var result = await _data.UploadFileAsync(stream, file.FileName, file.Length, CurrentUserId);

        return Ok(ApiResponse<UploadResultDto>.Ok(result,
            $"{result.RowCount} satır, {result.ColumnCount} kolon okundu."));
    }

    /// <summary>Dosya olmadan, doğrudan JSON gövdesiyle veri gönderimi.</summary>
    [HttpPost("push")]
    public async Task<ActionResult<ApiResponse<UploadResultDto>>> Push(PostDataRequestDto request)
    {
        var result = await _data.UploadJsonAsync(request, CurrentUserId);
        return Ok(ApiResponse<UploadResultDto>.Ok(result,
            $"{result.RowCount} satır alındı."));
    }

    // ---------------------------------------------------------------- OKUMA

    /// <summary>Kullanıcının yüklediği tüm veri setlerinin özeti.</summary>
    [HttpGet("files")]
    public async Task<ActionResult<ApiResponse<List<FileSummaryDto>>>> Files()
        => Ok(ApiResponse<List<FileSummaryDto>>.Ok(await _data.GetFilesAsync(CurrentUserId)));

    /// <summary>Bir veri setinin ham satırlarını sayfalı olarak döner.</summary>
    [HttpGet("files/{id:int}")]
    public async Task<ActionResult<ApiResponse<PagedDataDto>>> FileData(
        int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await _data.GetFileDataAsync(id, CurrentUserId, page, pageSize);
        return result is null
            ? NotFound(ApiResponse<PagedDataDto>.Fail("Veri seti bulunamadı."))
            : Ok(ApiResponse<PagedDataDto>.Ok(result));
    }

    [HttpDelete("files/{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteFile(int id)
    {
        var deleted = await _data.DeleteFileAsync(id, CurrentUserId);
        return deleted
            ? Ok(ApiResponse<bool>.Ok(true, "Veri seti silindi."))
            : NotFound(ApiResponse<bool>.Fail("Veri seti bulunamadı."));
    }

    /// <summary>
    /// Veri setini inceleyip otomatik temizlik kuralları önerir. Kullanıcı bunları
    /// toptan uygulayabilir ya da tek tek kural zincirine ekleyip düzenleyebilir.
    /// </summary>
    [HttpGet("files/{id:int}/suggestions")]
    public async Task<ActionResult<ApiResponse<List<RuleSuggestionDto>>>> Suggestions(int id)
    {
        var result = await _data.SuggestRulesAsync(id, CurrentUserId);
        return result is null
            ? NotFound(ApiResponse<List<RuleSuggestionDto>>.Fail("Veri seti bulunamadı."))
            : Ok(ApiResponse<List<RuleSuggestionDto>>.Ok(result,
                result.Count == 0
                    ? "Veri temiz görünüyor — önerilecek bir düzeltme bulunamadı."
                    : $"{result.Count} temizlik önerisi bulundu."));
    }

    // ---------------------------------------------------------------- İŞLEME

    /// <summary>
    /// Kural setini veri setine SIRAYLA uygular. DryRun=true ise sonuç kaydedilmez,
    /// yalnızca önizleme döner — arayüzdeki canlı önizleme bunu kullanır.
    /// </summary>
    [HttpPost("process")]
    public async Task<ActionResult<ApiResponse<ProcessResultDto>>> Process(ProcessRequestDto request)
    {
        var result = await _data.ProcessAsync(request, CurrentUserId);
        if (result is null)
            return NotFound(ApiResponse<ProcessResultDto>.Fail("Veri seti bulunamadı."));

        return Ok(ApiResponse<ProcessResultDto>.Ok(result,
            $"{result.RowsBefore} satır işlendi → {result.RowsAfter} satır kaldı."));
    }

    [HttpGet("processed")]
    public async Task<ActionResult<ApiResponse<List<ProcessedSummaryDto>>>> Processed()
        => Ok(ApiResponse<List<ProcessedSummaryDto>>.Ok(await _data.GetProcessedAsync(CurrentUserId)));

    [HttpGet("processed/{id:int}")]
    public async Task<ActionResult<ApiResponse<ProcessResultDto>>> ProcessedDetail(int id)
    {
        var result = await _data.GetProcessedDetailAsync(id, CurrentUserId);
        return result is null
            ? NotFound(ApiResponse<ProcessResultDto>.Fail("Kayıt bulunamadı."))
            : Ok(ApiResponse<ProcessResultDto>.Ok(result));
    }

    /// <summary>İşlenmiş veriyi CSV olarak indirir (Excel uyumlu, UTF-8 BOM'lu).</summary>
    [HttpGet("processed/{id:int}/export")]
    public async Task<IActionResult> Export(int id)
    {
        var bytes = await _data.ExportCsvAsync(id, CurrentUserId);
        if (bytes is null) return NotFound(ApiResponse<object>.Fail("Kayıt bulunamadı."));

        return File(bytes, "text/csv; charset=utf-8", $"dataflow-{id}.csv");
    }
}
