/**
 * Canonical community-flag reasons. Reporters pick one of these keys in the
 * FlagPostDialog; the key (not a localized string) is what we send to the API
 * and store in `ForumFlag.Reason`, so the admin moderation queue can render a
 * consistent localized label regardless of the reporter's language.
 *
 * `flagReasonLabel` falls back to the raw value so legacy / seeded free-text
 * reasons (entered before this dialog existed) still render readably.
 */

export const FLAG_REASONS = [
  "spam",
  "harassment",
  "off-topic",
  "misinformation",
  "inappropriate",
  "other",
] as const;

export type FlagReason = (typeof FLAG_REASONS)[number];

const LABELS_EN: Record<string, string> = {
  spam: "Spam or advertising",
  harassment: "Harassment or abuse",
  "off-topic": "Off-topic or irrelevant",
  misinformation: "Misinformation",
  inappropriate: "Inappropriate content",
  other: "Other",
};

const LABELS_AR: Record<string, string> = {
  spam: "إعلانات أو محتوى مزعج",
  harassment: "تحرّش أو إساءة",
  "off-topic": "خارج الموضوع",
  misinformation: "معلومات مضلّلة",
  inappropriate: "محتوى غير لائق",
  other: "أخرى",
};

/**
 * Localized label for a flag-reason key. Pass the i18n language code.
 * Unknown keys (legacy free-text reasons) fall through to the raw value.
 */
export function flagReasonLabel(reason: string, lang: string | undefined): string {
  const map = lang?.startsWith("ar") ? LABELS_AR : LABELS_EN;
  return map[reason] ?? reason;
}
