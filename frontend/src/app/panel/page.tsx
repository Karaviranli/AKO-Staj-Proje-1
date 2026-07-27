import Link from "next/link";
import { Overview } from "./Overview";

export const metadata = { title: "Genel Bakış — DataFlow" };

export default function PanelPage() {
  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-xl font-semibold text-ink-900">Genel Bakış</h1>
          <p className="mt-0.5 text-sm text-ink-500">
            Yüklenen veri setleri, kalite durumu ve son işlemler.
          </p>
        </div>
        <Link
          href="/panel/yukle"
          className="rounded-lg bg-brand-600 px-3.5 py-2 text-sm font-medium text-white transition-colors hover:bg-brand-700"
        >
          Yeni veri yükle
        </Link>
      </div>

      <Overview />
    </div>
  );
}
