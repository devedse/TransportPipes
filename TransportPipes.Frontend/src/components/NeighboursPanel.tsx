import { useState, type FormEvent } from "react";
import { gameClient } from "../app/client";
import { formatAmount, formatDuration } from "../app/format";
import { useNow } from "../app/useNow";
import type { ContractDto, NeighbourDto } from "../api/contracts";
import type { Content } from "../game/content";
import { setState, useAppStore } from "../state/store";
import { ResourceIcon } from "./ResourceIcon";
import { TradeSettings } from "./TradeSettings";

function Stars({ count }: { count: number }) {
  return (
    <span className="stars" title={`Contract reputation: ${count} of 5`}>
      {"★".repeat(count)}
      <span className="muted">{"★".repeat(5 - count)}</span>
    </span>
  );
}

function Chips({
  rates,
  content,
  kind,
}: {
  rates: NeighbourDto["surplus"];
  content: Content;
  kind: "surplus" | "needs";
}) {
  if (rates.length === 0) {
    return <span className="muted small">—</span>;
  }
  return (
    <span className="chips">
      {rates.slice(0, 4).map((r) => (
        <span
          key={r.resource}
          className={`res-chip ${kind}`}
          style={{ borderColor: content.resourceColor(r.resource) }}
        >
          <ResourceIcon resource={r.resource} /> {formatAmount(r.perMinute)}{" "}
          {content.resourceName(r.resource).toLowerCase()}
        </span>
      ))}
    </span>
  );
}

interface Draft {
  partner: number | null;
  selling: boolean;
  resource: string;
  rate: number;
  price: number;
  days: number;
}

function ContractForm({ draft, onDone }: { draft: Draft; onDone: () => void }) {
  const content = useAppStore((s) => s.content)!;
  const neighbours = useAppStore((s) => s.neighbours);
  const company = useAppStore((s) => s.company);
  const [value, setValue] = useState(draft);
  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (
      await gameClient.proposeContract(
        value.partner,
        value.selling,
        value.resource,
        value.rate,
        value.price,
        value.days,
      )
    ) {
      onDone();
    }
  };
  const base = content.resource(value.resource)?.price ?? 1;
  return (
    <form className="contract-form" onSubmit={(event) => void submit(event)}>
      <div className="form-row">
        <label>
          I want to
          <select
            value={value.selling ? "sell" : "buy"}
            onChange={(e) => setValue({ ...value, selling: e.target.value === "sell" })}
          >
            <option value="sell">deliver</option>
            <option value="buy">receive</option>
          </select>
        </label>
        <label>
          Resource
          <select
            value={value.resource}
            onChange={(e) => {
              const price = (content.resource(e.target.value)?.price ?? 1) * 1.75;
              setValue({ ...value, resource: e.target.value, price: Math.round(price * 100) / 100 });
            }}
          >
            {content.resources
              .filter((r) => r.era <= 2)
              .map((r) => (
                <option key={r.id} value={r.id}>
                  {r.name}
                </option>
              ))}
          </select>
        </label>
        <label>
          With
          <select
            value={value.partner ?? ""}
            onChange={(e) =>
              setValue({ ...value, partner: e.target.value === "" ? null : Number(e.target.value) })
            }
          >
            <option value="">Anyone (contract board)</option>
            {neighbours.map((n) => (
              <option key={n.id} value={n.id}>
                {n.name}
              </option>
            ))}
          </select>
        </label>
      </div>
      <div className="form-row">
        <label>
          Per minute
          <input
            type="number"
            min={1}
            step={1}
            value={value.rate}
            onChange={(e) => setValue({ ...value, rate: Number(e.target.value) })}
          />
        </label>
        <label>
          Price (g)
          <input
            type="number"
            min={0}
            step={0.01}
            value={value.price}
            onChange={(e) => setValue({ ...value, price: Number(e.target.value) })}
          />
        </label>
        <label>
          Days
          <input
            type="number"
            min={1}
            max={company?.researched.includes("futures") ? 30 : 7}
            step={1}
            value={value.days}
            onChange={(e) => setValue({ ...value, days: Number(e.target.value) })}
          />
        </label>
      </div>
      <p className="muted small">
        Worth {formatAmount(value.rate * value.price * 60 * 24 * value.days)} gold. The hub pays{" "}
        {base.toFixed(2)} g and charges {(base * 2.5).toFixed(2)} g. Missing deliveries cost the seller 5% and
        contract reputation.
      </p>
      <div className="button-row">
        <button className="primary" type="submit">
          Offer
        </button>
        <button type="button" onClick={onDone}>
          Cancel
        </button>
      </div>
    </form>
  );
}

function ContractRow({ contract, me, board }: { contract: ContractDto; me: number; board?: boolean }) {
  const content = useAppStore((s) => s.content)!;
  const serverOffset = useAppStore((s) => s.serverOffsetMs);
  const now = useNow();
  const selling = contract.seller === me;
  const partner = selling ? contract.buyerName : contract.sellerName;
  const fulfillment = contract.expected > 0 ? Math.min(1, contract.delivered / contract.expected) : 1;
  const toMe = contract.state === "offered" && contract.proposer !== me && !board;
  return (
    <div className={`contract ${contract.state}`}>
      <div>
        <div className="contract-title">
          {board ? (contract.seller === 0 ? "Buys" : "Sells") : selling ? "You deliver" : "You receive"}{" "}
          <strong>
            {formatAmount(contract.rate)} {content.resourceName(contract.resource).toLowerCase()}/min
          </strong>{" "}
          at {contract.price.toFixed(2)} g
        </div>
        <div className="small muted">
          {board ? (
            <>
              {contract.proposerName} <Stars count={contract.proposerStars} />
            </>
          ) : (
            partner || "anyone in the district"
          )}{" "}
          · {contract.days} days · {contract.state}
          {contract.state === "active" && contract.endsAt !== null && (
            <>
              {" "}
              ·{" "}
              {formatDuration(
                Math.max(0, (new Date(contract.endsAt).getTime() - (now + serverOffset)) / 1000),
              )}{" "}
              left
            </>
          )}
        </div>
        {contract.state === "active" && (
          <div className="meter-bar" title={`${Math.round(fulfillment * 100)}% delivered`}>
            <div
              className="meter-fill"
              style={{
                width: `${fulfillment * 100}%`,
                background: fulfillment > 0.95 ? "#2ec4a6" : "#f4a261",
              }}
            />
          </div>
        )}
      </div>
      <div className="button-row">
        {(toMe || board) && (
          <button className="primary" onClick={() => void gameClient.acceptContract(contract.id)}>
            Accept
          </button>
        )}
        {contract.state === "offered" && !board && (
          <button onClick={() => void gameClient.declineContract(contract.id)}>
            {toMe ? "Decline" : "Withdraw"}
          </button>
        )}
      </div>
    </div>
  );
}

/** The companies around you, your contracts and the contract board */
export function NeighboursPanel() {
  const open = useAppStore((s) => s.neighboursOpen);
  const neighbours = useAppStore((s) => s.neighbours);
  const contracts = useAppStore((s) => s.contracts);
  const content = useAppStore((s) => s.content);
  const company = useAppStore((s) => s.company);
  const tab = useAppStore((s) => s.neighboursTab);
  const setTab = (neighboursTab: typeof tab) => {
    setState({ neighboursTab });
    if (neighboursTab === "trade") {
      void gameClient.refreshMarket();
    }
  };
  const [draft, setDraft] = useState<Draft | null>(null);
  if (!open || content === null || company === null) {
    return null;
  }
  const close = () => setState({ neighboursOpen: false });
  const offersToMe =
    contracts?.mine.filter((t) => t.state === "offered" && t.proposer !== company.id).length ?? 0;
  const newDraft = (partner: number | null, resource = "energy", selling = true) =>
    setDraft({
      partner,
      selling,
      resource,
      rate: 10,
      price: Math.round((content.resource(resource)?.price ?? 1) * 175) / 100,
      days: 3,
    });

  return (
    <div className="modal-backdrop" onClick={close}>
      <div
        className="panel neighbours-panel"
        role="dialog"
        aria-label="Neighbours"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="panel-header">
          <h2>Neighbourhood</h2>
          <span className="muted small">
            Trade with the companies around you: connect a pipe, or agree a contract.
          </span>
          <button className="icon-button" title="Close (Esc)" onClick={close}>
            ×
          </button>
        </div>
        <div className="tabs">
          <button
            className={tab === "neighbours" ? "tab active" : "tab"}
            onClick={() => setTab("neighbours")}
          >
            Neighbours ({neighbours.length})
          </button>
          <button className={tab === "contracts" ? "tab active" : "tab"} onClick={() => setTab("contracts")}>
            Your contracts {offersToMe > 0 && <span className="badge">{offersToMe}</span>}
          </button>
          <button className={tab === "board" ? "tab active" : "tab"} onClick={() => setTab("board")}>
            Contract board ({contracts?.board.length ?? 0})
          </button>
          <button className={tab === "trade" ? "tab active" : "tab"} onClick={() => setTab("trade")}>
            ⚙ Trade settings
          </button>
        </div>

        {draft !== null && <ContractForm draft={draft} onDone={() => setDraft(null)} />}

        {tab === "neighbours" && (
          <div className="neighbour-list">
            {neighbours.length === 0 && <p className="muted">Nobody else in your district yet.</p>}
            {neighbours.map((n) => (
              <div key={n.id} className="neighbour">
                <span className="company-dot" style={{ background: n.color }} />
                <div className="neighbour-body">
                  <div>
                    <strong>{n.name}</strong> <span className="muted small">Lv {n.level}</span>{" "}
                    <Stars count={n.contractStars} />
                    {n.isNpc && <span className="badge npc">tenant</span>}
                    {n.connected && <span className="badge connected">connected</span>}
                  </div>
                  <div className="small">
                    <span className="muted">Spare:</span>{" "}
                    <Chips rates={n.surplus} content={content} kind="surplus" />
                  </div>
                  <div className="small">
                    <span className="muted">Needs:</span>{" "}
                    <Chips rates={n.needs} content={content} kind="needs" />
                  </div>
                  {(n.youEarn > 0 || n.youPay > 0) && (
                    <div className="small">
                      {n.youEarn > 0 && (
                        <span className="gold-text">you earn +{formatAmount(n.youEarn)} g/min </span>
                      )}
                      {n.youPay > 0 && <span className="muted">you pay {formatAmount(n.youPay)} g/min</span>}
                    </div>
                  )}
                </div>
                <div className="neighbour-actions">
                  {!n.connected && (
                    <button onClick={() => void gameClient.planConnection(n.id)}>Connect</button>
                  )}
                  <span className="button-row">
                    <button
                      title="Visit their estate"
                      onClick={() => void gameClient.visitEstate(n.id, true)}
                    >
                      Visit
                    </button>
                    {!n.isNpc && (
                      <button
                        title="Send a message"
                        onClick={() => void gameClient.messageCompany(n.id, n.name)}
                      >
                        ✉
                      </button>
                    )}
                  </span>
                  <button
                    onClick={() =>
                      newDraft(
                        n.id,
                        n.needs[0]?.resource ?? n.surplus[0]?.resource ?? "energy",
                        n.needs.length > 0,
                      )
                    }
                  >
                    Offer contract
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}

        {tab === "contracts" && (
          <div className="contract-list">
            <button onClick={() => newDraft(null)}>New offer…</button>
            {(contracts?.mine.length ?? 0) === 0 && <p className="muted">No contracts yet.</p>}
            {contracts?.mine.map((c) => (
              <ContractRow key={c.id} contract={c} me={company.id} />
            ))}
          </div>
        )}

        {tab === "trade" && <TradeSettings />}

        {tab === "board" && (
          <div className="contract-list">
            <button onClick={() => newDraft(null)}>Post an offer…</button>
            {(contracts?.board.length ?? 0) === 0 && <p className="muted">No offers on the board.</p>}
            {contracts?.board.map((c) => (
              <ContractRow key={c.id} contract={c} me={company.id} board />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

/** The pipe that Connect planned, with its cost: build it or drop it */
export function PlannedRouteBanner() {
  const route = useAppStore((s) => s.plannedRoute);
  const company = useAppStore((s) => s.company);
  const content = useAppStore((s) => s.content);
  const partner = useAppStore((s) => s.neighbours.find((t) => t.id === s.plannedPartner));
  if (route === null || company === null || content === null || gameClient.world === null) {
    return null;
  }
  const tiles = route.filter((t) => gameClient.world?.conductorAt(t.x, t.y)?.owner !== company.id);
  const gold = tiles.length * (content.pipeTiers[0]?.gold ?? 2);
  return (
    <div className="panel route-banner">
      <span>
        Pipe to {partner?.name ?? "your neighbour"}: {tiles.length} tiles, {formatAmount(gold)} g
      </span>
      <button className="primary" onClick={() => void gameClient.buildPlannedRoute()}>
        Build it
      </button>
      <button onClick={() => setState({ plannedRoute: null, plannedPartner: null })}>Cancel</button>
    </div>
  );
}
