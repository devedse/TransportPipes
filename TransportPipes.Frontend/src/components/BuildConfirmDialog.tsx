import { Suspense } from "react";
import type { ResourceInfo } from "../api/contracts";
import { displayName, formatDuration } from "../app/format";
import { useNow } from "../app/hooks";
import { gameClient } from "../app/instance";
import { formatGold, formatPerHour, goldAt } from "../state/gold";
import { useAppStore } from "../state/store";
import { SpriteThumbnail } from "./SpriteThumbnail";

/** Confirmation after clicking a tile with a building selected. */
export function BuildConfirmDialog() {
  const pending = useAppStore((state) => state.pendingBuild);
  const setPendingBuild = useAppStore((state) => state.setPendingBuild);
  const gold = useAppStore((state) => state.gold);
  const now = useNow(500);

  if (pending === null) {
    return null;
  }
  const { info, tile } = pending;
  const currentGold = goldAt(gold, now);
  const cancel = () => setPendingBuild(null);

  return (
    <div className="modal-backdrop" onClick={cancel}>
      <div
        className="panel dialog build-confirm"
        role="dialog"
        aria-modal="true"
        aria-labelledby="build-confirm-title"
        onClick={(event) => event.stopPropagation()}
      >
        <h2 id="build-confirm-title">{displayName(info.gameObjectTypeName)}</h2>
        <p className="muted">
          At tile {tile.x}, {tile.y}
        </p>
        <div className="build-confirm-body">
          <div className="build-confirm-preview">
            <Suspense fallback={<span className="sprite-thumbnail sprite-thumbnail--missing" />}>
              <SpriteThumbnail name={info.gameObjectTypeName} width={144} animate />
            </Suspense>
          </div>
          <div className="build-confirm-details">
            <dl className="stats">
              <dt>Price</dt>
              <dd>{info.price} gold</dd>
              <dt>Build time</dt>
              <dd>{formatDuration(info.buildTimeInSeconds)}</dd>
            </dl>
            <ResourceList title="Consumes" resources={info.consumptionInfo} />
            <ResourceList title="Produces" resources={info.productionInfo} />
          </div>
        </div>
        {gold !== null && currentGold <= info.price && info.price > 0 && (
          <p className="warning">
            You have {formatGold(currentGold)} gold, which is not enough for this building.
          </p>
        )}
        <div className="actions">
          <button
            type="button"
            className="button button--primary"
            autoFocus
            onClick={() => void gameClient.confirmPendingBuild()}
          >
            Build
          </button>
          <button type="button" className="button button--ghost" onClick={cancel}>
            Cancel
          </button>
        </div>
      </div>
    </div>
  );
}

function ResourceList({ title, resources }: { title: string; resources: ResourceInfo[] }) {
  return (
    <div className="resource-list">
      <h3>{title}</h3>
      {resources.length === 0 ? (
        <p className="muted">Nothing</p>
      ) : (
        <ul>
          {resources.map((resource) => (
            <li key={resource.resourceType}>
              <span>{displayName(resource.resourceTypeName)}</span>
              <span className="resource-value">{formatPerHour(resource.value)} per hour</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
