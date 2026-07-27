"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cx } from "./ui";

const NAV = [
  { href: "/panel", label: "Genel Bakış", exact: true },
  { href: "/panel/yukle", label: "Veri Yükle" },
  { href: "/panel/veri-setleri", label: "Veri Setleri" },
  { href: "/panel/gecmis", label: "İşlem Geçmişi" },
];

export function Sidebar() {
  const pathname = usePathname();

  return (
    <aside className="df-no-print sticky top-0 hidden h-screen w-56 shrink-0 flex-col border-r border-ink-200 bg-white md:flex">
      <div className="flex h-14 items-center gap-2.5 border-b border-ink-200 px-5">
        <span className="grid size-8 place-items-center rounded-lg bg-brand-600 text-xs font-bold text-white">
          DF
        </span>
        <span className="font-semibold tracking-tight text-ink-900">
          DataFlow
        </span>
      </div>

      <nav className="flex-1 space-y-0.5 p-3">
        {NAV.map((item) => {
          const active = item.exact
            ? pathname === item.href
            : pathname.startsWith(item.href);

          return (
            <Link
              key={item.href}
              href={item.href}
              className={cx(
                "block rounded-lg px-3 py-2 text-sm transition-colors",
                active
                  ? "bg-brand-50 font-medium text-brand-700"
                  : "text-ink-600 hover:bg-ink-100 hover:text-ink-900",
              )}
            >
              {item.label}
            </Link>
          );
        })}
      </nav>

      <div className="border-t border-ink-200 p-4">
        <p className="text-xs text-ink-400">Sürüm 1.0.0</p>
        <a
          href="http://localhost:5080/swagger"
          target="_blank"
          rel="noreferrer"
          className="mt-1 block text-xs text-brand-600 hover:underline"
        >
          API Dokümantasyonu →
        </a>
      </div>
    </aside>
  );
}
