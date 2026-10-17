interface SignalR {
    transportHub: {
        client: {
            loginResponse(success: boolean, message: string, userName: string, userId: number)
            serverHasSentBackChunkData(chunkX: number, chunkY: number, terrain: number[][], gameObjects: string[][], ownerData: number[][]);
            notifyMessage(message: string);
            notifyMessage2(message: string, duration: number);
            updateGoldPerSecond(userName: string, currentGold: number, goldPerSecond: number);
            updateGameObjectInfos(gameObjectInfos: GameObjectInfo[]);
        };
        server: {
            logMeIn(userName: string, password: string);
            registerMe(userName: string, password: string, email: string);
            requestChunk(chunkX: number, chunkY: number);
            setGameObject(x: number, y: number, gameObjectType: number);
            getStatusOfGameObject(x: number, y: number);
            calculateMyIncome();
            deleteGameObject(x: number, y: number);
            obtainGameObjectInfos();
        };
    };
} 