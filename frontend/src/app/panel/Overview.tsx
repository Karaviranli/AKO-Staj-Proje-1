"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { api } from "@/lib/api";
import {
  formatBytes,
  formatDate,
  formatNumber,
  qualityTone,
} from "@/lib/format";
import type { FileSummary, ProcessedSummary } from "@/lib/types";
import {
  Alert,
  Badge,
  Card,
  CardHeader,
  EmptyState,
  Spinner,
  Stat,
} from "@/components/ui";

export function Overview() {
  const [files, setFiles] = useState<FileSummary[]>([]);
  const [processed, setProcessed] = useState<ProcessedSummary[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([
      api.get<FileSummary[]>("/data/files"),
      api.get<ProcessedSummary[]>("/data/processed"),
    ])
      .then(([f, p]) => {
        setFiles(f);
        setProcessed(p);
      })
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <Spinner label="Yükleniyor…" />;
  if (error) return <Alert tone="error">{error}</Alert>;

  const totalRows = files.reduce((sum, f) => sum + f.rowCount, 0);
  const avgQuality = files.length
    ? Math.round(files.reduce((s, f) => s + f.qualityScore, 0) / files.length)
    : 100;
  const cellsFixed = processed.reduce((s, p) => s + p.cellsModified, 0);

  return (
    <div className="space-y-6">
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Stat
          label="Veri Seti"
          value={files.length}
          sub={`${formatNumber(totalRows)} toplam satır`}
        />
        <Stat
          label="Ortalama Kalite"
          value={`%${avgQuality}`}
          tone={qualityTone(avgQuality)}
          sub="Yükleme anındaki skor"
        />
        <Stat
          label="Çalıştırılan İşlem"
          value={processed.length}
          sub="Kaydedilmiş kural seti"
        />
        <Stat
          label="Düzeltilen Hücre"
          value={formatNumber(cellsFixed)}
          tone="success"
          sub="Kural motoru tarafından"
        />
      </div>

      <div className="grid gap-6 lg:grid-cols-5">
        <Card className="lg:col-span-3">
          <CardHeader
            title="Veri Setleri"
            description="En son yüklenenler"
            action={
              <Link
                href="/panel/veri-setleri"
                className="text-xs font-medium text-brand-600 hover:underline"
              >
                Tümünü gör →
              </Link>
            }
          />

          {files.length === 0 ? (
            <EmptyState
              title="Henüz veri seti yok"
              description="CSV, Excel veya JSON dosyanızı yükleyerek başlayın."
              action={
                <Link
                  href="/panel/yukle"
                  className="text-sm font-medium text-brand-600 hover:underline"
                >
                  Veri yükle →
                </Link>
              }
            />
          ) : (
            <ul className="divide-y divide-ink-100">
              {files.slice(0, 6).map((file) => (
                <li key={file.id}>
                  <Link
                    href={`/panel/veri-setleri/${file.id}`}
                    className="flex items-center justify-between gap-4 px-5 py-3 transition-colors hover:bg-ink-50"
                  >
                    <div className="min-w-0">
                      <p className="truncate text-sm font-medium text-ink-900">
                        {file.fileName}
                      </p>
                      <p className="mt-0.5 text-xs text-ink-500">
                        {formatNumber(file.rowCount)} satır ·{" "}
                        {file.columnCount} kolon · {formatBytes(file.sizeInBytes)}{" "}
                        · {formatDate(file.uploadedAt)}
                      </p>
                    </div>
                    <div className="flex shrink-0 items-center gap-2">
                      <Badge tone="neutral">
                        {file.sourceType.toUpperCase()}
                      </Badge>
                      <Badge
                        tone={
                          qualityTone(file.qualityScore) === "success"
                            ? "success"
                            : qualityTone(file.qualityScore) === "warning"
                              ? "warning"
                              : "danger"
                        }
                      >
                        %{file.qualityScore}
                      </Badge>
                    </div>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </Card>

        <Card className="lg:col-span-2">
          <CardHeader
            title="Son İşlemler"
            description="Kural motoru çalıştırmaları"
            action={
              <Link
                href="/panel/gecmis"
                className="text-xs font-medium text-brand-600 hover:underline"
              >
                Geçmiş →
              </Link>
            }
          />

          {processed.length === 0 ? (
            <EmptyState
              title="Henüz işlem yapılmadı"
              description="Bir veri seti açıp kural tanımlayın."
            />
          ) : (
            <ul className="divide-y divide-ink-100">
              {processed.slice(0, 6).map((item) => (
                <li key={item.id} className="px-5 py-3">
                  <p className="truncate text-sm font-medium text-ink-900">
                    {item.name}
                  </p>
                  <p className="mt-0.5 text-xs text-ink-500">
                    {item.ruleCount} kural · {formatNumber(item.rowsBefore)} →{" "}
                    {formatNumber(item.rowsAfter)} satır ·{" "}
                    {formatNumber(item.cellsModified)} hücre
                  </p>
                  <p className="mt-0.5 text-xs text-ink-400">
                    {formatDate(item.processedAt)}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>
    </div>
  );
}
