import { useState } from "react";
import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import type { LinesDto, TrunkLineDto } from "../api/contracts";
import type { Content } from "../game/content";
import { useAppStore } from "../state/store";
import { ResourceIcon } from "./ResourceIcon";

const percent = (share: number) => `${Math.round(share * 1000) / 10}%`;

/** A carrier's fee field: a share of the goods' base value, 0 to 10% (a player's own fee may be left empty) */
function FeeInput({
  line,
  who,
  value,
  label,
}: {
  line: number;
  who: string;
  value: number | null;
  label?: string;
}) {
  const [typed, setTyped] = useState(value === null ? "" : String(Math.round(value * 1000) / 10));
  return (
    <input
      type="number"
      min={0}
      max={10}
      step={0.5}
      value={typed}
      placeholder="—"
      aria-label={label ?? `${who} fee in percent`}
      onChange={(e) => setTyped(e.target.value)}
      onBlur={() => void gameClient.setLineFee(line, who, typed.trim() === "" ? null : Number(typed) / 100)}
    />
  );
}

/** A carrier's fees of their own for players of the two towns (a friend for free, say); empty: the public or crew fee */
function SpecialFees({ line }: { line: TrunkLineDto }) {
  if (line.players.length === 0) {
    return null;
  }
  return (
    <details className="small">
      <summary>
        Fees of their own for players ({line.players.filter((t) => t.fee !== null).length} set)
      </summary>
      <ul className="line-players">
        {line.players.map((player) => (
          <li key={player.id}>
            <span className="dot" style={{ background: player.color }} /> {player.name}{" "}
            <FeeInput
              line={line.id}
              who={String(player.id)}
              value={player.fee}
              label={`${player.name}'s fee in percent`}
            />{" "}
            %
          </li>
        ))}
      </ul>
    </details>
  );
}

/** One line: its carrier and towns, what arrives, its fees; a carrier sets them, anyone sends goods to the other market */
function LineCard({ line, lines, content }: { line: TrunkLineDto; lines: LinesDto; content: Content }) {
  const [resource, setResource] = useState("ironIngot");
  const exports = Object.entries(lines.exports).filter(([, id]) => id === line.id);
  const pipe = content.pipeTiers.find((t) => t.tier === line.tier);
  return (
    <li className="link-card">
      <div className="link-head">
        <span className="dot" style={{ background: line.carrierColor }} />
        <strong>
          {line.from} ⇄ {line.to}
        </strong>
        <span className="muted small grow">
          {pipe?.name ?? `tier ${line.tier}`} · {line.length} tiles · {percent(line.efficiency)} arrives ·
          carries up to {formatAmount(line.throughput)}/min
        </span>
      </div>
      <p className="small">
        {line.yours ? (
          <>
            You are its carrier: it earned you {formatAmount(line.earned)} g in fees. Fees, of the goods&apos;
            base value: anyone <FeeInput line={line.id} who="public" value={line.publicFee} /> % · your crew{" "}
            <FeeInput line={line.id} who="crew" value={line.crewFee} /> %
          </>
        ) : (
          <>
            Carrier {line.carrier}: you pay {percent(line.yourFee)} of what crosses it
          </>
        )}
      </p>
      {line.yours && <SpecialFees line={line} />}
      <div className="small">
        To {line.to}&apos;s market (your Hub Order there is half of yours here):{" "}
        {exports.map(([good]) => (
          <button
            key={good}
            className="link small"
            title="Stop sending it"
            onClick={() => void gameClient.setExport(good, null)}
          >
            <ResourceIcon resource={good} /> {content.resourceName(good).toLowerCase()} ×
          </button>
        ))}
        <select value={resource} onChange={(e) => setResource(e.target.value)} aria-label="Good to send">
          {content.resources
            .filter((r) => r.id !== "energy")
            .map((r) => (
              <option key={r.id} value={r.id}>
                {r.name}
              </option>
            ))}
        </select>
        <button className="small" onClick={() => void gameClient.setExport(resource, line.id)}>
          Send it there
        </button>
      </div>
    </li>
  );
}

/**
 * Trunk lines (1.4 §20): from Era 3 a line runs from your town's gate through the wilderness to a neighbouring town,
 * and its builder is its carrier. Links to players there run over it, and it is a second market.
 */
export function TrunkLines() {
  const lines = useAppStore((s) => s.lines);
  const content = useAppStore((s) => s.content);
  const gold = useAppStore((s) => s.company?.gold ?? 0);
  if (lines === null || content === null) {
    return null;
  }
  return (
    <section>
      <h4>Trunk lines to other towns</h4>
      <p className="muted small">
        From Era 3 anyone who researched Trunk Lines builds a line from a gate of your town to a neighbouring
        town, buys its tiles once and is its carrier: links to players there run over it, and anyone may send
        goods to that town&apos;s market, paying the carrier&apos;s fee.
      </p>
      {lines.lines.length > 0 && (
        <ul className="link-list">
          {lines.lines.map((line) => (
            <LineCard key={line.id} line={line} lines={lines} content={content} />
          ))}
        </ul>
      )}
      {lines.options.map((option) => (
        <div key={`${option.x},${option.y}`} className="small line-option">
          <strong>{option.name}</strong> <span className="muted">· Era {option.era}</span>{" "}
          {option.error !== null ? (
            <span className="muted">· {option.error}</span>
          ) : (
            <button
              className="small primary"
              disabled={gold < option.gold}
              onClick={() => void gameClient.buildLine(option.x, option.y, option.tier)}
            >
              Build a {content.pipeTiers.find((t) => t.tier === option.tier)?.name ?? "line"} there ·{" "}
              {option.length} tiles · {formatAmount(option.gold)} g
            </button>
          )}
        </div>
      ))}
    </section>
  );
}
