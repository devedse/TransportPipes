function clamp(val: number, min: number, max: number) {
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