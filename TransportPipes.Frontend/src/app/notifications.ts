import type { ToastDto } from "../api/contracts";

// Browser notifications while the game is in the background: the moments that bring a player back (GDD §10.1, "come
// back for a reason"), never every construction.

const NOTIFY_KINDS = new Set<ToastDto["kind"]>([
  "levelUp",
  "researchFinished",
  "storageFull",
  "contractChanged",
  "wonder",
  "achievement",
]);

export function notificationsSupported(): boolean {
  return typeof window !== "undefined" && "Notification" in window;
}

export function notificationsAsked(): boolean {
  return notificationsSupported() && Notification.permission !== "default";
}

export async function askForNotifications(): Promise<boolean> {
  if (!notificationsSupported()) {
    return false;
  }
  return (await Notification.requestPermission()) === "granted";
}

/** Shows a notification for an event when the tab is hidden and the player allowed it */
export function notify(toast: ToastDto) {
  if (
    !notificationsSupported() ||
    Notification.permission !== "granted" ||
    !document.hidden ||
    !NOTIFY_KINDS.has(toast.kind)
  ) {
    return;
  }
  // Digs and expeditions come as achievements and construction notes; the text says what happened
  const notification = new Notification("TransportPipes", {
    body: toast.text,
    icon: "/art/ui/loady-wave.webp",
    tag: toast.kind,
  });
  notification.onclick = () => {
    window.focus();
    notification.close();
  };
}
