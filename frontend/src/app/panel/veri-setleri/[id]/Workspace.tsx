"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { api } from "@/lib/api";
import { formatNumber } from "@/lib/format";
import type {
  PagedData,
  ProcessResult,
  Rule,
  RuleCatalog,
  RulePreset,
} from "@/lib/types";
import {
  Alert,
  Badge,
  Button,
  Card,
  CardHeader,
  Input,
  Select,
  Spinner,
  cx,
} from "@/components/ui";
import { DataTable } from "@/components/DataTable";
import { RuleBuilder, emptyRule } from "@/components/RuleBuilder";
import { ExecutionReport } from "@/components/ExecutionReport";

/**
 * Kural Stüdyosu — projenin ana ekranı.
 * Solda ham veri, sağda kural kurgusu; "Önizle" ile sonuç anında görülür,
 * "Kaydet" ile işlenmiş veri kalıcı hale gelir.
 */
export function Workspace({ fileId }: { fileId: number }) {
  const [source, setSource] = useState<PagedData | null>(null);
  const [catalog, setCatalog] = useState<RuleCatalog | null>(null);
  const [presets, setPresets] = useState<RulePreset[]>([]);

  const [rules, setRules] = useState<Rule[]>([]);
  const [name, setName] = useState("");
  const [result, setResult] = useState<ProcessResult | null>(null);

  const [loading, setLoading] = useState(true);
  const [running, setRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [tab, setTab] = useState<"source" | "result">("source");

  useEffect(() => {
    Promise.all([
      api.get<PagedData>(`/data/files/${fileId}?page=1&pageSize=200`),
      api.get<RuleCatalog>("/rules/catalog"),
      api.get<RulePreset[]>("/rules/presets"),
    ])
      .then(([data, cat, pre]) => {
        setSource(data);
        setCatalog(cat);
        setPresets(pre);
      })
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false));
  }, [fileId]);

  const run = useCallback(
    async (dryRun: boolean) => {
      if (rules.length === 0) {
        setError("Çalıştırmak için en az bir kural tanımlayın.");
        return;
      }

      setRunning(true);
      setError(null);
      setNotice(null);

      try {
        const data = await api.post<ProcessResult>("/data/process", {
          fileId,
          name: name || undefined,
          dryRun,
          // Sıra numaraları listedeki konuma göre yeniden yazılır.
          rules: rules.map((r, i) => ({ ...r, order: i + 1 })),
        });

        setResult(data);
        setTab("result");
        setNotice(
          dryRun
            ? "Önizleme oluşturuldu — bu sonuç henüz kaydedilmedi."
            : "İşlenmiş veri kaydedildi. İşlem geçmişinden erişebilirsiniz.",
        );
      } catch (e) {
        setError(e instanceof Error ? e.message : "İşlem başarısız.");
      } finally {
        setRunning(false);
      }
    },
    [fileId, name, rules],
  );

  function applyPreset(id: string) {
    const preset = presets.find((p) => String(p.id) === id);
    if (!preset) return;

    setRules(preset.rules.map((r, i) => ({ ...r, order: i + 1 })));
    setName(preset.name);
    setNotice(`"${preset.name}" şablonu yüklendi — düzenleyebilirsiniz.`);
  }

  if (loading) return <Spinner label="Veri seti yükleniyor…" />;
  if (error && !source) return <Alert tone="error">{error}</Alert>;
  if (!source || !catalog) return null;

  // Kural motorunun ürettiği yeni kolonlar tabloda vurgulanır.
  const newColumns =
    result?.columns.filter((c) => !source.columns.includes(c)) ?? [];

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <Link
            href="/panel/veri-setleri"
            className="text-xs text-ink-500 hover:text-brand-600 hover:underline"
          >
            ← Veri setleri
          </Link>
          <h1 className="mt-1 text-xl font-semibold text-ink-900">
            Kural Stüdyosu
          </h1>
          <p className="mt-0.5 text-sm text-ink-500">
            {formatNumber(source.totalRows)} satır · {source.columns.length}{" "}
            kolon
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <Select
            defaultValue=""
            onChange={(e) => applyPreset(e.target.value)}
            className="w-auto"
          >
            <option value="">Hazır şablon seç…</option>
            {presets.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name}
                {p.isSystemPreset ? " (sistem)" : ""}
              </option>
            ))}
          </Select>

          <Button
            variant="secondary"
            onClick={() => run(true)}
            disabled={running}
          >
            {running ? "Çalışıyor…" : "Önizle"}
          </Button>
          <Button onClick={() => run(false)} disabled={running}>
            Çalıştır ve kaydet
          </Button>
        </div>
      </div>

      {error && <Alert tone="error">{error}</Alert>}
      {notice && <Alert tone={result?.dryRun ? "info" : "success"}>{notice}</Alert>}

      {/* --- Kural kurgusu --- */}
      <Card>
        <CardHeader
          title="Kural Zinciri"
          description="Kurallar yukarıdan aşağıya sırayla çalışır — her kural bir öncekinin çıktısı üzerinde işlem yapar."
          action={
            <div className="flex items-center gap-2">
              <Input
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="İşlem adı (isteğe bağlı)"
                className="w-56"
              />
              <Button
                size="sm"
                variant="secondary"
                onClick={() => setRules((r) => [...r, emptyRule(r.length + 1)])}
              >
                + Kural ekle
              </Button>
            </div>
          }
        />

        <RuleBuilder
          rules={rules}
          columns={source.columns}
          catalog={catalog}
          onChange={setRules}
        />

        {rules.length > 0 && (
          <div className="flex items-center justify-between border-t border-ink-200 bg-ink-50 px-5 py-3">
            <p className="text-xs text-ink-500">
              {rules.filter((r) => r.enabled).length} aktif kural /{" "}
              {rules.length} toplam
            </p>
            <Button variant="ghost" size="sm" onClick={() => setRules([])}>
              Tümünü temizle
            </Button>
          </div>
        )}
      </Card>

      {/* --- Sonuç --- */}
      {result && <ExecutionReport result={result} />}

      {/* --- Veri görünümü --- */}
      <Card>
        <CardHeader
          title="Veri"
          description={
            tab === "source"
              ? "Yüklenen ham veri — hiçbir kural uygulanmamış hali."
              : "Kural zincirinden geçmiş sonuç."
          }
          action={
            <div className="flex items-center gap-2">
              {result?.processedDatasetId && (
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() =>
                    api.download(
                      `/data/processed/${result.processedDatasetId}/export`,
                      `${result.name || "dataflow"}.csv`,
                    )
                  }
                >
                  CSV indir
                </Button>
              )}
              <div className="inline-flex rounded-lg border border-ink-300 bg-white p-0.5">
                {(
                  [
                    ["source", `Ham (${formatNumber(source.totalRows)})`],
                    [
                      "result",
                      result
                        ? `Sonuç (${formatNumber(result.rowsAfter)})`
                        : "Sonuç",
                    ],
                  ] as const
                ).map(([value, label]) => (
                  <button
                    key={value}
                    onClick={() => setTab(value)}
                    disabled={value === "result" && !result}
                    className={cx(
                      "rounded-md px-3 py-1 text-xs font-medium transition-colors",
                      tab === value
                        ? "bg-ink-800 text-white"
                        : "text-ink-600 hover:text-ink-900 disabled:text-ink-300",
                    )}
                  >
                    {label}
                  </button>
                ))}
              </div>
            </div>
          }
        />

        {tab === "source" ? (
          <DataTable columns={source.columns} rows={source.rows} />
        ) : result ? (
          <DataTable
            columns={result.columns}
            rows={result.rows}
            highlightColumns={newColumns}
          />
        ) : null}

        {tab === "result" && newColumns.length > 0 && (
          <div className="border-t border-ink-200 bg-brand-50/50 px-5 py-2.5">
            <span className="text-xs text-brand-700">
              Kural motorunun eklediği yeni kolonlar:{" "}
              {newColumns.map((c) => (
                <Badge key={c} tone="brand">
                  {c}
                </Badge>
              ))}
            </span>
          </div>
        )}
      </Card>
    </div>
  );
}
