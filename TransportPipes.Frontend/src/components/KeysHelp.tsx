import { setState, useAppStore } from "../state/store";

const MOUSE: Array<[string, string]> = [
  [
    "Left drag",
    "select tool: a box around buildings · build: a row (Shift: an area) · pipe: a route · remove: an area",
  ],
  ["Shift + click", "add a building to the selection, or take it out"],
  ["Ctrl + click", "all your buildings of that kind"],
  ["Double click", "all of that kind in its network"],
  ["Right click", "one step back: put the tool away, clear the selection, close the panel"],
  ["Right drag", "move the map (also W A S D, the arrows, one finger)"],
  ["Wheel", "zoom (also + and −, two fingers)"],
];

const TOUCH: Array<[string, string]> = [
  ["One finger", "move the map; a tap selects"],
  ["Two fingers", "zoom"],
];

const KEYS: Array<[string, string]> = [
  ["V · B · P · X", "select · build · pipe · remove"],
  ["U · Shift + U", "upgrade the selection (or the building) by one · by ten"],
  ["Delete", "demolish the selection (asks first)"],
  ["Ctrl + A", "select all your buildings on screen"],
  ["Esc", "back"],
  ["H · G", "home · the Deep Level"],
  ["N · M · R · C · I · L", "neighbours · hub · research · corporation · company · leaderboard"],
  ["O · F · T", "overlays · photo mode · chat"],
  ["?", "this list"],
];

/** What the mouse and the keys do (the toolbar's ? and the ? key) */
export function KeysHelp() {
  const open = useAppStore((s) => s.keysOpen);
  if (!open) {
    return null;
  }
  return (
    <div className="panel keys-help" role="dialog" aria-label="Keys">
      <div className="panel-header">
        <h3>Mouse and keys</h3>
        <button className="icon-button" title="Close (Esc)" onClick={() => setState({ keysOpen: false })}>
          ×
        </button>
      </div>
      <dl>
        {[...MOUSE, ...TOUCH, ...KEYS].map(([key, text]) => (
          <div key={key} className="keys-row">
            <dt>{key}</dt>
            <dd>{text}</dd>
          </div>
        ))}
      </dl>
    </div>
  );
}
