import { formatAmount, formatRate } from "../app/format";
import type { NetworkDetailsDto } from "../api/contracts";
import type { Content } from "../game/content";
import { ResourceIcon } from "./ResourceIcon";

/**
 * What a network does per minute: every resource made, used, sold and lost, and per company what its buildings make
 * and use (open a company for its resources)
 */
export function NetworkSummary({
  details,
  content,
  ownCompany,
}: {
  details: NetworkDetailsDto;
  content: Content;
  ownCompany: number | null;
}) {
  return (
    <>
      {details.resources.length > 0 && (
        <table className="network-table">
          <thead>
            <tr>
              <th />
              <th title="Made by the buildings (and bought or taken out of storage)">out</th>
              <th title="Used by the buildings">in</th>
              <th title="Sold to the hub">sold</th>
              <th title="Lost on the way through the pipes">lost</th>
            </tr>
          </thead>
          <tbody>
            {details.resources.map((r) => {
              const made = r.produced + r.imported + r.fromStorage;
              return (
                <tr key={r.resource}>
                  <td>
                    <ResourceIcon resource={r.resource} /> {content.resourceName(r.resource)}
                  </td>
                  <td>{formatAmount(made)}</td>
                  <td>{formatAmount(r.used)}</td>
                  <td>{formatAmount(r.sold)}</td>
                  <td className={r.lost > 0.05 * made ? "warning" : ""}>{formatAmount(r.lost)}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
      <div className="network-owners">
        {details.owners.map((owner) => (
          <details
            key={owner.id}
            className="network-owner"
            open={details.owners.length === 1 ? false : owner.id === ownCompany}
          >
            <summary>
              <span className="company-dot" style={{ background: owner.color }} />
              <span className="network-owner-name">{owner.name}</span>
              <span className="flow-amount">{formatRate(owner.goldPerMinute)} g</span>
            </summary>
            {owner.resources.length === 0 ? (
              <p className="muted small">Nothing made or used here.</p>
            ) : (
              <ul className="flow-list">
                {owner.resources.map((r) => (
                  <li key={r.resource}>
                    <ResourceIcon resource={r.resource} />
                    <span>{content.resourceName(r.resource)}</span>
                    <span className="flow-amount">
                      {r.made > 0.005 && <>out {formatAmount(r.made)} </>}
                      {r.used > 0.005 && <>in {formatAmount(r.used)} </>}
                    </span>
                    <span className="muted flow-kind">
                      {r.sold > 0.005 ? `${formatAmount(r.sold)} sold` : ""}
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </details>
        ))}
      </div>
    </>
  );
}
