import { useState } from "react";
import { cn } from "@/lib/utils";
import { userPhotoUrl } from "@/lib/userPhoto";

export interface UserAvatarProps {
  /** The user whose profile photo should be shown. */
  userId: string;
  /** Display name — used for the `alt` text and to derive the initials fallback. */
  name: string;
  /** Sizing / extra classes for both the photo and the initials placeholder. */
  className?: string;
  /** Font-size class for the initials fallback (defaults to `text-sm`). */
  initialsClassName?: string;
}

/** First-letter initial of a display name, upper-cased. */
function initial(name: string): string {
  return name.trim()[0]?.toUpperCase() ?? "?";
}

/**
 * A user's profile photo, served from `GET /api/profiles/{userId}/photo`. When
 * the user has no photo (the endpoint 404s) it falls back to their initial in a
 * coloured circle, so the avatar always renders something sensible.
 */
export function UserAvatar({ userId, name, className, initialsClassName }: UserAvatarProps) {
  const [failed, setFailed] = useState(false);

  if (failed) {
    return (
      <div
        className={cn(
          "flex items-center justify-center rounded-full bg-brand-100 font-bold text-brand-600",
          initialsClassName ?? "text-sm",
          className,
        )}
      >
        {initial(name)}
      </div>
    );
  }

  return (
    <img
      src={userPhotoUrl(userId)}
      alt={name}
      onError={() => setFailed(true)}
      className={cn("rounded-full object-cover", className)}
    />
  );
}
