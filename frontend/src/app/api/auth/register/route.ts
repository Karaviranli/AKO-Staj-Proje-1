import { NextResponse } from "next/server";
import {
  BACKEND_URL,
  TOKEN_COOKIE,
  USER_COOKIE,
  buildCookieOptions,
} from "@/lib/session";
import type { ApiResponse, AuthResponse } from "@/lib/types";

export async function POST(request: Request) {
  const body = await request.json();

  const upstream = await fetch(`${BACKEND_URL}/api/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
    cache: "no-store",
  });

  const payload: ApiResponse<AuthResponse> = await upstream.json();

  if (!upstream.ok || !payload.success || !payload.data) {
    return NextResponse.json(
      { success: false, message: payload.message ?? "Kayıt başarısız." },
      { status: upstream.status || 400 },
    );
  }

  const { token, expiresIn, user } = payload.data;
  const response = NextResponse.json({ success: true, user });

  response.cookies.set(TOKEN_COOKIE, token, buildCookieOptions(expiresIn));
  response.cookies.set(USER_COOKIE, encodeURIComponent(JSON.stringify(user)), {
    ...buildCookieOptions(expiresIn),
    httpOnly: false,
  });

  return response;
}
