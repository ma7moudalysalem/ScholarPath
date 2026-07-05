import { describe, it, expect } from "vitest";
import { switchableRoles, postAuthPath } from "@/services/api/auth";
import type { CurrentUser } from "@/stores/authStore";

/** Minimal valid CurrentUser with overridable fields for focused assertions. */
function makeUser(overrides: Partial<CurrentUser> = {}): CurrentUser {
  return {
    id: "u1",
    email: "u@example.com",
    firstName: "Test",
    lastName: "User",
    fullName: "Test User",
    profileImageUrl: null,
    accountStatus: "Active",
    isOnboardingComplete: true,
    emailConfirmed: true,
    roles: ["Student"],
    activeRole: "Student",
    preferredLanguage: null,
    ...overrides,
  };
}

describe("switchableRoles", () => {
  it("returns the plain switchable roles a user holds", () => {
    const user = makeUser({ roles: ["Student", "ScholarshipProvider"] });
    expect(switchableRoles(user)).toEqual(["Student", "ScholarshipProvider"]);
  });

  it("hides Consultant when the backend has NOT confirmed eligibility", () => {
    // A raw/stale Consultant role row must not expose the switch target.
    const noFlag = makeUser({ roles: ["Student", "Consultant"] });
    expect(switchableRoles(noFlag)).toEqual(["Student"]);

    const falseFlag = makeUser({
      roles: ["Student", "Consultant"],
      canActAsConsultant: false,
    });
    expect(switchableRoles(falseFlag)).toEqual(["Student"]);
  });

  it("shows Consultant only when canActAsConsultant is true", () => {
    const user = makeUser({
      roles: ["Student", "Consultant"],
      canActAsConsultant: true,
    });
    expect(switchableRoles(user)).toEqual(["Student", "Consultant"]);
  });

  it("excludes non-switchable roles (SuperAdmin, Moderator)", () => {
    const user = makeUser({ roles: ["Admin", "SuperAdmin", "Moderator"] });
    expect(switchableRoles(user)).toEqual(["Admin"]);
  });

  it("excludes the pre-rename 'Company' role literal", () => {
    // Regression guard for the Company→ScholarshipProvider rename: a stale
    // "Company" role string is not offered as a switch target.
    const user = makeUser({ roles: ["Student", "Company"] });
    expect(switchableRoles(user)).toEqual(["Student"]);
  });
});

describe("postAuthPath", () => {
  it("routes an incomplete onboarding to /onboarding regardless of role", () => {
    const user = makeUser({ isOnboardingComplete: false, activeRole: "Student" });
    expect(postAuthPath(user)).toBe("/onboarding");
  });

  it.each([
    ["Student", "/student"],
    ["ScholarshipProvider", "/company"],
    ["Consultant", "/consultant"],
    ["Admin", "/admin"],
    ["SuperAdmin", "/admin"],
  ])("routes an onboarded %s to %s", (activeRole, expected) => {
    expect(postAuthPath(makeUser({ activeRole }))).toBe(expected);
  });

  it("falls back to /onboarding for an unknown or null active role", () => {
    expect(postAuthPath(makeUser({ activeRole: null }))).toBe("/onboarding");
    expect(postAuthPath(makeUser({ activeRole: "Wat" }))).toBe("/onboarding");
  });
});
