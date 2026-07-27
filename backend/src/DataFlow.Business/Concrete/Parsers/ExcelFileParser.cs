using System.Text;
using DataFlow.Business.Abstract;
using DataFlow.Business.Common;
using ExcelDataReader;

namespace DataFlow.Business.Concrete.Parsers;

/// <summary>
/// XLSX/XLS okuyucu. Stream üzerinden çalışır, dosyanın tamamını belleğe
/// nesne modeli olarak açmaz. İlk sayfa (worksheet) okunur.
/// </summary>
public class ExcelFileParser : IFileParser
{
    static ExcelFileParser()
    {
        // Eski .xls dosyalarındaki Windows-1254 gibi kod sayfaları için gerekli.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public string Extension => ".xlsx";

    public DatasetModel Parse(Stream stream, string fileName)
    {
        using var reader = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration
        {
            FallbackEncoding = Encoding.GetEncoding(1254)
        });

        var rows = new List<Dictionary<string, object?>>();
        List<string>? headers = null;

        do
        {
            while (reader.Read())
            {
                if (headers is null)
                {
                    var raw = new List<string?>();
                    for (var i = 0; i < reader.FieldCount; i++)
                        raw.Add(reader.GetValue(i)?.ToString());

                    // Başlık satırı tamamen boşsa bir sonraki satıra bak.
                    if (raw.All(string.IsNullOrWhiteSpace)) continue;

                    headers = CsvFileParser.NormalizeHeaders(raw);
                    continue;
                }

                var row = new Dictionary<string, object?>();
                for (var i = 0; i < headers.Count && i < reader.FieldCount; i++)
                    row[headers[i]] = ValueHelper.Normalize(reader.GetValue(i));

                if (row.Values.All(ValueHelper.IsNullish)) continue;
                rows.Add(row);
            }

            break; // yalnızca ilk sayfa
        } while (reader.NextResult());

        return new DatasetModel
        {
            Columns = headers ?? new List<string>(),
            Rows = rows
        };
    }
}
