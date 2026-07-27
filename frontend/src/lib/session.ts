import { cookies } from "next/headers";
import type { User } from "./types";

/**
 * Oturum yönetimi — yalnızca sunucu tarafında çalışır.
 *
 * JWT, HttpOnly çerezde tutulur: tarayıcıdaki hiçbir JavaScript kodu
 * (dolayısıyla XSS ile enjekte edilen kod da) token'a erişemez.
 */

export const TOKEN_COOKIE = "df_token";
export const USER_COOKIE = "df_user";

// Windows'ta 'localhost' Node fetch tarafından ::1 olarak çözülüp backend'e
// bağlanamayabiliyor; varsayılanı 127.0.0.1 tutuyoruz.
export const BACKEND_URL = process.env.BACKEND_URL ?? "http://127.0.0.1:5080";

export async function getToken(): Promise<string | null> {
  const store = await cookies();
  return store.get(TOKEN_COOKIE)?.value ?? null;
}

export async function getSessionUser(): Promise<User | null> {
  const store = await cookies();
  const raw = store.get(USER_COOKIE)?.value;
  if (!raw) return null;

  try {
    return JSON.parse(decodeURIComponent(raw)) as User;
  } catch {
    return null;
  }
}

export function buildCookieOptions(maxAge: number) {
  return {
    httpOnly: true,
    // Geliştirmede http kullanıldığı için secure yalnızca üretimde açılır.
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax" as const,
    path: "/",
    maxAge,
  };
}
