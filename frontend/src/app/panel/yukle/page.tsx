import { UploadPanel } from "./UploadPanel";

export const metadata = { title: "Veri Yükle — DataFlow" };

export default function UploadPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-xl font-semibold text-ink-900">Veri Yükle</h1>
        <p className="mt-0.5 text-sm text-ink-500">
          Dosya yükleyin veya JSON gövdesiyle doğrudan veri gönderin.
        </p>
      </div>

      <UploadPanel />
    </div>
  );
}
