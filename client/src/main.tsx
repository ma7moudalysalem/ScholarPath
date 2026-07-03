import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter } from "react-router";

import "@/theme/globals.css";
import "@/lib/i18n";
import { queryClient } from "@/lib/queryClient";
import { installStaleChunkRecovery } from "@/lib/staleChunkRecovery";
import { App } from "@/App";

// Self-heal "Failed to fetch dynamically imported module" after a new deploy
// replaces the hashed chunks an already-open tab still references.
installStaleChunkRecovery();

const root = document.getElementById("root");
if (!root) throw new Error("#root container not found");

createRoot(root).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>,
);
