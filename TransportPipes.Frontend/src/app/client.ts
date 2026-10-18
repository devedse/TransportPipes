import type { BuildingDto, CommandResult, CompanyDto, ToastDto } from "../api/contracts";
import { auth, fetchContent, GameConnection } from "../api/server";
import { Content } from "../game/content";
import type { Engine, EngineCallbacks, Tool } from "../game/Engine";
import type { TilePosition } from "../game/isometric";
import { buyableParcels, ESTATE_SIZE, isParcelUnlocked, parcelOf } from "../game/layout";
import { newPipeTiles } from "../game/rules";
import { WorldModel } from "../game/world";
import { appStore, initialState, setState, toast } from "../state/store";

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
    c.on("districtChanged", (district) => this.world?.setDistrict(district));
    c.on("companyChanged", (company) => this.setCompany(company));
    c.on("networksChanged", (networks) => void this.refreshFlows(networks));
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
    c.on("toast", (t: ToastDto) => {
      toast(
        t.text,
        t.kind === "levelUp" || t.kind === "researchFinished" || t.kind === "orderCompleted"
          ? "level"
          : "success",
        t.kind === "constructionFinished" ? 4500 : 7000,
      );
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
  }

  private setCompany(company: CompanyDto) {
    const previous = appStore.getState().company;
    setState({ company, companyAt: Date.now() });
    if (
      previous !== null &&
      (previous.unlockedParcels !== company.unlockedParcels || previous.level !== company.level)
    ) {
      this.engine?.invalidate();
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

  async openHub() {
    setState({ hubOpen: true, researchOpen: false, buildMenuOpen: false });
    await this.refreshMarket();
  }

  async refreshMarket() {
    const market = await this.connection.getMarket().catch(() => null);
    if (market !== null) {
      setState({ market });
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

  async openResearch() {
    setState({ researchOpen: true, hubOpen: false, buildMenuOpen: false });
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
        selectedBuilding: state.selectedBuilding,
        selectedNetwork:
          state.selectedNetwork === null
            ? null
            : {
                id: state.selectedNetwork,
                bottlenecks: state.networkDetails?.bottlenecks.map((t) => t.path) ?? [],
              },
        serverOffsetMs: state.serverOffsetMs,
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
    if (await this.command(this.connection.placeBuilding(type, x, y), `${name} is being built`)) {
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
    if (building !== undefined) {
      this.select(building.id);
      return;
    }
    const conductor = this.world?.conductorAt(tile.x, tile.y);
    if (conductor !== undefined && conductor.network !== null) {
      this.selectNetwork(conductor.network);
      return;
    }
    const company = state.company;
    if (company !== null) {
      const lx = tile.x - company.estateX;
      const ly = tile.y - company.estateY;
      if (lx >= 0 && ly >= 0 && lx < ESTATE_SIZE && ly < ESTATE_SIZE) {
        const parcel = parcelOf(lx, ly);
        if (!isParcelUnlocked(company.unlockedParcels, parcel)) {
          this.stopDetails();
          setState({ selectedParcel: parcel, selectedBuilding: null, buildingDetails: null });
          return;
        }
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
      buildMenuOpen: tool.kind === "build" ? appStore.getState().buildMenuOpen : false,
    });
  }

  async upgrade(building: number, levels: number) {
    if (await this.command(this.connection.upgrade(building, levels))) {
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

  async buyParcel(parcel: number) {
    if (await this.command(this.connection.buyParcel(parcel), "New land! Build on it right away.")) {
      setState({ selectedParcel: null });
    }
  }

  canBuyParcel(parcel: number): boolean {
    const company = appStore.getState().company;
    return company !== null && buyableParcels(company.unlockedParcels).includes(parcel);
  }

  closeReport() {
    setState({ report: null });
  }

  reset() {
    appStore.setState(initialState);
  }
}

export const gameClient = new GameClient();
