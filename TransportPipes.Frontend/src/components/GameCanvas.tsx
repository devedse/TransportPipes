import { useEffect, useRef, useState } from "react";
import { gameClient } from "../app/instance";
import { GameEngine } from "../game/GameEngine";

/** Hosts the PixiJS map; the engine lives outside React and is wired to the game client. */
export function GameCanvas() {
  const hostRef = useRef<HTMLDivElement>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const host = hostRef.current;
    if (host === null) {
      return;
    }
    let engine: GameEngine | null = null;
    let cancelled = false;
    GameEngine.create(host, gameClient.engineCallbacks).then(
      (created) => {
        if (cancelled) {
          created.destroy();
          return;
        }
        engine = created;
        gameClient.attachEngine(created);
      },
      (reason: unknown) => {
        console.error("Could not start the map renderer", reason);
        if (!cancelled) {
          setError(reason instanceof Error ? reason.message : String(reason));
        }
      },
    );
    return () => {
      cancelled = true;
      if (engine !== null) {
        gameClient.detachEngine();
        engine.destroy();
      }
    };
  }, []);

  return (
    <div ref={hostRef} className="game-host">
      {error !== null && (
        <div className="panel game-error" role="alert">
          <h2>The map could not be started</h2>
          <p>{error}</p>
        </div>
      )}
    </div>
  );
}
