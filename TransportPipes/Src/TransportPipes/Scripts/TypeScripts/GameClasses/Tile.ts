class Tile extends SelectableObject {
    isoXPos: number;
    isoYPos: number;

    constructor(xPos: number, yPos: number, sprite: PIXI.Sprite) {
        //This loads the sprite into the this.sprite property
        super(sprite);

        this.xPos = xPos;
        this.yPos = yPos;

        var iso = DiagonalToMouse(xPos, yPos);

        this.isoXPos = iso.x;
        this.isoYPos = iso.y;

        this.sprite.position.x = iso.x;
        this.sprite.position.y = iso.y;

        // bottom-left
        this.sprite.anchor.x = 0.0;
        this.sprite.anchor.y = 1;
    }

}