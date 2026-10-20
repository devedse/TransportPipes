import type {
  BlueprintDto,
  BlueprintSharing,
  BuildingDto,
  ChatMessageDto,
  CommandResult,
  CompanyDto,
  CorporationRole,
  GatePolicy,
  GateSide,
  LeaderboardKind,
  ToastDto,
} from "../api/contracts";
import { auth, fetchContent, GameConnection } from "../api/server";
import { Content } from "../game/content";
import type { Engine, EngineCallbacks, Overlay, Tool } from "../game/Engine";
import type { TilePosition } from "../game/isometric";
import {
  buyableParcels,
  DEEP_OFFSET,
  ESTATE_SIZE,
  PARCEL_SIZE,
  PARCELS_PER_SIDE,
  GATE_SIDES,
  gateTiles,
  isDeep,
  isParcelUnlocked,
  parcelOf,
  zoneAt,
} from "../game/layout";
import { canPlace, newPipeTiles, ownEstateAt, ownEstates, routePipe } from "../game/rules";
import { WorldModel } from "../game/world";
import { notify } from "./notifications";
import { sounds } from "./sound";
import { appStore, initialState, MODALS, setState, toast, type HubTab } from "../state/store";

const DETAILS_REFRESH_MS = 2000;

/** Connects the server, the world model, the map engine and the UI state */
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
    const user = await auth.me().catch(() => null);
    if (user === null) {
      setState({ phase: "login" });
      return;
    }
    setState({ user });
    await this.connect();
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
    c.on("networksChanged", (networks) => void this.refreshFlows(networks));
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
    });
  }

  private async hello() {
    const hello = await this.connection.hello();
    setState({
      user: hello.user,
      era: hello.era,
      serverOffsetMs: new Date(hello.serverTime).getTime() - Date.now(),
      report: hello.report,
      unread: hello.unread,
    });
    if (hello.company === null) {
      setState({ phase: "found" });
      return;
    }
    this.setCompany(hello.company);
    setState({ phase: "playing" });
    if (this.homedCompany !== hello.company.id) {
      this.goHome();
    }
    if (hello.company.season.endedAt !== null) {
      void this.openSeason();
    }
  }

  private setCompany(company: CompanyDto) {
    const previous = appStore.getState().company;
    setState({ company, companyAt: Date.now(), era: company.era });
    this.world?.setDeepLevel(company.estateX, company.estateY, company.deep.parcels);
    if (company.season.endedAt !== null && previous?.season.endedAt === null) {
      // The ceremony
      void this.openSeason();
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
    setState({ ...MODALS, neighboursOpen: true });
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
   * Plans the pipe from your nearest gate to a neighbour's through the corridors and shows it on the map. The chunks on
   * the way have to be loaded, so the camera moves there first and the route is found when they are.
   */
  async planConnection(partner: number) {
    const state = appStore.getState();
    const neighbour = state.neighbours.find((t) => t.id === partner);
    const company = state.company;
    if (neighbour === undefined || company === null || this.world === null) {
      return;
    }
    const pairs = GATE_SIDES.flatMap((mine) =>
      GATE_SIDES.map((theirs) => ({
        from: gateTiles(company.estateX, company.estateY, mine),
        to: gateTiles(neighbour.estateX, neighbour.estateY, theirs),
        policy: neighbour.gatePolicies[theirs] ?? "open",
      })),
    )
      .filter((t) => t.policy !== "closed")
      .sort(
        (a, b) =>
          Math.abs(a.from.outside.x - a.to.outside.x) +
          Math.abs(a.from.outside.y - a.to.outside.y) -
          (Math.abs(b.from.outside.x - b.to.outside.x) + Math.abs(b.from.outside.y - b.to.outside.y)),
      );
    const best = pairs[0];
    if (best === undefined) {
      toast(`${neighbour.name} has closed all its gates.`, "error");
      return;
    }
    setState({ neighboursOpen: false, tool: { kind: "select" } });
    this.engine?.lookAt(
      (best.from.outside.x + best.to.outside.x) / 2,
      (best.from.outside.y + best.to.outside.y) / 2,
      0.3,
    );
    for (let attempt = 0; attempt < 20; attempt++) {
      await new Promise((resolve) => setTimeout(resolve, 300));
      const current = appStore.getState().company;
      if (current === null) {
        return;
      }
      const route = routePipe(
        this.world,
        current,
        best.from.outside,
        best.to.outside,
        state.content ?? undefined,
        8000,
      );
      if (route !== null) {
        setState({ plannedRoute: route, plannedPartner: partner });
        return;
      }
    }
    toast("No free way through the corridors to their gate.", "error");
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
        "Connected! Trade starts right away.",
      )
    ) {
      setState({ plannedRoute: null, plannedPartner: null });
    }
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

  async deliverOrder(order: number) {
    if (await this.command(this.connection.deliverOrder(order))) {
      await this.refreshCompany();
    }
  }

  async setSellPrice(resource: string, price: number | null) {
    if (await this.command(this.connection.setSellPrice(resource, price))) {
      await this.refreshMarket();
    }
  }

  async setBuyMaxPrice(resource: string, price: number | null) {
    if (await this.command(this.connection.setBuyMaxPrice(resource, price))) {
      await this.refreshMarket();
    }
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

  async foundCompany(name: string, color: string): Promise<string | null> {
    const result = await this.connection.createCompany(name, color);
    if (!result.success) {
      return result.error ?? "That didn't work.";
    }
    await this.hello();
    toast(`Welcome, ${name}! Your estate is ready.`, "success", 7000);
    return null;
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
    onClick: (tile, building) => this.clickTile(tile, building),
    onPipeRoute: (route) => setState({ pipeRoute: route }),
    onPlacePipes: (route) => void this.placePipes(route),
    onRemovePipes: (tiles) =>
      void this.command(this.connection.removePipes(tiles.flatMap((t) => [t.x, t.y]))),
    onPlaceBuilding: (type, x, y) => void this.placeBuilding(type, x, y),
    onMoveBuilding: (id, x, y) => void this.moveBuilding(id, x, y),
    onSelectArea: (x, y, width, height) => setState({ blueprintDraft: { x, y, width, height } }),
    onLayer: (deep) => setState({ deepView: deep, selectedDeepParcel: null, selectedParcel: null }),
    onPlaceBlueprint: (blueprint, x, y) => void this.buildBlueprint(blueprint, x, y),
    onZoom: (zoom) => setState({ zoom }),
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
      setState({ hintTile: null });
      // Keep building more of the same until the player switches tools
      void this.refreshCompany();
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
    const tiles = newPipeTiles(this.world, state.company, route, state.content ?? undefined);
    if (tiles.length === 0) {
      return;
    }
    await this.command(
      this.connection.placePipes(
        tiles.flatMap((t) => [t.x, t.y]),
        state.tool.tier,
      ),
    );
    setState({ pipeRoute: null });
  }

  private clickTile(tile: TilePosition, building: BuildingDto | undefined) {
    const state = appStore.getState();
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
    if (
      conductor !== undefined &&
      conductor.kind === "gate" &&
      conductor.owner === state.company?.id &&
      conductor.side !== null
    ) {
      this.selectNetwork(null);
      setState({ selectedGate: conductor.side });
      return;
    }
    if (conductor !== undefined && conductor.network !== null) {
      this.selectNetwork(conductor.network);
      return;
    }
    const company = state.company;
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

  select(id: number | null) {
    this.stopDetails();
    setState({
      selectedBuilding: id,
      buildingDetails: null,
      selectedParcel: null,
      selectedNetwork: null,
      networkDetails: null,
    });
    if (id !== null) {
      void this.refreshDetails();
      this.detailsTimer = window.setInterval(() => void this.refreshDetails(), DETAILS_REFRESH_MS);
    }
  }

  selectNetwork(id: number | null) {
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
      this.selectNetwork(null);
      return;
    }
    setState({ networkDetails: details });
  }

  async setGatePolicy(side: GateSide, policy: GatePolicy) {
    await this.command(this.connection.setGatePolicy(side, policy), "Gate changed");
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
    const hint = step.hint;
    if (hint.overlay !== undefined) {
      setState({ overlay: hint.overlay as Overlay });
    }
    if (hint.panel === "neighbours") {
      void this.openNeighbours();
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
      const own = [...this.world.buildings.values()].find(
        (t) => t.owner === company.id && t.type === hint.select,
      );
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
    if (hint.tool === "build" && hint.building !== undefined) {
      const definition = state.content.building(hint.building);
      if (definition === undefined) {
        return;
      }
      this.setTool({ kind: "build", type: hint.building });
      // The best spot nearby for it: where it makes the most
      let best: { x: number; y: number; m: number } | null = null;
      for (let y = company.estateY; y < company.estateY + ESTATE_SIZE; y++) {
        for (let x = company.estateX; x < company.estateX + ESTATE_SIZE; x++) {
          const placement = canPlace(state.content, this.world, company, state.era, definition, x, y);
          if (placement.ok && (best === null || placement.multiplier > best.m)) {
            best = { x, y, m: placement.multiplier };
          }
        }
      }
      if (best !== null) {
        this.engine?.lookAt(best.x, best.y, 0.8);
        setState({ hintTile: { x: best.x, y: best.y, size: definition.size } });
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
    setState({ buildingDetails: details });
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
    setState({
      tool,
      pipeRoute: null,
      hintTile: null,
      buildMenuOpen: tool.kind === "build" ? appStore.getState().buildMenuOpen : false,
    });
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

  closeReport() {
    setState({ report: null });
  }

  /** The one-tap upgrade of a full storage in the offline report */
  async upgradeFromReport(building: number) {
    if (await this.command(this.connection.upgrade(building, 1), "Upgraded: room for more")) {
      const report = appStore.getState().report;
      if (report !== null) {
        setState({
          report: { ...report, fullStorages: report.fullStorages.filter((t) => t.building !== building) },
        });
      }
    }
  }

  // ---------------------------------------------------------------- Moving (GDD §12)

  async relocate(x: number, y: number) {
    if (await this.command(this.connection.relocate(x, y), "Up, up and away: your estate lands in an hour")) {
      setState({ companyOpen: false });
    }
  }

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
