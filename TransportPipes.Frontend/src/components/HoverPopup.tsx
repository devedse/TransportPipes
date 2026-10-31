import { useRef, useState, type ReactNode } from "react";
import { createPortal } from "react-dom";

/**
 * A little popup on hover or focus that says what something is (the owner, 2026-10-31: "a yard round ... this is
 * exactly one of those things which needs user explanations ... add an on hover here", and "go through all other
 * things"): over its element when there is room, else under it, never off the screen; on the page itself, so a panel's
 * scrolling or blur doesn't cut it off. The wrapper lays its element out as if it weren't there.
 */
export function HoverPopup({
  popup,
  children,
  width = 320,
  off = false,
}: {
  popup: ReactNode;
  children: ReactNode;
  width?: number;
  /** No popup now (the panel it explains is open) */
  off?: boolean;
}) {
  const box = useRef<HTMLSpanElement>(null);
  const [at, setAt] = useState<{ left: number; top: number; above: boolean; width: number } | null>(null);
  const show = () => {
    const rect = (box.current?.firstElementChild ?? box.current)?.getBoundingClientRect();
    if (rect === undefined) {
      return;
    }
    const w = Math.min(width, window.innerWidth - 16);
    const above = rect.top > 300;
    setAt({
      left: Math.max(8, Math.min(rect.left + rect.width / 2 - w / 2, window.innerWidth - w - 8)),
      top: above ? rect.top - 8 : rect.bottom + 8,
      above,
      width: w,
    });
  };
  return (
    <span
      ref={box}
      className="goods-tip-anchor"
      onMouseEnter={show}
      onMouseLeave={() => setAt(null)}
      onFocus={show}
      onBlur={() => setAt(null)}
    >
      {children}
      {at !== null &&
        !off &&
        createPortal(
          <span
            className={`panel goods-tip${at.above ? " above" : ""}`}
            role="tooltip"
            style={{ left: at.left, top: at.top, width: at.width }}
          >
            {popup}
          </span>,
          document.body,
        )}
    </span>
  );
}
