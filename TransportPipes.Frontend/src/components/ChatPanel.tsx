import { useEffect, useRef, useState, type FormEvent } from "react";
import { gameClient } from "../app/client";
import type { ChatChannelDto, ChatMessageDto } from "../api/contracts";
import { setState, useAppStore } from "../state/store";

const KIND_ICONS: Record<ChatChannelDto["kind"], string> = {
  crew: "👥",
  station: "🏗",
  district: "🏘",
  corporation: "🏢",
  direct: "✉",
};

function time(at: string): string {
  return new Date(at).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}

/** A day stamp between messages (F10): "Today", "Yesterday", "Tue 20 Oct" */
function day(at: string, now: number): string {
  const date = new Date(at);
  const today = new Date(now);
  const days = Math.round(
    (new Date(today.getFullYear(), today.getMonth(), today.getDate()).getTime() -
      new Date(date.getFullYear(), date.getMonth(), date.getDate()).getTime()) /
      86400000,
  );
  return days === 0
    ? "Today"
    : days === 1
      ? "Yesterday"
      : date.toLocaleDateString([], { weekday: "short", day: "numeric", month: "short" });
}

function Message({ message, mine }: { message: ChatMessageDto; mine: boolean }) {
  const [menu, setMenu] = useState(false);
  return (
    <div className={mine ? "chat-message mine" : "chat-message"}>
      <div className="chat-meta">
        <button className="link chat-sender" onClick={() => setMenu(!menu)} disabled={mine}>
          {message.senderName}
        </button>
        <span className="muted small">{time(message.sentAt)}</span>
      </div>
      <div className="chat-text">{message.text}</div>
      {menu && !mine && (
        <div className="chat-menu">
          <button onClick={() => void gameClient.visitEstate(message.sender, true)}>Visit</button>
          <button onClick={() => void gameClient.messageCompany(message.sender, message.senderName)}>
            Message
          </button>
          <button onClick={() => void gameClient.reportChat(message.id)}>Report</button>
          <button className="danger" onClick={() => void gameClient.setBlocked(message.sender, true)}>
            Block
          </button>
        </div>
      )}
    </div>
  );
}

/** Crew, station, district, corporation and direct chat, docked at the bottom right (after run 3, row 12) */
export function ChatPanel() {
  const open = useAppStore((s) => s.chatOpen);
  const chat = useAppStore((s) => s.chat);
  const channel = useAppStore((s) => s.chatChannel);
  const messages = useAppStore((s) => (s.chatChannel === null ? undefined : s.chatMessages[s.chatChannel]));
  const unread = useAppStore((s) => s.chatUnread);
  const company = useAppStore((s) => s.company);
  const leftAt = useAppStore((s) => s.leftAt);
  const now = (company === null ? 0 : new Date(company.serverTime).getTime()) || 0;
  const [text, setText] = useState("");
  const list = useRef<HTMLDivElement>(null);
  const total = Object.values(unread).reduce((sum, n) => sum + n, 0);

  useEffect(() => {
    list.current?.scrollTo({ top: list.current.scrollHeight });
  }, [messages, open]);

  if (!open) {
    return (
      <button className="panel chat-toggle" title="Chat (T)" onClick={() => void gameClient.toggleChat()}>
        <img className="ui-icon" src="/art/ui/ui-chat.webp" alt="" /> Chat
        {total > 0 && <span className="badge">{total}</span>}
      </button>
    );
  }
  const send = async (event: FormEvent) => {
    event.preventDefault();
    if (await gameClient.sendChat(text)) {
      setText("");
      // The keys belong to the map again (P picks the pipe, not a letter in the box); T or a click comes back here
      if (document.activeElement instanceof HTMLElement) {
        document.activeElement.blur();
      }
    }
  };
  const current = chat?.channels.find((t) => t.id === channel);
  return (
    <div className="panel chat-panel" role="complementary" aria-label="Chat">
      <div className="chat-channels">
        {chat?.channels.map((c) => (
          <button
            key={c.id}
            className={c.id === channel ? "chat-channel active" : "chat-channel"}
            title={c.name}
            onClick={() => void gameClient.openChannel(c.id)}
          >
            {KIND_ICONS[c.kind]} {c.name}
            {(unread[c.id] ?? 0) > 0 && <span className="badge">{unread[c.id]}</span>}
          </button>
        ))}
        <button
          className="icon-button chat-close"
          title="Close chat (T)"
          onClick={() => void gameClient.toggleChat()}
        >
          ×
        </button>
      </div>
      <div className="chat-list" ref={list}>
        {messages === undefined && <p className="muted small">Loading…</p>}
        {messages?.length === 0 && (
          <p className="muted small">
            {current?.kind === "crew"
              ? "Only your crew reads this, wherever they live."
              : current?.kind === "station"
                ? "Only the players of your station read this: your project, your Yard."
                : current?.kind === "district"
                  ? "Say hello to your neighbours! Everyone in your district reads this."
                  : current?.kind === "corporation"
                    ? "Only members of your corporation read this."
                    : "Only the two of you read this."}
          </p>
        )}
        {messages?.map((m, i) => {
          const before = messages[i - 1];
          const newDay = before === undefined || day(before.sentAt, now) !== day(m.sentAt, now);
          // The first message since the player last left
          const fresh =
            leftAt !== null &&
            m.sender !== company?.id &&
            new Date(m.sentAt) > new Date(leftAt) &&
            (before === undefined || new Date(before.sentAt) <= new Date(leftAt));
          return (
            <div key={m.id}>
              {newDay && <div className="chat-day">{day(m.sentAt, now)}</div>}
              {fresh && <div className="chat-new">New since you left</div>}
              <Message message={m} mine={m.sender === company?.id} />
            </div>
          );
        })}
      </div>
      <form className="chat-form" onSubmit={(event) => void send(event)}>
        <input
          value={text}
          maxLength={300}
          placeholder={current === undefined ? "" : `Message ${current.name}`}
          onChange={(e) => setText(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Escape") {
              (e.target as HTMLInputElement).blur();
              setState({ chatOpen: false });
            }
          }}
        />
        <button className="primary" disabled={text.trim() === "" || channel === null}>
          Send
        </button>
      </form>
    </div>
  );
}
