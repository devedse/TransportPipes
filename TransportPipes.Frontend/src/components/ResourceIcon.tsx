import { useState } from "react";
import { useAppStore } from "../state/store";

/** A resource's generated icon, or its coloured dot when there is no icon */
export function ResourceIcon({ resource }: { resource: string }) {
  const content = useAppStore((s) => s.content);
  const [failed, setFailed] = useState(false);
  if (failed || content === null) {
    return <span className="flow-dot" style={{ background: content?.resourceColor(resource) ?? "#999" }} />;
  }
  return (
    <img
      className="resource-icon"
      src={`/art/ui/res-${resource}.webp`}
      alt=""
      title={content.resourceName(resource)}
      onError={() => setFailed(true)}
    />
  );
}
