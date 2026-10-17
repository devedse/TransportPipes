import { TransportConnection } from "../api/transportConnection";
import { appStore } from "../state/store";
import { GameClient } from "./GameClient";

/** The single hub connection and game client of the page. */
export const connection = new TransportConnection();
export const gameClient = new GameClient(connection, appStore);
