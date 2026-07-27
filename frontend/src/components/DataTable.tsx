"use client";

import { cellText, isEmptyCell } from "@/lib/format";
import type { DataRow } from "@/lib/types";
import { cx } from "./ui";

/**
 * Dinamik kolonlu veri tablosu. Kolon adları çalışma zamanında geldiği için
 * sabit bir tablo şeması yoktur.
 */
export function DataTable({
  columns,
  rows,
  highlightColumns = [],
  maxHeight = "28rem",
}: {
  columns: string[];
  rows: DataRow[];
  /** Kural motorunun eklediği/değiştirdiği kolonlar görsel olarak vurgulanır. */
  highlightColumns?: string[];
  maxHeight?: string;
}) {
  if (columns.length === 0 || rows.length === 0) {
    return (
      <p className="px-5 py-10 text-center text-sm text-ink-500">
        Gösterilecek satır yok.
      </p>
    );
  }

  const highlighted = new Set(highlightColumns.map((c) => c.toLowerCase()));

  return (
    <div className="df-scroll overflow-auto" style={{ maxHeight }}>
      <table className="w-full border-collapse text-sm">
        <thead className="sticky top-0 z-10 bg-ink-50">
          <tr>
            <th className="w-12 border-b border-ink-200 px-3 py-2.5 text-left text-xs font-medium text-ink-400">
              #
            </th>
            {columns.map((column) => (
              <th
                key={column}
                className={cx(
                  "border-b border-ink-200 px-3 py-2.5 text-left text-xs font-semibold whitespace-nowrap",
                  highlighted.has(column.toLowerCase())
                    ? "bg-brand-50 text-brand-700"
                    : "text-ink-600",
                )}
              >
                {column}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, index) => (
            <tr key={index} className="even:bg-ink-50/60 hover:bg-brand-50/40">
              <td className="px-3 py-2 text-xs tabular-nums text-ink-400">
                {index + 1}
              </td>
              {columns.map((column) => {
                const value = row[column];
                return (
                  <td
                    key={column}
                    className={cx(
                      "border-b border-ink-100 px-3 py-2 whitespace-nowrap",
                      isEmptyCell(value)
                        ? "text-ink-300 italic"
                        : "text-ink-800",
                      highlighted.has(column.toLowerCase()) && "bg-brand-50/40",
                    )}
                    title={cellText(value)}
                  >
                    {cellText(value)}
                  </td>
                );
              })}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
