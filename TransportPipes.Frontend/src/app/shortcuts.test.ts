import { describe, expect, it } from "vitest";
import { shortcutForKey } from "./shortcuts";

const key = (
  value: string,
  modifiers: Partial<{ ctrlKey: boolean; metaKey: boolean; altKey: boolean }> = {},
) => ({
  key: value,
  ctrlKey: false,
  metaKey: false,
  altKey: false,
  ...modifiers,
});

describe("shortcutForKey", () => {
  it("maps the original build keys", () => {
    expect(shortcutForKey(key("f"))).toEqual({ kind: "build", gameObjectType: 1 });
    expect(shortcutForKey(key("M"))).toEqual({ kind: "build", gameObjectType: 2 });
    expect(shortcutForKey(key("w"))).toEqual({ kind: "build", gameObjectType: 200 });
    expect(shortcutForKey(key("s"))).toEqual({ kind: "build", gameObjectType: 201 });
    expect(shortcutForKey(key("o"))).toEqual({ kind: "build", gameObjectType: 400 });
    expect(shortcutForKey(key("t"))).toEqual({ kind: "build", gameObjectType: 450 });
    expect(shortcutForKey(key("p"))).toEqual({ kind: "build", gameObjectType: 10000 });
  });

  it("maps the other actions", () => {
    expect(shortcutForKey(key("Delete"))).toEqual({ kind: "delete" });
    expect(shortcutForKey(key("i"))).toEqual({ kind: "inspect" });
    expect(shortcutForKey(key("c"))).toEqual({ kind: "income" });
    expect(shortcutForKey(key("Escape"))).toEqual({ kind: "dismiss" });
    expect(shortcutForKey(key("?"))).toEqual({ kind: "help" });
  });

  it("ignores modified and unknown keys", () => {
    expect(shortcutForKey(key("c", { ctrlKey: true }))).toBeNull();
    expect(shortcutForKey(key("f", { metaKey: true }))).toBeNull();
    expect(shortcutForKey(key("x"))).toBeNull();
    expect(shortcutForKey(key("Enter"))).toBeNull();
  });
});
