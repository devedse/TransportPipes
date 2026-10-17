class BuildConfirmWindow {

    def: Default;
    thisWindowContainer: PIXI.Container;
    contentHandler: ContentHandler;

    bounds: PIXI.Rectangle;

    constructor(def: Default, stage: PIXI.Container, contentHandler: ContentHandler, gameObjectInfo: GameObjectInfo, tilePos: any) {
        this.def = def;
        this.thisWindowContainer = new PIXI.Container();
        this.contentHandler = contentHandler;

        stage.addChild(this.thisWindowContainer);

        this.thisWindowContainer.x = 400;
        this.thisWindowContainer.y = 200;

        var fadityOfBorder = 10;

        var graphicsBackground = new PIXI.Graphics();
        this.thisWindowContainer.addChild(graphicsBackground);



        var nameText = new PIXI.Text(gameObjectInfo.GameObjectTypeName, contentHandler.styleHeadingText);
        nameText.x = fadityOfBorder;
        nameText.y = fadityOfBorder;
        this.thisWindowContainer.addChild(nameText);

        var imageOfBuilding = contentHandler.imageFactory.createImage(gameObjectInfo.GameObjectTypeName);
        imageOfBuilding.x = fadityOfBorder;
        imageOfBuilding.y = nameText.y + nameText.height + 10;
        this.thisWindowContainer.addChild(imageOfBuilding);



        var consumptionInfoString = "Consumption Info:";
        for (var i = 0; i < gameObjectInfo.ConsumptionInfo.length; i++) {
            var currentConsumption = gameObjectInfo.ConsumptionInfo[i];
            consumptionInfoString += "\n" + currentConsumption.ResourceTypeName + ": " + (currentConsumption.Value * 3600) + " per hour";
        }

        var productionInfoString = "Production Info:";
        for (var i = 0; i < gameObjectInfo.ProductionInfo.length; i++) {
            var currentProduction = gameObjectInfo.ProductionInfo[i];
            productionInfoString += "\n" + currentProduction.ResourceTypeName + ": " + (currentProduction.Value * 3600) + " per hour";
        }


        var descriptionText = new PIXI.Text("Price: " + gameObjectInfo.Price + "\n\nBuild Time: " + gameObjectInfo.BuildTimeInSeconds + " seconds\n\n" + consumptionInfoString + "\n\n" + productionInfoString, contentHandler.styleNormalText);
        descriptionText.x = fadityOfBorder;
        descriptionText.y = imageOfBuilding.y + imageOfBuilding.height + 10;
        this.thisWindowContainer.addChild(descriptionText);


        var buildButton = CreateButton("Build", contentHandler.styleNormalText, 10);
        var cancelButton = CreateButton("Cancel", contentHandler.styleNormalText, 10);

        buildButton.x = fadityOfBorder;
        cancelButton.x = buildButton.x + buildButton.width + 10;
        buildButton.y = descriptionText.y + descriptionText.height + 20;
        cancelButton.y = buildButton.y;

        this.thisWindowContainer.addChild(buildButton);
        this.thisWindowContainer.addChild(cancelButton);

        graphicsBackground.beginFill(0xF1F1F1, 0.90);
        graphicsBackground.drawRoundedRect(0, 0, Math.max(nameText.width, imageOfBuilding.width, descriptionText.width, cancelButton.x + cancelButton.width) + 2 * fadityOfBorder, buildButton.y + buildButton.height + fadityOfBorder, 10);
        graphicsBackground.endFill();
        graphicsBackground.interactive = true;

        buildButton.on('click', () => {
            stage.removeChild(this.thisWindowContainer);
            this.def.signalRHandler.setGameObject(tilePos.x, tilePos.y, gameObjectInfo.GameObjectType);
        });

        cancelButton.on('click', () => {
            stage.removeChild(this.thisWindowContainer);
        });
    }
}