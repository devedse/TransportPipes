import { useState, type FormEvent } from "react";
import { gameClient } from "../app/client";
import { emblemIcon } from "../app/crews";
import { setState, useAppStore } from "../state/store";

/** A guest keeps the estate (v1.2 §5): a name and a password of its own, so it can log in anywhere */
export function KeepEstate() {
  const open = useAppStore((s) => s.keepOpen);
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [email, setEmail] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  if (!open) {
    return null;
  }
  const close = () => setState({ keepOpen: false });
  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError(
      await gameClient.keepEstate(userName, password, email).catch((reason: unknown) => String(reason)),
    );
    setBusy(false);
  };
  return (
    <div className="modal-backdrop" onClick={close}>
      <form
        className="panel keep-panel"
        onSubmit={(e) => void submit(e)}
        onClick={(e) => e.stopPropagation()}
      >
        <h2>Keep your plot</h2>
        <p className="muted">
          You play as a guest: this browser remembers you. With a name and a password your plot is yours on
          any device.
        </p>
        <label>
          User name
          <input
            autoComplete="username"
            value={userName}
            onChange={(e) => setUserName(e.target.value)}
            required
            minLength={3}
            maxLength={24}
          />
        </label>
        <label>
          Password
          <input
            type="password"
            autoComplete="new-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            minLength={6}
          />
        </label>
        <label>
          E-mail <span className="muted">(optional)</span>
          <input type="email" autoComplete="email" value={email} onChange={(e) => setEmail(e.target.value)} />
        </label>
        {error !== null && (
          <p className="form-error" role="alert">
            {error}
          </p>
        )}
        <button className="primary" type="submit" disabled={busy}>
          {busy ? "One moment…" : "Keep my plot"}
        </button>
      </form>
    </div>
  );
}

/**
 * A friend's "Play next to me" link opened by someone who already plays (1.4 §6): move next to them, on the map picker
 * at their Yard (their link lets you in although it's closed)
 */
export function InviteInGame() {
  const invite = useAppStore((s) => s.invite);
  const phase = useAppStore((s) => s.phase);
  const picking = useAppStore((s) => s.picker !== null);
  const company = useAppStore((s) => s.company);
  if (invite === null || phase !== "playing" || picking) {
    return null;
  }
  const dto = invite.dto;
  const yard = dto.yard;
  const close = () => gameClient.forgetInvite();
  const home =
    yard !== null &&
    company !== null &&
    company.districtX === yard.districtX &&
    company.districtY === yard.districtY &&
    Math.floor(company.slot / 4) === yard.index;
  return (
    <div className="modal-backdrop" onClick={close}>
      <div className="panel keep-panel" onClick={(e) => e.stopPropagation()}>
        <h2>
          {yard !== null && yard.emblem !== null ? `${emblemIcon(yard.emblem)} ` : ""}
          {dto.from ?? "A friend"} plays at {yard?.name ?? "their Yard"}
        </h2>
        <p>
          {home
            ? "You live there already."
            : "Move next to them: your plot lifts off with its layout and lands beside theirs."}
        </p>
        <span className="button-row">
          {!home && (
            <button className="primary" onClick={() => gameClient.openPicker()}>
              Show me their Yard
            </button>
          )}
          <button onClick={close}>{home ? "Close" : "Not now"}</button>
        </span>
      </div>
    </div>
  );
}
