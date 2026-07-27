import { NextResponse } from "next/server";
import { BACKEND_URL, getToken } from "@/lib/session";

/**
 * .NET API'sine giden tüm istekler bu vekil (proxy) üzerinden geçer.
 *
 * Faydası:
 *  - JWT HttpOnly çerezde kalır, istemci koduna hiç inmez.
 *  - Backend adresi tarayıcıya sızmaz; CORS yüzeyi tek origin'e iner.
 *  - Yetkisiz/süresi dolmuş oturum tek noktada yakalanır.
 */

type Ctx = { params: Promise<{ path: string[] }> };

async function forward(request: Request, ctx: Ctx) {
  const token = await getToken();
  if (!token) {
    return NextResponse.json(
      { success: false, message: "Oturum bulunamadı." },
      { status: 401 },
    );
  }

  const { path } = await ctx.params;
  const search = new URL(request.url).search;
  const target = `${BACKEND_URL}/api/${path.join("/")}${search}`;

  const headers = new Headers();
  headers.set("Authorization", `Bearer ${token}`);

  // Gövdeyi ham bayt olarak aktardığımız için Content-Type aynen taşınmalıdır.
  // multipart/form-data'da boundary bu başlığın içindedir; düşerse .NET gövdeyi
  // ayrıştıramaz ve "file alanı zorunludur" hatası döner.
  const contentType = request.headers.get("content-type");
  if (contentType) headers.set("Content-Type", contentType);

  const hasBody = request.method !== "GET" && request.method !== "DELETE";

  const upstream = await fetch(target, {
    method: request.method,
    headers,
    body: hasBody ? await request.arrayBuffer() : undefined,
    cache: "no-store",
    // @ts-expect-error - Node fetch, gövdeli isteklerde duplex bekler
    duplex: hasBody ? "half" : undefined,
  });

  // Dosya indirme (CSV export) gibi ikili yanıtları olduğu gibi geçir.
  const upstreamType = upstream.headers.get("content-type") ?? "";
  if (!upstreamType.includes("application/json")) {
    return new NextResponse(upstream.body, {
      status: upstream.status,
      headers: {
        "Content-Type": upstreamType || "application/octet-stream",
        "Content-Disposition":
          upstream.headers.get("content-disposition") ?? "attachment",
      },
    });
  }

  const text = await upstream.text();
  return new NextResponse(text, {
    status: upstream.status,
    headers: { "Content-Type": "application/json; charset=utf-8" },
  });
}

export const GET = forward;
export const POST = forward;
export const PUT = forward;
export const DELETE = forward;
export const PATCH = forward;
