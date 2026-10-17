/// <reference path="../../typings/signalr/signalr-1.0.d.ts" />
/// <reference path="signalrhandler.d.ts" />
/// <reference path="../default.ts" />
/// <reference path="../../typings/notify.js/notify2.js.d.ts" />


class SignalRHandler {

    def: Default;

    signalRUp: boolean = false;

    constructor(def: Default) {
        this.def = def;
    }

    setupConnection() {
        var that = this;

        this.registerReceivers();

        $.connection.hub.start().done(function () {
            that.connectionStarting();
        });
    }

    connectionStarting() {
        console.log("SignalR UP");

        this.def.onSignalRUp();
        this.signalRUp = true;
    }

    registerReceivers() {
        var that = this;
        $.connection.transportHub.client.loginResponse = function (success: boolean, message: string, userName: string, userId: number) {

            if (success) {
                that.def.currentUser.userName = userName;
                that.def.currentUser.userId = userId;
                that.def.currentUser.loggedIn = true;
                that.def.loginWindow.hide();

                that.calculateMyIncome();
                that.obtainGameObjectInfos();
            }
            else {
                that.def.loginWindow.loginFailed(message);
            }
        }

        $.connection.transportHub.client.serverHasSentBackChunkData = function (chunkX: number, chunkY: number, terrain: number[][], gameObjects: string[][], ownerData: number[][]) {
            that.def.transportPipesGame.chunkReceivedFromServer(chunkX, chunkY, terrain, gameObjects, ownerData);
        }

        $.connection.transportHub.client.notifyMessage = function (message: string) {
            $.notify(message, { className: "info", autoHide: false });
        }

        $.connection.transportHub.client.notifyMessage2 = function (message: string, duration: number) {

            if (message.indexOf("Entered Write Lock") >= 0) {
                $.notify(message, { className: "warning", autoHide: true, autoHideDelay: duration });
            }
            else {

                $.notify(message, { className: "info", autoHide: true, autoHideDelay: duration });
            }
        }

        $.connection.transportHub.client.updateGoldPerSecond = function (userName: string, currentGold: number, goldPerSecond: number) {
            if (def.currentUser !== undefined && def.currentUser.userName === userName) {
                def.currentUser.userResourceHandler.setNewGoldPerSecond(currentGold, goldPerSecond);
            }
        }

        $.connection.transportHub.client.updateGameObjectInfos = function (gameObjectInfos: GameObjectInfo[]) {
            for (var gameObjectInfoNumber in gameObjectInfos) {
                var gameObjectInfo = gameObjectInfos[gameObjectInfoNumber];

                console.log(gameObjectInfo.GameObjectTypeName);
                console.log(gameObjectInfo.GameObjectType);
                console.log(gameObjectInfo.Price);
                console.log(gameObjectInfo.ConsumptionInfo.length);
                console.log(gameObjectInfo.ProductionInfo.length);
                console.log(gameObjectInfo.BuildTimeInSeconds);

                for (var resourceInfoNumber in gameObjectInfo.ProductionInfo) {
                    var resourceInfo = gameObjectInfo.ProductionInfo[resourceInfoNumber];

                    console.log(resourceInfo.ResourceTypeName);
                    console.log(resourceInfo.ResourceType);
                    console.log(resourceInfo.Value);
                }

                for (var resourceInfoNumber in gameObjectInfo.ConsumptionInfo) {
                    var resourceInfo = gameObjectInfo.ConsumptionInfo[resourceInfoNumber];

                    console.log(resourceInfo.ResourceTypeName);
                    console.log(resourceInfo.ResourceType);
                    console.log(resourceInfo.Value);
                }
            }

            def.transportPipesGame.gameObjectInfoHelper.gameObjectInfos = gameObjectInfos;
            def.transportPipesGame.buildWindow.fillBuildMenu(gameObjectInfos);
        }
    }

    logMeIn(userName: string, password: string) {
        $.connection.transportHub.server.logMeIn(userName, password);
    }

    registerMe(userName: string, password: string, email: string) {
        $.connection.transportHub.server.registerMe(userName, password, email);
    }

    requestChunk(chunkX: number, chunkY: number) {
        $.connection.transportHub.server.requestChunk(chunkX, chunkY);
    }

    setGameObject(x: number, y: number, gameObjectType: number) {
        $.connection.transportHub.server.setGameObject(x, y, gameObjectType);
    }

    getStatusOfGameObject(x: number, y: number) {
        $.connection.transportHub.server.getStatusOfGameObject(x, y);
    }

    calculateMyIncome() {
        $.connection.transportHub.server.calculateMyIncome();
    }

    deleteGameObject(x: number, y: number) {
        $.connection.transportHub.server.deleteGameObject(x, y);
    }

    obtainGameObjectInfos() {
        $.connection.transportHub.server.obtainGameObjectInfos();
    }
} 