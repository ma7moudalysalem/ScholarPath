import { useState } from "react";
import { useTranslation } from "react-i18next";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router";
import * as Dialog from "@radix-ui/react-dialog";
import { X } from "lucide-react";
import { toast } from "sonner";
import { communityApi, type ForumCategory } from "@/services/api/community";
import { TagInput, MAX_TAG_LENGTH, MAX_TAGS_PER_POST } from "@/components/community/TagInput";
import { apiErrorMessage } from "@/services/api/client";

const BODY_MAX = 10000;
const TITLE_MAX = 200;

interface AskQuestionModalProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  categories: ForumCategory[];
}

/**
 * "Ask a Question" — create a new community discussion post.
 * Posts to `POST /api/community/posts` via `communityApi.createPost`.
 */
export function AskQuestionModal({
  isOpen,
  onOpenChange,
  categories,
}: AskQuestionModalProps) {
  const { t, i18n } = useTranslation("community");
  const isRtl = i18n.dir() === "rtl";
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  const [title, setTitle] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [body, setBody] = useState("");
  const [tags, setTags] = useState<string[]>([]);

  const createPost = useMutation({
    mutationFn: () =>
      communityApi.createPost({
        categoryId,
        title: title.trim(),
        bodyMarkdown: body.trim(),
        tags,
      }),
    onSuccess: (newPostId) => {
      void queryClient.invalidateQueries({ queryKey: ["community", "posts"] });
      toast.success(t("ask.success"));
      setTitle("");
      setCategoryId("");
      setBody("");
      setTags([]);
      onOpenChange(false);
      navigate(`/student/community/${newPostId}`);
    },
    onError: (err) => toast.error(apiErrorMessage(err, t("ask.error"))),
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim() || !categoryId || !body.trim()) {
      toast.error(t("ask.validation"));
      return;
    }
    createPost.mutate();
  };

  const fieldClass =
    "w-full rounded-md border border-border-default bg-bg-elevated px-3 py-2 text-sm text-text-primary outline-none transition focus:border-brand-500 focus:ring-2 focus:ring-brand-500/30";

  return (
    <Dialog.Root open={isOpen} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-black/50 backdrop-blur-sm" />
        <Dialog.Content
          dir={isRtl ? "rtl" : "ltr"}
          className="fixed left-1/2 top-1/2 z-50 w-[calc(100vw-2rem)] max-w-lg -translate-x-1/2 -translate-y-1/2 rounded-xl border border-border-subtle bg-bg-elevated p-6 shadow-2xl"
        >
          <div className="mb-1 flex items-start justify-between gap-4">
            <Dialog.Title className="text-xl font-semibold text-text-primary">
              {t("ask.title")}
            </Dialog.Title>
            <Dialog.Close
              className="rounded-md p-1 text-text-tertiary transition-colors hover:bg-bg-subtle hover:text-text-primary"
              aria-label={t("ask.cancel")}
            >
              <X size={20} />
            </Dialog.Close>
          </div>
          <Dialog.Description className="mb-5 text-sm text-text-secondary">
            {t("ask.subtitle")}
          </Dialog.Description>

          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-1.5">
              <label
                htmlFor="aq-title"
                className="block text-sm font-medium text-text-primary"
              >
                {t("ask.titleLabel")}
              </label>
              <input
                id="aq-title"
                type="text"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder={t("ask.titlePlaceholder")}
                maxLength={TITLE_MAX}
                className={fieldClass}
              />
            </div>

            <div className="space-y-1.5">
              <label
                htmlFor="aq-category"
                className="block text-sm font-medium text-text-primary"
              >
                {t("ask.categoryLabel")}
              </label>
              <select
                id="aq-category"
                value={categoryId}
                onChange={(e) => setCategoryId(e.target.value)}
                className={fieldClass}
              >
                <option value="">{t("ask.categoryPlaceholder")}</option>
                {categories.map((cat) => (
                  <option key={cat.id} value={cat.id}>
                    {isRtl ? cat.nameAr : cat.nameEn}
                  </option>
                ))}
              </select>
            </div>

            <div className="space-y-1.5">
              <label
                htmlFor="aq-body"
                className="block text-sm font-medium text-text-primary"
              >
                {t("ask.bodyLabel")}
              </label>
              <textarea
                id="aq-body"
                value={body}
                onChange={(e) => setBody(e.target.value)}
                placeholder={t("ask.bodyPlaceholder")}
                rows={5}
                maxLength={BODY_MAX}
                className={fieldClass}
              />
            </div>

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

            <div className="flex justify-end gap-3 pt-2">
              <Dialog.Close asChild>
                <button
                  type="button"
                  className="rounded-md px-4 py-2 text-sm font-medium text-text-secondary transition-colors hover:bg-bg-subtle"
                >
                  {t("ask.cancel")}
                </button>
              </Dialog.Close>
              <button
                type="submit"
                disabled={createPost.isPending}
                className="cta-pill bg-brand-500 px-5 py-2 text-sm font-medium text-white shadow-sm transition-colors hover:bg-brand-600 disabled:cursor-not-allowed disabled:opacity-50"
              >
                {createPost.isPending ? t("ask.submitting") : t("ask.submit")}
              </button>
            </div>
          </form>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
