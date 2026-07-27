import { HistoryList } from "./HistoryList";

export const metadata = { title: "İşlem Geçmişi — DataFlow" };

export default function HistoryPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-xl font-semibold text-ink-900">İşlem Geçmişi</h1>
        <p className="mt-0.5 text-sm text-ink-500">
          Çalıştırılan tüm kural setleri ve sonuçları. Her kayıt tekrar
          üretilebilir — uygulanan kurallar birlikte saklanır.
        </p>
      </div>

      <HistoryList />
    </div>
  );
}
