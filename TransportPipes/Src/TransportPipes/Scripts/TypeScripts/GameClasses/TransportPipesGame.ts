/// <reference path="../../typings/pixi/pixi.d.ts" />

declare var kd;

class TransportPipesGame {

    def: Default;

    contentHandler: ContentHandler;
    camera: Camera;

    mouseInputHandler: MouseInputHandler;

    stage: PIXI.Container;
    cameraAffectedContainer: PIXI.Container;
    chunksContainer: PIXI.Container;

    chunks: Chunk[];

    selectedGameObject: GameObject;
    hasSetSelected: boolean;
    shouldHandleSelected: boolean;
    zoomoutSprite: PIXI.Sprite;
    zoominSprite: PIXI.Sprite;

    goldText: PIXI.Text;

    buildWindow: BuildWindow;

    gameObjectInfoHelper: GameObjectInfoHelper;

    selectedGameObjectToBuildContainer: PIXI.Container;
    selectedGameObjectToBuild: PIXI.Sprite;

    // selectorMask: PIXI.Graphics;

    constructor(def: Default) {
        this.def = def;

        this.gameObjectInfoHelper = new GameObjectInfoHelper();

        this.stage = new PIXI.Container();

        this.camera = new Camera();


        this.contentHandler = new ContentHandler();

        this.cameraAffectedContainer = new PIXI.Container();
        this.chunksContainer = new PIXI.Container();
        this.chunksContainer.interactive = true;
        this.selectedGameObjectToBuildContainer = new PIXI.Container();

        this.mouseInputHandler = new MouseInputHandler(this.chunksContainer);

        this.cameraAffectedContainer.addChild(this.chunksContainer);
        this.cameraAffectedContainer.addChild(this.selectedGameObjectToBuildContainer);
        this.stage.addChild(this.cameraAffectedContainer);

        this.buildWindow = new BuildWindow(this.def, this.stage, this.contentHandler);

        this.chunks = [];

        // assign events
        var clickOnChunksContainer = (e: PIXI.interaction.InteractionEvent) => {
            this.buildSelectedGameObject();
        };
        this.chunksContainer.on('click', clickOnChunksContainer);
        this.chunksContainer.on('tap', clickOnChunksContainer);

        this.zoomoutSprite = PIXI.Sprite.fromImage("Images/zoomout.png");
        this.zoominSprite = PIXI.Sprite.fromImage("Images/zoomin.png");

        this.zoominSprite.scale.set(0.5, 0.5);
        this.zoomoutSprite.scale.set(0.5, 0.5);

        this.stage.addChild(this.zoomoutSprite);
        this.stage.addChild(this.zoominSprite);

        this.goldText = new PIXI.Text("", { font: "50px Arial", fill: "red" });
        this.goldText.position = new PIXI.Point(70, 10);
        this.stage.addChild(this.goldText);

        this.zoominSprite.interactive = true;
        this.zoomoutSprite.interactive = true;

        var zoominClick = () => {
            this.mouseInputHandler.scroll += 1;
        };
        var zoomoutClick = () => {
            this.mouseInputHandler.scroll -= 1;
        };

        this.zoominSprite.on('tap', zoominClick);
        this.zoominSprite.on('click', zoominClick);
        this.zoomoutSprite.on('tap', zoomoutClick);
        this.zoomoutSprite.on('click', zoomoutClick);

        this.resize(); // call resize once to fix gui
        $(window).resize(() => {
            this.resize();
        });


        this.registerKeybinds();

        var that = this;
        // activate content loader, after this it will call the start method
        this.contentHandler.assetLoader.load(function (loader, resources) {
            //create the stage
            that.start();
        });
    }

    buildSelectedGameObject() {
        //If we even have a game object selected
        if (this.selectedGameObjectToBuild != null) {
            var gameObjectInfo = this.gameObjectInfoHelper.getGameObjectInfoBasedOnName(this.selectedGameObjectToBuild.name);
            if (gameObjectInfo != null && gameObjectInfo.GameObjectType != -1) {
                var coordibro = this.getMousePosOnTile();
                var tilePos = { x: Math.floor(coordibro.x), y: Math.floor(coordibro.y) };
                //this.def.signalRHandler.setGameObject(tilePos.x, tilePos.y, gameObjectInfo.GameObjectType);

                var blah = new BuildConfirmWindow(this.def, this.stage, this.contentHandler, gameObjectInfo, tilePos);
            }
            this.clearSelectedGameObjectToBuild();
        }
    }

    registerKeybinds() {
        kd.F.press(() => {
            var mousePosOnTile = this.getMousePosOnTile();
            this.def.signalRHandler.setGameObject(Math.floor(mousePosOnTile.x), Math.floor(mousePosOnTile.y), 1);
        });

        kd.M.press(() => {
            var mousePosOnTile = this.getMousePosOnTile();
            this.def.signalRHandler.setGameObject(Math.floor(mousePosOnTile.x), Math.floor(mousePosOnTile.y), 2);
        });

        kd.W.press(() => {
            var mousePosOnTile = this.getMousePosOnTile();
            this.def.signalRHandler.setGameObject(Math.floor(mousePosOnTile.x), Math.floor(mousePosOnTile.y), 200);
        });

        kd.S.press(() => {
            var mousePosOnTile = this.getMousePosOnTile();
            this.def.signalRHandler.setGameObject(Math.floor(mousePosOnTile.x), Math.floor(mousePosOnTile.y), 201);
        });

        kd.O.press(() => {
            var mousePosOnTile = this.getMousePosOnTile();
            this.def.signalRHandler.setGameObject(Math.floor(mousePosOnTile.x), Math.floor(mousePosOnTile.y), 400);
        });

        kd.T.press(() => {
            var mousePosOnTile = this.getMousePosOnTile();
            this.def.signalRHandler.setGameObject(Math.floor(mousePosOnTile.x), Math.floor(mousePosOnTile.y), 450);
        });

        kd.DELETE.press(() => {
            var mousePosOnTile = this.getMousePosOnTile();
            this.def.signalRHandler.deleteGameObject(Math.floor(mousePosOnTile.x), Math.floor(mousePosOnTile.y));
        });

        kd.P.press(() => {
            var mousePosOnTile = this.getMousePosOnTile();
            this.def.signalRHandler.setGameObject(Math.floor(mousePosOnTile.x), Math.floor(mousePosOnTile.y), 10000);
        });

        kd.I.press(() => {
            var mousePosOnTile = this.getMousePosOnTile();
            this.def.signalRHandler.getStatusOfGameObject(Math.floor(mousePosOnTile.x), Math.floor(mousePosOnTile.y));
        });

        kd.C.press(() => {
            this.def.signalRHandler.calculateMyIncome();
        });
    }

    private start() {
        requestAnimationFrame(this.updateLoop.bind(this));
    }

    private resize() {
        this.camera.resize();

        this.zoomoutSprite.anchor.x = 1.0;
        this.zoomoutSprite.position.x = this.camera.stageWidth;
    }

    private updateLoop() {

        if (!def.signalRHandler.signalRUp) {
            console.log("SignalR is still down...");
            // signal R is not up
        } else {
            this.animate();
            this.render();
            this.checkForSelectedEvent();
        }
        requestAnimationFrame(this.updateLoop.bind(this));
    }

    goRequestChunk(chunkX, chunkY) {
        var newChunk = new Chunk(chunkX, chunkY);

        newChunk.moveMouse(this.camera.cameraOffsetX, this.camera.cameraOffsetY);

        this.chunks[chunkX + "_" + chunkY] = newChunk;

        var valuesFromDict = GetValuesFromDict(this.chunks);
        valuesFromDict.sort(Compare);

        var theIndex = valuesFromDict.indexOf(newChunk);

        this.chunksContainer.addChildAt(newChunk.displayContainer, theIndex);

        newChunk.stageMap();

        this.def.signalRHandler.requestChunk(chunkX, chunkY);
    }

    chunkReceivedFromServer(chunkX: number, chunkY: number, terrain: number[][], gameObjects: string[][], ownerData: number[][]) {
        console.log("Received: X: " + chunkX + " Y: " + chunkY);

        var chunkImWorkingOn = <Chunk>this.chunks[chunkX + "_" + chunkY];

        if (chunkImWorkingOn !== undefined) {

            chunkImWorkingOn.terrain = terrain;

            chunkImWorkingOn.loaded = true;

            chunkImWorkingOn.stageMap();

            chunkImWorkingOn.stageGameObjects(gameObjects, this.contentHandler.imageFactory, ownerData);
        }
    }

    getMousePosX() {
        var xposm = this.mouseInputHandler.preLocX / this.camera.scale - this.camera.cameraOffsetX - TILE_WIDTH;
        return xposm;
    }

    getMousePosY() {
        var yposm = this.mouseInputHandler.preLocY / this.camera.scale - this.camera.cameraOffsetY + TILE_HEIGHT;
        return yposm;
    }

    getMousePosOnTile() {
        var xposm = this.getMousePosX();
        var yposm = this.getMousePosY();
        var coordibro = MouseToDiagonal(xposm, yposm);
        return coordibro;
    }

    setSelectedGameObjectToBuild(gameObjectName: string) {
        if (this.selectedGameObjectToBuild != null) {
            this.selectedGameObjectToBuildContainer.removeChild(this.selectedGameObjectToBuild);
        }

        this.selectedGameObjectToBuild = this.contentHandler.imageFactory.createImage(gameObjectName);
        this.selectedGameObjectToBuild.name = gameObjectName;
        this.selectedGameObjectToBuild.position.set(0, -this.selectedGameObjectToBuild.height);
        this.selectedGameObjectToBuildContainer.addChild(this.selectedGameObjectToBuild);
    }

    clearSelectedGameObjectToBuild() {
        if (this.selectedGameObjectToBuild != null) {
            this.selectedGameObjectToBuildContainer.removeChild(this.selectedGameObjectToBuild);
        }

        this.selectedGameObjectToBuild = null;
    }

    private animate() {
        this.camera.update(this.mouseInputHandler, this.cameraAffectedContainer);
        this.mouseInputHandler.updateLocToPreLoc();

        // we can also use method getMousePosOnTile here
        var xposm = this.getMousePosX();
        var yposm = this.getMousePosY();

        var coordibro = MouseToDiagonal(xposm, yposm);
        //var coordiback = DiagonalToMouse(coordibro.x, coordibro.y);

        //this.selectorMask.x = 300;
        //this.selectorMask.y = 300;
        var chunk;
        for (var index in this.chunks) {
            chunk = this.chunks[index];
            chunk.canBeRemoved = true;
        }



        var tilePos = { x: Math.floor(coordibro.x), y: Math.floor(coordibro.y) };

        var tilePosBotLeft = DiagonalToMouse(tilePos.x, tilePos.y);

        this.selectedGameObjectToBuildContainer.position.set(tilePosBotLeft.x + this.camera.cameraOffsetX, tilePosBotLeft.y + this.camera.cameraOffsetY);

        if (this.def.signalRHandler.signalRUp) {
            //Load maps in screen
            //this.textTopLeft.setText(this.camera.topLeft.x + ", " + this.camera.topLeft.y);
            //this.textTopRight.setText(this.camera.topRight.x + ", " + this.camera.topRight.y);
            //this.textBotLeft.setText(this.camera.botLeft.x + ", " + this.camera.botLeft.y);
            //this.textBotRight.setText(this.camera.botRight.x + ", " + this.camera.botRight.y);

            for (var yy = this.camera.topRight.y; yy <= this.camera.botLeft.y; yy++) {
                for (var xx = this.camera.topLeft.x; xx <= this.camera.botRight.x; xx++) {

                    var possibleChunk = this.chunks[xx + "_" + yy];

                    if (possibleChunk === undefined) {
                        this.goRequestChunk(xx, yy);
                    } else {
                        //For all chunks that are in view set canberemoved to false
                        possibleChunk.canBeRemoved = false;
                    }
                }
            }
        }

        this.selectedGameObject = null;

        var chunksLoaded = 0;
        //Remove alle chunks waar c.CanBeRemoved == true
        for (index in this.chunks) {
            chunk = this.chunks[index];
            if (chunk.canBeRemoved) {
                this.chunksContainer.removeChild(chunk.displayContainer);
                delete this.chunks[index];
            } else {
                var selectedGameObject = chunk.update(coordibro.x, coordibro.y, this.shouldHandleSelected);
                if (selectedGameObject != null) {
                    this.selectedGameObject = selectedGameObject;
                }
                chunksLoaded++;
            }
        }


        if (def.currentUser !== null) {
            var curGold = def.currentUser.userResourceHandler.getCurrentGold();
            this.goldText.text = "Gold: " + Math.round(curGold) + " (" + (Math.round(def.currentUser.userResourceHandler.goldPerSecond * 10000) / 10000) + " per second)"; //Rounding is * 100 / 100 because javascript can't round on 2 decimals...
        }
        
        // this.textdingetje.setText("x: " + xposm + " y: " + yposm + "\n\rx: " + coordibro.x + " y: " + coordibro.y + "\n\rx: " + coordiback.x + " y: " + coordiback.y + "\n\rMouseX: " + this.mouseInputHandler.mouseX + " MouseY: " + this.mouseInputHandler.mouseY + "\n\rChunks Loaded: " + chunksLoaded);

        this.camera.moveChunks(this.chunks);
    }

    private render() {
        this.camera.render(this.stage);
    }

    private checkForSelectedEvent() {
        if (this.mouseInputHandler.isMouseDragging) {
            if (!this.hasSetSelected) {
                this.shouldHandleSelected = true;
            }
            this.hasSetSelected = true;
        } else {
            this.hasSetSelected = false;
            this.shouldHandleSelected = false;
        }
    }
}