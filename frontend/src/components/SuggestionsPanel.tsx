"use client";

import { useState } from "react";
import type { Rule, RuleSuggestion } from "@/lib/types";
import { Badge, Button, Card, CardHeader, cx } from "./ui";

const CATEGORY_LABEL: Record<RuleSuggestion["category"], string> = {
  temizlik: "Temizlik",
  tip: "Tip düzeltme",
  eksik: "Eksik değer",
  tekrar: "Tekrar",
  kolon: "Kolon",
};

const CATEGORY_TONE: Record<
  RuleSuggestion["category"],
  "neutral" | "brand" | "warning" | "danger" | "success"
> = {
  temizlik: "brand",
  tip: "neutral",
  eksik: "warning",
  tekrar: "warning",
  kolon: "danger",
};

/**
 * Akıllı temizlik önerileri paneli.
 *
 * Kullanıcıya üç yol sunar:
 *  - "Tümünü uygula ve kaydet"  → sistem kendi kurallarıyla tüm dosyayı düzenler (otomatik)
 *  - "Seçilenleri zincire ekle" → istediği önerileri tek tek alıp elle düzenleyebilir
 *  - Tek tek "Ekle"             → öneriyi zincire ekleyip diğerleriyle harmanlar
 */
export function SuggestionsPanel({
  suggestions,
  onAdd,
  onApplyAll,
  running,
}: {
  suggestions: RuleSuggestion[];
  onAdd: (rules: Rule[]) => void;
  onApplyAll: (rules: Rule[]) => void;
  running: boolean;
}) {
  // Varsayılan olarak tüm öneriler seçili gelir.
  const [selected, setSelected] = useState<Set<number>>(
    new Set(suggestions.map((_, i) => i)),
  );

  function toggle(index: number) {
    setSelected((prev) => {
      const next = new Set(prev);
      next.has(index) ? next.delete(index) : next.add(index);
      return next;
    });
  }

  const selectedRules = suggestions
    .filter((_, i) => selected.has(i))
    .map((s) => s.rule);

  if (suggestions.length === 0) {
    return (
      <Card>
        <div className="px-5 py-8 text-center">
          <p className="text-sm font-medium text-emerald-700">
            Veri temiz görünüyor 🎉
          </p>
          <p className="mt-1 text-xs text-ink-500">
            Sistem otomatik düzeltilecek bir sorun bulamadı. Yine de elle kural
            ekleyebilirsin.
          </p>
        </div>
      </Card>
    );
  }

  return (
    <Card className="border-brand-200">
      <CardHeader
        title="🪄 Akıllı Temizlik Önerileri"
        description="Sistem veriyi inceledi ve şu düzeltmeleri öneriyor. Hepsini birden uygulayabilir ya da tek tek seçebilirsin."
        action={
          <Badge tone="brand">{suggestions.length} öneri</Badge>
        }
      />

      <ul className="divide-y divide-ink-100">
        {suggestions.map((suggestion, index) => (
          <li
            key={index}
            className={cx(
              "flex items-start gap-3 px-5 py-3 transition-colors",
              selected.has(index) ? "bg-brand-50/40" : "hover:bg-ink-50",
            )}
          >
            <input
              type="checkbox"
              checked={selected.has(index)}
              onChange={() => toggle(index)}
              className="mt-1 size-4 accent-indigo-600"
            />

            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-2">
                <span className="text-sm font-medium text-ink-900">
                  {suggestion.rule.name}
                </span>
                <Badge tone={CATEGORY_TONE[suggestion.category]}>
                  {CATEGORY_LABEL[suggestion.category]}
                </Badge>
                <span className="text-xs text-ink-400">
                  ~{suggestion.impact} hücre/satır
                </span>
              </div>
              <p className="mt-0.5 text-xs text-ink-500">{suggestion.reason}</p>
            </div>

            <Button
              size="sm"
              variant="ghost"
              onClick={() => onAdd([suggestion.rule])}
              title="Bu öneriyi kural zincirine ekle"
            >
              + Ekle
            </Button>
          </li>
        ))}
      </ul>

      <div className="flex flex-wrap items-center justify-between gap-3 border-t border-ink-200 bg-ink-50 px-5 py-3">
        <div className="flex items-center gap-2 text-xs text-ink-500">
          <button
            onClick={() => setSelected(new Set(suggestions.map((_, i) => i)))}
            className="font-medium text-brand-600 hover:underline"
          >
            Tümünü seç
          </button>
          <span>·</span>
          <button
            onClick={() => setSelected(new Set())}
            className="font-medium text-brand-600 hover:underline"
          >
            Seçimi kaldır
          </button>
          <span className="ml-1">({selected.size} seçili)</span>
        </div>

        <div className="flex flex-wrap gap-2">
          <Button
            variant="secondary"
            size="sm"
            disabled={selectedRules.length === 0}
            onClick={() => onAdd(selectedRules)}
          >
            Seçilenleri zincire ekle
          </Button>
          <Button
            size="sm"
            disabled={selectedRules.length === 0 || running}
            onClick={() => onApplyAll(selectedRules)}
          >
            {running ? "Uygulanıyor…" : "Seçilenleri uygula ve kaydet"}
          </Button>
        </div>
      </div>
    </Card>
  );
}
