import { useState } from "react";
import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import { useNow } from "../app/useNow";
import type { YardResidentDto } from "../api/contracts";
import { useAppStore } from "../state/store";
import { ResourceIcon } from "./ResourceIcon";

/**
 * Your part of the Plan board and your Asks (v1.2 §12): pin what you make and need (the next player sees it on the
 * station's card), and ask the station for a good; your neighbours' spare goes there first, and at the deadline a
 * House brings the rest, so an Ask always closes
 */
export function PlanBoard({ me }: { me: YardResidentDto }) {
  const content = useAppStore((s) => s.content)!;
  const era = useAppStore((s) => s.era);
  const serverOffset = useAppStore((s) => s.serverOffsetMs);
  const now = useNow(30000) + serverOffset;
  const [pin, setPin] = useState("");
  const [ask, setAsk] = useState({ resource: "", amount: 500, hours: 72, renew: false });
  const goods = content.resources.filter((r) => r.era <= era);
  const name = (resource: string) => content.resourceName(resource).toLowerCase();
  const togglePin = (resource: string, need: boolean, on: boolean) =>
    void gameClient.yardCommand(gameClient.connection.setPin(resource, need, on));
  return (
    <section className="plan-board">
      <h4>📌 Plan board</h4>
      <div className="pins">
        {me.makes.map((r) => (
          <button
            key={`m${r}`}
            className="pin makes"
            title="Take the pin off"
            onClick={() => togglePin(r, false, false)}
          >
            <ResourceIcon resource={r} /> I make {name(r)} ×
          </button>
        ))}
        {me.needs.map((r) => (
          <button
            key={`n${r}`}
            className="pin needs"
            title="Take the pin off"
            onClick={() => togglePin(r, true, false)}
          >
            <ResourceIcon resource={r} /> I need {name(r)} ×
          </button>
        ))}
      </div>
      <span className="button-row">
        <select aria-label="Good to pin" value={pin} onChange={(e) => setPin(e.target.value)}>
          <option value="">Pin a good…</option>
          {goods.map((r) => (
            <option key={r.id} value={r.id}>
              {r.name}
            </option>
          ))}
        </select>
        <button disabled={pin === ""} onClick={() => togglePin(pin, false, true)}>
          I make it
        </button>
        <button disabled={pin === ""} onClick={() => togglePin(pin, true, true)}>
          I need it
        </button>
      </span>
      <h4>🙋 Ask the station</h4>
      {me.asks.map((a) => (
        <div key={a.resource} className="ask-row">
          <ResourceIcon resource={a.resource} />
          <span className="grow small">
            {formatAmount(a.got)} of {formatAmount(a.amount)} {name(a.resource)}
            {a.perMinute > 0 && ` · ${formatAmount(a.perMinute)}/min coming`} · a House brings the rest in{" "}
            {formatDuration(Math.max(0, (new Date(a.until).getTime() - now) / 1000))}
            {a.renew && " · renews itself"}
          </span>
          <button
            className="link small"
            onClick={() =>
              void gameClient.yardCommand(gameClient.connection.cancelAsk(a.resource), "Ask taken back")
            }
          >
            take back
          </button>
        </div>
      ))}
      <div className="ask-form">
        <select
          aria-label="Good to ask for"
          value={ask.resource}
          onChange={(e) => setAsk({ ...ask, resource: e.target.value })}
        >
          <option value="">Which good…</option>
          {goods.map((r) => (
            <option key={r.id} value={r.id}>
              {r.name}
            </option>
          ))}
        </select>
        <input
          type="number"
          aria-label="How much"
          min={1}
          value={ask.amount}
          onChange={(e) => setAsk({ ...ask, amount: Number(e.target.value) })}
        />
        <select
          aria-label="For how long"
          value={ask.hours}
          onChange={(e) => setAsk({ ...ask, hours: Number(e.target.value) })}
        >
          {[12, 24, 48, 72].map((h) => (
            <option key={h} value={h}>
              {h} h
            </option>
          ))}
        </select>
        <label className="check small">
          <input type="checkbox" checked={ask.renew} onChange={() => setAsk({ ...ask, renew: !ask.renew })} />
          renew it
        </label>
        <button
          className="primary"
          disabled={ask.resource === "" || ask.amount <= 0}
          onClick={() =>
            void gameClient
              .yardCommand(
                gameClient.connection.ask(ask.resource, ask.amount, ask.hours, ask.renew),
                "Asked: your neighbours' spare comes here first",
              )
              .then((ok) => ok && setAsk({ ...ask, resource: "" }))
          }
        >
          Ask
        </button>
      </div>
      <p className="muted small">
        It goes into your storages at the one player price; at the deadline a House brings what&apos;s left at
        2 × the hub&apos;s base price.
      </p>
    </section>
  );
}
