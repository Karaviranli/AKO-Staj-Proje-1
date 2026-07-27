/** .NET API'sindeki DTO'ların birebir TypeScript karşılıkları. */

export type ApiResponse<T> = {
  success: boolean;
  message?: string | null;
  data?: T;
  errors: string[];
};

export type User = {
  id: number;
  username: string;
  email: string;
  role: string;
  fullName?: string | null;
};

export type AuthResponse = {
  token: string;
  expiresIn: number;
  expiresAt: string;
  user: User;
};

export type DataRow = Record<string, unknown>;

// ---------------------------------------------------------------- Kurallar

export type Condition = {
  column: string;
  operator: string;
  value?: string | null;
  value2?: string | null;
  caseSensitive?: boolean;
};

export type ConditionGroup = {
  logic: "AND" | "OR";
  conditions: Condition[];
  groups: ConditionGroup[];
};

export type RuleAction = {
  type: string;
  targetColumn?: string | null;
  value?: string | null;
  value2?: string | null;
};

export type Rule = {
  order: number;
  name: string;
  enabled: boolean;
  condition?: ConditionGroup | null;
  action: RuleAction;
};

export type RuleExecutionLog = {
  order: number;
  ruleName: string;
  summary: string;
  rowsMatched: number;
  rowsBefore: number;
  rowsAfter: number;
  cellsModified: number;
  durationMs: number;
  skipped: boolean;
  warning?: string | null;
};

export type CatalogOperator = {
  value: string;
  label: string;
  needsValue: boolean;
  needsSecondValue: boolean;
};

export type CatalogAction = {
  value: string;
  label: string;
  scope: "row" | "dataset";
  needsColumn: boolean;
  needsValue: boolean;
  needsSecondValue: boolean;
};

export type RuleCatalog = {
  operators: CatalogOperator[];
  actions: CatalogAction[];
};

export type RulePreset = {
  id: number;
  name: string;
  description?: string | null;
  category: string;
  isSystemPreset: boolean;
  createdAt: string;
  rules: Rule[];
};

// ---------------------------------------------------------------- Veri

export type ColumnProfile = {
  name: string;
  inferredType: "number" | "date" | "boolean" | "text" | "empty";
  nullCount: number;
  nullRatio: number;
  distinctCount: number;
  typeMismatchCount: number;
  sampleValues: string[];
};

export type QualityReport = {
  rowCount: number;
  columnCount: number;
  duplicateRowCount: number;
  totalNullCells: number;
  totalTypeMismatches: number;
  qualityScore: number;
  columns: ColumnProfile[];
  warnings: string[];
};

export type UploadResult = {
  fileId: number;
  fileName: string;
  sourceType: string;
  sizeInBytes: number;
  rowCount: number;
  columnCount: number;
  uploadedAt: string;
  columns: string[];
  preview: DataRow[];
  quality?: QualityReport | null;
};

export type FileSummary = {
  id: number;
  fileName: string;
  sourceType: string;
  rowCount: number;
  columnCount: number;
  sizeInBytes: number;
  uploadedAt: string;
  qualityScore: number;
  processedCount: number;
};

export type PagedData = {
  columns: string[];
  rows: DataRow[];
  page: number;
  pageSize: number;
  totalRows: number;
  totalPages: number;
};

export type ProcessResult = {
  processedDatasetId?: number | null;
  fileId: number;
  name: string;
  dryRun: boolean;
  rowsBefore: number;
  rowsAfter: number;
  cellsModified: number;
  durationMs: number;
  columns: string[];
  rows: DataRow[];
  executionLog: RuleExecutionLog[];
  qualityAfter?: QualityReport | null;
};

export type ProcessedSummary = {
  id: number;
  uploadedFileId: number;
  fileName: string;
  name: string;
  rowsBefore: number;
  rowsAfter: number;
  cellsModified: number;
  ruleCount: number;
  processedAt: string;
};
