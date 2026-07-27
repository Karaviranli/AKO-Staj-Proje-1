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
import type { FileSummary } from "@/lib/types";
import {
  Alert,
  Badge,
  Button,
  Card,
  EmptyState,
  Spinner,
} from "@/components/ui";

export function DatasetList() {
  const [files, setFiles] = useState<FileSummary[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [deleting, setDeleting] = useState<number | null>(null);

  useEffect(() => {
    api
      .get<FileSummary[]>("/data/files")
      .then(setFiles)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  async function remove(id: number, name: string) {
    if (!confirm(`"${name}" ve ona ait tüm işlem kayıtları silinecek. Onaylıyor musunuz?`))
      return;

    setDeleting(id);
    try {
      await api.delete(`/data/files/${id}`);
      setFiles((prev) => prev.filter((f) => f.id !== id));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Silinemedi.");
    } finally {
      setDeleting(null);
    }
  }

  if (loading) return <Spinner label="Yükleniyor…" />;
  if (error) return <Alert tone="error">{error}</Alert>;

  if (files.length === 0) {
    return (
      <Card>
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
      </Card>
    );
  }

  return (
    <Card>
      <div className="df-scroll overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="bg-ink-50">
            <tr className="text-left text-xs font-semibold text-ink-600">
              <th className="px-5 py-3">Dosya</th>
              <th className="px-3 py-3">Kaynak</th>
              <th className="px-3 py-3 text-right">Satır</th>
              <th className="px-3 py-3 text-right">Kolon</th>
              <th className="px-3 py-3 text-right">Boyut</th>
              <th className="px-3 py-3 text-center">Kalite</th>
              <th className="px-3 py-3 text-right">İşlem</th>
              <th className="px-3 py-3">Yüklenme</th>
              <th className="px-5 py-3" />
            </tr>
          </thead>
          <tbody>
            {files.map((file) => {
              const tone = qualityTone(file.qualityScore);
              return (
                <tr
                  key={file.id}
                  className="border-t border-ink-100 hover:bg-ink-50/60"
                >
                  <td className="px-5 py-3">
                    <Link
                      href={`/panel/veri-setleri/${file.id}`}
                      className="font-medium text-ink-900 hover:text-brand-600 hover:underline"
                    >
                      {file.fileName}
                    </Link>
                  </td>
                  <td className="px-3 py-3">
                    <Badge>{file.sourceType.toUpperCase()}</Badge>
                  </td>
                  <td className="px-3 py-3 text-right tabular-nums text-ink-700">
                    {formatNumber(file.rowCount)}
                  </td>
                  <td className="px-3 py-3 text-right tabular-nums text-ink-700">
                    {file.columnCount}
                  </td>
                  <td className="px-3 py-3 text-right tabular-nums text-ink-500">
                    {formatBytes(file.sizeInBytes)}
                  </td>
                  <td className="px-3 py-3 text-center">
                    <Badge
                      tone={
                        tone === "success"
                          ? "success"
                          : tone === "warning"
                            ? "warning"
                            : "danger"
                      }
                    >
                      %{file.qualityScore}
                    </Badge>
                  </td>
                  <td className="px-3 py-3 text-right tabular-nums text-ink-500">
                    {file.processedCount}
                  </td>
                  <td className="px-3 py-3 whitespace-nowrap text-ink-500">
                    {formatDate(file.uploadedAt)}
                  </td>
                  <td className="px-5 py-3 text-right">
                    <Button
                      variant="danger"
                      size="sm"
                      disabled={deleting === file.id}
                      onClick={() => remove(file.id, file.fileName)}
                    >
                      {deleting === file.id ? "Siliniyor…" : "Sil"}
                    </Button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </Card>
  );
}
