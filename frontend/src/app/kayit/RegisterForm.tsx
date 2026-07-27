"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { Alert, Button, Field, Input } from "@/components/ui";

export function RegisterForm() {
  const router = useRouter();
  const [form, setForm] = useState({
    fullName: "",
    username: "",
    email: "",
    password: "",
  });
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  function update(key: keyof typeof form) {
    return (event: React.ChangeEvent<HTMLInputElement>) =>
      setForm((prev) => ({ ...prev, [key]: event.target.value }));
  }

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    setBusy(true);

    try {
      const response = await fetch("/api/auth/register", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(form),
      });

      const payload = await response.json();

      if (!response.ok || !payload.success) {
        setError(payload.message ?? "Kayıt başarısız.");
        return;
      }

      router.replace("/panel");
      router.refresh();
    } catch {
      setError("Sunucuya ulaşılamadı. API çalışıyor mu?");
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <h2 className="text-xl font-semibold text-ink-900">Hesap oluşturun</h2>
      <p className="mt-1 text-sm text-ink-500">
        Kayıt sonrası otomatik olarak oturum açılır.
      </p>

      <form onSubmit={handleSubmit} className="mt-6 space-y-4">
        {error && <Alert tone="error">{error}</Alert>}

        <Field label="Ad soyad">
          <Input
            value={form.fullName}
            onChange={update("fullName")}
            placeholder="Ad Soyad"
          />
        </Field>

        <Field label="Kullanıcı adı" hint="En az 3 karakter.">
          <Input
            value={form.username}
            onChange={update("username")}
            required
            minLength={3}
          />
        </Field>

        <Field label="E-posta">
          <Input
            type="email"
            value={form.email}
            onChange={update("email")}
            required
          />
        </Field>

        <Field label="Şifre" hint="En az 6 karakter.">
          <Input
            type="password"
            value={form.password}
            onChange={update("password")}
            required
            minLength={6}
            autoComplete="new-password"
          />
        </Field>

        <Button type="submit" disabled={busy} className="w-full">
          {busy ? "Kaydediliyor…" : "Kayıt ol"}
        </Button>
      </form>

      <p className="mt-6 text-center text-xs text-ink-500">
        Zaten hesabınız var mı?{" "}
        <Link
          href="/giris"
          className="font-medium text-brand-600 hover:underline"
        >
          Giriş yapın
        </Link>
      </p>
    </>
  );
}
