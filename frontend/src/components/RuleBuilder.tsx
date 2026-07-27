"use client";

import type {
  CatalogAction,
  CatalogOperator,
  Condition,
  Rule,
  RuleCatalog,
} from "@/lib/types";
import { Badge, Button, Input, Select, cx } from "./ui";

/**
 * Kural sihirbazı. Kullanıcı kod yazmadan "koşul → aksiyon" kuralları kurar.
 * Kuralların SIRASI önemlidir: her kural bir öncekinin çıktısı üzerinde çalışır.
 */

export function emptyRule(order: number): Rule {
  return {
    order,
    name: "",
    enabled: true,
    condition: { logic: "AND", conditions: [], groups: [] },
    action: { type: "setValue", targetColumn: "", value: "" },
  };
}

export function emptyCondition(column: string): Condition {
  return { column, operator: "eq", value: "", caseSensitive: false };
}

export function RuleBuilder({
  rules,
  columns,
  catalog,
  onChange,
}: {
  rules: Rule[];
  columns: string[];
  catalog: RuleCatalog;
  onChange: (rules: Rule[]) => void;
}) {
  function update(index: number, patch: Partial<Rule>) {
    onChange(rules.map((r, i) => (i === index ? { ...r, ...patch } : r)));
  }

  function remove(index: number) {
    // Silme sonrası sıra numaraları yeniden hesaplanır.
    onChange(
      rules.filter((_, i) => i !== index).map((r, i) => ({ ...r, order: i + 1 })),
    );
  }

  function move(index: number, direction: -1 | 1) {
    const target = index + direction;
    if (target < 0 || target >= rules.length) return;

    const next = [...rules];
    [next[index], next[target]] = [next[target], next[index]];
    onChange(next.map((r, i) => ({ ...r, order: i + 1 })));
  }

  if (rules.length === 0) {
    return (
      <div className="px-5 py-12 text-center">
        <p className="text-sm font-medium text-ink-700">Henüz kural yok</p>
        <p className="mx-auto mt-1 max-w-sm text-xs text-ink-500">
          Aşağıdan kural ekleyerek başlayın. Kurallar yukarıdan aşağıya sırayla
          çalışır; her kural bir öncekinin sonucu üzerinde işlem yapar.
        </p>
      </div>
    );
  }

  return (
    <ol className="divide-y divide-ink-200">
      {rules.map((rule, index) => (
        <RuleRow
          key={index}
          rule={rule}
          index={index}
          isFirst={index === 0}
          isLast={index === rules.length - 1}
          columns={columns}
          catalog={catalog}
          onUpdate={(patch) => update(index, patch)}
          onRemove={() => remove(index)}
          onMove={(d) => move(index, d)}
        />
      ))}
    </ol>
  );
}

// ---------------------------------------------------------------- Tek kural

function RuleRow({
  rule,
  index,
  isFirst,
  isLast,
  columns,
  catalog,
  onUpdate,
  onRemove,
  onMove,
}: {
  rule: Rule;
  index: number;
  isFirst: boolean;
  isLast: boolean;
  columns: string[];
  catalog: RuleCatalog;
  onUpdate: (patch: Partial<Rule>) => void;
  onRemove: () => void;
  onMove: (direction: -1 | 1) => void;
}) {
  const action =
    catalog.actions.find((a) => a.value === rule.action.type) ??
    catalog.actions[0];

  const conditions = rule.condition?.conditions ?? [];
  const isDatasetAction = action?.scope === "dataset";

  function setCondition(i: number, patch: Partial<Condition>) {
    const next = conditions.map((c, ci) => (ci === i ? { ...c, ...patch } : c));
    onUpdate({
      condition: { logic: rule.condition?.logic ?? "AND", conditions: next, groups: [] },
    });
  }

  function addCondition() {
    onUpdate({
      condition: {
        logic: rule.condition?.logic ?? "AND",
        conditions: [...conditions, emptyCondition(columns[0] ?? "")],
        groups: [],
      },
    });
  }

  function removeCondition(i: number) {
    onUpdate({
      condition: {
        logic: rule.condition?.logic ?? "AND",
        conditions: conditions.filter((_, ci) => ci !== i),
        groups: [],
      },
    });
  }

  return (
    <li className={cx("p-5", !rule.enabled && "bg-ink-50/70 opacity-60")}>
      {/* Başlık satırı */}
      <div className="flex flex-wrap items-center gap-3">
        <span className="grid size-6 shrink-0 place-items-center rounded-md bg-ink-800 text-xs font-semibold text-white">
          {index + 1}
        </span>

        <Input
          value={rule.name}
          onChange={(e) => onUpdate({ name: e.target.value })}
          placeholder={`Kural ${index + 1} — açıklayıcı bir ad yazın`}
          className="max-w-sm flex-1"
        />

        <div className="ml-auto flex items-center gap-1">
          <label className="mr-2 flex cursor-pointer items-center gap-1.5 text-xs text-ink-600">
            <input
              type="checkbox"
              checked={rule.enabled}
              onChange={(e) => onUpdate({ enabled: e.target.checked })}
              className="size-3.5 accent-indigo-600"
            />
            Aktif
          </label>
          <Button variant="ghost" size="sm" disabled={isFirst} onClick={() => onMove(-1)}>
            ↑
          </Button>
          <Button variant="ghost" size="sm" disabled={isLast} onClick={() => onMove(1)}>
            ↓
          </Button>
          <Button variant="danger" size="sm" onClick={onRemove}>
            Sil
          </Button>
        </div>
      </div>

      <div className="mt-4 grid gap-4 lg:grid-cols-2">
        {/* --- KOŞUL --- */}
        <div className="rounded-lg border border-ink-200 bg-ink-50/60 p-3.5">
          <div className="mb-2.5 flex items-center justify-between">
            <span className="text-xs font-semibold tracking-wide text-ink-600 uppercase">
              Koşul
            </span>
            {isDatasetAction ? (
              <Badge>Bu aksiyon tüm veri setine uygulanır</Badge>
            ) : conditions.length === 0 ? (
              <Badge tone="warning">Koşulsuz — tüm satırlar</Badge>
            ) : (
              <Select
                value={rule.condition?.logic ?? "AND"}
                onChange={(e) =>
                  onUpdate({
                    condition: {
                      logic: e.target.value as "AND" | "OR",
                      conditions,
                      groups: [],
                    },
                  })
                }
                className="w-auto py-1 text-xs"
              >
                <option value="AND">Tümü sağlanmalı (VE)</option>
                <option value="OR">Biri yeterli (VEYA)</option>
              </Select>
            )}
          </div>

          {isDatasetAction ? (
            <p className="py-3 text-xs text-ink-500">
              Kolon silme, yeniden adlandırma ve tekilleştirme işlemleri satır
              koşulu almaz.
            </p>
          ) : (
            <>
              <div className="space-y-2">
                {conditions.map((condition, ci) => (
                  <ConditionRow
                    key={ci}
                    condition={condition}
                    columns={columns}
                    operators={catalog.operators}
                    onChange={(patch) => setCondition(ci, patch)}
                    onRemove={() => removeCondition(ci)}
                  />
                ))}
              </div>

              <Button
                variant="secondary"
                size="sm"
                onClick={addCondition}
                className="mt-2.5"
              >
                + Koşul ekle
              </Button>
            </>
          )}
        </div>

        {/* --- AKSİYON --- */}
        <div className="rounded-lg border border-brand-200 bg-brand-50/50 p-3.5">
          <span className="mb-2.5 block text-xs font-semibold tracking-wide text-brand-700 uppercase">
            Aksiyon
          </span>

          <div className="grid gap-2 sm:grid-cols-2">
            <Select
              value={rule.action.type}
              onChange={(e) =>
                onUpdate({ action: { ...rule.action, type: e.target.value } })
              }
            >
              {catalog.actions.map((a) => (
                <option key={a.value} value={a.value}>
                  {a.label}
                </option>
              ))}
            </Select>

            {action?.needsColumn && (
              <ColumnPicker
                value={rule.action.targetColumn ?? ""}
                columns={columns}
                allowNew
                onChange={(v) =>
                  onUpdate({ action: { ...rule.action, targetColumn: v } })
                }
              />
            )}

            {action?.needsValue && (
              <Input
                value={rule.action.value ?? ""}
                onChange={(e) =>
                  onUpdate({ action: { ...rule.action, value: e.target.value } })
                }
                placeholder={valuePlaceholder(action)}
              />
            )}

            {action?.needsSecondValue && (
              <Input
                value={rule.action.value2 ?? ""}
                onChange={(e) =>
                  onUpdate({ action: { ...rule.action, value2: e.target.value } })
                }
                placeholder={secondValuePlaceholder(action)}
              />
            )}
          </div>

          <p className="mt-2.5 text-xs text-ink-500">
            {describe(rule, action, conditions)}
          </p>
        </div>
      </div>
    </li>
  );
}

// ---------------------------------------------------------------- Koşul satırı

function ConditionRow({
  condition,
  columns,
  operators,
  onChange,
  onRemove,
}: {
  condition: Condition;
  columns: string[];
  operators: CatalogOperator[];
  onChange: (patch: Partial<Condition>) => void;
  onRemove: () => void;
}) {
  const operator = operators.find((o) => o.value === condition.operator);

  return (
    <div className="flex flex-wrap items-center gap-2">
      <ColumnPicker
        value={condition.column}
        columns={columns}
        onChange={(v) => onChange({ column: v })}
        className="min-w-32 flex-1"
      />

      <Select
        value={condition.operator}
        onChange={(e) => onChange({ operator: e.target.value })}
        className="min-w-36 flex-1"
      >
        {operators.map((o) => (
          <option key={o.value} value={o.value}>
            {o.label}
          </option>
        ))}
      </Select>

      {operator?.needsValue && (
        <Input
          value={condition.value ?? ""}
          onChange={(e) => onChange({ value: e.target.value })}
          placeholder="değer"
          className="min-w-24 flex-1"
        />
      )}

      {operator?.needsSecondValue && (
        <Input
          value={condition.value2 ?? ""}
          onChange={(e) => onChange({ value2: e.target.value })}
          placeholder="üst sınır"
          className="min-w-24 flex-1"
        />
      )}

      <Button variant="ghost" size="sm" onClick={onRemove} title="Koşulu kaldır">
        ✕
      </Button>
    </div>
  );
}

/** Mevcut kolonlardan seçim yapılır; aksiyonlar için yeni kolon adı da yazılabilir. */
function ColumnPicker({
  value,
  columns,
  onChange,
  allowNew = false,
  className,
}: {
  value: string;
  columns: string[];
  onChange: (value: string) => void;
  allowNew?: boolean;
  className?: string;
}) {
  if (!allowNew) {
    return (
      <Select
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className={className}
      >
        <option value="">Kolon seçin…</option>
        {columns.map((c) => (
          <option key={c} value={c}>
            {c}
          </option>
        ))}
      </Select>
    );
  }

  // datalist sayesinde hem seçim hem serbest yazım (yeni kolon) mümkün.
  const listId = "cols";
  return (
    <>
      <Input
        list={listId}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder="Kolon (yeni de olabilir)"
        className={className}
      />
      <datalist id={listId}>
        {columns.map((c) => (
          <option key={c} value={c} />
        ))}
      </datalist>
    </>
  );
}

// ---------------------------------------------------------------- Yardımcılar

function valuePlaceholder(action: CatalogAction): string {
  switch (action.value) {
    case "multiply":
      return "çarpan (örn. 1.1)";
    case "divide":
      return "bölen";
    case "add":
    case "subtract":
      return "sayı";
    case "round":
      return "ondalık basamak (örn. 2)";
    case "replace":
      return "aranan metin";
    case "castDate":
      return "biçim (örn. yyyy-MM-dd)";
    case "renameColumn":
    case "copyColumn":
      return "—";
    default:
      return "atanacak değer";
  }
}

function secondValuePlaceholder(action: CatalogAction): string {
  switch (action.value) {
    case "replace":
      return "yerine yazılacak";
    case "renameColumn":
      return "yeni kolon adı";
    case "copyColumn":
      return "hedef kolon adı";
    default:
      return "";
  }
}

/** Kuralın ne yapacağını düz Türkçe cümleyle özetler. */
function describe(
  rule: Rule,
  action: CatalogAction | undefined,
  conditions: Condition[],
): string {
  const label = action?.label ?? rule.action.type;
  const column = rule.action.targetColumn || "seçilen kolon";
  const value = rule.action.value ? `"${rule.action.value}"` : "";

  const target =
    action?.scope === "dataset"
      ? "Tüm veri setinde"
      : conditions.length === 0
        ? "Tüm satırlarda"
        : `Koşulu sağlayan satırlarda`;

  if (rule.action.type === "deleteRow") return `${target} satır silinir.`;
  if (rule.action.type === "keepRow")
    return `Yalnızca koşulu sağlayan satırlar tutulur, kalanlar elenir.`;

  return `${target} "${column}" kolonuna ${label.toLowerCase()} ${value} uygulanır.`;
}
