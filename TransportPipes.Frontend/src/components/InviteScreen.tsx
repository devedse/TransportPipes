import { useState, type FormEvent } from "react";
import { gameClient } from "../app/client";
import { COLORS, emblemIcon, landIcon, landText, lived, nameIdea } from "../app/crews";
import { toast, useAppStore } from "../state/store";
import { Swatches } from "./FoundCompany";

/** Browsers inside chat apps forget guests: there the account is asked for right away */
function inAppBrowser(): boolean {
  return /WhatsApp|Instagram|FBAN|FBAV|Discord|Line\/|Snapchat|Telegram/i.test(navigator.userAgent);
}

/**
 * A friend's invite (v1.2 §5): "Ana invites you to Kettle & Co.", the seat kept for you preselected. Tap 1 moves in
 * (as a guest when you have no account), tap 2 names the company (filled in already). Inside a chat app's browser the
 * account comes with tap 2, because those forget guests.
 */
export function InviteScreen({ onLogin }: { onLogin?: () => void }) {
  const invite = useAppStore((state) => state.invite)!;
  const user = useAppStore((state) => state.user);
  const phase = useAppStore((state) => state.phase);
  const dto = invite.dto;
  const station = dto.station;
  const [name, setName] = useState(dto.name ?? nameIdea());
  const [color, setColor] = useState(dto.color ?? COLORS[0]!);
  const [account, setAccount] = useState({ userName: "", password: "" });
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const needsAccount = inAppBrowser() && (user === null || user.isGuest);

  const moveIn = async () => {
    setBusy(true);
    setError(await gameClient.playAsGuest().catch((reason: unknown) => String(reason)));
    setBusy(false);
  };

  const done = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    try {
      // The server settles you on the kept seat (a seat code) or next to the crew (its link) and joins you up
      const failure = await gameClient.foundCompany(name, color, null, invite.code);
      if (failure !== null) {
        setError(failure.error);
        return;
      }
      if (dto.needsApproval) {
        toast(`You asked to join ${dto.crewName}: a member lets you in`, "info", 7000);
      }
      if (needsAccount && account.userName !== "") {
        setError(await gameClient.keepEstate(account.userName, account.password, ""));
      }
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="screen found-screen">
      <div className="panel found-panel invite-panel">
        <div
          className="crew-banner"
          style={{
            background: `linear-gradient(90deg, ${dto.color ?? "#236b69"} 50%, ${dto.color2 ?? "#c9963a"} 50%)`,
          }}
        >
          <span>{emblemIcon(dto.emblem)}</span> <strong>{dto.crewName}</strong>
        </div>
        <h2>
          {dto.friendName !== null && dto.seatCode
            ? `${dto.from ?? "A friend"} kept this seat for ${dto.friendName}`
            : `${dto.from ?? "A friend"} invites you to ${dto.crewName}`}
        </h2>
        {station !== null && (
          <>
            <p className="muted">
              {dto.seatCode ? "A seat next to them" : "Their home"} in <strong>{station.name}</strong>
              {dto.needsApproval && " (an older link: a member lets you in)"}
            </p>
            <ul className="seat-list">
              {station.seats.map((s) => (
                <li key={s.slot} className={s.slot === dto.slot ? "chosen" : undefined}>
                  <span className="icon">{lived(s) ? "🏠" : landIcon(s.primary)}</span>{" "}
                  {lived(s) ? (
                    <strong>{s.ownerName}</strong>
                  ) : (
                    <>
                      {landText(s.primary, s.secondary)}
                      {s.slot === dto.slot && <span className="badge">your seat</span>}
                    </>
                  )}
                </li>
              ))}
            </ul>
          </>
        )}
        {phase === "login" ? (
          <>
            <button className="primary wide" disabled={busy} onClick={() => void moveIn()}>
              {busy ? "Moving in…" : "Move in"}
            </button>
            <p className="muted small">
              No account needed now: you can keep your estate with a name and password later.{" "}
              <button className="link" onClick={() => (onLogin ?? (() => gameClient.forgetInvite()))()}>
                I have an account
              </button>
            </p>
          </>
        ) : (
          <form onSubmit={(event) => void done(event)}>
            <label>
              Your company
              <input
                value={name}
                onChange={(e) => setName(e.target.value)}
                required
                minLength={3}
                maxLength={24}
              />
            </label>
            <Swatches
              value={color}
              onChange={setColor}
              colors={[dto.color ?? COLORS[0]!, dto.color2 ?? COLORS[1]!, ...COLORS]}
            />
            {needsAccount && (
              <div className="keep-fields">
                <p className="small">
                  This browser forgets guests: keep your estate with a name and a password.
                </p>
                <input
                  placeholder="User name"
                  autoComplete="username"
                  value={account.userName}
                  onChange={(e) => setAccount({ ...account, userName: e.target.value })}
                />
                <input
                  placeholder="Password"
                  type="password"
                  autoComplete="new-password"
                  value={account.password}
                  onChange={(e) => setAccount({ ...account, password: e.target.value })}
                />
              </div>
            )}
            <button className="primary wide" type="submit" disabled={busy}>
              {busy ? "Settling…" : "Done"}
            </button>
          </form>
        )}
        {error !== null && (
          <p className="form-error" role="alert">
            {error}
          </p>
        )}
      </div>
    </div>
  );
}
