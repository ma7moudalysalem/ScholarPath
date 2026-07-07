import { useTranslation } from "react-i18next";
import { differenceInCalendarDays } from "date-fns";

/**
 * A "closes in N days" / "Closed" urgency hint for a scholarship deadline.
 *
 * The people who OWN the deadline — the provider on their listing, the admin in
 * moderation — previously saw only a bare numeric date, while students got the
 * urgency signal. This gives everyone the same at-a-glance read. Uses the shared
 * moderation preview keys so the wording (and Arabic plurals) stay in one place.
 */
export function DeadlineHint({ deadline }: { deadline: string }) {
  const { t } = useTranslation(["moderation"]);
  const days = differenceInCalendarDays(new Date(deadline), new Date());
  if (days < 0) {
    return (
      <span className="text-danger-500">
        {t("moderation:scholarshipModeration.preview.closed")}
      </span>
    );
  }
  return (
    <span className={days <= 7 ? "text-warning-600" : "text-text-tertiary"}>
      {t("moderation:scholarshipModeration.preview.closesIn", { count: days })}
    </span>
  );
}
