import { useState } from "react";
import { gameClient } from "../app/client";
import { emblemIcon } from "../app/crews";
import { useAppStore } from "../state/store";
import { bannerBackground } from "../app/banner";

/** Browsers inside chat apps forget guests: there the account is asked for right away */
function inAppBrowser(): boolean {
  return /WhatsApp|Instagram|FBAN|FBAV|Discord|Line\/|Snapchat|Telegram/i.test(navigator.userAgent);
}

/**
 * A friend's "Play next to me" link before you play (1.4 §6): who sent it and their Yard, and Play next to them (as a
 * guest when you have no account). The map then opens at their Yard, their free plots glowing. Inside a chat app's
 * browser an account comes first, because those forget guests.
 */
export function InviteScreen({ onLogin }: { onLogin: () => void }) {
  const invite = useAppStore((state) => state.invite)!;
  const dto = invite.dto;
  const yard = dto.yard;
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const from = dto.from ?? "Your friend";
  const full = yard !== null && yard.free === 0;

  const play = async () => {
    setBusy(true);
    setError(await gameClient.playAsGuest().catch((reason: unknown) => String(reason)));
    setBusy(false);
  };

  return (
    <div className="screen found-screen">
      <div className="panel found-panel invite-panel">
        {yard !== null && (
          <div
            className={yard.emblem !== null ? "crew-banner" : "crew-banner empty"}
            style={
              yard.emblem !== null
                ? {
                    background: bannerBackground(yard.color ?? "#c9963a", yard.color2),
                  }
                : undefined
            }
          >
            {yard.emblem !== null && <span>{emblemIcon(yard.emblem)}</span>} <strong>{yard.name}</strong>
          </div>
        )}
        <h2>
          <span className="dot" style={{ background: dto.fromColor ?? "#999" }} /> {from} plays at{" "}
          {yard?.name ?? "their Yard"}
        </h2>
        <p>
          Play next to them: your plot goes beside theirs around the Yard, and you trade your spare with each
          other from the first evening.
          {full && " Their Yard is full now: the map shows you the nearest free plots."}
          {!full &&
            dto.opens &&
            yard !== null &&
            !yard.open &&
            " Their Yard is closed, but their link lets you in."}
        </p>
        {inAppBrowser() ? (
          <>
            <p className="small">
              This app&apos;s browser forgets guests: make an account first, it takes a moment.
            </p>
            <button className="primary wide" onClick={onLogin}>
              Make an account
            </button>
          </>
        ) : (
          <>
            <button className="primary wide" disabled={busy} onClick={() => void play()}>
              {busy ? "One moment…" : `Play next to ${from}`}
            </button>
            <p className="muted small">
              No account needed now: keep your company with a name and a password later.{" "}
              <button className="link" onClick={onLogin}>
                I have an account
              </button>
            </p>
          </>
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
