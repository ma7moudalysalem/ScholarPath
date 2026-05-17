/// <reference types="vitest/config" />
import path from "node:path";
import { defineConfig, loadEnv, type PluginOption } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

// `command` is "build" for `vite build` and "serve" for the dev server — it is
// the reliable production signal. `mode` is only a label (and can be passed as
// `--mode staging`), so it must NOT decide whether React is built in
// production: @vitejs/plugin-react picks the dev JSX runtime + React Refresh
// whenever the build is not production, which is what shipped the dev-mode
// React banner to the deployed site.
export default defineConfig(({ command, mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const apiBase = env.VITE_API_BASE_URL ?? "http://localhost:5000";
  const isProduction = command === "build";

  return {
    plugins: [react() as PluginOption, tailwindcss() as PluginOption],
    // Force the production value of NODE_ENV into the bundle for `vite build`,
    // regardless of the env mode label or any ambient NODE_ENV. This keeps
    // React (and every `process.env.NODE_ENV` guard) on the production path.
    define: {
      "process.env.NODE_ENV": JSON.stringify(
        isProduction ? "production" : "development",
      ),
    },
    resolve: {
      alias: {
        "@": path.resolve(__dirname, "./src"),
      },
    },
    server: {
      port: 5173,
      strictPort: false,
      proxy: {
        "/api": { target: apiBase, changeOrigin: true, secure: false },
        "/hubs": { target: apiBase, changeOrigin: true, ws: true, secure: false },
      },
    },
    build: {
      outDir: "dist",
      // Sourcemaps only outside production: a deployed bundle should not ship
      // .map files or a //# sourceMappingURL trailer.
      sourcemap: !isProduction,
      target: "es2022",
      // Minification is left at Vite's default minifier so no extra
      // dependency is required; `vite build` minifies for production.
      minify: true,
    },
    test: {
      environment: "happy-dom",
      globals: true,
      setupFiles: ["./src/test/setup.ts"],
      exclude: ["**/node_modules/**", "**/dist/**", "**/test/e2e/**"],
    },
  };
});
