import { useEffect } from "react";
import { useTranslation } from "react-i18next";
import { Toaster } from "sonner";

import { AppRouter } from "@/routes/router";
import { getDirection } from "@/lib/i18n";

export function App() {
  const { i18n } = useTranslation();

  useEffect(() => {
    const dir = getDirection(i18n.language);
    document.documentElement.setAttribute("dir", dir);
    document.documentElement.setAttribute("lang", i18n.language);
  }, [i18n.language]);

  return (
    <>
      <AppRouter />
      <Toaster
        richColors
        position={getDirection(i18n.language) === "rtl" ? "top-left" : "top-right"}
      />
    </>
  );
}
