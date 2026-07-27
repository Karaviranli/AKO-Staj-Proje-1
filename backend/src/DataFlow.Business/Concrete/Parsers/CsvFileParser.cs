using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using DataFlow.Business.Abstract;
using DataFlow.Business.Common;

namespace DataFlow.Business.Concrete.Parsers;

/// <summary>
/// CSV okuyucu. Ayırıcıyı (virgül / noktalı virgül / tab) otomatik tespit eder ve
/// eksik/fazla kolonlu bozuk satırları hata fırlatmadan tolere eder.
/// </summary>
public class CsvFileParser : IFileParser
{
    public string Extension => ".csv";

    public DatasetModel Parse(Stream stream, string fileName)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var content = reader.ReadToEnd();

        if (string.IsNullOrWhiteSpace(content))
            return new DatasetModel();

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = DetectDelimiter(content),
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,   // eksik kolonlu satırlarda patlama
            BadDataFound = null,        // kaçış karakteri bozuk satırlarda patlama
            HeaderValidated = null,
            IgnoreBlankLines = true,
            DetectColumnCountChanges = false
        };

        using var stringReader = new StringReader(content);
        using var csv = new CsvReader(stringReader, config);

        var rows = new List<Dictionary<string, object?>>();

        if (!csv.Read() || !csv.ReadHeader())
            return new DatasetModel();

        var headers = NormalizeHeaders(csv.HeaderRecord ?? Array.Empty<string>());

        while (csv.Read())
        {
            var row = new Dictionary<string, object?>();
            for (var i = 0; i < headers.Count; i++)
            {
                csv.TryGetField<string>(i, out var value);
                row[headers[i]] = ValueHelper.Normalize(value);
            }

            // Tamamen boş satırları atla.
            if (row.Values.All(ValueHelper.IsNullish)) continue;
            rows.Add(row);
        }

        return new DatasetModel { Columns = headers, Rows = rows };
    }

    /// <summary>İlk satırdaki aday ayırıcılardan en çok geçeni seçer.</summary>
    private static string DetectDelimiter(string content)
    {
        var firstLine = content.Split('\n').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? "";
        var candidates = new[] { ";", ",", "\t", "|" };

        return candidates
            .OrderByDescending(d => firstLine.Split(d).Length)
            .First();
    }

    /// <summary>Boş veya tekrar eden başlıkları benzersiz hale getirir.</summary>
    internal static List<string> NormalizeHeaders(IEnumerable<string?> raw)
    {
        var result = new List<string>();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var index = 0;

        foreach (var header in raw)
        {
            index++;
            var name = string.IsNullOrWhiteSpace(header) ? $"Kolon{index}" : header.Trim();

            var candidate = name;
            var suffix = 2;
            while (!used.Add(candidate))
                candidate = $"{name}_{suffix++}";

            result.Add(candidate);
        }

        return result;
    }
}
