"use client";

import { useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { formatNumber } from "@/lib/format";
import type { UploadResult } from "@/lib/types";
import {
  Alert,
  Button,
  Card,
  CardHeader,
  Field,
  Input,
  Select,
  Spinner,
  cx,
} from "@/components/ui";
import { QualityPanel } from "@/components/QualityPanel";
import { DataTable } from "@/components/DataTable";

type Mode = "file" | "post";

const ACCEPTED = ".csv,.xlsx,.xls,.json";

export function UploadPanel() {
  const router = useRouter();
  const [mode, setMode] = useState<Mode>("file");
  const [result, setResult] = useState<UploadResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  return (
    <div className="space-y-6">
      {/* Mod seçici — projedeki iki yükleme yolu */}
      <div className="inline-flex rounded-lg border border-ink-300 bg-white p-1">
        {(
          [
            ["file", "Dosya Yükleme"],
            ["post", "POST ile Veri Gönderme"],
          ] as const
        ).map(([value, label]) => (
          <button
            key={value}
            onClick={() => {
              setMode(value);
              setError(null);
            }}
            className={cx(
              "rounded-md px-3.5 py-1.5 text-sm font-medium transition-colors",
              mode === value
                ? "bg-brand-600 text-white"
                : "text-ink-600 hover:text-ink-900",
            )}
          >
            {label}
          </button>
        ))}
      </div>

      {error && <Alert tone="error">{error}</Alert>}

      {mode === "file" ? (
        <FileUpload
          busy={busy}
          setBusy={setBusy}
          onError={setError}
          onDone={setResult}
        />
      ) : (
        <PostUpload
          busy={busy}
          setBusy={setBusy}
          onError={setError}
          onDone={setResult}
        />
      )}

      {result && (
        <>
          <Alert tone="success">
            <strong>{result.fileName}</strong> yüklendi —{" "}
            {formatNumber(result.rowCount)} satır, {result.columnCount} kolon.
          </Alert>

          {result.quality && <QualityPanel report={result.quality} />}

          <Card>
            <CardHeader
              title="Ham Veri Önizlemesi"
              description={`İlk ${result.preview.length} satır — henüz hiçbir kural uygulanmadı.`}
              action={
                <Button
                  size="sm"
                  onClick={() =>
                    router.push(`/panel/veri-setleri/${result.fileId}`)
                  }
                >
                  Kural tanımla →
                </Button>
              }
            />
            <DataTable columns={result.columns} rows={result.preview} />
          </Card>
        </>
      )}
    </div>
  );
}

// ---------------------------------------------------------------- Dosya

type ChildProps = {
  busy: boolean;
  setBusy: (v: boolean) => void;
  onError: (m: string | null) => void;
  onDone: (r: UploadResult) => void;
};

function FileUpload({ busy, setBusy, onError, onDone }: ChildProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [dragging, setDragging] = useState(false);
  const [fileName, setFileName] = useState<string | null>(null);

  async function send(file: File) {
    onError(null);
    setFileName(file.name);
    setBusy(true);

    try {
      onDone(await api.upload<UploadResult>("/data/upload", file));
    } catch (e) {
      onError(e instanceof Error ? e.message : "Yükleme başarısız.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card>
      <CardHeader
        title="Dosya Yükleme"
        description="CSV, XLSX veya JSON — en fazla 25 MB."
      />

      <div className="p-5">
        <div
          onDragOver={(e) => {
            e.preventDefault();
            setDragging(true);
          }}
          onDragLeave={() => setDragging(false)}
          onDrop={(e) => {
            e.preventDefault();
            setDragging(false);
            const file = e.dataTransfer.files?.[0];
            if (file) void send(file);
          }}
          onClick={() => inputRef.current?.click()}
          className={cx(
            "flex cursor-pointer flex-col items-center justify-center rounded-xl border-2 border-dashed px-6 py-12 text-center transition-colors",
            dragging
              ? "border-brand-500 bg-brand-50"
              : "border-ink-300 bg-ink-50 hover:border-brand-400 hover:bg-brand-50/50",
          )}
        >
          {busy ? (
            <Spinner label={`${fileName} işleniyor…`} />
          ) : (
            <>
              <p className="text-sm font-medium text-ink-700">
                Dosyayı buraya sürükleyin veya tıklayarak seçin
              </p>
              <p className="mt-1 text-xs text-ink-500">
                Desteklenen formatlar: CSV · XLSX · XLS · JSON
              </p>
              {fileName && (
                <p className="mt-3 font-mono text-xs text-ink-400">
                  Son seçilen: {fileName}
                </p>
              )}
            </>
          )}
        </div>

        <input
          ref={inputRef}
          type="file"
          accept={ACCEPTED}
          className="hidden"
          onChange={(e) => {
            const file = e.target.files?.[0];
            if (file) void send(file);
            e.target.value = "";
          }}
        />
      </div>
    </Card>
  );
}

// ---------------------------------------------------------------- POST

const SAMPLE_JSON = `[
  { "Ad": "Ahmet", "Yas": 62, "DogumYeri": null, "Tutar": "1.250,50" },
  { "Ad": "  ayşe ", "Yas": 3, "DogumYeri": "İzmir", "Tutar": "3400" },
  { "Ad": "Mehmet", "Yas": 34, "DogumYeri": "n/a", "Tutar": "abc" }
]`;

function PostUpload({ busy, setBusy, onError, onDone }: ChildProps) {
  const [name, setName] = useState("API Verisi");
  const [category, setCategory] = useState("genel");
  const [raw, setRaw] = useState(SAMPLE_JSON);

  async function send() {
    onError(null);

    let rows: unknown;
    try {
      rows = JSON.parse(raw);
    } catch {
      onError("JSON geçerli değil. Söz dizimini kontrol edin.");
      return;
    }

    if (!Array.isArray(rows) || rows.length === 0) {
      onError("Gövde, en az bir nesne içeren bir dizi olmalıdır.");
      return;
    }

    setBusy(true);
    try {
      onDone(
        await api.post<UploadResult>("/data/push", {
          datasetName: name,
          category,
          rows,
        }),
      );
    } catch (e) {
      onError(e instanceof Error ? e.message : "Gönderim başarısız.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <Card>
      <CardHeader
        title="POST ile Veri Gönderme"
        description="Dosya olmadan, doğrudan JSON gövdesiyle veri aktarımı — POST /api/data/push"
      />

      <div className="space-y-4 p-5">
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Veri seti adı">
            <Input value={name} onChange={(e) => setName(e.target.value)} />
          </Field>
          <Field label="Kategori">
            <Select
              value={category}
              onChange={(e) => setCategory(e.target.value)}
            >
              <option value="genel">Genel</option>
              <option value="satis">Satış Verileri</option>
              <option value="calisan">Çalışan Verileri</option>
            </Select>
          </Field>
        </div>

        <Field
          label="JSON gövdesi"
          hint="Nesnelerden oluşan bir dizi. Eksik alanlar otomatik olarak boş kabul edilir."
        >
          <textarea
            value={raw}
            onChange={(e) => setRaw(e.target.value)}
            rows={12}
            spellCheck={false}
            className="df-scroll w-full rounded-lg border border-ink-300 bg-ink-50 px-3 py-2.5 font-mono text-xs text-ink-800 focus:border-brand-500 focus:ring-2 focus:ring-brand-100 focus:outline-none"
          />
        </Field>

        <div className="flex items-center gap-3">
          <Button onClick={send} disabled={busy}>
            {busy ? "Gönderiliyor…" : "Veriyi gönder"}
          </Button>
          <Button
            variant="secondary"
            onClick={() => setRaw(SAMPLE_JSON)}
            disabled={busy}
          >
            Örneği geri yükle
          </Button>
        </div>
      </div>
    </Card>
  );
}
