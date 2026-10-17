class ImageFactory {

    assetLoader: PIXI.loaders.Loader;
    jsonFile: string;
    jsonInfo: any;

    constructor(assetLoader: PIXI.loaders.Loader, jsonFile: string) {
        this.assetLoader = assetLoader;
        this.jsonFile = jsonFile;

        this.loadJson(this.jsonFile);
    }


    loadJson(jsonFile: string) {
        var jsonTemp;
        $.ajax({
            url: jsonFile,
            async: false,
            dataType: 'json',
            success: response => {
                jsonTemp = response;
            }
        });
        this.jsonInfo = jsonTemp;

    }

    createImage(imageName: string): PIXI.Sprite {
        var count = this.jsonInfo.imageinfos[imageName].count;

        if (count == 1) {
            var obtainedImage = PIXI.Sprite.fromFrame(imageName);

            return obtainedImage;
        } else {
            var allImages: PIXI.Texture[] = [];

            for (var i = 0; i < count; i++) {
                var image = PIXI.Texture.fromFrame(imageName + "-" + i);
                allImages.push(image);
            }

            var movieClip = new PIXI.extras.MovieClip(allImages);

            movieClip.animationSpeed = 0.2;
            movieClip.play();

            return movieClip;
        }
    }

    createCroppedRectangleImage(imageName: string): PIXI.Sprite {
        var count = this.jsonInfo.imageinfos[imageName].count;

        if (count == 1) {
            var startTexture = PIXI.Texture.fromFrame(imageName);
            var croppedTexture = this.cropThisTexture(startTexture);
            var croppedSprite = new PIXI.Sprite(croppedTexture);

            return croppedSprite;
        } else {
            var allImages: PIXI.Texture[] = [];

            for (var i = 0; i < count; i++) {
                var startTexture = PIXI.Texture.fromFrame(imageName + "-" + i);
                var croppedTexture = this.cropThisTexture(startTexture);
                allImages.push(croppedTexture);
            }

            var movieClip = new PIXI.extras.MovieClip(allImages);

            movieClip.animationSpeed = 0.2;
            movieClip.play();

            return movieClip;
        }
    }

    private cropThisTexture(texture: PIXI.Texture): PIXI.Texture {
        //TODO: Do caching here or something (We can't cache sprites, but we can cache textures

        if (texture.width != 192) {
            return texture;
        }

        if (texture.height <= 192) {
            return texture;
        }

        if (texture.crop) {
            var textureCropped = new PIXI.Texture(texture, new PIXI.Rectangle(texture.crop.x, texture.crop.y + texture.height - 192, 192, 192));
            return textureCropped;
        }
        else {
            var textureCropped = new PIXI.Texture(texture, new PIXI.Rectangle(0, texture.height - 192, 192, 192));
            return textureCropped;
        }
    }
}