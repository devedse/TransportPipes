import { gameClient } from "../app/client";
import { useAppStore } from "../state/store";

const TIMES = [
  { id: "day", label: "☀ Day" },
  { id: "golden", label: "🌅 Golden hour" },
  { id: "dusk", label: "🌆 Dusk" },
  { id: "night", label: "🌙 Night" },
] as const;

/** Photo mode (GDD §11.6): the map without the UI, a time of day, and a picture to keep */
export function PhotoBar() {
  const time = useAppStore((s) => s.timeOfDay);
  return (
    <div className="panel photo-bar">
      {TIMES.map((t) => (
        <button
          key={t.id}
          className={time === t.id ? "primary" : ""}
          onClick={() => gameClient.setTimeOfDay(t.id)}
        >
          {t.label}
        </button>
      ))}
      <button onClick={() => void gameClient.savePhoto()}>📷 Save picture</button>
      <button className="icon-button" title="Back (Esc)" onClick={() => gameClient.setPhotoMode(false)}>
        ×
      </button>
    </div>
  );
}
