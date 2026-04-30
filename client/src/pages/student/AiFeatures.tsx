import { useTranslation } from "react-i18next";
import { AiRecommendations } from "@/components/ai/AiRecommendations";
import { Chatbot } from "@/components/ai/Chatbot";

export function AiFeatures() {
  const { t } = useTranslation(["ai"]);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">{t("ai:title")}</h1>
        <p className="mt-1 max-w-2xl text-sm text-text-secondary">{t("ai:subtitle")}</p>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <AiRecommendations />
        <Chatbot />
      </div>
    </div>
  );
}
