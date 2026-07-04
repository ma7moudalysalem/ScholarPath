import { useState } from "react";
import { useTranslation } from "react-i18next";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import * as Dialog from "@radix-ui/react-dialog";
import { Loader2, X } from "lucide-react";
import { toast } from "sonner";
import { communityApi, type ForumPost } from "@/services/api/community";
import { TagInput, MAX_TAG_LENGTH, MAX_TAGS_PER_POST } from "@/components/community/TagInput";
import { apiErrorMessage } from "@/services/api/client";

const TITLE_MAX = 200;
const BODY_MAX = 10000;

export interface EditPostDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Whether this is the root post (title editable) or a reply (body only). */
  isRoot: boolean;
  /** The post being edited — used to seed the form. */
  post: Pick<
    ForumPost,
    "id" | "title" | "bodyMarkdown" | "tags" | "titleEn" | "titleAr" | "bodyEn" | "bodyAr"
  >;
  /** Invoked after a successful save so callers can refresh local UI. */
  onSaved?: () => void;
}

/**
 * Edit form for a community post or reply. Mirrors AskQuestionModal validation
 * (title required for root posts, body required, tag rules). Wires to
 * `PUT /api/community/posts/{id}` via `communityApi.updatePost`.
 */
export function EditPostDialog({
  open,
  onOpenChange,
  isRoot,
  post,
  onSaved,
}: EditPostDialogProps) {
  const { t, i18n } = useTranslation("community");
  const isRtl = i18n.dir() === "rtl";
  const qc = useQueryClient();

  // Initial values are seeded from props. To pick up a different post or
  // refreshed server data, the parent must remount this dialog (use a `key`
  // tied to post.id, and only render it while editing). That avoids the
  // "setState in an effect" anti-pattern.
  const [titleEn, setTitleEn] = useState(post.titleEn ?? post.title ?? "");
  const [titleAr, setTitleAr] = useState(post.titleAr ?? "");
  const [bodyEn, setBodyEn] = useState(post.bodyEn ?? post.bodyMarkdown);
  const [bodyAr, setBodyAr] = useState(post.bodyAr ?? "");
  const [tags, setTags] = useState<string[]>(post.tags ?? []);

  const updatePost = useMutation({
    mutationFn: () =>
      communityApi.updatePost(post.id, isRoot
        ? {
            titleEn: titleEn.trim(),
            titleAr: titleAr.trim(),
            bodyEn: bodyEn.trim(),
            bodyAr: bodyAr.trim(),
            tags,
          }
        : {
            // Replies are single-language.
            titleEn: null,
            titleAr: null,
            bodyEn: bodyEn.trim(),
            bodyAr: null,
          }),
    onSuccess: () => {
      void qc.invalidateQueries({ queryKey: ["community", "thread"] });
      void qc.invalidateQueries({ queryKey: ["community", "posts"] });
      void qc.invalidateQueries({ queryKey: ["community", "bookmarks"] });
      toast.success(t("edit.success"));
      onSaved?.();
      onOpenChange(false);
    },
    onError: (err) => toast.error(apiErrorMessage(err, t("edit.error"))),
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (isRoot && (!titleEn.trim() || !titleAr.trim())) {
      toast.error(t("edit.titleRequired"));
      return;
    }
    if (!bodyEn.trim() || (isRoot && !bodyAr.trim())) {
      toast.error(t("edit.bodyRequired"));
      return;
    }
    updatePost.mutate();
  };

  const fieldClass =
    "w-full rounded-md border border-border-default bg-bg-elevated px-3 py-2 text-sm text-text-primary outline-none transition focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30";

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-black/50 backdrop-blur-sm" />
        <Dialog.Content
          dir={isRtl ? "rtl" : "ltr"}
          className="fixed left-1/2 top-1/2 z-50 w-[calc(100vw-2rem)] max-w-lg -translate-x-1/2 -translate-y-1/2 rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-2xl"
        >
          <div className="mb-1 flex items-start justify-between gap-4">
            <Dialog.Title className="text-xl font-semibold text-text-primary">
              {isRoot ? t("edit.titleRoot") : t("edit.titleReply")}
            </Dialog.Title>
            <Dialog.Close
              className="rounded-md p-1 text-text-tertiary transition-colors hover:bg-bg-subtle hover:text-text-primary"
              aria-label={t("edit.cancel")}
            >
              <X size={20} />
            </Dialog.Close>
          </div>

          <form onSubmit={handleSubmit} className="mt-4 space-y-4">
            {isRoot && (
              <>
                <div className="space-y-1.5">
                  <label htmlFor="ep-title-en" className="block text-sm font-medium text-text-primary">
                    {t("ask.titleEnLabel")}
                  </label>
                  <input
                    id="ep-title-en"
                    type="text"
                    dir="ltr"
                    value={titleEn}
                    onChange={(e) => setTitleEn(e.target.value)}
                    maxLength={TITLE_MAX}
                    className={fieldClass}
                  />
                </div>
                <div className="space-y-1.5">
                  <label htmlFor="ep-title-ar" className="block text-sm font-medium text-text-primary">
                    {t("ask.titleArLabel")}
                  </label>
                  <input
                    id="ep-title-ar"
                    type="text"
                    dir="rtl"
                    value={titleAr}
                    onChange={(e) => setTitleAr(e.target.value)}
                    maxLength={TITLE_MAX}
                    className={fieldClass}
                  />
                </div>
              </>
            )}

            <div className="space-y-1.5">
              <label htmlFor="ep-body-en" className="block text-sm font-medium text-text-primary">
                {isRoot ? t("ask.bodyEnLabel") : t("ask.bodyLabel")}
              </label>
              <textarea
                id="ep-body-en"
                dir="ltr"
                value={bodyEn}
                onChange={(e) => setBodyEn(e.target.value)}
                rows={isRoot ? 4 : 6}
                maxLength={BODY_MAX}
                className={fieldClass}
              />
            </div>

            {isRoot && (
              <div className="space-y-1.5">
                <label htmlFor="ep-body-ar" className="block text-sm font-medium text-text-primary">
                  {t("ask.bodyArLabel")}
                </label>
                <textarea
                  id="ep-body-ar"
                  dir="rtl"
                  value={bodyAr}
                  onChange={(e) => setBodyAr(e.target.value)}
                  rows={4}
                  maxLength={BODY_MAX}
                  className={fieldClass}
                />
              </div>
            )}

            {isRoot && (
              <div className="space-y-1.5">
                <label className="block text-sm font-medium text-text-primary">
                  {t("ask.tagsLabel")}
                  <span className="ms-1 font-normal text-text-tertiary">
                    ({t("ask.tagsHint", { max: MAX_TAGS_PER_POST, len: MAX_TAG_LENGTH })})
                  </span>
                </label>
                <TagInput
                  value={tags}
                  onChange={setTags}
                  placeholder={t("ask.tagsPlaceholder")}
                />
              </div>
            )}

            <div className="flex justify-end gap-3 pt-2">
              <Dialog.Close asChild>
                <button
                  type="button"
                  className="rounded-md px-4 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-bg-subtle"
                >
                  {t("edit.cancel")}
                </button>
              </Dialog.Close>
              <button
                type="submit"
                disabled={updatePost.isPending}
                className="inline-flex items-center gap-2 rounded-md bg-brand-500 px-5 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-brand-600 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {updatePost.isPending && <Loader2 size={14} className="animate-spin" />}
                {updatePost.isPending ? t("edit.saving") : t("edit.save")}
              </button>
            </div>
          </form>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
