import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import { TOKEN_COOKIE } from "@/lib/session";

/**
 * Route Protection — Next.js 16'da "proxy" olarak adlandırılan istek ara katmanı.
 * Korumalı sayfalar sunucuya ulaşmadan önce burada denetlenir; oturumu olmayan
 * kullanıcı panel HTML'ini hiçbir zaman indiremez.
 */

const PROTECTED = ["/panel"];
const GUEST_ONLY = ["/giris", "/kayit"];

export default function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const hasSession = Boolean(request.cookies.get(TOKEN_COOKIE)?.value);

  if (PROTECTED.some((p) => pathname.startsWith(p)) && !hasSession) {
    const url = request.nextUrl.clone();
    url.pathname = "/giris";
    // Girişten sonra kullanıcıyı gitmek istediği sayfaya geri götür.
    url.searchParams.set("devam", pathname);
    return NextResponse.redirect(url);
  }

  if (GUEST_ONLY.some((p) => pathname.startsWith(p)) && hasSession) {
    const url = request.nextUrl.clone();
    url.pathname = "/panel";
    url.search = "";
    return NextResponse.redirect(url);
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/panel/:path*", "/giris", "/kayit"],
};
