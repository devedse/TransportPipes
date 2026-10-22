import { useEffect, useRef, useState, type FormEvent } from "react";
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
        <h2>Keep your estate</h2>
        <p className="muted">
          You play as a guest: this browser remembers you. With a name and a password your estate is yours on
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
          {busy ? "One moment…" : "Keep my estate"}
        </button>
      </form>
    </div>
  );
}

/**
 * A friend's link opened by someone who already plays: join their crew (and move to the kept seat). A seat link is
 * remembered, so the seat stays on the player's return card until they answer (run 3: Ben)
 */
export function InviteInGame() {
  const invite = useAppStore((s) => s.invite);
  const phase = useAppStore((s) => s.phase);
  const seen = useRef<string | null>(null);
  useEffect(() => {
    if (invite !== null && phase === "playing" && invite.dto.seatCode && seen.current !== invite.code) {
      seen.current = invite.code;
      void gameClient.connection.sawSeatCode(invite.code, false).catch(() => undefined);
    }
  }, [invite, phase]);
  if (invite === null || phase !== "playing") {
    return null;
  }
  const dto = invite.dto;
  const close = () => gameClient.forgetInvite();
  return (
    <div className="modal-backdrop" onClick={close}>
      <div className="panel keep-panel" onClick={(e) => e.stopPropagation()}>
        <h2>
          {emblemIcon(dto.emblem)} {dto.from ?? "A friend"} invites you to {dto.crewName}
        </h2>
        <p>
          {dto.seatCode
            ? `They keep a seat for you in ${dto.station?.name ?? "their station"}: your estate moves there for free, with its layout.`
            : "Join their crew: you can move next to them afterwards."}
        </p>
        <span className="button-row">
          <button
            className="primary"
            onClick={() => {
              void gameClient.crewCommand(
                gameClient.connection.joinCrew(invite.code),
                dto.seatCode ? "On your way to your friends" : "Welcome to the crew",
              );
              close();
            }}
          >
            {dto.seatCode ? "Join and move" : "Join"}
          </button>
          <button onClick={close}>Not now</button>
          {dto.seatCode && (
            <button
              className="link"
              onClick={() => {
                void gameClient.connection.sawSeatCode(invite.code, true).catch(() => undefined);
                close();
              }}
            >
              Not with them
            </button>
          )}
        </span>
      </div>
    </div>
  );
}
