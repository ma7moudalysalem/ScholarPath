# RTL + Arabic

EN+AR parity is a constitutional rule (IV). An English-only page is a shipping blocker.

## Direction flip

Triggered in `client/src/App.tsx` from the i18next `lng`:

```ts
useEffect(() => {
  document.documentElement.setAttribute("dir", getDirection(i18n.language));
  document.documentElement.setAttribute("lang", i18n.language);
}, [i18n.language]);
```

`getDirection("ar") → "rtl"` — everything else returns `ltr`.

## Logical CSS properties — use them everywhere

**Never** use:
```css
margin-left   padding-right   left   border-right   right: 0
```

Always use:
```css
margin-inline-start   padding-inline-end   inset-inline-start   border-inline-end   inset-inline-end: 0
```

Tailwind v4 utilities include logical variants: `ms-4` = `margin-inline-start`, `pe-2` = `padding-inline-end`, `start-0`, `end-0`, `border-s`, `border-e`, `rounded-s-md`, etc.

## Icons

Arrow icons flip with direction (e.g., `ChevronRight` in LTR is `ChevronLeft` in RTL). Two options:

1. **Scale transform** — `className="rtl:-scale-x-100"`. Cheap, keeps the same component.
2. **Conditional import** — only for asymmetric icons like `Send` or cultural/semantic icons.

## Typography

Arabic text uses `IBM Plex Sans Arabic` as the variable font. In `globals.css`:

```css
html[lang="ar"], html[dir="rtl"] { font-family: var(--font-arabic); }
```

Arabic line-height is naturally taller — we use 1.55 for body (vs 1.5 for Latin). Headline letter-spacing is kept at `-0.02em` for a consistent tight-headline feel across both languages.

## Date + number formatting

Use `Intl.DateTimeFormat(i18n.language, { ... })` and `Intl.NumberFormat(i18n.language, { ... })`. Arabic renders with `٠١٢٣٤٥٦٧٨٩` digits by default in some locales — opt out with `{ numberingSystem: "latn" }` if inconsistency is a problem (e.g., date tables).

Currency stays USD in v1, formatted per locale.

## Forms

- Labels remain **start-aligned** (label above, centered on wider screens) — not flipped.
- Inputs fill the row: `width: 100%` works identically.
- Placeholder text: `ms-*` for padding instead of `ml-*`.

## Testing parity

Every user-facing feature must:

- [ ] Have keys in both `locales/en/*.json` and `locales/ar/*.json` (CI will fail if keys diverge — see `tools/check-i18n-parity.mjs` on the roadmap).
- [ ] Render cleanly with `<html dir="rtl">` — screenshots via Playwright: `test.use({ locale: "ar-EG" })`.
- [ ] Keep icon semantics (no upside-down logos).
- [ ] Handle bidirectional text when user content mixes scripts (`unicode-bidi: plaintext` on message bubbles).

## Content writing guidelines

- Short, direct sentences. Arabic translations should aim for the same cadence, not a literal carry-over.
- Dates: `DD MMM YYYY` in both.
- Currency symbol position respects locale (`$50` → `50 $` in AR if needed).
- Avoid idioms that don't translate (e.g., "hit the ground running"). Use module-specific glossary in `docs/glossary.md` (TBD).

## Known gotchas

- **MUI, Chakra, Material UI** don't handle RTL cleanly in all components; we stick to Radix + shadcn which use logical properties throughout.
- **Shadows direction**: shadows stay directional and visually correct (they point down in both languages).
- **Scroll bar position**: flips to the left in RTL — that's expected.
- **Text input caret**: browsers handle this automatically based on `dir`.
