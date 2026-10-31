import type { ReactNode } from "react";
import { GLOSSARY, type GlossaryWord } from "../app/glossary";
import { HoverPopup } from "./HoverPopup";

/** A game word, underlined: hovering it says what it is (app/glossary.ts) */
export function Term({ word, children }: { word: GlossaryWord; children?: ReactNode }) {
  const entry = GLOSSARY[word];
  return (
    <HoverPopup
      width={300}
      popup={
        <>
          <strong className="tip-title">{entry.title}</strong>
          {/* A line a sentence: easier to take in than one block (the owner, 2026-10-31) */}
          <span className="tip-section">
            {entry.text.split(/(?<=[.:])\s+(?=[A-Z])/).map((sentence) => (
              <span key={sentence}>{sentence}</span>
            ))}
          </span>
        </>
      }
    >
      <span className="term" tabIndex={0}>
        {children ?? entry.title}
      </span>
    </HoverPopup>
  );
}
