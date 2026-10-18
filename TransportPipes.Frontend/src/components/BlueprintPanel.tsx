import { useState } from "react";
import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import type { BlueprintDto, BlueprintSharing } from "../api/contracts";
import type { Content } from "../game/content";
import { setState, useAppStore } from "../state/store";

const CATEGORY_COLORS: Record<string, string> = {
  energy: "#f2c14e",
  extraction: "#b98b5e",
  processing: "#8f9bb3",
  logistics: "#6cb4ee",
  commerce: "#2ec4a6",
  company: "#c7a6ff",
  decoration: "#7fc96b",
  late: "#ef8f6c",
};

const SHARING_NAMES: Record<BlueprintSharing, string> = {
  private: "Only you",
  corporation: "Your corporation",
  board: "Blueprint board",
};

/** A top-down plan of a blueprint: pipes as thin squares, buildings in the colour of their category */
function BlueprintPreview({ blueprint, content }: { blueprint: BlueprintDto; content: Content }) {
  const side = Math.max(blueprint.width, blueprint.height);
  return (
    <svg
      className="blueprint-preview"
      viewBox={`-0.5 -0.5 ${side + 1} ${side + 1}`}
      role="img"
      aria-label={`Plan of ${blueprint.name}`}
    >
      <rect x={-0.5} y={-0.5} width={side + 1} height={side + 1} className="blueprint-paper" />
      <rect x={0} y={0} width={blueprint.width} height={blueprint.height} className="blueprint-outline" />
      {blueprint.pipes.map((t) => (
        <rect
          key={`p${t.dx},${t.dy}`}
          x={t.dx + 0.3}
          y={t.dy + 0.3}
          width={0.4}
          height={0.4}
          fill="#cfe6ff"
        />
      ))}
      {blueprint.buildings.map((t) => {
        const definition = content.building(t.type);
        const size = definition?.size ?? 1;
        return (
          <rect
            key={`b${t.dx},${t.dy}`}
            x={t.dx + 0.08}
            y={t.dy + 0.08}
            width={size - 0.16}
            height={size - 0.16}
            rx={0.15}
            fill={CATEGORY_COLORS[definition?.category ?? ""] ?? "#ddd"}
          >
            <title>{definition?.name ?? t.type}</title>
          </rect>
        );
      })}
    </svg>
  );
}

function Stars({ blueprint }: { blueprint: BlueprintDto }) {
  return (
    <span className="stars" aria-label="Rate it">
      {[1, 2, 3, 4, 5].map((n) => (
        <button
          key={n}
          className={(blueprint.myRating ?? 0) >= n ? "star on" : "star"}
          title={`${n} ${n === 1 ? "star" : "stars"}`}
          onClick={() => void gameClient.rateBlueprint(blueprint.id, n)}
        >
          ★
        </button>
      ))}
    </span>
  );
}

function BlueprintCard({
  blueprint,
  canBuild,
  mine,
}: {
  blueprint: BlueprintDto;
  canBuild: boolean;
  mine: boolean;
}) {
  const content = useAppStore((s) => s.content);
  const gold = useAppStore((s) => s.company?.gold ?? 0);
  if (content === null) {
    return null;
  }
  const counts = new Map<string, number>();
  for (const item of blueprint.buildings) {
    counts.set(item.type, (counts.get(item.type) ?? 0) + 1);
  }
  const summary = [...counts.entries()]
    .sort((a, b) => b[1] - a[1])
    .map(([type, n]) => `${n}× ${content.building(type)?.name ?? type}`)
    .join(", ");
  return (
    <div className="blueprint-card">
      <BlueprintPreview blueprint={blueprint} content={content} />
      <div className="blueprint-body">
        <strong>{blueprint.name}</strong>
        <span className="small muted">
          {blueprint.width}×{blueprint.height} tiles · {blueprint.pipes.length} pipe tiles
        </span>
        <span className="small" title={summary}>
          {summary}
        </span>
        {!mine && (
          <span className="small muted">
            by <span className="company-dot" style={{ background: blueprint.ownerColor }} />{" "}
            {blueprint.ownerName}
          </span>
        )}
        <span className="small">
          {blueprint.ratings > 0
            ? `★ ${blueprint.rating.toFixed(1)} (${blueprint.ratings} ${blueprint.ratings === 1 ? "rating" : "ratings"})`
            : "Not rated yet"}
        </span>
        <div className="blueprint-actions">
          <button
            className="primary"
            disabled={!canBuild}
            title={canBuild ? "Pick a spot on your estate" : "Needs a Blueprint Office"}
            onClick={() => gameClient.holdBlueprint(blueprint)}
          >
            Build ·{" "}
            <span className={gold < blueprint.cost ? "bad" : ""}>{formatAmount(blueprint.cost)} g</span>
          </button>
          {mine ? (
            <>
              <select
                value={blueprint.sharing}
                aria-label="Shared with"
                onChange={(e) =>
                  void gameClient.shareBlueprint(blueprint.id, e.target.value as BlueprintSharing)
                }
              >
                {(Object.keys(SHARING_NAMES) as BlueprintSharing[]).map((t) => (
                  <option key={t} value={t}>
                    {SHARING_NAMES[t]}
                  </option>
                ))}
              </select>
              <button
                className="danger"
                title="Delete this blueprint"
                onClick={() => {
                  if (window.confirm(`Delete the blueprint "${blueprint.name}"?`)) {
                    void gameClient.deleteBlueprint(blueprint.id);
                  }
                }}
              >
                🗑
              </button>
            </>
          ) : (
            <Stars blueprint={blueprint} />
          )}
        </div>
      </div>
    </div>
  );
}

/** Saved layouts: your own, your corporation's and the hub's blueprint board (GDD §11.1) */
export function BlueprintPanel() {
  const open = useAppStore((s) => s.blueprintsOpen);
  const blueprints = useAppStore((s) => s.blueprints);
  const [tab, setTab] = useState<"mine" | "corporation" | "board">("mine");
  if (!open) {
    return null;
  }
  const close = () => setState({ blueprintsOpen: false });
  const list = blueprints === null ? [] : blueprints[tab];
  return (
    <div className="modal-backdrop" onClick={close}>
      <div
        className="panel blueprint-panel"
        role="dialog"
        aria-label="Blueprints"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="panel-header">
          <h2>📐 Blueprints</h2>
          <button className="icon-button" title="Close (Esc)" onClick={close}>
            ×
          </button>
        </div>
        {blueprints === null ? (
          <p className="muted">Loading…</p>
        ) : (
          <>
            <div className="blueprint-intro">
              {blueprints.hasOffice ? (
                <>
                  <p className="muted small">
                    Save part of your estate and build it again anywhere in one go, at the normal costs.
                  </p>
                  <button
                    className="primary"
                    disabled={blueprints.mine.length >= blueprints.max}
                    onClick={() => gameClient.startBlueprintArea()}
                  >
                    ✚ Save an area
                  </button>
                </>
              ) : (
                <p className="muted small">
                  🔒 Build a Blueprint Office (Company Level 22) to save, share and build blueprints.
                  {blueprints.canRebuild &&
                    " After your Franchise you can rebuild your own blueprints right away."}
                </p>
              )}
            </div>
            <div className="tabs">
              <button className={tab === "mine" ? "tab active" : "tab"} onClick={() => setTab("mine")}>
                Yours {blueprints.mine.length}/{blueprints.max}
              </button>
              <button
                className={tab === "corporation" ? "tab active" : "tab"}
                onClick={() => setTab("corporation")}
              >
                Corporation {blueprints.corporation.length > 0 && blueprints.corporation.length}
              </button>
              <button className={tab === "board" ? "tab active" : "tab"} onClick={() => setTab("board")}>
                Board {blueprints.board.length > 0 && blueprints.board.length}
              </button>
            </div>
            <div className="blueprint-list">
              {list.length === 0 && (
                <p className="muted">
                  {tab === "mine"
                    ? "No blueprints yet. Save an area of your estate that works well."
                    : tab === "corporation"
                      ? "Nobody in your corporation shared a blueprint yet."
                      : "The board is empty: share one of yours!"}
                </p>
              )}
              {list.map((t) => (
                <BlueprintCard
                  key={t.id}
                  blueprint={t}
                  mine={tab === "mine"}
                  canBuild={blueprints.hasOffice || (tab === "mine" && blueprints.canRebuild)}
                />
              ))}
            </div>
          </>
        )}
      </div>
    </div>
  );
}

/** Names the area the player dragged */
export function BlueprintSaveDialog() {
  const draft = useAppStore((s) => s.blueprintDraft);
  const [name, setName] = useState("");
  if (draft === null) {
    return null;
  }
  const cancel = () => setState({ blueprintDraft: null });
  const valid = name.trim().length >= 3 && name.trim().length <= 30;
  return (
    <div className="modal-backdrop" onClick={cancel}>
      <form
        className="panel blueprint-save"
        role="dialog"
        aria-label="Save a blueprint"
        onClick={(e) => e.stopPropagation()}
        onSubmit={(e) => {
          e.preventDefault();
          if (valid) {
            void gameClient.saveBlueprint(name).then(() => setName(""));
          }
        }}
      >
        <h2>📐 Save a blueprint</h2>
        <p className="muted small">
          {draft.width}×{draft.height} tiles: your buildings and pipes inside it
          {(draft.width > 32 || draft.height > 32) && <span className="bad"> (at most 32×32)</span>}
        </p>
        <input
          autoFocus
          maxLength={30}
          placeholder="Name, e.g. Steel line"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
        <div className="row">
          <button type="button" onClick={cancel}>
            Cancel
          </button>
          <button type="submit" className="primary" disabled={!valid}>
            Save
          </button>
        </div>
      </form>
    </div>
  );
}
