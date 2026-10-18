import { useEffect, useState } from "react";
import { gameClient } from "../app/client";
import type { ReportedMessageDto } from "../api/contracts";
import { setState, toast, useAppStore } from "../state/store";

/** Moderation (GDD §19): reported chat messages; mute, ban, rename (admins only, checked by the server) */
export function ModerationPanel() {
  const open = useAppStore((s) => s.moderationOpen);
  const [reports, setReports] = useState<ReportedMessageDto[] | null>(null);
  const [company, setCompany] = useState("");
  const [name, setName] = useState("");
  const refresh = () => void gameClient.connection.getReports().then(setReports);
  useEffect(() => {
    if (open) {
      refresh();
    }
  }, [open]);
  if (!open) {
    return null;
  }
  const close = () => setState({ moderationOpen: false });
  const act = async (id: number, action: "dismiss" | "delete" | "mute" | "ban") => {
    const result = await gameClient.connection.moderateMessage(id, action);
    toast(
      result.success ? "Done" : (result.error ?? "That didn't work."),
      result.success ? "success" : "error",
    );
    refresh();
  };
  const rename = async () => {
    const result = await gameClient.connection.renameCompany(Number(company), name);
    toast(
      result.success ? "Renamed" : (result.error ?? "That didn't work."),
      result.success ? "success" : "error",
    );
  };
  return (
    <div className="modal-backdrop" onClick={close}>
      <div
        className="panel moderation-panel"
        role="dialog"
        aria-label="Moderation"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="panel-header">
          <h2>🛡 Moderation</h2>
          <button className="icon-button" title="Close (Esc)" onClick={close}>
            ×
          </button>
        </div>
        <div className="panel-title">Reported messages</div>
        {reports === null && <p className="muted">Loading…</p>}
        {reports?.length === 0 && <p className="muted">Nothing reported. 🎉</p>}
        {reports?.map((r) => (
          <div key={r.id} className="report-row">
            <div>
              <strong>{r.senderName}</strong>{" "}
              <span className="muted small">
                in {r.channel} · {new Date(r.sentAt).toLocaleString()}
              </span>
              <div className="report-text">{r.text}</div>
            </div>
            <div className="button-row">
              <button onClick={() => void act(r.id, "dismiss")}>Fine</button>
              <button onClick={() => void act(r.id, "delete")}>Delete</button>
              <button onClick={() => void act(r.id, "mute")}>Mute 24 h</button>
              <button className="danger" onClick={() => void act(r.id, "ban")}>
                Ban 7 days
              </button>
            </div>
          </div>
        ))}
        <div className="panel-title">Rename a company</div>
        <div className="rename-row">
          <input placeholder="Company id" value={company} onChange={(e) => setCompany(e.target.value)} />
          <input placeholder="New name" value={name} onChange={(e) => setName(e.target.value)} />
          <button disabled={company === "" || name.trim().length < 3} onClick={() => void rename()}>
            Rename
          </button>
        </div>
      </div>
    </div>
  );
}
