function Clamp(val: number, min: number, max: number) {
    return Math.max(min, Math.min(max, val))
}

function GetValuesFromDict(dict: Chunk[]) {
    var retval: Chunk[] = [];

    for (var index in dict) {
        var val = dict[index];
        retval.push(val);
    }

    return retval;
}

function Compare(chunkOne: Chunk, chunkTwo: Chunk) {

    if (chunkOne.chunkX + chunkOne.chunkY < chunkTwo.chunkX + chunkTwo.chunkY) {
        return -1;
    } else if (chunkOne.chunkX + chunkOne.chunkY > chunkTwo.chunkX + chunkTwo.chunkY) {
        return 1;
    }
    return 0;
}

function Distance(x1: number, y1: number, x2: number, y2: number) {
    return Math.sqrt(((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1)));
}

function CreateButton(buttonText: string, textStyle: PIXI.TextStyle, borderWidth: number = 10): PIXI.Graphics {
    var text = new PIXI.Text(buttonText, textStyle);
    text.x = 10;
    text.y = 10;

    var button = new PIXI.Graphics();

    button.beginFill(0xBBBBBB, 0.90);
    button.drawRoundedRect(0, 0, text.width + 2 * borderWidth, text.height + 2 * borderWidth, borderWidth);
    button.endFill();
    button.interactive = true;

    button.addChild(text);

    return button;
}