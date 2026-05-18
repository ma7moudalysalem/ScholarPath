# ScholarPath Design System

> Single source of truth for colours, typography, spacing, and motion tokens.
> All tokens are defined in `client/src/theme/globals.css` under `@theme {}`.
> Tailwind v4 exposes every token as a utility class automatically — no config file needed.

---

## Colour Tokens

### Backgrounds

| Token | Light | Dark | Usage |
|-------|-------|------|-------|
| `bg-bg-canvas` | `#ffffff` | `#000000` | Root page background |
| `bg-bg-subtle` | `#f5f5f7` | `#0b0b0f` | Section/sidebar background |
| `bg-bg-elevated` | `#ffffff` | `#1f2937` | Cards, modals, inputs |
| `bg-bg-muted` | `#fafafa` | `#111827` | Inner card areas, data grids |

### Text

| Token | Light | Dark | Usage |
|-------|-------|------|-------|
| `text-text-primary` | `#1d1d1f` | `#f5f5f7` | Headings, body, labels |
| `text-text-secondary` | `#4b5563` | `#d1d5db` | Descriptions, captions |
| `text-text-tertiary` | `#9ca3af` | `#6b7280` | Placeholders, metadata |
| `text-text-on-brand` | `#ffffff` | `#ffffff` | Text on brand-coloured backgrounds |
| `text-text-inverse` | `#ffffff` | `#ffffff` | Text on dark surfaces |

### Borders

| Token | Light | Dark | Usage |
|-------|-------|------|-------|
| `border-border-subtle` | `#e5e7eb` | `#1f2937` | Card outlines, dividers |
| `border-border-default` | `#d1d5db` | `#374151` | Input borders |
| `border-border-strong` | `#9ca3af` | `#4b5563` | Emphasis borders |

### Brand (Blue)

| Token | Light | Dark | Usage |
|-------|-------|------|-------|
| `bg-brand-50` | `#eff6ff` | `#0c1a3a` | Chip/badge backgrounds |
| `brand-100` | `#dbeafe` | — | Focus rings |
| `brand-300` | `#93c5fd` | — | Focus border |
| `brand-500` | `#2563eb` | `#60a5fa` | Primary buttons, links |
| `brand-600` | `#1d4ed8` | `#3b82f6` | Button hover |
| `brand-700` | `#1e40af` | `#2563eb` | Dark text on light chips |

### Semantic — Success

| Token | Light | Dark | Usage |
|-------|-------|------|-------|
| `success-50` | `#f0fdf4` | `#052e16` | Alert / badge background |
| `success-100` | `#dcfce7` | `#14532d` | Success banner fill |
| `success-200` | `#bbf7d0` | `#166534` | Success banner border |
| `success-500` | `#16a34a` | — | Icon fill |
| `success-600` | `#15803d` | — | Badge text |
| `success-700` | `#166534` | — | Heavy text on success bg |

### Semantic — Warning

| Token | Light | Dark | Usage |
|-------|-------|------|-------|
| `warning-50` | `#fffbeb` | `#1a0e00` | Badge background |
| `warning-500` | `#d97706` | — | Icon fill |
| `warning-600` | `#b45309` | — | Badge text, border |

### Semantic — Danger

| Token | Light | Dark | Usage |
|-------|-------|------|-------|
| `danger-50` | `#fef2f2` | `#2c0a0a` | Error alert background |
| `danger-200` | `#fecaca` | `#7f1d1d` | Error alert border |
| `danger-400` | `#f87171` | — | Focus border on error inputs |
| `danger-500` | `#dc2626` | — | Error text / icon |

### Application Status (Kanban)

| Token | Value | Usage |
|-------|-------|-------|
| `status-planned` | `#6366f1` | Planned application |
| `status-applied` | `#2563eb` | Submitted |
| `status-pending` | `#d97706` | Under review |
| `status-accepted` | `#16a34a` | Accepted |
| `status-rejected` | `#dc2626` | Rejected |
| `status-withdrawn` | `#64748b` | Withdrawn |

---

## Typography Scale

| Token | Size | Usage |
|-------|------|-------|
| `text-xs` | 12 px | Captions, metadata, uppercase labels |
| `text-sm` | 14 px | Body small, form fields, badges |
| `text-base` | 16 px | Body default |
| `text-lg` | 18 px | Card headings |
| `text-xl` | 20 px | Section sub-headings |
| `text-2xl` | 28 px | Card titles, booking names |
| `text-3xl` | 36 px | Stat numbers |
| `text-4xl` | 48 px | Page titles |
| `text-5xl` | 64 px | Hero headline |
| `text-6xl` | 80 px | Marketing display |

**Fonts**
- Latin: `Inter`, then system-ui sans
- Arabic/RTL: `IBM Plex Sans Arabic`, `Noto Sans Arabic`, Geeza Pro
- Code: `JetBrains Mono`

**Heading defaults** (via `@layer base`): `font-weight: 600`, `letter-spacing: -0.02em`, `line-height: 1.08`.

---

## Spacing Rhythm

Use Tailwind's default 4 px base grid. Prefer:
- `gap-3` / `gap-4` / `gap-6` inside grid/flex layouts
- `p-4` / `p-5` / `p-6` for card padding
- `py-10` for page vertical rhythm
- `mt-8` between major page sections

---

## Radii

| Token | Value | Usage |
|-------|-------|-------|
| `rounded-sm` | 6 px | Tags, chips |
| `rounded-md` | 8 px | Inputs, small buttons |
| `rounded-lg` | 12 px | Buttons, action rows |
| `rounded-xl` | 16 px | Cards, form panels |
| `rounded-2xl` | 24 px | Main content cards |
| `rounded-full` | 980 px | Badges, pill buttons, avatars |

---

## Shadows

| Token | Usage |
|-------|-------|
| `shadow-xs` | Inline elements, tight lift |
| `shadow-sm` | Cards, dropdowns |
| `shadow-md` | Hover lift, popovers |
| `shadow-lg` | Modals, drawers |
| `shadow-focus` | Keyboard focus ring (auto via `:focus-visible`) |

---

## Motion

| Token | Duration | Easing | Usage |
|-------|----------|--------|-------|
| `duration-fast` | 120 ms | — | Icon swaps, badge transitions |
| `duration-base` | 200 ms | `ease-out-apple` | Buttons, inputs |
| `duration-slow` | 320 ms | `ease-in-out-apple` | Panel slides |
| `duration-slower` | 500 ms | — | Page transitions |

All transitions respect `prefers-reduced-motion` — durations collapse to `0.01 ms` globally.

---

## Rules

1. **No raw hex values in JSX/TSX.** Always use a token class.
2. **No `slate-*`, `gray-*`, or `primary-*` classes** — they bypass dark mode.
3. **No hardcoded `dark:` variants** for colours — dark mode is handled by the token overrides in `globals.css`.
4. Every interactive element needs a visible `:focus-visible` ring (provided globally via base styles).
5. Every screen must pass EN + AR (RTL) + light + dark mode review.
