/// <reference path='Tile.ts'/>

class TileTemplate {
    filename: string;

    constructor(filename: string) {
        this.filename = filename;
    }

    createTile(xPos: number, yPos: number) {

        var sprite = PIXI.Sprite.fromFrame(this.filename);

        var tile = new Tile(xPos, yPos, sprite);
        return tile;
    }
}