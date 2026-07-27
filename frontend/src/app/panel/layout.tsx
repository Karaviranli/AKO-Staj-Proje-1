import { getSessionUser } from "@/lib/session";
import { Sidebar } from "@/components/Sidebar";
import { UserMenu } from "@/components/UserMenu";

export default async function PanelLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  // Oturum bilgisi sunucuda okunur; middleware zaten yetkisiz erişimi engelledi.
  const user = await getSessionUser();

  return (
    <div className="flex min-h-screen">
      <Sidebar />

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="df-no-print sticky top-0 z-20 flex h-14 items-center justify-between border-b border-ink-200 bg-white/90 px-6 backdrop-blur">
          <p className="text-sm font-medium text-ink-500">
            Veri Dönüşüm ve Kural Motoru
          </p>
          <UserMenu user={user} />
        </header>

        <main className="min-w-0 flex-1 px-6 py-6">{children}</main>
      </div>
    </div>
  );
}
