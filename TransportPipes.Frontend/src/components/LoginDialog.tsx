import { Suspense, useState, type FormEvent } from "react";
import { gameClient } from "../app/instance";
import { useAppStore } from "../state/store";
import { SpriteThumbnail } from "./SpriteThumbnail";

type Mode = "login" | "register";

/** Shown while connected and not logged in. */
export function LoginDialog() {
  const visible = useAppStore((state) => state.connectionStatus === "connected" && state.user === null);
  const notice = useAppStore((state) => state.loginNotice);
  return visible ? <LoginForm notice={notice} /> : null;
}

function LoginForm({ notice }: { notice: string | null }) {
  const [mode, setMode] = useState<Mode>("login");
  const [userName, setUserName] = useState(import.meta.env.DEV ? "Devedse" : "");
  const [password, setPassword] = useState(import.meta.env.DEV ? "a" : "");
  const [email, setEmail] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [shaking, setShaking] = useState(false);

  const registering = mode === "register";

  async function submit(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const result = registering
        ? await gameClient.register(userName.trim(), password, email.trim())
        : await gameClient.login(userName.trim(), password);
      if (!result.success) {
        setError(result.message || "Login failed.");
        setShaking(true);
      }
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : String(reason));
      setShaking(true);
    } finally {
      setBusy(false);
    }
  }

  function switchMode() {
    setMode(registering ? "login" : "register");
    setError(null);
  }

  return (
    <div className="modal-backdrop">
      <form
        className={`panel dialog login-dialog${shaking ? " shake" : ""}`}
        onSubmit={(event) => void submit(event)}
        onAnimationEnd={() => setShaking(false)}
        aria-labelledby="login-title"
      >
        <div className="login-header">
          <span className="login-logo" aria-hidden="true">
            <Suspense fallback={null}>
              <SpriteThumbnail name="FactorySmall" width={56} crop animate />
            </Suspense>
          </span>
          <div>
            <h1 id="login-title">Transport Pipes</h1>
            <p className="muted">{registering ? "Create a new account" : "Log in to continue building"}</p>
          </div>
        </div>
        {notice !== null && <p className="notice">{notice}</p>}

        <label className="field">
          <span>Username</span>
          <input
            type="text"
            autoComplete="username"
            value={userName}
            onChange={(event) => setUserName(event.target.value)}
            minLength={registering ? 4 : undefined}
            required
            autoFocus
          />
        </label>
        <label className="field">
          <span>Password</span>
          <input
            type="password"
            autoComplete={registering ? "new-password" : "current-password"}
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
          />
        </label>
        <div className={`field-reveal${registering ? " field-reveal--open" : ""}`} aria-hidden={!registering}>
          <label className="field">
            <span>Email</span>
            <input
              type="email"
              autoComplete="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              required={registering}
              tabIndex={registering ? 0 : -1}
            />
          </label>
        </div>

        {error !== null && (
          <p className="error-message" role="alert">
            {error}
          </p>
        )}

        <div className="actions">
          <button type="submit" className="button button--primary" disabled={busy}>
            {busy ? "Please wait…" : registering ? "Create account" : "Log in"}
          </button>
          <button type="button" className="button button--ghost" onClick={switchMode} disabled={busy}>
            {registering ? "I already have an account" : "Create an account"}
          </button>
        </div>
      </form>
    </div>
  );
}
