import { useState, type FormEvent } from "react";
import { gameClient } from "../app/client";
import { useAppStore } from "../state/store";
import { InviteScreen } from "./InviteScreen";

/** Login and registration, over the title art */
export function AuthScreen() {
  const [mode, setMode] = useState<"login" | "register">("login");
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [email, setEmail] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const invite = useAppStore((state) => state.invite);
  const [loginWanted, setLoginWanted] = useState(false);

  const playNow = async () => {
    setBusy(true);
    setError(await gameClient.playAsGuest().catch((reason: unknown) => String(reason)));
    setBusy(false);
  };

  if (invite !== null && !loginWanted) {
    return <InviteScreen onLogin={() => setLoginWanted(true)} />;
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const failure =
        mode === "login"
          ? await gameClient.login(userName, password)
          : await gameClient.register(userName, password, email);
      setError(failure);
    } catch (reason) {
      setError(`The server didn't answer (${String(reason)}).`);
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="screen auth-screen">
      <div className="title-card">
        <h1 className="game-title">
          <img className="game-logo" src="/art/ui/logo.webp" alt="TransportPipes" />
        </h1>
        <p className="tagline">
          Build your plot. Pipe your neighbours. Grow an empire that keeps running while you sleep.
        </p>
      </div>
      <div className="panel play-now">
        <button className="primary big" disabled={busy} onClick={() => void playNow()}>
          Play now
        </button>
        <p className="muted small">
          No account needed: keep your plot with a name and a password whenever you like.
        </p>
      </div>
      <form className="panel auth-panel" onSubmit={(event) => void submit(event)}>
        <div className="tabs" role="tablist">
          <button
            type="button"
            role="tab"
            aria-selected={mode === "login"}
            className={mode === "login" ? "tab active" : "tab"}
            onClick={() => setMode("login")}
          >
            Log in
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={mode === "register"}
            className={mode === "register" ? "tab active" : "tab"}
            onClick={() => setMode("register")}
          >
            New player
          </button>
        </div>
        <label>
          User name
          <input
            autoFocus
            autoComplete="username"
            value={userName}
            onChange={(event) => setUserName(event.target.value)}
            required
            minLength={3}
            maxLength={24}
          />
        </label>
        <label>
          Password
          <input
            type="password"
            autoComplete={mode === "login" ? "current-password" : "new-password"}
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
            minLength={mode === "register" ? 6 : 1}
          />
        </label>
        {mode === "register" && (
          <label>
            E-mail <span className="muted">(optional)</span>
            <input
              type="email"
              autoComplete="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
            />
          </label>
        )}
        {error !== null && (
          <p className="form-error" role="alert">
            {error}
          </p>
        )}
        <button className="primary" type="submit" disabled={busy}>
          {busy ? "One moment…" : mode === "login" ? "Log in" : "Create my account"}
        </button>
        <p className="muted small">You stay logged in on this device.</p>
      </form>
    </div>
  );
}
