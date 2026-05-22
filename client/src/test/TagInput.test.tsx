import { describe, it, expect, beforeAll, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import i18n from "@/lib/i18n";
import { TagInput, MAX_TAGS_PER_POST } from "@/components/community/TagInput";

beforeAll(async () => {
  await i18n.changeLanguage("en");
});

function Harness({ initial = [] as string[] }) {
  const [tags, setTags] = useState<string[]>(initial);
  return (
    <div>
      <TagInput value={tags} onChange={setTags} placeholder="Add a tag" />
      <div data-testid="snapshot">{tags.join(",")}</div>
    </div>
  );
}

describe("TagInput", () => {
  it("commits a tag on Enter and slugifies the value", async () => {
    const user = userEvent.setup();
    render(<Harness />);
    const input = screen.getByPlaceholderText("Add a tag");
    await user.type(input, "Study Abroad{Enter}");
    expect(screen.getByTestId("snapshot")).toHaveTextContent("study-abroad");
  });

  it("dedupes case-insensitively", async () => {
    const user = userEvent.setup();
    render(<Harness initial={["visa"]} />);
    const input = screen.getByPlaceholderText("Add a tag");
    await user.type(input, "VISA{Enter}");
    expect(screen.getByTestId("snapshot")).toHaveTextContent(/^visa$/);
  });

  it(`caps at ${MAX_TAGS_PER_POST} tags`, async () => {
    const user = userEvent.setup();
    const initial = ["a", "b", "c", "d", "e"];
    render(<Harness initial={initial} />);
    const input = screen.getByPlaceholderText(/tag limit reached/i);
    // Input is disabled — typing should not add a new tag.
    expect(input).toBeDisabled();
    void user; // avoid lint unused warning
    expect(screen.getByTestId("snapshot")).toHaveTextContent(initial.join(","));
  });

  it("removes a tag when the chip's X is clicked", async () => {
    const user = userEvent.setup();
    render(<Harness initial={["alpha", "beta"]} />);
    await user.click(screen.getByLabelText("Remove tag alpha"));
    expect(screen.getByTestId("snapshot")).toHaveTextContent(/^beta$/);
  });
});

// Avoid unused-import warning when type-only.
void vi;
