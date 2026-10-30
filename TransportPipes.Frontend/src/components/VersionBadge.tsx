import { useEffect, useState } from "react";
import type { VersionInfo } from "../api/contracts";
import { fetchServerVersion } from "../api/version";
import { formatVersion } from "../app/format";
import { setState, useAppStore } from "../state/store";

/** The version of the server in the bottom corner. Fetched again after a reconnect (e.g. after an update). */
export function VersionBadge() {
  const status = useAppStore((state) => state.connectionStatus);
  const [version, setVersion] = useState<VersionInfo | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    fetchServerVersion(controller.signal).then(setVersion, () => {
      // Not important enough to bother the player, the badge just stays as it was.
    });
    return () => controller.abort();
  }, [status]);

  if (version === null) {
    return null;
  }

  return (
    <button
      type="button"
      className="version-badge"
      title={`What's new${version.commit ? ` · commit ${version.commit}` : ""}`}
      onClick={() => setState({ whatsNewOpen: "latest" })}
    >
      {formatVersion(version)}
    </button>
  );
}
