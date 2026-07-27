"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { api } from "@/lib/api";
import { formatDate, formatNumber } from "@/lib/format";
import type { ProcessResult, ProcessedSummary } from "@/lib/types";
import {
  Alert,
  Button,
  Card,
  CardHeader,
  EmptyState,
  Spinner,
} from "@/components/ui";
import { DataTable } from "@/components/DataTable";
import { ExecutionReport } from "@/components/ExecutionReport";

export function HistoryList() {
  const [items, setItems] = useState<ProcessedSummary[]>([]);
  const [detail, setDetail] = useState<ProcessResult | null>(null);
  const [openId, setOpenId] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api
      .get<ProcessedSummary[]>("/data/processed")
      .then(setItems)
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, []);

  async function open(id: number) {
    if (openId === id) {
      setOpenId(null);
      setDetail(null);
      return;
    }

    setBusy(true);
    setOpenId(id);
    try {
      setDetail(await api.get<ProcessResult>(`/data/processed/${id}`));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Detay yüklenemedi.");
    } finally {
      setBusy(false);
    }
  }

  if (loading) return <Spinner label="Yükleniyor…" />;
  if (error) return <Alert tone="error">{error}</Alert>;

  if (items.length === 0) {
    return (
      <Card>
        <EmptyState
          title="Henüz işlem yok"
          description="Bir veri seti açıp kural zinciri çalıştırdığınızda kayıtlar burada listelenir."
          action={
            <Link
              href="/panel/veri-setleri"
              className="text-sm font-medium text-brand-600 hover:underline"
            >
              Veri setlerine git →
            </Link>
          }
        />
      </Card>
    );
  }

  return (
    <div className="space-y-4">
      <Card>
        <div className="df-scroll overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-ink-50">
              <tr className="text-left text-xs font-semibold text-ink-600">
                <th className="px-5 py-3">İşlem</th>
                <th className="px-3 py-3">Kaynak dosya</th>
                <th className="px-3 py-3 text-right">Kural</th>
                <th className="px-3 py-3 text-right">Satır</th>
                <th className="px-3 py-3 text-right">Hücre</th>
                <th className="px-3 py-3">Tarih</th>
                <th className="px-5 py-3" />
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                <tr
                  key={item.id}
                  className="border-t border-ink-100 hover:bg-ink-50/60"
                >
                  <td className="px-5 py-3 font-medium text-ink-900">
                    {item.name}
                  </td>
                  <td className="px-3 py-3 text-ink-600">
                    <Link
                      href={`/panel/veri-setleri/${item.uploadedFileId}`}
                      className="hover:text-brand-600 hover:underline"
                    >
                      {item.fileName}
                    </Link>
                  </td>
                  <td className="px-3 py-3 text-right tabular-nums text-ink-700">
                    {item.ruleCount}
                  </td>
                  <td className="px-3 py-3 text-right tabular-nums text-ink-700">
                    {formatNumber(item.rowsBefore)} →{" "}
                    {formatNumber(item.rowsAfter)}
                  </td>
                  <td className="px-3 py-3 text-right tabular-nums text-emerald-600">
                    {formatNumber(item.cellsModified)}
                  </td>
                  <td className="px-3 py-3 whitespace-nowrap text-ink-500">
                    {formatDate(item.processedAt)}
                  </td>
                  <td className="px-5 py-3 text-right">
                    <div className="flex justify-end gap-1.5">
                      <Button
                        variant="secondary"
                        size="sm"
                        onClick={() => open(item.id)}
                      >
                        {openId === item.id ? "Kapat" : "Detay"}
                      </Button>
                      <Button
                        variant="secondary"
                        size="sm"
                        onClick={() =>
                          api.download(
                            `/data/processed/${item.id}/export`,
                            `${item.name}.csv`,
                          )
                        }
                      >
                        CSV
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>

      {busy && <Spinner label="Detay yükleniyor…" />}

      {detail && !busy && (
        <>
          <ExecutionReport result={detail} />
          <Card>
            <CardHeader
              title="Temizlenmiş Veri"
              description={`İlk ${detail.rows.length} satır gösteriliyor.`}
            />
            <DataTable columns={detail.columns} rows={detail.rows} />
          </Card>
        </>
      )}
    </div>
  );
}
