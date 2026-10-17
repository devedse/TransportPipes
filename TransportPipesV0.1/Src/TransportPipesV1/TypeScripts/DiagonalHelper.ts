function mouseToDiagonal(x: number, y: number) {
    return {
        x: x / (TILE_WIDTH * 2) + y / TILE_HEIGHT,
        y: y / TILE_WIDTH - x / (TILE_HEIGHT * 2)
    };
}

function mouseToDiagonalChunk(x: number, y: number) {
    return {
        x: Math.floor((x / (TILE_WIDTH * 2) + y / TILE_HEIGHT) / 8),
        y: Math.floor((y / TILE_WIDTH - x / (TILE_HEIGHT * 2)) / 8)
    };
}


function diagonalToMouse(x: number, y: number) {
    return {
        x: x * TILE_WIDTH - y * TILE_WIDTH,
        y: x * (TILE_HEIGHT / 2) + y * (TILE_HEIGHT / 2)
    };
}