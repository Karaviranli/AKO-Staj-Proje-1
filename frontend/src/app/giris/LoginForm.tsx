"use client";

import { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { Alert, Button, Field, Input } from "@/components/ui";

export function LoginForm() {
  const router = useRouter();
  const params = useSearchParams();

  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);

    try {
      const response = await fetch("/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ username, password }),
      });

      const payload = await response.json();

      if (!response.ok || !payload.success) {
        setError(payload.message ?? "Giriş başarısız.");
        return;
      }

      // Middleware'in yeni çerezi görebilmesi için tam yenileme yapılır.
      router.replace(params.get("devam") ?? "/panel");
      router.refresh();
    } catch {
      setError("Sunucuya ulaşılamadı. API çalışıyor mu?");
    } finally {
      setBusy(false);
    }
  }

  function fillDemo() {
    setUsername("admin");
    setPassword("Demo1234!");
    setError(null);
  }

  return (
    <div className="w-full max-w-sm">
      <div className="mb-8 lg:hidden">
        <span className="grid size-9 place-items-center rounded-lg bg-brand-600 text-sm font-bold text-white">
          DF
        </span>
      </div>

      <h2 className="text-xl font-semibold text-ink-900">Oturum açın</h2>
      <p className="mt-1 text-sm text-ink-500">
        Devam etmek için hesap bilgilerinizi girin.
      </p>

      <form onSubmit={handleSubmit} className="mt-7 space-y-4">
        {error && <Alert tone="error">{error}</Alert>}

        <Field label="Kullanıcı adı veya e-posta">
          <Input
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            placeholder="admin"
            autoComplete="username"
            required
            autoFocus
          />
        </Field>

        <Field label="Şifre">
          <Input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="••••••••"
            autoComplete="current-password"
            required
          />
        </Field>

        <Button type="submit" disabled={busy} className="w-full">
          {busy ? "Giriş yapılıyor…" : "Giriş yap"}
        </Button>
      </form>

      <div className="mt-6 rounded-lg border border-ink-200 bg-ink-50 px-3.5 py-3">
        <p className="text-xs font-medium text-ink-600">Demo hesabı</p>
        <p className="mt-0.5 font-mono text-xs text-ink-500">
          admin / Demo1234!
        </p>
        <button
          type="button"
          onClick={fillDemo}
          className="mt-2 text-xs font-medium text-brand-600 hover:text-brand-700 hover:underline"
        >
          Formu demo bilgileriyle doldur
        </button>
      </div>

      <p className="mt-6 text-center text-xs text-ink-500">
        Hesabınız yok mu?{" "}
        <Link
          href="/kayit"
          className="font-medium text-brand-600 hover:underline"
        >
          Kayıt olun
        </Link>
      </p>
    </div>
  );
}
