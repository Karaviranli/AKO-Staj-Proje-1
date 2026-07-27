using DataFlow.Business.Common;

namespace DataFlow.Business.Abstract;

/// <summary>
/// Her dosya formatı için tek bir uygulama. Yeni bir format (XML, TXT, Parquet)
/// eklendiğinde mevcut koda dokunulmaz; yalnızca yeni bir sınıf yazılıp
/// fabrikaya kaydedilir (Open/Closed Principle).
/// </summary>
public interface IFileParser
{
    /// <summary>Desteklenen uzantı: ".csv", ".xlsx", ".json"</summary>
    string Extension { get; }

    DatasetModel Parse(Stream stream, string fileName);
}

public interface IFileParserFactory
{
    IFileParser GetParser(string fileNameOrExtension);
    IReadOnlyCollection<string> SupportedExtensions { get; }
}
