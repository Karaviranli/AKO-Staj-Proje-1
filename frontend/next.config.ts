import path from "node:path";
import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Monorepo kökünde de lockfile bulunduğu için Turbopack'in çalışma kökünü
  // açıkça bu klasöre sabitliyoruz.
  turbopack: {
    root: path.resolve(__dirname),
  },
};

export default nextConfig;
