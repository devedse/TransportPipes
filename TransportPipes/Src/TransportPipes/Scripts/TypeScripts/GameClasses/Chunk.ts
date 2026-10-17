class Chunk {

    chunkX: number;
    chunkY: number;

    xOffsetChunk: number;
    yOffsetChunk: number;

    loaded: boolean;
    canBeRemoved: boolean;

    displayContainer = new PIXI.Container();
    tilesContainer = new PIXI.Container();
    gameObjectsContainer = new PIXI.Container();

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

        var g = 0,
            b = 1,
            w = 2,
            l = 3; //UNLOADORD aka fragtagey drawen

        this.terrain = [
            [l, l, l, l, l, l, l, l],
            [l, l, l, l, l, l, l, l],
            [l, l, l, l, l, l, l, l],
            [l, l, l, l, l, l, l, l],
            [l, l, l, l, l, l, l, l],
            [l, l, l, l, l, l, l, l],
            [l, l, l, l, l, l, l, l],
            [l, l, l, l, l, l, l, l]
        ];


    }

    update(xMouseDiag: number, yMouseDiag: number, shouldHandleSelected: boolean): GameObject {
        return this.handleFilters(xMouseDiag, yMouseDiag, shouldHandleSelected);
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
            this.tilesContainer.removeChild(this.allTiles[index].sprite);
        }
        this.allTiles = [];


        for (var x = 0; x < this.terrain.length; x++) {
            for (var y = 0; y < this.terrain[x].length; y++) {
                var tileType = this.terrain[x][y];
                var tileTemplate = this.tileTemplates[tileType];
                var tile = tileTemplate.createTile(x, y);

                this.tilesContainer.addChild(tile.sprite);
                this.allTiles.push(tile);
            }
        }
    }

    stageGameObjects(gameObjects: string[][], imageFactory: ImageFactory, ownerData: number[][]) {

        //Temporary code to make add a game object
        for (var index in this.gameObjects) {
            this.gameObjectsContainer.removeChild(this.gameObjects[index].sprite);
        }
        this.gameObjects = [];

        if (gameObjects == null) {
            return;
        }

        for (var x = 0; x < gameObjects.length; x++) {
            for (var y = 0; y < gameObjects[x].length; y++) {

                var gameObjectName = gameObjects[x][y];

                if (gameObjectName != "none") {
                    var gameObject = new GameObject(x, y, gameObjectName, imageFactory, ownerData[x][y]);

                    this.gameObjectsContainer.addChild(gameObject.sprite);
                    this.gameObjects.push(gameObject);
                }
            }
        }
    }

    private handleFilters(xMouseDiag: number, yMouseDiag: number, shouldHandleSelected: boolean): GameObject {

        var xInChunk = Math.floor(xMouseDiag) - (8 * this.chunkX);
        var yInChunk = Math.floor(yMouseDiag) - (8 * this.chunkY);

        this.handleOwnersOnObjects(this.gameObjects);

        this.handleSelectionOnObjects(this.gameObjects, shouldHandleSelected, xInChunk, yInChunk);
        return this.handleSelectionOnObjects(this.allTiles, shouldHandleSelected, xInChunk, yInChunk);
    }

    handleOwnersOnObjects(gameObjects: GameObject[]) {
        for (var index in gameObjects) {
            var curGameObject = gameObjects[index];
            if (curGameObject.ownerId != def.currentUser.userId) {
                curGameObject.setOwner(false);
            }
            else {
                curGameObject.setOwner(true);
            }
        }
    }

    handleSelectionOnObjects(selectableObjects: SelectableObject[], shouldHandleSelected: boolean, xInChunk: number, yInChunk: number): GameObject {
        var toReturn = null;
        for (var index in selectableObjects) {
            var curSelectableObject = selectableObjects[index];
            if (shouldHandleSelected) {
                if (curSelectableObject.xPos == xInChunk && curSelectableObject.yPos == yInChunk) {
                    curSelectableObject.setSelected(true);
                    toReturn = curSelectableObject;
                } else {
                    curSelectableObject.setSelected(false);
                }
            }
            if (curSelectableObject.xPos == xInChunk && curSelectableObject.yPos == yInChunk) {
                curSelectableObject.setHovered(true);
            } else {
                curSelectableObject.setHovered(false);
            }
        }
        return toReturn;
    }
}