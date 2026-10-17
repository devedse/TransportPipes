<%@ Page Title="" Language="C#" MasterPageFile="~/SuperMaster.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="TransportPipes.Default" %>

<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">


    <script src="Scripts/TypeScripts/Default.js"></script>
    <script src="Scripts/TypeScripts/LoginWindow.js"></script>

    <script src="Scripts/TypeScripts/GameClasses/BuildConfirmWindow.js"></script>
    <script src="Scripts/TypeScripts/GameClasses/BuildWindow.js"></script>
    <script src="Scripts/TypeScripts/GameClasses/SelectableObject.js"></script>
    <script src="Scripts/TypeScripts/GameClasses/Camera.js"></script>
    <script src="Scripts/TypeScripts/GameClasses/Chunk.js"></script>
    <script src="Scripts/TypeScripts/GameClasses/ContentHandler.js"></script>
    <script src="Scripts/TypeScripts/GameClasses/GameObject.js"></script>
    <script src="Scripts/TypeScripts/GameClasses/GlobalVariables.js"></script>
    <script src="Scripts/TypeScripts/GameClasses/ImageFactory.js"></script>
    <script src="Scripts/TypeScripts/GameClasses/MouseInputHandler.js"></script>
    <script src="Scripts/TypeScripts/GameClasses/Tile.js"></script>
    <script src="Scripts/TypeScripts/GameClasses/TileTemplate.js"></script>
    <script src="Scripts/TypeScripts/GameClasses/TransportPipesGame.js"></script>
    <script src="Scripts/TypeScripts/GameClasses/User.js"></script>
    <script src="Scripts/TypeScripts/GameClasses/UserResourceHandler.js"></script>
    <script src="Scripts/TypeScripts/GameClasses/GameObjectInfoHelper.js"></script>

    <div id="loginWindow" class="popyWindow animatable" data-bind="css: { hiddenthing: !loginWindowVisible() }">
        <div>
            <div>
                <input type="text" data-bind="value: userName" id="usernameField" placeholder="Username" />
                <input type="password" data-bind="value: password" id="passwordField" placeholder="Password" />
                <div>
                    <input data-bind="value: email, css: { hiddenComplete: loginModus() }" type="email" id="emailField" placeholder="Email" class="animatable" />
                </div>
            </div>
            <input type="button" id="loginButton" value="Login" data-bind="css: { hiddenWidth: !loginModus(), disabled: !buttonsEnabled() }, enable: buttonsEnabled()" class="button animatable delayedFast right" />
            <input type="button" id="registerButton" value="Register" data-bind="css: { registerModus: !loginModus(), disabled: !buttonsEnabled() }, enable: buttonsEnabled()" class="button animatable left" />
        </div>
        <span style="display: block" data-bind="text: errorMessage()" class="errorMessage"></span>
    </div>

</asp:Content>
