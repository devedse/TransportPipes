import { fetchServerVersion } from "../api/version";
import { setState } from "../state/store";

const CHECK_EVERY_MS = 60_000;

/**
 * Notices a new version of the game: the server serves the frontend, so a server with another build than the one this
 * page loaded with means this page runs old code. Checked after every reconnect (an update restarts the server) and
 * once a minute; the update overlay then reloads the page.
 */
export function watchForUpdates(onReconnected: (listener: () => void) => void) {
  let loaded: string | null = null;
  const check = async () => {
    try {
      const version = await fetchServerVersion();
      const build = `${version.version}+${version.commit ?? ""}`;
      if (loaded === null) {
        loaded = build;
      } else if (build !== loaded) {
        setState({ updateAvailable: true });
      }
    } catch {
      // The server is restarting: the next check (or the reconnect) tells
    }
  };
  void check();
  onReconnected(() => void check());
  window.setInterval(() => void check(), CHECK_EVERY_MS);
}
