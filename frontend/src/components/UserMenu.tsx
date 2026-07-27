"use client";

import { useRouter } from "next/navigation";
import type { User } from "@/lib/types";
import { Badge, Button } from "./ui";

export function UserMenu({ user }: { user: User | null }) {
  const router = useRouter();

  async function logout() {
    await fetch("/api/auth/logout", { method: "POST" });
    router.replace("/giris");
    router.refresh();
  }

  return (
    <div className="flex items-center gap-3">
      {user && (
        <>
          <div className="hidden text-right sm:block">
            <p className="text-sm leading-tight font-medium text-ink-900">
              {user.fullName ?? user.username}
            </p>
            <p className="text-xs text-ink-500">{user.email}</p>
          </div>
          <Badge tone="brand">{user.role}</Badge>
        </>
      )}
      <Button variant="secondary" size="sm" onClick={logout}>
        Çıkış
      </Button>
    </div>
  );
}
