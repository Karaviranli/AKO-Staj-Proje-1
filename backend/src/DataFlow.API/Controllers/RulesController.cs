using DataFlow.Business.Common;
using DataFlow.Business.Dtos.Common;
using DataFlow.Business.Dtos.Rules;
using DataFlow.DataAccess.Context;
using DataFlow.DataAccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataFlow.API.Controllers;

[Authorize]
public class RulesController : BaseApiController
{
    private readonly AppDbContext _db;

    public RulesController(AppDbContext db) => _db = db;

    /// <summary>
    /// Arayüzün kural sihirbazını dinamik olarak kurabilmesi için
    /// desteklenen tüm operatör ve aksiyonların kataloğu.
    /// </summary>
    [HttpGet("catalog")]
    [AllowAnonymous]
    public ActionResult<ApiResponse<object>> Catalog()
    {
        var catalog = new
        {
            operators = ConditionOperators.Labels.Select(x => new
            {
                value = x.Key,
                label = x.Value,
                needsValue = !NoValueOperators.Contains(x.Key),
                needsSecondValue = x.Key == ConditionOperators.Between
            }),
            actions = ActionTypes.Labels.Select(x => new
            {
                value = x.Key,
                label = x.Value,
                scope = ActionTypes.DatasetLevel.Contains(x.Key) ? "dataset" : "row",
                needsColumn = x.Key is not (ActionTypes.DeleteRow or ActionTypes.KeepRow),
                needsValue = ValueActions.Contains(x.Key),
                needsSecondValue = x.Key is ActionTypes.Replace or ActionTypes.RenameColumn or ActionTypes.CopyColumn
            })
        };

        return Ok(ApiResponse<object>.Ok(catalog));
    }

    /// <summary>Hazır ve kullanıcıya ait kayıtlı kural setleri.</summary>
    [HttpGet("presets")]
    public async Task<ActionResult<ApiResponse<object>>> Presets([FromQuery] string? category)
    {
        var query = _db.RulePresets.AsNoTracking()
            .Where(p => p.IsSystemPreset || p.UserId == CurrentUserId);

        if (!string.IsNullOrWhiteSpace(category) && category != "genel")
            query = query.Where(p => p.Category == category);

        var presets = await query
            .OrderByDescending(p => p.IsSystemPreset)
            .ThenByDescending(p => p.CreatedAt)
            .ToListAsync();

        var result = presets.Select(p => new
        {
            p.Id,
            p.Name,
            p.Description,
            p.Category,
            p.IsSystemPreset,
            p.CreatedAt,
            Rules = JsonDefaults.Deserialize<List<RuleDto>>(p.RulesJson) ?? new List<RuleDto>()
        });

        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("presets")]
    public async Task<ActionResult<ApiResponse<int>>> CreatePreset(CreatePresetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(ApiResponse<int>.Fail("Şablon adı zorunludur."));

        var preset = new RulePreset
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "genel" : request.Category,
            RulesJson = JsonDefaults.Serialize(request.Rules),
            UserId = CurrentUserId,
            IsSystemPreset = false
        };

        _db.RulePresets.Add(preset);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<int>.Ok(preset.Id, "Kural şablonu kaydedildi."));
    }

    [HttpDelete("presets/{id:int}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeletePreset(int id)
    {
        // Sistem şablonları silinemez; yalnızca kullanıcının kendi kayıtları.
        var preset = await _db.RulePresets
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == CurrentUserId && !p.IsSystemPreset);

        if (preset is null) return NotFound(ApiResponse<bool>.Fail("Şablon bulunamadı."));

        _db.RulePresets.Remove(preset);
        await _db.SaveChangesAsync();
        return Ok(ApiResponse<bool>.Ok(true, "Şablon silindi."));
    }

    private static readonly HashSet<string> NoValueOperators = new()
    {
        ConditionOperators.IsNull, ConditionOperators.IsNotNull,
        ConditionOperators.IsEmpty, ConditionOperators.IsNotEmpty,
        ConditionOperators.IsNumeric, ConditionOperators.IsNotNumeric
    };

    private static readonly HashSet<string> ValueActions = new()
    {
        ActionTypes.SetValue, ActionTypes.FillNull, ActionTypes.Replace,
        ActionTypes.Multiply, ActionTypes.Divide, ActionTypes.Add, ActionTypes.Subtract,
        ActionTypes.Round, ActionTypes.CastDate, ActionTypes.FlagRow,
        ActionTypes.RenameColumn, ActionTypes.CopyColumn
    };

    public class CreatePresetRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = "genel";
        public List<RuleDto> Rules { get; set; } = new();
    }
}
