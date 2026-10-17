
class ImageFactory {

    pixiLoader: any;
    jsonFile: string;
    jsonInfo: any;

    constructor(pixiLoader: any, jsonFile: string) {
        this.pixiLoader = pixiLoader;
        this.jsonFile = jsonFile;

        this.loadJson(this.jsonFile);
    }


    loadJson(jsonFile: string) {
        var jsonTemp;
        $.ajax({
            url: jsonFile,
            async: false,
            dataType: 'json',
            success: function (response) {
                jsonTemp = response;
            }
        });
        this.jsonInfo = jsonTemp;

    }

    createImage(imageName: string) {
        var count = this.jsonInfo.imageinfos[imageName].count;

        if (count == 1) {
            var image = PIXI.Sprite.fromFrame(imageName);

            return image;
        } else {

            var allImages = [];

            for (var i = 0; i < count; i++) {
                var image = PIXI.Texture.fromFrame(imageName + "-" + i);
                allImages.push(image);
            }

            var movieClip = new PIXI.MovieClip(allImages);

            movieClip.animationSpeed = 0.2;
            movieClip.play();

            return movieClip;
        }
    }
}