import { useState } from "react";
import { useTranslation } from "react-i18next";
import * as Dialog from "@radix-ui/react-dialog";
import { X, Search, Loader2 } from "lucide-react";
import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { chatApi, type ChatContact } from "@/services/api/chat";
import { UserAvatar } from "@/components/common/UserAvatar";

interface NewMessageModalProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  /** Invoked when a contact is picked — the parent opens/starts that conversation. */
  onSelectContact: (contact: ChatContact) => void;
}

export function NewMessageModal({ isOpen, onOpenChange, onSelectContact }: NewMessageModalProps) {
  const { t } = useTranslation();
  const [search, setSearch] = useState("");

  const term = search.trim();

  const {
    data: contacts = [],
    isFetching,
    isError,
  } = useQuery({
    queryKey: ["chat", "contacts", term],
    queryFn: () => chatApi.searchContacts(term || undefined),
    enabled: isOpen,
    placeholderData: keepPreviousData,
  });

  const handleSelect = (contact: ChatContact) => {
    onSelectContact(contact);
    onOpenChange(false);
    setSearch("");
  };

  const handleOpenChange = (open: boolean) => {
    if (!open) setSearch("");
    onOpenChange(open);
  };

  return (
    <Dialog.Root open={isOpen} onOpenChange={handleOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-black/50 backdrop-blur-sm" />
        <Dialog.Content className="fixed left-1/2 top-1/2 z-50 flex w-full max-w-md -translate-x-1/2 -translate-y-1/2 flex-col rounded-xl bg-white p-6 shadow-2xl dark:bg-slate-900">
          <div className="mb-4 flex items-center justify-between">
            <Dialog.Title className="text-xl font-semibold text-slate-900 dark:text-white">
              {t("chat.new_message_title", "New Message")}
            </Dialog.Title>
            <Dialog.Close className="text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-200">
              <X size={20} />
            </Dialog.Close>
          </div>

          <Dialog.Description className="mb-4 text-sm text-slate-500 dark:text-slate-400">
            {t("chat.new_message_desc", "Search for a person to start a conversation with.")}
          </Dialog.Description>

          <div className="relative mb-4">
            <Search
              className="absolute left-3 top-1/2 -translate-y-1/2 text-text-tertiary"
              size={16}
            />
            <input
              type="text"
              autoFocus
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder={t("chat.contact_search_placeholder", "Search by name...")}
              className="w-full rounded-xl border border-border-subtle bg-bg-elevated py-2 pl-9 pr-4 text-sm outline-none transition-all focus:ring-2 focus:ring-brand-400"
            />
          </div>

          <div className="max-h-72 min-h-[8rem] overflow-y-auto">
            {isError ? (
              <p className="py-8 text-center text-sm text-danger-500">
                {t("chat.contacts_error", "Could not load contacts. Please try again.")}
              </p>
            ) : isFetching && contacts.length === 0 ? (
              <div className="flex items-center justify-center py-8 text-text-tertiary">
                <Loader2 size={20} className="animate-spin" />
              </div>
            ) : contacts.length === 0 ? (
              <p className="py-8 text-center text-sm text-text-tertiary">
                {t("chat.no_contacts", "No people found.")}
              </p>
            ) : (
              <ul className="space-y-1">
                {contacts.map((contact) => (
                  <li key={contact.id}>
                    <button
                      type="button"
                      onClick={() => handleSelect(contact)}
                      className="flex w-full items-center gap-3 rounded-2xl p-3 text-start transition-all hover:bg-bg-subtle"
                    >
                      <UserAvatar
                        userId={contact.id}
                        name={contact.name}
                        className="h-10 w-10 flex-shrink-0 border border-border-subtle"
                      />
                      <div className="flex-1 overflow-hidden">
                        <h4 className="truncate text-sm font-bold text-text-primary">
                          {contact.name}
                        </h4>
                        {contact.role && (
                          <p className="truncate text-xs text-text-secondary">
                            {t(`role.${contact.role.toLowerCase()}`, contact.role)}
                          </p>
                        )}
                      </div>
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
