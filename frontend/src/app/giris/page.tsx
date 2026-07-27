import { Suspense } from "react";
import { LoginForm } from "./LoginForm";

export const metadata = { title: "Giriş — DataFlow" };

export default function LoginPage() {
  return (
    <main className="grid min-h-screen lg:grid-cols-2">
      {/* Sol taraf — kurumsal tanıtım paneli */}
      <aside className="hidden flex-col justify-between bg-ink-900 p-12 text-white lg:flex">
        <div className="flex items-center gap-2.5">
          <span className="grid size-9 place-items-center rounded-lg bg-brand-600 text-sm font-bold">
            DF
          </span>
          <span className="text-lg font-semibold tracking-tight">DataFlow</span>
        </div>

        <div className="max-w-md">
          <h1 className="text-3xl leading-tight font-semibold">
            Karışık veriyi
            <br />
            kurallarla düzene sokun.
          </h1>
          <p className="mt-4 text-sm leading-relaxed text-ink-300">
            CSV, Excel ve JSON dosyalarınızı yükleyin; sistem eksik, tutarsız ve
            tekrar eden kayıtları otomatik tespit etsin. Sıralı kurallar
            tanımlayın, her adımın kaç satırı etkilediğini görün.
          </p>

          <dl className="mt-10 grid grid-cols-3 gap-6 border-t border-ink-700 pt-6">
            <div>
              <dt className="text-xs text-ink-400">Desteklenen format</dt>
              <dd className="mt-1 text-xl font-semibold">3</dd>
            </div>
            <div>
              <dt className="text-xs text-ink-400">Koşul operatörü</dt>
              <dd className="mt-1 text-xl font-semibold">20</dd>
            </div>
            <div>
              <dt className="text-xs text-ink-400">Dönüşüm aksiyonu</dt>
              <dd className="mt-1 text-xl font-semibold">25</dd>
            </div>
          </dl>
        </div>

        <p className="text-xs text-ink-500">
          .NET Core 9 · Next.js 16 · JWT · Entity Framework Core
        </p>
      </aside>

      {/* Sağ taraf — giriş formu */}
      <div className="flex items-center justify-center bg-white px-6 py-12">
        <Suspense fallback={null}>
          <LoginForm />
        </Suspense>
      </div>
    </main>
  );
}
