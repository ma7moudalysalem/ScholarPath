# ScholarPath Design System
## Inspired by Apple's Design Philosophy — Adapted for an Education Platform

---

## 1. Visual Theme & Atmosphere

ScholarPath adopts Apple's philosophy of **controlled elegance** — vast expanses of clean backgrounds serve as canvases for scholarship content that is presented as the hero. The interface retreats until it becomes invisible, letting students focus on opportunities, not chrome.

The design is **reductive but warm** — unlike Apple's stark product photography, ScholarPath uses the same spatial discipline but with a warmer educational tone. Bilingual support (EN/AR) requires careful typography handling with the `{Field}` / `{Field}Ar` pattern throughout.

**Key Characteristics:**
- Inter font family (closest open-source match to SF Pro's proportions)
- Binary light/dark section rhythm: white (`#ffffff`) alternating with light gray (`#f5f5f7`)
- Single accent color: ScholarPath Blue (`#2563eb`) reserved for interactive elements
- Tight headline line-heights (1.07-1.14) creating compressed, confident impact
- Full-width section layout with centered content — clean, focused
- Pill-shaped CTAs (980px radius) for primary actions
- Generous whitespace between sections
- RTL-ready layout for Arabic content

## 2. Color Palette & Roles

### Primary
- **White** (`#ffffff`): Primary page background, card surfaces
- **Light Gray** (`#f5f5f7`): Alternate section backgrounds, sidebar areas
- **Near Black** (`#1d1d1f`): Primary text on light backgrounds

### Interactive
- **ScholarPath Blue** (`#2563eb`): Primary CTA backgrounds, focus rings, active states
- **Link Blue** (`#1d4ed8`): Inline text links on light backgrounds
- **Bright Blue** (`#60a5fa`): Links on dark backgrounds, info states

### Semantic
- **Success Green** (`#16a34a`): Accepted status, eligibility match, completion
- **Warning Amber** (`#d97706`): Pending status, deadline approaching, needs attention
- **Error Red** (`#dc2626`): Rejected status, errors, destructive actions
- **Info Blue** (`#2563eb`): Informational banners, tips

### Text
- **Near Black** (`#1d1d1f`): Primary body text
- **Gray 600** (`#4b5563`): Secondary text, descriptions
- **Gray 400** (`#9ca3af`): Placeholder text, disabled states, tertiary info
- **White** (`#ffffff`): Text on dark/blue backgrounds

### Surface
- **Card White** (`#ffffff`): Card backgrounds with subtle shadow
- **Card Gray** (`#f9fafb`): Subtle card variant
- **Dark Surface** (`#111827`): Dark mode backgrounds, hero sections
- **Dark Card** (`#1f2937`): Cards on dark backgrounds

### Status Colors (Application Tracking)
- **Planned** (`#6366f1`): Indigo - intention phase
- **Applied** (`#2563eb`): Blue - active
- **Pending** (`#d97706`): Amber - waiting
- **Accepted** (`#16a34a`): Green - success
- **Rejected** (`#dc2626`): Red - declined

## 3. Typography Rules

### Font Family
- **Primary**: `Inter`, with fallbacks: `-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif`
- **Arabic**: `"IBM Plex Sans Arabic"`, with fallbacks: `"Noto Sans Arabic", sans-serif`
- **Monospace**: `"JetBrains Mono", "Fira Code", "Consolas", monospace` (code snippets, reference numbers)

### Hierarchy

| Role | Size | Weight | Line Height | Letter Spacing | Usage |
|------|------|--------|-------------|----------------|-------|
| Display Hero | 48px (3rem) | 700 | 1.08 | -0.02em | Landing page hero headlines |
| Page Title | 36px (2.25rem) | 700 | 1.11 | -0.02em | Page headings (Dashboard, Scholarships) |
| Section Heading | 28px (1.75rem) | 600 | 1.14 | -0.01em | Section titles within pages |
| Card Title | 20px (1.25rem) | 600 | 1.2 | -0.01em | Scholarship card titles, widget headings |
| Subtitle | 18px (1.125rem) | 500 | 1.33 | normal | Subtitles, lead paragraphs |
| Body | 16px (1rem) | 400 | 1.5 | normal | Standard reading text |
| Body Small | 14px (0.875rem) | 400 | 1.43 | normal | Secondary descriptions, metadata |
| Caption | 12px (0.75rem) | 500 | 1.33 | 0.01em | Labels, timestamps, badges |
| Micro | 10px (0.625rem) | 500 | 1.4 | 0.02em | Legal, footnotes |

### Principles
- **Weight restraint**: 400 (regular) and 600 (semibold) carry 90% of the UI. 700 (bold) for hero headlines only.
- **Negative tracking on headlines**: -0.02em on display/title sizes creates Apple-like compressed confidence.
- **RTL support**: All text containers must use `direction: inherit` and logical properties (`margin-inline-start` not `margin-left`).

## 4. Component Stylings

### Buttons

**Primary Blue (CTA)**
- Background: `#2563eb`
- Text: `#ffffff`
- Padding: 10px 20px
- Radius: 8px
- Font: Inter, 16px, weight 500
- Hover: `#1d4ed8` (darker)
- Active: `#1e40af`
- Focus: 2px solid `#2563eb` outline with 2px offset

**Secondary (Outline)**
- Background: transparent
- Text: `#2563eb`
- Border: 1.5px solid `#2563eb`
- Radius: 8px
- Hover: `#eff6ff` background tint

**Pill Link**
- Background: transparent
- Text: `#1d4ed8`
- Radius: 980px (full pill)
- Border: 1px solid `#1d4ed8`
- Font: Inter, 14px, weight 500
- Use: "Learn more", "View details" inline CTAs

**Destructive**
- Background: `#dc2626`
- Text: `#ffffff`
- Radius: 8px
- Hover: `#b91c1c`

### Cards
- Background: `#ffffff`
- Border: 1px solid `#e5e7eb`
- Radius: 12px
- Shadow: `0 1px 3px rgba(0,0,0,0.1), 0 1px 2px rgba(0,0,0,0.06)`
- Hover: `0 4px 6px rgba(0,0,0,0.1), 0 2px 4px rgba(0,0,0,0.06)` + translateY(-1px)
- Padding: 24px

### Navigation
- Background: `rgba(255, 255, 255, 0.8)` with `backdrop-filter: saturate(180%) blur(20px)`
- Height: 64px
- Border-bottom: 1px solid `rgba(0,0,0,0.08)`
- Logo: ScholarPath wordmark, Inter 20px weight 700, `#1d1d1f`
- Links: Inter 14px weight 500, `#4b5563`, hover `#1d1d1f`
- Active link: `#2563eb` with 2px bottom border

### Status Badges
- Radius: 9999px (full pill)
- Padding: 2px 10px
- Font: Inter 12px weight 500
- Each status has background tint + text color:
  - Planned: bg `#eef2ff`, text `#4338ca`
  - Applied: bg `#eff6ff`, text `#1d4ed8`
  - Pending: bg `#fffbeb`, text `#b45309`
  - Accepted: bg `#f0fdf4`, text `#15803d`
  - Rejected: bg `#fef2f2`, text `#b91c1c`

### Form Inputs
- Background: `#ffffff`
- Border: 1.5px solid `#d1d5db`
- Radius: 8px
- Padding: 10px 14px
- Font: Inter 16px
- Focus: border `#2563eb`, ring `0 0 0 3px rgba(37,99,235,0.1)`
- Error: border `#dc2626`, ring `0 0 0 3px rgba(220,38,38,0.1)`

## 5. Layout Principles

### Spacing Scale (8px base)
4, 8, 12, 16, 20, 24, 32, 40, 48, 64, 80, 96, 128

### Grid
- Max content width: 1280px
- Page padding: 16px (mobile), 24px (tablet), 32px (desktop)
- Sidebar: 256px fixed width on desktop, drawer on mobile
- Card grid: 1 col (mobile) -> 2 col (tablet) -> 3 col (desktop)

### Whitespace
- **Section spacing**: 64px-96px between major page sections
- **Card gap**: 24px between cards in grid
- **Content breathing**: 48px padding inside major containers

## 6. Depth & Elevation

| Level | Shadow | Usage |
|-------|--------|-------|
| Level 0 | none | Flat content, inline elements |
| Level 1 | `0 1px 3px rgba(0,0,0,0.1), 0 1px 2px rgba(0,0,0,0.06)` | Cards, dropdowns |
| Level 2 | `0 4px 6px rgba(0,0,0,0.1), 0 2px 4px rgba(0,0,0,0.06)` | Hover cards, popovers |
| Level 3 | `0 10px 15px rgba(0,0,0,0.1), 0 4px 6px rgba(0,0,0,0.05)` | Modals, drawers |
| Navigation | `backdrop-filter: saturate(180%) blur(20px)` on semi-transparent | Sticky nav |

## 7. Do's and Don'ts

### Do
- Use ScholarPath Blue (`#2563eb`) ONLY for interactive elements
- Alternate between white and `#f5f5f7` section backgrounds
- Use 980px pill radius for inline CTAs
- Keep scholarship cards clean with generous padding
- Use status-colored badges for application tracking
- Support RTL layout with logical CSS properties
- Use subtle shadows — Apple-like, not Material Design
- Compress headline line-heights for confident typography

### Don't
- Don't introduce additional accent colors beyond the semantic set
- Don't use heavy shadows or multiple shadow layers
- Don't use borders heavier than 1.5px
- Don't apply wide letter-spacing — Inter runs tight like SF Pro
- Don't center-align body text — left-align (or right-align for Arabic)
- Don't use gradients on backgrounds — solid colors only
- Don't make cards overly decorative — content is the hero

## 8. Responsive Behavior

### Breakpoints
| Name | Width | Layout |
|------|-------|--------|
| Mobile | <640px | Single column, bottom nav, drawer sidebar |
| Tablet | 640-1024px | 2-column grids, collapsible sidebar |
| Desktop | 1024-1280px | Full layout, fixed sidebar |
| Large | >1280px | Centered with generous margins |

### Touch Targets
- Minimum: 44x44px
- Primary CTAs: 48px height
- Navigation items: 44px height
- Card click areas: entire card surface

## 9. Agent Prompt Guide

### Quick Color Reference
- Primary CTA: `#2563eb`
- Page background: `#ffffff` / `#f5f5f7`
- Heading text: `#1d1d1f`
- Body text: `#4b5563`
- Link: `#1d4ed8`
- Success: `#16a34a`
- Warning: `#d97706`
- Error: `#dc2626`
- Card border: `#e5e7eb`
- Card shadow: `0 1px 3px rgba(0,0,0,0.1)`
- Focus ring: `#2563eb`

### Example Prompts
- "Create a scholarship card: white bg, 12px radius, 1px solid #e5e7eb border, subtle shadow. Scholarship title at 20px Inter weight 600. Provider name at 14px #4b5563. Deadline badge in amber. Two pill CTAs: 'Learn more' (outline blue, 980px radius) and 'Save' (filled blue, 8px radius)."
- "Build the ScholarPath nav: sticky, 64px height, white bg with backdrop blur, 1px bottom border. Logo left (Inter 20px bold), links center (14px medium #4b5563), auth buttons right (primary blue CTA)."
- "Design the dashboard summary: white bg section with 4 stat cards in a grid. Each card has an icon, count (36px bold), and label (14px #4b5563). Cards have 12px radius, subtle shadow, 24px padding."
