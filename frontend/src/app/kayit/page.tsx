import { RegisterForm } from "./RegisterForm";

export const metadata = { title: "Kayıt — DataFlow" };

export default function RegisterPage() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-ink-100 px-6 py-12">
      <div className="w-full max-w-md rounded-xl border border-ink-200 bg-white p-8 shadow-sm">
        <div className="mb-6 flex items-center gap-2.5">
          <span className="grid size-9 place-items-center rounded-lg bg-brand-600 text-sm font-bold text-white">
            DF
          </span>
          <span className="text-lg font-semibold tracking-tight text-ink-900">
            DataFlow
          </span>
        </div>
        <RegisterForm />
      </div>
    </main>
  );
}
