import { useAppStore } from "../state/store";

export function ConnectionOverlay() {
  const status = useAppStore((state) => state.connectionStatus);
  const phase = useAppStore((state) => state.phase);
  if (phase !== "playing" || status === "connected") {
    return null;
  }
  return (
    <div className="panel connection-banner" role="status">
      {status === "reconnecting" ? "Connection lost, reconnecting…" : "Connecting to the game server…"}
    </div>
  );
}
