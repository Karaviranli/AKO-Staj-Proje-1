import { DatasetList } from "./DatasetList";

export const metadata = { title: "Veri Setleri — DataFlow" };

export default function DatasetsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-xl font-semibold text-ink-900">Veri Setleri</h1>
        <p className="mt-0.5 text-sm text-ink-500">
          Yüklediğiniz tüm veri setleri. Kural tanımlamak için birini seçin.
        </p>
      </div>

      <DatasetList />
    </div>
  );
}
