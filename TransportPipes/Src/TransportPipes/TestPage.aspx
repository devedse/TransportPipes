<%@ Page Title="" Language="C#" MasterPageFile="~/SuperMaster.Master" AutoEventWireup="true" CodeBehind="TestPage.aspx.cs" Inherits="TransportPipes.Schijt" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <script>
        var viewWidth = 1200;
        var viewHeight = 800;

        // Create a pixi renderer
        var renderer = new PIXI.CanvasRenderer(viewWidth, viewHeight, null, false);
        //var renderer = PIXI.autoDetectRenderer(viewWidth, viewHeight, null, false, false);
        renderer.view.className = "rendererView";

        // add render view to DOM
        document.body.appendChild(renderer.view);

        // create an new instance of a pixi stage
        var stage = new PIXI.Stage(0x00FFFF);

        // create a background texture
        var guestTexture = PIXI.Texture.fromImage("gebouw.png");

        // create a new background sprite
        var guest = new PIXI.Sprite(guestTexture);

        guest.width = 500;
        guest.height = 500;
        stage.addChild(guest);


        var guestTexture2 = PIXI.Texture.fromImage("gebouw.png");

        var guest2 = new PIXI.Sprite(guestTexture2);

        guest2.width = 500;
        guest2.height = 500;
        guest2.x = 500;
        stage.addChild(guest2);

        

        //var maskDing = new PIXI.Graphics();
        //maskDing.beginFill(0xFF0000, 0.5);
        //maskDing.drawCircle(400, 300, 300);
        //maskDing.endFill();

        //maskDing.alpha = 0.5;
        //maskDing.worldAlpha = 0.5;
        //stage.worldAlpha = 0.5;

        //guest2.worldAlpha = 0.5;
        //guest2.alpha = 0.5;
        //guest2.color = 0xFF0000;

        //guest2.shader = new PIXI.WebGLShaderManager().

        //guest2.blendMode = PIXI.blendModes.NORMAL;

        //guest2.tint = 0xFFFFFF;


        //guest2.addChild(maskSprite);


        
        //guest2.color = 0xFF0000;

        

        //var colorMatrix = [
        //    1, 0, 0, 0,
        //    0, 1, 0, 0,
        //    0, 0, 1, 0,
        //    0, 0, 0, 1
        //];

        var filterThing = 0.2;

        colorMatrix = [
            1, 0, 0, filterThing,
            0, 1, 0, filterThing,
            0, 0, 1, filterThing,
            0, 0, 0, 1
        ];
        //var filter = new PIXI.ColorMatrixFilter();
        //filter.matrix = colorMatrix;
        ////stage.filters = [filter];
        //guest2.filters = [filter];

        //guest2.tint = 0xFFFFFF;

        //guest2.mask = maskDing;

        //stage.addChild(maskDing);



        // create an array to store a refference to the dude in the pond
        var dudeArray = [];



        var counter = 0;

        //// create a bounding box box for the little dudes 
        //var dudeBoundsPadding = 100;

        //var dudeBounds = new PIXI.Rectangle(-dudeBoundsPadding,
        //									-dudeBoundsPadding, 
        //									viewWidth + dudeBoundsPadding * 2, 
        //									viewHeight + dudeBoundsPadding * 2);

        var lasers = [];
        var tick = 0;
        var frequency = 80;
        var type = 0;

        function animate() {
            if (tick > frequency) {
                tick = 0;

            }

            // increment the ticker
            tick += 1;

            // time to render the stage !
            renderer.render(stage);

            // request another animation frame...
            requestAnimationFrame(animate);
        }
        requestAnimationFrame(animate);
    </script>
</asp:Content>
