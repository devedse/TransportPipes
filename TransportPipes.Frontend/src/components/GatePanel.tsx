import { gameClient } from "../app/client";
import type { GatePolicy } from "../api/contracts";
import { setState, useAppStore } from "../state/store";

const POLICIES: Array<{ id: GatePolicy; name: string; text: string }> = [
  { id: "open", name: "Open", text: "Anyone in the corridor can connect and trade with you at your prices." },
  {
    id: "contract",
    name: "Contract only",
    text: "The pipe goes through, but only companies with a contract can trade.",
  },
  {
    id: "corporation",
    name: "Corporation only",
    text: "Only members of your corporation can trade through it.",
  },
  { id: "closed", name: "Closed", text: "Nothing goes through: your estate is cut off on this side." },
];

const SIDE_NAMES: Record<string, string> = { top: "north", right: "east", bottom: "south", left: "west" };

/** The policy of one of your gates */
export function GatePanel() {
  const side = useAppStore((state) => state.selectedGate);
  const company = useAppStore((state) => state.company);
  // Renders again when the district (with the gate policies) changes
  useAppStore((state) => state.districtVersion);
  const district =
    company === null ? undefined : gameClient.world?.district(company.districtX, company.districtY);
  if (side === null || company === null) {
    return null;
  }
  const estate = district?.estates.find((t) => t.slot === company.slot);
  const current = estate?.gatePolicies[side] ?? "open";
  return (
    <aside className="panel gate-panel">
      <div className="panel-header">
        <div>
          <h3>Gate · {SIDE_NAMES[side]}</h3>
          <div className="muted small">Where your pipes meet the corridor and your neighbours</div>
        </div>
        <button className="icon-button" title="Close (Esc)" onClick={() => setState({ selectedGate: null })}>
          ×
        </button>
      </div>
      <div className="policies">
        {POLICIES.map((policy) => (
          <button
            key={policy.id}
            className={policy.id === current ? "policy active" : "policy"}
            onClick={() => void gameClient.setGatePolicy(side, policy.id)}
          >
            <strong>{policy.name}</strong>
            <span className="small muted">{policy.text}</span>
          </button>
        ))}
      </div>
    </aside>
  );
}
