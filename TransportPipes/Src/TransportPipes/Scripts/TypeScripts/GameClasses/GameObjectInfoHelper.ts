class GameObjectInfoHelper {
    gameObjectInfos: GameObjectInfo[];

    getGameObjectInfoBasedOnName(gameObjectName: string): GameObjectInfo {
        for (var i = 0; i < this.gameObjectInfos.length; i++) {
            var gameObject = this.gameObjectInfos[i];
            if (gameObject.GameObjectTypeName == gameObjectName) {
                return gameObject;
            }
        }
        return null;
    }
}