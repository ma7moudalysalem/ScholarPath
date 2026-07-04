import { useTranslation } from "react-i18next";
import { motion, AnimatePresence } from "framer-motion";
import type { StudentApplicationRow, ApplicationStatus } from "@/services/api/applications";
import { ApplicationCard } from "./ApplicationCard";
import { cn } from "@/lib/utils";

interface KanbanBoardProps {
  applications: StudentApplicationRow[];
  onStatusChange: (id: string, newStatus: ApplicationStatus) => void;
}

/**
 * Logical columns rendered on the board. We surface 5 buckets, but each
 * bucket aggregates ALL related backend statuses so no application ever
 * vanishes between the KPI tiles and the kanban (which used to happen
 * when a row was `Draft`, `Pending`, `Shortlisted`, `Withdrawn`, or
 * `WaitingResult` — none of those had a column).
 *
 * Drop-target status is the one we write back to the server when the
 * student drags a card into the column.
 */
const COLUMNS: {
  key: ApplicationStatus;
  members: readonly ApplicationStatus[];
}[] = [
  { key: "Intending",   members: ["Intending", "Draft"] },
  { key: "Applied",     members: ["Applied", "Pending"] },
  { key: "UnderReview", members: ["UnderReview", "Shortlisted", "WaitingResult"] },
  { key: "Accepted",    members: ["Accepted"] },
  { key: "Rejected",    members: ["Rejected", "Withdrawn"] },
];

// Only EXTERNAL trackers are draggable, and FR-APP-33/34 limits them to the
// three self-tracking states. Map each column to the external status it writes
// back — the "In Review" column represents "waiting for the result", so it maps
// to WaitingResult (which also makes that status reachable). Columns with no
// entry (Accepted / Rejected — in-app provider decisions) reject the drop.
const EXTERNAL_DROP_STATUS: Partial<Record<ApplicationStatus, ApplicationStatus>> = {
  Intending: "Intending",
  Applied: "Applied",
  UnderReview: "WaitingResult",
};

export function KanbanBoard({ applications, onStatusChange }: KanbanBoardProps) {
  const { t } = useTranslation("applications");

  return (
    <div className="flex h-full w-full space-x-6 overflow-x-auto pb-6 scrollbar-hide">
      {COLUMNS.map(({ key: status, members }) => {
        const columnApps = applications.filter((app) => members.includes(app.status));

        return (
          <div
            key={status}
            className="flex w-80 shrink-0 flex-col space-y-4"
          >
            <div className="flex items-center justify-between px-1">
              <h3 className="text-sm font-bold text-text-primary uppercase tracking-wider">
                {t(`kanban.columns.${status}`)}
              </h3>
              <span className="rounded-full bg-bg-subtle px-2 py-0.5 text-xs font-medium text-text-secondary">
                {columnApps.length}
              </span>
            </div>

            <div
              className={cn(
                "flex min-h-[500px] flex-col space-y-3 rounded-xl bg-bg-subtle/50 p-3 transition-colors",
                "border border-transparent",
                "hover:border-brand-200/50 dark:hover:border-brand-800/30"
              )}
              onDragOver={(e: React.DragEvent) => e.preventDefault()}
              onDrop={(e: React.DragEvent) => {
                const id = e.dataTransfer.getData("applicationId");
                const target = EXTERNAL_DROP_STATUS[status];
                // No mapping = an in-app-only column (Accepted/Rejected): reject
                // the drop rather than send a status the server will 409 on.
                if (id && target) onStatusChange(id, target);
              }}
            >
              <AnimatePresence mode="popLayout">
                {columnApps.map((app) => {
                  // Only external trackers (self-managed) can move across
                  // columns. In-app applications follow the platform's own
                  // submit / withdraw workflow — the server rejects a manual
                  // PATCH with 409 ("Only external applications can be
                  // manually updated"). Making the in-app cards undraggable
                  // is the honest UI signal.
                  const isExternal = app.mode === "External";
                  return (
                    <motion.div
                      key={app.applicationId}
                      layout
                      initial={{ opacity: 0, y: 10 }}
                      animate={{ opacity: 1, y: 0 }}
                      exit={{ opacity: 0, scale: 0.95 }}
                      draggable={isExternal}
                      onDragStartCapture={(e: React.DragEvent) => {
                        if (!isExternal) {
                          e.preventDefault();
                          return;
                        }
                        e.dataTransfer.setData("applicationId", app.applicationId);
                      }}
                      title={isExternal ? undefined : t("kanban.inAppNotDraggable")}
                      className={isExternal ? "cursor-grab active:cursor-grabbing" : "cursor-default"}
                    >
                      <ApplicationCard application={app} />
                    </motion.div>
                  );
                })}
              </AnimatePresence>

              {columnApps.length === 0 && (
                <div className="flex flex-1 flex-col items-center justify-center border-2 border-dashed border-border-subtle rounded-xl p-8 text-center text-text-tertiary">
                  <p className="text-xs">{t("kanban.emptyColumn")}</p>
                </div>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}
