class MouseInputHandler {

    preLocX: number;
    preLocY: number;

    mouseX: number;
    mouseY: number;

    scroll: number = 0;
    preScroll: number = 0;

    isMouseDragging: boolean;

    constructor(chunksContainer: PIXI.Container) {

        var mouseDownEvent = (e: PIXI.interaction.InteractionEvent) => {
            this.isMouseDragging = true;

            this.preLocX = e.data.global.x;
            this.preLocY = e.data.global.y;
            this.mouseX = e.data.global.x;
            this.mouseY = e.data.global.y;
        };

        var mouseUpEvent = (e: PIXI.interaction.InteractionEvent) => {
            this.isMouseDragging = false;
        };

        var mouseMoveEvent = (e: PIXI.interaction.InteractionEvent) => {
            this.mouseX = e.data.global.x;
            this.mouseY = e.data.global.y;
        };

        chunksContainer.on('mousedown', mouseDownEvent);
        chunksContainer.on('touchstart', mouseDownEvent);

        chunksContainer.on('mouseup', mouseUpEvent);
        chunksContainer.on('mouseupoutside', mouseUpEvent);
        chunksContainer.on('touchend', mouseUpEvent);
        chunksContainer.on('touchendoutside', mouseUpEvent);

        chunksContainer.on('mousemove', mouseMoveEvent);
        chunksContainer.on('touchmove', mouseMoveEvent);

        $(document).bind('mousewheel', e => {
            if ((<any>e.originalEvent).wheelDelta > 0) {
                this.scroll += 1;
            }
            else {
                this.scroll -= 1;
            }
        });
    }

    updateLocToPreLoc() {
        this.preLocX = this.mouseX;
        this.preLocY = this.mouseY;
        this.preScroll = this.scroll;
    }
}