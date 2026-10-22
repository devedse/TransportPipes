import type {
  BlueprintDto,
  BlueprintSharing,
  BuildingDetailsDto,
  BuildingDto,
  BuildingFixDto,
  ChatMessageDto,
  CommandResult,
  CompanyDto,
  CorporationRole,
  DailyOrderDto,
  LeaderboardKind,
  NextStepDto,
  ReturnLineDto,
  SeatRefDto,
  ToastDto,
} from "../api/contracts";
import { auth, fetchContent, fetchInvite, GameConnection } from "../api/server";
import { Content } from "../game/content";
import type { ClickModifiers, Engine, EngineCallbacks, Overlay, Tool } from "../game/Engine";
import type { TilePosition } from "../game/isometric";
import {
  buyableParcels,
  DEEP_OFFSET,
  ESTATE_SIZE,
  PARCEL_SIZE,
  PARCELS_PER_SIDE,
  isDeep,
  isParcelUnlocked,
  parcelOf,
  yardGate,
  zoneAt,
} from "../game/layout";
import {
  canPlace,
  newPipeTiles,
  ownEstateAt,
  ownEstates,
  routePipe,
  upgradablePipeTiles,
} from "../game/rules";
import { WorldModel } from "../game/world";
import { formatAmount, possessive } from "./format";
import { notify } from "./notifications";
import { sounds } from "./sound";
import { appStore, initialState, MODALS, setState, toast, type HubTab } from "../state/store";

const DETAILS_REFRESH_MS = 2000;
const INVITE_KEY = "tp.invite";
const GUEST_KEY = "tp.guest";

/** The hidden account of a guest, kept by the browser */
function readGuest(): { userName: string; password: string } | null {
  try {
    const raw = localStorage.getItem(GUEST_KEY);
    return raw === null ? null : (JSON.parse(raw) as { userName: string; password: string });
  } catch {
    return null;
  }
}

/** The address a friend opens to move into a seat (or join the crew) */
export function inviteLink(code: string, seat: boolean): string {
  return `${location.origin}/?${seat ? "seat" : "crew"}=${encodeURIComponent(code)}`;
}

/** Connects the server, the world model, the map engine and the UI state */
/** Whether the screen is a phone's (the layout's 720 px break) */
const PHONE = () => window.matchMedia?.("(max-width: 720px)").matches === true;

export class GameClient {
  readonly connection = new GameConnection();
  world: WorldModel | null = null;
  engine: Engine | null = null;
  private detailsTimer: number | null = null;
  private wiredEvents = false;

  async start() {
    try {
      const content = new Content(await fetchContent());
      this.world = new WorldModel(content.dto.layout.terrain);
      setState({ content });
    } catch (error) {
      setState({ phase: "error", error: `The game server can't be reached (${String(error)}).` });
      return;
    }
    await this.readInvite();
    let user = await auth.me().catch(() => null);
    if (user === null) {
      // A guest whose cookie is gone: the browser still has its hidden account
      const guest = readGuest();
      if (guest !== null) {
        const result = await auth.login(guest.userName, guest.password).catch(() => null);
        user = result?.success === true ? result.user : null;
      }
    }
    if (user === null) {
      setState({ phase: "login" });
      return;
    }
    setState({ user });
    await this.connect();
  }

  /** A seat code (?seat=) or crew link (?crew=) in the address: what it opens */
  private async readInvite() {
    const params = new URLSearchParams(location.search);
    const code = params.get("seat") ?? params.get("crew") ?? sessionStorage.getItem(INVITE_KEY);
    if (code === null || code.trim() === "") {
      return;
    }
    sessionStorage.setItem(INVITE_KEY, code);
    const dto = await fetchInvite(code).catch(() => null);
    if (dto?.valid === true) {
      setState({ invite: { code, dto } });
    } else {
      sessionStorage.removeItem(INVITE_KEY);
    }
  }

  /** The invite is used (or the player chose another way to start) */
  forgetInvite() {
    sessionStorage.removeItem(INVITE_KEY);
    setState({ invite: null });
    if (location.search !== "") {
      history.replaceState(null, "", location.pathname);
    }
  }

  /** Plays right away without an account: a hidden one the browser keeps (an account can come later) */
  async playAsGuest(): Promise<string | null> {
    const result = await auth.guest();
    if (!result.success || result.user === null || result.password === null) {
      return result.error ?? "That didn't work.";
    }
    try {
      localStorage.setItem(
        GUEST_KEY,
        JSON.stringify({ userName: result.user.userName, password: result.password }),
      );
    } catch {
      // Private browsing: the cookie still works for this visit
    }
    setState({ user: result.user });
    await this.connect();
    return null;
  }

  /** A guest keeps the estate: a name and a password of its own */
  async keepEstate(userName: string, password: string, email: string): Promise<string | null> {
    const result = await auth.keep(userName, password, email);
    if (!result.success || result.user === null) {
      return result.error ?? "That didn't work.";
    }
    try {
      localStorage.removeItem(GUEST_KEY);
    } catch {
      // Nothing kept
    }
    setState({ user: result.user, keepOpen: false });
    toast(`Your estate is yours: log in as ${result.user.userName} anywhere.`, "success", 7000);
    return null;
  }

  async login(userName: string, password: string): Promise<string | null> {
    const result = await auth.login(userName, password);
    if (!result.success || result.user === null) {
      return result.error ?? "That didn't work.";
    }
    setState({ user: result.user });
    await this.connect();
    return null;
  }

  async register(userName: string, password: string, email: string): Promise<string | null> {
    const result = await auth.register(userName, password, email);
    if (!result.success || result.user === null) {
      return result.error ?? "That didn't work.";
    }
    setState({ user: result.user });
    await this.connect();
    return null;
  }

  async logout() {
    await this.connection.stop();
    await auth.logout();
    location.reload();
  }

  private async connect() {
    if (!this.wiredEvents) {
      this.wireEvents();
    }
    setState({ phase: "loading" });
    await this.connection.start();
    await this.hello();
  }

  private wireEvents() {
    this.wiredEvents = true;
    const c = this.connection;
    c.onStatusChanged((status) => setState({ connectionStatus: status }));
    c.onReconnected(() => {
      // Everything may have changed while we were away
      this.engine?.reloadChunks();
      void this.hello();
    });
    c.on("chunkChanged", (chunk) => this.world?.applyObjects(chunk));
    c.on("districtChanged", (district) => {
      this.world?.setDistrict(district);
      setState({ districtVersion: appStore.getState().districtVersion + 1 });
    });
    c.on("companyChanged", (company) => this.setCompany(company));
    c.on("networksChanged", (networks) => {
      this.followSelectedNetwork(networks);
      void this.refreshFlows(networks);
    });
    c.on("exchangeChanged", () => {
      if (appStore.getState().hubOpen) {
        void this.refreshMarket();
      }
    });
    c.on("marketChanged", (x, y) => {
      const company = appStore.getState().company;
      if (
        appStore.getState().hubOpen &&
        company !== null &&
        company.districtX === x &&
        company.districtY === y
      ) {
        void this.refreshMarket();
      }
    });
    c.on("chatMessage", (message) => this.receiveChat(message));
    c.on("inboxChanged", (unread) => {
      setState({ unread });
      if (appStore.getState().inboxOpen && unread > 0) {
        void this.refreshInbox();
      }
    });
    // A new season: a new world, everything starts again
    c.on("seasonChanged", () => location.reload());
    c.on("wondersChanged", () => {
      if (appStore.getState().hubOpen && appStore.getState().hubTab === "wonders") {
        void this.refreshWonders();
      }
    });
    c.on("crewChanged", () => {
      if (appStore.getState().crewOpen) {
        void this.refreshCrew();
      }
    });
    c.on("linksChanged", () => {
      if (appStore.getState().yardOpen && appStore.getState().yardTab === "links") {
        void this.refreshLinks();
      }
    });
    c.on("mapLinksChanged", () => void this.loadMapLinks());
    c.on("corporationChanged", () => {
      if (appStore.getState().corporationOpen) {
        void this.refreshCorporations();
      }
      if (appStore.getState().chat !== null) {
        void this.refreshChat();
      }
    });
    c.on("toast", (t: ToastDto) => {
      notify(t);
      if (t.kind === "constructionFinished" && t.building !== null) {
        this.engine?.celebrate(t.building);
      }
      // The first screen (the plan's first evening): the level card and achievements wait until Loady's first step
      if ((t.kind === "levelUp" || t.kind === "achievement") && this.onLoadysFirstStep()) {
        this.waiting.push(t);
        return;
      }
      this.showToast(t);
    });
  }

  /** The overlay Loady's Show me turned on, and for which step: it turns off when the step is done (F10) */
  private hintOverlay: { step: string; overlay: Overlay } | null = null;

  /** What waits for Loady's first step to be done (level cards, achievements) */
  private readonly waiting: ToastDto[] = [];

  private onLoadysFirstStep(): boolean {
    return appStore.getState().company?.tutorial?.step === 0;
  }

  /** What waited for Loady's first step, now that it's done */
  private releaseWaiting() {
    if (this.waiting.length === 0 || this.onLoadysFirstStep()) {
      return;
    }
    for (const t of this.waiting.splice(0)) {
      this.showToast(t);
    }
  }

  private showToast(t: ToastDto) {
    if (t.kind === "levelUp") {
      const level = Number(/\d+/.exec(t.text)?.[0] ?? 0);
      if (level > 0) {
        // The celebration says it: no toast as well
        setState({ levelUp: level });
        sounds.levelUp();
        return;
      }
    }
    toast(
      t.text,
      t.kind === "levelUp" ||
        t.kind === "researchFinished" ||
        t.kind === "orderCompleted" ||
        t.kind === "achievement" ||
        t.kind === "wonder"
        ? "level"
        : "success",
      t.kind === "constructionFinished" ? 4500 : 7000,
    );
    if (t.kind === "achievement" && appStore.getState().companyOpen) {
      void this.refreshCompanyDetails();
    }
    if (t.building !== null && t.building === appStore.getState().selectedBuilding) {
      void this.refreshDetails();
    }
  }

  private async hello() {
    const hello = await this.connection.hello();
    setState({
      user: hello.user,
      era: hello.era,
      serverOffsetMs: new Date(hello.serverTime).getTime() - Date.now(),
      returnCard: hello.card,
      leftAt: hello.leftAt,
      unread: hello.unread,
    });
    if (hello.company === null) {
      setState({ phase: "found" });
      return;
    }
    this.setCompany(hello.company);
    setState({ phase: "playing" });
    void this.loadMapLinks();
    if (this.homedCompany !== hello.company.id) {
      this.goHome();
    }
    if (hello.company.season.endedAt !== null) {
      void this.openSeason();
    }
  }

  private setCompany(company: CompanyDto) {
    if (this.world !== null) {
      this.world.dealtLand = company.land !== null;
    }
    const previous = appStore.getState().company;
    setState({ company, companyAt: Date.now(), era: company.era });
    this.releaseWaiting();
    if (this.hintOverlay !== null && company.tutorial?.id !== this.hintOverlay.step) {
      if (appStore.getState().overlay === this.hintOverlay.overlay) {
        setState({ overlay: "none" });
      }
      this.hintOverlay = null;
    }
    this.world?.setDeepLevel(company.estateX, company.estateY, company.deep.parcels);
    if (company.season.endedAt !== null && previous?.season.endedAt === null) {
      // The ceremony
      void this.openSeason();
    }
    if (
      previous !== null &&
      previous.id === company.id &&
      (previous.estateX !== company.estateX || previous.estateY !== company.estateY)
    ) {
      // The estate moved: the camera follows it to its new seat (where it lands)
      this.engine?.invalidate();
      this.goHome();
    }
    if (
      previous !== null &&
      (previous.unlockedParcels !== company.unlockedParcels ||
        previous.level !== company.level ||
        previous.era !== company.era ||
        previous.deep.open !== company.deep.open ||
        previous.deep.digParcel !== company.deep.digParcel)
    ) {
      this.engine?.invalidate();
    }
    if (appStore.getState().neighboursOpen && this.neighboursTimer === null) {
      // At most one refresh per few seconds while the panel is open
      this.neighboursTimer = window.setTimeout(() => {
        this.neighboursTimer = null;
        void this.refreshNeighbours();
      }, 3000);
    }
    if (
      appStore.getState().researchOpen &&
      (previous?.currentResearch !== company.currentResearch ||
        previous?.researched.length !== company.researched.length ||
        Math.floor((previous?.researchPoints ?? 0) / 10) !== Math.floor(company.researchPoints / 10))
    ) {
      void this.refreshResearch();
    }
  }

  async openNeighbours() {
    setState({ ...MODALS, neighboursOpen: true, neighboursTab: "neighbours" });
    await this.refreshNeighbours();
  }

  async refreshNeighbours() {
    const [neighbours, contracts] = await Promise.all([
      this.connection.getNeighbourhood().catch(() => null),
      this.connection.getContracts().catch(() => null),
    ]);
    setState({
      ...(neighbours !== null ? { neighbours } : {}),
      ...(contracts !== null ? { contracts } : {}),
    });
  }

  /**
   * Plans the pipe from your network (your nearest pipe, or your trading post) to your Yard gate and shows it on the
   * map: once it is laid, your neighbours in the station buy what you have spare and sell you what you lack
   */
  async planYardPipe() {
    const state = appStore.getState();
    const company = state.company;
    if (company === null || this.world === null) {
      return;
    }
    const gate = yardGate(company.estateX, company.estateY, company.slot);
    setState({ neighboursOpen: false, yardOpen: false, tool: { kind: "select" } });
    this.engine?.lookAt(gate.x, gate.y, 0.6);
    const steps: Array<[number, number]> = [
      [1, 0],
      [-1, 0],
      [0, 1],
      [0, -1],
    ];
    for (let attempt = 0; attempt < 10; attempt++) {
      await new Promise((resolve) => setTimeout(resolve, 250));
      const current = appStore.getState().company;
      if (current === null) {
        return;
      }
      // From the gate to the nearest of your own pipes (or the trading post's side)
      const own: Array<{ x: number; y: number }> = [];
      for (let dy = 0; dy < ESTATE_SIZE; dy++) {
        for (let dx = 0; dx < ESTATE_SIZE; dx++) {
          const x = current.estateX + dx;
          const y = current.estateY + dy;
          const conductor = this.world.conductorAt(x, y);
          if (conductor?.kind === "pipe" && conductor.owner === current.id) {
            own.push({ x, y });
          }
        }
      }
      if (own.length === 0) {
        // No pipe yet: start next to the trading post
        for (let dy = 0; dy < ESTATE_SIZE && own.length === 0; dy++) {
          for (let dx = 0; dx < ESTATE_SIZE && own.length === 0; dx++) {
            const building = this.world.buildingAt(current.estateX + dx, current.estateY + dy);
            if (building?.owner === current.id && building.type === "tradingPost") {
              for (const [sx, sy] of steps) {
                own.push({ x: building.x + sx, y: building.y + sy });
              }
            }
          }
        }
      }
      const targets = own
        .sort(
          (a, b) =>
            Math.abs(a.x - gate.x) +
            Math.abs(a.y - gate.y) -
            (Math.abs(b.x - gate.x) + Math.abs(b.y - gate.y)),
        )
        .slice(0, 6);
      for (const [dx, dy] of steps) {
        const start = { x: gate.x + dx, y: gate.y + dy };
        if (zoneAt(start.x, start.y).kind !== "estate") {
          continue;
        }
        for (const target of targets) {
          const route = routePipe(this.world, current, start, target, state.content ?? undefined, 4000);
          if (route !== null) {
            setState({
              plannedRoute: route,
              plannedPartner: null,
              plannedTarget: "your Yard gate (G)",
              plannedDone: null,
            });
            return;
          }
        }
      }
    }
    toast("No free way to your Yard gate: clear a path to it, or lay the pipe yourself.", "error");
  }

  async buildPlannedRoute() {
    const state = appStore.getState();
    if (state.plannedRoute === null || state.company === null || this.world === null) {
      return;
    }
    const tiles = newPipeTiles(this.world, state.company, state.plannedRoute, state.content ?? undefined);
    if (
      await this.command(
        this.connection.placePipes(
          tiles.flatMap((t) => [t.x, t.y]),
          1,
        ),
        state.plannedDone ?? "Connected! Trade starts right away.",
      )
    ) {
      setState({ plannedRoute: null, plannedPartner: null, plannedTarget: null, plannedDone: null });
    }
  }

  /** The one fix button of a building's panel (F2) */
  async fixBuilding(details: BuildingDetailsDto, fix: BuildingFixDto) {
    const name = appStore.getState().content?.building(details.building.type)?.name ?? "It";
    switch (fix.kind) {
      case "layPipe":
        // "Laid the pipe to your Iron Mine", "Laid the pipe from your Wind Moily to your Trading Post"
        await this.layPipeNow(
          fix.tiles,
          `Laid ${fix.label.split(" · ")[0]!.replace(/^Lay /, "")}: ${name} runs`,
        );
        break;
      case "open":
        if (fix.building !== null) {
          this.showBuilding(fix.building);
        }
        break;
      case "repair":
        await this.repair(details.building.id);
        break;
      case "upgrade":
        if (fix.building !== null) {
          await this.upgrade(fix.building, 1);
        }
        break;
      case "build":
        if (fix.buildingType !== null) {
          this.setTool({ kind: "build", type: fix.buildingType });
        }
        break;
    }
  }

  /**
   * A pipe the game planned, laid on the first tap (F2's fix button, an advisor step): the button already says where it
   * goes and what it costs (run 2: the preview's second "Lay it" was missed twice, and the buildings stayed stuck for
   * six days each). Tiles are x, y pairs; done is said once it is laid.
   */
  async layPipeNow(tiles: number[], done: string) {
    const state = appStore.getState();
    if (state.company === null || this.world === null) {
      return;
    }
    const route: TilePosition[] = [];
    for (let i = 0; i + 1 < tiles.length; i += 2) {
      route.push({ x: tiles[i]!, y: tiles[i + 1]! });
    }
    if (route.length === 0) {
      return;
    }
    const middle = route[Math.floor(route.length / 2)]!;
    this.engine?.lookAt(middle.x, middle.y, 0.8);
    const fresh = newPipeTiles(this.world, state.company, route, state.content ?? undefined);
    if (
      await this.command(
        this.connection.placePipes(
          fresh.flatMap((t) => [t.x, t.y]),
          1,
        ),
        done,
      )
    ) {
      void this.refreshDetails();
    }
  }

  /**
   * A pipe the game planned for Loady's Show me: shown as a ghost on the map with its price, laid with one click on the
   * banner. Tiles are x, y pairs; target names where it goes, done is said once it is laid.
   */
  showPlannedPipe(tiles: number[], target: string, done: string) {
    const route: TilePosition[] = [];
    for (let i = 0; i + 1 < tiles.length; i += 2) {
      route.push({ x: tiles[i]!, y: tiles[i + 1]! });
    }
    if (route.length === 0) {
      return;
    }
    setState({
      plannedRoute: route,
      plannedPartner: null,
      plannedTarget: target,
      plannedDone: done,
      tool: { kind: "select" },
    });
    const middle = route[Math.floor(route.length / 2)]!;
    this.engine?.lookAt(middle.x, middle.y, 0.8);
  }

  async proposeContract(
    partner: number | null,
    selling: boolean,
    resource: string,
    rate: number,
    price: number,
    days: number,
  ) {
    const ok = await this.command(
      this.connection.proposeContract(partner, selling, resource, rate, price, days),
      partner === null ? "Your offer is on the contract board" : "Offer sent",
    );
    if (ok) {
      await this.refreshNeighbours();
    }
    return ok;
  }

  async acceptContract(contract: number) {
    if (await this.command(this.connection.acceptContract(contract), "Contract signed")) {
      await this.refreshNeighbours();
    }
  }

  async declineContract(contract: number) {
    if (await this.command(this.connection.declineContract(contract))) {
      await this.refreshNeighbours();
    }
  }

  async openHub(tab?: HubTab) {
    setState({ ...MODALS, hubOpen: true, ...(tab !== undefined ? { hubTab: tab } : {}) });
    await Promise.all([this.refreshMarket(), this.refreshWonders()]);
  }

  async refreshWonders() {
    const wonders = await this.connection.getWonders().catch(() => null);
    if (wonders !== null) {
      setState({ wonders });
    }
  }

  async deliverToWonder(eraWonder: boolean) {
    if (await this.command(this.connection.deliverToWonder(eraWonder), "Delivered: thank you, builder!")) {
      await this.refreshWonders();
    }
  }

  async refreshMarket() {
    const [market, exchange] = await Promise.all([
      this.connection.getMarket().catch(() => null),
      this.connection.getExchange().catch(() => null),
    ]);
    setState({ ...(market !== null ? { market } : {}), ...(exchange !== null ? { exchange } : {}) });
  }

  async placeExchangeOrder(resource: string, selling: boolean, amount: number, price: number) {
    if (
      await this.command(this.connection.placeExchangeOrder(resource, selling, amount, price), "Order placed")
    ) {
      await this.refreshMarket();
      return true;
    }
    return false;
  }

  async cancelExchangeOrder(order: number) {
    if (await this.command(this.connection.cancelExchangeOrder(order))) {
      await this.refreshMarket();
    }
  }

  /**
   * Delivers what the Storages hold for an order and says what happened, for the order's own line: a toast hides behind
   * the Hub, and a part delivery said nothing at all (run 2: "Hub Deliver silent again")
   */
  async deliverOrder(order: DailyOrderDto): Promise<string> {
    try {
      const result = await this.connection.deliverOrder(order.id);
      if (!result.success) {
        return result.error ?? "That didn't work.";
      }
      await this.refreshCompany();
      this.scheduleNextSteps();
      const after =
        appStore.getState().company?.dailyOrders.find((t) => t.id === order.id)?.delivered ?? order.amount;
      const left = Math.max(0, order.amount - after);
      return left <= 1e-9
        ? `Done: +${formatAmount(order.rewardGold)} g`
        : `Delivered ${formatAmount(after - order.delivered)} of ${formatAmount(order.amount)} · ${formatAmount(left)} to go`;
    } catch (error) {
      return `The server didn't answer (${String(error)}).`;
    }
  }

  async setNoBuy(resource: string, noBuy: boolean) {
    if (await this.command(this.connection.setNoBuy(resource, noBuy))) {
      await this.refreshMarket();
    }
  }

  async setTradeBlocked(other: number, blocked: boolean) {
    if (await this.command(this.connection.setTradeBlocked(other, blocked))) {
      await this.refreshNeighbours();
    }
  }

  async setTradeWith(tradeWith: "everyone" | "corporation" | "contracts") {
    await this.command(this.connection.setTradeWith(tradeWith));
  }

  /** Who you trade with and at what prices: the Neighbours panel's trade settings */
  async openTradeSettings() {
    setState({ ...MODALS, neighboursOpen: true, neighboursTab: "trade" });
    await Promise.all([this.refreshNeighbours(), this.refreshMarket()]);
  }

  async setNoTrade(resource: string, noTrade: boolean) {
    if (await this.command(this.connection.setNoTrade(resource, noTrade))) {
      await this.refreshMarket();
    }
  }

  private neighboursTimer: number | null = null;

  async openResearch() {
    setState({ ...MODALS, researchOpen: true });
    await this.refreshResearch();
  }

  async refreshResearch() {
    const research = await this.connection.getResearch().catch(() => null);
    if (research !== null) {
      setState({ research });
    }
  }

  async startResearch(tech: string) {
    const name = appStore.getState().content?.research(tech)?.name ?? tech;
    if (await this.command(this.connection.startResearch(tech), `Researching ${name}`)) {
      await this.refreshCompany();
      await this.refreshResearch();
    }
  }

  /** A new company on a chosen seat (or the one a friend's code holds, or the one the game suggests) */
  /** A new company; the refusal (null: founded), with its code ("seatTaken": someone took the seat a moment ago) */
  async foundCompany(
    name: string,
    color: string,
    seat: SeatRefDto | null = null,
    code: string | null = null,
  ): Promise<{ error: string; code: string | null } | null> {
    const result = await this.connection.createCompany(name, color, seat, code);
    if (!result.success) {
      return { error: result.error ?? "That didn't work.", code: result.code ?? null };
    }
    if (code !== null) {
      this.forgetInvite();
    }
    await this.hello();
    toast(`Welcome, ${name}! Your estate is ready.`, "success", 7000);
    return null;
  }

  /** Start with friends: your company and a crew, with the station's other seats held for them */
  async startWithFriends(
    name: string,
    color: string,
    crewName: string,
    emblem: string,
    crewColor: string,
    crewColor2: string,
    seat: SeatRefDto,
    holdSeats: number,
  ): Promise<{ error: string; code: string | null } | null> {
    const result = await this.connection.startWithFriends(
      name,
      color,
      crewName,
      emblem,
      crewColor,
      crewColor2,
      seat,
      holdSeats,
    );
    if (!result.success) {
      return { error: result.error ?? "That didn't work.", code: result.code ?? null };
    }
    setState({ shareCodes: result.holds });
    await this.hello();
    return null;
  }

  // ---------------------------------------------------------------- Crews

  async openCrew() {
    setState({ ...MODALS, crewOpen: true });
    await this.refreshCrew();
  }

  async refreshCrew() {
    const crew = await this.connection.getCrew().catch(() => null);
    setState({ crew });
  }

  /** A crew command, then the crew panel again */
  async crewCommand(run: Promise<CommandResult>, done?: string): Promise<boolean> {
    const ok = await this.command(run, done);
    await this.refreshCrew();
    return ok;
  }

  async foundCrew(name: string, emblem: string, color: string, color2: string, holdSeats: number) {
    const result = await this.connection.foundCrew(name, emblem, color, color2, holdSeats);
    if (!result.success) {
      toast(result.error ?? "That didn't work.", "error");
      return;
    }
    setState({ shareCodes: result.holds.length > 0 ? result.holds : null });
    await this.refreshCrew();
  }

  /** Moves to another seat (a free one, or one your crew holds for you) */
  async relocateTo(seat: SeatRefDto) {
    if (await this.command(this.connection.relocateTo(seat), "Up, up and away: your estate is on its way")) {
      setState({ companyOpen: false });
    }
  }

  // ---------------------------------------------------------------- Engine

  attachEngine(engine: Engine) {
    this.engine = engine;
    if (appStore.getState().phase === "playing") {
      this.goHome();
    }
  }

  detachEngine() {
    this.engine = null;
    setState({ mapShown: false });
    delete document.body.dataset.ready;
  }

  get textures() {
    return this.engine?.textureBank ?? null;
  }

  private homedCompany: number | null = null;

  /** Down into the Deep Level, or back up */
  toggleDeep() {
    const state = appStore.getState();
    const company = state.company;
    if (company === null || this.engine === null || (!company.deep.open && !state.deepView)) {
      return;
    }
    if (state.deepView) {
      this.goHome();
    } else {
      this.engine.lookAt(
        company.estateX + ESTATE_SIZE / 2,
        company.estateY + DEEP_OFFSET + ESTATE_SIZE / 2,
        0.42,
      );
    }
  }

  async dig(parcel: number) {
    if (await this.command(this.connection.dig(parcel), "The miners start digging")) {
      await this.refreshCompany();
    }
  }

  goHome() {
    const company = appStore.getState().company;
    if (company !== null && this.engine !== null) {
      this.homedCompany = company.id;
      this.engine?.lookAt(company.estateX + ESTATE_SIZE / 2, company.estateY + ESTATE_SIZE / 2, 0.42);
    }
  }

  zoomBy(steps: number) {
    this.engine?.zoomBy(steps);
  }

  readonly engineCallbacks: EngineCallbacks = {
    getState: () => {
      const state = appStore.getState();
      return {
        content: state.content!,
        company: state.company,
        era: state.era,
        tool: state.tool,
        overlay: state.overlay,
        plannedRoute: state.plannedRoute,
        selectedBuilding: state.selectedBuilding,
        selection: state.selection,
        selectedNetwork:
          state.selectedNetwork === null
            ? null
            : {
                id: state.selectedNetwork,
                bottlenecks: state.networkDetails?.bottlenecks.map((t) => t.path) ?? [],
              },
        serverOffsetMs: state.serverOffsetMs,
        wildTile: state.wildTile,
        hintTile: state.hintTile,
        outpostCheck: state.outpostCheck,
        mapLinks: state.mapLinks,
        focusLink: state.focusLink,
      };
    },
    canRequest: () => this.connection.status === "connected" && appStore.getState().phase === "playing",
    requestChunks: (pairs) => {
      this.connection.getChunks(pairs).then(
        (chunks) => {
          for (const chunk of chunks) {
            this.world?.setChunk(chunk);
          }
          void this.refreshFlows();
        },
        (error: unknown) => console.warn("Could not load chunks", error),
      );
    },
    unwatchChunks: (pairs) => {
      if (this.connection.status === "connected") {
        void this.connection.unwatch(pairs).catch(() => undefined);
      }
    },
    requestDistrict: (x, y) => {
      void this.connection.getDistrict(x, y).then(
        (district) => this.world?.setDistrict(district),
        (error: unknown) => console.warn("Could not load a district", error),
      );
    },
    onHover: (tile, placement) => setState({ hoverTile: tile, placement }),
    onClick: (tile, building, modifiers) => this.clickTile(tile, building, modifiers),
    onSelectBuildings: (ids, add) => this.selectMany(ids, add),
    onPipeRoute: (route) => setState({ pipeRoute: route }),
    onPlacePipes: (route) => void this.placePipes(route),
    onRemovePipes: (tiles) =>
      void this.command(this.connection.removePipes(tiles.flatMap((t) => [t.x, t.y]))),
    onPlaceBuilding: (type, x, y) => void this.placeBuilding(type, x, y),
    onPlaceBuildings: (type, spots) => void this.placeBuildings(type, spots),
    onMoveBuilding: (id, x, y) => void this.moveBuilding(id, x, y),
    onSelectArea: (x, y, width, height) => setState({ blueprintDraft: { x, y, width, height } }),
    onLayer: (deep) => setState({ deepView: deep, selectedDeepParcel: null, selectedParcel: null }),
    onPlaceBlueprint: (blueprint, x, y) => void this.buildBlueprint(blueprint, x, y),
    onZoom: (zoom) => setState({ zoom }),
    onBack: () => this.back(),
    onMapShown: () => {
      setState({ mapShown: true });
      document.body.dataset.ready = "1";
    },
  };

  private async command(promise: Promise<CommandResult>, success?: string): Promise<boolean> {
    try {
      const result = await promise;
      if (!result.success) {
        toast(result.error ?? "That didn't work.", "error");
        return false;
      }
      if (success !== undefined) {
        toast(success, "success");
      }
      this.scheduleNextSteps();
      return true;
    } catch (error) {
      toast(`The server didn't answer (${String(error)}).`, "error");
      return false;
    }
  }

  private async placeBuilding(type: string, x: number, y: number) {
    const state = appStore.getState();
    const placement = state.placement;
    if (placement !== null && !placement.ok) {
      toast(placement.reason ?? "You can't build here.", "error");
      return;
    }
    const name = state.content?.building(type)?.name ?? type;
    // A building from a crate: unpacked as it was, no construction
    if (state.tool.kind === "build" && state.tool.crate !== undefined) {
      if (await this.command(this.connection.placeCrate(state.tool.crate, x, y), `${name} is unpacked`)) {
        this.setTool({ kind: "select" });
      }
      return;
    }
    if (await this.command(this.connection.placeBuilding(type, x, y), `${name} is being built`)) {
      const size = state.content?.building(type)?.size ?? 1;
      setState({ hintTile: null, justBuilt: { x, y, size, name } });
      // Keep building more of the same until the player switches tools
      void this.refreshCompany();
    }
  }

  /** A painted row or area: built in one command, as far as the gold goes */
  private async placeBuildings(type: string, spots: TilePosition[]) {
    const name = appStore.getState().content?.building(type)?.name ?? type;
    const result = await this.connection
      .placeBuildings(
        type,
        spots.flatMap((t) => [t.x, t.y]),
      )
      .catch(() => null);
    if (result === null) {
      toast("The server didn't answer.", "error");
      return;
    }
    if (result.placed > 0) {
      sounds.click();
      toast(
        `${result.placed} × ${name} for ${formatAmount(result.gold)} g${result.error !== null ? ` (${result.error})` : ""}`,
        "success",
      );
      setState({ hintTile: null });
      this.scheduleNextSteps();
      void this.refreshCompany();
    } else {
      toast(result.error ?? "Nothing could be built there.", "error");
    }
  }

  private async moveBuilding(id: number, x: number, y: number) {
    if (await this.command(this.connection.move(id, x, y), "Moved")) {
      setState({ tool: { kind: "select" } });
    }
  }

  private async placePipes(route: TilePosition[]) {
    const state = appStore.getState();
    if (state.company === null || this.world === null || state.tool.kind !== "pipe") {
      return;
    }
    const tier = state.tool.tier;
    const tiles = newPipeTiles(this.world, state.company, route, state.content ?? undefined);
    // A better pipe drawn over the player's own pipes upgrades them
    const upgrades = upgradablePipeTiles(this.world, state.company, route, tier);
    if (tiles.length > 0) {
      await this.command(
        this.connection.placePipes(
          tiles.flatMap((t) => [t.x, t.y]),
          tier,
        ),
      );
    }
    if (upgrades.length > 0) {
      const name = state.content?.pipeTiers.find((t) => t.tier === tier)?.name ?? "pipe";
      await this.command(
        this.connection.upgradePipes(
          upgrades.flatMap((t) => [t.x, t.y]),
          tier,
        ),
        `${upgrades.length} pipe tiles upgraded to ${name}`,
      );
    }
    setState({ pipeRoute: null });
  }

  private clickTile(tile: TilePosition, building: BuildingDto | undefined, modifiers?: ClickModifiers) {
    const state = appStore.getState();
    // Shift adds a building to the selection (or takes it out), Ctrl takes all of its type, a double click all of
    // its type in its network
    const company = state.company;
    if (
      building !== undefined &&
      company !== null &&
      building.owner === company.id &&
      modifiers !== undefined
    ) {
      const own = [...(this.world?.buildings.values() ?? [])].filter((t) => t.owner === company.id);
      if (modifiers.shift) {
        const current =
          state.selection.length > 0
            ? state.selection
            : state.selectedBuilding !== null
              ? [state.selectedBuilding]
              : [];
        this.selectMany(
          current.includes(building.id)
            ? current.filter((t) => t !== building.id)
            : [...current, building.id],
          false,
        );
        return;
      }
      if (modifiers.ctrl) {
        this.selectMany(
          own.filter((t) => t.type === building.type).map((t) => t.id),
          false,
        );
        return;
      }
      if (modifiers.double && building.network !== null) {
        this.selectMany(
          own.filter((t) => t.type === building.type && t.network === building.network).map((t) => t.id),
          false,
        );
        return;
      }
    }
    if (state.tool.kind === "terraform") {
      // Keeps reshaping until the player picks another tool
      void this.command(
        this.connection.terraform(tile.x, tile.y, state.tool.action, state.tool.resource ?? null),
      );
      return;
    }
    if (building !== undefined) {
      this.select(building.id);
      return;
    }
    const conductor = this.world?.conductorAt(tile.x, tile.y);
    if (conductor !== undefined && conductor.kind === "gate" && conductor.owner === state.company?.id) {
      void this.openYard();
      return;
    }
    if (zoneAt(tile.x, tile.y).kind === "yard") {
      void this.openYard();
      return;
    }
    if (conductor !== undefined && conductor.network !== null) {
      this.selectNetwork(conductor.network, tile);
      return;
    }
    if (company !== null && isDeep(tile.y)) {
      // Down in the Deep Level: a parcel still in the rock can be dug out
      const lx = tile.x - company.estateX;
      const ly = tile.y - DEEP_OFFSET - company.estateY;
      const inside = lx >= 0 && ly >= 0 && lx < ESTATE_SIZE && ly < ESTATE_SIZE;
      const parcel = inside ? parcelOf(lx, ly) : null;
      this.select(null);
      if (parcel !== null && (company.deep.parcels & (1 << parcel)) === 0) {
        setState({ selectedDeepParcel: parcel });
      }
      return;
    }
    if (company !== null) {
      const own = ownEstateAt(company, tile.x, tile.y);
      if (own !== undefined) {
        const parcel = parcelOf(tile.x - own.x, tile.y - own.y);
        if (!isParcelUnlocked(own.unlockedParcels, parcel)) {
          this.stopDetails();
          setState({
            selectedParcel: parcel,
            selectedParcelEstate: own.index,
            selectedBuilding: null,
            buildingDetails: null,
          });
          return;
        }
      }
      // The wilderness: an outpost, or a claim
      if (zoneAt(tile.x, tile.y).kind === "wilderness") {
        this.select(null);
        void this.selectWild(tile);
        return;
      }
      // Someone else's estate: visit it
      const estate = this.world?.estateAt(tile.x, tile.y);
      if (estate !== undefined && estate.ownerId !== null && estate.ownerId !== company.id) {
        this.select(null);
        void this.visitEstate(estate.ownerId);
        return;
      }
    }
    this.select(null);
  }

  /** Selects several buildings (one: the building panel; none: nothing); add: to what is selected */
  selectMany(ids: number[], add: boolean) {
    const state = appStore.getState();
    const before = add
      ? state.selection.length > 0
        ? state.selection
        : state.selectedBuilding !== null
          ? [state.selectedBuilding]
          : []
      : [];
    const all = [...new Set([...before, ...ids])];
    if (all.length === 0) {
      this.select(null);
    } else if (all.length === 1) {
      this.select(all[0]!);
    } else {
      this.stopDetails();
      setState({ selection: all });
    }
  }

  /** Selects all the player's buildings on screen */
  selectInView() {
    this.selectMany(this.engine?.ownBuildingsInView() ?? [], false);
  }

  /** Upgrades the selection: each by levels, or with a budget the cheapest levels first */
  async upgradeSelection(levelsEach: number, budget: number | null = null) {
    const ids = appStore.getState().selection;
    if (ids.length === 0) {
      return;
    }
    const result = await this.connection.upgradeMany(ids, levelsEach, budget).catch(() => null);
    if (result === null) {
      toast("That didn't work.", "error");
    } else if (!result.success) {
      toast(result.error ?? "That didn't work.", "error");
    } else {
      ids.forEach((id) => this.engine?.twinkle(id));
      sounds.click();
      toast(`${result.levels} levels for ${formatAmount(result.gold)} g`, "success");
    }
  }

  /** Demolishes the selection, one by one */
  async demolishSelection() {
    const ids = appStore.getState().selection;
    let done = 0;
    for (const id of ids) {
      const result = await this.connection.demolish(id).catch(() => null);
      if (result?.success) {
        done++;
      }
    }
    toast(`${done} buildings demolished`, "info");
    setState({ selection: [] });
  }

  /**
   * One step back (Escape, a right-click on the map): close a window, leave photo mode, cancel a plan or a tool, then
   * deselect
   */
  back() {
    const state = appStore.getState();
    if (state.keysOpen) {
      setState({ keysOpen: false });
    } else if (
      state.researchOpen ||
      state.hubOpen ||
      state.neighboursOpen ||
      state.corporationOpen ||
      state.leaderboardOpen ||
      state.companyOpen ||
      state.blueprintsOpen ||
      state.moderationOpen
    ) {
      setState({ ...MODALS, buildMenuOpen: state.buildMenuOpen });
    } else if (state.photoMode) {
      this.setPhotoMode(false);
    } else if (state.selectedDeepParcel !== null) {
      setState({ selectedDeepParcel: null });
    } else if (state.blueprintDraft !== null) {
      setState({ blueprintDraft: null });
    } else if (state.inboxOpen) {
      setState({ inboxOpen: false });
    } else if (state.chatOpen) {
      // An open panel goes before a tool (run 2: Escape left the chat and the Neighbours panel open)
      setState({ chatOpen: false });
    } else if (state.yardOpen) {
      setState({ yardOpen: false });
    } else if (state.visiting !== null) {
      this.closeVisit();
    } else if (state.wildTile !== null) {
      this.closeWild();
    } else if (state.plannedRoute !== null) {
      setState({ plannedRoute: null, plannedPartner: null, plannedTarget: null, plannedDone: null });
    } else if (state.tool.kind !== "select") {
      this.setTool({ kind: "select" });
    } else if (state.buildMenuOpen) {
      setState({ buildMenuOpen: false });
    } else if (state.selectedNetwork !== null) {
      this.selectNetwork(null);
    } else if (state.selection.length > 0) {
      setState({ selection: [] });
    } else {
      this.select(null);
    }
  }

  select(id: number | null) {
    this.stopDetails();
    setState({
      selectedBuilding: id,
      buildingDetails: null,
      buildingNetwork: null,
      selectedParcel: null,
      selectedNetwork: null,
      networkDetails: null,
    });
    if (id !== null) {
      void this.refreshDetails();
      this.detailsTimer = window.setInterval(() => void this.refreshDetails(), DETAILS_REFRESH_MS);
    }
  }

  /** The pipe a selected network was clicked at: the selection follows the network there when it is rebuilt */
  private networkTile: { x: number; y: number } | null = null;

  selectNetwork(id: number | null, tile: { x: number; y: number } | null = null) {
    this.networkTile = id === null ? null : (tile ?? this.networkTile);
    this.stopDetails();
    setState({
      selectedNetwork: id,
      networkDetails: null,
      selectedBuilding: null,
      buildingDetails: null,
      selectedParcel: null,
    });
    if (id !== null) {
      void this.refreshNetwork();
      this.detailsTimer = window.setInterval(() => void this.refreshNetwork(), DETAILS_REFRESH_MS);
    }
  }

  async refreshNetwork() {
    const id = appStore.getState().selectedNetwork;
    if (id === null || this.connection.status !== "connected") {
      return;
    }
    const details = await this.connection.getNetwork(id).catch(() => null);
    if (appStore.getState().selectedNetwork !== id) {
      return;
    }
    if (details === null) {
      // The network was rebuilt (a pipe changed): it has a new id
      this.followSelectedNetwork([id]);
      return;
    }
    setState({ networkDetails: details });
  }

  /**
   * A rebuilt network has a new id (its pipes come with it, before this): the selection goes on with the network at
   * the pipe it was clicked at, or ends when that pipe is gone
   */
  private followSelectedNetwork(changed: number[]) {
    const id = appStore.getState().selectedNetwork;
    if (id === null || !changed.includes(id)) {
      return;
    }
    const tile = this.networkTile;
    const now = tile === null ? null : (this.world?.conductorAt(tile.x, tile.y)?.network ?? null);
    if (now === null) {
      this.selectNetwork(null);
    } else if (now !== id) {
      this.selectNetwork(now, tile);
    }
  }

  /** Opens the Yard panel (your station, its residents, your trades with them) */
  async openYard() {
    this.selectNetwork(null);
    setState({ yardOpen: true, yardTab: "yard" });
    await this.refreshYard();
  }

  async refreshYard() {
    const yard = await this.connection.getYard().catch(() => null);
    setState({ yard });
  }

  /** The Yard panel on its Links tab: your links to other stations, offers, and who a link would suit */
  async openLinks() {
    this.selectNetwork(null);
    setState({ yardOpen: true, yardTab: "links" });
    await this.refreshLinks();
  }

  /** The Yard panel on its Project tab: the station's project, its basket and who helped */
  async openProject() {
    this.selectNetwork(null);
    setState({ yardOpen: true, yardTab: "project" });
    await this.refreshProject();
  }

  /** A Yard command (pins, Asks); the panel follows */
  async yardCommand(promise: Promise<CommandResult>, success?: string): Promise<boolean> {
    const ok = await this.command(promise, success);
    await this.refreshYard();
    return ok;
  }

  async refreshProject() {
    const project = await this.connection.getProject().catch(() => null);
    setState({ project });
  }

  async refreshLinks() {
    const links = await this.connection.getLinks().catch(() => null);
    setState({ links });
  }

  /** A link command; the panel and the map follow */
  async linkCommand(promise: Promise<CommandResult>, success?: string): Promise<boolean> {
    const ok = await this.command(promise, success);
    await this.refreshLinks();
    return ok;
  }

  async loadMapLinks() {
    const mapLinks = await this.connection.getMapLinks().catch(() => null);
    if (mapLinks !== null) {
      setState({ mapLinks });
    }
  }

  /** Shows a link's route whole: the camera fits it, the map draws it bright */
  showLink(route: number[], id: number) {
    if (route.length < 4 || this.engine === null) {
      return;
    }
    const xs = route.filter((_, i) => i % 2 === 0);
    const ys = route.filter((_, i) => i % 2 === 1);
    setState({ focusLink: id });
    this.engine.lookAtArea(Math.min(...xs), Math.min(...ys), Math.max(...xs), Math.max(...ys));
  }

  async setBuildingSetting(building: number, value: number) {
    if (await this.command(this.connection.setBuildingSetting(building, value))) {
      await this.refreshDetails();
    }
  }

  async startExpedition(hours: number) {
    await this.command(this.connection.startExpedition(hours), "The team sets off");
  }

  /** Photo mode: hides the UI; the time of day colours the world */
  setPhotoMode(on: boolean) {
    setState({
      photoMode: on,
      ...MODALS,
      selectedBuilding: null,
      buildingDetails: null,
      buildMenuOpen: false,
    });
    this.engine?.setPhotoMode(on);
    if (!on) {
      this.setTimeOfDay("day");
    }
  }

  setTimeOfDay(time: "day" | "golden" | "dusk" | "night") {
    setState({ timeOfDay: time });
    this.engine?.setTimeOfDay(time);
  }

  /** Saves the picture on screen as a PNG */
  async savePhoto() {
    if (this.engine === null) {
      return;
    }
    const url = await this.engine.snapshot();
    const link = document.createElement("a");
    link.href = url;
    link.download = `transportpipes-${new Date().toISOString().slice(0, 16).replace(/[:T]/g, "-")}.png`;
    link.click();
  }

  private nextStepsTimer: number | null = null;

  /** The advisor's steps again soon: after a command the network needs a moment to solve */
  scheduleNextSteps(delay = 1500) {
    if (this.nextStepsTimer !== null) {
      clearTimeout(this.nextStepsTimer);
    }
    this.nextStepsTimer = window.setTimeout(() => {
      this.nextStepsTimer = null;
      void this.refreshNextSteps();
    }, delay);
  }

  async refreshNextSteps() {
    const steps = await this.connection.getNextSteps().catch(() => null);
    if (steps !== null) {
      setState({ nextSteps: steps });
    }
  }

  /** The advisor's Show me: looks at the step and opens the tool or panel for it */
  async showNextStep(step: NextStepDto) {
    const state = appStore.getState();
    switch (step.action) {
      case "upgrade":
      case "select":
        if (step.building !== null) {
          this.showBuilding(step.building);
        }
        break;
      case "build": {
        const definition =
          step.buildingType !== null ? state.content?.building(step.buildingType) : undefined;
        if (definition === undefined) {
          break;
        }
        this.setTool({ kind: "build", type: definition.id });
        const overlay: Overlay | null =
          definition.modifier === "wind"
            ? "wind"
            : definition.modifier === "sun"
              ? "sun"
              : definition.placement.deposit !== null
                ? "deposits"
                : null;
        if (overlay !== null) {
          setState({ overlay });
          this.hintOverlay = { step: appStore.getState().company?.tutorial?.id ?? "", overlay };
        }
        if (step.x !== null && step.y !== null) {
          this.engine?.lookAt(step.x, step.y, 0.8);
          setState({ hintTile: { x: step.x, y: step.y, size: definition.size } });
          // Say what is in hand and where it goes (run 2: Dev's Show me for the Lab "opened build mode with nothing chosen")
          toast(`${definition.name} in hand: ${PHONE() ? "tap" : "click"} the golden tile to build it there`);
        }
        break;
      }
      case "research":
        await this.openResearch();
        break;
      case "parcel":
        if (step.parcel !== null) {
          if (step.x !== null && step.y !== null) {
            this.engine?.lookAt(step.x, step.y, 0.5);
          }
          setState({ selectedParcel: step.parcel, selectedParcelEstate: 0 });
        }
        break;
      case "partner":
        if (step.partner !== null) {
          await this.refreshNeighbours();
          await this.planYardPipe();
        }
        break;
      case "connect":
        this.setTool({ kind: "pipe", tier: 1 });
        if (step.x !== null && step.y !== null) {
          this.engine?.lookAt(step.x, step.y, 0.8);
        }
        break;
      case "layPipe":
        await this.layPipeNow(step.tiles, "Laid: its goods can leave now");
        break;
      case "pipes":
        if (step.network !== null && step.x !== null && step.y !== null) {
          this.engine?.lookAt(step.x, step.y, 0.8);
          this.selectNetwork(step.network, { x: step.x, y: step.y });
        }
        break;
    }
  }

  /** Raises the player's own pipes on these tiles to a better tier */
  async upgradePipes(tiles: TilePosition[], tier: number) {
    const name = appStore.getState().content?.pipeTiers.find((t) => t.tier === tier)?.name ?? "pipe";
    if (
      await this.command(
        this.connection.upgradePipes(
          tiles.flatMap((t) => [t.x, t.y]),
          tier,
        ),
        `${tiles.length} pipe tiles upgraded to ${name}`,
      )
    ) {
      window.setTimeout(() => void this.refreshNetwork(), 1200);
    }
  }

  /** Looks at a building and opens its panel */
  showBuilding(id: number) {
    const building = this.world?.buildings.get(id);
    if (building !== undefined) {
      this.engine?.lookAt(building.x, building.y, 0.8);
    }
    this.select(id);
  }

  async skipTutorial() {
    await this.command(this.connection.skipTutorial());
  }

  /** Loady's "Show me": the tool, the overlay and a good spot for the current step */
  showTutorialHint() {
    const state = appStore.getState();
    const step = state.company?.tutorial;
    const company = state.company;
    if (step == null || company === null || this.world === null || state.content === null) {
      return;
    }
    // A pipe Loady can lay (a stuck building's way out, or on a phone the first pipe: no drag there)
    const phone = window.matchMedia?.("(max-width: 720px)").matches === true;
    if (step.pipe !== null && (step.id !== "firstSale" || phone)) {
      this.showPlannedPipe(
        step.pipe,
        `your ${step.pipeTarget ?? "Trading Post"}`,
        "Laid: what it makes is sold now",
      );
      return;
    }
    const hint = step.hint;
    if (hint.overlay !== undefined) {
      setState({ overlay: hint.overlay as Overlay });
      this.hintOverlay = { step: step.id, overlay: hint.overlay as Overlay };
    }
    if (hint.panel === "project") {
      void this.openProject();
      return;
    }
    if (hint.panel === "yard") {
      void this.openYard();
      return;
    }
    if (hint.panel === "neighbours") {
      void this.openNeighbours();
      return;
    }
    if (hint.panel === "hub") {
      void this.openHub("orders");
      return;
    }
    if (hint.panel === "research") {
      void this.openResearch();
      return;
    }
    if (hint.panel === "corporation") {
      void this.openCorporation();
      return;
    }
    if (hint.panel === "parcel") {
      const parcel = buyableParcels(company.unlockedParcels)[0];
      if (parcel !== undefined) {
        const px = company.estateX + (parcel % PARCELS_PER_SIDE) * PARCEL_SIZE + PARCEL_SIZE / 2;
        const py = company.estateY + Math.floor(parcel / PARCELS_PER_SIDE) * PARCEL_SIZE + PARCEL_SIZE / 2;
        this.engine?.lookAt(px, py, 0.5);
        setState({ selectedParcel: parcel, selectedParcelEstate: 0 });
      }
      return;
    }
    if (hint.select !== undefined) {
      // "highest": the building closest to its next milestone (the HQ grows with the Company Level)
      const own =
        hint.select === "highest"
          ? [...this.world.buildings.values()]
              .filter((t) => t.owner === company.id && t.type !== "hq" && t.level < 10)
              .sort((a, b) => b.level - a.level)[0]
          : [...this.world.buildings.values()].find((t) => t.owner === company.id && t.type === hint.select);
      if (own !== undefined) {
        this.engine?.lookAt(own.x, own.y, 0.8);
        this.select(own.id);
      }
      return;
    }
    if (hint.tool === "pipe") {
      this.setTool({ kind: "pipe", tier: 1 });
      const post = [...this.world.buildings.values()].find(
        (t) => t.owner === company.id && t.type === "tradingPost",
      );
      if (post !== undefined) {
        this.engine?.lookAt(post.x, post.y, 0.8);
      }
      return;
    }
    // The Lab is there: what's left is picking a research
    if (
      step.id === "firstResearch" &&
      [...this.world.buildings.values()].some((t) => t.owner === company.id && t.type === "lab")
    ) {
      void this.openResearch();
      return;
    }
    if (hint.tool === "build" && hint.building !== undefined) {
      // A list is built in its order: the first one the company doesn't have yet
      const owned = new Set(
        [...this.world.buildings.values()].filter((t) => t.owner === company.id).map((t) => t.type),
      );
      const list = hint.building.split(",").map((t) => t.trim());
      const type = list.find((t) => !owned.has(t)) ?? list.at(-1)!;
      const definition = state.content.building(type);
      if (definition === undefined) {
        return;
      }
      this.setTool({ kind: "build", type });
      // The best spot nearby for it: where it makes the most, else (a storage) close to the trading post
      const post = [...this.world.buildings.values()].find(
        (t) => t.owner === company.id && t.type === "tradingPost",
      );
      const score = (x: number, y: number, multiplier: number) =>
        Object.keys(definition.out).length > 0 || post === undefined
          ? multiplier
          : -(Math.abs(x - post.x) + Math.abs(y - post.y));
      let best: { x: number; y: number; m: number } | null = null;
      for (let y = company.estateY; y < company.estateY + ESTATE_SIZE; y++) {
        for (let x = company.estateX; x < company.estateX + ESTATE_SIZE; x++) {
          const placement = canPlace(state.content, this.world, company, state.era, definition, x, y);
          if (placement.ok && (best === null || score(x, y, placement.multiplier) > best.m)) {
            best = { x, y, m: score(x, y, placement.multiplier) };
          }
        }
      }
      if (best !== null) {
        this.engine?.lookAt(best.x, best.y, 0.8);
        setState({ hintTile: { x: best.x, y: best.y, size: definition.size } });
        toast(`${definition.name} in hand: ${PHONE() ? "tap" : "click"} the golden tile to build it there`);
      }
    }
  }

  async evolve(building: number, name: string) {
    if (await this.command(this.connection.evolve(building), `Evolving into a ${name}`)) {
      this.select(null);
      void this.refreshCompany();
    }
  }

  async linkTeleporter(sender: number, receiver: number, resource: string) {
    if (await this.command(this.connection.linkTeleporter(sender, receiver, resource), "Linked")) {
      await this.refreshDetails();
    }
  }

  async unlinkTeleporter(station: number) {
    if (await this.command(this.connection.unlinkTeleporter(station))) {
      await this.refreshDetails();
    }
  }

  async setShowroomProduct(building: number, resource: string) {
    if (await this.command(this.connection.setShowroomProduct(building, resource))) {
      await this.refreshDetails();
    }
  }

  async setValveFilter(building: number, resource: string, enabled: boolean) {
    if (await this.command(this.connection.setValveFilter(building, resource, enabled))) {
      await this.refreshDetails();
    }
  }

  private stopDetails() {
    if (this.detailsTimer !== null) {
      clearInterval(this.detailsTimer);
      this.detailsTimer = null;
    }
  }

  async refreshDetails() {
    const id = appStore.getState().selectedBuilding;
    if (id === null || this.connection.status !== "connected") {
      return;
    }
    const details = await this.connection.getBuilding(id).catch(() => null);
    if (appStore.getState().selectedBuilding !== id) {
      return;
    }
    if (details === null) {
      this.select(null);
      return;
    }
    // Its network as a whole too (a network being rebuilt, e.g. after an upgrade, keeps showing the last one)
    const network =
      details.building.network === null
        ? null
        : await this.connection.getNetwork(details.building.network).catch(() => null);
    if (appStore.getState().selectedBuilding !== id) {
      return;
    }
    setState({
      buildingDetails: details,
      buildingNetwork:
        network ?? (details.building.network === null ? null : appStore.getState().buildingNetwork),
    });
  }

  /** Selects the network of the selected building (at one of its pipes, so the selection follows it) */
  showBuildingNetwork() {
    const network = appStore.getState().buildingDetails?.building.network ?? null;
    if (network === null || this.world === null) {
      return;
    }
    const pipe = [...this.world.conductors.values()].find((t) => t.network === network);
    this.selectNetwork(network, pipe === undefined ? null : { x: pipe.x, y: pipe.y });
  }

  private async refreshCompany() {
    const company = await this.connection.getCompany().catch(() => null);
    if (company !== null) {
      this.setCompany(company);
    }
  }

  private flowsRequest: Promise<void> | null = null;

  /** Fetches the flows of the networks on screen (all of them, or only those that changed) */
  async refreshFlows(changed?: number[]) {
    const world = this.world;
    if (world === null || this.connection.status !== "connected") {
      return;
    }
    const visible = world.visibleNetworks();
    const wanted =
      changed === undefined
        ? [...visible].filter((id) => !world.flows.has(id))
        : changed.filter((id) => visible.has(id) || world.flows.has(id));
    const removed = [...world.flows.keys()].filter((id) => !visible.has(id));
    if (wanted.length === 0) {
      if (removed.length > 0) {
        world.setFlows([], removed);
      }
      return;
    }
    while (this.flowsRequest !== null) {
      await this.flowsRequest;
    }
    this.flowsRequest = this.connection.getFlows(wanted.slice(0, 200)).then(
      (flows) => {
        const gone = wanted.filter((id) => !flows.some((f) => f.id === id));
        world.setFlows(flows, [...removed, ...gone]);
      },
      (error: unknown) => console.warn("Could not load flows", error),
    );
    await this.flowsRequest;
    this.flowsRequest = null;
    if (changed !== undefined && appStore.getState().selectedBuilding !== null) {
      void this.refreshDetails();
    }
  }

  // ---------------------------------------------------------------- Commands from the UI

  setTool(tool: Tool) {
    // An overlay a Show me switched on goes off with the tool it came with (run 2: Deposits stayed on for whole sessions)
    const overlayOff =
      tool.kind === "select" &&
      this.hintOverlay !== null &&
      appStore.getState().overlay === this.hintOverlay.overlay;
    setState({
      tool,
      pipeRoute: null,
      hintTile: null,
      buildMenuOpen: tool.kind === "build" ? appStore.getState().buildMenuOpen : false,
      ...(overlayOff ? { overlay: "none" as const } : {}),
    });
    if (overlayOff) {
      this.hintOverlay = null;
    }
  }

  async upgrade(building: number, levels: number) {
    if (await this.command(this.connection.upgrade(building, levels))) {
      this.engine?.twinkle(building);
      sounds.click();
      await this.refreshDetails();
    }
  }

  async repair(building: number) {
    if (await this.command(this.connection.repair(building), "Repaired")) {
      await this.refreshDetails();
    }
  }

  async demolish(building: number) {
    if (await this.command(this.connection.demolish(building), "Demolished")) {
      this.select(null);
    }
  }

  async setImport(building: number, resource: string, enabled: boolean) {
    if (await this.command(this.connection.setImport(building, resource, enabled))) {
      await this.refreshDetails();
    }
  }

  async buyParcel(parcel: number, estate: number) {
    if (await this.command(this.connection.buyParcel(parcel, estate), "New land! Build on it right away.")) {
      setState({ selectedParcel: null });
    }
  }

  canBuyParcel(parcel: number, estate: number): boolean {
    const company = appStore.getState().company;
    const own = company === null ? undefined : ownEstates(company)[estate];
    return own !== undefined && buyableParcels(own.unlockedParcels).includes(parcel);
  }

  /** Looks at one of the company's estates (0 is the first, then the branches) */
  goToEstate(index: number) {
    const company = appStore.getState().company;
    const own = company === null ? undefined : ownEstates(company)[index];
    if (own !== undefined) {
      this.engine?.lookAt(own.x + ESTATE_SIZE / 2, own.y + ESTATE_SIZE / 2, 0.42);
    }
  }

  async openFranchise() {
    if (await this.command(this.connection.openFranchise(null, null))) {
      // Off to the new estate
      const company = appStore.getState().company;
      await this.refreshCompany();
      const after = appStore.getState().company;
      if (after !== null && company !== null && after.branches.length > company.branches.length) {
        this.goToEstate(after.branches.length);
      }
    }
  }

  /**
   * Send ▸ (step 4): a good's spare goes to a neighbour (to: null, the station's project) first, from now on. When the
   * building's pipes don't reach the gate, the same tap lays the pipe to it (gatePipe: x, y pairs).
   */
  async sendGood(
    resource: string,
    to: number | null,
    toName: string,
    building: number | null = null,
    gatePipe: number[] | null = null,
  ) {
    const state = appStore.getState();
    if (gatePipe !== null && gatePipe.length > 0 && this.world !== null && state.company !== null) {
      const route: TilePosition[] = [];
      for (let i = 0; i + 1 < gatePipe.length; i += 2) {
        route.push({ x: gatePipe[i]!, y: gatePipe[i + 1]! });
      }
      const tiles = newPipeTiles(this.world, state.company, route, state.content ?? undefined);
      if (
        tiles.length > 0 &&
        !(await this.command(
          this.connection.placePipes(
            tiles.flatMap((t) => [t.x, t.y]),
            1,
          ),
        ))
      ) {
        return false;
      }
    }
    const good = state.content?.resourceName(resource).toLowerCase() ?? resource;
    const ok = await this.command(
      this.connection.sendGood(resource, to, building),
      `💛 Your ${good} goes to ${toName} first`,
    );
    if (ok) {
      void this.refreshDetails();
      void this.refreshYard();
    }
    return ok;
  }

  async stopSend(resource: string, to: number | null) {
    if (await this.command(this.connection.stopSend(resource, to))) {
      void this.refreshDetails();
      void this.refreshYard();
    }
  }

  /** The research queue (F11): a tech in or out; it starts by itself when the Labs are free and the points are there */
  async queueResearch(tech: string, queued: boolean, name: string) {
    await this.command(
      this.connection.queueResearch(tech, queued),
      queued ? `${name} is in the queue: it starts by itself` : undefined,
    );
  }

  /** With nothing queued, the cheapest tech starts by itself (on by default) */
  async setKeepResearching(keep: boolean) {
    if (await this.command(this.connection.setKeepResearching(keep))) {
      await this.refreshCompany();
    }
  }

  /** 💛 Thanks: once a day per neighbour, no gold, no XP; they hear it */
  async thank(giver: number, resource: string, name: string) {
    if (await this.command(this.connection.thank(giver, resource), `💛 ${name} hears your thanks`)) {
      void this.refreshDetails();
      void this.refreshYard();
    }
  }

  /** Repairs every storm-damaged building (the goal tracker's alert keeps it while damage lasts) */
  async repairAll() {
    if (await this.command(this.connection.repairAll(), "Repaired: full speed again")) {
      await this.refreshCompany();
    }
  }

  /** The return card's buttons: each closes the card on what it shows, Repair all, Send, Thanks and Tend keep it open */
  async returnAction(line: ReturnLineDto): Promise<boolean> {
    switch (line.action) {
      case "send":
        return line.resource !== null && line.partner !== null
          ? await this.sendGood(line.resource, line.partner, "them")
          : false;
      case "thank":
        if (line.partner === null || line.resource === null) {
          return false;
        }
        await this.thank(line.partner, line.resource, line.partnerName ?? "They");
        return true;
      case "tend":
        return line.partner !== null
          ? await this.command(
              this.connection.tend(line.partner),
              `💛 You repaired ${line.partnerName === null ? "their" : line.partnerName + possessive(line.partnerName)} storm damage`,
            )
          : false;
      case "repairAll":
        return await this.command(this.connection.repairAll(), "Repaired: full speed again");
      case "show":
      case "upgrade":
        setState({ returnCard: null });
        if (line.building !== null) {
          this.showBuilding(line.building);
        }
        return true;
      case "project":
        setState({ returnCard: null });
        await this.openProject();
        return true;
      case "links":
        setState({ returnCard: null });
        await this.openLinks();
        return true;
      default:
        return false;
    }
  }

  // ---------------------------------------------------------------- Moving (GDD §12)

  async scrapCrate(crate: number) {
    await this.command(this.connection.scrapCrate(crate), "Scrapped for 75% of its cost");
  }

  placeCrate(crate: number, type: string) {
    setState({ companyOpen: false });
    this.setTool({ kind: "build", type, crate });
  }

  async proposeSwap(other: number) {
    if (await this.command(this.connection.proposeSwap(other), "Swap proposed")) {
      await this.refreshVisit();
    }
  }

  async reserveBlock(x: number, y: number) {
    await this.command(this.connection.reserveBlock(x, y), "Block reserved for 72 hours");
  }

  // ---------------------------------------------------------------- Franchise and season

  async openSeason() {
    const season = await this.connection.getSeason().catch(() => null);
    setState({ season, seasonOpen: season !== null });
  }

  // ---------------------------------------------------------------- Outposts

  async selectWild(tile: TilePosition) {
    setState({ wildTile: tile, outpostCheck: null, visiting: null, visit: null });
    if (this.world?.outpostAt(tile.x, tile.y) !== undefined) {
      return;
    }
    const check = await this.connection.checkOutpost(tile.x, tile.y).catch(() => null);
    if (appStore.getState().wildTile === tile) {
      setState({ outpostCheck: check });
    }
  }

  closeWild() {
    setState({ wildTile: null, outpostCheck: null });
  }

  async claimOutpost() {
    // The best claim the server found around the tile
    const check = appStore.getState().outpostCheck;
    if (
      check !== null &&
      (await this.command(this.connection.claimOutpost(check.x, check.y), "The outpost is yours!"))
    ) {
      setState({ outpostCheck: null });
    }
  }

  async releaseOutpost(outpost: number) {
    if (await this.command(this.connection.releaseOutpost(outpost), "The land went back to the wilderness")) {
      this.closeWild();
    }
  }

  // ---------------------------------------------------------------- Blueprints

  async openBlueprints() {
    setState({ ...MODALS, blueprintsOpen: true });
    await this.refreshBlueprints();
  }

  async refreshBlueprints() {
    const blueprints = await this.connection.getBlueprints().catch(() => null);
    if (blueprints !== null) {
      setState({ blueprints });
    }
  }

  /** Drag a rectangle over the estate to save it */
  startBlueprintArea() {
    setState({ blueprintsOpen: false });
    this.setTool({ kind: "blueprintArea" });
  }

  async saveBlueprint(name: string) {
    const draft = appStore.getState().blueprintDraft;
    if (draft === null) {
      return;
    }
    if (
      await this.command(
        this.connection.saveBlueprint(name, draft.x, draft.y, draft.width, draft.height),
        `Blueprint "${name.trim()}" saved`,
      )
    ) {
      setState({ blueprintDraft: null });
      this.setTool({ kind: "select" });
      await this.openBlueprints();
    }
  }

  holdBlueprint(blueprint: BlueprintDto) {
    setState({ blueprintsOpen: false });
    this.setTool({ kind: "blueprint", blueprint });
  }

  private async buildBlueprint(blueprint: BlueprintDto, x: number, y: number) {
    const placement = appStore.getState().placement;
    if (placement !== null && !placement.ok) {
      toast(placement.reason ?? "Nothing of the blueprint fits here.", "error");
      return;
    }
    if (
      await this.command(
        this.connection.buildBlueprint(blueprint.id, x, y),
        `"${blueprint.name}" is being built`,
      )
    ) {
      // Keep the blueprint in hand to build it again
      void this.refreshCompany();
    }
  }

  async shareBlueprint(blueprint: number, sharing: BlueprintSharing) {
    if (await this.command(this.connection.shareBlueprint(blueprint, sharing))) {
      await this.refreshBlueprints();
    }
  }

  async rateBlueprint(blueprint: number, stars: number) {
    if (await this.command(this.connection.rateBlueprint(blueprint, stars), "Thanks for rating")) {
      await this.refreshBlueprints();
    }
  }

  async deleteBlueprint(blueprint: number) {
    if (await this.command(this.connection.deleteBlueprint(blueprint))) {
      await this.refreshBlueprints();
    }
  }

  // ---------------------------------------------------------------- Company: managers and achievements

  async openCompany() {
    setState({ ...MODALS, companyOpen: true });
    await this.refreshCompanyDetails();
  }

  async refreshCompanyDetails() {
    const companyDetails = await this.connection.getCompanyDetails().catch(() => null);
    if (companyDetails !== null) {
      setState({ companyDetails });
    }
  }

  async hireManager(manager: string) {
    if (await this.command(this.connection.hireManager(manager), "Welcome aboard!")) {
      await this.refreshCompanyDetails();
    }
  }

  async dismissManager(manager: string) {
    if (await this.command(this.connection.dismissManager(manager))) {
      await this.refreshCompanyDetails();
    }
  }

  // ---------------------------------------------------------------- Corporations

  async openCorporation() {
    setState({ ...MODALS, corporationOpen: true });
    await this.refreshCorporations();
  }

  async refreshCorporations() {
    const corporations = await this.connection.getCorporations().catch(() => null);
    if (corporations !== null) {
      setState({ corporations });
    }
  }

  /** Runs a corporation command and shows the result */
  async corporation(run: (c: GameConnection) => Promise<CommandResult>, success?: string) {
    const ok = await this.command(run(this.connection), success);
    if (ok) {
      await this.refreshCorporations();
    }
    return ok;
  }

  foundCorporation = (name: string, tag: string, color: string) =>
    this.corporation((c) => c.foundCorporation(name, tag, color), `${name} is founded!`);
  applyToCorporation = (id: number) => this.corporation((c) => c.applyToCorporation(id), "Application sent");
  inviteToCorporation = (company: number) => this.corporation((c) => c.inviteToCorporation(company));
  declineCorporation = (id: number) => this.corporation((c) => c.declineCorporation(id));
  declineApplication = (company: number) => this.corporation((c) => c.declineApplication(company));
  leaveCorporation = () => this.corporation((c) => c.leaveCorporation(), "You left the corporation");
  kickFromCorporation = (member: number) => this.corporation((c) => c.kickFromCorporation(member));
  setCorporationRole = (member: number, role: CorporationRole) =>
    this.corporation((c) => c.setCorporationRole(member, role));
  donateToCorporation = (gold: number) =>
    this.corporation((c) => c.donateToCorporation(gold), "Thanks for the donation");
  setCorporationSettings = (tax: number, perkResource: string | null, description: string) =>
    this.corporation((c) => c.setCorporationSettings(tax, perkResource, description), "Saved");
  upgradeCorporationHq = () => this.corporation((c) => c.upgradeCorporationHq(), "The HQ grows!");
  deliverToCorporationGoal = () => this.corporation((c) => c.deliverToCorporationGoal(), "Delivered");

  // ---------------------------------------------------------------- Leaderboards and visits

  async openLeaderboard() {
    setState({ ...MODALS, leaderboardOpen: true });
    await this.refreshLeaderboard();
  }

  async refreshLeaderboard(kind?: LeaderboardKind, district?: boolean) {
    const state = appStore.getState();
    const k = kind ?? state.leaderboardKind;
    const d = district ?? state.leaderboardDistrict;
    setState({ leaderboardKind: k, leaderboardDistrict: d });
    const leaderboard = await this.connection.getLeaderboard(k, d).catch(() => null);
    setState({ leaderboard });
  }

  /** Opens the visit card of a company's estate (and looks at the estate) */
  async visitEstate(owner: number, look = false) {
    setState({ ...MODALS, visiting: owner, visit: null });
    await this.refreshVisit();
    const visit = appStore.getState().visit;
    if (look && visit !== null) {
      this.engine?.lookAt(visit.estateX + ESTATE_SIZE / 2, visit.estateY + ESTATE_SIZE / 2, 0.35);
    }
  }

  async refreshVisit() {
    const owner = appStore.getState().visiting;
    if (owner === null) {
      return;
    }
    const visit = await this.connection.visitEstate(owner).catch(() => null);
    if (appStore.getState().visiting === owner) {
      setState({ visit });
    }
  }

  closeVisit() {
    setState({ visiting: null, visit: null });
  }

  lookAtEstate(x: number, y: number) {
    this.engine?.lookAt(x + ESTATE_SIZE / 2, y + ESTATE_SIZE / 2, 0.35);
  }

  async likeEstate(owner: number) {
    if (await this.command(this.connection.likeEstate(owner), "Liked!")) {
      await this.refreshVisit();
    }
  }

  async writeGuestbook(owner: number, text: string) {
    const ok = await this.command(this.connection.writeGuestbook(owner, text), "Note left");
    if (ok) {
      await this.refreshVisit();
    }
    return ok;
  }

  async deleteGuestbookNote(note: number) {
    if (await this.command(this.connection.deleteGuestbookNote(note))) {
      await this.refreshVisit();
    }
  }

  async chooseMentor(mentor: number) {
    if (await this.command(this.connection.chooseMentor(mentor), "You have a mentor!")) {
      await this.refreshVisit();
    }
  }

  // ---------------------------------------------------------------- Inbox

  async openInbox() {
    setState({ inboxOpen: true });
    await this.refreshInbox();
    if (appStore.getState().unread > 0) {
      await this.connection.markInboxRead().catch(() => undefined);
    }
  }

  async refreshInbox() {
    const inbox = await this.connection.getInbox().catch(() => null);
    if (inbox !== null) {
      setState({ inbox });
    }
  }

  // ---------------------------------------------------------------- Chat

  async toggleChat() {
    if (appStore.getState().chatOpen) {
      setState({ chatOpen: false });
      return;
    }
    setState({ chatOpen: true });
    await this.refreshChat();
    const state = appStore.getState();
    const channel = state.chatChannel ?? state.chat?.channels[0]?.id ?? null;
    if (channel !== null) {
      await this.openChannel(channel);
    }
  }

  async openCorporationChat() {
    setState({ corporationOpen: false });
    if (!appStore.getState().chatOpen) {
      setState({ chatOpen: true });
    }
    await this.refreshChat();
    const channel = appStore.getState().chat?.channels.find((t) => t.kind === "corporation");
    if (channel !== undefined) {
      await this.openChannel(channel.id);
    }
  }

  async refreshChat() {
    const chat = await this.connection.getChat().catch(() => null);
    if (chat === null) {
      return;
    }
    const current = appStore.getState().chatChannel;
    const known = current !== null && chat.channels.some((t) => t.id === current);
    setState({ chat, ...(known || current === null ? {} : { chatChannel: chat.channels[0]?.id ?? null }) });
  }

  async openChannel(channel: string) {
    const state = appStore.getState();
    setState({ chatChannel: channel, chatUnread: { ...state.chatUnread, [channel]: 0 } });
    const messages = await this.connection.getChatHistory(channel, null).catch(() => null);
    if (messages !== null) {
      setState({ chatMessages: { ...appStore.getState().chatMessages, [channel]: messages } });
    }
  }

  /** Opens a direct conversation with a company */
  async messageCompany(company: number, name: string) {
    const me = appStore.getState().company;
    if (me === null || me.id === company) {
      return;
    }
    const id = `direct:${Math.min(me.id, company)}:${Math.max(me.id, company)}`;
    const chat = appStore.getState().chat;
    if (chat !== null && !chat.channels.some((t) => t.id === id)) {
      setState({
        chat: { ...chat, channels: [...chat.channels, { id, name, kind: "direct", lastAt: null }] },
      });
    }
    if (!appStore.getState().chatOpen) {
      setState({ chatOpen: true });
      if (appStore.getState().chat === null) {
        await this.refreshChat();
        const loaded = appStore.getState().chat;
        if (loaded !== null && !loaded.channels.some((t) => t.id === id)) {
          setState({
            chat: { ...loaded, channels: [...loaded.channels, { id, name, kind: "direct", lastAt: null }] },
          });
        }
      }
    }
    await this.openChannel(id);
  }

  async sendChat(text: string) {
    const channel = appStore.getState().chatChannel;
    if (channel === null || text.trim() === "") {
      return false;
    }
    return this.command(this.connection.sendChat(channel, text));
  }

  async reportChat(message: number) {
    await this.command(this.connection.reportChat(message), "Reported to the moderators. Thanks!");
  }

  async setBlocked(company: number, blocked: boolean) {
    if (await this.command(this.connection.setBlocked(company, blocked), blocked ? "Blocked" : "Unblocked")) {
      await this.refreshChat();
      const channel = appStore.getState().chatChannel;
      if (channel !== null) {
        await this.openChannel(channel);
      }
    }
  }

  private receiveChat(message: ChatMessageDto) {
    const state = appStore.getState();
    if (state.chat?.blocked.includes(message.sender)) {
      return;
    }
    const loaded = state.chatMessages[message.channel];
    const update: Partial<typeof state> = {};
    if (loaded !== undefined && !loaded.some((t) => t.id === message.id)) {
      update.chatMessages = { ...state.chatMessages, [message.channel]: [...loaded, message].slice(-200) };
    }
    const looking = state.chatOpen && state.chatChannel === message.channel;
    if (!looking && message.sender !== state.company?.id) {
      update.chatUnread = {
        ...state.chatUnread,
        [message.channel]: (state.chatUnread[message.channel] ?? 0) + 1,
      };
    }
    setState(update);
    if (state.chat !== null && !state.chat.channels.some((t) => t.id === message.channel)) {
      void this.refreshChat();
    }
  }

  reset() {
    appStore.setState(initialState);
  }
}

export const gameClient = new GameClient();
