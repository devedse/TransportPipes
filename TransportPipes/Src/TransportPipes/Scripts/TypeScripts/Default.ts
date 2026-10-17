/// <reference path="..\typings\jquery\jquery.d.ts" />
/// <reference path="loginwindow.ts" />
/// <reference path="gameclasses/transportpipesgame.ts" />
/// <reference path="gameclasses/user.ts" />



var def: Default;

//Happens first
$(document).ready(function () {
    console.log("document.ready");
    def = new Default();
    def.onReady();
});

//Happens later
$(window).on('load', function () {
    console.log("window.load");
    def.onLoad();
});

class Default {

    loginWindow: LoginWindow;
    currentUser: User;
    signalRHandler: SignalRHandler;

    transportPipesGame: TransportPipesGame;

    constructor() {
        this.currentUser = new User();

        this.loginWindow = new LoginWindow(this);

        this.signalRHandler = new SignalRHandler(this);
        this.signalRHandler.setupConnection();
    }

    onReady() {
        var that = this;

        //$("#testButton").on("click", function (eventArgs) {
        //    that.loginWindow.show();
        //});

        this.transportPipesGame = new TransportPipesGame(this);
    }

    onLoad() {

    }

    onSignalRUp() {
        this.loginWindow.show();
    }

}