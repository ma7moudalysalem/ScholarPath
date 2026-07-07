import type { ReactNode } from "react";

/**
 * Minimal Markdown renderer for resource/article content (PB-009).
 *
 * Resource content is light Markdown — headings, bullet / ordered / task lists,
 * and bold. It is rendered straight to React elements (NO HTML injection), so it
 * is safe to show untrusted author content. Shared by the student resource
 * detail page and the admin moderation preview so the admin reads exactly what a
 * student would see before approving it.
 */
function renderInline(text: string): ReactNode {
  return text.split(/(\*\*[^*]+\*\*)/g).map((part, i) => {
    const bold = /^\*\*([^*]+)\*\*$/.exec(part);
    return bold ? (
      <strong key={i} className="font-semibold text-text-primary">
        {bold[1]}
      </strong>
    ) : (
      part
    );
  });
}

export function Markdown({ source }: { source: string }) {
  const blocks: ReactNode[] = [];
  let items: ReactNode[] = [];
  let ordered = false;
  let n = 0;

  const flush = () => {
    if (items.length === 0) return;
    const rendered = items;
    blocks.push(
      ordered ? (
        <ol
          key={`b${n++}`}
          className="ms-5 list-decimal space-y-1 text-sm text-text-secondary"
        >
          {rendered}
        </ol>
      ) : (
        <ul key={`b${n++}`} className="space-y-1.5 text-sm text-text-secondary">
          {rendered}
        </ul>
      ),
    );
    items = [];
  };

  for (const raw of source.replace(/\r\n/g, "\n").split("\n")) {
    const line = raw.trim();
    if (!line) {
      flush();
      continue;
    }

    const heading = /^(#{1,3})\s+(.+)$/.exec(line);
    if (heading) {
      flush();
      const level = heading[1].length;
      const cls =
        level === 1
          ? "text-base font-semibold text-text-primary"
          : level === 2
            ? "text-sm font-semibold text-text-primary"
            : "text-sm font-medium text-text-primary";
      blocks.push(
        <p key={`b${n++}`} className={cls}>
          {renderInline(heading[2])}
        </p>,
      );
      continue;
    }

    const task = /^[-*]\s+\[([ xX])\]\s+(.+)$/.exec(line);
    if (task) {
      if (ordered) flush();
      ordered = false;
      const checked = task[1].toLowerCase() === "x";
      items.push(
        <li key={`i${n++}`} className="flex items-start gap-2">
          <span aria-hidden className="mt-px text-brand-500">
            {checked ? "☑" : "☐"}
          </span>
          <span>{renderInline(task[2])}</span>
        </li>,
      );
      continue;
    }

    const bullet = /^[-*]\s+(.+)$/.exec(line);
    if (bullet) {
      if (ordered) flush();
      ordered = false;
      items.push(
        <li key={`i${n++}`} className="flex items-start gap-2">
          <span
            aria-hidden
            className="mt-1.5 size-1.5 shrink-0 rounded-full bg-brand-500"
          />
          <span>{renderInline(bullet[1])}</span>
        </li>,
      );
      continue;
    }

    const numbered = /^\d+\.\s+(.+)$/.exec(line);
    if (numbered) {
      if (!ordered) flush();
      ordered = true;
      items.push(<li key={`i${n++}`}>{renderInline(numbered[1])}</li>);
      continue;
    }

    flush();
    blocks.push(
      <p key={`b${n++}`} className="text-sm leading-relaxed text-text-secondary">
        {renderInline(line)}
      </p>,
    );
  }
  flush();

  return <div className="space-y-2">{blocks}</div>;
}
