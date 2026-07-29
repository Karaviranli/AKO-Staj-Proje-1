# DataFlow — UML Diyagramları

Bu diyagramlar koddaki gerçek sınıf, arayüz ve uç noktalarla birebir uyumludur.
GitHub bu `mermaid` bloklarını otomatik render eder.

---

## 1. Use-Case Diyagramı

```mermaid
flowchart LR
  analyst(("👤 Analist"))
  admin(("👤 Yönetici"))

  subgraph S["DataFlow Sistemi"]
    direction TB
    uc1["Giriş / Kayıt — JWT"]
    uc2["Dosya yükle — CSV / JSON / XLSX"]
    uc3["POST ile veri gönder"]
    uc4["Veri kalite raporunu gör"]
    uc5["Otomatik temizlik önerisi al"]
    uc6["Elle kural zinciri kur"]
    uc7["Kuralları çalıştır — önizle / kaydet"]
    uc8["İşlem geçmişini gör"]
    uc9["Temiz veriyi CSV indir"]
    uc10["Kural şablonu kaydet / uygula"]
    uc11["Denetim kayıtlarını izle"]
  end

  analyst --- uc1
  analyst --- uc2
  analyst --- uc3
  analyst --- uc4
  analyst --- uc5
  analyst --- uc6
  analyst --- uc7
  analyst --- uc8
  analyst --- uc9
  analyst --- uc10
  admin --- uc1
  admin --- uc11
```

---

## 2. Katmanlı Mimari / Bileşen Diyagramı

```mermaid
flowchart TB
  subgraph P["Sunum Katmanı · Next.js 16 (React)"]
    pg["Sayfalar<br/>/giris · /panel · /veri-setleri · /yukle · /gecmis"]
    cmp["Bileşenler<br/>RuleBuilder · SuggestionsPanel · DataTable · QualityPanel"]
  end

  subgraph BFF["BFF Proxy · Next Route Handlers"]
    proxy["/api/backend/[...path]<br/>HttpOnly cookie → Bearer token"]
  end

  subgraph API["Uygulama Katmanı · .NET 9 Web API"]
    mw["JWT Bearer Middleware"]
    ctl["Controllers<br/>Auth · Data · Rules"]
  end

  subgraph BLL["İş Katmanı · Business"]
    svc["Servisler<br/>AuthService · DataService · AuditService"]
    fac["FileParserFactory<br/>(Factory Pattern)"]
    eng["RuleEngine<br/>(Strategy / Pipeline)"]
    sug["RuleSuggester · DataQualityAnalyzer"]
  end

  subgraph DAL["Veri Katmanı · DataAccess"]
    ctx["AppDbContext · EF Core 9 (Code-First)"]
  end

  db[("SQLite<br/>dataflow.db")]

  pg --> proxy
  cmp --> proxy
  proxy --> mw
  mw --> ctl
  ctl --> svc
  svc --> fac
  svc --> eng
  svc --> sug
  svc --> ctx
  ctx --> db
```

---

## 3. Varlık-İlişki (ER) Diyagramı — Veritabanı

```mermaid
erDiagram
  USER ||--o{ UPLOADED_FILE : yukler
  UPLOADED_FILE ||--o{ PROCESSED_DATASET : uretir
  USER ||--o{ PROCESSED_DATASET : sahiptir
  USER ||--o{ RULE_PRESET : olusturur
  USER ||--o{ AUDIT_LOG : kaydeder

  USER {
    int Id PK
    string Username
    string Email
    string PasswordHash
    string Role
    bool IsActive
    datetime CreatedAt
    datetime LastLoginAt
  }
  UPLOADED_FILE {
    int Id PK
    int UserId FK
    string FileName
    string SourceType
    string RawDataJson
    string ColumnsJson
    string QualityReportJson
    int RowCount
    int ColumnCount
    datetime UploadedAt
  }
  PROCESSED_DATASET {
    int Id PK
    int UploadedFileId FK
    int UserId FK
    string Name
    string AppliedRulesJson
    string ExecutionLogJson
    string CleanDataJson
    int RowsBefore
    int RowsAfter
    int CellsModified
    datetime ProcessedAt
  }
  RULE_PRESET {
    int Id PK
    int UserId FK
    string Name
    string Category
    string RulesJson
    bool IsSystemPreset
  }
  AUDIT_LOG {
    int Id PK
    int UserId FK
    string Action
    string Detail
    string IpAddress
    datetime CreatedAt
  }
```

---

## 4. Sınıf Diyagramı — Servisler ve Tasarım Kalıpları

```mermaid
classDiagram
  direction LR

  class IFileParser {
    <<interface>>
    +string Extension
    +Parse(Stream, string) DatasetModel
  }
  class IFileParserFactory {
    <<interface>>
    +GetParser(string) IFileParser
    +SupportedExtensions IReadOnlyCollection
  }
  class CsvFileParser
  class ExcelFileParser
  class JsonFileParser
  class FileParserFactory

  IFileParser <|.. CsvFileParser
  IFileParser <|.. ExcelFileParser
  IFileParser <|.. JsonFileParser
  IFileParserFactory <|.. FileParserFactory
  FileParserFactory o--> IFileParser : seçer

  class IRuleEngine {
    <<interface>>
    +Execute(DatasetModel, IEnumerable) RuleEngineResult
  }
  class RuleEngine
  class ConditionEvaluator {
    <<static>>
    +Matches(row, ConditionGroupDto) bool
  }
  class ActionExecutor {
    <<static>>
    +ApplyToRow(row, RuleActionDto, DatasetModel) bool
  }
  class RuleSuggester {
    <<static>>
    +Suggest(DatasetModel) List~RuleSuggestionDto~
  }

  IRuleEngine <|.. RuleEngine
  RuleEngine ..> ConditionEvaluator : kullanır
  RuleEngine ..> ActionExecutor : kullanır

  class IDataService {
    <<interface>>
    +UploadFileAsync() UploadResultDto
    +SuggestRulesAsync() List~RuleSuggestionDto~
    +ProcessAsync() ProcessResultDto
    +ExportCsvAsync() byte[]
  }
  class DataService
  IDataService <|.. DataService
  DataService ..> IFileParserFactory : kullanır
  DataService ..> IRuleEngine : kullanır
  DataService ..> RuleSuggester : kullanır
```

---

## 5. Sınıf Diyagramı — Kural Modeli (DTO Kompozisyonu)

Kural motorunun kalbi: bir kural, iç içe koşul grupları ve tek bir aksiyondan oluşur.

```mermaid
classDiagram
  direction TB

  class RuleDto {
    +int Order
    +string Name
    +bool Enabled
    +ConditionGroupDto Condition
    +RuleActionDto Action
  }
  class ConditionGroupDto {
    +string Logic  AND_OR
    +List~ConditionDto~ Conditions
    +List~ConditionGroupDto~ Groups
  }
  class ConditionDto {
    +string Column
    +string Operator
    +string Value
    +string Value2
    +bool CaseSensitive
  }
  class RuleActionDto {
    +string Type
    +string TargetColumn
    +string Value
    +string Value2
  }
  class RuleSuggestionDto {
    +RuleDto Rule
    +string Reason
    +string Category
    +int Impact
  }

  RuleDto "1" *-- "0..1" ConditionGroupDto : koşul
  RuleDto "1" *-- "1" RuleActionDto : aksiyon
  ConditionGroupDto "1" *-- "*" ConditionDto : koşullar
  ConditionGroupDto "1" *-- "*" ConditionGroupDto : iç içe grup
  RuleSuggestionDto "1" *-- "1" RuleDto : sarmalar
```

> **Operatörler (20):** eq, neq, gt, gte, lt, lte, between, contains, notContains,
> startsWith, endsWith, isNull, isNotNull, isEmpty, isNotEmpty, in, notIn, regex,
> isNumeric, isNotNumeric
>
> **Aksiyonlar (25):** deleteRow, keepRow, flagRow, setValue, fillNull, trim,
> toUpper, toLower, toTitleCase, replace, removeSpaces, onlyDigits, multiply,
> divide, add, subtract, round, abs, castNumber, castDate, castText,
> renameColumn, dropColumn, copyColumn, deduplicate

---

## 6. Sekans Diyagramı — Yükleme + Otomatik Temizleme Akışı

```mermaid
sequenceDiagram
  actor U as Analist
  participant FE as Next.js UI
  participant PX as BFF Proxy
  participant API as DataController
  participant SVC as DataService
  participant F as FileParserFactory
  participant Q as QualityAnalyzer
  participant E as RuleEngine
  participant DB as SQLite

  Note over U,DB: 1) Dosya yükleme
  U->>FE: Dosya seç (CSV/XLSX/JSON)
  FE->>PX: POST /api/backend/data/upload + cookie
  PX->>API: POST /api/data/upload (Bearer JWT)
  API->>SVC: UploadFileAsync(stream)
  SVC->>F: GetParser(".csv")
  F-->>SVC: IFileParser
  SVC->>Q: Analyze(dataset)
  Q-->>SVC: QualityReport
  SVC->>DB: UploadedFile kaydet
  API-->>FE: rowCount, columns, quality

  Note over U,DB: 2) Otomatik öneri
  U->>FE: "Otomatik öneriler"
  FE->>PX: GET .../files/{id}/suggestions
  PX->>API: GET files/{id}/suggestions
  API->>SVC: SuggestRulesAsync(id)
  SVC-->>API: List~RuleSuggestionDto~
  API-->>FE: öneriler

  Note over U,DB: 3) Kuralları uygula ve kaydet
  U->>FE: "Uygula ve kaydet"
  FE->>PX: POST .../process {rules}
  PX->>API: POST /api/data/process
  API->>SVC: ProcessAsync(rules)
  SVC->>E: Execute(dataset, rules)
  loop her kural (sırayla)
    E->>E: ConditionEvaluator.Matches(row)
    E->>E: ActionExecutor.ApplyToRow(row)
  end
  E-->>SVC: RuleEngineResult + logs
  SVC->>DB: ProcessedDataset kaydet
  API-->>FE: temiz veri + yürütme raporu
```
