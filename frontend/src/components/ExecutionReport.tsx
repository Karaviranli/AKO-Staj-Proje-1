"use client";

import { formatNumber } from "@/lib/format";
import type { ProcessResult } from "@/lib/types";
import { Badge, Card, CardHeader, Stat } from "./ui";

/**
 * Yürütme raporu — her kuralın kaç satıra dokunduğunu ve kaç hücreyi
 * değiştirdiğini adım adım gösterir. Sistemin "kara kutu" olmadığının kanıtı.
 */
export function ExecutionReport({ result }: { result: ProcessResult }) {
  const removed = result.rowsBefore - result.rowsAfter;

  return (
    <Card>
      <CardHeader
        title="Yürütme Raporu"
        description={`${result.executionLog.length} kural ${result.durationMs} ms içinde çalıştırıldı.`}
        action={
          result.dryRun ? (
            <Badge tone="warning">Önizleme — kaydedilmedi</Badge>
          ) : (
            <Badge tone="success">Kaydedildi</Badge>
          )
        }
      />

      <div className="grid gap-4 border-b border-ink-200 p-5 sm:grid-cols-4">
        <Stat label="Giriş Satırı" value={formatNumber(result.rowsBefore)} />
        <Stat
          label="Çıkış Satırı"
          value={formatNumber(result.rowsAfter)}
          sub={removed > 0 ? `${formatNumber(removed)} satır elendi` : "Satır kaybı yok"}
          tone={removed > 0 ? "warning" : "neutral"}
        />
        <Stat
          label="Düzeltilen Hücre"
          value={formatNumber(result.cellsModified)}
          tone="success"
        />
        <Stat label="Süre" value={`${result.durationMs} ms`} />
      </div>

      <ol className="divide-y divide-ink-100">
        {result.executionLog.map((log, index) => (
          <li key={index} className="flex gap-3 px-5 py-3">
            <span
              className={
                log.skipped
                  ? "mt-0.5 grid size-6 shrink-0 place-items-center rounded-md bg-amber-100 text-xs font-semibold text-amber-700"
                  : "mt-0.5 grid size-6 shrink-0 place-items-center rounded-md bg-ink-800 text-xs font-semibold text-white"
              }
            >
              {log.order}
            </span>

            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-2">
                <p className="text-sm font-medium text-ink-900">
                  {log.ruleName}
                </p>
                {log.skipped && <Badge tone="warning">Atlandı</Badge>}
                {log.durationMs > 0 && (
                  <span className="text-xs text-ink-400">{log.durationMs} ms</span>
                )}
              </div>

              <p className="mt-0.5 text-sm text-ink-600">{log.summary}</p>

              {log.warning && !log.skipped && (
                <p className="mt-1 text-xs text-amber-700">⚠ {log.warning}</p>
              )}
            </div>

            <div className="hidden shrink-0 text-right sm:block">
              <p className="text-xs tabular-nums text-ink-500">
                {formatNumber(log.rowsBefore)} → {formatNumber(log.rowsAfter)}{" "}
                satır
              </p>
              {log.cellsModified > 0 && (
                <p className="text-xs tabular-nums text-emerald-600">
                  +{formatNumber(log.cellsModified)} hücre
                </p>
              )}
            </div>
          </li>
        ))}
      </ol>
    </Card>
  );
}
