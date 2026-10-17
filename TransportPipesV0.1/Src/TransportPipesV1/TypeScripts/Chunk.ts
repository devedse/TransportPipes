/// <reference path='Tile.ts'/>
/// <reference path='TileTemplate.ts'/>
/// <reference path='DiagonalHelper.ts'/>
/// <reference path='GameObject.ts'/>

declare var PIXI;

class Chunk {

    chunkX: number;
    chunkY: number;

    xOffsetChunk: number;
    yOffsetChunk: number;

    loaded: boolean;
    canBeRemoved: boolean;

    displayContainer = new PIXI.DisplayObjectContainer();
    tilesContainer = new PIXI.DisplayObjectContainer();
    gameObjectsContainer = new PIXI.DisplayObjectContainer();

    allTiles: Tile[];
    tileTemplates: TileTemplate[];

    terrain: number[][];

    gameObjects: GameObject[];

    constructor(chunkX: number, chunkY: number) {
        this.chunkX = chunkX;
        this.chunkY = chunkY;

        this.xOffsetChunk = (8 * TILE_WIDTH) * (chunkX - chunkY);
        this.yOffsetChunk = (8 * (TILE_HEIGHT / 2)) * (chunkX + chunkY);

        this.displayContainer.addChild(this.tilesContainer);
        this.displayContainer.addChild(this.gameObjectsContainer);

        this.allTiles = [];

        var grass = new TileTemplate("grass.png");
        var beach = new TileTemplate("beach.png");
        var water = new TileTemplate("water.png");
        var loading = new TileTemplate("loading.png");

        this.tileTemplates = [grass, beach, water, loading];

        var G = 0,
            B = 1,
            W = 2,
            L = 3; //UNLOADORD aka fragtagey drawen
        this.terrain = [
            [L, L, L, L, L, L, L, L],
            [L, L, L, L, L, L, L, L],
            [L, L, L, L, L, L, L, L],
            [L, L, L, L, L, L, L, L],
            [L, L, L, L, L, L, L, L],
            [L, L, L, L, L, L, L, L],
            [L, L, L, L, L, L, L, L],
            [L, L, L, L, L, L, L, L],
        ]


    }


    moveMouse(xOffset, yOffset) {
        this.displayContainer.position.x = this.xOffsetChunk + xOffset;
        this.displayContainer.position.y = this.yOffsetChunk + yOffset;
    }

    //This method generates the tiles for the chunk
    stageMap() {
        //var stageTile, tileType, x, y, iso;

        //Clear the tiles first
        for (var index in this.allTiles) {
            this.tilesContainer.removeChild(this.allTiles[index].tileSprite);
        }
        this.allTiles = [];


        for (var x = 0; x < this.terrain.length; x++) {
            for (var y = 0; y < this.terrain[x].length; y++) {
                var iso = diagonalToMouse(x, y);

                var tileType = this.terrain[x][y];
                var tileTemplate = this.tileTemplates[tileType];
                var tile = tileTemplate.createTile(iso.x, iso.y);

                this.tilesContainer.addChild(tile.tileSprite);
                this.allTiles.push(tile);
            }
        }


        //for (var i = 0, iL = this.terrain.length; i < iL; i++) {
        //    for (var j = 0, jL = this.terrain[i].length; j < jL; j++) {
        //        // dd 2D coordinate
        //        //x = j * TILE_WIDTH;
        //        //y = i * TILE_HEIGHT;

        //        var x = j;
        //        var y = i;

        //        // iso coordinate
        //        //iso = ddToIso(x, y);
        //        var iso = diagonalToMouse(x, y);

        //        var tileType = this.terrain[i][j];
        //        var tileTemplate = this.tileTemplates[tileType];
        //        var tile = tileTemplate.createTile(iso.x, iso.y);

        //        this.tilesContainer.addChild(tile.tileSprite);
        //        this.allTiles.push(tile);
        //    }
        //}
    }

    stageGameObjects(gameObjects: string[][], imageFactory: ImageFactory) {


        for (var index in this.gameObjects) {
            this.gameObjectsContainer.removeChild(this.gameObjects[index].sprite);
        }
        this.gameObjects = [];


        for (var x = 0; x < gameObjects.length; x++) {
            for (var y = 0; y < gameObjects[x].length; y++) {

                var gameObjectName = gameObjects[x][y];

                if (gameObjectName != "none") {
                    var gameObject = new GameObject(x, y, gameObjectName, imageFactory);

                    this.gameObjectsContainer.addChild(gameObject.sprite);
                    this.gameObjects.push(gameObject);
                }
            }
        }
    }
}