import { VERSION_URL, type VersionInfo } from "./contracts";

/** Fetches the version of the running server. */
export async function fetchServerVersion(signal?: AbortSignal): Promise<VersionInfo> {
  const response = await fetch(VERSION_URL, { signal, cache: "no-store" });
  if (!response.ok) {
    throw new Error(`GET ${VERSION_URL} failed with ${response.status}`);
  }
  return (await response.json()) as VersionInfo;
}
