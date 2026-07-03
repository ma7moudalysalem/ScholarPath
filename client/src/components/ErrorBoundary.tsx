import { Component, type ErrorInfo, type ReactNode } from "react";
import { useTranslation } from "react-i18next";
import { AlertTriangle, RotateCcw } from "lucide-react";
import { isChunkLoadError, recoverFromStaleChunk } from "@/lib/staleChunkRecovery";

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
  // A stale lazy chunk after a deploy — self-heals with a one-shot reload
  // rather than showing the crash screen.
  recovering: boolean;
}

/**
 * React error boundary that catches unhandled rendering errors and shows a
 * friendly fallback instead of a blank screen.  Logs the component stack in
 * development so engineers can pinpoint the broken subtree.
 *
 * Usage (wrap the router or individual page sections):
 *   <ErrorBoundary>
 *     <AppRouter />
 *   </ErrorBoundary>
 */
export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null, recovering: false };
  }

  static getDerivedStateFromError(error: Error): State {
    // A failed lazy-route chunk (stale after a deploy) is a recoverable render
    // error — show a neutral "updating" state, not the crash screen.
    return { hasError: true, error, recovering: isChunkLoadError(error) };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    if (isChunkLoadError(error)) {
      // A new deploy replaced the chunk this tab tried to lazy-load. Reload once
      // to pull the fresh assets instead of stranding the user on an error page.
      recoverFromStaleChunk();
      return;
    }
    // Print to console in every environment — visible in Playwright traces and
    // Azure App Service log streams without any external SDK dependency.
    console.error("[ErrorBoundary] Uncaught rendering error:", error, info.componentStack);
  }

  handleReload = () => {
    window.location.reload();
  };

  render() {
    if (this.state.hasError) {
      // While a stale-chunk reload is in flight, avoid flashing the crash screen.
      if (this.state.recovering) {
        return <div className="min-h-dvh bg-bg-canvas" aria-busy="true" />;
      }
      return <ErrorFallback onReload={this.handleReload} />;
    }
    return this.props.children;
  }
}

function ErrorFallback({ onReload }: { onReload: () => void }) {
  const { t } = useTranslation("errors");

  return (
    <div className="flex min-h-dvh flex-col items-center justify-center gap-6 bg-bg-canvas px-4 text-center">
      <div className="flex size-16 items-center justify-center rounded-full bg-danger-subtle">
        <AlertTriangle className="size-8 text-danger-emphasis" aria-hidden />
      </div>

      <div className="space-y-2">
        <h1 className="text-xl font-semibold text-text-primary">
          {t("boundary.title")}
        </h1>
        <p className="max-w-md text-sm text-text-secondary">
          {t("boundary.description")}
        </p>
      </div>

      <button
        onClick={onReload}
        className="inline-flex items-center gap-2 rounded-lg bg-brand-500 px-4 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:ring-offset-2"
      >
        <RotateCcw className="size-4" aria-hidden />
        {t("boundary.reload")}
      </button>
    </div>
  );
}
