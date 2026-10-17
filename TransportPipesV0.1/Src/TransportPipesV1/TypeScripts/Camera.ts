class Camera {

    renderer;

    STAGE_WIDTH: number;
    STAGE_HEIGHT: number;

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


    constructor() {
        this.LoadStageSize();

        this.renderer = PIXI.autoDetectRenderer(this.STAGE_WIDTH, this.STAGE_HEIGHT);
        document.body.appendChild(this.renderer.view);


    }

    Resize() {
        this.LoadStageSize();
        this.renderer.resize(this.STAGE_WIDTH, this.STAGE_HEIGHT);
    }

    Render(stage) {
        this.renderer.render(stage);
    }

    private LoadStageSize() {
        this.STAGE_WIDTH = $(window).width() - 10;
        this.STAGE_HEIGHT = $(window).height() - 10;
    }

    Update(mouseInputHandler: MouseInputHandler, objectToScale: any) {
        var deltaMouseScroll = mouseInputHandler.scroll - mouseInputHandler.preScroll;
        var tempScale = this.scale + deltaMouseScroll * 0.1;
        var newscale = Math.round(clamp(tempScale, 0.3, 2) * 10) / 10;

        var scaleNeedsUpdate = newscale != this.scale;

        if (scaleNeedsUpdate) {

            var preWidth = this.STAGE_WIDTH / this.scale;
            var newWidth = this.STAGE_WIDTH / newscale;

            var preHeight = this.STAGE_HEIGHT / this.scale;
            var newHeight = this.STAGE_HEIGHT / newscale;

            var widthVar = (newWidth - preWidth) / (this.STAGE_WIDTH / mouseInputHandler.mouseX);
            var heightVar = (newHeight - preHeight) / (this.STAGE_HEIGHT / mouseInputHandler.mouseY);

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

        this.topLeft = mouseToDiagonalChunk(0 - this.cameraOffsetX - TILE_WIDTH, 0 - this.cameraOffsetY + TILE_HEIGHT);
        this.topRight = mouseToDiagonalChunk(this.STAGE_WIDTH / this.scale - this.cameraOffsetX - TILE_WIDTH, 0 - this.cameraOffsetY + TILE_HEIGHT);
        this.botLeft = mouseToDiagonalChunk(0 - this.cameraOffsetX - TILE_WIDTH, this.STAGE_HEIGHT / this.scale - this.cameraOffsetY + TILE_HEIGHT);
        this.botRight = mouseToDiagonalChunk(this.STAGE_WIDTH / this.scale - this.cameraOffsetX - TILE_WIDTH, this.STAGE_HEIGHT / this.scale - this.cameraOffsetY + TILE_HEIGHT);
    }


    MoveChunks(chunks: Chunk[]) {
        if (this.cameraOffsetX != this.lastSetCameraX || this.cameraOffsetY != this.lastSetCameraY) {

            for (var index in chunks) {
                var chunk = chunks[index];

                chunk.moveMouse(this.cameraOffsetX, this.cameraOffsetY);
            }

            this.lastSetCameraX = this.cameraOffsetX;
            this.lastSetCameraY = this.cameraOffsetY;
        }
    }

    MoveCameraTo(x: number, y: number) {
        var result = diagonalToMouse(-x, -y);

        this.cameraOffsetX = result.x;
        this.cameraOffsetY = result.y;

        this.lastSetCameraX = this.cameraOffsetX;
        this.lastSetCameraY = this.cameraOffsetY;
    }


}