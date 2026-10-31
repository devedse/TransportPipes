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
          <strong>{entry.title}</strong>
          <span className="goods-tip-line">{entry.text}</span>
        </>
      }
    >
      <span className="term" tabIndex={0}>
        {children ?? entry.title}
      </span>
    </HoverPopup>
  );
}
