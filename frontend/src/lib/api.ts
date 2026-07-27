import type { ApiResponse } from "./types";

/**
 * İstemci tarafı API istemcisi. Tüm istekler Next.js vekiline gider;
 * token'ı eklemek vekilin işidir, burada hiç token görülmez.
 */

const BASE = "/api/backend";

export class ApiError extends Error {
  status: number;
  constructor(message: string, status: number) {
    super(message);
    this.status = status;
  }
}

async function handle<T>(response: Response): Promise<T> {
  if (response.status === 401 && typeof window !== "undefined") {
    window.location.href = "/giris";
    throw new ApiError("Oturum süresi doldu.", 401);
  }

  let payload: ApiResponse<T>;
  try {
    payload = await response.json();
  } catch {
    throw new ApiError("Sunucudan geçersiz yanıt alındı.", response.status);
  }

  if (!response.ok || !payload.success) {
    throw new ApiError(payload.message ?? "İşlem başarısız.", response.status);
  }

  return payload.data as T;
}

export const api = {
  get: <T>(path: string) =>
    fetch(`${BASE}${path}`, { cache: "no-store" }).then(handle<T>),

  post: <T>(path: string, body: unknown) =>
    fetch(`${BASE}${path}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    }).then(handle<T>),

  delete: <T>(path: string) =>
    fetch(`${BASE}${path}`, { method: "DELETE" }).then(handle<T>),

  upload: <T>(path: string, file: File) => {
    const form = new FormData();
    form.append("file", file);
    return fetch(`${BASE}${path}`, { method: "POST", body: form }).then(handle<T>);
  },

  /** CSV indirme — JSON değil, ikili yanıt döner. */
  download: async (path: string, fileName: string) => {
    const response = await fetch(`${BASE}${path}`);
    if (!response.ok) throw new ApiError("Dosya indirilemedi.", response.status);

    const blob = await response.blob();
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
  },
};
