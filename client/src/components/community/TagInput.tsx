import { useState } from "react";
import { useTranslation } from "react-i18next";
import { X } from "lucide-react";

export const MAX_TAGS_PER_POST = 5;
export const MAX_TAG_LENGTH = 30;

export interface TagInputProps {
  value: string[];
  onChange: (next: string[]) => void;
  placeholder?: string;
  disabled?: boolean;
}

function slugify(raw: string): string {
  return raw
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9-]+/g, "-")
    .replace(/-{2,}/g, "-")
    .replace(/^-+|-+$/g, "")
    .slice(0, MAX_TAG_LENGTH);
}

/**
 * Chip-style multi-tag input. Mirrors the server-side rules so the user
 * doesn't hit surprise validation errors: slugifies on Enter/comma/Tab,
 * caps the list at MAX_TAGS_PER_POST, dedupes case-insensitively.
 */
export function TagInput({ value, onChange, placeholder, disabled }: TagInputProps) {
  const { t } = useTranslation("community");
  const [draft, setDraft] = useState("");

  const commit = () => {
    const slug = slugify(draft);
    setDraft("");
    if (!slug) return;
    if (value.includes(slug)) return;
    if (value.length >= MAX_TAGS_PER_POST) return;
    onChange([...value, slug]);
  };

  const remove = (slug: string) => {
    onChange(value.filter((t) => t !== slug));
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter" || e.key === "," || e.key === "Tab") {
      if (draft.trim().length > 0) {
        e.preventDefault();
        commit();
      }
    } else if (e.key === "Backspace" && draft.length === 0 && value.length > 0) {
      onChange(value.slice(0, -1));
    }
  };

  const atLimit = value.length >= MAX_TAGS_PER_POST;

  return (
    <div className="flex flex-wrap items-center gap-1.5 rounded-md border border-border-default bg-bg-elevated px-2 py-1.5 focus-within:border-brand-500 focus-within:ring-2 focus-within:ring-brand-500/30">
      {value.map((tag) => (
        <span
          key={tag}
          className="inline-flex items-center gap-1 rounded-full bg-brand-50 px-2.5 py-0.5 text-xs font-medium text-brand-700"
        >
          #{tag}
          {!disabled && (
            <button
              type="button"
              onClick={() => remove(tag)}
              aria-label={t("tags.remove", { tag })}
              className="text-brand-500 transition-colors hover:text-brand-800"
            >
              <X size={12} />
            </button>
          )}
        </span>
      ))}
      <input
        type="text"
        value={draft}
        onChange={(e) => setDraft(e.target.value)}
        onKeyDown={handleKeyDown}
        onBlur={commit}
        placeholder={atLimit ? t("tags.limitReached") : placeholder}
        disabled={disabled || atLimit}
        maxLength={MAX_TAG_LENGTH}
        className="min-w-[6rem] flex-1 bg-transparent px-1 py-1 text-sm outline-none placeholder:text-text-tertiary disabled:cursor-not-allowed"
      />
    </div>
  );
}
