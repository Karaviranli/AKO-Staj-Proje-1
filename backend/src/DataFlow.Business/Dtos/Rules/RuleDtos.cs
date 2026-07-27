namespace DataFlow.Business.Dtos.Rules;

/// <summary>
/// Tek bir kural. Kurallar "Order" alanına göre SIRAYLA çalışır; bir kuralın çıktısı
/// bir sonraki kuralın girdisidir (Pipeline / Chain of Responsibility).
///
/// Beyaz tahta örneği:
///   Kural 1 -> Koşul: Yas > 50            Aksiyon: Segment = "X"
///   Kural 2 -> Koşul: Yas &lt; 5          Aksiyon: Segment = "Y"
///   Kural 3 -> Koşul: DogumYeri IS NULL   Aksiyon: DogumYeri = "Belirsiz"
/// </summary>
public class RuleDto
{
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;

    /// <summary>Boş bırakılırsa kural TÜM satırlara uygulanır (ör. genel trim/temizlik).</summary>
    public ConditionGroupDto? Condition { get; set; }

    public RuleActionDto Action { get; set; } = new();
}

/// <summary>
/// Koşul grubu. İç içe gruplar ve AND/OR mantığı desteklenir:
///   (Yas > 50 AND Sehir = "Ankara") OR (Maas &gt; 50000)
/// </summary>
public class ConditionGroupDto
{
    /// <summary>AND | OR</summary>
    public string Logic { get; set; } = "AND";

    public List<ConditionDto> Conditions { get; set; } = new();

    /// <summary>İç içe (nested) alt gruplar.</summary>
    public List<ConditionGroupDto> Groups { get; set; } = new();
}

public class ConditionDto
{
    public string Column { get; set; } = string.Empty;

    /// <summary><see cref="ConditionOperators"/> içindeki değerlerden biri.</summary>
    public string Operator { get; set; } = ConditionOperators.Equal;

    public string? Value { get; set; }

    /// <summary>Yalnızca "between" operatörü için üst sınır.</summary>
    public string? Value2 { get; set; }

    /// <summary>Metin karşılaştırmalarında büyük/küçük harf duyarlılığı.</summary>
    public bool CaseSensitive { get; set; }
}

public class RuleActionDto
{
    /// <summary><see cref="ActionTypes"/> içindeki değerlerden biri.</summary>
    public string Type { get; set; } = ActionTypes.SetValue;

    /// <summary>Aksiyonun uygulanacağı kolon. Yoksa yeni kolon olarak oluşturulur.</summary>
    public string? TargetColumn { get; set; }

    public string? Value { get; set; }

    /// <summary>Replace aksiyonunda "yeni değer", rename aksiyonunda "yeni kolon adı".</summary>
    public string? Value2 { get; set; }
}

public static class ConditionOperators
{
    // "Equals" adı object.Equals'i gizlediği için (CS0108) tekil biçim kullanıldı.
    // Dışarıya giden değerler ("eq"/"neq") değişmedi.
    public const string Equal = "eq";
    public const string NotEqual = "neq";
    public const string GreaterThan = "gt";
    public const string GreaterOrEqual = "gte";
    public const string LessThan = "lt";
    public const string LessOrEqual = "lte";
    public const string Between = "between";
    public const string Contains = "contains";
    public const string NotContains = "notContains";
    public const string StartsWith = "startsWith";
    public const string EndsWith = "endsWith";
    public const string IsNull = "isNull";
    public const string IsNotNull = "isNotNull";
    public const string IsEmpty = "isEmpty";
    public const string IsNotEmpty = "isNotEmpty";
    public const string In = "in";
    public const string NotIn = "notIn";
    public const string Regex = "regex";
    public const string IsNumeric = "isNumeric";
    public const string IsNotNumeric = "isNotNumeric";

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [Equal] = "eşittir",
        [NotEqual] = "eşit değildir",
        [GreaterThan] = "büyüktür",
        [GreaterOrEqual] = "büyük veya eşittir",
        [LessThan] = "küçüktür",
        [LessOrEqual] = "küçük veya eşittir",
        [Between] = "arasındadır",
        [Contains] = "içerir",
        [NotContains] = "içermez",
        [StartsWith] = "ile başlar",
        [EndsWith] = "ile biter",
        [IsNull] = "boştur (null)",
        [IsNotNull] = "dolu",
        [IsEmpty] = "boş metin",
        [IsNotEmpty] = "boş metin değil",
        [In] = "listede var",
        [NotIn] = "listede yok",
        [Regex] = "desene uyar",
        [IsNumeric] = "sayısaldır",
        [IsNotNumeric] = "sayısal değildir",
    };
}

public static class ActionTypes
{
    // --- Satır seviyesi ---
    public const string DeleteRow = "deleteRow";
    public const string KeepRow = "keepRow";
    public const string FlagRow = "flagRow";

    // --- Hücre seviyesi ---
    public const string SetValue = "setValue";
    public const string FillNull = "fillNull";
    public const string Trim = "trim";
    public const string ToUpper = "toUpper";
    public const string ToLower = "toLower";
    public const string ToTitleCase = "toTitleCase";
    public const string Replace = "replace";
    public const string RemoveSpaces = "removeSpaces";
    public const string OnlyDigits = "onlyDigits";

    // --- Sayısal ---
    public const string Multiply = "multiply";
    public const string Divide = "divide";
    public const string Add = "add";
    public const string Subtract = "subtract";
    public const string Round = "round";
    public const string Abs = "abs";

    // --- Tip dönüşümü ---
    public const string CastNumber = "castNumber";
    public const string CastDate = "castDate";
    public const string CastText = "castText";

    // --- Kolon seviyesi (satır bağımsız) ---
    public const string RenameColumn = "renameColumn";
    public const string DropColumn = "dropColumn";
    public const string CopyColumn = "copyColumn";
    public const string Deduplicate = "deduplicate";

    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [DeleteRow] = "Satırı sil",
        [KeepRow] = "Sadece bu satırları tut",
        [FlagRow] = "Satırı işaretle",
        [SetValue] = "Değer ata",
        [FillNull] = "Boşsa doldur",
        [Trim] = "Baş/son boşlukları kırp",
        [ToUpper] = "BÜYÜK HARF",
        [ToLower] = "küçük harf",
        [ToTitleCase] = "İlk Harfler Büyük",
        [Replace] = "Metni değiştir",
        [RemoveSpaces] = "Tüm boşlukları sil",
        [OnlyDigits] = "Sadece rakamları bırak",
        [Multiply] = "Çarp",
        [Divide] = "Böl",
        [Add] = "Ekle",
        [Subtract] = "Çıkar",
        [Round] = "Yuvarla",
        [Abs] = "Mutlak değer",
        [CastNumber] = "Sayıya çevir",
        [CastDate] = "Tarihe çevir",
        [CastText] = "Metne çevir",
        [RenameColumn] = "Kolonu yeniden adlandır",
        [DropColumn] = "Kolonu sil",
        [CopyColumn] = "Kolonu kopyala",
        [Deduplicate] = "Tekrar eden satırları sil",
    };

    /// <summary>Satır döngüsü yerine tüm veri seti üzerinde bir kez çalışan aksiyonlar.</summary>
    public static readonly HashSet<string> DatasetLevel = new()
    {
        RenameColumn, DropColumn, CopyColumn, Deduplicate
    };
}

/// <summary>Bir kuralın çalışması sonucu üretilen rapor satırı — sunumun en güçlü çıktısı.</summary>
public class RuleExecutionLogDto
{
    public int Order { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int RowsMatched { get; set; }
    public int RowsBefore { get; set; }
    public int RowsAfter { get; set; }
    public int CellsModified { get; set; }
    public int DurationMs { get; set; }
    public bool Skipped { get; set; }
    public string? Warning { get; set; }
}

/// <summary>
/// Sistemin veri kalitesi analizine bakarak ürettiği "akıllı temizlik" önerisi.
/// Kullanıcı bunları toptan ya da tek tek kural zincirine ekleyebilir.
/// </summary>
public class RuleSuggestionDto
{
    public RuleDto Rule { get; set; } = new();

    /// <summary>Bu kuralın neden önerildiğinin insan-okur açıklaması.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>temizlik | tip | eksik | tekrar | kolon</summary>
    public string Category { get; set; } = "temizlik";

    /// <summary>Kaç hücre/satırın etkileneceğine dair tahmin (öncelik sıralaması için).</summary>
    public int Impact { get; set; }
}
