import type { ReactNode } from "react";
import { Link } from "react-router";
import { useTranslation } from "react-i18next";
import { motion } from "motion/react";
import { Check, GraduationCap } from "lucide-react";
import { authApi } from "@/services/api/auth";

// Shared chrome for split-screen auth pages (Login + Register).
// Left brand panel renders only on lg+ screens; right form panel is always
// visible. RTL is handled with logical properties (start/end, ms/me).

export function AuthShell({ children }: { children: ReactNode }) {
  return (
    <div className="relative min-h-[calc(100vh-3.5rem)] bg-bg-canvas">
      <div className="grid min-h-[calc(100vh-3.5rem)] lg:grid-cols-[1.2fr_1fr]">
        <BrandPanel />
        <FormPanel>{children}</FormPanel>
      </div>
    </div>
  );
}

export function BrandPanel() {
  const { t } = useTranslation(["auth", "common"]);
  const features = [
    t("auth:brand.feature1"),
    t("auth:brand.feature2"),
    t("auth:brand.feature3"),
  ];

  return (
    <div className="relative hidden overflow-hidden bg-gradient-to-br from-brand-600 via-brand-700 to-brand-800 text-white lg:flex lg:flex-col lg:justify-between lg:p-12 xl:p-16">
      {/* Animated decorative orbs */}
      <div
        aria-hidden
        className="pointer-events-none absolute -top-24 -start-24 size-80 rounded-full bg-brand-400/30 blur-3xl"
        style={{ animation: "auth-orb-float 14s ease-in-out infinite" }}
      />
      <div
        aria-hidden
        className="pointer-events-none absolute -bottom-32 -end-24 size-96 rounded-full bg-brand-300/20 blur-3xl"
        style={{ animation: "auth-orb-float 18s ease-in-out infinite reverse" }}
      />
      <div
        aria-hidden
        className="pointer-events-none absolute top-1/3 end-1/4 size-64 rounded-full bg-white/10 blur-3xl"
        style={{ animation: "auth-orb-float 22s ease-in-out infinite" }}
      />

      {/* Dot pattern overlay */}
      <div
        aria-hidden
        className="pointer-events-none absolute inset-0 opacity-25"
        style={{
          backgroundImage:
            "radial-gradient(circle, rgba(255,255,255,0.18) 1px, transparent 1px)",
          backgroundSize: "24px 24px",
        }}
      />

      <style>{`
        @keyframes auth-orb-float {
          0%, 100% { transform: translate(0, 0) scale(1); }
          33% { transform: translate(20px, -30px) scale(1.08); }
          66% { transform: translate(-20px, 20px) scale(0.95); }
        }
      `}</style>

      <div className="relative">
        <Link
          to="/"
          className="inline-flex items-center gap-2.5 text-base font-semibold text-white"
        >
          <span className="flex size-9 items-center justify-center rounded-xl bg-white/15 backdrop-blur-md ring-1 ring-white/20">
            <GraduationCap aria-hidden className="size-5" />
          </span>
          {t("common:appName")}
        </Link>
      </div>

      <div className="relative max-w-lg">
        <motion.h2
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5, ease: [0.22, 1, 0.36, 1], delay: 0.1 }}
          className="font-display text-4xl font-bold leading-[1.1] tracking-tight text-white xl:text-5xl"
        >
          {t("auth:brand.tagline")}
        </motion.h2>
        <motion.p
          initial={{ opacity: 0, y: 16 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.5, ease: [0.22, 1, 0.36, 1], delay: 0.18 }}
          className="mt-5 text-base leading-relaxed text-white/80 xl:text-lg"
        >
          {t("auth:brand.subtitle")}
        </motion.p>

        <ul className="mt-10 space-y-4">
          {features.map((feature, idx) => (
            <motion.li
              key={feature}
              initial={{ opacity: 0, x: -12 }}
              animate={{ opacity: 1, x: 0 }}
              transition={{
                duration: 0.4,
                ease: [0.22, 1, 0.36, 1],
                delay: 0.26 + idx * 0.07,
              }}
              className="flex items-start gap-3 text-sm text-white/90 xl:text-base"
            >
              <span className="mt-0.5 flex size-6 shrink-0 items-center justify-center rounded-full bg-white/15 ring-1 ring-white/25">
                <Check aria-hidden className="size-3.5 text-white" />
              </span>
              <span>{feature}</span>
            </motion.li>
          ))}
        </ul>
      </div>

      <motion.div
        initial={{ opacity: 0, y: 16 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.5, ease: [0.22, 1, 0.36, 1], delay: 0.5 }}
        className="relative"
      >
        <figure className="rounded-2xl bg-white/10 p-6 backdrop-blur-md ring-1 ring-white/15">
          <blockquote className="text-sm leading-relaxed text-white/95 xl:text-base">
            &ldquo;{t("auth:brand.testimonialQuote")}&rdquo;
          </blockquote>
          <figcaption className="mt-5 flex items-center gap-3">
            <span
              aria-hidden
              className="flex size-10 items-center justify-center rounded-full bg-gradient-to-br from-white/40 to-white/10 text-sm font-semibold text-white ring-1 ring-white/20"
            >
              {t("auth:brand.testimonialName").slice(0, 1)}
            </span>
            <div className="text-sm">
              <div className="font-semibold text-white">
                {t("auth:brand.testimonialName")}
              </div>
              <div className="text-white/70">
                {t("auth:brand.testimonialRole")}
              </div>
            </div>
          </figcaption>
        </figure>
      </motion.div>
    </div>
  );
}

function FormPanel({ children }: { children: ReactNode }) {
  return (
    <div className="flex items-center justify-center px-6 py-12 sm:px-10 lg:px-12 xl:px-16">
      {children}
    </div>
  );
}

// ─── Mobile-only logo (shown above form on small screens) ─────────────

export function MobileBrandMark() {
  const { t } = useTranslation("common");
  return (
    <Link
      to="/"
      className="mb-8 inline-flex items-center gap-2 text-sm font-semibold text-text-primary lg:hidden"
    >
      <span className="flex size-8 items-center justify-center rounded-xl bg-gradient-to-br from-brand-500 to-brand-700 text-white shadow-brand">
        <GraduationCap aria-hidden className="size-4" />
      </span>
      {t("appName")}
    </Link>
  );
}

// ─── Reusable form pieces ─────────────────────────────────────────────

export function Separator({ text }: { text: string }) {
  return (
    <div className="my-7 flex items-center gap-3 text-xs font-medium uppercase tracking-wider text-text-tertiary">
      <div className="h-px flex-1 bg-border-subtle" />
      {text}
      <div className="h-px flex-1 bg-border-subtle" />
    </div>
  );
}

export function SsoButtons() {
  const { t } = useTranslation("auth");
  return (
    <div className="space-y-2.5">
      <button
        type="button"
        onClick={() => authApi.beginSso("google")}
        className="inline-flex h-12 w-full items-center justify-center gap-3 rounded-xl border border-border-default bg-bg-elevated px-4 text-sm font-medium text-text-primary transition hover:border-border-strong hover:bg-bg-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500/30"
      >
        <GoogleIcon />
        {t("sso.google")}
      </button>
      <button
        type="button"
        onClick={() => authApi.beginSso("microsoft")}
        className="inline-flex h-12 w-full items-center justify-center gap-3 rounded-xl border border-border-default bg-bg-elevated px-4 text-sm font-medium text-text-primary transition hover:border-border-strong hover:bg-bg-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500/30"
      >
        <MicrosoftIcon />
        {t("sso.microsoft")}
      </button>
    </div>
  );
}

function GoogleIcon() {
  return (
    <svg viewBox="0 0 24 24" className="size-4 shrink-0" aria-hidden>
      <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4" />
      <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853" />
      <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l3.66-2.84z" fill="#FBBC05" />
      <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" fill="#EA4335" />
    </svg>
  );
}

function MicrosoftIcon() {
  return (
    <svg viewBox="0 0 24 24" className="size-4 shrink-0" aria-hidden>
      <path d="M11.4 2H2v9.4h9.4V2z" fill="#F35325" />
      <path d="M22 2h-9.4v9.4H22V2z" fill="#81BC06" />
      <path d="M11.4 12.6H2V22h9.4v-9.4z" fill="#05A6F0" />
      <path d="M22 12.6h-9.4V22H22v-9.4z" fill="#FFBA08" />
    </svg>
  );
}

// ─── Form input ───────────────────────────────────────────────────────

export function AuthField({
  id,
  label,
  type,
  placeholder,
  autoComplete,
  hint,
  error,
  inputProps,
}: {
  id: string;
  label: string;
  type: string;
  placeholder?: string;
  autoComplete?: string;
  hint?: string;
  error?: string;
  inputProps: React.InputHTMLAttributes<HTMLInputElement>;
}) {
  return (
    <div className="space-y-1.5">
      <label
        htmlFor={id}
        className="text-sm font-medium text-text-primary"
      >
        {label}
      </label>
      <input
        id={id}
        type={type}
        placeholder={placeholder}
        autoComplete={autoComplete}
        aria-invalid={error ? "true" : "false"}
        className={[
          "block w-full rounded-xl border bg-bg-elevated px-4 py-3 text-sm text-text-primary placeholder:text-text-tertiary",
          "transition focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20",
          error ? "border-danger-400" : "border-border-default",
        ].join(" ")}
        {...inputProps}
      />
      {hint && !error && (
        <p className="text-xs text-text-tertiary">{hint}</p>
      )}
      {error && (
        <p role="alert" className="text-xs text-danger-500">
          {error}
        </p>
      )}
    </div>
  );
}
