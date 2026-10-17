declare var PIXI;

class GameObject {
    xPos: number;
    yPos: number;

    sprite;

    constructor(xPos: number, yPos: number, filename: string, imageFactory: ImageFactory) {
        this.xPos = xPos;
        this.yPos = yPos;

        var iso = diagonalToMouse(xPos, yPos);


        this.sprite = imageFactory.createImage(filename);
        //this.sprite = PIXI.Sprite.fromImage("Images/GameObjects/" + filename + ".png");
        this.sprite.position.x = iso.x;
        this.sprite.position.y = iso.y;

        // bottom-left
        this.sprite.anchor.x = 0.0;
        this.sprite.anchor.y = 1;
    }

}