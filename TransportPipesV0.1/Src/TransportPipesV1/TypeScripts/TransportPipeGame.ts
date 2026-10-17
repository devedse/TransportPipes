class TransportPipeGame {

    signalRup: boolean;

    serverConnectionThing;

    //isMouseDragging: boolean = false;

    // account for tile thickness
    /* globals PIXI, kd, requestAnimFrame */
    "use strict";

    // avatar is 32x32 + 6px for shadow
    AVATAR_X_OFFSET: number;
    AVATAR_Y_OFFSET: number;
    //MAP_WIDTH: number;
    //MAP_HEIGHT: number;
    //STAGE_WIDTH: number;
    //STAGE_HEIGHT: number;
    //THICKNESS: number;

    SKEW_X_OFFSET: number;
    SKEW_Y_OFFSET: number;

    avatar// = PIXI.MovieClip(texturearray);
    stage;
    //renderer;

    loader;
    chunksContainer

    style;
    style2;
    style3;

    textTopLeft;
    textTopRight;
    textBotLeft;
    textBotRight;

    serverText;
    textdingetje;

    chunks: Chunk[];

    //preLocX: number = 0;
    //preLocY: number = 0;



    //mouseX: number = 0;
    //mouseY: number = 0;



    camera: Camera;

    mouseInputHandler: MouseInputHandler;

    imageFactory: ImageFactory;

    constructor() {
        //this.STAGE_WIDTH = 1024;
        //this.STAGE_HEIGHT = 768;

        //this.THICKNESS = 0; // 10 pixels of dirt height

        this.stage = new PIXI.Stage(0xEEFFFF);


        this.camera = new Camera();


        this.mouseInputHandler = new MouseInputHandler();


        this.loader = new PIXI.AssetLoader(['Images/roadTiles/transportTiles.json', 'Images/roadTiles/factorysmall.json', 'Images/CombinedGameObjects/combinedjson.json']);
        this.imageFactory = new ImageFactory(this.loader, "Images/CombinedGameObjects/combinedjsoninfo.json");

        this.chunksContainer = new PIXI.DisplayObjectContainer()
        this.stage.addChild(this.chunksContainer);

        this.style = { font: "20px Arial", fill: "blue" };
        this.style2 = { font: "40px Arial", fill: "red" };
        this.style3 = { font: "20px Arial", fill: "black" };

        this.textTopLeft = new PIXI.Text("", this.style);
        this.textTopRight = new PIXI.Text("", this.style);
        this.textBotLeft = new PIXI.Text("", this.style);
        this.textBotRight = new PIXI.Text("", this.style);

        this.stage.addChild(this.textTopLeft);
        this.stage.addChild(this.textTopRight);
        this.stage.addChild(this.textBotLeft);
        this.stage.addChild(this.textBotRight);


        this.serverText = new PIXI.Text("- no server message -", this.style3);
        this.serverText.position.y = 40;
        this.stage.addChild(this.serverText);

        this.textdingetje = new PIXI.Text("Pixi.js can has text!", this.style2);
        this.textdingetje.position.y = 120;
        this.stage.addChild(this.textdingetje);

        this.chunks = [];
    }

    render() {
        this.camera.Render(this.stage);
    }

    resize() {
        this.camera.Resize();

        this.textTopRight.position.x = this.camera.STAGE_WIDTH - 100;
        this.textBotLeft.position.y = this.camera.STAGE_HEIGHT - 25;
        this.textBotRight.position.x = this.camera.STAGE_WIDTH - 100;
        this.textBotRight.position.y = this.camera.STAGE_HEIGHT - 25;
    }

    //Stages the avatar and returns it, although its also declared in an easily (danke sehr jabaschriepte!) variable ;^)
    stageAvatar(x: number, y: number, chunk: Chunk) {
        //Call this method before moving avatar PLS

        var t1 = PIXI.Texture.fromFrame("factorysmall1.png");
        var t2 = PIXI.Texture.fromFrame("factorysmall2.png");
        var t3 = PIXI.Texture.fromFrame("factorysmall3.png");
        var t4 = PIXI.Texture.fromFrame("factorysmall4.png");
        var t5 = PIXI.Texture.fromFrame("factorysmall5.png");
        var t6 = PIXI.Texture.fromFrame("factorysmall6.png");
        var t7 = PIXI.Texture.fromFrame("factorysmall7.png");

        var texturearray: Array<any> = [t1, t2, t3, t4, t5, t6, t7];

        this.avatar = new PIXI.MovieClip(texturearray);

        this.avatar.animationSpeed = 0.2;
        this.avatar.play();


        // track 2D position
        this.avatar.diagonalLocation = new PIXI.Point(x, y);

        var pt = diagonalToMouse(x, y);
        this.avatar.position.x = pt.x;
        this.avatar.position.y = pt.y;
        this.avatar.anchor.x = 0;
        this.avatar.anchor.y = 1;

        //chunk.mapChunk.addChild(avatar);
        chunk.gameObjectsContainer.addChild(this.avatar);
        return this.avatar;
    }

    moveAvatar(byX, byY) {

        this.avatar.diagonalLocation.x += byX;
        this.avatar.diagonalLocation.y += byY;

        var pt = diagonalToMouse(this.avatar.diagonalLocation.x, this.avatar.diagonalLocation.y);
        this.avatar.position.x = pt.x;
        this.avatar.position.y = pt.y;
    }

    //move avatar
    moveUp() {
        this.moveAvatar(0, -1);
    }


    moveDown() {
        this.moveAvatar(0, 1);
    }


    moveLeft(e) {
        this.moveAvatar(-1, 0);
    }


    moveRight(e) {
        this.moveAvatar(1, 0);
    }

    animate() {
        this.camera.Update(this.mouseInputHandler, this.chunksContainer);
        this.mouseInputHandler.updateLocToPreLoc();


        var xposm = this.mouseInputHandler.preLocX / this.camera.scale - this.camera.cameraOffsetX - TILE_WIDTH;
        var yposm = this.mouseInputHandler.preLocY / this.camera.scale - this.camera.cameraOffsetY + TILE_HEIGHT;

        var coordibro = mouseToDiagonal(xposm, yposm);


        var coordiback = diagonalToMouse(coordibro.x, coordibro.y);






        for (var index in this.chunks) {
            var chunk = this.chunks[index];
            chunk.canBeRemoved = true;
        }





        if (this.serverConnectionThing.signalRup) {
            //Load maps in screen


            this.textTopLeft.setText(this.camera.topLeft.x + ", " + this.camera.topLeft.y);
            this.textTopRight.setText(this.camera.topRight.x + ", " + this.camera.topRight.y);
            this.textBotLeft.setText(this.camera.botLeft.x + ", " + this.camera.botLeft.y);
            this.textBotRight.setText(this.camera.botRight.x + ", " + this.camera.botRight.y);

            for (var yy = this.camera.topRight.y; yy <= this.camera.botLeft.y; yy++) {
                for (var xx = this.camera.topLeft.x; xx <= this.camera.botRight.x; xx++) {

                    var possibleChunk = this.chunks[xx + "_" + yy];

                    if (possibleChunk == undefined) {
                        this.GoRequestChunk(xx, yy);
                    } else {
                        //For all chunks that are in view set canberemoved to false
                        possibleChunk.canBeRemoved = false;
                    }
                }
            }
        }


        var chunksLoaded = 0;
        //Remove alle chunks waar c.CanBeRemoved == true
        for (index in this.chunks) {
            var chunk = this.chunks[index];
            if (chunk.canBeRemoved) {
                this.chunksContainer.removeChild(chunk.displayContainer);
                delete this.chunks[index];
            } else {
                chunksLoaded++;
            }
        }

        this.textdingetje.setText("x: " + xposm + " y: " + yposm + "\n\rx: " + coordibro.x + " y: " + coordibro.y + "\n\rx: " + coordiback.x + " y: " + coordiback.y + "\n\rMouseX: " + this.mouseInputHandler.mouseX + " MouseY: " + this.mouseInputHandler.mouseY + "\n\rChunks Loaded: " + chunksLoaded);

        this.camera.MoveChunks(this.chunks);


    }

    GoRequestChunk(chunkX, chunkY) {
        var newChunk = new Chunk(chunkX, chunkY);

        newChunk.moveMouse(this.camera.cameraOffsetX, this.camera.cameraOffsetY);

        this.chunks[chunkX + "_" + chunkY] = newChunk;

        var valuesFromDict = GetValuesFromDict(this.chunks);
        valuesFromDict.sort(Compare);

        var theIndex = valuesFromDict.indexOf(newChunk);
        
        this.chunksContainer.addChildAt(newChunk.displayContainer, theIndex);
        
        newChunk.stageMap();

        this.serverConnectionThing.hub.server.requestChunk(chunkX, chunkY);
    }
}