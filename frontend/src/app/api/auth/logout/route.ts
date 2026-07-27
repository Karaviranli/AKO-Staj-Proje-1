import { NextResponse } from "next/server";
import { TOKEN_COOKIE, USER_COOKIE } from "@/lib/session";

export async function POST() {
  const response = NextResponse.json({ success: true });
  response.cookies.delete(TOKEN_COOKIE);
  response.cookies.delete(USER_COOKIE);
  return response;
}
