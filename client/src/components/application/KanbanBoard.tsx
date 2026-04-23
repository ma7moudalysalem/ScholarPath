import { useTranslation } from "react-i18next";
import { motion, AnimatePresence } from "framer-motion";
import type { StudentApplicationRow, ApplicationStatus } from "@/services/api/applications";
import { ApplicationCard } from "./ApplicationCard";
import { cn } from "@/lib/utils";

interface KanbanBoardProps {
  applications: StudentApplicationRow[];
  onStatusChange: (id: string, newStatus: ApplicationStatus) => void;
}

const COLUMNS: ApplicationStatus[] = [
  "Intending",
  "Applied",
  "UnderReview",
  "Accepted",
  "Rejected",
];

export function KanbanBoard({ applications, onStatusChange }: KanbanBoardProps) {
  const { t } = useTranslation("applications");

  return (
    <div className="flex h-full w-full space-x-6 overflow-x-auto pb-6 scrollbar-hide">
      {COLUMNS.map((status) => {
        const columnApps = applications.filter((app) => app.status === status);

        return (
          <div
            key={status}
            className="flex w-80 shrink-0 flex-col space-y-4"
          >
            <div className="flex items-center justify-between px-1">
              <h3 className="text-sm font-bold text-text-primary uppercase tracking-wider">
                {t(`kanban.columns.${status}`)}
              </h3>
              <span className="rounded-full bg-bg-subtle px-2 py-0.5 text-xs font-medium text-text-secondary dark:bg-slate-800">
                {columnApps.length}
              </span>
            </div>

            <div
              className={cn(
                "flex min-h-[500px] flex-col space-y-3 rounded-xl bg-bg-subtle/50 p-3 transition-colors",
                "dark:bg-slate-900/40 border border-transparent",
                "hover:border-brand-200/50 dark:hover:border-brand-800/30"
              )}
              onDragOver={(e: React.DragEvent) => e.preventDefault()}
              onDrop={(e: React.DragEvent) => {
                const id = e.dataTransfer.getData("applicationId");
                if (id) onStatusChange(id, status);
              }}
            >
              <AnimatePresence mode="popLayout">
                {columnApps.map((app) => (
                  <motion.div
                    key={app.applicationId}
                    layout
                    initial={{ opacity: 0, y: 10 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0, scale: 0.95 }}
                    draggable
                    onDragStartCapture={(e: React.DragEvent) => {
                      e.dataTransfer.setData("applicationId", app.applicationId);
                    }}
                  >
                    <ApplicationCard application={app} />
                  </motion.div>
                ))}
              </AnimatePresence>

              {columnApps.length === 0 && (
                <div className="flex flex-1 flex-col items-center justify-center border-2 border-dashed border-border-subtle rounded-xl p-8 text-center text-text-tertiary dark:border-slate-800">
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
