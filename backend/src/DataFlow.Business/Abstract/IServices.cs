using DataFlow.Business.Dtos.Auth;
using DataFlow.Business.Dtos.Data;
using DataFlow.Business.Dtos.Rules;

namespace DataFlow.Business.Abstract;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string? ip = null);
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, string? ip = null);
    Task<UserDto?> GetProfileAsync(int userId);
}

public interface IDataService
{
    Task<UploadResultDto> UploadFileAsync(Stream stream, string fileName, long size, int userId);
    Task<UploadResultDto> UploadJsonAsync(PostDataRequestDto request, int userId);

    Task<List<FileSummaryDto>> GetFilesAsync(int userId);
    Task<PagedDataDto?> GetFileDataAsync(int fileId, int userId, int page, int pageSize);
    Task<bool> DeleteFileAsync(int fileId, int userId);

    /// <summary>Veri setini inceleyip otomatik temizlik kuralları önerir.</summary>
    Task<List<RuleSuggestionDto>?> SuggestRulesAsync(int fileId, int userId);

    Task<ProcessResultDto?> ProcessAsync(ProcessRequestDto request, int userId);
    Task<List<ProcessedSummaryDto>> GetProcessedAsync(int userId);
    Task<ProcessResultDto?> GetProcessedDetailAsync(int id, int userId);
    Task<byte[]?> ExportCsvAsync(int processedId, int userId);
}

/// <summary>İşlem izlerini (kim, ne zaman, ne yaptı) kaydeder.</summary>
public interface IAuditService
{
    Task LogAsync(int? userId, string username, string action, string? detail = null, string? ip = null);
}
