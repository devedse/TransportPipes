/// <reference path="../../typings/pixi/pixi.d.ts" />

class BuildWindow {

    def: Default;
    buildItemsContainer: PIXI.Container;
    contentHandler: ContentHandler;

    constructor(def: Default, stage: PIXI.Container, contentHandler: ContentHandler) {
        this.def = def;
        this.buildItemsContainer = new PIXI.Container();
        this.contentHandler = contentHandler;

        stage.addChild(this.buildItemsContainer);

        this.buildItemsContainer.y = 74; //Zoom button height + 10
        this.buildItemsContainer.x = 10; //10 pixels from the left
    }

    fillBuildMenu(gameObjectInfos: GameObjectInfo[]) {

        //Clear container
        for (var i = this.buildItemsContainer.children.length - 1; i >= 0; i--) {
            this.buildItemsContainer.removeChild(this.buildItemsContainer.children[i]);
        };
        
        //Add an empty background object
        var graphicsBackground = new PIXI.Graphics();

        this.buildItemsContainer.addChild(graphicsBackground);

        var desiredWidthHeight = 96;
        var previousY = 20;

        //TODO Clean this up a bit
        for (var i = 0; i < gameObjectInfos.length; i++) {
            var gameObject = gameObjectInfos[i];


            var sprite = this.contentHandler.imageFactory.createCroppedRectangleImage(gameObject.GameObjectTypeName);

            var scale = desiredWidthHeight / sprite.width;
            sprite.scale.set(scale, scale);
            
            sprite.y = ((desiredWidthHeight - sprite.height) / 2);
            sprite.x = 10;

            var graphicsButton = new PIXI.Graphics();
            graphicsButton.beginFill(0xB1B1B1, 0.90);
            graphicsButton.drawRoundedRect(0, 0, desiredWidthHeight + 20, desiredWidthHeight + 0, 10);
            graphicsButton.endFill();
            graphicsButton.x = 10;
            graphicsButton.y = previousY - 0;

            previousY += desiredWidthHeight + 10;
            
            //Make the button clickable
            var that = this;
            var clickOnButtonFunction = function (e) {
                that.def.transportPipesGame.setSelectedGameObjectToBuild(e.target.name);
            };

            graphicsButton.name = gameObject.GameObjectTypeName;
            graphicsButton.interactive = true;
            graphicsButton.on('click', clickOnButtonFunction);

            this.buildItemsContainer.addChild(graphicsButton);
            graphicsButton.addChild(sprite);
        }

        //Actually draw the background now that we know the width / height
        graphicsBackground.beginFill(0xF1F1F1, 0.90);
        graphicsBackground.drawRoundedRect(0, 0, desiredWidthHeight + 40, previousY + 20, 10);
        graphicsBackground.endFill();

        graphicsBackground.interactive = true;
    }
}