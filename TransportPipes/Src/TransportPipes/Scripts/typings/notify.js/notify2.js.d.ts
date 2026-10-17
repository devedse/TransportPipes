interface JQueryStatic {
    notify(message: string, type: string): JQuery;
    notify(message: string, options: any): JQuery;
}

interface notifyOptions {

    // whether to hide the notification on click
    clickToHide: boolean;
    // whether to auto-hide the notification
    autoHide: boolean;
    // if autoHide, hide after milliseconds
    autoHideDelay: number;
    // show the arrow pointing at the element
    arrowShow: boolean;
    // arrow size in pixels
    arrowSize: number;
    // default positions
    elementPosition: string;
    globalPosition: string;
    // default style
    style: string;
    // default class (string or [string])
    className: string;
    // show animation
    showAnimation: string;
    // show animation duration
    showDuration: number;
    // hide animation
    hideAnimation: string;
    // hide animation duration
    hideDuration: number;
    // padding between element and notification
    gap: number;
}