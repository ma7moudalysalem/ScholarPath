import { useTranslation } from "react-i18next";
import { Info } from "lucide-react";

interface Props {
  text?: string;
  className?: string;
}

export function AiDisclaimer({ text, className }: Props) {
  const { t } = useTranslation(["ai"]);
  const body = text ?? t("ai:disclaimer");
  return (
    <div
      className={`inline-flex items-start gap-2 rounded-md border border-border-subtle bg-bg-subtle/60 px-3 py-2 text-xs text-text-secondary ${className ?? ""}`}
    >
      <Info aria-hidden className="mt-0.5 size-3.5 flex-shrink-0" />
      <span>{body}</span>
    </div>
  );
}
