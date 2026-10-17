import { Suspense } from "react";
import { displayName } from "../app/format";
import { useAppStore } from "../state/store";
import { SpriteThumbnail } from "./SpriteThumbnail";

const THUMBNAIL_WIDTH = 64;

/** The list of buildable game objects; picking one shows its ghost on the map. */
export function BuildMenu() {
  const loggedIn = useAppStore((state) => state.user !== null);
  const infos = useAppStore((state) => state.gameObjectInfos);
  const selected = useAppStore((state) => state.selectedBuildType);
  const select = useAppStore((state) => state.selectBuildType);

  if (!loggedIn || infos.length === 0) {
    return null;
  }

  return (
    <aside className="panel build-menu" aria-label="Build menu">
      <h2 className="panel-title">Build</h2>
      <ul className="build-list">
        {infos.map((info) => {
          const isSelected = info.gameObjectType === selected;
          return (
            <li key={info.gameObjectType}>
              <button
                type="button"
                className={`build-item${isSelected ? " build-item--selected" : ""}`}
                aria-pressed={isSelected}
                onClick={() => select(isSelected ? null : info.gameObjectType)}
              >
                <span className="build-item-thumbnail">
                  <Suspense fallback={<span className="sprite-thumbnail sprite-thumbnail--missing" />}>
                    <SpriteThumbnail name={info.gameObjectTypeName} width={THUMBNAIL_WIDTH} crop />
                  </Suspense>
                </span>
                <span className="build-item-text">
                  <span className="build-item-name">{displayName(info.gameObjectTypeName)}</span>
                  <span className="build-item-price">{info.price === 0 ? "Free" : `${info.price} gold`}</span>
                </span>
              </button>
            </li>
          );
        })}
      </ul>
      {selected !== null && <p className="build-hint">Click a tile to place it · Esc to cancel</p>}
    </aside>
  );
}
