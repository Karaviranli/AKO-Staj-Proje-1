/** Arayüzde tekrarlayan biçimlendirmeler tek yerde toplanır. */

export function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}

export function formatNumber(value: number): string {
  return new Intl.NumberFormat("tr-TR").format(value);
}

export function formatDate(iso: string): string {
  return new Intl.DateTimeFormat("tr-TR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  }).format(new Date(iso));
}

/** Tablo hücresi — null/boş değerler görsel olarak ayırt edilebilmeli. */
export function cellText(value: unknown): string {
  if (value === null || value === undefined || value === "") return "—";
  if (typeof value === "boolean") return value ? "Evet" : "Hayır";
  if (typeof value === "number") return formatNumber(value);
  return String(value);
}

export function isEmptyCell(value: unknown): boolean {
  return value === null || value === undefined || value === "";
}

export function qualityTone(score: number): "success" | "warning" | "danger" {
  if (score >= 85) return "success";
  if (score >= 60) return "warning";
  return "danger";
}

export const TYPE_LABELS: Record<string, string> = {
  number: "Sayı",
  date: "Tarih",
  boolean: "Evet/Hayır",
  text: "Metin",
  empty: "Boş",
};
