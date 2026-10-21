/** Loady while the world loads: before the game starts, and over the map until it has been drawn */
export function MapLoading({ text, cover = false }: { text: string; cover?: boolean }) {
  return (
    <div className={cover ? "map-loading" : "screen loading-screen"} role="status">
      <img className="loading-loady" src="/art/ui/loady-think.webp" alt="" />
      <p className="loading-text">{text}</p>
    </div>
  );
}
