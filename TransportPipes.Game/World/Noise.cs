namespace TransportPipes.Game.World;

/// <summary>Deterministic hashing, random numbers and smooth value noise for world generation</summary>
public static class Noise
{
    public static ulong Hash(params long[] values)
    {
        ulong h = 0x9E3779B97F4A7C15UL;
        foreach (var value in values)
        {
            h ^= (ulong)value + 0x9E3779B97F4A7C15UL + (h << 6) + (h >> 2);
            h = Mix(h);
        }
        return h;
    }

    public static ulong Mix(ulong z)
    {
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>A hash mapped to [0, 1)</summary>
    public static double Unit(params long[] values) => (Hash(values) >> 11) * (1.0 / (1UL << 53));

    /// <summary>Smooth value noise in [0, 1) with features of about <paramref name="cell"/> tiles</summary>
    public static double Value(int seed, double x, double y, double cell)
    {
        var fx = x / cell;
        var fy = y / cell;
        var gx = (long)Math.Floor(fx);
        var gy = (long)Math.Floor(fy);
        var tx = fx - gx;
        var ty = fy - gy;
        var sx = tx * tx * (3 - 2 * tx);
        var sy = ty * ty * (3 - 2 * ty);
        var a = Unit(seed, gx, gy);
        var b = Unit(seed, gx + 1, gy);
        var c = Unit(seed, gx, gy + 1);
        var d = Unit(seed, gx + 1, gy + 1);
        return a + (b - a) * sx + (c - a) * sy + (a - b - c + d) * sx * sy;
    }

    /// <summary>Two octaves of value noise</summary>
    public static double Fractal(int seed, double x, double y, double cell) =>
        Value(seed, x, y, cell) * 0.7 + Value(seed + 17, x, y, cell / 2.5) * 0.3;
}

/// <summary>A small deterministic random generator (SplitMix64)</summary>
public sealed class SeededRandom
{
    private ulong _state;

    public SeededRandom(ulong seed)
    {
        _state = seed;
    }

    public ulong NextUlong()
    {
        _state += 0x9E3779B97F4A7C15UL;
        return Noise.Mix(_state);
    }

    public double NextDouble() => (NextUlong() >> 11) * (1.0 / (1UL << 53));

    public int Next(int maxExclusive) => (int)(NextDouble() * maxExclusive);

    public int Next(int minInclusive, int maxExclusive) => minInclusive + Next(maxExclusive - minInclusive);

    public void Shuffle<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
