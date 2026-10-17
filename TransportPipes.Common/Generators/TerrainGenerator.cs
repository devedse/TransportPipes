namespace TransportPipes.Common.Generators;

public static class TerrainGenerator
{
    public static int[,] GenerateTerrain(int xChunk, int yChunk)
    {
        int[,] maparray = new int[StaticConfigInfo.ChunkWidth, StaticConfigInfo.ChunkHeight];

        for (int y = 0; y < StaticConfigInfo.ChunkHeight; y++)
        {
            for (int x = 0; x < StaticConfigInfo.ChunkWidth; x++)
            {
                float xInMap = xChunk * StaticConfigInfo.ChunkWidth + x;
                float yInMap = yChunk * StaticConfigInfo.ChunkHeight + y;

                int noiseValue = Noise.GenerateTile(xInMap, yInMap);
                maparray[x, y] = noiseValue;
            }
        }

        if (xChunk == 0 && yChunk == 0)
        {
            maparray[0, 0] = 3;
        }

        return maparray;
    }
}
