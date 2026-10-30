import { useState } from "react";
import { gameClient } from "../app/client";
import { formatAmount } from "../app/format";
import { useAppStore } from "../state/store";
import { ResourceIcon } from "./ResourceIcon";

/**
 * Building in the Yard (1.4 §7): your workshops at its ends, and the crew's Depot. Put goods in for the crew: a
 * neighbour's levels and new buildings take them at your crew price, and the Wonder takes them as gifts.
 */
export function DepotSection() {
  const company = useAppStore((s) => s.company);
  const content = useAppStore((s) => s.content);
  const [resource, setResource] = useState("planks");
  const [amount, setAmount] = useState(100);
  if (company === null || content === null || company.workshops === null) {
    return null;
  }
  const { workshops } = company;
  const depot = workshops.depot;
  const open = company.level >= workshops.level;
  return (
    <section className="depot-section">
      <h4>Building in the Yard</h4>
      {!open ? (
        <p className="muted small">
          From Company Level {workshops.level} you build at the Yard&apos;s ends: up to {workshops.tiles}{" "}
          tiles of your own producers and Storages, joined to your Yard gate without pipes, and the
          crew&apos;s Depot.
        </p>
      ) : (
        <p className="small">
          Your workshops cover {workshops.tilesUsed} of your {workshops.tiles} tiles at the Yard&apos;s ends:
          they trade through your Yard gate without pipes, and go along when you move.
        </p>
      )}
      {open && depot === null && (
        <p className="muted small">
          No Depot yet: build a Yard Depot at one of the Yard&apos;s ends (Logistics) to keep goods for the
          crew.
        </p>
      )}
      {depot !== null && (
        <>
          <div
            className="meter-bar"
            title={`${formatAmount(depot.holds)} of ${formatAmount(depot.capacity)}`}
          >
            <div
              className="meter-fill"
              style={{ width: `${Math.min(100, (depot.holds / depot.capacity) * 100)}%` }}
            />
          </div>
          <p className="small">
            The Depot holds {formatAmount(depot.holds)} of {formatAmount(depot.capacity)}. Your
            neighbours&apos; levels and buildings take what you put in at your crew price; the Wonder takes it
            as your gift.
          </p>
          {depot.goods.length > 0 && (
            <ul className="flow-list">
              {depot.goods.map((good) => (
                <li key={`${good.owner}-${good.resource}`}>
                  <ResourceIcon resource={good.resource} />
                  <span>
                    {content.resourceName(good.resource)}{" "}
                    <span className="muted">· {good.yours ? "yours" : good.ownerName}</span>
                  </span>
                  <span className="flow-amount">
                    {formatAmount(good.amount)}
                    {good.yours && (
                      <button
                        className="link small"
                        onClick={() => void gameClient.takeFromDepot(good.resource, good.amount)}
                      >
                        take back
                      </button>
                    )}
                  </span>
                </li>
              ))}
            </ul>
          )}
          <form
            className="form-row depot-form"
            onSubmit={(event) => {
              event.preventDefault();
              void gameClient.putInDepot(resource, amount);
            }}
          >
            <label>
              Good
              <select value={resource} onChange={(e) => setResource(e.target.value)}>
                {content.resources
                  .filter((r) => r.era <= company.yourEra && r.id !== "energy")
                  .map((r) => (
                    <option key={r.id} value={r.id}>
                      {r.name}
                    </option>
                  ))}
              </select>
            </label>
            <label>
              Amount
              <input
                type="number"
                min={1}
                value={amount}
                onChange={(e) => setAmount(Number(e.target.value))}
              />
            </label>
            <button className="primary" type="submit">
              Put in
            </button>
          </form>
        </>
      )}
    </section>
  );
}
