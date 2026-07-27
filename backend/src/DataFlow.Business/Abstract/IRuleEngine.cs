using DataFlow.Business.Common;
using DataFlow.Business.Dtos.Rules;

namespace DataFlow.Business.Abstract;

public interface IRuleEngine
{
    /// <summary>Kural setini sırayla veri seti üzerinde çalıştırır.</summary>
    RuleEngineResult Execute(DatasetModel dataset, IEnumerable<RuleDto> rules);
}

public class RuleEngineResult
{
    public DatasetModel Dataset { get; set; } = new();
    public List<RuleExecutionLogDto> Logs { get; set; } = new();
    public int RowsBefore { get; set; }
    public int RowsAfter { get; set; }
    public int CellsModified { get; set; }
    public int DurationMs { get; set; }
}
