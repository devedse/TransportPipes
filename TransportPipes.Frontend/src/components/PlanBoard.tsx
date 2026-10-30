import { useState } from "react";
import { gameClient } from "../app/client";
import type { YardResidentDto } from "../api/contracts";
import { useAppStore } from "../state/store";
import { ResourceIcon } from "./ResourceIcon";

/** Your part of the Plan board (1.4 §7): pin what you make and need, one tap per good (the map's Yard card shows it) */
export function PlanBoard({ me }: { me: YardResidentDto }) {
  const content = useAppStore((s) => s.content)!;
  const era = useAppStore((s) => s.era);
  const [pin, setPin] = useState("");
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
    </section>
  );
}
