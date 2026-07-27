using DataFlow.Business.Abstract;

namespace DataFlow.Business.Factories;

/// <summary>
/// Factory Pattern: yüklenen dosyanın uzantısına bakarak doğru okuyucuyu seçer.
/// Okuyucular DI konteynerinden gelir, böylece test edilebilirlik korunur.
/// </summary>
public class FileParserFactory : IFileParserFactory
{
    private readonly Dictionary<string, IFileParser> _parsers;

    public FileParserFactory(IEnumerable<IFileParser> parsers)
    {
        _parsers = parsers.ToDictionary(p => p.Extension, StringComparer.OrdinalIgnoreCase);

        // .xls, .xlsx okuyucusuna yönlendirilir.
        if (_parsers.TryGetValue(".xlsx", out var excel))
            _parsers.TryAdd(".xls", excel);
    }

    public IReadOnlyCollection<string> SupportedExtensions => _parsers.Keys.ToList();

    public IFileParser GetParser(string fileNameOrExtension)
    {
        var extension = fileNameOrExtension.StartsWith('.')
            ? fileNameOrExtension
            : Path.GetExtension(fileNameOrExtension);

        if (string.IsNullOrWhiteSpace(extension))
            throw new NotSupportedException("Dosya uzantısı okunamadı.");

        if (_parsers.TryGetValue(extension, out var parser))
            return parser;

        throw new NotSupportedException(
            $"'{extension}' formatı desteklenmiyor. Desteklenenler: {string.Join(", ", _parsers.Keys)}");
    }
}
