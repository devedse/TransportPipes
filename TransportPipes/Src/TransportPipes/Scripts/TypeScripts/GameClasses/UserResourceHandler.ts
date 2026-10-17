class UserResourceHandler {
    goldAtUpdateTime: number;
    goldPerSecond: number;
    lastGoldPerSecondUpdate: Date;

    constructor() {

    }

    setNewGoldPerSecond(currentGold: number, goldPerSecond: number) {
        this.goldAtUpdateTime = currentGold;
        this.goldPerSecond = goldPerSecond;
        this.lastGoldPerSecondUpdate = new Date();
    }

    getCurrentGold(): number {
        if (this.lastGoldPerSecondUpdate == null) {
            return 0;
        }
        
        var timeNow = new Date();

        // time difference in ms
        var timeDiff = timeNow.getTime() - this.lastGoldPerSecondUpdate.getTime();

        // strip the ms
        timeDiff /= 1000;

        return this.goldAtUpdateTime + (timeDiff * this.goldPerSecond);
    }
}