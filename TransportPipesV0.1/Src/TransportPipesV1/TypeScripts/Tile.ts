declare var PIXI;

class Tile {
    xPos: number;
    yPos: number;

    tileSprite: any;

    constructor(xPos: number, yPos: number, sprite: any) {
        this.xPos = xPos;
        this.yPos = yPos;

        this.tileSprite = sprite;
        this.tileSprite.position.x = xPos;
        this.tileSprite.position.y = yPos;

        // bottom-left
        this.tileSprite.anchor.x = 0.0;
        this.tileSprite.anchor.y = 1;
    }

}