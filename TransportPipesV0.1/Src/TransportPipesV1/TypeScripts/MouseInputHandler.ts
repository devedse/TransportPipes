class MouseInputHandler {

    preLocX: number;
    preLocY: number;

    mouseX: number;
    mouseY: number;

    scroll: number = 0;
    preScroll: number = 0;

    isMouseDragging: boolean;

    constructor() {
        var self = this;
        $(document).mousedown(function (e) {
            self.isMouseDragging = true;

            self.preLocX = e.pageX;
            self.preLocY = e.pageY;
        });

        $(document).mouseup(function (e) {
            self.isMouseDragging = false;
        });

        $(document).mousemove(function (e) {
            self.mouseX = e.pageX;
            self.mouseY = e.pageY;
        });

        $(document).bind('mousewheel', function (e) {
            if (e.originalEvent.wheelDelta > 0) {
                self.scroll += 1;
                //$(this).text('scrolling up !');
            }
            else {
                self.scroll -= 1;
                //$(this).text('scrolling down !');
            }
        });
     
    }

    updateLocToPreLoc() {
        this.preLocX = this.mouseX;
        this.preLocY = this.mouseY;
        this.preScroll = this.scroll;
    }
}



