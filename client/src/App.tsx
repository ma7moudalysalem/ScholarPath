import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Toaster } from "sonner";

import { AppRouter } from "@/routes/router";
import { getDirection } from "@/lib/i18n";

/** Watches html[data-theme] and returns "dark" | "light" */
function useHtmlTheme(): "dark" | "light" {
  const [theme, setTheme] = useState<"dark" | "light">(() => {
    return (document.documentElement.getAttribute("data-theme") as "dark" | "light") ?? "light";
  });

  useEffect(() => {
    const observer = new MutationObserver(() => {
      const t = document.documentElement.getAttribute("data-theme");
      setTheme(t === "dark" ? "dark" : "light");
    });
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ["data-theme"] });
    return () => observer.disconnect();
  }, []);

  return theme;
}

export function App() {
  const { i18n } = useTranslation();
  const theme = useHtmlTheme();

  useEffect(() => {
    const dir = getDirection(i18n.language);
    document.documentElement.setAttribute("dir", dir);
    document.documentElement.setAttribute("lang", i18n.language);
  }, [i18n.language]);

  return (
    <>
      <AppRouter />
      <Toaster
        theme={theme}
        position={getDirection(i18n.language) === "rtl" ? "top-left" : "top-right"}
        gap={8}
        toastOptions={{
          classNames: {
            toast: [
              "group !rounded-xl !border !shadow-lg !font-sans !text-sm",
              "!bg-bg-elevated !border-border-subtle !text-text-primary",
            ].join(" "),
            title:       "!font-semibold !text-text-primary",
            description: "!text-text-secondary",
            closeButton: [
              "!rounded-lg !border !border-border-subtle",
              "!bg-bg-canvas !text-text-tertiary",
              "hover:!bg-bg-subtle hover:!text-text-primary",
            ].join(" "),
            // Semantic variants — keep Sonner's bg but override radius/border
            success: "!border-success-200",
            error:   "!border-danger-200",
            warning: "!border-warning-500/30",
            info:    "!border-brand-200",
            actionButton: [
              "!rounded-lg !bg-brand-500 !text-white !text-xs !font-semibold",
              "hover:!bg-brand-600",
            ].join(" "),
            cancelButton: [
              "!rounded-lg !bg-bg-subtle !text-text-secondary !text-xs",
              "hover:!bg-bg-muted",
            ].join(" "),
          },
        }}
      />
    </>
  );
}
