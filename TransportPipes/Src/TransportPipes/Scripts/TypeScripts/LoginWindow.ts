/// <reference path="default.ts" />
/// <reference path="signalr/signalrhandler.ts" />

class LoginWindow {
    def: Default;

    loginWindowVisible: KnockoutObservable<boolean> = ko.observable(false);

    userName: KnockoutObservable<string> = ko.observable("Devedse");
    password: KnockoutObservable<string> = ko.observable("a");
    email: KnockoutObservable<string> = ko.observable("davydavidse@gmail.com");

    loginModus: KnockoutObservable<boolean> = ko.observable(true);
    buttonsEnabled: KnockoutObservable<boolean> = ko.observable(true);

    errorMessage: KnockoutObservable<string> = ko.observable("");

    constructor(def: Default) {
        this.def = def;

        var that = this;

        ko.applyBindings(that);


        $("#loginButton").on("click", function (eventArgs) {
            that.loginClicked();
        });

        $("#registerButton").on("click", function (eventArgs) {
            that.registerClicker();
        });
    }

    loginClicked() {
        this.errorMessage();

        var userNameTyped = this.userName.peek();
        var passwordHashed = CryptoJS.SHA512(this.password.peek()).toString();
        this.def.signalRHandler.logMeIn(userNameTyped, passwordHashed);

        this.buttonsEnabled(false);
    }

    registerClicker() {
        this.errorMessage();

        if (this.loginModus.peek()) {
            this.loginModus(false);
        }
        else {
            var userNameTyped = this.userName.peek();
            var passwordHashed = CryptoJS.SHA512(this.password.peek()).toString();
            var emailTyped = this.email.peek();
            this.def.signalRHandler.registerMe(userNameTyped, passwordHashed, emailTyped);

            this.buttonsEnabled(false);
        }
    }

    show() {
        this.loginWindowVisible(true);
        //this.loginClicked();

        this.reset();
    }

    hide() {
        this.loginWindowVisible(false);

        this.reset();
    }

    reset() {
        this.loginModus(true);
        this.buttonsEnabled(true);

        this.userName("Devedse");
        this.password("a");
        this.email("");
        this.errorMessage("");
    }

    loginFailed(message: string) {
        var that = this;

        this.buttonsEnabled(true);

        this.errorMessage(message);

        var animation = "shake";

        var element = $("#loginWindow");

        element.addClass('animated ' + animation);
        //wait for animation to finish before removing classes
        window.setTimeout(function () {
            element.removeClass('animated ' + animation);
        }, 1000);

    }
} 