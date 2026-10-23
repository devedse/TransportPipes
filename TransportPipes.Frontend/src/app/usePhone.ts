import { useSyncExternalStore } from "react";

const PHONE = "(max-width: 720px)";

function subscribe(onChange: () => void): () => void {
  const query = window.matchMedia?.(PHONE);
  query?.addEventListener("change", onChange);
  return () => query?.removeEventListener("change", onChange);
}

/** Whether the screen is a phone's (the layout's 720 px break) */
export function usePhone(): boolean {
  return useSyncExternalStore(
    subscribe,
    () => window.matchMedia?.(PHONE).matches === true,
    () => false,
  );
}
