using System;

namespace TurnBasedRpg.Core
{
    /// <summary>
    /// A small seeded xorshift32 generator. The same seed always gives the same sequence on every
    /// runtime, so battles, tests and saved runs are reproducible.
    /// </summary>
    public sealed class Rng
    {
        uint state;

        public Rng(int seed)
        {
            state = unchecked((uint)seed * 2654435761u) ^ 0x9E3779B9u;
            if (state == 0)
            {
                state = 0x6D2B79F5u;
            }
        }

        public uint NextUInt()
        {
            uint x = state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            state = x;
            return x;
        }

        /// <summary>A number in [0, 1).</summary>
        public double NextDouble() => NextUInt() / 4294967296.0;

        /// <summary>An integer in [minInclusive, maxExclusive).</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            }
            return minInclusive + (int)(NextUInt() % (uint)(maxExclusive - minInclusive));
        }

        /// <summary>True with probability <paramref name="probability"/> (0..1).</summary>
        public bool Chance(double probability) => probability >= 1.0 || (probability > 0.0 && NextDouble() < probability);
    }
}
