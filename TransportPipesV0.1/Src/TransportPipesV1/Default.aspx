<%@ Page Title="" Language="C#" MasterPageFile="~/TheMasterPage.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="TransportPipesV1.Default" %>

<asp:Content ID="ContentHead" ContentPlaceHolderID="ContentPlaceHolderHead" runat="server">
    <meta name="viewport" content="initial-scale=1 maximum-scale=1 user-scalable=0" />
    <style>
        body {
            margin: 0;
            padding: 0;
            background-color: #EEFFFF;
        }
    </style>
</asp:Content>

<asp:Content ID="Content1" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">

    <%--<div id="grid" style="background-image: url(Images/background.png); width: 2000px; height: 2000px;"></div>--%>
    <script src="TypeScripts\GlobalVariables.js"></script>
    <script src="TypeScripts\Chunk.js"></script>
    <script src="TypeScripts\DiagonalHelper.js"></script>
    <script src="TypeScripts\GeneralHelper.js"></script>
    <script src="TypeScripts\Tile.js"></script>
    <script src="TypeScripts\TileTemplate.js"></script>
    <script src="TypeScripts\TransportPipeGame.js"></script>
    <script src="TypeScripts\GameObject.js"></script>
    <script src="TypeScripts\Camera.js"></script>
    <script src="TypeScripts\ImageFactory.js"></script>
    <script src="TypeScripts\MouseInputHandler.js"></script>

    <script type="text/javascript">
        $(function () {
            $(document).ready(function () {



                var transportPipeGame = new TransportPipeGame();


                function resize() {
                    transportPipeGame.resize();
                }

                resize(); //Call resize once to fix gui
                $(window).resize(resize);


                // account for tile thickness
                /* globals PIXI, kd, requestAnimFrame */
                "use strict";

                //events like this for now :/
                // game loop optimized keyboard handling
                kd.UP.press(transportPipeGame.moveUp);
                kd.DOWN.press(transportPipeGame.moveDown);
                kd.LEFT.press(transportPipeGame.moveLeft);
                kd.RIGHT.press(transportPipeGame.moveRight);

                kd.SPACE.press(pressingSpace);
                kd.F.press(pressingF);
                kd.W.press(pressingW);

                kd.B.press(showBiggestWater);

                function showBiggestWater(e) {
                    transportPipeGame.camera.MoveCameraTo(11133, 14370);
                }


                function pressingSpace(e) {
                    var xposm = transportPipeGame.mouseInputHandler.preLocX / transportPipeGame.camera.scale - transportPipeGame.camera.cameraOffsetX - TILE_WIDTH;
                    var yposm = transportPipeGame.mouseInputHandler.preLocY / transportPipeGame.camera.scale - transportPipeGame.camera.cameraOffsetY + TILE_HEIGHT;
                    var coordibro = mouseToDiagonal(xposm, yposm);

                    transportPipeGame.serverConnectionThing.hub.server.pressingSpace(coordibro.x, coordibro.y);
                }


                function pressingF(e) {
                    var xposm = transportPipeGame.mouseInputHandler.preLocX / transportPipeGame.camera.scale - transportPipeGame.camera.cameraOffsetX - TILE_WIDTH;
                    var yposm = transportPipeGame.mouseInputHandler.preLocY / transportPipeGame.camera.scale - transportPipeGame.camera.cameraOffsetY + TILE_HEIGHT;
                    var coordibro = mouseToDiagonal(xposm, yposm);

                    transportPipeGame.serverConnectionThing.hub.server.pressingF(coordibro.x, coordibro.y);
                }

                function pressingW(e) {
                    var xposm = transportPipeGame.mouseInputHandler.preLocX / transportPipeGame.camera.scale - transportPipeGame.camera.cameraOffsetX - TILE_WIDTH;
                    var yposm = transportPipeGame.mouseInputHandler.preLocY / transportPipeGame.camera.scale - transportPipeGame.camera.cameraOffsetY + TILE_HEIGHT;
                    var coordibro = mouseToDiagonal(xposm, yposm);

                    transportPipeGame.serverConnectionThing.hub.server.pressingW(coordibro.x, coordibro.y);
                }

                function start() {

                    function animate() {

                        // keyboard handler
                        kd.tick();

                        transportPipeGame.animate();

                        requestAnimFrame(animate);

                        transportPipeGame.render();
                    }
                    requestAnimFrame(animate);
                }

                function ServerConnectionThing() {
                    this.hub = $.connection.defaultHub;

                    this.signalRup = false;

                    //this.liveStatusus = ko.observableArray([]);
                    //var liveStatusus = this.liveStatusus;

                    this.init = function () {
                        console.log("init");
                        //GoRequestChunk(0, 0);
                        //GoRequestChunk(0, 1);
                        //GoRequestChunk(0, 2);
                        //GoRequestChunk(0, 3);
                        //GoRequestChunk(0, 4);
                        //GoRequestChunk(0, 5);
                        //GoRequestChunk(0, 6);
                        //GoRequestChunk(0, 7);
                        //GoRequestChunk(0, 8);
                        //GoRequestChunk(0, 9);
                        //GoRequestChunk(0, 10);
                        //GoRequestChunk(0, 11);

                        //avatar = stageAvatar(0, 0, chunks["0_0"]);

                        this.signalRup = true;
                    }

                    this.hub.client.serverHasSentBackChunkData = function (x, y, data, gameObjectsData, serverMessage) {
                        console.log(x + "_" + y + ": " + data);

                        var chunkImWorkingOn = transportPipeGame.chunks[x + "_" + y];

                        if (chunkImWorkingOn != undefined) {

                            chunkImWorkingOn.terrain = data;

                            chunkImWorkingOn.loaded = true;

                            chunkImWorkingOn.stageMap();

                            chunkImWorkingOn.stageGameObjects(gameObjectsData, transportPipeGame.imageFactory);

                            transportPipeGame.serverText.setText(serverMessage);
                        }
                    }
                }

                transportPipeGame.serverConnectionThing = new ServerConnectionThing();
                $.connection.hub.start().done(function () {
                    transportPipeGame.serverConnectionThing.init();
                });

                transportPipeGame.loader.onComplete = start;
                transportPipeGame.loader.load();
            });
        });
    </script>


</asp:Content>
