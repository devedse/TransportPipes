/// <reference path="../../typings/pixi/pixi.d.ts" />

class ContentHandler {

    styleBlueText: PIXI.TextStyle;
    styleRedText: PIXI.TextStyle;
    styleHeadingText: PIXI.TextStyle;
    styleNormalText: PIXI.TextStyle;

    assetLoader: PIXI.loaders.Loader;
    imageFactory: ImageFactory;

    constructor() {
        this.assetLoader = new PIXI.loaders.Loader();

        //The name here as first parameter is actually not required
        this.assetLoader.add('combinedgameobjects', 'Images/CombinedGameObjects/combinedjson.json');
        this.assetLoader.add('transporttiles', 'Images/roadTiles/transportTiles.json');

        this.imageFactory = new ImageFactory(this.assetLoader, "Images/CombinedGameObjects/combinedjsoninfo.json");

        this.styleBlueText = { font: "20px Arial", fill: "blue" };
        this.styleRedText = { font: "40px Arial", fill: "red" };
        this.styleHeadingText = { font: "40px Arial", fill: "black" };
        this.styleNormalText = { font: "20px Arial", fill: "black" };
    }
}