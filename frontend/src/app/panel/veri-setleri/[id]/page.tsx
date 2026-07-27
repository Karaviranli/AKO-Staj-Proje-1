import { Workspace } from "./Workspace";

export const metadata = { title: "Kural Stüdyosu — DataFlow" };

export default async function DatasetPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  return <Workspace fileId={Number(id)} />;
}
