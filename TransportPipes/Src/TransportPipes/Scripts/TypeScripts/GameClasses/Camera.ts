class Camera {

    renderer: PIXI.SystemRenderer;

    stageWidth: number;
    stageHeight: number;

    cameraOffsetX: number = 0;
    cameraOffsetY: number = 0;

    lastSetCameraX: number = 0;
    lastSetCameraY: number = 0;

    speedX: number = 0;
    speedY: number = 0;

    topLeft: any;
    topRight: any;
    botLeft: any;
    botRight: any;

    scale: number = 1;

    static usingWebGl: boolean = false;


    constructor() {
        this.loadStageSize();

        var options = {
            view: null,
            resolution: 1,
            antialias: true,
            forceFXAA: false,
            autoResize: false,
            transparent: true,
            backgroundColor: 0xFFFFFF,
            clearBeforeRender: true,
            preserveDrawingBuffer: false
        };

        this.renderer = PIXI.autoDetectRenderer(this.stageWidth, this.stageHeight, options, false);

        if (this.renderer instanceof PIXI.WebGLRenderer) {
            Camera.usingWebGl = true;
            console.log("Using webgl :)");
        }
        else {
            console.log("Using canvas :(");
        }

        document.body.appendChild(this.renderer.view);
    }

    resize() {
        this.loadStageSize();
        this.renderer.resize(this.stageWidth, this.stageHeight);
    }

    render(stage: PIXI.Container) {
        this.renderer.render(stage);
    }

    private loadStageSize() {
        this.stageWidth = $(window).width() - 10;
        this.stageHeight = $(window).height() - 10;
    }

    update(mouseInputHandler: MouseInputHandler, objectToScale: PIXI.Container) {
        var deltaMouseScroll = mouseInputHandler.scroll - mouseInputHandler.preScroll;
        var tempScale = this.scale + deltaMouseScroll * 0.1;
        var newscale = Math.round(Clamp(tempScale, 0.3, 2) * 10) / 10;

        var scaleNeedsUpdate = newscale != this.scale;

        if (scaleNeedsUpdate) {

            var preWidth = this.stageWidth / this.scale;
            var newWidth = this.stageWidth / newscale;

            var preHeight = this.stageHeight / this.scale;
            var newHeight = this.stageHeight / newscale;

            var widthVar = (newWidth - preWidth) / (this.stageWidth / mouseInputHandler.mouseX);
            var heightVar = (newHeight - preHeight) / (this.stageHeight / mouseInputHandler.mouseY);

            this.cameraOffsetX += widthVar;
            this.cameraOffsetY += heightVar;

            objectToScale.scale.x = newscale;
            objectToScale.scale.y = newscale;
            this.scale = newscale;
        }

        if (mouseInputHandler.isMouseDragging) {
            this.cameraOffsetX += (mouseInputHandler.mouseX - mouseInputHandler.preLocX) / this.scale;
            this.cameraOffsetY += (mouseInputHandler.mouseY - mouseInputHandler.preLocY) / this.scale;
        }
        else {

            this.speedX /= 1.1;
            this.speedY /= 1.1;

            if (Math.abs(this.speedX) < 0.1) {
                this.speedX = 0;
            }
            if (Math.abs(this.speedY) < 0.1) {
                this.speedY = 0;
            }

            this.cameraOffsetX += this.speedX;
            this.cameraOffsetY += this.speedY;
        }

        if (!scaleNeedsUpdate) {
            this.speedX = this.cameraOffsetX - this.lastSetCameraX;
            this.speedY = this.cameraOffsetY - this.lastSetCameraY;
        }

        this.topLeft = MouseToDiagonalChunk(0 - this.cameraOffsetX - TILE_WIDTH, 0 - this.cameraOffsetY + TILE_HEIGHT);
        this.topRight = MouseToDiagonalChunk(this.stageWidth / this.scale - this.cameraOffsetX - TILE_WIDTH, 0 - this.cameraOffsetY + TILE_HEIGHT);
        this.botLeft = MouseToDiagonalChunk(0 - this.cameraOffsetX - TILE_WIDTH, this.stageHeight / this.scale - this.cameraOffsetY + TILE_HEIGHT);
        this.botRight = MouseToDiagonalChunk(this.stageWidth / this.scale - this.cameraOffsetX - TILE_WIDTH, this.stageHeight / this.scale - this.cameraOffsetY + TILE_HEIGHT);
    }


    moveChunks(chunks: Chunk[]) {
        if (this.cameraOffsetX != this.lastSetCameraX || this.cameraOffsetY != this.lastSetCameraY) {

            for (var index in chunks) {
                var chunk = chunks[index];

                chunk.moveMouse(this.cameraOffsetX, this.cameraOffsetY);
            }

            this.lastSetCameraX = this.cameraOffsetX;
            this.lastSetCameraY = this.cameraOffsetY;
        }
    }

    moveCameraTo(x: number, y: number) {
        var result = DiagonalToMouse(-x, -y);

        this.cameraOffsetX = result.x;
        this.cameraOffsetY = result.y;

        this.lastSetCameraX = this.cameraOffsetX;
        this.lastSetCameraY = this.cameraOffsetY;
    }


}