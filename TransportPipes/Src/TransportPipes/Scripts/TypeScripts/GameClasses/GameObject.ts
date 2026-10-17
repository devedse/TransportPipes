class GameObject extends SelectableObject {
    ownerId: number;

    constructor(xPos: number, yPos: number, name: string, imageFactory: ImageFactory, ownerId: number) {
        var loadedSprite = imageFactory.createImage(name);
        //This loads the loadedSprite into this.sprite
        super(loadedSprite);

        this.xPos = xPos;
        this.yPos = yPos;
        this.ownerId = ownerId;

        var iso = DiagonalToMouse(xPos, yPos);
        
        //this.sprite = PIXI.Sprite.fromImage("Images/GameObjects/" + filename + ".png");
        this.sprite.position.x = iso.x;
        this.sprite.position.y = iso.y;

        // bottom-left
        this.sprite.anchor.x = 0.0;
        this.sprite.anchor.y = 1;
    }

}