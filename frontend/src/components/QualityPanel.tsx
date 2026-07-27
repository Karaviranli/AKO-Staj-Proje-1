"use client";

import { TYPE_LABELS, formatNumber, qualityTone } from "@/lib/format";
import type { QualityReport } from "@/lib/types";
import { Badge, Card, CardHeader, Stat } from "./ui";

/**
 * Veri kalitesi raporu — kullanıcı kural yazmadan önce
 * hangi kolonun neden sorunlu olduğunu buradan görür.
 */
export function QualityPanel({ report }: { report: QualityReport }) {
  const tone = qualityTone(report.qualityScore);

  return (
    <Card>
      <CardHeader
        title="Veri Kalitesi Analizi"
        description="Yükleme anında otomatik çıkarılan profil."
        action={
          <Badge
            tone={
              tone === "success"
                ? "success"
                : tone === "warning"
                  ? "warning"
                  : "danger"
            }
          >
            Kalite skoru: %{report.qualityScore}
          </Badge>
        }
      />

      <div className="grid gap-4 border-b border-ink-200 p-5 sm:grid-cols-4">
        <Stat label="Satır" value={formatNumber(report.rowCount)} />
        <Stat
          label="Boş Hücre"
          value={formatNumber(report.totalNullCells)}
          tone={report.totalNullCells > 0 ? "warning" : "neutral"}
        />
        <Stat
          label="Tip Uyumsuzluğu"
          value={formatNumber(report.totalTypeMismatches)}
          tone={report.totalTypeMismatches > 0 ? "danger" : "neutral"}
        />
        <Stat
          label="Tekrar Eden Satır"
          value={formatNumber(report.duplicateRowCount)}
          tone={report.duplicateRowCount > 0 ? "warning" : "neutral"}
        />
      </div>

      {report.warnings.length > 0 && (
        <div className="border-b border-ink-200 bg-amber-50/60 px-5 py-4">
          <p className="text-xs font-semibold tracking-wide text-amber-800 uppercase">
            Tespit edilen sorunlar
          </p>
          <ul className="mt-2 space-y-1">
            {report.warnings.map((warning, i) => (
              <li key={i} className="flex gap-2 text-sm text-amber-900">
                <span className="text-amber-500">•</span>
                {warning}
              </li>
            ))}
          </ul>
        </div>
      )}

      <div className="df-scroll overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="bg-ink-50">
            <tr className="text-left text-xs font-semibold text-ink-600">
              <th className="px-5 py-2.5">Kolon</th>
              <th className="px-3 py-2.5">Tip</th>
              <th className="px-3 py-2.5 text-right">Boş</th>
              <th className="px-3 py-2.5 text-right">Farklı değer</th>
              <th className="px-3 py-2.5 text-right">Uyumsuz</th>
              <th className="px-5 py-2.5">Örnek değerler</th>
            </tr>
          </thead>
          <tbody>
            {report.columns.map((column) => (
              <tr key={column.name} className="border-t border-ink-100">
                <td className="px-5 py-2.5 font-medium text-ink-900">
                  {column.name}
                </td>
                <td className="px-3 py-2.5">
                  <Badge tone={column.inferredType === "empty" ? "danger" : "neutral"}>
                    {TYPE_LABELS[column.inferredType] ?? column.inferredType}
                  </Badge>
                </td>
                <td
                  className={
                    column.nullRatio >= 0.5
                      ? "px-3 py-2.5 text-right font-medium text-amber-600 tabular-nums"
                      : "px-3 py-2.5 text-right text-ink-600 tabular-nums"
                  }
                >
                  {column.nullCount}
                  <span className="ml-1 text-xs text-ink-400">
                    (%{Math.round(column.nullRatio * 100)})
                  </span>
                </td>
                <td className="px-3 py-2.5 text-right text-ink-600 tabular-nums">
                  {column.distinctCount}
                </td>
                <td
                  className={
                    column.typeMismatchCount > 0
                      ? "px-3 py-2.5 text-right font-medium text-red-600 tabular-nums"
                      : "px-3 py-2.5 text-right text-ink-400 tabular-nums"
                  }
                >
                  {column.typeMismatchCount}
                </td>
                <td className="max-w-xs truncate px-5 py-2.5 font-mono text-xs text-ink-500">
                  {column.sampleValues.join(" · ") || "—"}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </Card>
  );
}
